// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.FluentApi;

/// <summary>Verifies fluent API for adding <see cref="RegressionSeries"/> via Axes and AxesBuilder.</summary>
public class RegressionBuilderTests
{
    private static readonly double[] X = [1.0, 2.0, 3.0, 4.0, 5.0];
    private static readonly double[] Y = [2.1, 3.9, 6.2, 7.8, 10.1];

    /// <summary>Axes.Regression adds a RegressionSeries to the series list.</summary>
    [Fact]
    public void Axes_Regression_AddsRegressionSeries()
    {
        var axes = new Axes();
        var series = axes.Regression(X, Y);
        Assert.IsType<RegressionSeries>(series);
        Assert.Contains(series, axes.Series);
    }

    /// <summary>Axes.Regression stores X and Y data.</summary>
    [Fact]
    public void Axes_Regression_StoresData()
    {
        var axes = new Axes();
        var series = axes.Regression(X, Y);
        Assert.Equal(X, series.XData);
        Assert.Equal(Y, series.YData);
    }

    /// <summary>AxesBuilder.Regression returns the builder for chaining.</summary>
    [Fact]
    public void AxesBuilder_Regression_ReturnsBuilder()
    {
        var builder = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Regression(X, Y));
        Assert.NotNull(builder);
    }

    /// <summary>JSON round-trip preserves RegressionSeries type tag "regression".</summary>
    [Fact]
    public void Regression_JsonRoundTrip_PreservesType()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Regression(X, Y))
            .Build();
        var json = new MatPlotLibNet.Serialization.ChartSerializer().ToJson(figure);
        Assert.Contains("\"type\":\"regression\"", json);

        var restored = new MatPlotLibNet.Serialization.ChartSerializer().FromJson(json);
        var series = restored.SubPlots[0].Series.OfType<RegressionSeries>().FirstOrDefault();
        Assert.NotNull(series);
        Assert.Equal(X, series.XData);
    }

    /// <summary>JSON round-trip preserves Degree when non-default.</summary>
    [Fact]
    public void Regression_JsonRoundTrip_PreservesDegree()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Regression(X, Y, s => s.Degree = 2))
            .Build();
        var json = new MatPlotLibNet.Serialization.ChartSerializer().ToJson(figure);
        var restored = new MatPlotLibNet.Serialization.ChartSerializer().FromJson(json);
        var series = restored.SubPlots[0].Series.OfType<RegressionSeries>().FirstOrDefault();
        Assert.Equal(2, series!.Degree);
    }
}
