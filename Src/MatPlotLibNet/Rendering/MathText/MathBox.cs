// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using System.Globalization;

namespace MatPlotLibNet.Rendering.MathText;

/// <summary>What TeX reads of the last character of a nucleus to place its scripts (<c>Parser.subsuper</c>, node756): its height and
/// depth, whether it leans (a math italic letter, an integral sign) and whether its scripts drop beside it as an integral's do.</summary>
/// <param name="Height">How far the character rises above the baseline.</param>
/// <param name="Depth">How far it reaches below the baseline.</param>
/// <param name="Slanted">Whether the character leans right, so a superscript stands further from it.</param>
/// <param name="DropsScripts">Whether the character is an integral sign, whose scripts stand by its top and its foot.</param>
internal readonly record struct MathLastChar(double Height, double Depth, bool Slanted, bool DropsScripts);

/// <summary>A box of math as TeX's box model holds it (matplotlib's <c>_mathtext</c> nodes): what the parse of a label makes
/// (<see cref="MathTextCursor"/>, each command leaf of <see cref="MathCommand"/> building its box) and what the one layout of math reads.
/// Each kind of box is a sealed leaf that lays itself out (<see cref="Lay"/>) by the rule TeX lays that construction out by, on the
/// constants of the font set (<see cref="MathConstants"/>), and writes its part of the public flat view (<see cref="Flatten"/>). A box
/// knows no surface: it places runs of glyphs and rules (<see cref="MathInk"/>), which the one verb draws through any surface's public
/// <c>DrawText</c> and <c>DrawRectangle</c>.</summary>
internal abstract class MathBox
{
    /// <summary>Lays the box out with the left end of its baseline at the origin, into <paramref name="ink"/>.</summary>
    /// <param name="ink">Where the runs and rules go.</param>
    /// <param name="style">The size and the measure.</param>
    /// <returns>The room the box takes.</returns>
    internal abstract MathExtent Lay(MathInk ink, in MathStyle style);

    /// <summary>Whether TeX counts the box as a character node, holding no vertical list (node756): a script of such a nucleus is
    /// raised by the script's own rules alone, not from the nucleus's top and bottom.</summary>
    internal virtual bool IsCharNode => true;

    /// <summary>The last character of the box, when the box ends in one, as the scripts of a nucleus read it.</summary>
    /// <param name="style">The style the box is laid out in.</param>
    internal virtual MathLastChar? LastChar(in MathStyle style) => null;

    /// <summary>This box as the nucleus of <paramref name="superscript"/> and <paramref name="subscript"/>: scripts beside it, as a
    /// nucleus takes them; a large operator or a function name whose scripts are limits takes them above and below
    /// (<see cref="TakesLimitsBox"/>).</summary>
    /// <param name="superscript">The superscript, or <see langword="null"/>.</param>
    /// <param name="subscript">The subscript, or <see langword="null"/>.</param>
    internal virtual MathBox WithScripts(MathBox? superscript, MathBox? subscript) => new ScriptsBox(this, superscript, subscript);

    /// <summary>This box under the accent <paramref name="mark"/> (a combining character): a run of glyphs takes the mark on its last
    /// character, which the face places over it; any other box is followed by the mark.</summary>
    /// <param name="mark">The combining mark.</param>
    internal virtual MathBox WithAccent(string mark) => new AccentBox(new RowBox([this, new GlyphsBox(mark)]));

    /// <summary>Writes the box into the public flat view of the label (<see cref="RichText"/>).</summary>
    /// <param name="writer">The spans being written.</param>
    internal abstract void Flatten(MathSpanWriter writer);

    /// <summary>Adds the pieces this box is painted as, with their bidi levels, for a label that reads right to left
    /// (<see cref="LineBox"/>): the box whole, at the level of its first character.</summary>
    /// <param name="levels">The level of each character of the box's text (<see cref="MathSpanWriter.TextOf"/>), at least one.</param>
    /// <param name="pieces">The pieces.</param>
    /// <param name="pieceLevels">Their levels.</param>
    internal virtual void CutAtLevels(ReadOnlySpan<byte> levels, List<MathBox> pieces, List<byte> pieceLevels)
    {
        pieces.Add(this);
        pieceLevels.Add(levels[0]);
    }

