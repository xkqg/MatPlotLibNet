// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.FluentApi;

/// <summary>Verifies fluent API for adding <see cref="KdeSeries"/> via Axes, AxesBuilder, and FigureBuilder.</summary>
public class KdeBuilderTests
{
    private static readonly double[] Data = [1.0, 2.0, 3.0, 4.0, 5.0];

    /// <summary>Axes.Kde adds a KdeSeries to the series list.</summary>
    [Fact]
    public void Axes_Kde_AddsKdeSeries()
    {
        var axes = new Axes();
        var series = axes.Kde(Data);
        Assert.IsType<KdeSeries>(series);
        Assert.Contains(series, axes.Series);
    }

    /// <summary>Axes.Kde stores the data array.</summary>
    [Fact]
    public void Axes_Kde_StoresData()
    {
        var axes = new Axes();
        var series = axes.Kde(Data);
        Assert.Equal(Data, series.Data);
    }

    /// <summary>AxesBuilder.Kde returns the builder for chaining.</summary>
    [Fact]
    public void AxesBuilder_Kde_ReturnsBuilder()
    {
        var builder = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Kde(Data));
        Assert.NotNull(builder);
    }

    /// <summary>FigureBuilder.Kde shortcut adds series to the default axes.</summary>
    [Fact]
    public void FigureBuilder_Kde_AddsToDefaultAxes()
    {
        var figure = Plt.Create()
            .Kde(Data)
            .Build();
        var series = figure.SubPlots[0].Series.OfType<KdeSeries>().FirstOrDefault();
        Assert.NotNull(series);
    }

    /// <summary>JSON round-trip preserves KdeSeries type tag "kde".</summary>
    [Fact]
    public void Kde_JsonRoundTrip_PreservesType()
    {
        var figure = Plt.Create().Kde(Data).Build();
        var json = new MatPlotLibNet.Serialization.ChartSerializer().ToJson(figure);
        Assert.Contains("\"type\":\"kde\"", json);

        var restored = new MatPlotLibNet.Serialization.ChartSerializer().FromJson(json);
        var series = restored.SubPlots[0].Series.OfType<KdeSeries>().FirstOrDefault();
        Assert.NotNull(series);
        Assert.Equal(Data, series.Data);
    }

    /// <summary>JSON round-trip preserves explicit Bandwidth.</summary>
    [Fact]
    public void Kde_JsonRoundTrip_PreservesBandwidth()
    {
        var figure = Plt.Create().Kde(Data, s => s.Bandwidth = 0.5).Build();
        var json = new MatPlotLibNet.Serialization.ChartSerializer().ToJson(figure);
        var restored = new MatPlotLibNet.Serialization.ChartSerializer().FromJson(json);
        var series = restored.SubPlots[0].Series.OfType<KdeSeries>().FirstOrDefault();
        Assert.Equal(0.5, series!.Bandwidth);
    }
}
