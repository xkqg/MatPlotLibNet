// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Skia.Tests;

/// <summary>The one layout of math (<see cref="MathBox"/>, on the font set's <see cref="MathConstants"/>) places what matplotlib 3.11.2's
/// mathtext places, measured by the ink of the Skia package's fonts as matplotlib measures a glyph. Each row is matplotlib's own box and
/// the place of a glyph or a rule in it, printed with python at 12 points and 100 dpi (16.67 px):
/// <code>
/// p = matplotlib._mathtext.Parser(); box = p.parse(label, DejaVuSansFonts(FontProperties(size=12), get_hinting_flag()), 12, 100)
/// out = matplotlib._mathtext.ship(box)   # out.glyphs: (ox, oy, info); out.rects: (x, y, w, h); y measured down from the top
/// </code>
/// (the classic style and <c>BakomaFonts</c> for 'cm'). A place is written relative to the left end of the baseline, y downward.</summary>
public sealed class MathLayoutParityTests
{
    /// <summary>12 points at 100 dpi, in pixels.</summary>
    private const double Size = 12 * 100 / 72.0;

    /// <summary>The tolerance of a place or a size: a pixel, the step of the hinting both sides round a glyph's ink to.</summary>
    private const double Pixel = 1.0;

    private static MathInk Laid(string set, string label) => MathInk.Of(TextLabel.Of(label).Parsed(null).Tree!,
        new MathStyle(MathFontSet.Named(set), new Font { Family = "DejaVu Sans", Size = Size }, MathMeasure.ByInk(new SkiaFontMetrics())));

    /// <summary>The box of each label: its width, height and depth.</summary>
    [Theory]
    [InlineData("dejavusans", "$x^2$", 18.475, 15.283, 0.000)]
    [InlineData("dejavusans", "$x_i$", 13.564, 9.000, 2.500)]
    [InlineData("dejavusans", "$x_i^2$", 18.475, 15.692, 5.975)]
    [InlineData("dejavusans", @"$\frac{a}{b}$", 9.491, 12.029, 6.496)]
    [InlineData("dejavusans", @"$\sqrt{x}$", 24.656, 15.000, 0.000)]
    [InlineData("dejavusans", @"$\left(\frac{a}{b}\right)$", 23.240, 12.500, 6.500)]
    [InlineData("dejavusans", @"$\sqrt{\frac{a}{b}}$", 27.120, 18.500, 6.500)]
    [InlineData("dejavusans", @"$\left[x\right]$", 22.880, 13.000, 2.000)]
    [InlineData("dejavusans", @"$\sum_{i=0}^{n}$", 24.993, 27.425, 17.225)]
    [InlineData("dejavusans", "$x^{a^b}$", 24.216, 18.072, 0.000)]
    [InlineData("dejavusans", "$a+b$", 41.265, 13.000, 0.000)]
    [InlineData("dejavusans", @"$\int_a^b$", 22.011, 27.267, 6.833)]
    [InlineData("cm", "$x^2$", 18.315, 15.282, 0.000)]
    [InlineData("cm", "$x_i$", 14.487, 8.000, 2.659)]
    [InlineData("cm", @"$\frac{a}{b}$", 8.243, 11.329, 5.796)]
    [InlineData("cm", "$a+b$", 35.574, 12.000, 2.000)]
    [InlineData("cm", @"$\left(\frac{a}{b}\right)$", 19.540, 10.630, 5.100)]
    [InlineData("cm", @"$\sqrt{x}$", 23.350, 14.000, 0.000)]
    [InlineData("cm", @"$\sqrt{\frac{a}{b}}$", 29.020, 16.900, 5.100)]
    [InlineData("cm", @"$\left[x\right]$", 13.310, 8.000, 0.000)]
    [InlineData("cm", @"$\sum$", 24.072, 17.056, 5.944)]
    [InlineData("cm", @"$\prod$", 21.288, 17.056, 5.944)]
    public void TheBoxOfALabel_IsMatplotlibs(string set, string label, double width, double height, double depth)
    {
        var box = Laid(set, label).Extent;

        Assert.InRange(box.Width, width - Pixel, width + Pixel);
        Assert.InRange(box.Height, height - Pixel, height + Pixel);
        Assert.InRange(box.Depth, depth - Pixel, depth + Pixel);
    }

