// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace MatPlotLibNet.Wiki.Tests;

/// <summary>The wiki: a folder of pages, found beside the repository where GitHub keeps it as a repository of its own.</summary>
/// <param name="folder">The wiki's folder.</param>
internal sealed class Wiki(string folder)
{
    /// <summary>The command that puts the wiki where this suite looks for it, run from the repository's root.</summary>
    public const string CloneCommand = "git clone https://github.com/xkqg/MatPlotLibNet.wiki.git ../MatPlotLibNet.wiki";

    /// <summary>The wiki beside the repository, where a contributor clones it and the build machine does too.</summary>
    public static Wiki BesideTheRepository { get; } = new(Path.GetFullPath(Path.Join(Repository.Root, "..", "MatPlotLibNet.wiki")));

    /// <summary>The wiki's folder.</summary>
    public string Folder { get; } = folder;

    /// <summary>Every page of the wiki, its blocks read into programs, cells and blocks for another head.</summary>
    /// <returns>The pages, by file name.</returns>
    /// <exception cref="DirectoryNotFoundException">The wiki is not there; the message names the command that clones it.</exception>
    public IReadOnlyList<WikiPage> Pages()
    {
        if (!File.Exists(Path.Join(Folder, "Home.md")))
        {
            throw new DirectoryNotFoundException(
                $"The wiki is not beside the repository, at {Folder}. Clone it there from the repository's root, as the build machine does: {CloneCommand}");
        }

        return [.. Directory.GetFiles(Folder, "*.md")
            .Select(Path.GetFileName)
            .Order(StringComparer.Ordinal)
            .Select(name => WikiPage.Read(name!, Text(name!)))];
    }

    /// <summary>The text of one page, a line feed between its lines.</summary>
    /// <param name="name">The page's file name.</param>
    /// <returns>Its text.</returns>
    public string Text(string name) => File.ReadAllText(Path.Join(Folder, name)).ReplaceLineEndings("\n");
}

/// <summary>A C# block of a page: the line its code starts on, and the code.</summary>
/// <param name="Line">The line its code starts on, from one.</param>
/// <param name="Code">The code.</param>
internal readonly record struct WikiBlock(int Line, string Code);

/// <summary>What is compiled as one program: the line of the block it is compiled for, and the blocks it is made of, in order.</summary>
/// <param name="Line">The line of the block it is compiled for.</param>
/// <param name="Blocks">The blocks, in the order they stand.</param>
internal readonly record struct WikiProgram(int Line, IReadOnlyList<WikiBlock> Blocks);

