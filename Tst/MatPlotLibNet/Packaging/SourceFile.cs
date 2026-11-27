// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace MatPlotLibNet.Tests.Packaging;

/// <summary>One source file: its path from the repository root, written with forward slashes, and its text. The file is the one owner of
/// how that text reads as lines: it splits the text once, and says once which of its lines are comments, so the tests that scan a file
/// ask it and none of them splits the text or tests a line for a comment mark again.
/// <para>A line is a <em>comment line</em> when its first characters after the indentation are the comment mark of the file: <c>//</c>
/// for C# and every other file, <c>#</c> for a script or a workflow (a <c>.ps1</c>, <c>.sh</c>, <c>.py</c>, <c>.yml</c> or <c>.yaml</c>);
/// every XML-documentation line (<c>///</c>) of a C# file is one too. The <em>code</em> of a file is its lines with the comment lines
/// emptied, so a line keeps its number.</para></summary>
internal sealed class SourceFile
{
    /// <summary>The comment of a line, from its mark to the end: a <c>//</c> that does not follow a colon, a quote or a letter (so not
    /// the one of an address in a string).</summary>
    private static readonly Regex CommentMark = new(@"(?<![:""\w])//.*", RegexOptions.CultureInvariant);

    private static readonly string[] HashCommentExtensions = [".ps1", ".sh", ".py", ".yml", ".yaml"];

    private readonly Lazy<Reading> _reading;
    private readonly string _mark;

    /// <summary>Makes the file at <paramref name="path"/> that holds <paramref name="text"/>.</summary>
    /// <param name="path">The path from the repository root, with forward slashes.</param>
    /// <param name="text">The text of the file.</param>
    public SourceFile(string path, string text)
    {
        Path = path;
        Text = text;
        _mark = HashCommentExtensions.Contains(System.IO.Path.GetExtension(path), StringComparer.OrdinalIgnoreCase) ? "#" : "//";
        _reading = new Lazy<Reading>(() => Reading.Of(this), LazyThreadSafetyMode.ExecutionAndPublication);
    }

    /// <summary>The path from the repository root, written with forward slashes.</summary>
    public string Path { get; }

    /// <summary>The text of the file, as it was read.</summary>
    public string Text { get; }

    /// <summary>Hands out the path and the text, so <c>foreach (var (path, text) in files)</c> reads a file as the pair it is.</summary>
    /// <param name="path">The path from the repository root.</param>
    /// <param name="text">The text of the file.</param>
    public void Deconstruct(out string path, out string text) => (path, text) = (Path, Text);

    /// <summary>Reads the file at <paramref name="path"/>, a file of the wiki, of a package or of a folder a test made, which the walk of
    /// the repository does not list: its path is the one given.</summary>
    /// <param name="path">The path of the file, as the caller names it.</param>
    /// <returns>The file, read whole.</returns>
    public static SourceFile ReadFrom(string path) => new(path, File.ReadAllText(path));

    /// <summary>Cuts a text that is no file of the repository (a section of a document, the text of a syntax node, a markdown table, the
    /// schema an agent is shown, an exception message) at its line ends, the way a file is cut: at every line feed, with a carriage return
    /// before the cut left out.</summary>
    /// <param name="text">Any text.</param>
    /// <returns>Its lines, each without its line end; a text that ends with a line end ends with an empty line.</returns>
    public static string[] LinesOf(string text) => Cut(text, out _);

    /// <summary>The lines of a text the library wrote one element to a line (an svg), each trimmed, a blank one left out.</summary>
    public static string[] ElementLinesOf(string text) => [.. Cut(text, out _).Select(line => line.Trim()).Where(line => line.Length > 0)];

    /// <summary>The lines of a csv, cut at its own line end (a carriage return and a line feed: its contract), so a line feed inside a quoted
    /// field stays in the field; a csv that ends with its line end ends with an empty line.</summary>
    public static string[] CrlfLinesOf(string csv) => csv.Split("\r\n");

    /// <summary>The records of a csv: <see cref="CrlfLinesOf"/> without the empty line after the last line end.</summary>
    public static string[] CsvRecordsOf(string csv) => [.. CrlfLinesOf(csv).Where(record => record.Length > 0)];

