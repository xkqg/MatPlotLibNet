// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace MatPlotLibNet.Tests.Packaging;

/// <summary>No block of code is written twice. The scan reads the code of every file under <c>Src</c> and <c>Tst</c>, and it is the one
/// reader of what a line of code is (the detector of the plan reads lines by the same rule): each line trimmed, the lines that carry no code
/// (<see cref="NoCode"/>: a line of brackets, semicolons and commas only, whichever brackets close; a comment, a <c>using</c>, an attribute,
/// a line of a raw string literal) left out, and every run of <see cref="Window"/> such lines that stands in two places is a copy. It reads the lines
/// twice: as they are (<see cref="CloneKind.Exact"/>), and with every name, number and string made one token (<see cref="CloneKind.SameShape"/>),
/// so the same rule written with other names is found too. A run made only of lines that declare (<see cref="Declarations"/>) is no copy.
/// A copy that stands in two files, or twice in one file of the library or its tests, turns the test red with every place named, unless it is
/// a copy a <see cref="Declared"/> family declares (see <see cref="IsDeclared"/>): a family is a rule the plan decided not to write once, with
/// its reason, and no other file hides a copy. There is no ceiling, the budget is 0.</summary>
public class CodeCloneContractTests
{
    /// <summary>How many lines of code in a row make a copy.</summary>
    private const int Window = 8;

    /// <summary>How long a line of the same shape must be to count, so a run of short declarations of the same shape (a field, a
    /// <c>break</c>) is not a copy: at most two lines of a window may be shorter.</summary>
    private const int ShortLine = 12;

    /// <summary>The two ways the lines are read.</summary>
    public enum CloneKind
    {
        /// <summary>The lines as written, spaces collapsed.</summary>
        Exact,

        /// <summary>Every name, number and string one token: the same code with other names.</summary>
        SameShape,
    }

    /// <summary>Files that stand in a copy of each other because each one declares what the others declare, and whose copy is no rule written
    /// twice: <see cref="Reason"/> says what is declared, <see cref="Paths"/> are the files that declare it. A copy is accepted when every
    /// place it stands in is a file of one family and each file holds it once (a family of one file declares what the file repeats in
    /// itself); a copy that reaches a file outside the family, or stands twice in a file of a family of several, or a family that stands in
    /// no copy any more, turns the test red. Only a family the plan decided not to write once is declared: a file that merely resembles
    /// its sibling is a copy to remove.</summary>
    /// <param name="Reason">What the files declare, and why the declaration cannot be written once.</param>
    /// <param name="Paths">The files of the family, each from the root of the repository.</param>
    internal readonly record struct DeclaredFamily(string Reason, string[] Paths);

    /// <summary>The families of files that are declarations: a copy that stays inside one of them is no block written twice. What the plan
    /// decided to leave written per file: the four toolkits' controls (S5: the one rule of a gesture is <c>InteractionHost</c>, each
    /// framework control forwards to it), the four interpolation engines (SD8) and the two azimuthal projections (SD4), the six faces of the
    /// cube (SD10), and the table of composed verbs (a row of literals for each verb).</summary>
    internal static readonly DeclaredFamily[] Declared =
    [
        new("Each toolkit's chart and streaming control derives from its own framework control and forwards its input to the one InteractionHost: "
            + "WPF, Avalonia, Uno and MAUI share no base type, so the forwarding members (the handler, the controller factory) stand once per "
            + "toolkit (the surface is the owner inside each; on MAUI the chart view the streaming view derives from).",
            [
                "Src/MatPlotLibNet.Wpf/MplChartSurface.cs", "Src/MatPlotLibNet.Uno/MplChartSurface.cs", "Src/MatPlotLibNet.Avalonia/MplChartSurface.cs",
                "Src/MatPlotLibNet.Maui/MplChartView.cs",
                "Src/MatPlotLibNet.Wpf/MplChartControl.cs", "Src/MatPlotLibNet.Uno/MplChartElement.cs",
                "Src/MatPlotLibNet.Wpf/MplStreamingChartControl.cs", "Src/MatPlotLibNet.Uno/MplStreamingChartElement.cs",
            ]),
        new("The four interpolation engines are sealed singletons that name themselves and forward the two resampling interfaces to their own "
            + "algorithm: the shell is each engine's declaration, the algorithm is the one rule each owns.",
            [
                "Src/MatPlotLibNet/Rendering/Interpolation/BilinearInterpolation.cs", "Src/MatPlotLibNet/Rendering/Interpolation/HanningInterpolation.cs",
                "Src/MatPlotLibNet/Rendering/Interpolation/BicubicInterpolation.cs", "Src/MatPlotLibNet/Rendering/Interpolation/NearestInterpolation.cs",
            ]),
        new("The two azimuthal projections declare their centre, their parameter set and their constructor pair alike; the maths of each is its own "
            + "Forward and Bounds.",
            ["Src/MatPlotLibNet.Geo/Projections/AzimuthalEquidistant.cs", "Src/MatPlotLibNet.Geo/Projections/Stereographic.cs"]),
        new("The six faces of the 3-D cube each draw their two grid directions: six pairings of face and axis, each a line of its own.",
            ["Src/MatPlotLibNet/Rendering/ThreeDAxesRenderer.cs"]),
        new("The table of composed verbs holds one entry for each verb, and two verbs of one recipe (the phase spectrum beside the angle "
            + "spectrum, each over the same signal) are entered with the same members under their own names: an entry is a declaration of what "
            + "the verb is checked by.",
            ["Tst/MatPlotLibNet/Rendering/ComposedChartShapes.cs"]),
    ];

    /// <summary>How many facts of one shape in one file of any test project make a theory that was written as facts: <see cref="ShapeGroup"/> or
    /// more facts whose bodies differ only in their data (a string, a number, a named member such as <c>Theme.Default</c> or an enum member) are
    /// one theory with a row each, whether the body is a block or an expression.</summary>
    private const int ShapeGroup = 3;

    private static readonly Regex FactHead = new(@"\[Fact\]\s*(?:\[[^\]]*\]\s*)*public (?:(?:async|virtual|override) )*(?:void|Task) (\w+)\(\)\s*", RegexOptions.CultureInvariant);

