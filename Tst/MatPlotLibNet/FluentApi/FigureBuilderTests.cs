// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.FluentApi;

/// <summary>Verifies <see cref="FigureBuilder"/> behavior.</summary>
public class FigureBuilderTests
{
    /// <summary>Verifies that Build produces a figure with default dimensions.</summary>
    [Fact]
    public void Build_ProducesDefaultFigure()
    {
        var figure = Plt.Create().Build();
        Assert.NotNull(figure);
        Assert.Equal(800, figure.Width);
    }

    /// <summary>Verifies that WithTitle sets the figure title.</summary>
    [Fact]
    public void WithTitle_SetsTitle()
    {
        var figure = Plt.Create()
            .WithTitle("Hello")
            .Build();
        Assert.Equal("Hello", figure.Title);
    }

    /// <summary>Verifies that WithSize sets the figure width and height.</summary>
    [Fact]
    public void WithSize_SetsDimensions()
    {
        var figure = Plt.Create()
            .WithSize(1200, 900)
            .Build();
        Assert.Equal(1200, figure.Width);
        Assert.Equal(900, figure.Height);
    }

    /// <summary>Verifies that WithDpi sets the figure DPI.</summary>
    [Fact]
    public void WithDpi_SetsDpi()
    {
        var figure = Plt.Create()
            .WithDpi(150)
            .Build();
        Assert.Equal(150, figure.Dpi);
    }

    /// <summary>Verifies that WithTheme sets the figure theme.</summary>
    [Fact]
    public void WithTheme_SetsTheme()
    {
        var figure = Plt.Create()
            .WithTheme(Theme.Dark)
            .Build();
        Assert.Same(Theme.Dark, figure.Theme);
    }

    /// <summary>Verifies that WithBackground sets the figure background color.</summary>
    [Fact]
    public void WithBackground_SetsColor()
    {
        var figure = Plt.Create()
            .WithBackground(Color.Black)
            .Build();
        Assert.Equal(Color.Black, figure.BackgroundColor);
    }

    /// <summary>Verifies that Plot creates default axes with a <see cref="LineSeries"/>.</summary>
    [Fact]
    public void Plot_CreatesDefaultAxesWithLineSeries()
    {
        var figure = Plt.Create()
            .Plot([1.0, 2.0], [3.0, 4.0])
            .Build();
        Assert.Single(figure.SubPlots);
        Assert.Single(figure.SubPlots[0].Series);
        Assert.IsType<LineSeries>(figure.SubPlots[0].Series[0]);
    }

    /// <summary>Verifies that Plot with a configure callback applies series settings.</summary>
    [Fact]
    public void Plot_WithConfigure_AppliesSettings()
    {
        var figure = Plt.Create()
            .Plot([1.0], [2.0], line =>
            {
                line.Color = Color.Red;
                line.LineWidth = 3.0;
            })
            .Build();

        var series = (LineSeries)figure.SubPlots[0].Series[0];
        Assert.Equal(Color.Red, series.Color);
        Assert.Equal(3.0, series.LineWidth);
    }

    /// <summary>Verifies that Scatter creates a <see cref="ScatterSeries"/>.</summary>
    [Fact]
    public void Scatter_CreatesScatterSeries()
    {
        var figure = Plt.Create()
            .Scatter([1.0], [2.0])
            .Build();
        Assert.IsType<ScatterSeries>(figure.SubPlots[0].Series[0]);
    }

    /// <summary>Verifies that Bar creates a <see cref="BarSeries"/>.</summary>
    [Fact]
    public void Bar_CreatesBarSeries()
    {
        var figure = Plt.Create()
            .Bar(["A", "B"], [1.0, 2.0])
            .Build();
        Assert.IsType<BarSeries>(figure.SubPlots[0].Series[0]);
    }

    /// <summary>Verifies that Hist creates a <see cref="HistogramSeries"/>.</summary>
    [Fact]
    public void Hist_CreatesHistogramSeries()
    {
        var figure = Plt.Create()
            .Hist([1.0, 2.0, 3.0])
            .Build();
        Assert.IsType<HistogramSeries>(figure.SubPlots[0].Series[0]);
    }

    /// <summary>Verifies that Pie creates a <see cref="PieSeries"/>.</summary>
    [Fact]
    public void Pie_CreatesPieSeries()
    {
        var figure = Plt.Create()
            .Pie([30.0, 70.0])
            .Build();
        Assert.IsType<PieSeries>(figure.SubPlots[0].Series[0]);
    }

    /// <summary>Verifies that multiple Plot calls add series to the same axes.</summary>
    [Fact]
    public void MultiplePlotCalls_AddSeriesToSameAxes()
    {
        var figure = Plt.Create()
            .Plot([1.0], [1.0])
            .Plot([2.0], [2.0])
            .Build();
        Assert.Single(figure.SubPlots);
        Assert.Equal(2, figure.SubPlots[0].Series.Count);
    }

    /// <summary>Verifies that AddSubPlot creates multiple axes in the figure.</summary>
    [Fact]
    public void AddSubPlot_CreatesMultipleAxes()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 2, 1, ax => ax.Plot([1.0], [2.0]))
            .AddSubPlot(1, 2, 2, ax => ax.Scatter([3.0], [4.0]))
            .Build();
        Assert.Equal(2, figure.SubPlots.Count);
    }

    /// <summary>Verifies that AddSubPlot applies the configure callback to the axes.</summary>
    [Fact]
    public void AddSubPlot_ConfigureCallback_IsApplied()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.WithTitle("Sub 1")
                  .SetXLabel("X")
                  .SetYLabel("Y")
                  .Plot([1.0], [2.0]);
            })
            .Build();

        var axes = figure.SubPlots[0];
        Assert.Equal("Sub 1", axes.Title);
        Assert.Equal("X", axes.XAxis.Label);
        Assert.Equal("Y", axes.YAxis.Label);
        Assert.Single(axes.Series);
    }

    /// <summary>Verifies that all fluent methods return the builder for chaining.</summary>
    [Fact]
    public void MethodChaining_AllMethodsReturnBuilder()
    {
        var figure = Plt.Create()
            .WithTitle("Test")
            .WithSize(800, 600)
            .WithDpi(150)
            .WithTheme(Theme.Dark)
            .WithBackground(Color.White)
            .Plot([1.0], [2.0])
            .Scatter([3.0], [4.0])
            .Build();

        Assert.Equal("Test", figure.Title);
    }
}