    /// <summary>How far a box of <paramref name="inner"/> width stands from the left of one <paramref name="outer"/> wide when it is
    /// centred in it (matplotlib's <c>HCentered</c>, two glues that stretch alike): half the room to spare on its left, on whole pixels,
    /// as matplotlib's <c>ship</c> sets a stretched glue (<c>round</c>, halves to even).</summary>
    /// <param name="outer">The width it is centred in.</param>
    /// <param name="inner">Its own width.</param>
    protected static double CentredOffset(double outer, double inner) => Math.Round(Range1D.CentredIn(0, outer, inner).Lo);
}

/// <summary>A run of characters in one font switch (matplotlib's <c>Char</c>s, set side by side): cut into runs of one face each by the
/// font set (<see cref="MathFontSet.RunsOf"/>), each measured and placed after the one before.</summary>
/// <param name="text">The characters.</param>
/// <param name="variant">Their font switch.</param>
/// <param name="outsideMath">Whether they are the label's text outside <c>$...$</c>, which stays in the label's face.</param>
/// <param name="scale">The size against the size the box is laid out at: 1, but for a span of a rich text made by hand that names
/// its own size.</param>
internal sealed class GlyphsBox(string text, FontVariant variant = FontVariant.Default, bool outsideMath = false, double scale = 1) : MathBox
{
    /// <summary>Every ASCII character as a run of its own in math, in no font switch, made once: a box holds no state of a label, so
    /// one box of a character serves every label that sets it alone (a script, a nucleus, an operator).</summary>
    private static readonly GlyphsBox?[] Ascii = [.. Enumerable.Range(0, 128).Select(c => new GlyphsBox(((char)c).ToString()))];

    /// <summary>A run of <paramref name="text"/> at the size it is laid out at: the one box of an ASCII character alone in math in no
    /// font switch, else a new box.</summary>
    /// <param name="text">The characters.</param>
    /// <param name="variant">Their font switch.</param>
    /// <param name="outsideMath">Whether they are the label's text outside <c>$...$</c>.</param>
    internal static GlyphsBox Of(string text, FontVariant variant, bool outsideMath) =>
        (text.Length == 1 && variant == FontVariant.Default && !outsideMath ? Ascii.ElementOr(text[0], (GlyphsBox?)null) : null)
            ?? new GlyphsBox(text, variant, outsideMath);

    /// <summary>The character <paramref name="c"/> as a text: the one string of an ASCII character, made once.</summary>
    /// <param name="c">A character.</param>
    internal static string TextOf(char c) => Ascii.ElementOr(c, (GlyphsBox?)null)?._text ?? c.ToString();

    private readonly string _text = text;

    /// <inheritdoc />
    internal override MathExtent Lay(MathInk ink, in MathStyle style)
    {
        var at = style.Scaled(scale);
        var runs = at.Set.RunsOf(_text, variant, outsideMath);
        double x = 0, height = 0, depth = 0;
        for (int i = 0; i < runs.Count; i++)
        {
            var run = runs[i];
            if (run.Text.Length == 0) continue; // an empty run (an empty script) is no ink and takes no room
            var measured = at.Run(run.Face, run.Text);
            ink.AddGlyphs(run.Text, measured.Font, x, -measured.Lift);
            x += measured.Extent.Width;
            height = Math.Max(height, measured.Extent.Height);
            depth = Math.Max(depth, measured.Extent.Depth);
        }

        return new MathExtent(x, height, depth);
    }

    /// <inheritdoc />
    /// <remarks>The last cluster of the last run (a character with the marks after it, or a surrogate pair), measured on its own.</remarks>
    internal override MathLastChar? LastChar(in MathStyle style)
    {
        if (_text.Length == 0) return null;
        var at = style.Scaled(scale);
        var runs = at.Set.RunsOf(_text, variant, outsideMath);
        var run = runs[runs.Count - 1];
        string last = run.Text;
        int start = LastClusterStart(last);
        var size = at.Run(run.Face, start == 0 ? last : last[start..]).Extent;
        return new MathLastChar(size.Height, size.Depth, run.Face.Slanted, DropsScripts: false);
    }

