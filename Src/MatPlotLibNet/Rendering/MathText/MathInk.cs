// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.MathText;

/// <summary>A run of glyphs placed by a layout: its characters, the font it is drawn in, and where its baseline starts, in pixels from
/// the left end of the label's baseline (y downward).</summary>
/// <param name="Text">The characters, as the face codes them.</param>
/// <param name="Font">The font the run is drawn in: its face at its size, in the label's colour (<see cref="SpanFont.Colored"/>), made
/// once when the run is placed, so a draw of the layout makes no font.</param>
/// <param name="X">Where the run starts along the baseline.</param>
/// <param name="Y">How far below the label's baseline the run's baseline stands (negative above it).</param>
internal readonly record struct MathGlyphInk(string Text, Font Font, double X, double Y);


/// <summary>What a label of math is drawn as: the runs of glyphs and the rules (a fraction's line, a radical's bar) its layout placed,
/// relative to the left end of its baseline, and the box it takes. Made by laying out the label's <see cref="MathBox"/> tree; a box lays
/// itself out at the origin and its parent moves its ink into place (<see cref="Shift(MathInkMark, MathInkMark, double, double)"/>). The one layout the verb that draws a label
/// and the seam that measures it both read.</summary>
internal sealed class MathInk
{
    /// <summary>The ink a layout asked only for its box places nothing in: it keeps no run and no rule, so it holds no state.</summary>
    private static readonly MathInk Unkept = new(keeps: false, color: null);

    private readonly List<MathGlyphInk> _glyphs;
    private readonly List<Rect> _rules;

    /// <summary>The label's colour, which every run is drawn in.</summary>
    private readonly Color? _color;

    private MathInk(bool keeps, Color? color)
    {
        _glyphs = keeps ? new(4) : [];
        _rules = [];
        _color = color;
        Keeps = keeps;
    }

    /// <summary>Whether the ink keeps what is placed in it; the ink of a layout asked only for its box keeps nothing.</summary>
    private bool Keeps { get; }

    /// <summary>The runs of glyphs, in the order they were laid out.</summary>
    internal IReadOnlyList<MathGlyphInk> Glyphs => _glyphs;

    /// <summary>The rules, each a rectangle relative to the left end of the baseline (y downward).</summary>
    internal IReadOnlyList<Rect> Rules => _rules;

    /// <summary>The box of the whole label.</summary>
    internal MathExtent Extent { get; private set; }

    /// <summary>Lays out <paramref name="box"/> in <paramref name="style"/>.</summary>
    /// <param name="box">The label's tree.</param>
    /// <param name="style">The style of the label.</param>
    /// <returns>The ink.</returns>
    internal static MathInk Of(MathBox box, in MathStyle style)
    {
        var ink = new MathInk(keeps: true, style.Label.Color);
        ink.Extent = box.Lay(ink, style);
        return ink;
    }

    /// <summary>The box <paramref name="box"/> takes laid out in <paramref name="style"/>, its runs and rules placed nowhere: what a
    /// measure of a label reads, which draws nothing.</summary>
    /// <param name="box">The label's tree.</param>
    /// <param name="style">The style of the label.</param>
    /// <returns>The box.</returns>
    internal static MathExtent ExtentOf(MathBox box, in MathStyle style) => box.Lay(Unkept, style);

    /// <summary>Where the next box's ink will start.</summary>
    internal MathInkMark Mark => new(_glyphs.Count, _rules.Count);

    /// <summary>Places a run of glyphs, to be drawn in its face at its size in the label's colour.</summary>
    internal void AddGlyphs(string text, SpanFont font, double x, double y)
    {
        if (Keeps) _glyphs.Add(new MathGlyphInk(text, font.Colored(_color), x, y));
    }

    /// <summary>Places a rule from <paramref name="x"/> along the baseline, its top <paramref name="top"/> below it.</summary>
    internal void AddRule(double x, double top, double width, double height)
    {
        if (Keeps) _rules.Add(new Rect(x, top, width, height));
    }

    /// <summary>Moves the ink laid out from <paramref name="from"/> to the end by (<paramref name="dx"/>, <paramref name="dy"/>).</summary>
    internal void Shift(MathInkMark from, double dx, double dy) => Shift(from, Mark, dx, dy);

    /// <summary>Moves the ink laid out from <paramref name="from"/> up to <paramref name="to"/> by (<paramref name="dx"/>,
    /// <paramref name="dy"/>): a box laid out at the origin, moved into its place.</summary>
    internal void Shift(MathInkMark from, MathInkMark to, double dx, double dy)
    {
        if (dx == 0 && dy == 0) return;
        foreach (ref var glyph in CollectionsMarshal.AsSpan(_glyphs)[from.Glyphs..to.Glyphs])
            glyph = glyph with { X = glyph.X + dx, Y = glyph.Y + dy };
        foreach (ref var rule in CollectionsMarshal.AsSpan(_rules)[from.Rules..to.Rules])
            rule = rule with { X = rule.X + dx, Y = rule.Y + dy };
    }

    /// <summary>Takes away the ink laid out from <paramref name="from"/> on: a phantom keeps its room and draws nothing.</summary>
    internal void Erase(MathInkMark from)
    {
        _glyphs.RemoveRange(from.Glyphs, _glyphs.Count - from.Glyphs);
        _rules.RemoveRange(from.Rules, _rules.Count - from.Rules);
    }

    /// <summary>The one run of a label laid out as a single run on its own baseline from its start, with no rule; else
    /// <see langword="null"/>. A surface places such a label by its own measure of the run, as it places a plain text.</summary>
    internal MathGlyphInk? Single => _glyphs.Count == 1 && _rules.Count == 0 && _glyphs[0] is { X: 0, Y: 0 } run ? run : null;
}
