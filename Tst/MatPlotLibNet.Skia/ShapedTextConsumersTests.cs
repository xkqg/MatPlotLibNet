// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;
using SkiaSharp;

namespace MatPlotLibNet.Skia.Tests;

/// <summary>The three places the Skia package turns text into numbers or ink all read the ONE shaped text: the
/// width the layout measures is the width the glyph outlines span and the width the canvas paints. These facts
/// pin that identity, and that Hebrew is painted reversed.</summary>
public sealed class ShapedTextConsumersTests
{
    private static readonly Font Font13 = new() { Family = "DejaVu Sans", Size = 13 };

    [Fact]
    public void FontMetrics_MeasureTheShapedWidth()
    {
        var size = new SkiaFontMetrics().Measure("Ta", Font13);

        Assert.Equal(13.762, size.Width, 0.01);
        Assert.Equal(13 * 1.2, size.Height, 0.001);
    }

    [Fact]
    public void MeasureAdvance_IsTheShapedWidth() =>
        Assert.Equal(13.762, SkiaGlyphPathProvider.MeasureAdvance("Ta", Font13), 0.01);

    [Fact]
    public void GlyphPaths_UseTheJoinedArabicForms()
    {
        var provider = new SkiaGlyphPathProvider();

        string? joined = provider.GetPathData("درجة", Font13);

        Assert.NotNull(joined);
        Assert.NotEmpty(joined);
        // Isolated forms drawn one after the other span 44.4 px; the joined word spans 27.1 px of advance, and
        // its ink cannot be wider than that.
        using var path = SKPath.ParseSvgPathData(joined);
        Assert.True(path.Bounds.Width < 30, $"ink width {path.Bounds.Width}");
    }

    [Fact]
    public void GlyphPaths_OfKernedLatin_AreNarrowerThanTheLettersSideBySide()
    {
        var provider = new SkiaGlyphPathProvider();

        using var pair = SKPath.ParseSvgPathData(provider.GetPathData("Ta", Font13)!);
        using var t = SKPath.ParseSvgPathData(provider.GetPathData("T", Font13)!);
        using var a = SKPath.ParseSvgPathData(provider.GetPathData("a", Font13)!);

        Assert.True(pair.Bounds.Width < t.Bounds.Width + a.Bounds.Width);
    }

    [Fact]
    public void GlyphPaths_ForAnEmptyString_AreNull() =>
        Assert.Null(new SkiaGlyphPathProvider().GetPathData("", Font13));

    [Fact]
    public void Canvas_PaintsHebrewReversed()
    {
        // The rightmost letter in logical order (alef, the first) must be painted on the RIGHT: the leftmost
        // columns of "אב" hold bet, and they equal a render of bet alone at the same origin.
        using var pair = Render("אב");
        using var bet = Render("ב");
        using var alef = Render("א");

        int betWidth = InkRight(bet);
        Assert.True(betWidth > 0);
        for (int x = 0; x < betWidth; x++)
        {
            for (int y = 0; y < pair.Height; y++)
            {
                Assert.Equal(bet.GetPixel(x, y), pair.GetPixel(x, y));
            }
        }

        Assert.True(InkRight(pair) > InkRight(bet));
        Assert.True(InkRight(pair) > InkRight(alef));
    }

    [Fact]
    public void Canvas_PaintsRichTextWithoutThrowing()
    {
        using var bitmap = new SKBitmap(200, 60);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.White);
        var ctx = new SkiaRenderContext(canvas);

        ctx.DrawRichText(Rendering.MathText.MathTextParser.Parse("x$^{2}$ درجة"), new Point(10, 40), Font13, TextAlignment.Left);

        Assert.True(InkRight(bitmap) > 0);
    }

    [Fact]
    public void MeasureAdvance_OfAnEmptyString_IsZero() =>
        Assert.Equal(0, SkiaGlyphPathProvider.MeasureAdvance("", Font13));

    [Fact]
    public void GlyphPaths_OfSpacesOnly_AreEmpty() =>
        Assert.Equal(string.Empty, new SkiaGlyphPathProvider().GetPathData("   ", Font13));

    [Fact]
    public void Canvas_PaintsNothingForAnEmptyString()
    {
        using var bitmap = Render("");

        Assert.Equal(0, InkRight(bitmap));
    }

    [Fact]
    public void Canvas_PaintsNothingForAnEmptyRichTextSpan()
    {
        using var bitmap = new SKBitmap(120, 40);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.White);
        var empty = new Rendering.MathText.RichText([new Rendering.MathText.TextSpan("")]);

        new SkiaRenderContext(canvas).DrawRichText(empty, new Point(2, 30), Font13, TextAlignment.Left);

        Assert.Equal(0, InkRight(bitmap));
    }

    [Fact]
    public void Canvas_PaintsRichTextInTheFontColour()
    {
        using var bitmap = new SKBitmap(200, 60);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.White);
        var red = new Font { Family = "DejaVu Sans", Size = 26, Color = Colors.Red };

        new SkiaRenderContext(canvas).DrawRichText(Rendering.MathText.MathTextParser.Parse("x$^{2}$"), new Point(10, 45), red, TextAlignment.Left);

        Assert.Contains(SKColors.Red, Pixels(bitmap));
    }

    private static IEnumerable<SKColor> Pixels(SKBitmap bitmap)
    {
        for (int x = 0; x < bitmap.Width; x++)
        {
            for (int y = 0; y < bitmap.Height; y++)
            {
                yield return bitmap.GetPixel(x, y);
            }
        }
    }

    private static SKBitmap Render(string text)
    {
        var bitmap = new SKBitmap(120, 40);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.White);
        new SkiaRenderContext(canvas).DrawText(text, new Point(2, 30), Font13, TextAlignment.Left);
        return bitmap;
    }

    /// <summary>The first column from the right that carries ink, or 0 when nothing was painted.</summary>
    private static int InkRight(SKBitmap bitmap)
    {
        for (int x = bitmap.Width - 1; x >= 0; x--)
        {
            for (int y = 0; y < bitmap.Height; y++)
            {
                if (bitmap.GetPixel(x, y) != SKColors.White)
                {
                    return x + 1;
                }
            }
        }

        return 0;
    }
}
