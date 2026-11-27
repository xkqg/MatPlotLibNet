// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Models.Series;

public class TricontourSeriesTests
{
    /// <summary>A triangle contour ranges its axes as a grid contour does: tight to the points, with no margin past them (matplotlib
    /// 3.11.2, <c>TriContourSet._process_args</c> takes <c>_mins</c> and <c>_maxs</c> from the triangulation's points, and
    /// <c>ContourSet.__init__</c> makes them the sticky edges and calls <c>autoscale_view(tight=True)</c>). Without them the 5 % margin
    /// stands between the lines and the spines, and the lines sit where a smaller picture of the surface would put them.</summary>
    [Fact]
    public void ComputeDataRange_ReturnsThePointsExtent_WithStickyEdgesOnAllFourSides()
    {
        double[] x = [2.0, 5.0, 3.5, -1.0], y = [1.0, 1.0, 4.0, 2.0], z = [1.0, 2.0, 3.0, 4.0];
        var range = new TricontourSeries(x, y, z).ComputeDataRange(null!);

        Assert.Equal((-1.0, 5.0, 1.0, 4.0), (range.XMin, range.XMax, range.YMin, range.YMax));
        Assert.Equal((-1.0, 5.0, 1.0, 4.0), (range.StickyXMin, range.StickyXMax, range.StickyYMin, range.StickyYMax));
    }
}
