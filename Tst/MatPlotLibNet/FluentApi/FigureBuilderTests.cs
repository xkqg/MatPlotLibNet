// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

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
            .WithBackground(Colors.Black)
            .Build();
        Assert.Equal(Colors.Black, figure.BackgroundColor);
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
                line.Color = Colors.Red;
                line.LineWidth = 3.0;
            })
            .Build();

        var series = (LineSeries)figure.SubPlots[0].Series[0];
        Assert.Equal(Colors.Red, series.Color);
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
            .WithBackground(Colors.White)
            .Plot([1.0], [2.0])
            .Scatter([3.0], [4.0])
            .Build();

        Assert.Equal("Test", figure.Title);
    }

    // --- GridSpec builder tests ---

    /// <summary>Verifies that WithGridSpec sets the GridSpec on the built Figure.</summary>
    [Fact]
    public void WithGridSpec_SetsGridSpecOnFigure()
    {
        var figure = Plt.Create()
            .WithGridSpec(3, 3)
            .Build();

        Assert.NotNull(figure.GridSpec);
        Assert.Equal(3, figure.GridSpec.Rows);
        Assert.Equal(3, figure.GridSpec.Cols);
    }

    /// <summary>Verifies that WithGridSpec with ratios stores them on the Figure.</summary>
    [Fact]
    public void WithGridSpec_WithRatios_StoresRatios()
    {
        var figure = Plt.Create()
            .WithGridSpec(2, 3, heightRatios: [1, 3], widthRatios: [1, 2, 1])
            .Build();

        Assert.Equal([1.0, 3.0], figure.GridSpec!.HeightRatios!);
        Assert.Equal([1.0, 2.0, 1.0], figure.GridSpec.WidthRatios!);
    }

    /// <summary>Verifies that AddSubPlot with GridPosition creates correct Axes.</summary>
    [Fact]
    public void AddSubPlot_WithGridPosition_CreatesCorrectAxes()
    {
        var figure = Plt.Create()
            .WithGridSpec(2, 2)
            .AddSubPlot(GridPosition.Single(0, 0), ax => ax.Plot([1.0], [2.0]))
            .AddSubPlot(GridPosition.Single(0, 1), ax => ax.Scatter([3.0], [4.0]))
            .Build();

        Assert.Equal(2, figure.SubPlots.Count);
        Assert.NotNull(figure.SubPlots[0].GridPosition);
        Assert.Equal(0, figure.SubPlots[0].GridPosition!.Value.ColStart);
        Assert.Equal(1, figure.SubPlots[1].GridPosition!.Value.ColStart);
    }

    /// <summary>Verifies that AddSubPlot with span coordinates creates spanning Axes.</summary>
    [Fact]
    public void AddSubPlot_WithSpanCoordinates_CreatesCorrectAxes()
    {
        var figure = Plt.Create()
            .WithGridSpec(3, 3)
            .AddSubPlot(new GridPosition(0, 1, 0, 3), ax => ax.Plot([1.0], [2.0]))
            .Build();

        var pos = figure.SubPlots[0].GridPosition!.Value;
        Assert.Equal(0, pos.RowStart);
        Assert.Equal(1, pos.RowEnd);
        Assert.Equal(0, pos.ColStart);
        Assert.Equal(3, pos.ColEnd);
    }

    // --- Shared axes builder tests ---

    /// <summary>Verifies that key-based ShareX resolves at Build time.</summary>
    [Fact]
    public void AddSubPlot_WithShareXKey_ResolvesAtBuild()
    {
        var figure = Plt.Create()
            .AddSubPlot(2, 1, 1, ax => ax.Plot([1.0], [2.0]), key: "price")
            .AddSubPlot(2, 1, 2, ax => ax.ShareX("price").Plot([3.0], [4.0]))
            .Build();

        Assert.Same(figure.SubPlots[0], figure.SubPlots[1].ShareXWith);
    }

    /// <summary>Verifies that key-based ShareY resolves at Build time.</summary>
    [Fact]
    public void AddSubPlot_WithShareYKey_ResolvesAtBuild()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 2, 1, ax => ax.Plot([1.0], [2.0]), key: "main")
            .AddSubPlot(1, 2, 2, ax => ax.ShareY("main").Plot([3.0], [4.0]))
            .Build();

        Assert.Same(figure.SubPlots[0], figure.SubPlots[1].ShareYWith);
    }

    // ── v1.10 chart-pack: configure null vs non-null branches ────────────────

    private static TreeNode SimpleTree() =>
        new() { Label = "root", Children = [new() { Label = "a", Value = 1.0 }, new() { Label = "b", Value = 2.0 }] };

    [Fact]
    public void Dendrogram_NullConfigure_AddsSeries()
    {
        var fig = Plt.Create().Dendrogram(SimpleTree()).Build();
        Assert.Single(fig.SubPlots[0].Series);
    }

    [Fact]
    public void Dendrogram_WithConfigure_InvokesCallback()
    {
        bool invoked = false;
        Plt.Create().Dendrogram(SimpleTree(), _ => invoked = true).Build();
        Assert.True(invoked);
    }

    [Fact]
    public void Clustermap_NullConfigure_AddsSeries()
    {
        var fig = Plt.Create().Clustermap(new double[,] { { 1, 2 }, { 3, 4 } }).Build();
        Assert.Single(fig.SubPlots[0].Series);
    }

    [Fact]
    public void Clustermap_WithConfigure_InvokesCallback()
    {
        bool invoked = false;
        Plt.Create().Clustermap(new double[,] { { 1, 2 }, { 3, 4 } }, _ => invoked = true).Build();
        Assert.True(invoked);
    }

    [Fact]
    public void PairGrid_NullConfigure_AddsSeries()
    {
        var fig = Plt.Create().PairGrid(new[] { new[] { 1.0, 2.0 }, new[] { 3.0, 4.0 } }).Build();
        Assert.Single(fig.SubPlots[0].Series);
    }

    [Fact]
    public void PairGrid_WithConfigure_InvokesCallback()
    {
        bool invoked = false;
        Plt.Create().PairGrid(new[] { new[] { 1.0, 2.0 }, new[] { 3.0, 4.0 } }, _ => invoked = true).Build();
        Assert.True(invoked);
    }

    [Fact]
    public void NetworkGraph_NullConfigure_AddsSeries()
    {
        var fig = Plt.Create()
            .NetworkGraph([new GraphNode("a"), new GraphNode("b")], [new GraphEdge("a", "b")])
            .Build();
        Assert.Single(fig.SubPlots[0].Series);
    }

    [Fact]
    public void NetworkGraph_WithConfigure_InvokesCallback()
    {
        bool invoked = false;
        Plt.Create()
            .NetworkGraph([new GraphNode("a"), new GraphNode("b")], [new GraphEdge("a", "b")],
                _ => invoked = true)
            .Build();
        Assert.True(invoked);
    }
}
