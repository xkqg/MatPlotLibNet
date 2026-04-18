// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="RegressionSeries"/> default properties and serialization.</summary>
public class RegressionSeriesTests
{
    /// <summary>Constructor stores X and Y data.</summary>
    [Fact]
    public void Constructor_StoresXAndYData()
    {
        double[] x = [1.0, 2.0, 3.0];
        double[] y = [1.0, 2.0, 3.0];
        var series = new RegressionSeries(x, y);
        Assert.Equal(x, series.XData);
        Assert.Equal(y, series.YData);
    }

    /// <summary>Degree defaults to 1 (linear).</summary>
    [Fact]
    public void Degree_DefaultsTo1()
    {
        var series = new RegressionSeries([1.0], [1.0]);
        Assert.Equal(1, series.Degree);
    }

    /// <summary>ShowConfidence defaults to false.</summary>
    [Fact]
    public void ShowConfidence_DefaultsToFalse()
    {
        var series = new RegressionSeries([1.0], [1.0]);
        Assert.False(series.ShowConfidence);
    }

    /// <summary>ConfidenceLevel defaults to 0.95.</summary>
    [Fact]
    public void ConfidenceLevel_DefaultsTo0p95()
    {
        var series = new RegressionSeries([1.0], [1.0]);
        Assert.Equal(0.95, series.ConfidenceLevel);
    }

    /// <summary>LineWidth defaults to 2.0.</summary>
    [Fact]
    public void LineWidth_DefaultsTo2p0()
    {
        var series = new RegressionSeries([1.0], [1.0]);
        Assert.Equal(2.0, series.LineWidth);
    }

    /// <summary>BandAlpha defaults to 0.2.</summary>
    [Fact]
    public void BandAlpha_DefaultsTo0p2()
    {
        var series = new RegressionSeries([1.0], [1.0]);
        Assert.Equal(0.2, series.BandAlpha);
    }

    /// <summary>BandColor defaults to null.</summary>
    [Fact]
    public void BandColor_DefaultsToNull()
    {
        var series = new RegressionSeries([1.0], [1.0]);
        Assert.Null(series.BandColor);
    }

    /// <summary>ToSeriesDto returns type "regression".</summary>
    [Fact]
    public void ToSeriesDto_ReturnsTypeRegression()
    {
        var series = new RegressionSeries([1.0], [1.0]);
        Assert.Equal("regression", series.ToSeriesDto().Type);
    }

}
