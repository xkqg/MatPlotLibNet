// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using System.Globalization;

namespace MatPlotLibNet.Rendering.MathText;

/// <summary>One family of math-mode commands: the commands that read their arguments the same way and build the same kind of box (a
/// space, a fraction, a radical, an accent, a font switch, a delimited group, a matrix, an operator). <see cref="Find"/> is the one
/// table a command name is looked up in; a name it does not hold is a symbol (<see cref="MathSymbol"/>), or the command as it was
/// written when it names nothing. Each family builds its box (<see cref="MathBox"/>) from arguments read, recursively, as math.</summary>
internal abstract class MathCommand
{
    private static readonly MathNames<MathCommand> Table = new(BuildTable());

    /// <summary>The family that reads the command <paramref name="name"/>, or <see langword="null"/> for a symbol.</summary>
    internal static MathCommand? Find(ReadOnlySpan<char> name) => Table.TryFind(name, out var family) ? family : null;

    /// <summary>Reads what follows the command from <paramref name="cursor"/> and adds its box.</summary>
    /// <param name="cursor">The parse, standing just after the command name.</param>
    /// <param name="name">The command name as written, without its backslash.</param>
    internal abstract void Read(scoped ref MathTextCursor cursor, ReadOnlySpan<char> name);

    /// <summary>Every command with its family; a name given twice throws, so a name has exactly one family. The spaces, the
    /// accents, the function names and the large operators are registered from the grammar (<see cref="MathGrammar"/>); the
    /// families that read a structure are named here.</summary>
    private static FrozenDictionary<string, MathCommand> BuildTable()
    {
        var table = new Dictionary<string, MathCommand>(StringComparer.Ordinal);

        void Add(MathCommand family, params string[] names)
        {
            foreach (var name in names) table.Add(name, family);
        }

        Add(new MathFraction(null, 1), "frac");
        Add(new MathFraction(null, 0), "dfrac");
        Add(new MathFraction(0, 1), "binom");
        Add(new MathGeneralFraction(), "genfrac");
        Add(new MathRadical(), "sqrt");
        Add(new MathRule(), "overline", "underline");
        Add(new MathAnnotation(), "overset", "underset");
        Add(new MathSubstack(), "substack");
        Add(new MathPhantom(), "phantom", "llap", "rlap");
        Add(new MathCustomSpace(), "hspace");
        Add(new MathOperatorNameCommand(), "operatorname");
        Add(new MathFontSwitch(FontVariant.Roman), "mathrm");
        Add(new MathTextRun(), "text");
        Add(new MathFontSwitch(FontVariant.Bold), "mathbf");
        Add(new MathFontSwitch(FontVariant.Italic), "mathit");
        Add(new MathFontSwitch(FontVariant.Calligraphic), "mathcal");
        Add(new MathFontSwitch(FontVariant.BlackboardBold), "mathbb");
        Add(new MathDelimiter(), "left", "middle", "right");
        Add(new MathEnvironment(), "begin");
        Add(new MathEnvironmentEnd(), "end");
        foreach (var space in MathGrammar.Spaces) Add(new MathSpacing(space.Value), space.Key);
        foreach (var accent in MathGrammar.Accents) Add(new MathAccent(accent.Value), accent.Key);
        foreach (var name in MathGrammar.FunctionNames)
        {
            Add(MathGrammar.LimitFunctions.Contains(name) ? MathOperatorName.Instance : MathFunctionName.Instance, name);
        }

        foreach (var name in MathGrammar.LargeOperators)
        {
            Add(MathGrammar.LimitOperators.Contains(name) ? MathLargeOperator.Instance : MathIntegral.Instance, name);
        }

        return table.ToFrozenDictionary(StringComparer.Ordinal);
    }

    /// <summary>A number written as an argument (<c>\hspace{2}</c>, the rule of <c>\genfrac</c>), or <paramref name="none"/> when it
    /// is none.</summary>
    private protected static double NumberOf(string text, double none) =>
        text.TryReadInvariant(NumberStyles.Float, out double value) ? value : none;
}

/// <summary>A space command (<c>\,</c>, <c>\quad</c> and the others of <see cref="MathGrammar.Spaces"/>): a space of its width in
/// ems; <c>\!</c> takes room away.</summary>
internal sealed class MathSpacing(double em) : MathCommand
{
    internal override void Read(scoped ref MathTextCursor cursor, ReadOnlySpan<char> name) => cursor.Add(KernBox.Of(em));
}

