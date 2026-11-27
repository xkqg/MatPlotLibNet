// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace MatPlotLibNet.Wiki.Tests;

/// <summary>How a document names a member of a type.</summary>
internal enum MemberClaimSpelling
{
    /// <summary>A code span that spells the member after the type, <c>Type.Member</c> or <c>Type.Member()</c>, or a bare member that
    /// follows one after a slash.</summary>
    Dotted,

    /// <summary>A member in the methods column of a table that stands under a type: the type named in the paragraph above the table or in
    /// the nearest heading that names one (the table's own section or one that encloses it), in the table's first header cell, or in a
    /// cell of each row.</summary>
    Table,

    /// <summary>A list of members after a type in a sentence: <c>`Type` has `A`, `B` and `C`</c>, or <c>`Type` has the verbs: `A`, `B`</c>.</summary>
    List,

    /// <summary>A call of the fluent builders written with a leading dot in the methods column of a table, <c>`.WithTitle(text)`</c>: the
    /// document names no type, so one of the builders must have it.</summary>
    Chain,
}

/// <summary>
/// A member a document names under or beside a type, as the document spells it. The document says the type has the member: the
/// claim is true when one of the types the owner names declares it, inherits it, or is reached by an extension of it, in the
/// packages as built. A document that names a member a type no longer has describes a library that is gone.
/// </summary>
/// <param name="Document">The document's name, for the message.</param>
/// <param name="Line">The line the member stands on, from one.</param>
/// <param name="Owners">The names that may be the type, in the order the document names them: a dotted claim has one; a table or a
/// list has every type name of the line or paragraph it stands under, the first that is a type of the packages is the owner.</param>
/// <param name="Path">The member and, after a dotted chain, what is read from it: <c>Style</c>, <c>Use</c> of <c>Plt.Style.Use(...)</c>.</param>
/// <param name="Spelling">How the document spells it.</param>
internal readonly partial record struct MemberClaim(
    string Document,
    int Line,
    IReadOnlyList<string> Owners,
    IReadOnlyList<string> Path,
    MemberClaimSpelling Spelling)
{
    /// <summary>The code span the claim was read from, such as <c>Ema(period)</c> or <c>Plt.Style.Use("x")</c>: the call reader of a
    /// document (<see cref="CallClaim"/>) takes the owner of a call from the claim of its span.</summary>
    public string? Code { get; init; }

    /// <summary>The type a table's Returns column gives for the member in the same row, or null when the table has no such column.</summary>
    public string? Returns { get; init; }

    /// <summary>The words a table's column is headed with when its cells name what the call in the same row returns.</summary>
    private static readonly HashSet<string> ReturnsColumns = new(StringComparer.OrdinalIgnoreCase) { "Returns", "Return", "Return type", "Gives" };

    /// <summary>The words a table's column is headed with when its cells name the members of a type.</summary>
    private static readonly HashSet<string> MemberColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        "Method", "Methods", "Call", "Calls", "Verb", "Verbs", "Member", "Members", "Property", "Properties", "Factory", "Factories",
    };

    /// <summary>The words a table's column is headed with when each row starts with a type and the cells hold that type's own members.</summary>
    private static readonly HashSet<string> RowMemberColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        "Member", "Members", "Method", "Methods", "Property", "Properties", "Holds", "Settings of its own", "Verb", "Verbs", "Call", "Calls", "Values",
    };

    /// <summary>The header of a column that lists what a document says was removed.</summary>
    private const string RemovedColumn = "Removed";

    [GeneratedRegex(@"^[A-Z]\w*")]
    private static partial Regex LeadingName();

    // What may stand between two members of a list: a comma, a slash, 'and' or 'or', and a remark in brackets after the first.
    [GeneratedRegex(@"^\s*(?:\([^()`]*\)\s*)?(?:[,/]\s*)?(?:(?:and|or)\s+)?$")]
    private static partial Regex Joiner();

    // The words that say a type has what a list names: 'has', 'takes', 'names' and the like.
    [GeneratedRegex(@"\b(?:has|have|holds?|takes?|names?|offers?|provides?|exposes?|accepts?|controls?|receives?|gives?|returns?)\b")]
    private static partial Regex Possession();

    // The end of a sentence: a full stop, a question or exclamation mark, or a semicolon, then a space or the end.
    [GeneratedRegex(@"[.!?;](?:\s|$)")]
    private static partial Regex SentenceEnd();

    /// <summary>What is wrong with the claim against the packages as built: the owner is the first name of <see cref="Owners"/> that is a
    /// type of the packages (a claim whose owners are no type of the packages says nothing about them), and each name of the path
    /// is read on the types the one before it gives.</summary>
    /// <param name="surface">The packages.</param>
    /// <returns>A line saying where the document names a member the type does not have, or null when the claim holds or names no type of the packages.</returns>
    public string? Missing(PackageSurface surface)
    {
        // A call written with a dot names no type: one of the builders has it.
        if (Spelling == MemberClaimSpelling.Chain)
        {
            return surface.Reaches(surface.TypesEndingWith("Builder"), Path[0]) ? null : $"{Document}, line {Line}: no builder has the call `.{Path[0]}` ({Spelling})";
        }

        var owner = Owners.FirstOrDefault(surface.IsType);

        // A list names members and types alike (`Rsi`, `WilliamsR`): a name that is a type of the packages is read as the type.
        if (owner is null || (Spelling == MemberClaimSpelling.List && surface.IsType(Path[0])))
        {
            return null;
        }

        var types = surface.TypesNamed(owner);

        // A type of a package this suite cannot build is read from its source, for the member the claim names first.
        if (types.Count == 0)
        {
            return UnbuiltPackages.Sources.Has(owner, Path[0]) ? null : Said(owner, Path[0]);
        }

        foreach (var name in Path)
        {
            if (!surface.Reaches(types, name))
            {
                return Said(owner, name);
            }

            types = surface.Gives(types, name);

            if (types.Count == 0)
            {
                return null;
            }
        }

        return null;
    }

    private string Said(string owner, string name) => $"{Document}, line {Line}: `{owner}` has no member `{name}` ({Spelling})";

    /// <summary>Reads every member the text names under or beside a type, in the three spellings of <see cref="MemberClaimSpelling"/>.</summary>
    /// <param name="document">The document's name.</param>
    /// <param name="text">The document's text, a line feed between lines.</param>
    /// <returns>The claims, in the order they stand.</returns>
    public static IReadOnlyList<MemberClaim> Of(string document, string text)
    {
        var lines = SourceFile.MarkdownLinesOf(text).ToArray();
        var claims = new List<MemberClaim>();

        claims.AddRange(TextsOf(lines).SelectMany(each => DottedIn(document, each.Number, each.Text)));
        claims.AddRange(TablesOf(lines).SelectMany(table => TableIn(document, lines, table.First, table.Last)));
        claims.AddRange(ListsIn(document, lines));

        return [.. claims.OrderBy(claim => claim.Line).ThenBy(claim => claim.Spelling)];
    }

    /// <summary>The texts of a document a reader of code spans reads, in the order they stand: each line of prose and each cell of a table
    /// row, outside the fenced blocks, a cell of a column headed Removed left out (it names what the document says is gone). A line of
    /// prose that opens a code span the lines after it close is one text with them, under the number of its first line.</summary>
    /// <param name="lines">The lines of the document.</param>
    /// <returns>Each text with the number of the line it stands on and, for a cell, the header of its column.</returns>
    public static IEnumerable<DocumentText> TextsOf(IReadOnlyList<MarkdownLine> lines)
    {
        var tables = TablesOf(lines).ToDictionary(table => table.First);

        for (var at = 0; at < lines.Count; at++)
        {
            if (lines[at].InFence)
            {
                continue;
            }

            if (!tables.TryGetValue(at, out var table))
            {
                var wrapped = WrappedAt(lines, at, tables.ContainsKey);

                yield return new DocumentText(lines[at].Number, wrapped == 1 ? lines[at].Text : string.Join(' ', lines.Skip(at).Take(wrapped).Select(line => line.Text.Trim())), null);
                at += wrapped - 1;
                continue;
            }

            var header = SourceFile.TableCellsOf(lines[at].Text);
            var removed = header.ToList().FindIndex(cell => NameOf(cell).Equals(RemovedColumn, StringComparison.OrdinalIgnoreCase));

            for (var row = at; row <= table.Last; row++)
            {
                foreach (var (cell, column) in SourceFile.TableCellsOf(lines[row].Text).Select((cell, column) => (cell, column)).Where(each => each.column != removed))
                {
                    yield return new DocumentText(lines[row].Number, cell, header.ElementAtOrDefault(column));
                }
            }

            at = table.Last;
        }
    }

    // How many lines of prose a code span that opens on a line runs over, a paragraph long at most: one when the line leaves no span
    // open, or when no line after it in its paragraph closes it.
    private static int WrappedAt(IReadOnlyList<MarkdownLine> lines, int at, Func<int, bool> startsTable)
    {
        if (!SourceFile.OpensCodeSpan(lines[at].Text))
        {
            return 1;
        }

        var text = lines[at].Text;

        for (var next = at + 1; next < lines.Count && next - at < MostWrappedLines && !lines[next].InFence && !startsTable(next) && lines[next].Text.Trim().Length > 0; next++)
        {
            text += " " + lines[next].Text.Trim();

            if (!SourceFile.OpensCodeSpan(text))
            {
                return next - at + 1;
            }
        }

        return 1;
    }

    // A span is a call or a name: one that has not closed after this many lines is a stray backtick.
    private const int MostWrappedLines = 4;

    // The tables of a document: the index of each one's first row (its header) and of its last.
    private static IEnumerable<(int First, int Last)> TablesOf(IReadOnlyList<MarkdownLine> lines)
    {
        for (var at = 0; at < lines.Count; at++)
        {
            if (!IsTableRow(lines, at))
            {
                continue;
            }

            var end = at;
            while (IsTableRow(lines, end + 1))
            {
                end++;
            }

            yield return (at, end);
            at = end;
        }
    }

    // The members a line spells after a type, and the bare members a slash joins to one: `SankeyNode.SubLabel` / `SubLabelColor`.
    private static IEnumerable<MemberClaim> DottedIn(string document, int number, string text)
    {
        var spans = SourceFile.CodeSpansOf(text);

        for (var at = 0; at < spans.Count; at++)
        {
            if (DottedPath(spans[at].Code) is not { } dotted)
            {
                continue;
            }

            yield return new MemberClaim(document, number, [dotted.Type], dotted.Path, MemberClaimSpelling.Dotted) { Code = spans[at].Code };

            for (var next = at + 1; next < spans.Count && IsSlash(text, spans[next - 1], spans[next]) && BareName(spans[next].Code) is { } bare; next++)
            {
                yield return new MemberClaim(document, number, [dotted.Type], [bare], MemberClaimSpelling.Dotted) { Code = spans[next].Code };
            }
        }
    }

    private static bool IsSlash(string line, MarkdownCodeSpan before, MarkdownCodeSpan after) =>
        line[(before.Index + before.Length)..after.Index].Trim() == "/";

    // The type and the path of a span that spells `Type.Member(...).Member`; null for any other span.
    private static (string Type, string[] Path)? DottedPath(string code)
    {
        var type = LeadingName().Match(code);

        if (!type.Success)
        {
            return null;
        }

        var path = new List<string>();
        var at = type.Length;

        if (at < code.Length && code[at] == '<')
        {
            at = SkipGroup(code, at, '<', '>');
        }

        while (at + 1 < code.Length && code[at] == '.' && char.IsAsciiLetterUpper(code[at + 1]))
        {
            var name = LeadingName().Match(code[(at + 1)..]);
            path.Add(name.Value);
            at += 1 + name.Length;

            if (at < code.Length && code[at] == '(')
            {
                at = SkipGroup(code, at, '(', ')');
            }
        }

        // A file name (`Type.Part.cs`) or a pattern (`Colors.Tab10*`) names no member.
        var namesAMember = at >= code.Length || !(code[at] == '*' || (code[at] == '.' && at + 1 < code.Length && char.IsAsciiLetterLower(code[at + 1])));

        return path.Count == 0 || !namesAMember ? null : (type.Value, [.. path]);
    }

    // The index after the group that opens at the given index, with the groups inside it; the end of the text when it never closes.
    private static int SkipGroup(string code, int open, char opening, char closing)
    {
        var depth = 0;

        for (var at = open; at < code.Length; at++)
        {
            depth += code[at] == opening ? 1 : code[at] == closing ? -1 : 0;

            if (depth == 0)
            {
                return at + 1;
            }
        }

        return code.Length;
    }

    // The name a span starts with, when the span is a member written bare (`Name` or `Name(args)`), else null.
    private static string? BareName(string code) =>
        LeadingName().Match(code) is { Success: true } name && (name.Length == code.Length || code[name.Length] == '(') ? name.Value : null;

    private static bool IsTableRow(IReadOnlyList<MarkdownLine> lines, int at) =>
        at >= 0 && at < lines.Count && !lines[at].InFence && lines[at].Text.TrimStart().StartsWith('|');

    // The members the cells of a table name, when the table stands under a type.
    private static IEnumerable<MemberClaim> TableIn(string document, MarkdownLine[] lines, int first, int last)
    {
        var header = SourceFile.TableCellsOf(lines[first].Text);
        var rows = Enumerable.Range(first + 2, Math.Max(0, last - first - 1)).Select(at => (Line: lines[at].Number, Cells: SourceFile.TableCellsOf(lines[at].Text))).ToArray();

        if (header.Count == 0 || rows.Length == 0)
        {
            yield break;
        }

        // Three shapes: the first header cell is a type and the first column lists its members; each row names a type in one cell (any
        // column, beside other words or not) and a column headed Members, Properties, Methods, Verbs or Holds lists the members of that
        // row's type; or a column headed Method, Call, Verb, Property and the like lists the members of the type the paragraph above the
        // table or the heading of its section names.
        var ownType = SourceFile.SoleCodeOf(header[0]) is { } soleCode ? BareName(soleCode) : null;
        var typeColumn = ownType is null ? RowTypeColumn(header, rows.Select(row => row.Cells).ToArray()) : -1;
        var typedRows = typeColumn >= 0;

        string[] owners = ownType is not null ? [ownType] : typedRows ? [] : OwnersAbove(lines, first);
        int[] columns = ownType is not null
            ? [0]
            : [.. header.Select((cell, index) => (cell, index)).Where(each => each.index > typeColumn && IsColumnOf(typedRows ? RowMemberColumns : MemberColumns, each.cell)).Select(each => each.index)];

        if (columns.Length == 0)
        {
            yield break;
        }

        var returns = header.ToList().FindIndex(cell => ReturnsColumns.Contains(NameOf(cell)));

        for (var at = 0; at < rows.Length; at++)
        {
            foreach (var column in columns.Where(column => column < rows[at].Cells.Count))
            {
                foreach (var span in SourceFile.CodeSpansOf(rows[at].Cells[column]))
                {
                    // A name in brackets is a value or a remark about the member before it: `Scaling` (`Density`).
                    if (InBrackets(rows[at].Cells[column], span.Index))
                    {
                        continue;
                    }

                    var returned = returns >= 0 && returns < rows[at].Cells.Count ? SourceFile.SoleCodeOf(rows[at].Cells[returns]) : null;

                    if (BareName(span.Code) is { } name && (typedRows || owners.Length > 0))
                    {
                        yield return new MemberClaim(document, rows[at].Line, typedRows ? [NameOfRowType(rows[at].Cells, typeColumn)!] : owners, [name], MemberClaimSpelling.Table) { Code = span.Code, Returns = returned };
                    }
                    else if (span.Code.StartsWith('.') && !typedRows && BareName(span.Code[1..]) is { } call)
                    {
                        yield return new MemberClaim(document, rows[at].Line, [], [call], MemberClaimSpelling.Chain) { Code = span.Code, Returns = returned };
                    }
                }
            }
        }
    }

    // The first column whose cell in every row holds one code span that is a plain name and that a column of members follows: the column
    // that names the type each row is about, the subject the members after it belong to. -1 when no column does.
    private static int RowTypeColumn(IReadOnlyList<string> header, IReadOnlyList<string>[] rows)
    {
        for (var column = 0; column < header.Count - 1; column++)
        {
            if (header.Skip(column + 1).Any(cell => IsColumnOf(RowMemberColumns, cell)) && rows.All(cells => NameOfRowType(cells, column) is not null))
            {
                return column;
            }
        }

        return -1;
    }

    // The type a row's cell names: its one code span, when that is a plain name (no arguments, no member path).
    private static string? NameOfRowType(IReadOnlyList<string> cells, int column) =>
        column < cells.Count && SourceFile.CodeSpansOf(cells[column]) is [{ } only] && only.Code.All(char.IsAsciiLetterOrDigit) && BareName(only.Code) is { } name ? name : null;

    // Whether the text at the index stands inside a bracket that opened before it and has not closed.
    private static bool InBrackets(string text, int index) => text.AsSpan(0, index).Count('(') > text.AsSpan(0, index).Count(')');

    // Whether a header cell heads a column of the kind the words name: the cell says one of them, or its last word does ("Axes method").
    private static bool IsColumnOf(HashSet<string> words, string cell) =>
        words.Contains(NameOf(cell)) || words.Contains(NameOf(cell).Split(' ', StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? string.Empty);

    // What a header cell says: the code of a cell that is one code span, else the cell.
    private static string NameOf(string cell) => SourceFile.SoleCodeOf(cell) ?? cell.Trim();

    // The type names a table stands under, in the order they stand: those of the heading or the paragraph straight above it (the lines up
    // to the first blank line, heading or fence above it, after the blank lines between it and the table), then those of the nearest
    // heading of the table's section and the headings of the sections that enclose it, the nearest first: the first name that is a type of
    // the packages is the owner, so a paragraph or a section that names none of them leaves the table to the section that does.
    private static string[] OwnersAbove(MarkdownLine[] lines, int table)
    {
        var at = table - 1;

        while (at >= 0 && !lines[at].InFence && lines[at].Text.Trim().Length == 0)
        {
            at--;
        }

        var found = new List<string>();

        for (; at >= 0 && !lines[at].InFence && lines[at].Text.Trim().Length > 0 && !IsTableRow(lines, at); at--)
        {
            found.InsertRange(0, lines[at].Heading is null ? TypeNamesIn(lines[at].Text) : TypeNamesInHeading(lines[at].Text));

            if (lines[at].Heading is not null)
            {
                break;
            }
        }

        // The section of the table, then the section that encloses it, and so on up to the title: a heading of a lower level than the last
        // one read is the one that encloses it.
        var level = int.MaxValue;
        var enclosed = new List<string>();

        for (var heading = table - 1; heading >= 0; heading--)
        {
            if (lines[heading].HeadingLevel is > 0 and var enclosing && enclosing < level)
            {
                // The heading of the table's own section may name its type in words; the sections that enclose it only in code.
                enclosed.AddRange(level == int.MaxValue ? TypeNamesInHeading(lines[heading].Text) : TypeNamesIn(lines[heading].Text));
                level = enclosing;
            }
        }

        return [.. found.Concat(enclosed).Distinct()];
    }

    private static IEnumerable<string> TypeNamesIn(string text) => SourceFile.CodeSpansOf(text).Select(span => TypeName(span.Code)).OfType<string>();

    // The names the heading of a table's own section may mean as a type: because it says "Fluent API reference - BarSeries" or
    // "ReferenceLine properties" with no code span around the name, every capitalised word of it, in the order they stand.
    private static IEnumerable<string> TypeNamesInHeading(string text) =>
        CapitalisedWord().Matches(text).Select(word => word.Value);

    [GeneratedRegex(@"\b[A-Z][A-Za-z0-9]*\b")]
    private static partial Regex CapitalisedWord();

    // The type a span names: its leading name, whatever follows it (a member path, an argument list), and null when it names none.
    private static string? TypeName(string code) => LeadingName().Match(code) is { Success: true } name ? name.Value : null;

    // The members listed after a type in a sentence: `Type` has `A`, `B` and `C`, or `Type` takes the verbs: `A`, `B`. A paragraph of prose
    // is one text, a list item or a cell of a table another.
    private static IEnumerable<MemberClaim> ListsIn(string document, MarkdownLine[] lines)
    {
        for (var start = 0; start < lines.Length; start++)
        {
            if (IsTableRow(lines, start) && !lines[start].InFence)
            {
                var cells = SourceFile.TableCellsOf(lines[start].Text);

                foreach (var cell in cells)
                {
                    foreach (var claim in RunsIn(document, cell, _ => lines[start].Number))
                    {
                        yield return claim;
                    }
                }

                continue;
            }

            if (!IsProse(lines, start) || Continues(lines, start))
            {
                continue;
            }

            var end = start;
            while (IsProse(lines, end + 1) && Continues(lines, end + 1))
            {
                end++;
            }

            var starts = new List<int>();
            var paragraph = new System.Text.StringBuilder();

            for (var at = start; at <= end; at++)
            {
                starts.Add(paragraph.Length);
                paragraph.Append(lines[at].Text).Append('\n');
            }

            foreach (var claim in RunsIn(document, paragraph.ToString(), offset => lines[start + starts.FindLastIndex(each => each <= offset)].Number))
            {
                yield return claim;
            }
        }
    }

    // The runs of a text: a name in a code span, up to sixty characters of words with no end of a sentence, and then two or more names in
    // code spans joined by commas, slashes, 'and' or 'or'. Each name of the run is a member the first one is said to have. A name that
    // is itself a member of a run before it starts no run.
    private static IEnumerable<MemberClaim> RunsIn(string document, string text, Func<int, int> lineOf)
    {
        var spans = SourceFile.CodeSpansOf(text);
        var names = spans.Select(span => BareName(span.Code)).ToArray();

        var listed = new HashSet<int>();

        for (var head = 0; head + 2 < spans.Count; head++)
        {
            var apart = text[(spans[head].Index + spans[head].Length)..spans[head + 1].Index];

            if (names[head] is not { } owner
                || listed.Contains(head)
                || names[head + 1] is null
                || apart.Length > 60
                || SentenceEnd().IsMatch(apart)
                || !(Possession().IsMatch(apart) || apart.TrimEnd().EndsWith(':') || apart.TrimStart().StartsWith('(')))
            {
                continue;
            }

            var run = new List<int> { head + 1 };

            while (run[^1] + 1 < spans.Count
                   && names[run[^1] + 1] is not null
                   && Joiner().IsMatch(text[(spans[run[^1]].Index + spans[run[^1]].Length)..spans[run[^1] + 1].Index]))
            {
                run.Add(run[^1] + 1);
            }

            if (run.Count < 2)
            {
                continue;
            }

            foreach (var member in run)
            {
                listed.Add(member);

                yield return new MemberClaim(document, lineOf(spans[member].Index), [owner], [names[member]!], MemberClaimSpelling.List) { Code = spans[member].Code };
            }
        }
    }

    // A line of prose: no fenced line, no blank line, no heading, no table row.
    private static bool IsProse(MarkdownLine[] lines, int at) =>
        at >= 0 && at < lines.Length && !lines[at].InFence && lines[at].Text.Trim().Length > 0 && lines[at].Heading is null && !IsTableRow(lines, at);

    // Whether a line of prose carries on the paragraph of the line above it: a list item starts a sentence stream of its own.
    private static bool Continues(MarkdownLine[] lines, int at) => IsProse(lines, at - 1) && !ListItem().IsMatch(lines[at].Text);

    [GeneratedRegex(@"^\s*(?:[-*+]|\d+[.)])\s")]
    private static partial Regex ListItem();
}

/// <summary>A text of a document a reader of code spans reads: a line of prose, or a cell of a table with the header of its column.</summary>
/// <param name="Number">The number of the line it stands on, from one.</param>
/// <param name="Text">The line or the cell.</param>
/// <param name="Column">The header of the cell's column; null for a line of prose.</param>
internal readonly record struct DocumentText(int Number, string Text, string? Column);
