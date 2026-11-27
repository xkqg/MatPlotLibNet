// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;
using SkiaSharp;

namespace MatPlotLibNet.Skia.Tests;

/// <summary>The lines of a box in a raster export stand on the pixel grid the way matplotlib's Agg backend puts them there. A box's
/// whiskers, caps and median are <c>Line2D</c>s of one horizontal or vertical segment and its rectangle a closed path of four, and a
/// path of rectilinear segments is snapped (<c>snap</c> is automatic): each moved to a whole pixel, half a pixel on when the width rounds
/// to an odd number of pixels, so a line of 1 pt (1.39 px) fills one column or row with its colour and 19 percent of it on each side.
/// matplotlib 3.11.2 draws the default style's box as one black column with 20 percent of it each side (the colours #000000 and #CDCDCD
/// of <c>v2/box.png</c>), where a line left where it stands spreads over two columns of every blend.</summary>
public class BoxPixelGridTests
{
    /// <summary>One box plot over an X axis of 0 to 2: under the matplotlib themes a box plot stands its first box at 1, as
    /// <c>ax.boxplot</c> does, so the box stands in the middle of the plot.</summary>
    private static Figure Build(Theme theme, double median, double widths = 0.5) => Plt.Create().WithSize(800, 600).WithTheme(theme)
        .AddSubPlot(1, 1, 1, ax => ax
            .BoxPlot([[median - 1, median, median + 1]], s => s.Widths = widths)
            .SetXLim(0, 2).SetYLim(0, 10).WithGrid(g => g with { Visible = false }))
        .Build();

    /// <summary>The figure with its median on a pixel edge: the value whose row is a whole number, found from the layout (the Y axis runs
    /// 0 to 10, so a unit is about 46 pixels and the box is about 46 high, its whiskers about 92).</summary>
    private static (Figure Figure, Rect Plot, int Edge) OnAnEdge(Theme theme, double widths = 0.5)
    {
        var plot = Build(theme, 5, widths).PlotAreaOnSkia();
        // value -> y = plot.Y + plot.Height * (1 - value / 10); take the value that lands on an edge near the middle.
        int edge = (int)Math.Round(plot.Y + plot.Height / 2);
        double median = 10 * (1 - (edge - plot.Y) / plot.Height);
        var figure = Build(theme, median, widths);
        return (figure, figure.PlotAreaOnSkia(), edge);
    }

    [Fact]
    public void V2_TheMedian_FillsOneRowWithItsColour_AndSpillsTheRestEvenlyOntoTheRowsBesideIt()
    {
        var (figure, plot, edge) = OnAnEdge(Theme.MatplotlibV2);
        using var picture = SKBitmap.Decode(figure.ToPng());
        int x = (int)(plot.X + plot.Width / 2);

        var c1 = Theme.MatplotlibV2.CycleColors[1];
        Assert.Equal(new SKColor(c1.R, c1.G, c1.B), picture.GetPixel(x + 4, edge));
        // 1.39 px: a row of orange and the rest, a fifth of a pixel, evenly on the rows beside it, over the white plot.
        int above = picture.GetPixel(x + 4, edge - 1).Blue, below = picture.GetPixel(x + 4, edge + 1).Blue;
        Assert.InRange(above, 150, 240);
        Assert.InRange(Math.Abs(above - below), 0, 3);
    }

    [Fact]
    public void V2_AWhisker_IsOneColumnOfBlack_WithTheRestOntoTheTwoBesideIt()
    {
        var (figure, plot, edge) = OnAnEdge(Theme.MatplotlibV2);
        using var picture = SKBitmap.Decode(figure.ToPng());
        // The upper whisker runs from the box's top (median + 0.5, about 23 pixels up) to median + 1 (about 46); a row between is in it.
        int row = edge - 35;
        int x = (int)(plot.X + plot.Width / 2);
        var columns = Enumerable.Range(x - 3, 7).Select(c => picture.GetPixel(c, row).Red).ToArray();

        Assert.Equal(1, columns.Count(r => r == 0));   // one column of pure black
        Assert.Equal(2, columns.Count(r => r is >= 195 and <= 215));   // 255 * (1 - 0.19) = 207 on each side
    }

    [Fact]
    public void V2_TheBoxSides_AreOneColumnOfBlackEach()
    {
        // A box 0.4 wide on an axis of 2 over 620 pixels has its sides on whole pixel edges (62 pixels either side of the middle), where an
        // unsnapped line of 1.39 pixels is two columns of 0.69 and a snapped one a column of black.
        var (figure, plot, edge) = OnAnEdge(Theme.MatplotlibV2, widths: 0.4);
        using var picture = SKBitmap.Decode(figure.ToPng());
        // Between the box's top and its bottom (about 23 pixels each side of the median row), a row below the median crosses both sides.
        int row = edge + 10;
        int from = (int)(plot.X + plot.Width / 2 - plot.Width / 5) - 4, to = from + (int)(2 * plot.Width / 5) + 8;
        var black = Enumerable.Range(from, to - from).Where(c => picture.GetPixel(c, row).Red == 0).ToArray();

        // Each side is a column of black on its own, and nothing else on the row is.
        Assert.Equal(2, black.Length);
    }
}