    /// <summary>A line comment, a verbatim string, a string or a character, whichever stands first: read in one pass from the left, so a
    /// string that holds two slashes is no comment and a quote inside a comment is no string.</summary>
    private static readonly Regex CommentOrData = new(
        @"(?<comment>//[^\n]*)|(?<raw>""{3,})[\s\S]*?\k<raw>|@""(?:[^""]|"""")*""|""(?:[^""\\]|\\.)*""|'(?:[^'\\]|\\.)+'", RegexOptions.CultureInvariant);

    /// <summary>A number with its sign, so <c>-0.5</c> and <c>0.1</c> are the same piece of data; a minus after a name or a closing
    /// bracket is a subtraction and stays.</summary>
    private static readonly Regex SignedNumber = new(@"(?<![\w)\]]\s*)-?\b\d[\d_.]*(?:[eE][+-]?\d+)?[fdmulFDMUL]*\b", RegexOptions.CultureInvariant);

    /// <summary>A literal that is a word: <c>null</c>, <c>true</c> and <c>false</c> are data like a number or a string.</summary>
    private static readonly Regex LiteralWord = new(@"\b(?:null|true|false)\b", RegexOptions.CultureInvariant);

    private static readonly Regex NamedMember =new(@"\b[A-Z]\w*\.[A-Z]\w*\b(?!\s*[(<])", RegexOptions.CultureInvariant);

    /// <summary>A member called through a dot (<c>.Top(</c>, <c>.Get&lt;</c>), the name <c>Assert</c> itself left out: read as data, facts that
    /// differ only in the member they call are one shape too.</summary>
    private static readonly Regex CalledMember = new(@"(?<=\.)(?!Assert\b)[A-Z]\w*(?=\s*[(<])", RegexOptions.CultureInvariant);

    /// <summary>A name, whatever it names (a type, a member, a local, an argument, a static receiver, a keyword): the data token <c>D</c>
    /// is left as it is, so a datum and a name are two things.</summary>
    private static readonly Regex Identifier = new(@"\b(?!D\b)[A-Za-z_]\w*\b", RegexOptions.CultureInvariant);

    /// <summary>The shape of a fact as the subject of a theory reads it: every name is the row, so facts that differ in names alone, the
    /// type they are about, the member they call, read or set, a local, an argument or a static receiver, are one shape.</summary>
    private static string SubjectShapeOf(string shape) => Identifier.Replace(shape, "I");

    /// <summary>Where the body of a fact that starts at <paramref name="start"/> ends: the closing brace of a block, or the semicolon of an
    /// expression body, read past strings, characters and comments.</summary>
    private static int BodyEnd(string text, int start)
    {
        bool block = text[start] == '{';
        int depth = 0;
        for (int i = start; i < text.Length; i++)
        {
            char c = text[i];
            if (c == '/' && i + 1 < text.Length && text[i + 1] == '/')
            {
                i = SourceFile.LineEndAt(text, i);
            }
            else if (c == '"' && string.CompareOrdinal(text, i, "\"\"\"", 0, 3) == 0 && text[i - 1] != '@' && !(text[i - 1] == '$' && text[i - 2] == '@'))
            {
                int quotes = 3;
                while (text[i + quotes] == '"') quotes++;
                int close = text.IndexOf(new string('"', quotes), i + quotes, StringComparison.Ordinal);
                i = close < 0 ? text.Length : close + quotes - 1;
            }
            else if (c == '"')
            {
                bool verbatim = text[i - 1] == '@' || (text[i - 1] == '$' && text[i - 2] == '@');
                i = StringEnd(text, i, verbatim);
            }
            else if (c == '\'')
            {
                i = text[i + 1] == '\\' ? text.IndexOf('\'', i + 3) : i + 2;
            }
            else if (c is '{' or '(')
            {
                depth++;
            }
            else if (c is '}' or ')')
            {
                depth--;
                if (block && depth == 0) return i;
            }
            else if (c == ';' && !block && depth == 0)
            {
                return i;
            }
        }

        return text.Length - 1;
    }

    /// <summary>Where the string that opens at <paramref name="open"/> closes: a backslash escapes the next character, and in a verbatim
    /// string two quotes stand for one.</summary>
    private static int StringEnd(string text, int open, bool verbatim)
    {
        for (int i = open + 1; i < text.Length; i++)
        {
            if (text[i] == '\\' && !verbatim)
            {
                i++;
            }
            else if (text[i] == '"')
            {
                if (!verbatim || i + 1 >= text.Length || text[i + 1] != '"') return i;
                i++;
            }
        }

        return text.Length;
    }

    /// <summary>A fact's body with every piece of data made the one token <c>D</c> (a string, a character, a number, a named member that
    /// is not called), so facts that differ in data of another kind are one shape too; comments and spaces left out.</summary>
    private static string ShapeOf(string body)
    {
        string text = CommentOrData.Replace(body, match => match.Groups["comment"].Success ? "" : "D");
        text = LiteralWord.Replace(SignedNumber.Replace(text, "D"), "D");
        return Space.Replace(NamedMember.Replace(text, "D"), " ").Trim();
    }

    /// <summary>The facts of <paramref name="file"/>, each with the line it stands on, its name and the shape of its body.</summary>
    private static IEnumerable<FactAt> FactsOf(SourceFile file)
    {
        foreach (Match head in FactHead.Matches(file.Text))
        {
            int start = head.Index + head.Length;
            if (start >= file.Text.Length) continue;
            bool expression = string.CompareOrdinal(file.Text, start, "=>", 0, 2) == 0;
            if (!expression && file.Text[start] != '{') continue;
            int end = BodyEnd(file.Text, expression ? start + 2 : start);
            yield return new FactAt(SourceFile.LineIndexAt(file.Text, head.Index) + 1, head.Groups[1].Value,
                ShapeOf(file.Text[(expression ? start + 2 : start)..(end + 1)]));
        }
    }

    /// <summary>Every group of <see cref="ShapeGroup"/> or more facts of <paramref name="file"/> whose bodies have one shape, read with the
    /// members they call as they are written or with each called member as data (<see cref="CalledMember"/>): the file, the lines the facts
    /// stand on and the first fact's name.</summary>
    internal static IReadOnlyList<string> FactsOfOneShape(SourceFile file)
    {
        var facts = FactsOf(file).ToList();
        var byShape = facts.GroupBy(fact => fact.Shape, StringComparer.Ordinal).Select(group => group.ToList()).ToList();
        var byCall = facts.GroupBy(fact => CalledMember.Replace(fact.Shape, "V"), StringComparer.Ordinal)
            .Select(group => group.ToList())
            .Where(group => group.Count >= ShapeGroup && group.Select(fact => fact.Shape).Distinct(StringComparer.Ordinal).Count() > 1);

        return [.. byShape.Where(group => group.Count >= ShapeGroup || IsAThemePair(group)).Concat(byCall)
            .Select(group => $"{file.Path}:{string.Join(", ", group.Select(f => f.Line))} ({group[0].Name} and {group.Count - 1} more of its shape)")];
    }

    /// <summary>Every fact asked of two or more subjects in one folder, in one file or in several: facts whose bodies have one shape, every
    /// name read as data (<see cref="SubjectShapeOf"/>), and whose names END alike (the last part after an underscore, the outcome, is the
    /// same: <c>Light_Exists</c>, <c>Axes_Dark_Exists</c>, wherever the subject sits before it) or START alike (every part but the last
    /// is the same: <c>Loop_DefaultsToTrue</c>, <c>Loop_DefaultsFalse</c>, a datum named in the outcome), with the folder, the name read
    /// with its differing part as <c>*</c> and each place. A name with no underscore says no outcome and is read by neither.</summary>
    internal static IReadOnlyList<string> FactsOfOneSubject(IEnumerable<SourceFile> files)
    {
        var named = files.SelectMany(file => FactsOf(file)
                .Where(fact => fact.Name.Contains('_', StringComparison.Ordinal))
                .Select(fact => new FactIn(file.Path, fact)))
            .ToList();
        var groups = named.GroupBy(at => new { at.Folder, Shape = SubjectShapeOf(at.Fact.Shape), Name = "*_" + at.Outcome })
            .Concat(named.GroupBy(at => new { at.Folder, Shape = SubjectShapeOf(at.Fact.Shape), Name = at.Subject + "_*" }))
            .Where(group => group.Count() >= 2);

        return [.. groups.Select(group => (Places: string.Join(", ", group.Select(at => $"{at.Path}:{at.Fact.Line}")), Group: group))
            .DistinctBy(found => found.Places, StringComparer.Ordinal)
            .Select(found => $"{found.Group.Key.Folder}: {found.Group.Key.Name} in {found.Places}")];
    }

    /// <summary>Two facts of one shape of which one is named for the matplotlib default style (V2) and the other for the classic one: the
    /// same fact asked of two themes, which is a theory over <c>ThemeCases.Matplotlib</c> with the numbers of each as its row.</summary>
    private static bool IsAThemePair(List<FactAt> facts) =>
        facts.Count == 2
        && facts.Any(fact => fact.Name.Contains("V2", StringComparison.Ordinal))
        && facts.Any(fact => fact.Name.Contains("Classic", StringComparison.Ordinal));

    /// <summary>The shapes of a line that declares and works out nothing, one line for each field or each kind, written alike in every
    /// file because each file declares its own (a DECLARATION, read in the same-shape form). A run made of these lines only is no copy:
    /// the read-back of a written field (<c>if (dto.X is { } x) s.X = x;</c>, one line per field of the wire: the rule, read only what
    /// was written, is the line itself); a property a public record carries field by field (<c>public string Label { get; init; } = "";</c>:
    /// a model and its wire record, TreeNode and TreeNodeDto, declare the same fields, mapped by their one owner
    /// TreeNodeSerializationExtensions); and a kind's given text size (its public setting over its Resting constant, its GivenX slot and
    /// the two lines of IGivenTextSize, whose shape OneOwnerSourceContractTests holds: GivenSlotOfAModel, SeriesTextSize); a kind's wire name
    /// (<c>static string ISeriesKind&lt;X&gt;.Kind => "x";</c>, one line per kind, the registry's key); and the field-by-field write of a
    /// kind's wire (<c>public override SeriesDto ToSeriesDto() => new()</c> and its <c>Field = Value,</c> lines), with the lines that only end
    /// or open such a block (<c>return s;</c> at the end of a reader, <c>var s = axes.X(dto.A ?? [], dto.B ?? []);</c> at its start, the
    /// header of an override that takes nothing); the forward of a slot by an explicit interface member (<c>double? IGivenTextSize.GivenTextSize
    /// => GivenFontSize;</c>) and of one output by a streaming form (<c>outputs[0] = _step.Next(input);</c>); a row of a table of literals,
    /// names and calls of names (<c>new("Name", "what replaced it", Published: true),</c>, <c>{ nameof(A), nameof(B) },</c>); a visitor member
    /// that records what it was given; a line of assignments of literals in an object initializer; and a row of a table of numbers (<c>0.74, 0.76, -0.81,</c>), the data of an array
    /// of reference values. Each pattern was tried out of the list
    /// one at a time and together (the greedy order of the manifest): a pattern that hid no copy is not here, and one that hides a copy names
    /// the family in the manifest. A run with fewer than two lines that work something out is no copy either: one shared line (a call of the
    /// one owner) is not a block written twice.</summary>
    private static readonly Regex[] Declarations =
    [
        new(@"^if \(I(?:\.I)+(?:<I>)?(?:\(\))? is \{ \} I\) I\.I = I;$", RegexOptions.CultureInvariant),
        new(@"^static string I<I>\.I => S;$", RegexOptions.CultureInvariant),
        new(@"^public override I I\(\) => (?:new\(\)|I\(\) with)$", RegexOptions.CultureInvariant),
        new(@"^I = (?:this\.)?I(?:\.I)*(?:\(\))?,$", RegexOptions.CultureInvariant),
        new(@"^return I;$", RegexOptions.CultureInvariant),
        new(@"^(?:public|internal|private|protected) (?:(?:static|readonly|const|volatile) )*[\w<>?\[\], .]+ I(?: = [^;]+)?;$", RegexOptions.CultureInvariant),
        new(@"^(?:(?:public|internal|private|protected) )?(?:(?:sealed|abstract|static|readonly|partial|ref) )*(?:class|struct|interface|record(?: struct| class)?) I(?:<[^>]*>)?(?:\([^)]*\))?(?: : .+)?$", RegexOptions.CultureInvariant),
        new(@"^(?:public|internal|private|protected) (?:(?:override|virtual|abstract|static|required|new) )*[\w<>?\[\], .]+ I \{ get(?:;| =>)(?:[^{}]|\{[^{}]*\})*\}(?: = [^;]+;)?$", RegexOptions.CultureInvariant),
        new(@"^(?:public|internal|private|protected) (?:(?:override|virtual|static|new) )*[\w<>?\[\], .]+ I => (?:S|N|I(?:\.I)*(?:\([^()]*\))?);$", RegexOptions.CultureInvariant),
        new(@"^(?:public|internal|private|protected) (?:(?:override|virtual|abstract|static|sealed|new|async) )*(?:[\w<>?\[\], .]+ )?I(?:<[^>]*>)?\([^()]*\)(?: where .+)?(?: =>)?$", RegexOptions.CultureInvariant),
        new(@"^: (?:base|this)\(.*\)$", RegexOptions.CultureInvariant),
        new(@"^var I = I\.I\((?:I\.I(?: \?\? \[\])?(?:, )?)+\);$", RegexOptions.CultureInvariant),
        new(@"^internal static I I\([^()]*\) => I\.I\(S(?:, [^()]*)?\);$", RegexOptions.CultureInvariant),
        new(@"^=> I\.I\(I\.I\.I\([^()]*\), I\);$", RegexOptions.CultureInvariant),
        new(@"^=> I\.I\(new I(?:\.I)?\([^()]*\), I\);$", RegexOptions.CultureInvariant),
        new(@"^I\[N\] = I\.I\(I\);$", RegexOptions.CultureInvariant),
        new(@"^public I\([^()]*\) : base\([^()]*(?:\([^()]*\))?[^()]*\)(?: \{ \})?$", RegexOptions.CultureInvariant),
        new(@"^[\w<>?\[\], .]+ I\.I => I;$", RegexOptions.CultureInvariant),
        new(@"^new(?: I)?\((?:-?[NSI]|[@$]S|[., ()\[\]:]|=>|true|false|null|typeof|default|new)*\),?(?: //.*)?$", RegexOptions.CultureInvariant),
        new(@"^new(?: I)?\(\) \{ (?:I = (?:-?[NSI]|[@$]S|true|false|null)(?:, )?)+ \},?$", RegexOptions.CultureInvariant),
        new(@"^\{ (?:-?[NSI]|[@$]S|[., ()\[\]:]|=>|true|false|null|typeof|default|new)* \},?$", RegexOptions.CultureInvariant),
        new(@"^I = (?:-?[NSI]|[@$]S|true|false|null)(?:, I = (?:-?[NSI]|[@$]S|true|false|null))*,?$", RegexOptions.CultureInvariant),
        new(@"^public void I\(I I, I I\) => I = I\(I\);$", RegexOptions.CultureInvariant),
        new(@"^I = I \?\? I\.I,$", RegexOptions.CultureInvariant),
        new(@"^I = I\.I\.I\(I\.I\)[,;]$", RegexOptions.CultureInvariant),
        new(@"^I = I\.I[,;]$", RegexOptions.CultureInvariant),
        new(@"^(?:I\.I|_) => (?:new I\(\)|I),$", RegexOptions.CultureInvariant),
        new(@"^public override void I\(I I, I I\) => I\.I\(this, I\);$", RegexOptions.CultureInvariant),
        new(@"^void I\(I I, I I\);$", RegexOptions.CultureInvariant),
        new(@"^=> I\.I\(I(?:, I)?\);$", RegexOptions.CultureInvariant),
        new(@"^(?:public|internal|private|protected) (?:(?:override|virtual|abstract|static|sealed|new|async) )*(?:[\w<>?\[\], .]+ )?I(?:<[^>]*>)?\((?:[^()]*,)?$", RegexOptions.CultureInvariant),
        new(@"^public void I\(I I, I I\) => I\(new I\(I\), I\);$", RegexOptions.CultureInvariant),
        new(@"^(?:static I\(\)|I\(I\.I\.I, I\.I\);)$", RegexOptions.CultureInvariant),
        new(@"^(?:[\w<>?\[\], .]+ I(?: = [\w.]+)?, )*[\w<>?\[\], .]+ I(?: = [\w.]+)?[,)]$", RegexOptions.CultureInvariant),
        new(@"^-?N(?:, -?N)*,?$", RegexOptions.CultureInvariant),
    ];

    /// <summary>The shapes of a declaring line that only the names as written tell from work: a field set from the argument of the same name
    /// (<c>_period = period;</c>), one line per field a kind keeps.</summary>
    private static readonly Regex[] DeclarationsAsWritten =
    [
        new(@"^(?:this\.\w+|_\w+) = \w+;$", RegexOptions.CultureInvariant),
        new(@"^\w+(?:\.\w+)*\.ThrowIf\w+\(.*nameof\(\w+\)\);$", RegexOptions.CultureInvariant),
        new(@"^return IndicatorSpec\.\w+\(.*\);$", RegexOptions.CultureInvariant),
        new(@"^\w+\.Restore\w+\(dto\);$", RegexOptions.CultureInvariant),
        new(@"^public void \w+\(\w+ evt\) => Route\(evt\);$", RegexOptions.CultureInvariant),
    ];

    private static readonly HashSet<string> Keywords = new(
        ("abstract as base bool break byte case catch char checked class const continue decimal default delegate do double else enum event " +
         "explicit extern false finally fixed float for foreach goto if implicit in int interface internal is lock long namespace new null " +
         "object operator out override params private protected public readonly ref return sbyte sealed short sizeof stackalloc static " +
         "string struct switch this throw true try typeof uint ulong unchecked unsafe ushort using virtual void volatile while var async " +
         "await record init get set value with when yield").Split(' '), StringComparer.Ordinal);

    /// <summary>A line that carries no code: brackets, semicolons and commas only (<c>}</c>, <c>});</c>, <c>}));</c>, <c>]),</c>, <c>{ }</c>), so
    /// two blocks that close alike are not a copy for their closing lines.</summary>
    private static readonly Regex NoCode = new(@"^[\s{}()\[\];,]*$", RegexOptions.CultureInvariant);

    private static readonly string[] NoCodeStarts = ["using ", "namespace ", "#", "["];

    private static readonly Regex Space = new(@"\s+", RegexOptions.CultureInvariant);

    private static readonly Regex Text = new(@"""(?:[^""\\]|\\.)*""", RegexOptions.CultureInvariant);

    private static readonly Regex Number = new(@"\b\d[\d_\.]*[fdmulFDMUL]*\b", RegexOptions.CultureInvariant);

    private static readonly Regex Name = new(@"[A-Za-z_]\w*", RegexOptions.CultureInvariant);

    /// <summary>The files the measure leaves out: generated code and the data tables written by a generator.</summary>
    private static readonly string[] Generated = [".g.cs", ".Designer.cs", "MatplotlibColorTables.Data", "matplotlib-mathtext"];

    /// <summary>A line of a file as the scan reads it: where it stands, what it says once read, and whether it only declares
    /// (<see cref="Declarations"/>).</summary>
    private readonly record struct CodeLine(int Number, string Text, bool Declares);

    /// <summary>A fact of a file: the line it stands on, its name and the shape of its body.</summary>
    private readonly record struct FactAt(int Line, string Name, string Shape);

    /// <summary>A fact with the file it stands in: the folder of the file, the last part of the fact's name after its last underscore (what
    /// it asks) and every part before it (what it asks it of).</summary>
    private readonly record struct FactIn(string Path, FactAt Fact)
    {
        public string Folder => Path[..Math.Max(0, Path.LastIndexOf('/'))];

        public string Outcome => Fact.Name[(Fact.Name.LastIndexOf('_') + 1)..];

        public string Subject => Fact.Name[..Fact.Name.LastIndexOf('_')];
    }

    /// <summary>A run of lines that stands in a file.</summary>
    private readonly record struct Run(string Path, int FirstLine, int Index);

    /// <summary>The place of a run in the lines of the code of a file: the file and the index of the run's first line among them.</summary>
    private readonly record struct Slot(string Path, int Index);

    /// <summary>A place a copy stands in: the file and the line its first run starts on.</summary>
    internal readonly record struct Place(string Path, int Line);

    /// <summary>A block of code that stands in more than one place, as the scan read it.</summary>
    /// <param name="Kind">How the lines were read.</param>
    /// <param name="Lines">How many lines of code the block has: <see cref="Window"/> or more.</param>
    /// <param name="Places">Every place the block stands in.</param>
    internal sealed record Copy(CloneKind Kind, int Lines, IReadOnlyList<Place> Places)
    {
        /// <summary>The block named by its size and every place it stands in.</summary>
        public override string ToString() =>
            $"{Kind} {Lines} lines at {string.Join(", ", Places.Select(place => $"{place.Path}:{place.Line}"))}";
    }

    /// <summary>A line as the scan reads it, or <see langword="null"/> for a line that carries no code.</summary>
    private static string? Read(string line, CloneKind kind)
    {
        string trimmed = line.Trim();
        if (NoCode.IsMatch(trimmed) || SourceFile.IsComment(trimmed) || NoCodeStarts.Any(start => trimmed.StartsWith(start, StringComparison.Ordinal)))
        {
            return null;
        }

        string text = Space.Replace(trimmed, " ");
        if (kind == CloneKind.Exact) return text;
        text = Number.Replace(Text.Replace(text, "S"), "N");
        return Name.Replace(text, m => Keywords.Contains(m.Value) || m.Value is "S" or "N" ? m.Value : "I");
    }

    /// <summary>The lines of code of each source file, read as <paramref name="kind"/> says.</summary>
    private static Dictionary<string, List<CodeLine>> CodeOf(IEnumerable<SourceFile> sources, CloneKind kind)
    {
        var code = new Dictionary<string, List<CodeLine>>(StringComparer.Ordinal);
        foreach (var file in sources.Where(f => !Generated.Any(g => f.Path.Contains(g, StringComparison.Ordinal))))
        {
            var lines = new List<CodeLine>();
            var raw = file.Lines;
            bool inRawText = false;
            for (int i = 0; i < raw.Count; i++)
            {
                string line = raw[i];
                bool edge = line.Contains("\"\"\"", StringComparison.Ordinal);
                if (inRawText || edge)
                {
                    // The lines of a raw string literal are text a test hands to the code (a document, a JSON), not code.
                    if (edge && line.Split("\"\"\"").Length % 2 == 0) inRawText = !inRawText;
                    continue;
                }

                if (Read(line, kind) is { } text)
                {
                    string shape = kind == CloneKind.SameShape ? text : Read(line, CloneKind.SameShape)!;
                    string written = Read(line, CloneKind.Exact)!;
                    lines.Add(new CodeLine(i + 1, text, Declarations.Any(d => d.IsMatch(shape)) || DeclarationsAsWritten.Any(d => d.IsMatch(written))));
                }
            }

            code[file.Path] = lines;
        }

        return code;
    }

    /// <summary>Every block of code that stands in two or more places of <paramref name="sources"/>: each run of <see cref="Window"/> lines
    /// that stands somewhere else too, and the runs that follow it line by line in the same places counted as one block.</summary>
    /// <param name="sources">The files of the tree.</param>
    /// <param name="kind">How the lines are read.</param>
    /// <returns>The copies, each with its size and every place; empty when no block is written twice.</returns>
    internal static IReadOnlyList<Copy> CopiesIn(IEnumerable<SourceFile> sources, CloneKind kind)
    {
        var runs = new Dictionary<string, List<Run>>(StringComparer.Ordinal);
        foreach (var (file, lines) in CodeOf(sources, kind))
        {
            for (int k = 0; k + Window <= lines.Count; k++)
            {
                var window = lines.GetRange(k, Window);
                if (kind == CloneKind.SameShape && window.Count(line => line.Text.Length > ShortLine) < Window - 2) continue;
                if (window.Count(line => !line.Declares) < 2) continue;
                string key = string.Join('\n', window.Select(line => line.Text));
                if (!runs.TryGetValue(key, out var at)) runs[key] = at = [];
                at.Add(new Run(file, lines[k].Number, k));
            }
        }

        var clusters = runs.Values.Where(at => at.Count > 1).ToList();
        var clusterAt = new Dictionary<Slot, List<Run>>();
        foreach (var cluster in clusters)
        {
            foreach (var run in cluster) clusterAt[new Slot(run.Path, run.Index)] = cluster;
        }

        bool Follows(List<Run> previous, List<Run> next) =>
            previous.Count == next.Count && next.All(run => run.Index > 0 && clusterAt.GetValueOrDefault(new Slot(run.Path, run.Index - 1)) == previous);

        List<Run>? Next(List<Run> cluster) =>
            clusterAt.GetValueOrDefault(new Slot(cluster[0].Path, cluster[0].Index + 1)) is { } next && Follows(cluster, next) ? next : null;

        var continued = new HashSet<List<Run>>(clusters.Select(Next).OfType<List<Run>>());
        var copies = new List<Copy>();
        foreach (var head in clusters.Where(cluster => !continued.Contains(cluster)))
        {
            int length = Window;
            for (var next = Next(head); next is not null; next = Next(next)) length++;
            var places = head.OrderBy(run => run.Path, StringComparer.Ordinal).ThenBy(run => run.Index).Select(run => new Place(run.Path, run.FirstLine));
            copies.Add(new Copy(kind, length, [.. places]));
        }

        return [.. copies.OrderBy(copy => copy.Places[0].Path, StringComparer.Ordinal).ThenBy(copy => copy.Places[0].Line)];
    }

    /// <summary>Whether a copy stays inside one of <paramref name="families"/>: every place it stands in is a file of that one family, and in
    /// a family of several files each file stands in it once (a file that repeats the block inside itself is no declaration of the family).</summary>
    internal static bool IsDeclared(Copy copy, IEnumerable<DeclaredFamily> families) =>
        families.Any(family => copy.Places.All(place => family.Paths.Contains(place.Path, StringComparer.Ordinal))
            && (family.Paths.Length == 1 || copy.Places.Select(place => place.Path).Distinct(StringComparer.Ordinal).Count() == copy.Places.Count));

    /// <summary>The files of the library and of its tests, the tree read once for every reading of it.</summary>
    private static readonly Lazy<IReadOnlyList<SourceFile>> Tree = new(() => [.. RepositoryTree.SourceFiles().Concat(RepositoryTree.TestFiles())]);

    /// <summary>The copies of <paramref name="tree"/> that stand outside every declared family.</summary>
    private static IReadOnlyList<Copy> Violations(IEnumerable<SourceFile> tree, CloneKind kind) =>
        [.. CopiesIn(tree, kind).Where(copy => !IsDeclared(copy, Declared))];

    private static bool CrossesFiles(Copy copy) => copy.Places.Select(place => place.Path).Distinct(StringComparer.Ordinal).Count() > 1;

    /// <summary>No block of code stands in two files of the library or its tests, in either reading, unless every place is a file of one
    /// declared family.</summary>
    [Theory]
    [InlineData(CloneKind.Exact)]
    [InlineData(CloneKind.SameShape)]
    public void NoBlockOfCode_IsWrittenInTwoFiles_OutsideADeclaredFamily(CloneKind kind)
    {
        var copies = Violations(Tree.Value, kind).Where(CrossesFiles).ToList();

        Assert.True(copies.Count == 0, copies.Count + " copies:" + Environment.NewLine + string.Join(Environment.NewLine, copies));
    }

    /// <summary>No block of the library or of a test file stands twice in one file, in either reading, unless every place is a file of one
    /// declared family.</summary>
    [Theory]
    [InlineData(CloneKind.Exact)]
    [InlineData(CloneKind.SameShape)]
    public void NoBlockOfCode_IsWrittenTwiceInOneFile_OutsideADeclaredFamily(CloneKind kind)
    {
        var copies = Violations(Tree.Value, kind).Where(copy => !CrossesFiles(copy)).ToList();

        Assert.True(copies.Count == 0, copies.Count + " copies:" + Environment.NewLine + string.Join(Environment.NewLine, copies));
    }

    /// <summary>A declared family names files that exist, says why, and stands in a copy: a declaration that stands in none is deleted.</summary>
    [Fact]
    public void EveryDeclaredFamily_NamesFilesThatExist_SaysWhy_AndStandsInACopy()
    {
        var paths = Tree.Value.Select(file => file.Path).ToHashSet(StringComparer.Ordinal);
        var copied = new[] { CloneKind.Exact, CloneKind.SameShape }
            .SelectMany(kind => CopiesIn(Tree.Value, kind)).SelectMany(copy => copy.Places).Select(place => place.Path).ToHashSet(StringComparer.Ordinal);

        var problems = Declared.SelectMany(family => family.Paths
                .Where(path => !paths.Contains(path)).Select(path => $"{path} is no file of the tree")
                .Concat(family.Paths.Where(path => paths.Contains(path) && !copied.Contains(path)).Select(path => $"{path} stands in no copy: delete it from its family"))
                .Concat(string.IsNullOrWhiteSpace(family.Reason) || family.Paths.Length < 1 ? [$"a family of {family.Paths.Length} files says '{family.Reason}'"] : []))
            .ToList();

        Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
    }

    /// <summary>A test file is declared only when it is a table of literals, one entry for each verb (<c>ComposedChartShapes</c>): test classes
    /// that resemble each other, a harness repeated for every control or a shell written for every part are copies to fold into a theory, a
    /// fixture or a shared extension, never a family.</summary>
    [Fact]
    public void ADeclaredFamily_NamesNoTestFile_ButTheTableOfComposedVerbs()
    {
        var tests = Declared.SelectMany(family => family.Paths)
            .Where(path => path.StartsWith("Tst/", StringComparison.Ordinal) && path != "Tst/MatPlotLibNet/Rendering/ComposedChartShapes.cs");

        Assert.Empty(tests);
    }

    [Fact]
    public void FactsOfOneShape_InEveryTestProject_AreRowsOfATheory()
    {
        var files = RepositoryTree.TestFiles().ToList();
        var found = files.SelectMany(FactsOfOneShape).Concat(FactsOfOneSubject(files)).ToList();

        Assert.True(found.Count == 0, string.Join(Environment.NewLine, found));
    }

    /// <summary>Three facts that differ only in a number (signed or not), a string or a named member are one shape, written as a block or as an
    /// expression, and a semicolon or a brace inside a string does not end the body; two such facts, or three of which one calls
    /// something else, are not, unless one of the two is named for the V2 style and the other for the classic one.</summary>
    [Fact]
    public void ThreeFactsThatDifferOnlyInTheirData_AreOneShape()
    {
        static string Fact(string name, string body) => $"    [Fact]\n    public void {name}() {body}\n";
        string Expression(string name, string data) => Fact(name, $"=> Assert.Equal({data}, Clip(2, \"a; {{\"));");
        string three = Expression("A", "-0.5") + Expression("B", "Side.Top") + Expression("C", "\"x\"");
        string blocks = Fact("A", "{ var a = Clip(1, @\"\"\"}\"); Assert.Null(a); }") + Fact("B", "{ var a = Clip(2, \"}\"); Assert.Null(a); }")
            + Fact("C", "{ var a = Clip(3, Edge.Left); // the left edge }\n Assert.Null(a); }");
        string other = Expression("A", "1") + Expression("B", "2") + Fact("C", "=> Assert.Equal(3, Cut(2, \"a\"));");

        Assert.Equal("Tst/A.cs:1, 3, 5 (A and 2 more of its shape)", Assert.Single(FactsOfOneShape(new SourceFile("Tst/A.cs", three))));
        Assert.Single(FactsOfOneShape(new SourceFile("Tst/A.cs", blocks)));
        Assert.Empty(FactsOfOneShape(new SourceFile("Tst/A.cs", Expression("A", "1") + Expression("B", "2"))));
        Assert.Empty(FactsOfOneShape(new SourceFile("Tst/A.cs", other)));
        Assert.Single(FactsOfOneShape(new SourceFile("Tst/A.cs", Expression("V2_A", "1") + Expression("Classic_A", "2"))));
        Assert.Empty(FactsOfOneShape(new SourceFile("Tst/A.cs", Expression("V2_A", "1") + Expression("V2_B", "2"))));
    }

    /// <summary>A string is data whatever it holds: an address with two slashes (<c>"wss://a/b"</c>) is no comment, and a quote or an
    /// apostrophe inside a comment is no string, so three facts that differ only in such a string or follow such a comment are one shape.</summary>
    [Fact]
    public void AStringWithTwoSlashes_AndAQuoteInAComment_AreReadAsDataAndAsNothing()
    {
        static string Fact(string name, string body) => $"    [Fact]\n    public void {name}() {body}\n";
        string Url(string name, string url) => Fact(name, $"{{ Open(\"{url}\"); Assert.Equal(\"{url}\", Last()); }}");
        string three = Url("A", "wss://a/b") + Url("B", "http://c") + Url("C", "d");
        string comments = Fact("A", "{ Open(1); // it's \"quoted\"\n Assert.Null(Last()); }") + Fact("B", "{ Open(2); // don't\n Assert.Null(Last()); }")
            + Fact("C", "{ Open(3); Assert.Null(Last()); }");

        Assert.Single(FactsOfOneShape(new SourceFile("Tst/A.cs", three)));
        Assert.Single(FactsOfOneShape(new SourceFile("Tst/A.cs", comments)));
    }

    /// <summary>Three facts of one file that differ only in the member they call (and in their data) are one shape with the member as a
    /// row; two such facts are not, nor three of which one is built otherwise.</summary>
    [Fact]
    public void ThreeFactsThatDifferOnlyInTheMemberTheyCall_AreOneShape()
    {
        static string Fact(string name, string body) => $"    [Fact]\n    public void {name}() {body}\n";
        string Calls(string name, string member, string data) => Fact(name, $"=> Assert.True(Clip.{member}({data}, \"a\"));");
        string three = Calls("A", "Top", "1") + Calls("B", "Left", "2") + Calls("C", "Right", "3");
        string other = Calls("A", "Top", "1") + Calls("B", "Left", "2") + Fact("C", "=> Assert.True(Clip.Right(3, \"a\", 4));");

        Assert.Equal("Tst/A.cs:1, 3, 5 (A and 2 more of its shape)", Assert.Single(FactsOfOneShape(new SourceFile("Tst/A.cs", three))));
        Assert.Empty(FactsOfOneShape(new SourceFile("Tst/A.cs", Calls("A", "Top", "1") + Calls("B", "Left", "2"))));
        Assert.Empty(FactsOfOneShape(new SourceFile("Tst/A.cs", other)));
    }

    /// <summary>Two or more facts of one folder whose bodies have one shape and whose names end alike (the same last part) are one
    /// fact asked of several subjects, a theory with a row each, whether they stand in two files or in one, and whether the subjects
    /// differ in a datum or in the member the body calls; facts in two folders, with names that end and start differently, of another shape, or with no
    /// underscore in the name, are not.</summary>
    [Fact]
    public void FactsOfOneShapeWhoseNamesEndAlike_InOneFolder_AreOneFactOfSeveralSubjects()
    {
        static string Fact(string name, string data) => $"    [Fact]\n    public void {name}() => Assert.Equal({data}, Theme.Of(\"a\").Name);\n";
        static string Calls(string name, string member) => $"    [Fact]\n    public void {name}() => Assert.Equal(1, Theme.{member}(\"a\").Name);\n";
        SourceFile File(string path, string text) => new(path, text);

        Assert.Equal("Tst/F: *_Exists in Tst/F/A.cs:1, Tst/F/B.cs:1", Assert.Single(FactsOfOneSubject(
            [File("Tst/F/A.cs", Fact("Light_Exists", "1")), File("Tst/F/B.cs", Fact("Dark_Exists", "2"))])));
        Assert.Equal("Tst/F: *_Exists in Tst/F/A.cs:1, Tst/F/A.cs:3", Assert.Single(FactsOfOneSubject(
            [File("Tst/F/A.cs", Fact("Light_Exists", "1") + Fact("Dark_Exists", "2"))])));
        Assert.Equal("Tst/F: *_Exists in Tst/F/A.cs:1, Tst/F/B.cs:1", Assert.Single(FactsOfOneSubject(
            [File("Tst/F/A.cs", Calls("Light_Exists", "Of")), File("Tst/F/B.cs", Calls("Dark_Exists", "Named"))])));
        Assert.Equal("Tst/F: *_Exists in Tst/F/A.cs:1, Tst/F/A.cs:3", Assert.Single(FactsOfOneSubject(
            [File("Tst/F/A.cs", Calls("Light_Exists", "Of") + Calls("Dark_Exists", "Named"))])));
        Assert.Empty(FactsOfOneSubject([File("Tst/F/A.cs", Fact("Light_Exists", "1")), File("Tst/G/B.cs", Fact("Dark_Exists", "2"))]));
        Assert.Empty(FactsOfOneSubject([File("Tst/F/A.cs", Fact("Light_Exists", "1")), File("Tst/F/B.cs", Fact("Dark_IsNamed", "2"))]));
        Assert.Empty(FactsOfOneSubject([File("Tst/F/A.cs", Fact("Light_Exists", "1") + Fact("Dark_IsNamed", "2"))]));
        Assert.Empty(FactsOfOneSubject([File("Tst/F/A.cs", Fact("Light_Exists", "1")),
            File("Tst/F/B.cs", "    [Fact]\n    public void Dark_Exists() => Assert.Null(Theme.Of(\"a\"));\n")]));
        Assert.Empty(FactsOfOneSubject([File("Tst/F/A.cs", Fact("Exists", "1")), File("Tst/F/B.cs", Fact("Exists", "2"))]));
        Assert.Empty(FactsOfOneSubject([File("Tst/F/A.cs", Fact("Exists", "1") + Fact("Exists", "2"))]));
    }

    /// <summary>Two facts whose names end alike that differ only in a name (the type they are about, the member they read or set, a local, an
    /// argument, a static receiver) or in a datum, a raw string literal among them, are one fact of two subjects; a difference in an
    /// operator or in the number of arguments is another fact.</summary>
    [Fact]
    public void FactsOfTheSameOutcome_ThatDifferOnlyInANameOrADatum_AreOneFactOfTwoSubjects()
    {
        static string Fact(string name, string body) => $"    [Fact]\n    public void {name}() => {body};\n";
        SourceFile File(params string[] facts) => new("Tst/F/A.cs", string.Concat(facts));
        string Pair(string first, string second) => Assert.Single(FactsOfOneSubject([File(Fact("A_Same", first), Fact("B_Same", second))]));

        Assert.Equal("Tst/F: *_Same in Tst/F/A.cs:1, Tst/F/A.cs:3", Pair("Assert.Equal(1, new Linear(1).Name)", "Assert.Equal(1, new Listed(1).Name)"));
        Assert.Contains("Same", Pair("Assert.IsType<Fixed>(Make())", "Assert.IsType<AutoScale>(Make())"));
        Assert.Contains("Same", Pair("Assert.Equal(typeof(Zoom), Of(1).BaseType)", "Assert.Equal(typeof(Reset), Of(1).BaseType)"));
        Assert.Contains("Same", Pair("Assert.Equal(c, map.UnderColor)", "Assert.Equal(c, map.OverColor)"));
        Assert.Contains("Same", Pair("Assert.Equal(c, new Map { UnderColor = c }.Get())", "Assert.Equal(c, new Map { OverColor = c }.Get())"));
        Assert.Contains("Same", Pair("Assert.Equal(c, first.Name)", "Assert.Equal(c, second.Name)"));
        Assert.Contains("Same", Pair("Assert.Throws<ArgumentNullException>(() => Colors.Get(null!))", "Assert.Throws<ArgumentNullException>(() => Styles.Get(null!))"));
        Assert.Contains("Same", Pair("Assert.Equal(\"\"\"{\"a\":\"b\"}\"\"\", Read(1))", "Assert.Equal(\"\"\"{\"a\":\"b\",\"c\":\"d;\"}\"\"\", Read(1))"));
        Assert.Contains("Same", Pair("Assert.Equal(\"\"\"\"\"{\"a\":\"\"\"}\"\"\"\"\", Read(1))", "Assert.Equal(\"\"\"x\"\"\", Read(1))"));
        Assert.Empty(FactsOfOneSubject([File(Fact("A_Same", "Assert.Equal(c, new Map(1).Name)"), Fact("B_Same", "Assert.Equal(c, new Map(1, 2).Name)"))]));
        Assert.Empty(FactsOfOneSubject([File(Fact("A_Same", "Assert.Equal(c, first.Name)"), Fact("B_Same", "Assert.Equal(c, first.Name + 1)"))]));
    }

    /// <summary>A literal that is a word (<c>null</c>, <c>true</c>, <c>false</c>) is a datum like a string or a number: facts that differ only
    /// in one are one shape, and one fact asked with <c>null</c> and one asked with a named member are two subjects of one fact.</summary>
    [Fact]
    public void FactsThatDifferOnlyInNullTrueOrFalse_DifferInADatum()
    {
        static string Fact(string name, string data) => $"    [Fact]\n    public void {name}() => Assert.Equal(1, Apply({data}));\n";

        Assert.Single(FactsOfOneSubject([new("Tst/F/A.cs", Fact("Apply_NullStyle_ReturnsInput", "null") + Fact("Apply_DefaultStyle_ReturnsInput", "Style.Default"))]));
        Assert.Single(FactsOfOneShape(new SourceFile("Tst/F/A.cs", Fact("A", "null") + Fact("B", "true") + Fact("C", "false"))));
        Assert.Empty(FactsOfOneShape(new SourceFile("Tst/F/A.cs", Fact("A", "null") + Fact("B", "true") + Fact("C", "Make()"))));
    }

    /// <summary>A fact that is a task (<c>public async Task X_Y()</c>) or carries a second attribute is read like any other: two of one shape
    /// and names that end alike are one fact of two subjects.</summary>
    [Fact]
    public void AnAsynchronousFact_AndAFactWithASecondAttribute_AreReadLikeAnyOther()
    {
        static string Task(string name, string data) =>
            $"    [Fact]\n    public async Task {name}() => Assert.Equal({data}, await Theme.Of(\"a\"));\n";
        static string Marked(string name, string data) =>
            $"    [Fact]\n    [Trait(\"k\", \"v\")]\n    public void {name}() => Assert.Equal({data}, Theme.Of(\"a\"));\n";

        Assert.Single(FactsOfOneSubject([new("Tst/F/A.cs", Task("Light_Exists", "1") + Task("Dark_Exists", "2"))]));
        Assert.Single(FactsOfOneSubject([new("Tst/F/A.cs", Marked("Light_Exists", "1") + Marked("Dark_Exists", "2"))]));
    }

    /// <summary>A fact is a fact whatever modifier its method carries: <c>public virtual void</c> and <c>public override void</c> are read like
    /// <c>public void</c>.</summary>
    [Theory]
    [InlineData("virtual ")]
    [InlineData("override ")]
    [InlineData("async ")]
    public void AFactWithAModifier_IsReadLikeAnyOther(string modifier)
    {
        static string Fact(string modifier, string name, string data) =>
            $"    [Fact]\n    public {modifier}{(modifier == "async " ? "Task" : "void")} {name}() => Assert.Equal({data}, Theme.Of(\"a\"));\n";

        Assert.Single(FactsOfOneSubject([new("Tst/F/A.cs", Fact(modifier, "Light_Exists", "1") + Fact(modifier, "Dark_Exists", "2"))]));
        Assert.Empty(FactsOfOneShape(new SourceFile("Tst/F/A.cs", Fact(modifier, "Light_Exists", "1") + Fact(modifier, "Dark_Exists", "2"))));
        Assert.Single(FactsOfOneShape(new SourceFile("Tst/F/A.cs", Fact(modifier, "A", "1") + Fact(modifier, "B", "2") + Fact(modifier, "C", "3"))));
    }

    /// <summary>Two facts of one shape in one folder whose names END alike (the last part, the outcome, is the same: <c>Axes_AxHLine_Adds</c>,
    /// <c>Axes_AxVLine_Adds</c>, whatever the number of parts before it and wherever the subject sits in them) or START alike (all parts but
    /// the last are the same: <c>Loop_DefaultsToTrue</c>, <c>Loop_DefaultsFalse</c>, the datum is in the outcome's name) are one fact of two
    /// subjects; names that share nothing, or only the middle, are not.</summary>
    [Fact]
    public void FactsWhoseNamesEndAlikeOrStartAlike_AreOneFactOfSeveralSubjects()
    {
        static string Fact(string name, string data) => $"    [Fact]\n    public void {name}() => Assert.Equal({data}, Theme.Of(\"a\").Name);\n";
        SourceFile File(params string[] facts) => new("Tst/F/A.cs", string.Concat(facts));
        string Group(params string[] facts) => Assert.Single(FactsOfOneSubject([File(facts)]));

        Assert.Equal("Tst/F: *_AddsLine in Tst/F/A.cs:1, Tst/F/A.cs:3", Group(Fact("Axes_AxHLine_AddsLine", "5"), Fact("Axes_AxVLine_AddsLine", "3")));
        Assert.Equal("Tst/F: *_ReturnsInput in Tst/F/A.cs:1, Tst/F/A.cs:3", Group(Fact("Apply_NullStyle_ReturnsInput", "1"), Fact("Apply_DefaultStyle_ReturnsInput", "2")));
        Assert.Equal("Tst/F: *_Same in Tst/F/A.cs:1, Tst/F/A.cs:3", Group(Fact("A_X_Y_Same", "1"), Fact("B_Z_Same", "2")));
        Assert.Equal("Tst/F: Loop_* in Tst/F/A.cs:1, Tst/F/A.cs:3", Group(Fact("Loop_DefaultsToTrue", "1"), Fact("Loop_DefaultsFalse", "2")));
        Assert.Equal("Tst/F: Axes_Ax_* in Tst/F/A.cs:1, Tst/F/A.cs:3", Group(Fact("Axes_Ax_Adds", "1"), Fact("Axes_Ax_Removes", "2")));
        Assert.Empty(FactsOfOneSubject([File(Fact("Axes_AxHLine_AddsLine", "5"), Fact("Spans_AxVLine_AddsSpan", "3"))]));
        Assert.Empty(FactsOfOneSubject([File(Fact("Axes_AxHLine_AddsLine", "5"), Fact("Spans_AxHLine_AddsSpan", "3"))]));
        Assert.Empty(FactsOfOneSubject([File(Fact("Axes_AxHLine_AddsLine", "5")), new("Tst/G/B.cs", Fact("Axes_AxVLine_AddsLine", "3"))]));
        Assert.Empty(FactsOfOneSubject([File(Fact("AddsLine", "5"), Fact("AddsLine", "3"))]));
    }

    /// <summary>Eight lines that stand in two files are one copy, whatever the files call their locals when the shape is read.</summary>
    [Theory]
    [InlineData(CloneKind.Exact, "total")]
    [InlineData(CloneKind.SameShape, "sum")]
    public void EightLinesInTwoFiles_AreACopy(CloneKind kind, string otherName)
    {
        string Body(string name) => string.Join('\n', Enumerable.Range(0, Window).Select(i => $"        double {name}{i} = Math.Max(first[{i}], second[{i}]);"));

        var copy = Assert.Single(CopiesIn([new("Src/A.cs", Body("total")), new("Src/B.cs", Body(otherName))], kind));

        Assert.Equal(kind + " " + Window + " lines at Src/A.cs:1, Src/B.cs:1", copy.ToString());
    }

    /// <summary>A block longer than <see cref="Window"/> lines is one copy of its whole length, not a copy for each window of it.</summary>
    [Fact]
    public void ABlockLongerThanTheWindow_IsOneCopyOfItsWholeLength()
    {
        string body = string.Join('\n', Enumerable.Range(0, Window + 4).Select(i => $"        double total{i} = Math.Max(first[{i}], second[{i}]);"));

        var copy = Assert.Single(CopiesIn([new("Src/A.cs", body), new("Src/B.cs", "int x = 1;\n" + body)], CloneKind.Exact));

        Assert.Equal(Window + 4, copy.Lines);
        Assert.Equal([new Place("Src/A.cs", 1), new Place("Src/B.cs", 2)], copy.Places);
    }

    /// <summary>A copy is declared when every place it stands in is a file of one family, each file standing in it once; one place outside
    /// the family, places in two families, or two places in one file of a family of several files make it a copy again. A family of one
    /// file declares what that file repeats in itself.</summary>
    [Fact]
    public void ACopyIsDeclared_OnlyWhenEveryPlaceIsAFileOfOneFamily_EachFileOnce()
    {
        var copy = new Copy(CloneKind.Exact, Window, [new Place("Src/A.cs", 1), new Place("Src/B.cs", 1)]);
        var inOneFile = new Copy(CloneKind.Exact, Window, [new Place("Src/A.cs", 1), new Place("Src/A.cs", 40)]);
        var twiceInOneOfTwo = new Copy(CloneKind.Exact, Window, [new Place("Src/A.cs", 1), new Place("Src/A.cs", 40), new Place("Src/B.cs", 1)]);
        DeclaredFamily Family(params string[] paths) => new("the files declare one table each", paths);

        Assert.True(IsDeclared(copy, [Family("Src/A.cs", "Src/B.cs", "Src/C.cs")]));
        Assert.False(IsDeclared(copy, [Family("Src/A.cs", "Src/C.cs")]));
        Assert.False(IsDeclared(copy, [Family("Src/A.cs"), Family("Src/B.cs")]));
        Assert.False(IsDeclared(copy, []));
        Assert.False(IsDeclared(inOneFile, [Family("Src/A.cs", "Src/B.cs")]));
        Assert.False(IsDeclared(twiceInOneOfTwo, [Family("Src/A.cs", "Src/B.cs")]));
        Assert.True(IsDeclared(inOneFile, [Family("Src/A.cs")]));
    }

    /// <summary>One run of <see cref="Window"/> lines copied out of a file of the library into a file of its own turns the guard red, with
    /// both places named: the tree that stands green without it has the one copy and no other.</summary>
    [Fact]
    public void OneRunOfEightLinesCopiedIntoANewFile_TurnsTheGuardRed()
    {
        var source = Tree.Value.First(file => file.Path.StartsWith("Src/", StringComparison.Ordinal)
            && CodeOf([file], CloneKind.Exact)[file.Path] is { } lines && lines.Count >= Window && lines.GetRange(0, Window).Count(line => !line.Declares) >= 2);
        var code = CodeOf([source], CloneKind.Exact)[source.Path];
        string run = string.Join('\n', source.Lines.Skip(code[0].Number - 1).Take(code[Window - 1].Number - code[0].Number + 1));

        var found = Violations([.. Tree.Value, new SourceFile("Src/Injected.cs", run)], CloneKind.Exact);

        Assert.Contains(found, copy => copy.Places.Any(place => place.Path == "Src/Injected.cs") && copy.Places.Any(place => place.Path == source.Path));
    }

    /// <summary>A run of declarations (the read-back of each written field, one line per field) is no copy, nor is one with a single
    /// line of work; two lines that work something out make the run a copy again.</summary>
    [Fact]
    public void ARunOfDeclarations_IsNoCopy_ButTwoLinesOfWorkMakeItOne()
    {
        string Reads(string extra) =>
            string.Join('\n', Enumerable.Range(0, Window).Select(i => $"        if (dto.Field{i} is {{ }} field{i}) s.Field{i} = field{i};")) + extra;
        string one = "\n        double total = Math.Max(first[0], second[0]) * scale;";
        string two = one + "\n        double rest = Math.Min(first[1], second[1]) / scale;";

        Assert.Empty(CopiesIn([new("Src/A.cs", Reads("")), new("Src/B.cs", Reads(""))], CloneKind.SameShape));
        Assert.Empty(CopiesIn([new("Src/A.cs", Reads(one)), new("Src/B.cs", Reads(one))], CloneKind.SameShape));
        Assert.NotEmpty(CopiesIn([new("Src/A.cs", Reads(two)), new("Src/B.cs", Reads(two))], CloneKind.SameShape));
    }

    /// <summary>Each form of line a kind declares one of (a constant, a field, a type header, a property, a signature, a base call, a wire
    /// field read or written, a table row, a registration, a forward) is no work: eight of them in a row are no copy, under any names.</summary>
    [Theory]
    [InlineData("public const int DefaultPeriod{0} = 20;")]
    [InlineData("private readonly int _period{0};")]
    [InlineData("public sealed class Indicator{0} : CandleIndicator<SignalResult>")]
    [InlineData("public Color? Color{0} { get; set => Change(ref field, value); }")]
    [InlineData("public override int WarmupPeriod{0} => Spec.Warmup;")]
    [InlineData("public Indicator{0}(OhlcvBars bars, int period = DefaultPeriod)")]
    [InlineData(": base(bars{0}, Spec(period))")]
    [InlineData("_period{0} = period;")]
    [InlineData("bars.Close{0}.ThrowIfNotPositivePrices(nameof(Amihud), \"close\", nameof(bars));")]
    [InlineData("if (dto.Levels{0} is { } levels) s.Levels = levels;")]
    [InlineData("Visible{0} = ticks.Visible.OrNullAtRest(resting.Visible),")]
    [InlineData("FrameOn{0} = FrameOn{0} ?? Rest.FrameOn,")]
    [InlineData("new(1.5, 2.5, 3.5), // {0}")]
    [InlineData("new(\"Name{0}\", \"AddSubPlot(rows, cols)\", CellsOfAGrid, Published: true),")]
    [InlineData("new(\"Cap{0}\", figure => TheAxes(figure).Spans[0], \"\", typeof(SpanRegion)),")]
    [InlineData("{ nameof(EventplotSeries{0}), nameof(LineWidthRule.OnePixelCollection) },")]
    [InlineData("new() { Label{0} = \"Phones\", Value = 22 },")]
    [InlineData("Label{0} = \"Electronics\", Value = 42,")]
    [InlineData("public void Visit(LineSeries{0} series, RenderArea area) => LastVisited = nameof(LineSeries{0});")]
    [InlineData("LegendPosition.Upper{0} => new UpperStrategy(),")]
    [InlineData("void Visit(LineSeries{0} series, RenderArea area);")]
    [InlineData("public void OnZoom{0}(ZoomEvent evt) => Route(evt);")]
    [InlineData("Register(Engine{0}.Instance.Name, Engine{0}.Instance);")]
    public void EightLinesOfOneDeclaredForm_AreNoCopy_UnderAnyNames(string form)
    {
        string Run(string prefix) => string.Join('\n', Enumerable.Range(0, Window).Select(i => "        " + form.Replace("{0}", prefix + i, StringComparison.Ordinal)));

        Assert.Empty(CopiesIn([new("Src/A.cs", Run("a")), new("Src/B.cs", Run("b"))], CloneKind.SameShape));
    }

    /// <summary>Eight rows of a table of numbers stand in two files whatever the numbers are: the data of two arrays of reference values is no copy.</summary>
    [Fact]
    public void RowsOfANumberTable_AreNoCopy_WhateverTheNumbers()
    {
        string Table(double offset) => string.Join('\n', Enumerable.Range(0, Window).Select(i => $"        {offset + i}.5, -{i}.25, {i * 3}.125, {i}.75,"));

        Assert.Empty(CopiesIn([new("Tst/A.cs", Table(0)), new("Tst/B.cs", Table(10))], CloneKind.SameShape));
    }

    /// <summary>Seven lines are not a copy, nor are lines that carry no code (a comment, a line of brackets, semicolons and commas only,
    /// whichever brackets close), nor a run of short lines of the same shape.</summary>
    [Fact]
    public void SevenLines_BracesAndCommentsAndShortLines_AreNoCopy()
    {
        string seven = string.Join('\n', Enumerable.Range(0, Window - 1).Select(i => $"        double total{i} = Math.Max(first[{i}], second[{i}]);"));
        string[] noCodeLines = ["    {", "    // the same comment", "    ]);", "    ],", "    })", "    }));", "    })];", "    { }"];
        string noCode = string.Join('\n', Enumerable.Range(0, 2 * Window).Select(i => noCodeLines[i % noCodeLines.Length]));
        string shortLines = string.Join('\n', Enumerable.Range(0, Window).Select(i => $"int a{i};"));

        Assert.Empty(CopiesIn([new("Src/A.cs", seven), new("Src/B.cs", seven)], CloneKind.Exact));
        Assert.Empty(CopiesIn([new("Src/A.cs", noCode), new("Src/B.cs", noCode)], CloneKind.Exact));
        Assert.Empty(CopiesIn([new("Src/A.cs", shortLines), new("Src/B.cs", shortLines)], CloneKind.SameShape));
    }
}
