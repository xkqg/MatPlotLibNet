// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;
using SkiaSharp;

namespace MatPlotLibNet.Skia.Tests;

/// <summary>A fill that says it is aliased (<c>ShapeStyle.Aliased</c>, matplotlib's <c>antialiased=False</c> for the bands of a
/// filled contour) is painted by the raster export with no antialiasing: a pixel its edge crosses is the fill or the ground, never a
/// blend. The picture of a filled contour under a matplotlib theme then holds one colour for each band.</summary>
public class AliasedFillRasterTests
{
    private static HashSet<SKColor> ColoursOfARectangleOnAHalfPixel(bool aliased)
    {
        using var bitmap = new SKBitmap(24, 24);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.White);
        new SkiaRenderContext(canvas).DrawRectangle(new Rect(2.5, 2.5, 10.25, 10.25), new ShapeStyle(Colors.Red, null, 0) { Aliased = aliased });

        var colours = new HashSet<SKColor>();
        for (int y = 0; y < 24; y++)
            for (int x = 0; x < 24; x++)
                colours.Add(bitmap.GetPixel(x, y));
        return colours;
    }

    [Fact]
    public void AnAliasedRectangle_OnAHalfPixel_HoldsTheFillAndTheGroundOnly() =>
        Assert.Equal(2, ColoursOfARectangleOnAHalfPixel(aliased: true).Count);

    [Fact]
    public void AnOrdinaryRectangle_OnAHalfPixel_BlendsItsEdge() =>
        Assert.True(ColoursOfARectangleOnAHalfPixel(aliased: false).Count > 2);

    private static readonly double[] Axis = [0.0, 1.0, 2.0, 3.0, 4.0];

    private static double[,] Diagonal()
    {
        var z = new double[5, 5];
        for (int r = 0; r < 5; r++)
            for (int c = 0; c < 5; c++)
                z[r, c] = r + 0.7 * c;
        return z;
    }

    /// <summary>The colours the plot of a filled contour holds, inside its frame.</summary>
    private static int ColoursInThePlot(Theme theme)
    {
        var figure = Plt.Create().WithSize(800, 600).WithTheme(theme)
            .AddSubPlot(1, 1, 1, ax => ax.Contourf(Axis, Axis, Diagonal(), s => { s.Levels = 4; s.ShowLines = false; }))
            .Build();
        var plot = figure.PlotAreaOnSkia();
        using var picture = SKBitmap.Decode(figure.ToPng());

        var colours = new HashSet<SKColor>();
        for (int y = (int)plot.Y + 3; y < (int)(plot.Y + plot.Height) - 3; y++)
            for (int x = (int)plot.X + 3; x < (int)(plot.X + plot.Width) - 3; x++)
                colours.Add(picture.GetPixel(x, y));
        return colours.Count;
    }

    /// <summary>The values 0 to 6.8 asked for 4 levels bound a handful of bands (matplotlib's round levels), each one colour; the
    /// edges between them blend nothing.</summary>
    [Fact]
    public void AFilledContour_UnderV2_HoldsOneColourForEachBand() =>
        Assert.InRange(ColoursInThePlot(Theme.MatplotlibV2), 3, 6);

    [Fact]
    public void AFilledContour_UnderTheLibrarysOwnTheme_BlendsItsBandsEdges() =>
        Assert.True(ColoursInThePlot(Theme.Default) > 50);
}
