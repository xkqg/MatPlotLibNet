// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="Histogram2DSeries"/> construction, data range, serialization, and rendering.</summary>
public class Histogram2DSeriesTests
{
    private static readonly double[] SampleX = [1.0, 2.0, 3.0, 4.0, 5.0, 1.5, 2.5, 3.5, 4.5, 2.0];
    private static readonly double[] SampleY = [10.0, 20.0, 30.0, 40.0, 50.0, 15.0, 25.0, 35.0, 45.0, 20.0];

    /// <summary>Verifies that the constructor stores X and Y data.</summary>
    [Fact]
    public void Constructor_StoresData()
    {
        var series = new Histogram2DSeries(SampleX, SampleY);
        Assert.Same(SampleX, series.X);
        Assert.Same(SampleY, series.Y);
    }

    /// <summary>Verifies that ComputeDataRange returns the min/max of the input X and Y data.</summary>
    [Fact]
    public void ComputeDataRange_MatchesInputRange()
    {
        var series = new Histogram2DSeries(SampleX, SampleY);
        var range = series.ComputeDataRange(null!);
        Assert.Equal(1.0, range.XMin);
        Assert.Equal(5.0, range.XMax);
        Assert.Equal(10.0, range.YMin);
        Assert.Equal(50.0, range.YMax);
    }

    /// <summary>Verifies that the DTO type is "histogram2d".</summary>
    [Fact]
    public void ToSeriesDto_TypeIsHistogram2d()
    {
        var series = new Histogram2DSeries(SampleX, SampleY);
        var dto = series.ToSeriesDto();
        Assert.Equal("histogram2d", dto.Type);
    }

    /// <summary>Verifies that the default number of bins is 20 for both axes.</summary>
    [Fact]
    public void DefaultBins_Is20()
    {
        var series = new Histogram2DSeries(SampleX, SampleY);
        Assert.Equal(20, series.BinsX);
        Assert.Equal(20, series.BinsY);
    }

    /// <summary>Verifies that GetColorBarRange returns non-negative values.</summary>
    [Fact]
    public void GetColorBarRange_ReturnsNonNegative()
    {
        var series = new Histogram2DSeries(SampleX, SampleY, 5, 5);
        var (min, max) = series.GetColorBarRange();
        Assert.True(min >= 0);
        Assert.True(max >= min);
    }

    /// <summary>Verifies that JSON round-trip preserves data and bin configuration.</summary>
    [Fact]
    public void RoundTrip_PreservesData()
    {
        var fig = new Figure();
        var axes = fig.AddSubPlot();
        var h2d = axes.Histogram2D(SampleX, SampleY, 10);
        h2d.BinsY = 15;

        var serializer = new ChartSerializer();
        var json = serializer.ToJson(fig);
        var restored = serializer.FromJson(json);

        var restoredSeries = restored.SubPlots[0].Series.OfType<Histogram2DSeries>().Single();
        Assert.Equal(SampleX.Length, restoredSeries.X.Length);
        Assert.Equal(SampleY.Length, restoredSeries.Y.Length);
        Assert.Equal(10, restoredSeries.BinsX);
        Assert.Equal(15, restoredSeries.BinsY);
        Assert.Equal(SampleX[0], restoredSeries.X[0]);
        Assert.Equal(SampleY[0], restoredSeries.Y[0]);
    }

    /// <summary>Verifies that rendering produces SVG output containing rect elements.</summary>
    [Fact]
    public void Render_ProducesSvg()
    {
        var svg = Plt.Create()
            .Histogram2D(SampleX, SampleY, 5)
            .ToSvg();

        Assert.Contains("rect", svg);
    }
}
