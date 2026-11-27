// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;
using SkiaSharp;

namespace MatPlotLibNet.Skia.Tests;

/// <summary>A rug plot's ticks in a raster export stand on the pixel grid the way matplotlib's Agg backend puts them there. seaborn's
/// <c>rugplot</c> draws a <c>LineCollection</c> of vertical segments, and Agg puts a stroke of horizontal and vertical segments on a
/// whole pixel, half a pixel on when its width rounds to an odd number of pixels: a tick of 1 pt (1.39 px) fills one column with its
/// colour and 19 percent of it on each side, wherever its data value falls. matplotlib 3.11.2 draws the ticks of
/// <c>classic/rugplot.png</c> as the colours #0000FF and #CDCDFF (one column of blue and 20 percent of it beside), the two
/// colours that make up most of the blue in the picture, where a tick left where it stands spreads over two or three columns of
/// every blend. The ticks have butt ends, as a <c>LineCollection</c>'s have, so a tick is as long as it was given.</summary>
public class RugTickPixelGridTests
{
    private static readonly SKColor Blue = new(0, 0, 255);

    /// <summary>A rug plot of one tick, the X range as wide as the plot is in pixels, with the tick on a pixel edge: the column
    /// it is drawn into when snapped is the one on the right of the edge. It stands between two major ticks of the X axis, whose
    /// inward marks the classic theme draws over the data (the axis at zorder 2.5, the rug's collection at 2).</summary>
    private static (Figure Figure, int Edge, int Row) OneTick(Theme theme)
    {
        Figure Build(double value) => Plt.Create().WithSize(800, 600).WithTheme(theme)
            .AddSubPlot(1, 1, 1, ax => ax
                .Rugplot([value], s => { s.Height = 0.3; s.Alpha = 1; s.Color = Colors.Blue; s.LineWidth = 100.0 / 72; })
                .SetXLim(0, 620).SetYLim(0, 1))
            .Build();

        var plot = Build(330).PlotAreaOnSkia();
        int edge = (int)Math.Ceiling(plot.X + 330 * plot.Width / 620);
        double value = (edge - plot.X) * 620 / plot.Width;
        return (Build(value), edge, (int)(plot.Y + plot.Height * 0.85));
    }

    [Fact]
    public void ATick_FillsOneColumnOfThePixelGridWithItsColour()
    {
        var (figure, edge, row) = OneTick(Theme.MatplotlibClassic);
        using var picture = SKBitmap.Decode(figure.ToPng());

        Assert.Equal(Blue, picture.GetPixel(edge, row));
    }

    [Fact]
    public void ATick_SpillsItsRestEvenlyOntoTheTwoColumnsBesideIt()
    {
        var (figure, edge, row) = OneTick(Theme.MatplotlibClassic);
        using var picture = SKBitmap.Decode(figure.ToPng());

        // 1 pt is 1.39 px: a column of blue and 0.19 px on each side, over the white plot: 255 * (1 - 0.19) = 207.
        Assert.InRange(picture.GetPixel(edge - 1, row).Red, 200, 210);
        Assert.InRange(picture.GetPixel(edge + 1, row).Red, 200, 210);
    }

    [Fact]
    public void ATick_IsAsLongAsItWasGiven_ItsEndsAreButt()
    {
        var (figure, edge, _) = OneTick(Theme.MatplotlibClassic);
        var plot = figure.PlotAreaOnSkia();
        using var picture = SKBitmap.Decode(figure.ToPng());
        int bottom = (int)Math.Round(plot.Y + plot.Height);   // where the tick starts; it runs 0.3 of the plot's height up

        Assert.Equal(Blue, picture.GetPixel(edge, bottom - 2));
        Assert.NotEqual(Blue, picture.GetPixel(edge, bottom + 3));
        Assert.NotEqual(Blue, picture.GetPixel(edge, (int)Math.Round(plot.Y + plot.Height * 0.7) - 3));
    }

    [Fact]
    public void ATheme_ThatDoesNotAskForIt_LeavesTheTickWhereItStands()
    {
        var (figure, edge, row) = OneTick(Theme.Default);
        using var picture = SKBitmap.Decode(figure.ToPng());

        // 1.39 px centred on a pixel edge: about 0.7 px on each of the two columns around it, a blend in both, a column of blue in none.
        Assert.InRange(picture.GetPixel(edge - 1, row).Red, 40, 120);
        Assert.InRange(picture.GetPixel(edge, row).Red, 40, 120);
    }
}