    /// <summary>Where the last cluster of <paramref name="text"/> starts: its last character with the combining marks after it, or a
    /// surrogate pair. The nucleus of scripts written after a run is that cluster.</summary>
    /// <param name="text">A text of at least one character.</param>
    internal static int LastClusterStart(string text)
    {
        int start = text.Length - 1;
        while (start > 0 && (char.IsLowSurrogate(text[start]) || CharUnicodeInfo.GetUnicodeCategory(text[start]) is
                   UnicodeCategory.NonSpacingMark or UnicodeCategory.SpacingCombiningMark or UnicodeCategory.EnclosingMark))
        {
            start--;
        }

        return start;
    }

    /// <inheritdoc />
    internal override MathBox WithAccent(string mark) => new AccentBox(new GlyphsBox(_text + mark, variant, outsideMath, scale));

    /// <inheritdoc />
    internal override void Flatten(MathSpanWriter writer) => writer.Glyphs(_text, variant, outsideMath, scale);

    /// <inheritdoc />
    /// <remarks>The run is cut where the level changes, each piece a run of its own.</remarks>
    internal override void CutAtLevels(ReadOnlySpan<byte> levels, List<MathBox> pieces, List<byte> pieceLevels)
    {
        for (int start = 0, i = 1; i <= _text.Length; i++)
        {
            if (i < _text.Length && levels[i] == levels[start]) continue;
            pieces.Add(start == 0 && i == _text.Length ? this : new GlyphsBox(_text[start..i], variant, outsideMath, scale));
            pieceLevels.Add(levels[start]);
            start = i;
        }
    }
}

/// <summary>A space of a width in ems (matplotlib's <c>Kern</c> from <c>_make_space</c>): a space command, the space around an operator
/// and after punctuation, the thin space after a function name. A negative width takes room away.</summary>
/// <param name="em">The width, in quads of the size it is laid out at.</param>
internal sealed class KernBox(double em) : MathBox
{
    /// <summary>The kerns of the grammar's widths (its spaces and the space around a symbol), made once: a box holds no state, so one
    /// instance of a width serves every label.</summary>
    private static readonly Dictionary<double, KernBox> Shared = MathGrammar.Spaces.Values.Concat(MathGrammar.LiteralSpaces.Values)
        .Append(MathGrammar.SymbolSpaceWidth).Distinct().ToDictionary(width => width, width => new KernBox(width));

    /// <summary>A kern of <paramref name="width"/> ems: the one made for a width of the grammar, else a new one.</summary>
    /// <param name="width">The width, in quads.</param>
    internal static KernBox Of(double width) => Shared.TryGetValue(width, out var kern) ? kern : new KernBox(width);

    /// <summary>The Unicode spaces the flat view writes the kern as (<see cref="MathGrammar.SpaceOf"/>), worked out the first time a
    /// flat view is written of it.</summary>
    private string? _spaces;

    /// <inheritdoc />
    internal override MathExtent Lay(MathInk ink, in MathStyle style) => new(style.Space(em), 0, 0);

    /// <inheritdoc />
    internal override void Flatten(MathSpanWriter writer) => writer.Space(_spaces ??= MathGrammar.SpaceOf(em));
}

/// <summary>Boxes side by side on one baseline (matplotlib's <c>Hlist</c>): a group, a row of math, the content of a fraction's part.</summary>
/// <param name="children">The boxes, left to right.</param>
internal sealed class RowBox(MathBox[] children) : MathBox
{
    /// <summary>The row that holds nothing: what an argument that is not there, a script with no nucleus and an empty row read as one
    /// box are. A box holds no state of a draw, so the one empty row serves every label.</summary>
    internal static RowBox Empty { get; } = new([]);

    /// <inheritdoc />
    internal override MathExtent Lay(MathInk ink, in MathStyle style) => LayInOrder(ink, style, children);