/// <summary>
/// A page: its name, the programs its C# blocks make, its notebook cells, the blocks that need a head this suite cannot
/// reference, and the faults in its fences.
/// </summary>
/// <param name="Name">The page's file name.</param>
/// <param name="Programs">The programs its blocks make, one for each block that is compiled.</param>
/// <param name="Cells">The notebook cells it holds: not compiled, counted.</param>
/// <param name="Needing">The blocks that need a Windows or workload head, or the repository's own test project: not compiled, counted.</param>
/// <param name="Faults">Every fence that is opened and never closed, by page and line.</param>
internal readonly partial record struct WikiPage(
    string Name,
    IReadOnlyList<WikiProgram> Programs,
    IReadOnlyList<WikiBlock> Cells,
    IReadOnlyList<WikiBlock> Needing,
    IReadOnlyList<string> Faults)
{
    /// <summary>The comment above a block that is a notebook's C# cell.</summary>
    public const string Cell = "a C# cell of a notebook";

    /// <summary>Every C# block the page holds, whatever is done with it.</summary>
    public int BlockCount => Programs.Count + Cells.Count + Needing.Count;

    [GeneratedRegex("^(?<kind>example|continues): (?<name>[a-z0-9-]+)$")]
    private static partial Regex ExampleMarker();

    // Wpf, Uno and Maui are heads the ubuntu runner cannot build; Tests is the repository's own test project, whose
    // types (an interaction harness, xunit's Assert) a wiki block may use and this suite cannot reference.
    [GeneratedRegex("^needs: (?:Wpf|Uno|Maui|Tests)$")]
    private static partial Regex NeedsMarker();

    /// <summary>
    /// A page's C# blocks, read line by line. A fence ends at a bare fence at least as long as the one that opened it; a
    /// fence that meets another opening fence first, or the end of the page, is a fault, and the opening fence it met is
    /// read as a block of its own, so one missing line never hides the block after it.
    /// </summary>
    /// <param name="name">The page's name.</param>
    /// <param name="text">Its text, a line feed between lines.</param>
    /// <returns>The page.</returns>
    /// <exception cref="InvalidOperationException">A comment above a block is none the suite knows, or names an example twice or one nobody named.</exception>
    public static WikiPage Read(string name, string text)
    {
        var lines = text.Split('\n');
        var examples = new Dictionary<string, List<WikiBlock>>(StringComparer.Ordinal);
        var programs = new List<WikiProgram>();
        var cells = new List<WikiBlock>();
        var needing = new List<WikiBlock>();
        var faults = new List<string>();

        for (var at = 0; at < lines.Length; at++)
        {
            if (Fence.Of(lines[at]) is not { } open)
            {
                continue;
            }

            var end = at + 1;
            Fence? met = null;

            for (; end < lines.Length; end++)
            {
                if (Fence.Of(lines[end]) is { } next && next.Ticks >= open.Ticks)
                {
                    met = next;
                    break;
                }
            }

            var closed = met is { Info.Length: 0 };

            if (!closed)
            {
                faults.Add(end < lines.Length
                    ? $"{name}, line {at + 1}: the fence opened here is never closed before the fence at line {end + 1}."
                    : $"{name}, line {at + 1}: the fence opened here is never closed before the page ends.");
            }

            if (open.Info == "csharp")
            {
                var block = new WikiBlock(at + 2, string.Concat(lines[(at + 1)..end].Select(line => line + "\n")));
                var marker = at > 0 ? CommentOn(lines[at - 1]) : null;

                if (marker is null)
                {
                    programs.Add(new WikiProgram(block.Line, [block]));
                }
                else if (marker == Cell)
                {
                    cells.Add(block);
                }
                else if (NeedsMarker().IsMatch(marker))
                {
                    needing.Add(block);
                }
                else if (ExampleMarker().Match(marker) is { Success: true } said)
                {
                    programs.Add(new WikiProgram(block.Line, GoOn(name, examples, said.Groups["kind"].Value, said.Groups["name"].Value, block)));
                }
                else
                {
                    throw new InvalidOperationException($"{name}, line {block.Line}: the comment '{marker}' above the block is one this suite does not know.");
                }
            }

            // An opening fence that interrupted this one is read again, as the start of its own block.
            at = closed ? end : end - 1;
        }

        return new WikiPage(name, programs, cells, needing, faults);
    }

    // The blocks an example is compiled from after this block joins it: a new example starts with it, a continuation is
    // compiled after every block that went before under that name.
    private static WikiBlock[] GoOn(string page, Dictionary<string, List<WikiBlock>> examples, string kind, string example, WikiBlock block)
    {
        if (kind == "example")
        {
            if (!examples.TryAdd(example, [block]))
            {
                throw new InvalidOperationException($"{page}, line {block.Line}: the example '{example}' is named twice.");
            }
        }
        else
        {
            var before = examples.GetValueOrDefault(example)
                         ?? throw new InvalidOperationException($"{page}, line {block.Line}: it continues '{example}', and no example above it has that name.");

            before.Add(block);
        }

        return [.. examples[example]];
    }

    // What an HTML comment filling the line says, or null when the line is no such comment.
    private static string? CommentOn(string line)
    {
        var trimmed = line.Trim();

        return trimmed.StartsWith("<!--", StringComparison.Ordinal) && trimmed.EndsWith("-->", StringComparison.Ordinal)
            ? trimmed[4..^3].Trim()
            : null;
    }

    /// <summary>A fence line: how many backticks open it, and the words after them.</summary>
    /// <param name="Ticks">The number of backticks.</param>
    /// <param name="Info">The words after them, such as csharp; empty for a bare fence.</param>
    private readonly record struct Fence(int Ticks, string Info)
    {
        /// <summary>The fence a line is, or null when it is none.</summary>
        public static Fence? Of(string line)
        {
            var trimmed = line.TrimStart();
            var ticks = trimmed.Length - trimmed.TrimStart('`').Length;
            var info = trimmed[ticks..].Trim();

            return ticks >= 3 && !info.Contains('`', StringComparison.Ordinal) ? new Fence(ticks, info) : null;
        }
    }
}