    /// <summary>The text with every line end written as a line feed.</summary>
    public static string LineFeedTextOf(string text) => string.Join('\n', Cut(text, out _));

    /// <summary>The section of a markdown document that opens with the line <c>## heading</c>: that line and every line after it up to the
    /// next <c>## </c> heading (a <c>###</c> heading stays inside), or to the end; the lines are joined with a line feed. The test fails
    /// when the document has no such section.</summary>
    /// <param name="markdown">The text of a markdown document.</param>
    /// <param name="heading">What the heading says after its <c>## </c>.</param>
    public static string SectionOf(string markdown, string heading)
    {
        var section = SectionsOf(markdown).Cast<MarkdownSection?>().FirstOrDefault(candidate => candidate!.Value.Heading == heading);
        Xunit.Assert.True(section is not null, $"The document has no section \"## {heading}\".");
        return section!.Value.Text;
    }

    /// <summary>The sections of a markdown document in the order they stand: each opens with its <c>## </c> line and runs to the line before
    /// the next one. What stands before the first heading belongs to none (<see cref="IntroductionOf"/>).</summary>
    /// <param name="markdown">The text of a markdown document.</param>
    public static IReadOnlyList<MarkdownSection> SectionsOf(string markdown) =>
        [.. BlocksOf(Cut(markdown, out _), line => line.StartsWith("## ", StringComparison.Ordinal))
            .Select(lines => new MarkdownSection(lines[0][3..], string.Join('\n', lines)))];

    /// <summary>What every heading of a markdown document says, of any level, in the order the headings stand; a line inside a fenced code
    /// block is no heading.</summary>
    /// <param name="markdown">The text of a markdown document.</param>
    public static IReadOnlyList<string> HeadingsOf(string markdown) =>
        [.. MarkdownLinesOf(markdown).Select(line => line.Heading).OfType<string>()];

    /// <summary>The fenced code blocks of a markdown document in the order they stand, read once for every test that needs to know where a
    /// fence is: a fence opens at a line of three or more backticks and ends at the next fence of at least as many, so a longer fence holds
    /// the shorter ones as text; a block ends unclosed when that next fence has words after its backticks (it opens the block after) or when
    /// the document ends.</summary>
    /// <param name="markdown">The text of a markdown document.</param>
    public static IReadOnlyList<MarkdownFenceBlock> FenceBlocksOf(string markdown)
    {
        var lines = Cut(markdown, out _);
        var blocks = new List<MarkdownFenceBlock>();

        for (var at = 0; at < lines.Length; at++)
        {
            if (MarkdownFence.Of(lines[at]) is not { } open)
            {
                continue;
            }

            var end = at + 1;
            MarkdownFence? met = null;

            for (; end < lines.Length; end++)
            {
                if (MarkdownFence.Of(lines[end]) is { } next && next.Ticks >= open.Ticks)
                {
                    met = next;
                    break;
                }
            }

            blocks.Add(new MarkdownFenceBlock(open, at, end, met, lines[(at + 1)..end]));

            // A fence with words that interrupted this block opens the next one.
            at = met is { Info.Length: 0 } ? end : end - 1;
        }

        return blocks;
    }

    /// <summary>Every line of a markdown document that is no fence line, in the order they stand, each with its number and the fenced block
    /// that holds it: what a reader of prose (the headings, the links) takes from outside the blocks and a reader of a tree or of code takes
    /// from inside one.</summary>
    /// <param name="markdown">The text of a markdown document.</param>
    public static IReadOnlyList<MarkdownLine> MarkdownLinesOf(string markdown)
    {
        var lines = Cut(markdown, out _);
        var blocks = FenceBlocksOf(markdown);
        var found = new List<MarkdownLine>(lines.Length);

        for (int at = 0, next = 0; at < lines.Length; at++)
        {
            while (next < blocks.Count && blocks[next].EndAt < at)
            {
                next++;
            }

            var block = next < blocks.Count && blocks[next].OpenAt <= at ? next : -1;
            var isFence = block >= 0 && (at == blocks[block].OpenAt || (at == blocks[block].EndAt && blocks[block].Met is not null));

            if (!isFence)
            {
                found.Add(new MarkdownLine(at + 1, lines[at], block));
            }
        }

        return found;
    }