    /// <summary>Lays <paramref name="boxes"/> out one after the other along the baseline: a row, and a label in the order it is
    /// painted (<see cref="LineBox"/>).</summary>
    internal static MathExtent LayInOrder(MathInk ink, in MathStyle style, ReadOnlySpan<MathBox> boxes)
    {
        double x = 0, height = 0, depth = 0;
        foreach (var box in boxes)
        {
            var from = ink.Mark;
            var size = box.Lay(ink, style);
            ink.Shift(from, x, 0);
            x += size.Width;
            height = Math.Max(height, size.Height);
            depth = Math.Max(depth, size.Depth);
        }

        return new MathExtent(x, height, depth);
    }

    /// <inheritdoc />
    internal override bool IsCharNode => Array.TrueForAll(children, static child => child.IsCharNode);

    /// <inheritdoc />
    internal override MathLastChar? LastChar(in MathStyle style) => children.Length == 0 ? null : children[^1].LastChar(style);

    /// <inheritdoc />
    /// <remarks>A group of one box (<c>\hat{x}</c>) puts the accent on that box.</remarks>
    internal override MathBox WithAccent(string mark) => children.Length == 1 ? children[0].WithAccent(mark) : base.WithAccent(mark);

    /// <inheritdoc />
    internal override void Flatten(MathSpanWriter writer)
    {
        foreach (var child in children) child.Flatten(writer);
    }
}

/// <summary>A whole label: its boxes side by side, painted left to right in the order the bidi algorithm puts them in when the label
/// holds right-to-left text (one paragraph over the label's text, as the viewer of an SVG text resolves it, a run of glyphs cut where
/// the level changes, rule L2), else in the order they are written. matplotlib reads no direction; the library's labels do.</summary>
/// <param name="text">The label's text, which says whether anything in it reads right to left.</param>
/// <param name="children">The boxes, in the order they are written.</param>
internal sealed class LineBox(string text, MathBox[] children) : MathBox
{
    /// <summary>How many layouts a label keeps: a text is set in a few styles at most (a title, an axis label, a legend entry).</summary>
    private const int KeptLayouts = 4;

    /// <summary>The label's layouts, newest first, each with what it was laid out by: a label measured and then drawn, or measured again
    /// on a later draw of the figure that keeps it (<see cref="KeptMathTexts"/>), is laid out once in each style it is set in. One
    /// reference, replaced whole, so a reader never sees half of it.</summary>
    private LaidOutLine[] _layouts = [];

    /// <summary>A layout of the label and the set, the font and the measure it was made by: its box, and its ink when a draw asked for
    /// it (a measure asks only for the box, and places no ink).</summary>
    private sealed record LaidOutLine(MathFontSet Set, Styling.Font Font, MathMeasure Measure, MathExtent Extent, MathInk? Ink)
    {
        /// <summary>Whether this layout was made in <paramref name="style"/>.</summary>
        internal bool In(in MathStyle style) => ReferenceEquals(Set, style.Set) && Measure == style.Measure && Font == style.Label;
    }

    /// <summary>The label laid out in <paramref name="style"/>: the layout kept for the style when it placed its ink, else a new one,
    /// kept in its place.</summary>
    /// <param name="style">The label's style.</param>
    /// <returns>The ink.</returns>
    internal MathInk LaidOut(in MathStyle style)
    {
        var layouts = _layouts;
        int at = IndexIn(layouts, style);
        if (at >= 0 && layouts[at].Ink is { } kept) return kept;

        var ink = MathInk.Of(this, style);
        _layouts = Kept(layouts, at, new LaidOutLine(style.Set, style.Label, style.Measure, ink.Extent, ink));
        return ink;
    }

    /// <summary>The box of the label laid out in <paramref name="style"/>: the one of the layout kept for the style, else the box of a
    /// layout that places no ink, kept.</summary>
    /// <param name="style">The label's style.</param>
    /// <returns>The box.</returns>
    internal MathExtent ExtentIn(in MathStyle style)
    {
        var layouts = _layouts;
        int at = IndexIn(layouts, style);
        if (at >= 0) return layouts[at].Extent;

        var extent = MathInk.ExtentOf(this, style);
        _layouts = Kept(layouts, at, new LaidOutLine(style.Set, style.Label, style.Measure, extent, null));
        return extent;
    }

