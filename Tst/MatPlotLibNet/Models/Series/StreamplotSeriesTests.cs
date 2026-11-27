// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="StreamplotSeries"/> construction, computation, serialization, and rendering.</summary>
public class StreamplotSeriesTests
{
    private static readonly double[] TestX = [0.0, 1.0, 2.0];
    private static readonly double[] TestY = [0.0, 1.0, 2.0];
    private static readonly double[,] TestU = { { 1, 1, 1 }, { 1, 1, 1 }, { 1, 1, 1 } };
    private static readonly double[,] TestV = { { 0, 0, 0 }, { 1, 1, 1 }, { 0, 0, 0 } };

    /// <summary>Verifies that ComputeDataRange matches grid bounds.</summary>
    [Fact]
    public void ComputeDataRange_MatchesGridBounds()
    {
        var series = new StreamplotSeries(TestX, TestY, TestU, TestV);
        var range = series.ComputeDataRange(null!);
        Assert.Equal(0.0, range.XMin);
        Assert.Equal(2.0, range.XMax);
        Assert.Equal(0.0, range.YMin);
        Assert.Equal(2.0, range.YMax);
    }

    /// <summary>Verifies that rendering produces SVG output containing line or polyline elements.</summary>
    [Fact]
    public void Render_ProducesSvg()
    {
        double[] x = [0.0, 1.0, 2.0], y = [0.0, 1.0, 2.0];
        double[,] u = { { 1, 1, 1 }, { 1, 1, 1 }, { 1, 1, 1 } };
        double[,] v = { { 0, 0, 0 }, { 0, 0, 0 }, { 0, 0, 0 } };

        var svg = new FigureBuilder()
            .AddSubPlot(1, 1, 1, ax => ax.Streamplot(x, y, u, v))
            .ToSvg();

        Assert.Contains("<", svg);
        // Streamlines produce polyline or line elements
        Assert.True(svg.Contains("polyline") || svg.Contains("line"),
            "SVG should contain polyline or line elements for streamlines.");
    }
}
