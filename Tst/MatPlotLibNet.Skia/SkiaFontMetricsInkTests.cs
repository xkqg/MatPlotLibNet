// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Rendering.TextMeasurement;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Skia.Tests;

/// <summary>The Skia metrics read the glyphs of the bundled DejaVu Sans as matplotlib 3.11.2's Agg backend sets them (text.hinting
/// 'default'), and a PNG surface told the matplotlib v2 looks measures by them. Every number is matplotlib's, printed with python at
/// 100 dpi: <c>FT2Font.set_text(s, 0, flags=LoadFlags.DEFAULT)</c>, <c>get_width_height()</c> / 64 and <c>get_descent()</c> / 64 for
/// the glyphs, <c>fig.text(..., fontsize=pt).get_window_extent(renderer)</c> for a box, and
/// <c>renderer.get_text_width_height_descent(s, FontProperties(size=pt), True)</c> for math.</summary>
public sealed class SkiaFontMetricsInkTests
{
    private static readonly Font Ten = new() { Family = "DejaVu Sans", Size = 10 * 100.0 / 72 };
    private static readonly Font Twelve = new() { Family = "DejaVu Sans", Size = 12 * 100.0 / 72 };

    /// <summary>A glyph's advance is whole pixels at the size rounded to whole pixels (12 pt is 16.67 px, set at 17), the kerning
    /// between two glyphs kept ("AV" at 10 pt: 20 less 0.89; the hist title's pairs, read by HarfBuzz from the font's GPOS table,
    /// kern it 0.22 px less than FreeType does); its ink stands on whole pixels, an overshoot on its zone ("0" rises 12 at
    /// 12 pt as "1" does, not the 12.6 its outline reaches).</summary>
    [Theory]
    [InlineData("lp", 10, 13.0, 11, 3)]
    [InlineData("lp", 12, 16.0, 13, 4)]
    [InlineData("0.5", 10, 22.0, 10, 0)]
    [InlineData("0.5", 12, 27.0, 12, 0)]
    [InlineData("AV", 10, 19.109375, 10, 0)]
    [InlineData("AV", 12, 22.9375, 12, 0)]
    [InlineData("value", 12, 46.0, 13, 0)]
    [InlineData("Amplitude", 10, 73.0, 11, 3)]
    [InlineData("Amplitude", 12, 89.0, 13, 4)]
    [InlineData("Histogram — 1000 normal samples, 30 bins", 12, 373.703125, 13, 4, 0.25)]
    public void TheGlyphs_StandOnWholePixels_AsAggSetsThem(string text, int points, double advance, double rise, double drop,
        double kerning = 0.06)
    {
        var metrics = new SkiaFontMetrics();
        var font = points == 10 ? Ten : Twelve;

        var ink = ((IGlyphInkMetrics)metrics).InkOf(text, font, font.Size);

        Assert.Equal(advance, ink.PixelAdvance, kerning);
        Assert.Equal(rise, ink.Rise);
        Assert.Equal(drop, ink.Drop);
        Assert.Equal(metrics.Shaped.Of(text, font).Width, ink.Advance, 4);
    }

    /// <summary>A glyph the face gives no instructions of its own (a face hinted by its own instructions, as DejaVu Sans is) keeps
    /// its outline: FreeType's grid fit stands the top of its ink on the pixel above it and the foot on the pixel below
    /// (<c>ft_glyphslot_grid_fit_metrics</c>), where a glyph the face hints is rounded to the nearest. At 12 pt (17 ppem) DejaVu Sans
    /// Display's integral reaches 18.195 px up and 5.15 px down, which matplotlib reads as 19 and 6
    /// (<c>DejaVuSansFonts.get_metrics('it', 'it', r'\int', 12, 100).iceberg</c> 19); its sum 17.03 up, matplotlib 18; DejaVu Sans' partial
    /// sign 11.4 up and 0.2 down, matplotlib 12 and 1.</summary>
    [Theory]
    [InlineData("∫", "DejaVu Sans Display", 19, 6)]
    [InlineData("∑", "DejaVu Sans Display", 18, 5)]
    [InlineData("∂", "DejaVu Sans", 12, 1)]
    public void AGlyphTheFaceGivesNoInstructions_StandsOnThePixelsOutsideItsInk(string text, string family, double rise, double drop)
    {
        var font = new Font { Family = family, Size = 12 * 100.0 / 72 };

        var ink = ((IGlyphInkMetrics)new SkiaFontMetrics()).InkOf(text, font, font.Size);

        Assert.Equal(rise, ink.Rise);
        Assert.Equal(drop, ink.Drop);
    }