    internal static readonly Regex HeadingLine = new(@"^(?<hashes>#{1,6})\s+(?<heading>.*)$");

    private static readonly Regex CodeSpan = new("`([^`\\n]+)`");

    /// <summary>The inline code spans of a text, in the order they stand: each is the text between two backticks on one line, with the
    /// place of its opening backtick. A backtick that never closes opens none.</summary>
    /// <param name="text">A line of markdown, or a cell of a table.</param>
    /// <returns>The spans; none when the text holds no code.</returns>
    public static IReadOnlyList<MarkdownCodeSpan> CodeSpansOf(string text) =>
        [.. CodeSpan.Matches(text).Select(match => new MarkdownCodeSpan(match.Groups[1].Value, match.Index, match.Length))];

    /// <summary>Whether a text opens a code span it does not close: a backtick is left once every span of the line is taken out, as on a
    /// line of prose whose span goes on on the next line.</summary>
    /// <param name="text">A line of markdown.</param>
    /// <returns>True when a span is left open.</returns>
    public static bool OpensCodeSpan(string text) => WithoutCodeSpans(text).Contains('`', StringComparison.Ordinal);

    /// <summary>The text with every code span, its backticks included, taken out: what a reader sees as prose.</summary>
    /// <param name="text">A line of markdown.</param>
    /// <returns>The text without its code.</returns>
    public static string WithoutCodeSpans(string text) => CodeSpan.Replace(text, string.Empty);

    /// <summary>The code of a text that is exactly one code span, blanks around it left out, such as a table cell that names one member.</summary>
    /// <param name="text">A cell of a table or a line of markdown.</param>
    /// <returns>The code, or null when the text is anything else.</returns>
    public static string? SoleCodeOf(string text) =>
        CodeSpansOf(text.Trim()) is [{ Index: 0 } only] && only.Length == text.Trim().Length ? only.Code : null;

    /// <summary>The cells of one row of a markdown table, each trimmed: the text between the row's bars, where a bar inside a code span or
    /// after a backslash is no bar. The bar at each end of the row is no cell's, and text after a last bar is none.</summary>
    /// <param name="row">One line of a table, such as <c>| `Name` | what it does |</c>.</param>
    /// <returns>The cells in the order they stand; none for a line that does not start with a bar or has fewer than two.</returns>
    public static IReadOnlyList<string> TableCellsOf(string row)
    {
        var cells = new List<string>();
        var current = new System.Text.StringBuilder();
        var inCode = false;
        var trimmed = row.Trim();

        if (!trimmed.StartsWith('|'))
        {
            return [];
        }

        for (var at = 0; at < trimmed.Length; at++)
        {
            inCode ^= trimmed[at] == '`';

            if (trimmed[at] == '|' && !inCode && (at == 0 || trimmed[at - 1] != '\\'))
            {
                cells.Add(current.ToString().Trim());
                current.Clear();
            }
            else
            {
                current.Append(trimmed[at]);
            }
        }

        // The text before the first bar and after the last is no cell.
        return cells.Count >= 2 ? [.. cells.Skip(1)] : [];
    }

    /// <summary>The part of a section of a markdown document that stands under the line <c>### heading</c>: that line and every line after
    /// it up to the next <c>### </c> heading, or to the end; the lines are joined with a line feed. The test fails when the section has no
    /// such heading.</summary>
    /// <param name="section">The text of a section (<see cref="SectionOf"/>).</param>
    /// <param name="heading">What the heading says after its <c>### </c>.</param>
    public static string SubsectionOf(string section, string heading)
    {
        var block = BlocksOf(Cut(section, out _), line => line.StartsWith("### ", StringComparison.Ordinal)).FirstOrDefault(lines => lines[0] == "### " + heading);
        Xunit.Assert.True(block is not null, $"The section has no subsection \"### {heading}\".");
        return string.Join('\n', block!);
    }