/// <summary><c>\hspace{w}</c>: a space of <c>w</c> ems (matplotlib's <c>customspace</c>).</summary>
internal sealed class MathCustomSpace : MathCommand
{
    internal override void Read(scoped ref MathTextCursor cursor, ReadOnlySpan<char> name) =>
        cursor.Add(KernBox.Of(NumberOf(cursor.ReadBraceGroup(), 0)));
}

/// <summary><c>\frac{num}{den}</c>, <c>\dfrac</c> (display style) and <c>\binom</c> (no rule, in parentheses): a stacked fraction
/// (<see cref="FractionBox"/>).</summary>
/// <param name="rule">The rule's thickness in pixels; <see langword="null"/>: the font's.</param>
/// <param name="style">The math style: 0 display, 1 text.</param>
internal sealed class MathFraction(double? rule, int style) : MathCommand
{
    internal override void Read(scoped ref MathTextCursor cursor, ReadOnlySpan<char> name)
    {
        var numerator = cursor.ReadArgument();
        var denominator = cursor.ReadArgument();
        var fraction = new FractionBox(numerator, denominator, rule, style);
        cursor.Add(rule == 0 ? new DelimitedBox("(", [fraction], [], ")") : fraction);
    }
}

/// <summary><c>\genfrac{ldelim}{rdelim}{rule}{style}{num}{den}</c>: a fraction with the delimiters, the rule (in pixels, as matplotlib
/// reads the number; the font's rule when empty, as LaTeX reads it) and the style (0 display to 3; text when empty) written.</summary>
internal sealed class MathGeneralFraction : MathCommand
{
    internal override void Read(scoped ref MathTextCursor cursor, ReadOnlySpan<char> name)
    {
        string left = MathDelimiter.GlyphOf(cursor.ReadBraceGroup());
        string right = MathDelimiter.GlyphOf(cursor.ReadBraceGroup());
        string thickness = cursor.ReadBraceGroup();
        double? rule = thickness.Length == 0 ? null : NumberOf(thickness, 0); // empty: the font's rule, as LaTeX reads it
        int style = (int)Math.Clamp(NumberOf(cursor.ReadBraceGroup(), 1), 0, 3);
        var fraction = new FractionBox(cursor.ReadArgument(), cursor.ReadArgument(), rule, style);
        cursor.Add(left.Length == 0 && right.Length == 0 ? fraction : new DelimitedBox(left, [fraction], [], right));
    }
}

/// <summary><c>\sqrt{x}</c> or <c>\sqrt[n]{x}</c>: a radical (<see cref="RadicalBox"/>), the index read as math up to its <c>]</c>.</summary>
internal sealed class MathRadical : MathCommand
{
    internal override void Read(scoped ref MathTextCursor cursor, ReadOnlySpan<char> name)
    {
        MathBox? index = null;
        if (cursor.Peek == '[')
        {
            cursor.Skip("[");
            index = cursor.ReadRow(MathStop.Bracket);
            cursor.Skip("]");
        }

        cursor.Add(new RadicalBox(cursor.ReadArgument(), index));
    }
}

/// <summary><c>\overline{x}</c> and <c>\underline{x}</c>: a rule over or under the body.</summary>
internal sealed class MathRule : MathCommand
{
    internal override void Read(scoped ref MathTextCursor cursor, ReadOnlySpan<char> name)
    {
        var body = cursor.ReadArgument();
        cursor.Add(name is "overline" ? new OverlineBox(body) : new UnderlineBox(body));
    }
}

/// <summary><c>\overset{a}{b}</c> and <c>\underset{a}{b}</c> (matplotlib's <c>_genset</c>): the annotation one size smaller, centred
/// over or under the body, as a limit stands (<see cref="LimitsBox"/>).</summary>
internal sealed class MathAnnotation : MathCommand
{
    internal override void Read(scoped ref MathTextCursor cursor, ReadOnlySpan<char> name)
    {
        var annotation = cursor.ReadArgument();
        var body = cursor.ReadArgument();
        cursor.Add(name is "overset" ? new LimitsBox(body, annotation, null) : new LimitsBox(body, null, annotation));
    }
}