    /// <summary>One line of a font is measured by a digit, the line a tick label stands in: where a digit's ink rises above the font's
    /// typographic ascender (matplotlib rounds the ink of "0" up to whole pixels: 7 at 8.5 px, over the band's 6.46) the line is as tall as
    /// the digit, as it is for the label's own digits, and everywhere else it is the band.</summary>
    [Theory]
    [InlineData(8.5, 7)]
    [InlineData(10 * 100.0 / 72, 10)]
    public void ALineOfAFont_IsTheLineOfADigit_TheBandWhereTheDigitStaysUnderIt(double size, double rise)
    {
        using var bitmap = new SkiaSharp.SKBitmap(10, 10);
        using var canvas = new SkiaSharp.SKCanvas(bitmap);
        IRenderContext ctx = new SkiaRenderContext(canvas);
        ctx.UseLooksOf(Theme.MatplotlibV2);
        var font = new Font { Family = "DejaVu Sans", Size = size };
        var band = ((IGlyphInkMetrics)new SkiaFontMetrics()).BandOf(font);

        var line = ctx.LineExtentOf(font);

        Assert.Equal(Math.Max(rise, band.Ascender), line.Ascent, 6);
        Assert.Equal(Math.Max(rise, band.Ascender) + band.Descender, line.Height, 6);
        Assert.Equal(ctx.ExtentOf(TextLabel.Plain("0"), font), line);
    }

    /// <summary>The OS/2 <c>sTypoAscender</c> 1556 and <c>sTypoDescender</c> -492 of DejaVu Sans' 2048 units, at the font's size.</summary>
    [Fact]
    public void TheBand_IsTheFontsTypographicAscenderAndDescender()
    {
        var band = ((IGlyphInkMetrics)new SkiaFontMetrics()).BandOf(Twelve);

        Assert.Equal(12.66276, band.Ascender, 5);
        Assert.Equal(4.003906, band.Descender, 5);
    }

    [Fact]
    public void AnEmptyText_HasNoAdvanceAndNoInk_ButItsFontsBand()
    {
        IGlyphInkMetrics metrics = new SkiaFontMetrics();

        Assert.Equal(new GlyphInk(0, 0, 0, 0, metrics.BandOf(Ten)), metrics.InkOf("", Ten, Ten.Size));
        Assert.Equal(metrics.BandOf(Ten), metrics.InkOf("lp", Ten, Ten.Size).Band);
    }

    /// <summary>The boxes <c>Text.get_window_extent</c> gives under the default style.</summary>
    [Theory]
    [InlineData("0.5", 10, 13.8889)]
    [InlineData("Time (s)", 10, 14.3366)]
    [InlineData("Peak", 12, 17.0039)]
    [InlineData("0.5", 12, 16.6667)]
    [InlineData("Histogram — 1000 normal samples, 30 bins", 12, 17.0039)]
    public void APngSurfaceToldTheMatplotlibLooks_MeasuresTheBoxMatplotlibDoes(string text, int points, double height)
    {
        using var bitmap = new SkiaSharp.SKBitmap(10, 10);
        using var canvas = new SkiaSharp.SKCanvas(bitmap);
        IRenderContext ctx = new SkiaRenderContext(canvas);
        ctx.UseLooksOf(Theme.MatplotlibV2);

        Assert.Equal(height, ctx.ExtentOf(TextLabel.Plain(text), points == 10 ? Ten : Twelve).Height, 4);
    }

    /// <summary>A label measured again on a later frame reads the glyphs kept with its shaping, and a math label the tree and the layout
    /// its figure kept (the surface told the figure's texts, as every stage of a draw tells them): the measure allocates nothing.</summary>
    [Fact]
    public void AMeasureUnderTheMatplotlibLooks_AllocatesNothing_OnceTheTextIsShaped()
    {
        using var bitmap = new SkiaSharp.SKBitmap(10, 10);
        using var canvas = new SkiaSharp.SKCanvas(bitmap);
        IRenderContext ctx = new SkiaRenderContext(canvas);
        var texts = new KeptMathTexts();
        texts.StartDraw();
        ctx.UseStageOf(texts, Theme.MatplotlibV2);
        var plain = TextLabel.Plain("Amplitude");
        var math = TextLabel.Of("$x^2$ decay");
        double sum = ctx.ExtentOf(plain, Ten).Height + ctx.ExtentOf(math, Twelve).Height;
        for (int i = 0; i < 100; i++)
            sum += ctx.ExtentOf(plain, Ten).Height + ctx.ExtentOf(math, Twelve).Height;

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 100; i++)
            sum += ctx.ExtentOf(plain, Ten).Height + ctx.ExtentOf(math, Twelve).Height;
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.True(sum > 0);
        Assert.Equal(0, allocated);
    }

    /// <summary>Math by its ink, rounded up: <c>$x$</c> at 10 pt is (9, 8, 0), its box the font's 13.8889; <c>$\sigma$ (normalised)</c> is
    /// 102 wide and <c>$\alpha$ decay</c> 56.</summary>
    [Fact]
    public void AMathText_IsMeasuredByItsInk()
    {
        using var bitmap = new SkiaSharp.SKBitmap(10, 10);
        using var canvas = new SkiaSharp.SKCanvas(bitmap);
        IRenderContext ctx = new SkiaRenderContext(canvas);
        ctx.UseLooksOf(Theme.MatplotlibV2);

        var x = ctx.ExtentOf(TextLabel.Of("$x$"), Ten);
        Assert.Equal(9, x.Width);
        Assert.Equal(13.8889, x.Height, 4);
        Assert.Equal(102, ctx.ExtentOf(TextLabel.Of(@"$\sigma$ (normalised)"), Ten).Width);
        Assert.Equal(56, ctx.ExtentOf(TextLabel.Of(@"$\alpha$ decay"), Ten).Width);
    }
}
