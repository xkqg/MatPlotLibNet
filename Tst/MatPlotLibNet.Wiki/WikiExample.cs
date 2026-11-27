// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.RegularExpressions;

namespace MatPlotLibNet.Wiki.Tests;

/// <summary>
/// An example of a wiki page: a C# block and what a reader sees beside it, the pictures that stand straight under it, before any line of prose or heading.
/// </summary>
/// <param name="Line">The line its code starts on, from one.</param>
/// <param name="Pictures">The files of the pictures under it, in the order they stand.</param>
/// <param name="Figures">The SVG files its code saves, each named once.</param>
/// <param name="Entries">How many figures its code starts of its own: the calls of <see cref="Plt"/> that begin a figure and the <c>new Figure</c> expressions.
/// A block that goes on with the figure of the block above starts none.</param>
/// <param name="Refuses">Whether its code catches an exception: an example that shows how a call refuses draws nothing.</param>
/// <param name="After">The first line of prose after it, before the next block or heading, a picture or the output it shows leaving out; empty when none follows.</param>
internal readonly record struct WikiExample(int Line, IReadOnlyList<string> Pictures, IReadOnlyList<string> Figures, int Entries, bool Refuses, string After)
{
    /// <summary>Whether the code draws a figure of its own, and so owes a reader the picture of it: it starts one and does not catch a
    /// refusal. A handler, a block that goes on with the figure above, and a call that is shown to refuse draw none.</summary>
    public bool Draws => Entries > 0 && !Refuses;
}

/// <summary>What a page's text holds of examples: each example, and the pictures that stand under no block of C# in their section.</summary>
/// <param name="Examples">The examples, in the order they stand.</param>
/// <param name="Loose">Each picture with no block above it in its section, as "line N: images/file".</param>
internal readonly partial record struct WikiExampleReading(IReadOnlyList<WikiExample> Examples, IReadOnlyList<string> Loose)
{
    [GeneratedRegex(@"\.Save\w*\(\s*""(?<file>[^""]+\.svg)""")]
    private static partial Regex SavedFigure();

    [GeneratedRegex(@"\bcatch\b")]
    private static partial Regex Catch();

    /// <summary>The static calls of <see cref="Plt"/> that begin something: every method the class declares, read from the class (its style
    /// is a nested type, not a method).</summary>
    internal static IReadOnlyList<string> PltEntries { get; } =
        [.. typeof(Plt).GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly).Select(method => method.Name).Distinct()];

    /// <summary>What starts a figure in code: a call of <see cref="PltEntries"/> (<c>Plt.Create(</c>, <c>Plt.Mosaic(</c> and the rest) and
    /// <c>new Figure(</c>.</summary>
    internal static Regex FigureEntry { get; } = new(
        @"\bPlt\.(?:" + string.Join('|', PltEntries) + @")\(|\bnew Figure\(",
        RegexOptions.CultureInvariant);

    /// <summary>Reads the examples of a page: the blocks and the lines outside blocks come from <see cref="SourceFile"/>, so a fence or a
    /// heading in code is never read as one.</summary>
    /// <param name="text">The page's text, a line feed between lines.</param>
    /// <returns>Its examples and its loose pictures.</returns>
    public static WikiExampleReading Of(string text)
    {
        var blocks = SourceFile.FenceBlocksOf(text);
        var starts = new List<(int Line, string[] Figures, int Entries, bool Refuses)>();
        var pictures = new List<List<string>>();
        var after = new List<string>();
        var loose = new List<string>();
        var seen = new HashSet<int>();
        var current = -1;
        var waiting = -1;

        foreach (var line in SourceFile.MarkdownLinesOf(text))
        {
            if (line.InFence)
            {
                if (seen.Add(line.Block) && blocks[line.Block].Open.Info == "csharp")
                {
                    var body = blocks[line.Block].Body;

                    starts.Add((
                        blocks[line.Block].OpenAt + 2,
                        [.. body.SelectMany(code => SavedFigure().Matches(code).Select(saved => saved.Groups["file"].Value)).Distinct()],
                        body.Sum(code => FigureEntry.Matches(code).Count),
                        body.Any(code => Catch().IsMatch(code))));
                    pictures.Add([]);
                    after.Add(string.Empty);
                    current = starts.Count - 1;
                    waiting = current;
                }
            }
            else
            {
                var shown = Wiki.PictureLink().Matches(line.Text).Select(link => link.Groups["name"].Value).ToArray();

                // Prose, a heading or a list between a block and a picture makes the picture another paragraph's: it is no longer the block's.
                if (shown.Length == 0 && line.Text.Trim().Length > 0)
                {
                    current = -1;
                }

                // The first line of prose, or a heading, after a block ends what the block waits for: it is the sentence that says what it shows.
                if (shown.Length == 0 && line.Text.Trim().Length > 0 && waiting >= 0)
                {
                    after[waiting] = line.Heading is null ? line.Text : string.Empty;
                    waiting = -1;
                }

                foreach (var picture in shown)
                {
                    if (current >= 0)
                    {
                        pictures[current].Add(picture);
                    }
                    else
                    {
                        loose.Add($"line {line.Number}: images/{picture}");
                    }
                }
            }
        }

        return new WikiExampleReading([.. starts.Select((start, index) => new WikiExample(start.Line, pictures[index], start.Figures, start.Entries, start.Refuses, after[index]))], loose);
    }
}