    /// <summary>The lines between the two <c>---</c> lines that open a markdown document (its YAML header); none when it has none.</summary>
    public static IReadOnlyList<string> FrontMatterOf(string markdown) =>
        Cut(markdown, out _) is ["---", .. var rest] && Array.IndexOf(rest, "---") is var end and >= 0 ? rest[..end] : [];

    /// <summary>The closed fenced blocks of <paramref name="language"/> in a markdown document, each as its lines joined with a line feed.</summary>
    public static IReadOnlyList<string> FencedBlocksOf(string markdown, string language) =>
        [.. FenceBlocksOf(markdown).Where(block => block.Closed && block.Open.Info == language).Select(block => string.Join('\n', block.Body))];

    /// <summary>The bullets of a markdown list in the order they stand: each is its <c>- </c> line and the indented lines that continue it,
    /// the continuation joined with a space; a blank line, a heading or the next bullet ends it, and what stands before the first bullet
    /// belongs to none.</summary>
    /// <param name="markdown">The text of a section or of a document.</param>
    public static IReadOnlyList<string> BulletsOf(string markdown) =>
        [.. BlocksOf(Cut(markdown, out _), line => line.StartsWith("- ", StringComparison.Ordinal))
            .Select(block => string.Join(' ', block.TakeWhile((line, index) => index == 0 || line.StartsWith("  ", StringComparison.Ordinal)).Select(line => line.Trim())))];

    /// <summary>The blocks one level below the line <c>key:</c> of a YAML text (the jobs under <c>jobs:</c>, the steps under
    /// <c>steps:</c>), each from its own line to the line before the next, joined with a line feed; a blank or comment line stays with the
    /// block before it. The list ends at the first line no deeper than the key. The test fails when the text has no such key.</summary>
    public static IReadOnlyList<string> YamlChildrenOf(string yaml, string key)
    {
        static bool Significant(string line) => line.Trim().Length > 0 && !line.TrimStart().StartsWith('#');
        static int Indent(string line) => line.Length - line.TrimStart().Length;
        string[] lines = Cut(yaml, out _);
        int at = Array.FindIndex(lines, line => line.Trim() == key + ":");
        Xunit.Assert.True(at >= 0, $"The yaml has no \"{key}:\".");
        var below = lines[(at + 1)..].TakeWhile(line => !Significant(line) || Indent(line) > Indent(lines[at])).ToArray();
        int child = below.Where(Significant).Select(Indent).DefaultIfEmpty(-1).First();
        return [.. BlocksOf(below, line => Significant(line) && Indent(line) == child).Select(block => string.Join('\n', block))];
    }

    /// <summary>Groups lines into blocks: a block opens at a line <paramref name="opens"/> accepts and runs to the line before the next
    /// one; the lines before the first opener belong to none.</summary>
    private static List<List<string>> BlocksOf(IEnumerable<string> lines, Func<string, bool> opens)
    {
        var blocks = new List<List<string>>();
        foreach (string line in lines)
        {
            if (opens(line)) { blocks.Add([]); }
            if (blocks.Count > 0) { blocks[^1].Add(line); }
        }

        return blocks;
    }

    /// <summary>What stands before the first <c>## </c> heading of a markdown document (the whole text when it has none), its lines joined
    /// with a line feed.</summary>
    /// <param name="markdown">The text of a markdown document.</param>
    public static string IntroductionOf(string markdown) =>
        string.Join('\n', Cut(markdown, out _).TakeWhile(line => !line.StartsWith("## ", StringComparison.Ordinal)));

    /// <summary>The line of <paramref name="text"/> that holds <paramref name="offset"/>, without its line end: what a scan that found a
    /// match at an offset quotes of it.</summary>
    public static string LineAt(string text, int offset) => text[(text.LastIndexOf('\n', offset) + 1)..LineEndAt(text, offset)].TrimEnd('\r');

    /// <summary>The offset at which the line that holds <paramref name="offset"/> ends: its line feed, or the length of the text for the
    /// last line.</summary>
    public static int LineEndAt(string text, int offset) => text.IndexOf('\n', offset) is var end and >= 0 ? end : text.Length;

    /// <summary>The zero-based number of the line that holds <paramref name="offset"/>: the line feeds before it.</summary>
    public static int LineIndexAt(string text, int offset) => text.AsSpan(0, offset).Count('\n');

