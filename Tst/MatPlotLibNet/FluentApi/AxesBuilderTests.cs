// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.FluentApi;

/// <summary>Verifies <see cref="AxesBuilder"/> fluent API methods.</summary>
public class AxesBuilderTests
{
    /// <summary>Verifies that WithTitle sets the axes title.</summary>
    [Fact]
    public void WithTitle_SetsTitle()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.WithTitle("My Plot"))
            .Build();
        Assert.Equal("My Plot", figure.SubPlots[0].Title);
    }

    /// <summary>Verifies that SetXLabel sets the X-axis label.</summary>
    [Fact]
    public void SetXLabel_SetsXAxisLabel()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.SetXLabel("Time"))
            .Build();
        Assert.Equal("Time", figure.SubPlots[0].XAxis.Label);
    }

    /// <summary>Verifies that SetYLabel sets the Y-axis label.</summary>
    [Fact]
    public void SetYLabel_SetsYAxisLabel()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.SetYLabel("Value"))
            .Build();
        Assert.Equal("Value", figure.SubPlots[0].YAxis.Label);
    }

    /// <summary>Verifies that SetXLim sets the X-axis min and max limits.</summary>
    [Fact]
    public void SetXLim_SetsXAxisLimits()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.SetXLim(0, 100))
            .Build();
        Assert.Equal(0, figure.SubPlots[0].XAxis.Min);
        Assert.Equal(100, figure.SubPlots[0].XAxis.Max);
    }

    /// <summary>Verifies that SetYLim sets the Y-axis min and max limits.</summary>
    [Fact]
    public void SetYLim_SetsYAxisLimits()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.SetYLim(-50, 50))
            .Build();
        Assert.Equal(-50, figure.SubPlots[0].YAxis.Min);
        Assert.Equal(50, figure.SubPlots[0].YAxis.Max);
    }

    /// <summary>Verifies that SetXScale sets the X-axis scale type.</summary>
    [Fact]
    public void SetXScale_SetsScale()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.SetXScale(AxisScale.Log))
            .Build();
        Assert.Equal(AxisScale.Log, figure.SubPlots[0].XAxis.Scale);
    }

    /// <summary>Verifies that ShowGrid enables grid visibility.</summary>
    [Fact]
    public void ShowGrid_EnablesGrid()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.ShowGrid())
            .Build();
        Assert.True(figure.SubPlots[0].Grid.Visible);
    }

    /// <summary>Verifies that Plot and Scatter add series to the axes.</summary>
    [Fact]
    public void Plot_AddsSeriesToAxes()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.Plot([1.0, 2.0], [3.0, 4.0]);
                ax.Scatter([5.0], [6.0]);
            })
            .Build();

        Assert.Equal(2, figure.SubPlots[0].Series.Count);
    }

    /// <summary>Verifies that all fluent methods can be chained together.</summary>
    [Fact]
    public void MethodChaining_Works()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.WithTitle("Chart")
                  .SetXLabel("X")
                  .SetYLabel("Y")
                  .SetXLim(0, 10)
                  .SetYLim(0, 100)
                  .ShowGrid()
                  .Plot([1.0], [2.0]);
            })
            .Build();

        var axes = figure.SubPlots[0];
        Assert.Equal("Chart", axes.Title);
        Assert.Equal("X", axes.XAxis.Label);
        Assert.True(axes.Grid.Visible);
    }

    /// <summary>Verifies that Pie adds a <see cref="PieSeries"/> to the axes.</summary>
    [Fact]
    public void Pie_AddsPieSeries()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Pie([30.0, 70.0]))
            .Build();
        Assert.IsType<PieSeries>(figure.SubPlots[0].Series[0]);
    }

    /// <summary>Verifies that Heatmap adds a <see cref="HeatmapSeries"/> to the axes.</summary>
    [Fact]
    public void Heatmap_AddsHeatmapSeries()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Heatmap(new double[,] { { 1, 2 }, { 3, 4 } }))
            .Build();
        Assert.IsType<HeatmapSeries>(figure.SubPlots[0].Series[0]);
    }

    /// <summary>Verifies that BoxPlot adds a <see cref="BoxSeries"/> to the axes.</summary>
    [Fact]
    public void BoxPlot_AddsBoxSeries()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.BoxPlot([[1.0, 2.0, 3.0]]))
            .Build();
        Assert.IsType<BoxSeries>(figure.SubPlots[0].Series[0]);
    }

    /// <summary>Verifies that Violin adds a <see cref="ViolinSeries"/> to the axes.</summary>
    [Fact]
    public void Violin_AddsViolinSeries()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Violin([[1.0, 2.0, 3.0]]))
            .Build();
        Assert.IsType<ViolinSeries>(figure.SubPlots[0].Series[0]);
    }

    /// <summary>Verifies that Contour adds a <see cref="ContourSeries"/> to the axes.</summary>
    [Fact]
    public void Contour_AddsContourSeries()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Contour([1.0, 2.0], [1.0, 2.0], new double[,] { { 1, 2 }, { 3, 4 } }))
            .Build();
        Assert.IsType<ContourSeries>(figure.SubPlots[0].Series[0]);
    }

    /// <summary>Verifies that Stem adds a <see cref="StemSeries"/> to the axes.</summary>
    [Fact]
    public void Stem_AddsStemSeries()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Stem([1.0, 2.0], [3.0, 4.0]))
            .Build();
        Assert.IsType<StemSeries>(figure.SubPlots[0].Series[0]);
    }

    /// <summary>Verifies that WithSecondaryYAxis configures a secondary Y-axis with its own series.</summary>
    [Fact]
    public void WithSecondaryYAxis_ConfiguresSecondaryAxis()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2.0], [3.0, 4.0])
                .WithSecondaryYAxis(sec => sec
                    .SetYLabel("Secondary")
                    .Plot([1.0, 2.0], [100.0, 200.0])))
            .Build();

        Assert.NotNull(figure.SubPlots[0].SecondaryYAxis);
        Assert.Equal("Secondary", figure.SubPlots[0].SecondaryYAxis!.Label);
        Assert.Single(figure.SubPlots[0].SecondarySeries);
    }

    /// <summary>Verifies that Annotate adds an annotation to the axes.</summary>
    [Fact]
    public void Annotate_AddsAnnotation()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2.0], [3.0, 4.0])
                .Annotate("peak", 2.0, 4.0))
            .Build();
        Assert.Single(figure.SubPlots[0].Annotations);
    }

    /// <summary>Verifies that AxHLine adds a horizontal reference line.</summary>
    [Fact]
    public void AxHLine_AddsReferenceLine()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2.0], [3.0, 4.0])
                .AxHLine(3.5))
            .Build();
        Assert.Single(figure.SubPlots[0].ReferenceLines);
    }

    /// <summary>Verifies that AxVSpan adds a vertical span region.</summary>
    [Fact]
    public void AxVSpan_AddsSpanRegion()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2.0], [3.0, 4.0])
                .AxVSpan(1.2, 1.8))
            .Build();
        Assert.Single(figure.SubPlots[0].Spans);
    }

    /// <summary>Verifies that Radar adds a <see cref="RadarSeries"/> to the axes.</summary>
    [Fact]
    public void Radar_AddsRadarSeries()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Radar(["A", "B", "C"], [1.0, 2.0, 3.0]))
            .Build();
        Assert.IsType<RadarSeries>(figure.SubPlots[0].Series[0]);
    }

    /// <summary>Verifies that SetBarMode sets the bar stacking mode on the axes.</summary>
    [Fact]
    public void SetBarMode_SetsStackedMode()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .SetBarMode(BarMode.Stacked)
                .Bar(["A", "B"], [10.0, 20.0])
                .Bar(["A", "B"], [5.0, 10.0]))
            .Build();
        Assert.Equal(BarMode.Stacked, figure.SubPlots[0].BarMode);
    }

    /// <summary>Verifies that Quiver adds a <see cref="QuiverSeries"/> to the axes.</summary>
    [Fact]
    public void Quiver_AddsQuiverSeries()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Quiver([1.0], [2.0], [0.5], [0.5]))
            .Build();
        Assert.IsType<QuiverSeries>(figure.SubPlots[0].Series[0]);
    }

    /// <summary>Verifies that Candlestick adds a <see cref="CandlestickSeries"/> to the axes.</summary>
    [Fact]
    public void Candlestick_AddsCandlestickSeries()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Candlestick([10.0], [15.0], [8.0], [13.0]))
            .Build();
        Assert.IsType<CandlestickSeries>(figure.SubPlots[0].Series[0]);
    }

    /// <summary>Verifies that ErrorBar adds an <see cref="ErrorBarSeries"/> to the axes.</summary>
    [Fact]
    public void ErrorBar_AddsErrorBarSeries()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.ErrorBar([1.0, 2.0], [3.0, 4.0], [0.1, 0.1], [0.2, 0.2]))
            .Build();
        Assert.IsType<ErrorBarSeries>(figure.SubPlots[0].Series[0]);
    }

    /// <summary>Verifies that Step adds a <see cref="StepSeries"/> to the axes.</summary>
    [Fact]
    public void Step_AddsStepSeries()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Step([1.0, 2.0], [3.0, 4.0]))
            .Build();
        Assert.IsType<StepSeries>(figure.SubPlots[0].Series[0]);
    }

    /// <summary>Verifies that FillBetween adds an <see cref="AreaSeries"/> to the axes.</summary>
    [Fact]
    public void FillBetween_AddsAreaSeries()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.FillBetween([1.0, 2.0], [3.0, 4.0]))
            .Build();
        Assert.IsType<AreaSeries>(figure.SubPlots[0].Series[0]);
    }
}
