// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;
using SkiaSharp;

namespace MatPlotLibNet.Skia.Tests;

/// <summary>What every drawing surface of the library does with a call, whatever it draws on, held for the PNG and PDF surface: the
/// rules are the core's (an opacity is held between 0 and 1, a turn is counter-clockwise, a font is bold when its weight is bold), and
/// this surface holds only the conversion of each to its canvas. The SVG surface is held by the same-named class of the core suite, the
/// MAUI surface by the one of its suite.</summary>
public class RenderContextConformanceTests
{
    [Theory]
    [InlineData(0f, 0f, 10f, 20f, 0f, 0f, 10f, 20f)]
    [InlineData(5.5f, 7.25f, 100f, 40f, 5.5f, 7.25f, 105.5f, 47.25f)]
    public void ARect_BecomesTheRectBetweenItsCorners(float x, float y, float width, float height, float left, float top, float right, float bottom)
    {
        var rect = new SKRect(left, top, right, bottom);

        Assert.Equal(rect, SkiaRenderContext.ToSkRect(new Rect(x, y, width, height)));
    }

    [Fact]
    public void ARectOfNegativeSize_KeepsItsCorners_AsTheCanvasTakesThem()
    {
        // No normalising: a clip or an image drawn into a rect with its right edge left of its left edge is the canvas' to refuse.
        var rect = SkiaRenderContext.ToSkRect(new Rect(10, 10, -4, -2));

        Assert.Equal(new SKRect(10, 10, 6, 8), rect);
    }

    [Theory]
    [InlineData(JoinStyle.Round, SKStrokeJoin.Round)]
    [InlineData(JoinStyle.Bevel, SKStrokeJoin.Bevel)]
    [InlineData(JoinStyle.Miter, SKStrokeJoin.Miter)]
    public void AJoin_IsTheJoinOfTheCanvas(JoinStyle join, SKStrokeJoin expected)
    {
        Assert.Equal(expected, SkiaRenderContext.ToSkia(join));
    }

    [Theory]
    [InlineData(-0.5, 0.0)]
    [InlineData(0.25, 0.25)]
    [InlineData(1.5, 1.0)]
    public void ASurface_HoldsAnOpacityBetweenZeroAndOne(double asked, double held)
    {
        using var bitmap = new SKBitmap(10, 10);
        using var canvas = new SKCanvas(bitmap);
        var ctx = new SkiaRenderContext(canvas);

        ctx.SetOpacity(asked);

        Assert.Equal(held, ((IOpacityRenderContext)ctx).Opacity);
    }

    private static int InkedPixels(SKBitmap bitmap)
    {
        int inked = 0;
        for (int y = 0; y < bitmap.Height; y++)
            for (int x = 0; x < bitmap.Width; x++)
                if (bitmap.GetPixel(x, y).Alpha > 0) inked++;
        return inked;
    }

    /// <summary>A polygon is drawn from the points it is given, whatever their number: the outline of two points is the line between
    /// them, as the SVG surface writes it and matplotlib draws it (a single-sample area is outlined so). The PNG surface returned before
    /// it drew it.</summary>
    [Fact]
    public void APolygonOfTwoPoints_IsDrawnAsTheOutlineItIs()
    {
        using var bitmap = new SKBitmap(40, 40);
        using var canvas = new SKCanvas(bitmap);
        var ctx = new SkiaRenderContext(canvas);

        ctx.DrawPolygon([new Point(20, 5), new Point(20, 35)], new ShapeStyle(null, Colors.Black, 2));

        Assert.True(InkedPixels(bitmap) > 30);
        Assert.NotEqual(0, bitmap.GetPixel(20, 20).Alpha);
    }

    [Fact]
    public void ALineOfOnePoint_DrawsNothing()
    {
        using var bitmap = new SKBitmap(40, 40);
        using var canvas = new SKCanvas(bitmap);
        var ctx = new SkiaRenderContext(canvas);

        ctx.DrawLines([new Point(20, 20)], StrokeStyle.Solid(Colors.Black, 4));

        Assert.Equal(0, InkedPixels(bitmap));
    }

    [Fact]
    public void ASurfaceThatWasToldNothing_AnswersWithTheLibrarysLooks_AndOneThatWasToldAnswersWithThem()
    {
        using var bitmap = new SKBitmap(10, 10);
        using var canvas = new SKCanvas(bitmap);
        var ctx = new SkiaRenderContext(canvas);
        var told = (IThemedRenderContext)ctx;

        Assert.Same(ThemeLooks.Library, told.Looks);
        ctx.UseLooksOf(Theme.MatplotlibV2);

        Assert.Same(Theme.MatplotlibV2.Looks, told.Looks);
        Assert.Same(Theme.MatplotlibV2.Looks.Text.MathFonts, told.MathFonts);
    }
}