/// <summary><c>\substack{a \\ b}</c>: rows centred on the widest, two rules apart (<see cref="StackBox"/>).</summary>
internal sealed class MathSubstack : MathCommand
{
    internal override void Read(scoped ref MathTextCursor cursor, ReadOnlySpan<char> name)
    {
        cursor.Skip("{");
        var rows = new List<MathBox>();
        while (true)
        {
            rows.Add(cursor.ReadRow(MathStop.StackRow));
            if (cursor.Peek != '\\') break;
            cursor.Skip(@"\\");
        }

        cursor.Skip("}");
        cursor.Add(new StackBox([.. rows]));
    }
}

/// <summary><c>\phantom{x}</c>, <c>\llap{x}</c> and <c>\rlap{x}</c>: the content's room without its ink, or its ink without its
/// room.</summary>
internal sealed class MathPhantom : MathCommand
{
    internal override void Read(scoped ref MathTextCursor cursor, ReadOnlySpan<char> name)
    {
        var content = cursor.ReadArgument();
        cursor.Add(name switch
        {
            "phantom" => new PhantomBox(content),
            "llap" => new LapBox(content, 1),
            _ => new LapBox(content, 0),
        });
    }
}

/// <summary><c>\hat{x}</c> and the other accents of <see cref="MathGrammar.Accents"/>: the argument under the combining mark the
/// accent sets over it.</summary>
internal sealed class MathAccent(string mark) : MathCommand
{
    internal override void Read(scoped ref MathTextCursor cursor, ReadOnlySpan<char> name) => cursor.Add(cursor.ReadArgument().WithAccent(mark));
}

/// <summary><c>\mathrm{text}</c>, <c>\mathbf{text}</c> and the other font switches: the argument, read as math, in that font
/// variant.</summary>
internal sealed class MathFontSwitch(FontVariant variant) : MathCommand
{
    internal override void Read(scoped ref MathTextCursor cursor, ReadOnlySpan<char> name) => cursor.Add(cursor.ReadArgument(variant));
}

/// <summary><c>\text{a b}</c>: the argument as it was written, in roman, its spaces kept and a backslash before a character dropped
/// (matplotlib reads it as quoted text, not as math: <c>\text{a \alpha}</c> is "a alpha").</summary>
internal sealed class MathTextRun : MathCommand
{
    internal override void Read(scoped ref MathTextCursor cursor, ReadOnlySpan<char> name) =>
        cursor.Add(new GlyphsBox(cursor.ReadBraceGroup().Replace("\\", string.Empty, StringComparison.Ordinal), FontVariant.Roman));
}

/// <summary><c>\left(</c> ... <c>\middle|</c> ... <c>\right)</c>: a delimited group (<see cref="DelimitedBox"/>), each delimiter one
/// token (<see cref="GlyphOf"/>), the content read as math up to the next <c>\middle</c> or <c>\right</c>. A <c>\right</c> or
/// <c>\middle</c> that no <c>\left</c> opened is its delimiter's character.</summary>
internal sealed class MathDelimiter : MathCommand
{
    /// <summary>The character the delimiter token <paramref name="token"/> stands for: <c>.</c>, the delimiter that is not drawn, as
    /// nothing; a symbol's name as its character (<c>\langle</c> is ⟨, <c>\|</c> is ‖); <c>\{</c> a brace; any other character as
    /// itself.</summary>
    internal static string GlyphOf(string token)
    {
        if (token.Length == 0 || token == ".") return string.Empty;
        if (token[0] != '\\' || token.Length == 1) return token;
        if (token.Length == 2 && MathTextCursor.Escaped.Contains(token[1])) return token[1].ToString();
        return MathSymbol.GlyphOf(token.AsSpan(1));
    }

    internal override void Read(scoped ref MathTextCursor cursor, ReadOnlySpan<char> name)
    {
        string left = GlyphOf(cursor.ReadToken());
        if (name is not "left")
        {
            cursor.AppendText(left);
            return;
        }

        var segments = new List<MathBox>();
        var middles = new List<string>();
        string right = string.Empty;
        while (true)
        {
            segments.Add(cursor.ReadRow(MathStop.Delimiter));
            if (cursor.AtCommand("middle"))
            {
                cursor.Skip(@"\middle");
                middles.Add(GlyphOf(cursor.ReadToken()));
                continue;
            }

            if (cursor.AtCommand("right"))
            {
                cursor.Skip(@"\right");
                right = GlyphOf(cursor.ReadToken());
            }

            break;
        }

        cursor.Add(new DelimitedBox(left, [.. segments], [.. middles], right));
    }
}