    /// <summary>Where the layout made in <paramref name="style"/> stands in <paramref name="layouts"/>; -1 when none is kept.</summary>
    private static int IndexIn(LaidOutLine[] layouts, in MathStyle style)
    {
        for (int i = 0; i < layouts.Length; i++)
            if (layouts[i].In(style)) return i;
        return -1;
    }

    /// <summary>The layouts with <paramref name="layout"/> in the place of the one at <paramref name="at"/>, or first when there is none
    /// (the oldest let go of past <see cref="KeptLayouts"/>).</summary>
    private static LaidOutLine[] Kept(LaidOutLine[] layouts, int at, LaidOutLine layout)
    {
        if (at >= 0)
        {
            var replaced = (LaidOutLine[])layouts.Clone();
            replaced[at] = layout;
            return replaced;
        }

        var kept = new LaidOutLine[Math.Min(layouts.Length + 1, KeptLayouts)];
        kept[0] = layout;
        Array.Copy(layouts, 0, kept, 1, kept.Length - 1);
        return kept;
    }

    /// <inheritdoc />
    internal override MathExtent Lay(MathInk ink, in MathStyle style) =>
        RowBox.LayInOrder(ink, style, Text.BidiAlgorithm.ReadsInWrittenOrder(text) ? children : InPaintOrder());

    /// <summary>The boxes in the order they are painted: each box at the level of its first character, a run of glyphs cut where the
    /// level changes, the pieces put in visual order.</summary>
    private MathBox[] InPaintOrder()
    {
        var texts = new string[children.Length];
        for (int i = 0; i < children.Length; i++) texts[i] = MathSpanWriter.TextOf(children[i]);
        var paragraph = Text.BidiAlgorithm.Resolve(string.Concat(texts));
        byte[] levels = paragraph.RenderLevels();

        var pieces = new List<MathBox>(children.Length + 2);
        var pieceLevels = new List<byte>(children.Length + 2);
        int offset = 0;
        for (int i = 0; i < children.Length; i++)
        {
            if (texts[i].Length == 0)
            {
                // A box with no text (a phantom, a space of no width) stands with what is written before it.
                pieces.Add(children[i]);
                pieceLevels.Add(pieceLevels.Count > 0 ? pieceLevels[^1] : paragraph.ParagraphLevel);
                continue;
            }

            children[i].CutAtLevels(levels.AsSpan(offset, texts[i].Length), pieces, pieceLevels);
            offset += texts[i].Length;
        }

        Text.BidiAlgorithm.ReverseByLevels(System.Runtime.InteropServices.CollectionsMarshal.AsSpan(pieces),
            System.Runtime.InteropServices.CollectionsMarshal.AsSpan(pieceLevels));
        return [.. pieces];
    }

    /// <inheritdoc />
    internal override void Flatten(MathSpanWriter writer)
    {
        foreach (var child in children) child.Flatten(writer);
    }
}

/// <summary>A large operator's sign (<c>\sum</c>, <c>\int</c>): the character at the set's large-operator size
/// (<see cref="MathConstants.LargeOperatorScale"/>), larger than the text where the set draws it in the label's face.</summary>
/// <param name="glyph">The sign.</param>
internal sealed class LargeSymbolBox(string glyph) : MathBox
{
    private readonly GlyphsBox _sign = new(glyph);

    /// <inheritdoc />
    internal override MathExtent Lay(MathInk ink, in MathStyle style) => _sign.Lay(ink, style.Scaled(style.Constants.LargeOperatorScale));

    /// <inheritdoc />
    internal override MathLastChar? LastChar(in MathStyle style) => _sign.LastChar(style.Scaled(style.Constants.LargeOperatorScale));

    /// <inheritdoc />
    internal override void Flatten(MathSpanWriter writer) => writer.Glyphs(glyph, FontVariant.Default, false, MathSpanWriter.Constants.LargeOperatorScale);
}

