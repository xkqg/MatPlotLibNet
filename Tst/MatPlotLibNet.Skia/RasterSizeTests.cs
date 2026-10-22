// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using SkiaSharp;

namespace MatPlotLibNet.Skia.Tests;

/// <summary>The one place a raster export turns a figure and a resolution into a pixel size and a scale.</summary>
public class RasterSizeTests
{
    private static Figure FigureOf(double width, double height) => new() { Width = width, Height = height };

    [Fact]
    public void At96Dpi_AFigureOfWholeSize_IsDrawnUnscaled()
    {
        Assert.Equal(new RasterSize(400, 300, 1f, 1f), RasterSize.For(FigureOf(400, 300), 96));
    }

    [Fact]
    public void At192Dpi_EveryUnitIsTwoPixels()
    {
        Assert.Equal(new RasterSize(800, 600, 2f, 2f), RasterSize.For(FigureOf(400, 300), 192));
    }

    [Fact]
    public void AtHalfTheResolution_EveryUnitIsHalfAPixel()
    {
        Assert.Equal(new RasterSize(200, 150, 0.5f, 0.5f), RasterSize.For(FigureOf(400, 300), 48));
    }

    [Fact]
    public void TheScale_IsExactlyThePixelCountOverTheSize()
    {
        // 500 * 150/96 = 781.25 -> 781 pixels, so the drawing is scaled by 781/500 and meets both edges.
        RasterSize size = RasterSize.For(FigureOf(500, 350), 150);

        Assert.Equal(781, size.Width);
        Assert.Equal(547, size.Height);
        Assert.Equal(781f / 500f, size.ScaleX);
        Assert.Equal(547f / 350f, size.ScaleY);
    }

    [Fact]
    public void AHalfPixel_RoundsAwayFromZero()
    {
        // 10.5 units at 96 dpi is 10.5 pixels: 11, not the banker's 10.
        Assert.Equal(11, RasterSize.For(FigureOf(10.5, 10.5), 96).Width);
        Assert.Equal(13, RasterSize.For(FigureOf(12.5, 12.5), 96).Height);
    }

    [Fact]
    public void AFractionalFigureAt96Dpi_IsRoundedNotCut()
    {
        Assert.Equal(500, RasterSize.For(FigureOf(499.6, 350.4), 96).Width);
        Assert.Equal(350, RasterSize.For(FigureOf(499.6, 350.4), 96).Height);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void ADpiThatIsNotPositiveAndFinite_IsRefused(double dpi)
    {
        var refusal = Assert.Throws<ArgumentOutOfRangeException>(() => RasterSize.For(FigureOf(400, 300), dpi));

        Assert.Equal("dpi", refusal.ParamName);
    }

    [Fact]
    public void LessThanOnePixel_IsRefused()
    {
        var refusal = Assert.Throws<ArgumentException>(() => RasterSize.For(FigureOf(400, 300), 0.1));

        Assert.Equal("figure", refusal.ParamName);
        Assert.Contains("pixels", refusal.Message);
    }

    [Fact]
    public void ThinInOneDirectionOnly_IsRefused()
    {
        // 1 unit tall at half the resolution is half a pixel, which rounds to 1; a quarter is 0.
        Assert.Equal(1, RasterSize.For(FigureOf(400, 1), 48).Height);
        Assert.Throws<ArgumentException>(() => RasterSize.For(FigureOf(400, 0.4), 96));
    }

    [Fact]
    public void MorePixelsThanACountCanHold_IsRefused()
    {
        Assert.Throws<ArgumentException>(() => RasterSize.For(FigureOf(400, 300), 1e12));
        Assert.Throws<ArgumentException>(() => RasterSize.For(FigureOf(1, 3e9), 96));
    }

    [Theory]
    [InlineData(0.0, 300.0)]
    [InlineData(400.0, 0.0)]
    [InlineData(double.NaN, 300.0)]
    [InlineData(400.0, double.NaN)]
    [InlineData(-400.0, 300.0)]
    public void AFigureWithNoSize_IsRefused(double width, double height)
    {
        Assert.Throws<ArgumentException>(() => RasterSize.For(FigureOf(width, height), 96));
    }

    // ── drawing onto a bitmap ────────────────────────────────────────────────────────────────────────

    private sealed class ThrowingRenderer : IChartRenderer
    {
        public void Render(Figure figure, IRenderContext ctx) => throw new InvalidOperationException("the drawing failed");

        public LayoutResult ComputeLayout(Figure figure, IRenderContext measureCtx) => throw new NotSupportedException();
    }

    [Fact]
    public void ADrawingThatFails_PropagatesItsException()
    {
        var refusal = Assert.Throws<InvalidOperationException>(
            () => new ThrowingRenderer().RenderToBitmap(FigureOf(40, 30), 96));

        Assert.Equal("the drawing failed", refusal.Message);
    }

    [Fact]
    public void ABitmap_HasTheRasterSize()
    {
        using SKBitmap bitmap = new ChartRenderer().RenderToBitmap(FigureOf(40, 30), 192);

        Assert.Equal(80, bitmap.Width);
        Assert.Equal(60, bitmap.Height);
    }

    [Fact]
    public void ADpiNoBitmapCanHave_IsRefusedBeforeAnythingIsAllocated()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartRenderer().RenderToBitmap(FigureOf(40, 30), 0));
    }
}
