// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;
using SkiaSharp;
using MplColor = MatPlotLibNet.Styling.Color;

namespace MatPlotLibNet.Skia.Tests;

/// <summary>The PNG dashes a line as matplotlib's Agg does under the matplotlib themes (python, matplotlib 3.11.2:
/// <c>Line2D(linestyle=..., linewidth=2.0)._dash_pattern</c> gives [7.4, 3.2] points under the default style, where
/// <c>lines.scale_dashes</c> is True, and (6.0, 6.0) points under the classic one, where it is False; a point is 100 / 72 pixels).</summary>
public sealed class MatplotlibDashRasterTests
{
    private static readonly MplColor Black = MplColor.FromHex("#000000");

    /// <summary>A white bitmap of 80 by 40 pixels, drawn on under <paramref name="theme"/> by <paramref name="draw"/>.</summary>
    private static SKBitmap Raster(Theme theme, Action<SkiaRenderContext> draw)
    {
        var bitmap = new SKBitmap(80, 40);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.White);
        draw(canvas.RasterExportUnder(theme));
        return bitmap;
    }

    [Fact]
    public void ADashedPolylineUnderTheDefaultStyle_IsDashedAtThePatternTimesTheWidth()
    {
        using var bitmap = Raster(Theme.MatplotlibV2, ctx => ctx.DrawLines([new Point(5, 20), new Point(75, 20)], new StrokeStyle(Black, 2, LineStyle.Dashed)));

        // A width of 2: 7.4 on and 3.2 off from x = 5: the first dash to 12.4, the gap to 15.6, the next dash to 23.
        Assert.Equal(0, bitmap.GetPixel(10, 19).Red);
        Assert.Equal(255, bitmap.GetPixel(13, 19).Red);
        Assert.Equal(255, bitmap.GetPixel(14, 19).Red);
        Assert.Equal(0, bitmap.GetPixel(18, 19).Red);
    }

    [Fact]
    public void ADashedPolylineUnderTheClassicStyle_IsDashedInPoints_WhateverTheWidth()
    {
        using var bitmap = Raster(Theme.MatplotlibClassic, ctx => ctx.DrawLines([new Point(5, 20), new Point(75, 20)], new StrokeStyle(Black, 2, LineStyle.Dashed)));

        // 6 pt on and 6 pt off is 8.33 px each: the first dash runs 5 to 13.33, the gap to 21.67, the next dash to 30.
        Assert.Equal(0, bitmap.GetPixel(12, 19).Red);
        Assert.Equal(255, bitmap.GetPixel(15, 19).Red);
        Assert.Equal(255, bitmap.GetPixel(20, 19).Red);
        Assert.Equal(0, bitmap.GetPixel(24, 19).Red);
    }

    [Fact]
    public void ADottedGridLineUnderTheClassicStyle_CoversEachDotByTheAreaItTakes()
    {
        // classic grid lines: 0.5 pt wide, dotted [1, 3] pt: 1.39 px on and 4.17 px off, a dot every 5.56 px, at any width.
        using var bitmap = Raster(Theme.MatplotlibClassic, ctx => ctx.DrawLine(new Point(5, 20.5), new Point(75, 20.5), new StrokeStyle(Black, 0.6944, LineStyle.Dotted)));

        Assert.InRange(bitmap.GetPixel(5, 20).Red, 0, 80);        // the first dot covers pixel 5 across 0.69 of its height
        Assert.Equal(255, bitmap.GetPixel(8, 20).Red);             // the gap, 6.39 to 10.56
        Assert.InRange(bitmap.GetPixel(10, 20).Red, 140, 215);     // the next dot starts 0.56 into pixel 10
        Assert.Equal(255, bitmap.GetPixel(13, 20).Red);
    }
}
