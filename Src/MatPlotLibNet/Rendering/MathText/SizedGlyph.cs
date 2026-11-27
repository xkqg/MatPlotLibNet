// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.MathText;

/// <summary>A character grown to the height of what it stands beside (matplotlib's <c>AutoHeightChar</c>): a delimiter after
/// <c>\left</c>, <c>\middle</c> or <c>\right</c>, the sign of a radical. A set with sized faces (<see cref="MathFontSet.SizedAlternatives"/>)
/// is read as matplotlib reads it: the first size tall enough (within a fifth of the x-height of what is asked) is taken, the last when
/// none is, and set at the size that makes its height and depth exactly the height and depth asked, its foot at the depth asked. A set with
/// none grows the character's own glyph: it stays at the text's size when it is tall enough, else is set larger until it is.</summary>
internal static class SizedGlyph
{
    /// <summary>Lays <paramref name="glyph"/> out at the origin, as tall as <paramref name="height"/> above the baseline and
    /// <paramref name="depth"/> below it.</summary>
    /// <returns>The room it takes.</returns>
    internal static MathExtent Lay(MathInk ink, in MathStyle style, string glyph, double height, double depth)
    {
        var sizes = style.Set.SizedAlternatives(glyph);
        return sizes.Glyphs.Length == 0 ? Grown(ink, style, glyph, height, depth) : Chosen(ink, style, sizes, height, depth);
    }

    /// <summary>The size of <paramref name="sizes"/> matplotlib takes for a delimiter <paramref name="height"/> above the baseline and
    /// <paramref name="depth"/> below it: the first size tall enough, the last when none is; set at the size that makes it exactly that tall
    /// unless it is the first of a set that keeps its own size, whose box is the glyph's.</summary>
    private static MathExtent Chosen(MathInk ink, in MathStyle style, MathSizes sizes, double height, double depth)
    {
        double target = height + depth, least = LeastFor(target, style);
        int at = 0;
        var natural = default(MeasuredRun);
        for (; at < sizes.Glyphs.Length; at++)
        {
            natural = style.Run(sizes.Glyphs[at].Face, TextOf(sizes.Glyphs[at]));
            if (natural.Extent.Height + natural.Extent.Depth >= least || at == sizes.Glyphs.Length - 1) break;
        }

        var chosen = sizes.Glyphs[at];
        string text = TextOf(chosen);
        if (at == 0 && sizes.FirstIsNatural)
        {
            ink.AddGlyphs(text, natural.Font, 0, -natural.Lift);
            return natural.Extent;
        }

        double own = natural.Extent.Height + natural.Extent.Depth;
        if (own <= 0) return natural.Extent;

        var grown = style.Scaled(target / own).Run(chosen.Face, text);
        double shift = depth - grown.Extent.Depth;
        ink.AddGlyphs(text, grown.Font, 0, shift - grown.Lift);
        return new MathExtent(grown.Extent.Width, grown.Extent.Height - shift, grown.Extent.Depth + shift);
    }

    /// <summary>The least height and depth a glyph may have to stand for a delimiter <paramref name="target"/> tall: short of it by the
    /// set's <see cref="MathConstants.DelimiterShortfall"/> of an x-height.</summary>
    private static double LeastFor(double target, in MathStyle style) => target - style.Constants.DelimiterShortfall * style.XHeight;

    /// <summary>The character of a size as its face codes it (the generated tables hold only sizes of the Basic Multilingual Plane).</summary>
    private static string TextOf(MathGlyph glyph) => GlyphsBox.TextOf((char)glyph.Code);

    /// <summary>The character's own glyph, grown when it is shorter than what it stands beside.</summary>
    private static MathExtent Grown(MathInk ink, in MathStyle style, string glyph, double height, double depth)
    {
        var box = new GlyphsBox(glyph);
        var scratch = ink.Mark;
        var natural = box.Lay(ink, style);
        double target = height + depth, own = natural.Height + natural.Depth;
        if (own <= 0 || own >= LeastFor(target, style)) return natural;

        ink.Erase(scratch);
        double factor = target / own;
        var grown = box.Lay(ink, style.Scaled(factor));
        double shift = depth - grown.Depth;
        ink.Shift(scratch, 0, shift);
        return new MathExtent(grown.Width, grown.Height - shift, grown.Depth + shift);
    }
}
