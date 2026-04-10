// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.FluentApi;

/// <summary>Verifies fluent API for adding <see cref="ContourfSeries"/> via Axes, AxesBuilder, and FigureBuilder.</summary>
public class ContourfBuilderTests
{
    private static readonly double[] X = [0.0, 1.0, 2.0];
    private static readonly double[] Y = [0.0, 1.0, 2.0];
    private static readonly double[,] Z = { { 0, 1, 2 }, { 1, 2, 3 }, { 2, 3, 4 } };

    /// <summary>Axes.Contourf adds a ContourfSeries to the series list.</summary>
    [Fact]
    public void Axes_Contourf_AddsContourfSeries()
    {
        var axes = new Axes();
        var series = axes.Contourf(X, Y, Z);
        Assert.IsType<ContourfSeries>(series);
        Assert.Contains(series, axes.Series);
    }

    /// <summary>Axes.Contourf returns the series with the correct data.</summary>
    [Fact]
    public void Axes_Contourf_StoresData()
    {
        var axes = new Axes();
        var series = axes.Contourf(X, Y, Z);
        Assert.Equal(X, series.XData);
        Assert.Equal(Y, series.YData);
        Assert.Equal(Z, series.ZData);
    }

    /// <summary>AxesBuilder.Contourf returns the builder for chaining.</summary>
    [Fact]
    public void AxesBuilder_Contourf_ReturnsBuilder()
    {
        var figure = Plt.Create();
        var builder = figure.AddSubPlot(1, 1, 1, ax => ax.Contourf(X, Y, Z));
        Assert.NotNull(builder);
    }

    /// <summary>FigureBuilder.Contourf shortcut adds series to the default axes.</summary>
    [Fact]
    public void FigureBuilder_Contourf_AddsToDefaultAxes()
    {
        var figure = Plt.Create()
            .Contourf(X, Y, Z)
            .Build();
        var series = figure.SubPlots[0].Series.OfType<ContourfSeries>().FirstOrDefault();
        Assert.NotNull(series);
    }

    /// <summary>JSON round-trip preserves ContourfSeries type tag "contourf".</summary>
    [Fact]
    public void Contourf_JsonRoundTrip_PreservesType()
    {
        var figure = Plt.Create()
            .Contourf(X, Y, Z)
            .Build();

        var json = new MatPlotLibNet.Serialization.ChartSerializer().ToJson(figure);
        Assert.Contains("\"type\":\"contourf\"", json);

        var restored = new MatPlotLibNet.Serialization.ChartSerializer().FromJson(json);
        var series = restored.SubPlots[0].Series.OfType<ContourfSeries>().FirstOrDefault();
        Assert.NotNull(series);
    }
}
