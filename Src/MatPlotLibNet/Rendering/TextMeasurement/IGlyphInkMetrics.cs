// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.TextMeasurement;

/// <summary>Font metrics that read the glyphs of a font, so a text can be measured as matplotlib's Agg backend measures it
/// (<see cref="TextLook.MeasuresLikeAgg"/>, the one seam <see cref="TextExtentExtensions"/>). The Skia package's metrics read them; the
/// core's own width table has no glyphs, and metrics without them measure every text by its line. It lives here and not on
/// <see cref="IFontMetrics"/> so that an application can still implement the public interface.</summary>
internal interface IGlyphInkMetrics
{
    /// <summary>How the glyphs of <paramref name="text"/> stand in the face of <paramref name="face"/> at <paramref name="size"/>.</summary>
    /// <param name="text">The text; an empty one has no advance and no ink, and the face's band.</param>
    /// <param name="face">The font whose family, weight and slant the text is set in.</param>
    /// <param name="size">The size it is set at, in pixels (a span of rich text has its own).</param>
    /// <returns>The advance of the text and the ink of its glyphs.</returns>
    GlyphInk InkOf(string text, Font face, double size);

    /// <summary>The typographic band of <paramref name="font"/>'s face at its size.</summary>
    /// <param name="font">The font.</param>
    /// <returns>The ascender and the descender of the face.</returns>
    FontBand BandOf(Font font);
}
