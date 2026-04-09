// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="StreamplotSeries"/> construction, computation, serialization, and rendering.</summary>
public class StreamplotSeriesTests
{
    private static readonly double[] TestX = [0.0, 1.0, 2.0];
    private static readonly double[] TestY = [0.0, 1.0, 2.0];
    private static readonly double[,] TestU = { { 1, 1, 1 }, { 1, 1, 1 }, { 1, 1, 1 } };
    private static readonly double[,] TestV = { { 0, 0, 0 }, { 1, 1, 1 }, { 0, 0, 0 } };

    /// <summary>Verifies that the constructor stores X, Y, U, and V data.</summary>
    [Fact]
    public void Constructor_StoresData()
    {
        var series = new StreamplotSeries(TestX, TestY, TestU, TestV);
        Assert.Same(TestX, series.X);
        Assert.Same(TestY, series.Y);
        Assert.Same(TestU, series.U);
        Assert.Same(TestV, series.V);
    }

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

    /// <summary>Verifies that the DTO type is "streamplot".</summary>
    [Fact]
    public void ToSeriesDto_TypeIsStreamplot()
    {
        var series = new StreamplotSeries(TestX, TestY, TestU, TestV);
        var dto = series.ToSeriesDto();
        Assert.Equal("streamplot", dto.Type);
    }

    /// <summary>Verifies that Density defaults to 1.0.</summary>
    [Fact]
    public void DefaultDensity_IsOne()
    {
        var series = new StreamplotSeries(TestX, TestY, TestU, TestV);
        Assert.Equal(1.0, series.Density);
    }

    /// <summary>Verifies that Density can be set to a custom value.</summary>
    [Fact]
    public void Density_CanBeSet()
    {
        var series = new StreamplotSeries(TestX, TestY, TestU, TestV) { Density = 2.5 };
        Assert.Equal(2.5, series.Density);
    }

    /// <summary>Verifies that JSON round-trip preserves X, Y, and U dimensions.</summary>
    [Fact]
    public void RoundTrip_PreservesData()
    {
        var fig = new Figure();
        var axes = fig.AddSubPlot();
        axes.Streamplot(TestX, TestY, TestU, TestV);

        var serializer = new ChartSerializer();
        var json = serializer.ToJson(fig);
        var restored = serializer.FromJson(json);

        var restoredSeries = restored.SubPlots[0].Series.OfType<StreamplotSeries>().Single();
        Assert.Equal(TestX, restoredSeries.X);
        Assert.Equal(TestY, restoredSeries.Y);
        Assert.Equal(TestU.GetLength(0), restoredSeries.U.GetLength(0));
        Assert.Equal(TestU.GetLength(1), restoredSeries.U.GetLength(1));
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
