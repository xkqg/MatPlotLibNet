// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;
using SkiaSharp;
using MplColor = MatPlotLibNet.Styling.Color;

namespace MatPlotLibNet.Skia.Tests;

/// <summary>A dash is as long in the PNG as in the SVG of the same figure: the SVG is the library's native output, and under a library
/// theme it writes the pattern's lengths as they are (5 on, 2 off) whatever the width of the line, so the raster draws them so too
/// (it scaled them by the width, 10 on and 4 off at a width of 2).</summary>
public sealed class DashLengthTests
{
    private static readonly StrokeStyle Dashed = new(MplColor.FromHex("#000000"), 2, LineStyle.Dashed);

    private static SKBitmap Draw(Action<SkiaRenderContext> draw)
    {
        var bitmap = new SKBitmap(60, 40);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.White);
        draw(new SkiaRenderContext(canvas));
        return bitmap;
    }

    [Fact]
    public void ADashedPolylineUnderALibraryTheme_IsDashedAtTheLengthsTheSvgWrites()
    {
        // Two points, a width of 2 centred on y = 20 (rows 19 and 20): 5 on from x = 5, 2 off, 5 on, 2 off.
        using var bitmap = Draw(ctx => ctx.DrawLines([new Point(5, 20), new Point(45, 20)], Dashed));

        Assert.Equal(0, bitmap.GetPixel(7, 19).Red);     // the first dash, 5 to 10
        Assert.Equal(255, bitmap.GetPixel(11, 19).Red);  // the first gap, 10 to 12
        Assert.Equal(0, bitmap.GetPixel(14, 19).Red);    // the second dash, 12 to 17
        Assert.Equal(255, bitmap.GetPixel(18, 19).Red);  // the second gap, 17 to 19
    }

    [Fact]
    public void ADashedShapeEdgeUnderALibraryTheme_IsDashedAtTheSameLengths()
    {
        using var bitmap = Draw(ctx => ctx.DrawRectangle(new Rect(5, 20, 40, 10), new ShapeStyle(null, MplColor.FromHex("#000000"), 2) { StrokeLineStyle = LineStyle.Dashed }));

        // The top edge runs along y = 20 from x = 5: the same dashes.
        Assert.Equal(255, bitmap.GetPixel(11, 19).Red);
        Assert.Equal(0, bitmap.GetPixel(14, 19).Red);
    }
}