    /// <summary>The lines of the text, each without its line end: the text cut at every line feed, and a carriage return before the cut
    /// left out. A text that ends with a line end ends with an empty line.</summary>
    public IReadOnlyList<string> Lines => _reading.Value.Lines;

    /// <summary>The text with every line end written as a line feed: the lines joined with a line feed, so a pattern that runs over a
    /// line break reads a file whatever line end its checkout chose.</summary>
    public string LineFeedText => _reading.Value.LineFeedText;

    /// <summary>The lines of the file with every comment line emptied: line <c>i</c> of the code is line <c>i</c> of <see cref="Lines"/>,
    /// or an empty line where that one is a comment.</summary>
    public IReadOnlyList<string> CodeLines => _reading.Value.CodeLines;

    /// <summary>The code of the file as one text, its lines joined with a line feed: a pattern that runs over a line break reads it, and
    /// the line a match starts on is the number of line feeds before it.</summary>
    public string Code => _reading.Value.Code;

    /// <summary>The offset in <see cref="Text"/> at which line <paramref name="index"/> starts.</summary>
    /// <param name="index">The zero-based number of the line.</param>
    /// <returns>The offset of the line's first character.</returns>
    public int StartOf(int index) => _reading.Value.Starts[index];

    /// <summary>Whether line <paramref name="index"/> is a comment line (see <see cref="IsComment(string)"/>).</summary>
    /// <param name="index">The zero-based number of the line.</param>
    /// <returns><see langword="true"/> for a comment line or an XML-documentation line.</returns>
    public bool IsComment(int index) => _reading.Value.Kinds[index] != LineKind.Code;

    /// <summary>Whether line <paramref name="index"/> is an XML-documentation line (see <see cref="IsDocumentation(string)"/>).</summary>
    /// <param name="index">The zero-based number of the line.</param>
    /// <returns><see langword="true"/> for a line that starts with <c>///</c>.</returns>
    public bool IsDocumentation(int index) => _reading.Value.Kinds[index] == LineKind.Documentation;

    /// <summary>Whether <paramref name="line"/> is a comment line of C#: after its indentation it starts with <c>//</c>, an
    /// XML-documentation line included.</summary>
    /// <param name="line">A line of source.</param>
    /// <returns><see langword="true"/> for a comment line.</returns>
    public static bool IsComment(string line) => KindOf(line, "//") != LineKind.Code;

    /// <summary>Whether <paramref name="line"/> is an XML-documentation line: after its indentation it starts with <c>///</c>.</summary>
    /// <param name="line">A line of source.</param>
    /// <returns><see langword="true"/> for a documentation line.</returns>
    public static bool IsDocumentation(string line) => KindOf(line, "//") == LineKind.Documentation;

    /// <summary>The comment of <paramref name="line"/>, from its <c>//</c> to the end of the line: the whole line when it is a comment
    /// line, and the tail after the code when a comment follows code. A <c>//</c> that follows a colon, a quote or a letter (an address in
    /// a string) is no comment mark.</summary>
    /// <param name="line">A line of source.</param>
    /// <returns>The comment, or <see langword="null"/> when the line has none.</returns>
    public static string? CommentOf(string line) => CommentMark.Match(line) is { Success: true } mark ? mark.Value : null;

    private static LineKind KindOf(string line, string mark)
    {
        var rest = line.AsSpan().TrimStart();
        return !rest.StartsWith(mark, StringComparison.Ordinal) ? LineKind.Code
            : mark == "//" && rest.StartsWith("///", StringComparison.Ordinal) ? LineKind.Documentation
            : LineKind.Comment;
    }

    /// <summary>Cuts <paramref name="text"/> at every line feed and leaves a carriage return before the cut out.</summary>
    private static string[] Cut(string text, out int[] starts)
    {
        string[] raw = text.Split('\n');
        starts = new int[raw.Length];
        int start = 0;
        for (int i = 0; i < raw.Length; i++)
        {
            starts[i] = start;
            start += raw[i].Length + 1;
            raw[i] = raw[i].TrimEnd('\r');
        }

        return raw;
    }

