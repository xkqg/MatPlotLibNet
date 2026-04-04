// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.FluentApi;

public class AxesBuilderTests
{
    [Fact]
    public void WithTitle_SetsTitle()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.WithTitle("My Plot"))
            .Build();
        Assert.Equal("My Plot", figure.SubPlots[0].Title);
    }

    [Fact]
    public void SetXLabel_SetsXAxisLabel()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.SetXLabel("Time"))
            .Build();
        Assert.Equal("Time", figure.SubPlots[0].XAxis.Label);
    }

    [Fact]
    public void SetYLabel_SetsYAxisLabel()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.SetYLabel("Value"))
            .Build();
        Assert.Equal("Value", figure.SubPlots[0].YAxis.Label);
    }

    [Fact]
    public void SetXLim_SetsXAxisLimits()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.SetXLim(0, 100))
            .Build();
        Assert.Equal(0, figure.SubPlots[0].XAxis.Min);
        Assert.Equal(100, figure.SubPlots[0].XAxis.Max);
    }

    [Fact]
    public void SetYLim_SetsYAxisLimits()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.SetYLim(-50, 50))
            .Build();
        Assert.Equal(-50, figure.SubPlots[0].YAxis.Min);
        Assert.Equal(50, figure.SubPlots[0].YAxis.Max);
    }

    [Fact]
    public void SetXScale_SetsScale()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.SetXScale(AxisScale.Log))
            .Build();
        Assert.Equal(AxisScale.Log, figure.SubPlots[0].XAxis.Scale);
    }

    [Fact]
    public void ShowGrid_EnablesGrid()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.ShowGrid())
            .Build();
        Assert.True(figure.SubPlots[0].Grid.Visible);
    }

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

    [Fact]
    public void Pie_AddsPieSeries()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Pie([30.0, 70.0]))
            .Build();
        Assert.IsType<PieSeries>(figure.SubPlots[0].Series[0]);
    }

    [Fact]
    public void Heatmap_AddsHeatmapSeries()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Heatmap(new double[,] { { 1, 2 }, { 3, 4 } }))
            .Build();
        Assert.IsType<HeatmapSeries>(figure.SubPlots[0].Series[0]);
    }

    [Fact]
    public void BoxPlot_AddsBoxSeries()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.BoxPlot([[1.0, 2.0, 3.0]]))
            .Build();
        Assert.IsType<BoxSeries>(figure.SubPlots[0].Series[0]);
    }

    [Fact]
    public void Violin_AddsViolinSeries()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Violin([[1.0, 2.0, 3.0]]))
            .Build();
        Assert.IsType<ViolinSeries>(figure.SubPlots[0].Series[0]);
    }

    [Fact]
    public void Contour_AddsContourSeries()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Contour([1.0, 2.0], [1.0, 2.0], new double[,] { { 1, 2 }, { 3, 4 } }))
            .Build();
        Assert.IsType<ContourSeries>(figure.SubPlots[0].Series[0]);
    }

    [Fact]
    public void Stem_AddsStemSeries()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Stem([1.0, 2.0], [3.0, 4.0]))
            .Build();
        Assert.IsType<StemSeries>(figure.SubPlots[0].Series[0]);
    }
}
