// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.TextMeasurement;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests;

/// <summary>The ink of the glyphs a test hands a math layout or installs as the text measure: the ink of a text and the band of a font as
/// <paramref name="ink"/> and <paramref name="band"/> give them, and the box <paramref name="measure"/> gives (an empty one, left out).</summary>
/// <param name="ink">The ink of a text in a face at a size.</param>
/// <param name="band">The typographic band of a font.</param>
/// <param name="measure">The box of a text in a font; left out, an empty box.</param>
internal sealed class TestGlyphInk(Func<string, Font, double, GlyphInk> ink, Func<Font, FontBand> band, Func<string, Font, Size>? measure = null)
    : TestFontMetrics(measure ?? ((_, _) => Size.Empty)), IGlyphInkMetrics
{
    /// <summary>Glyphs as boxes easy to sum: each character half the size wide, rising 0.7 of it (an empty text not at all), dropping 0.2
    /// of it when a descending letter is in the text, in a band one size high over a quarter of one.</summary>
    public static TestGlyphInk Boxes => new((text, face, size) =>
    {
        double advance = text.Length * size / 2;
        return new GlyphInk(advance, advance, text.Length == 0 ? 0 : 0.7 * size, text.IndexOfAny(['g', 'y', 'p', 'j', 'q']) >= 0 ? 0.2 * size : 0,
            BoxBand(face with { Size = size }));
    }, BoxBand);

    public GlyphInk InkOf(string text, Font face, double size) => ink(text, face, size);
    public FontBand BandOf(Font font) => band(font);

    /// <summary>The band of <see cref="Boxes"/>: one size high over a quarter of one.</summary>
    /// <param name="font">The font.</param>
    /// <returns>The band.</returns>
    internal static FontBand BoxBand(Font font) => new(font.Size, 0.25 * font.Size);
}
