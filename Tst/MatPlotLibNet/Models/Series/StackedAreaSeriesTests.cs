// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="StackedAreaSeries"/> construction, data range, serialization, and rendering.</summary>
public class StackedAreaSeriesTests
{
    private static readonly double[] X = [1.0, 2.0, 3.0];
    private static readonly double[][] YSets = [[1.0, 2.0, 3.0], [2.0, 3.0, 1.0]];

    /// <summary>Verifies that the constructor stores X and YSets data.</summary>
    [Fact]
    public void Constructor_StoresData()
    {
        var series = new StackedAreaSeries(X, YSets);
        Assert.Equal(X, series.X);
        Assert.Equal(YSets, series.YSets);
    }

    /// <summary>Verifies that the Y data range is 0 to the max cumulative sum across all X positions.</summary>
    [Fact]
    public void ComputeDataRange_YIsZeroToCumulativeMax()
    {
        // At x=1: 1+2=3, at x=2: 2+3=5, at x=3: 3+1=4 => max cumulative = 5
        var series = new StackedAreaSeries(X, YSets);
        var range = series.ComputeDataRange(null!);
        Assert.Equal(0.0, range.YMin);
        Assert.Equal(5.0, range.YMax);
    }

    /// <summary>Verifies that the DTO type is "stackedarea".</summary>
    [Fact]
    public void ToSeriesDto_TypeIsStackedarea()
    {
        var series = new StackedAreaSeries(X, YSets);
        var dto = series.ToSeriesDto();
        Assert.Equal("stackedarea", dto.Type);
    }

    /// <summary>Verifies that Labels property is stored correctly.</summary>
    [Fact]
    public void Labels_StoredCorrectly()
    {
        var labels = new[] { "Layer A", "Layer B" };
        var series = new StackedAreaSeries(X, YSets) { Labels = labels };
        Assert.Equal(labels, series.Labels);

        var dto = series.ToSeriesDto();
        Assert.Equal(labels, dto.PieLabels);
    }

    /// <summary>Verifies that JSON round-trip preserves the data.</summary>
    [Fact]
    public void RoundTrip_PreservesData()
    {
        var fig = new Figure();
        var axes = fig.AddSubPlot();
        var original = axes.StackPlot(X, YSets);
        original.Labels = ["A", "B"];
        original.Alpha = 0.5;

        var serializer = new ChartSerializer();
        var json = serializer.ToJson(fig);
        var restored = serializer.FromJson(json);

        var restoredSeries = restored.SubPlots[0].Series.OfType<StackedAreaSeries>().Single();
        Assert.Equal(X, restoredSeries.X);
        Assert.Equal(YSets.Length, restoredSeries.YSets.Length);
        for (int i = 0; i < YSets.Length; i++)
            Assert.Equal(YSets[i], restoredSeries.YSets[i]);
        Assert.Equal(["A", "B"], restoredSeries.Labels!);
        Assert.Equal(0.5, restoredSeries.Alpha);
    }

    /// <summary>Verifies that rendering produces SVG output containing a polygon element.</summary>
    [Fact]
    public void Render_ProducesSvg()
    {
        var svg = Plt.Create()
            .StackPlot(X, YSets)
            .ToSvg();

        Assert.Contains("polygon", svg);
    }

    /// <summary>Verifies that Accept calls the visitor's Visit method for StackedAreaSeries.</summary>
    [Fact]
    public void Accept_CallsVisitor()
    {
        var series = new StackedAreaSeries(X, YSets);
        var visitor = new TestSeriesVisitor();
        series.Accept(visitor, null!);
        Assert.Equal(nameof(StackedAreaSeries), visitor.LastVisited);
    }

    /// <summary>Verifies that three layers produce the correct cumulative sums.</summary>
    [Fact]
    public void ThreeLayers_CumulativeIsCorrect()
    {
        double[] x = [1.0, 2.0];
        double[][] ySets = [[1.0, 2.0], [3.0, 4.0], [5.0, 6.0]];
        // At x=1: 1+3+5=9, at x=2: 2+4+6=12 => max cumulative = 12
        var series = new StackedAreaSeries(x, ySets);
        var range = series.ComputeDataRange(null!);
        Assert.Equal(0.0, range.YMin);
        Assert.Equal(12.0, range.YMax);
        Assert.Equal(1.0, range.XMin);
        Assert.Equal(2.0, range.XMax);
    }
}
