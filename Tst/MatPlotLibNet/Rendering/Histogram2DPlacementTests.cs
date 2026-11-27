// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A 2-D histogram's cells stand on their bins in data units, the lowest Y bin at the bottom. matplotlib 3.11.2,
/// <c>Axes.hist2d</c>: <c>h, xedges, yedges = np.histogram2d(x, y, bins, range)</c> (no range: the extent of the points), then
/// <c>pcolormesh(xedges, yedges, h.T)</c>, so the mesh covers the points' extent and the axis may reach past it (the classic
/// style rounds it out to its ticks).</summary>
public sealed class Histogram2DPlacementTests
{
    private readonly record struct Cell(double X, double Y, double Width, double Height, string Fill);

    private static List<Cell> Rects(string svg) => [.. svg.Elements("rect").Where(r => r["fill"] is not null)
        .Select(r => new Cell(r.Number("x"), r.Number("y"), r.Number("width"), r.Number("height"), r["fill"]!))];

    /// <summary>The mesh's edges are sticky, as <c>Axes._update_pcolor_lims</c> makes them (<c>collection.sticky_edges.x[:] =
    /// [minx, maxx]</c>): under the v2 theme's 5 % margin the axes end on the mesh, <c>ax.hist2d([0, 1, 2, 3], [0, 1, 3, 2])</c>
    /// drawing X and Y from 0 to 3.</summary>
    [Fact]
    public void TheMeshsEdges_AreSticky()
    {
        var axes = new MatPlotLibNet.Models.Axes();
        axes.AddSeries(new MatPlotLibNet.Models.Series.Histogram2DSeries([0.0, 1, 2, 3], [0.0, 1, 3, 2], 3, 3));

        var range = axes.ComputeDataRanges(Theme.MatplotlibV2);

        Assert.Equal(0, range.XMin, 9);
        Assert.Equal(3, range.XMax, 9);
        Assert.Equal(0, range.YMin, 9);
        Assert.Equal(3, range.YMax, 9);
    }

    /// <summary>Two points, one at (0, 0) and one at (1, 1), in 2 x 2 bins over 0 .. 1, drawn on axes from -1 to 2: each cell is
    /// half a unit wide, a sixth of the plot; the cell of (0, 0) stands left of and below the cell of (1, 1).</summary>
    [Fact]
    public void ACell_StandsOnItsBin_TheLowestYAtTheBottom()
    {
        string svg = Plt.Create().WithTheme(Theme.Default).WithSize(400, 400)
            .AddSubPlot(1, 1, 1, ax => ax
                .Histogram2D([0.0, 1], [0.0, 1], 2, s => s.ColorMap = ColorMaps.Viridis)
                .SetXLim(-1, 2).SetYLim(-1, 2))
            .ToSvg();
        var rects = Rects(svg);
        var plot = rects.Where(r => r.Fill.Equals("#FFFFFF", StringComparison.OrdinalIgnoreCase)).OrderByDescending(r => r.Width * r.Height).Skip(1).First();
        var full = rects.Where(r => r.Fill.Equals("#FDE724", StringComparison.OrdinalIgnoreCase)).OrderBy(r => r.X).ToList();

        Assert.Equal(2, full.Count);
        Assert.Equal(plot.Width / 6, full[0].Width, 1);
        Assert.Equal(plot.Height / 6, full[0].Height, 1);
        Assert.True(full[0].Y > full[1].Y, "the cell of (0, 0) stands below the cell of (1, 1)");
        Assert.Equal(plot.X + plot.Width / 3, full[0].X, 1);
    }
}