    /// <summary>Where a glyph of each label stands: its start along the baseline and the height of its baseline.</summary>
    [Theory]
    [InlineData("dejavusans", "$x^2$", "2", 10.595, -6.883)]
    [InlineData("dejavusans", "$x_i$", "i", 9.866, 2.500)]
    [InlineData("dejavusans", "$x_i^2$", "2", 10.595, -7.292)]
    [InlineData("dejavusans", "$x_i^2$", "i", 9.866, 5.975)]
    [InlineData("dejavusans", @"$\frac{a}{b}$", "a", 1.042, -5.729)]
    [InlineData("dejavusans", @"$\frac{a}{b}$", "b", 1.042, 6.496)]
    [InlineData("dejavusans", @"$\sqrt{x}$", "x", 12.707, 0.042)]
    [InlineData("dejavusans", "$x^{a^b}$", "b", 18.256, -11.702)]
    [InlineData("dejavusans", @"$\int_a^b$", "a", 11.943, 6.833)]
    [InlineData("dejavusans", @"$\int_a^b$", "b", 14.603, -18.167)]
    [InlineData("dejavusans", @"$\sum_{i=0}^{n}$", "n", 9.000, -21.125)]
    [InlineData("dejavusans", @"$\sum_{i=0}^{n}$", "∑", 4.000, 0.000)]
    [InlineData("dejavusans", "$a+b$", "+", 13.465, 0.000)]
    [InlineData("dejavusans", "$a+b$", "b", 30.682, 0.000)]
    [InlineData("cm", "$x^2$", "2", 11.526, -6.882)]
    [InlineData("cm", "$x_i$", "i", 9.516, 2.659)]
    [InlineData("cm", @"$\frac{a}{b}$", "b", 2.042, 5.796)]
    [InlineData("cm", "$a+b$", "+", 12.133, 0.000)]
    [InlineData("cm", "$a+b$", "b", 28.418, 0.000)]
    // a glyph of cmex10 is drawn above the baseline its box stands on, by matplotlib's Cmex10 offset: python's oy less the box height less info.offset
    [InlineData("cm", @"$\sum$", "X", 0.000, -17.056)]
    [InlineData("cm", @"$\prod$", "Y", 0.000, -17.056)]
    public void AGlyphOfALabel_StandsWhereMatplotlibSetsIt(string set, string label, string glyph, double x, double y)
    {
        var run = Laid(set, label).Glyphs.Single(g => g.Text == glyph);

        Assert.InRange(run.X, x - Pixel, x + Pixel);
        Assert.InRange(run.Y, y - Pixel, y + Pixel);
    }

    /// <summary>The size a delimiter or a radical sign is set in: the face matplotlib's AutoHeightChar takes it from, the size that makes it
    /// as tall as what it stands beside (the font size in points printed with python is 72 / 100 of the pixels here), and where it is drawn (a glyph of cmex10 is drawn above the baseline its box stands on by matplotlib's Cmex10 offset: python's oy less the height of the box less info.offset).</summary>
    [Theory]
    [InlineData("dejavusans", @"$\left(\frac{a}{b}\right)$", "(", "STIXSizeOneSym", 14.708, 0.000, 3.500)]
    [InlineData("dejavusans", @"$\left(\frac{a}{b}\right)$", ")", "STIXSizeOneSym", 14.708, 16.370, 3.500)]
    [InlineData("dejavusans", @"$\sqrt{\frac{a}{b}}$", "\u221A", "STIXSizeOneSym", 12.764, 0.000, 2.500)]
    [InlineData("dejavusans", @"$\left[x\right]$", "[", "DejaVu Sans", 16.667, 0.000, 0.000)]
    [InlineData("cm", @"$\left(\frac{a}{b}\right)$", "(", "cmr10", 14.556, 0.000, 1.090)]
    [InlineData("cm", @"$\sqrt{x}$", "p", "cmex10", 9.694, 0.000, -12.000)]
    [InlineData("cm", @"$\sqrt{\frac{a}{b}}$", "p", "cmex10", 16.611, 0.000, -14.904)]
    public void ADelimiterOfALabel_IsSetInTheSizeMatplotlibChooses(string set, string label, string glyph, string family, double size, double x, double y)
    {
        var run = Laid(set, label).Glyphs.First(g => g.Text == glyph);

        Assert.Equal(family, run.Font.Family);
        Assert.InRange(run.Font.Size, size - 1.5, size + 1.5);
        Assert.InRange(run.X, x - Pixel, x + Pixel);
        Assert.InRange(run.Y, y - Pixel, y + Pixel);
    }

    /// <summary>The rule of a fraction and the bar of a radical: where it starts, where its top stands, how wide and how thick it is.</summary>
    [Theory]
    [InlineData("dejavusans", @"$\frac{a}{b}$", 1.042, -4.687, 7.408, 1.042)]
    [InlineData("dejavusans", @"$\sqrt{x}$", 10.623, -15.000, 14.033, 1.042)]
    [InlineData("cm", @"$\frac{a}{b}$", 1.042, -4.687, 6.160, 1.042)]
    public void TheRuleOfALabel_IsMatplotlibs(string set, string label, double x, double top, double width, double height)
    {
        var rule = Assert.Single(Laid(set, label).Rules);

        Assert.InRange(rule.X, x - Pixel, x + Pixel);
        Assert.InRange(rule.Y, top - Pixel, top + Pixel);
        Assert.InRange(rule.Width, width - Pixel, width + Pixel);
        Assert.Equal(height, rule.Height, 0.001);
    }
}