/// <summary>The delimiters of a matrix environment.</summary>
/// <param name="Left">The left delimiter's character, or empty.</param>
/// <param name="Right">The right delimiter's character, or empty.</param>
internal readonly record struct MathDelimiters(string Left, string Right);

/// <summary><c>\begin{env} a &amp; b \\ c &amp; d \end{env}</c>: a matrix (<see cref="MatrixBox"/>), its rows split at <c>\\</c> and its
/// cells at <c>&amp;</c>, each cell read as math, in the delimiters the environment names.</summary>
internal sealed class MathEnvironment : MathCommand
{
    internal override void Read(scoped ref MathTextCursor cursor, ReadOnlySpan<char> name)
    {
        string environment = cursor.ReadBraceGroup();
        var delimiters = DelimitersOf(environment);
        cursor.Add(new MatrixBox(environment, delimiters.Left, delimiters.Right, ReadCells(ref cursor)));
    }

    /// <summary>Reads the cells of a matrix up to its <c>\end{...}</c>: a cell ends at <c>&amp;</c>, a row at <c>\\</c>; a <c>\\</c>
    /// right before the end starts no row, and a matrix that holds nothing has none.</summary>
    /// <returns>The cells, row by row.</returns>
    private static MathBox[][] ReadCells(scoped ref MathTextCursor cursor)
    {
        var rows = new List<MathBox[]>();
        var cells = new List<MathBox>();
        while (true)
        {
            var cell = cursor.ReadRow(MathStop.Cell);
            if (cell != RowBox.Empty || cells.Count > 0 || !cursor.AtCommand("end")) cells.Add(cell);
            if (cursor.AtEnd || cursor.Peek == '$') break;
            if (cursor.Peek == '&')
            {
                cursor.Skip("&");
                continue;
            }

            if (cursor.AtCommand("end")) break;
            cursor.Skip(@"\\");
            rows.Add([.. cells]);
            cells.Clear();
            cursor.SkipSpaces();
            if (cursor.AtCommand("end")) break;
        }

        if (cells.Count > 0) rows.Add([.. cells]);
        if (cursor.AtCommand("end"))
        {
            cursor.Skip(@"\end");
            cursor.ReadBraceGroup();
        }

        return [.. rows];
    }

    /// <summary>The delimiters of the environment <paramref name="environment"/>: parentheses for <c>pmatrix</c>, brackets for
    /// <c>bmatrix</c>, braces for <c>Bmatrix</c>, bars for <c>vmatrix</c>, double bars for <c>Vmatrix</c>, none for any other.</summary>
    internal static MathDelimiters DelimitersOf(string environment) => environment switch
    {
        "pmatrix" => new("(", ")"),
        "bmatrix" => new("[", "]"),
        "Bmatrix" => new("{", "}"),
        "vmatrix" => new("|", "|"),
        "Vmatrix" => new("‖", "‖"),
        _ => new(string.Empty, string.Empty),
    };
}

/// <summary>An <c>\end{env}</c> that no <c>\begin</c> took: its name is read and nothing is drawn.</summary>
internal sealed class MathEnvironmentEnd : MathCommand
{
    internal override void Read(scoped ref MathTextCursor cursor, ReadOnlySpan<char> name) => cursor.ReadBraceGroup();
}

/// <summary><c>\lim</c>, <c>\max</c> and the other function names whose scripts are limits
/// (<see cref="MathGrammar.LimitFunctions"/>): the name in roman (<see cref="MathTextCursor.AddFunctionName"/>), its scripts its limits.</summary>
internal sealed class MathOperatorName : MathCommand
{
    /// <summary>The one instance; the family holds no state.</summary>
    internal static readonly MathOperatorName Instance = new();

    private MathOperatorName() { }

    internal override void Read(scoped ref MathTextCursor cursor, ReadOnlySpan<char> name) =>
        cursor.AddFunctionName(new GlyphsBox(name.ToString(), FontVariant.Roman), takesLimits: true);
}