/// <summary>A nucleus whose scripts are its limits, set above and below it (matplotlib's <c>_overunder_symbols</c> and
/// <c>_overunder_functions</c>): <c>\sum</c>, <c>\prod</c>, <c>\lim</c>, <c>\max</c>.</summary>
/// <param name="nucleus">The sign or the function name.</param>
internal sealed class TakesLimitsBox(MathBox nucleus) : MathBox
{
    /// <inheritdoc />
    internal override MathExtent Lay(MathInk ink, in MathStyle style) => nucleus.Lay(ink, style);

    /// <inheritdoc />
    internal override MathLastChar? LastChar(in MathStyle style) => nucleus.LastChar(style);

    /// <inheritdoc />
    internal override MathBox WithScripts(MathBox? superscript, MathBox? subscript) => new LimitsBox(this, superscript, subscript);

    /// <inheritdoc />
    internal override void Flatten(MathSpanWriter writer) => writer.Marked(nucleus, TextSpanKind.LargeOperator);
}

/// <summary>An integral sign (matplotlib's <c>_dropsub_symbols</c>): its scripts stand beside it, by its top and its foot, and the
/// sign leans (<c>_slanted_symbols</c>).</summary>
/// <param name="sign">The sign.</param>
internal sealed class IntegralBox(MathBox sign) : MathBox
{
    /// <inheritdoc />
    internal override MathExtent Lay(MathInk ink, in MathStyle style) => sign.Lay(ink, style);

    /// <inheritdoc />
    internal override MathLastChar? LastChar(in MathStyle style) =>
        sign.LastChar(style) is { } last ? last with { Slanted = true, DropsScripts = true } : null;

    /// <inheritdoc />
    internal override void Flatten(MathSpanWriter writer) => writer.Marked(sign, TextSpanKind.LargeOperator);
}

/// <summary>A letter or a box under an accent: the run of glyphs with the combining mark on its last character, which the face places
/// over it (matplotlib draws the accent's glyph over the letter; a face's own mark placement draws the same mark).</summary>
/// <param name="accented">The run with its mark.</param>
internal sealed class AccentBox(MathBox accented) : MathBox
{
    /// <inheritdoc />
    internal override MathExtent Lay(MathInk ink, in MathStyle style) => accented.Lay(ink, style);

    /// <inheritdoc />
    /// <remarks>matplotlib sets an accent as a vertical list, so a script of an accented letter is placed from its top.</remarks>
    internal override bool IsCharNode => false;

    /// <inheritdoc />
    internal override void Flatten(MathSpanWriter writer) => writer.Marked(accented, TextSpanKind.Accent);
}

/// <summary><c>\phantom{x}</c>: the room of its content and none of its ink.</summary>
/// <param name="content">The content.</param>
internal sealed class PhantomBox(MathBox content) : MathBox
{
    /// <inheritdoc />
    internal override MathExtent Lay(MathInk ink, in MathStyle style)
    {
        var from = ink.Mark;
        var size = content.Lay(ink, style);
        ink.Erase(from);
        return size;
    }

    /// <inheritdoc />
    internal override bool IsCharNode => content.IsCharNode;

    /// <inheritdoc />
    /// <remarks>A phantom draws nothing, so the flat view holds nothing of it.</remarks>
    internal override void Flatten(MathSpanWriter writer) { }
}

/// <summary><c>\llap{x}</c> and <c>\rlap{x}</c>: the content drawn, taking no room; <c>\llap</c> ends it where it stands, <c>\rlap</c>
/// starts it there.</summary>
/// <param name="content">The content.</param>
/// <param name="reach">How much of its width the content stands left of where it is set: all of it for <c>\llap</c>, none for
/// <c>\rlap</c>.</param>
internal sealed class LapBox(MathBox content, double reach) : MathBox
{
    /// <inheritdoc />
    internal override MathExtent Lay(MathInk ink, in MathStyle style)
    {
        var from = ink.Mark;
        var size = content.Lay(ink, style);
        ink.Shift(from, -reach * size.Width, 0);
        return size with { Width = 0 };
    }

    /// <inheritdoc />
    internal override bool IsCharNode => content.IsCharNode;

    /// <inheritdoc />
    internal override void Flatten(MathSpanWriter writer) => content.Flatten(writer);
}
