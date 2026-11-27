// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;
using SkiaSharp;

namespace MatPlotLibNet.Skia.Tests;

/// <summary>A line of horizontal and vertical segments (a step line, an ECDF) stands on the pixel grid of a PNG under the matplotlib
/// themes, as matplotlib's Agg backend snaps a <c>Line2D</c> (<see cref="Skia.SkiaRenderContext.AggPixelGrid"/> has the arithmetic's own tests). matplotlib 3.11.2
/// draws the horizontal run of a 1 pt <c>ax.step</c> at device y 19.7 as the rows 205, 0, 205: one row of the line's colour. The library's
/// own themes leave the line where it falls.</summary>
public class LinePixelGridTests
{
    /// <summary>A step line in blue whose top run stands at <paramref name="top"/> in data units, on axes from 0 to 10 both ways.</summary>
    private static Figure Step(Theme theme, double top) => Plt.Create().WithSize(800, 600).WithTheme(theme)
        .AddSubPlot(1, 1, 1, ax => ax.Step([0.0, 4.0, 10.0], [0.0, top, top], s => s.Color = Colors.Blue).SetXLim(0, 10).SetYLim(0, 10))
        .Build();

    /// <summary>The pixel row whose top edge a step's run at 70 % of the axes' height is moved onto, and the data value that puts the
    /// run exactly on that edge, where an unsnapped line blends into the two rows beside it.</summary>
    private static (int Edge, double Top, int Column) OnAPixelEdge(Theme theme)
    {
        var plot = new ChartRenderer().ComputeLayout(Step(theme, 7), new SvgRenderContext()).PlotAreas[0];
        int edge = (int)Math.Ceiling(plot.Y + plot.Height * 0.3);
        return (edge, 10 * (1 - (edge - plot.Y) / plot.Height), (int)(plot.X + plot.Width * 0.73));
    }

    [Fact]
    public void UnderClassic_AStepLinesHorizontalRun_FillsOneRowOfThePixelGrid()
    {
        var (edge, top, column) = OnAPixelEdge(Theme.MatplotlibClassic);
        using var picture = SKBitmap.Decode(Step(Theme.MatplotlibClassic, top).ToPng());

        // Snapped (1 pt rounds to one pixel, half a pixel on) the run fills the row below the edge with its colour.
        Assert.Equal(new SKColor(0, 0, 255), picture.GetPixel(column, edge));
    }

    [Fact]
    public void UnderTheLibrarysThemes_AStepLinesRun_StaysWhereItFalls()
    {
        var (edge, top, column) = OnAPixelEdge(Theme.Default);
        using var picture = SKBitmap.Decode(Step(Theme.Default, top).ToPng());

        // 1.5 px centred on the edge: the rows on both sides of it are blends, neither the line's full colour.
        Assert.NotEqual(new SKColor(0, 0, 255), picture.GetPixel(column, edge));
        Assert.NotEqual(new SKColor(0, 0, 255), picture.GetPixel(column, edge - 1));
    }
}