/// <summary><c>\sin</c>, <c>\log</c>, <c>\exp</c> and the other function names of <see cref="MathGrammar.FunctionNames"/> whose
/// scripts are scripts: the name in roman, as a word (<see cref="MathTextCursor.AddFunctionName"/>).</summary>
internal sealed class MathFunctionName : MathCommand
{
    /// <summary>The one instance; the family holds no state.</summary>
    internal static readonly MathFunctionName Instance = new();

    private MathFunctionName() { }

    internal override void Read(scoped ref MathTextCursor cursor, ReadOnlySpan<char> name) =>
        cursor.AddFunctionName(new GlyphsBox(name.ToString(), FontVariant.Roman), takesLimits: false);
}

/// <summary><c>\operatorname{name}</c>: the argument in roman, as a function name whose scripts stand beside it.</summary>
internal sealed class MathOperatorNameCommand : MathCommand
{
    internal override void Read(scoped ref MathTextCursor cursor, ReadOnlySpan<char> name) =>
        cursor.AddFunctionName(cursor.ReadArgument(FontVariant.Roman), takesLimits: false);
}

/// <summary><c>\sum</c>, <c>\prod</c> and the other large operators of <see cref="MathGrammar.LimitOperators"/>: the sign at the set's
/// large-operator size, its scripts its limits.</summary>
internal sealed class MathLargeOperator : MathCommand
{
    /// <summary>The one instance; the family holds no state.</summary>
    internal static readonly MathLargeOperator Instance = new();

    private MathLargeOperator() { }

    internal override void Read(scoped ref MathTextCursor cursor, ReadOnlySpan<char> name) =>
        cursor.Add(new TakesLimitsBox(new LargeSymbolBox(MathSymbol.GlyphOf(name))));
}

/// <summary><c>\int</c>, <c>\oint</c> and the other integrals of <see cref="MathGrammar.LargeOperators"/>: the sign at the set's
/// large-operator size, its scripts beside it, as matplotlib sets an integral's (<see cref="IntegralBox"/>).</summary>
internal sealed class MathIntegral : MathCommand
{
    /// <summary>The one instance; the family holds no state.</summary>
    internal static readonly MathIntegral Instance = new();

    private MathIntegral() { }

    internal override void Read(scoped ref MathTextCursor cursor, ReadOnlySpan<char> name) =>
        cursor.Add(new IntegralBox(new LargeSymbolBox(MathSymbol.GlyphOf(name))));
}

/// <summary>A command the table does not hold: a symbol of the grammar (<see cref="MathGrammar.Symbols"/>) as its character, and
/// a name that is none as the command as it was written, backslash included, so a label shows what it was given.</summary>
internal sealed class MathSymbol : MathCommand
{
    /// <summary>The one instance; a symbol holds no state.</summary>
    internal static readonly MathSymbol Instance = new();

    /// <summary>The grammar's symbols, read by the characters of a name where it stands in a label.</summary>
    private static readonly MathNames<MathSymbolEntry> Names = new(MathGrammar.Symbols);

    private MathSymbol() { }

    /// <summary>The family of the symbol <paramref name="name"/>; <see langword="null"/> when the grammar holds no symbol of that
    /// name.</summary>
    /// <param name="name">The name as written, without its backslash.</param>
    internal static MathSymbol? Named(ReadOnlySpan<char> name) => Names.TryFind(name, out _) ? Instance : null;

    /// <summary>The symbol with the space matplotlib puts around it (<see cref="MathGrammar.SpacesAround"/>), read from what is
    /// written before its backslash and after its name.</summary>
    internal override void Read(scoped ref MathTextCursor cursor, ReadOnlySpan<char> name)
    {
        if (!Names.TryFind(name, out var symbol) || symbol.Role == MathSymbolRole.Ordinary)
        {
            cursor.AppendText(symbol.Glyph ?? AsWritten(name));
            return;
        }

        var math = cursor.Source;
        int end = cursor.Position;
        cursor.AddSpaced(symbol.Glyph, symbol.Role, '\0', math.Before(end - name.Length - 1), math.After(end));
    }

    /// <summary>The character of the command <paramref name="name"/>: its symbol, or <c>\name</c>.</summary>
    internal static string GlyphOf(ReadOnlySpan<char> name) => Names.TryFind(name, out var symbol) ? symbol.Glyph : AsWritten(name);

    /// <summary>The command <paramref name="name"/> as it was written, backslash included.</summary>
    private static string AsWritten(ReadOnlySpan<char> name) => string.Concat(@"\", name);
}