    private enum LineKind : byte { Code, Comment, Documentation }

    /// <summary>What reading the text once gives, shared by every scan that asks for it.</summary>
    private sealed record Reading(string[] Lines, string[] CodeLines, string Code, string LineFeedText, int[] Starts, LineKind[] Kinds)
    {
        public static Reading Of(SourceFile file)
        {
            string[] lines = Cut(file.Text, out int[] starts);
            var code = new string[lines.Length];
            var kinds = new LineKind[lines.Length];
            for (int i = 0; i < lines.Length; i++)
            {
                kinds[i] = KindOf(lines[i], file._mark);
                code[i] = kinds[i] == LineKind.Code ? lines[i] : string.Empty;
            }

            return new Reading(lines, code, string.Join('\n', code), string.Join('\n', lines), starts, kinds);
        }
    }
}

/// <summary>One inline code span of a line of markdown.</summary>
/// <param name="Code">The text between the backticks.</param>
/// <param name="Index">Where the opening backtick stands in the text.</param>
/// <param name="Length">The length of the span, both backticks included.</param>
internal readonly record struct MarkdownCodeSpan(string Code, int Index, int Length);

/// <summary>One section of a markdown document: the heading it opens with and the text from that heading line to the line before the next.</summary>
/// <param name="Heading">What the heading says, after its <c>## </c>.</param>
/// <param name="Text">The section, its heading line first, its lines joined with a line feed.</param>
internal readonly record struct MarkdownSection(string Heading, string Text);

/// <summary>A fence line of a markdown document: how many backticks open it, and the words after them.</summary>
/// <param name="Ticks">The number of backticks.</param>
/// <param name="Info">The words after them, such as csharp; empty for a bare fence.</param>
internal readonly record struct MarkdownFence(int Ticks, string Info)
{
    /// <summary>The fence a line is, or null when it is none (fewer than three backticks, or backticks among the words, as in a sentence).</summary>
    /// <param name="line">One line of a document.</param>
    public static MarkdownFence? Of(string line)
    {
        var trimmed = line.TrimStart();
        var ticks = trimmed.Length - trimmed.TrimStart('`').Length;
        var info = trimmed[ticks..].Trim();

        return ticks >= 3 && !info.Contains('`', StringComparison.Ordinal) ? new MarkdownFence(ticks, info) : null;
    }
}

/// <summary>One fenced block of a markdown document.</summary>
/// <param name="Open">The fence that opens it.</param>
/// <param name="OpenAt">The index of its opening line, from zero.</param>
/// <param name="EndAt">The index of the fence that ended it, or the number of lines when the document ended first.</param>
/// <param name="Met">The fence at <paramref name="EndAt"/>, or null when the document ended.</param>
/// <param name="Body">The lines between the two fences.</param>
internal readonly record struct MarkdownFenceBlock(MarkdownFence Open, int OpenAt, int EndAt, MarkdownFence? Met, IReadOnlyList<string> Body)
{
    /// <summary>Whether a bare fence ended the block.</summary>
    public bool Closed => Met is { Info.Length: 0 };
}

/// <summary>One line of a markdown document that is no fence line.</summary>
/// <param name="Number">The line's number, from one.</param>
/// <param name="Text">The line, without its line end.</param>
/// <param name="Block">The index of the fenced block that holds it, or -1 outside every block.</param>
internal readonly record struct MarkdownLine(int Number, string Text, int Block)
{
    /// <summary>Whether a fenced block holds the line.</summary>
    public bool InFence => Block >= 0;

    /// <summary>What the line says after its hashes when it is a heading of any level outside a fenced block; null for any other line,
    /// a <c>#hashtag</c> with no space after its hash included.</summary>
    public string? Heading => InFence || SourceFile.HeadingLine.Match(Text) is not { Success: true } heading ? null : heading.Groups["heading"].Value.Trim();

    /// <summary>How many hashes the line opens with when it is a heading (one for the title, two for a section); zero for any other line.</summary>
    public int HeadingLevel => InFence || SourceFile.HeadingLine.Match(Text) is not { Success: true } heading ? 0 : heading.Groups["hashes"].Length;
}
