// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models;

public class AxesTests
{
    [Fact]
    public void DefaultAxes_HasEmptySeries()
    {
        var axes = new Axes();
        Assert.Empty(axes.Series);
    }

    [Fact]
    public void DefaultAxes_HasAxes()
    {
        var axes = new Axes();
        Assert.NotNull(axes.XAxis);
        Assert.NotNull(axes.YAxis);
    }

    [Fact]
    public void Plot_AddsLineSeries()
    {
        var axes = new Axes();
        var series = axes.Plot([1.0, 2.0], [3.0, 4.0]);
        Assert.IsType<LineSeries>(series);
        Assert.Single(axes.Series);
    }

    [Fact]
    public void Plot_StoresDataCorrectly()
    {
        var axes = new Axes();
        double[] x = [1, 2, 3], y = [4, 5, 6];
        var series = axes.Plot(x, y);
        Assert.Equal(x, series.XData);
        Assert.Equal(y, series.YData);
    }

    [Fact]
    public void Plot_ThrowsOnMismatchedArrayLengths()
    {
        var axes = new Axes();
        Assert.Throws<ArgumentException>(() =>
            axes.Plot([1.0, 2.0], [3.0, 4.0, 5.0]));
    }

    [Fact]
    public void Scatter_AddsScatterSeries()
    {
        var axes = new Axes();
        var series = axes.Scatter([1.0], [2.0]);
        Assert.IsType<ScatterSeries>(series);
        Assert.Single(axes.Series);
    }

    [Fact]
    public void Scatter_ThrowsOnMismatchedArrayLengths()
    {
        var axes = new Axes();
        Assert.Throws<ArgumentException>(() =>
            axes.Scatter([1.0], [2.0, 3.0]));
    }

    [Fact]
    public void Bar_AddsBarSeries()
    {
        var axes = new Axes();
        var series = axes.Bar(["A", "B"], [1.0, 2.0]);
        Assert.IsType<BarSeries>(series);
        Assert.Single(axes.Series);
    }

    [Fact]
    public void Bar_ThrowsOnMismatchedLengths()
    {
        var axes = new Axes();
        Assert.Throws<ArgumentException>(() =>
            axes.Bar(["A"], [1.0, 2.0]));
    }

    [Fact]
    public void Barh_AddsHorizontalBarSeries()
    {
        var axes = new Axes();
        var series = axes.Barh(["A"], [1.0]);
        Assert.IsType<BarSeries>(series);
        Assert.Equal(BarOrientation.Horizontal, series.Orientation);
    }

    [Fact]
    public void Hist_AddsHistogramSeries()
    {
        var axes = new Axes();
        var series = axes.Hist([1.0, 2.0, 3.0, 4.0]);
        Assert.IsType<HistogramSeries>(series);
    }

    [Fact]
    public void Hist_DefaultBinsIs10()
    {
        var axes = new Axes();
        var series = axes.Hist([1.0, 2.0]);
        Assert.Equal(10, series.Bins);
    }

    [Fact]
    public void Pie_AddsPieSeries()
    {
        var axes = new Axes();
        var series = axes.Pie([30.0, 70.0]);
        Assert.IsType<PieSeries>(series);
    }

    [Fact]
    public void Pie_WithLabels()
    {
        var axes = new Axes();
        var series = axes.Pie([30.0, 70.0], ["A", "B"]);
        Assert.Equal(["A", "B"], series.Labels);
    }

    [Fact]
    public void Heatmap_AddsHeatmapSeries()
    {
        var axes = new Axes();
        var data = new double[,] { { 1, 2 }, { 3, 4 } };
        var series = axes.Heatmap(data);
        Assert.IsType<HeatmapSeries>(series);
    }

    [Fact]
    public void BoxPlot_AddsBoxSeries()
    {
        var axes = new Axes();
        var series = axes.BoxPlot([[1.0, 2.0, 3.0]]);
        Assert.IsType<BoxSeries>(series);
    }

    [Fact]
    public void Violin_AddsViolinSeries()
    {
        var axes = new Axes();
        var series = axes.Violin([[1.0, 2.0, 3.0]]);
        Assert.IsType<ViolinSeries>(series);
    }

    [Fact]
    public void Stem_AddsStemSeries()
    {
        var axes = new Axes();
        var series = axes.Stem([1.0, 2.0], [3.0, 4.0]);
        Assert.IsType<StemSeries>(series);
    }

    [Fact]
    public void MultipleSeries_MaintainOrder()
    {
        var axes = new Axes();
        axes.Plot([1.0], [1.0]);
        axes.Scatter([2.0], [2.0]);
        axes.Bar(["A"], [3.0]);
        Assert.Equal(3, axes.Series.Count);
        Assert.IsType<LineSeries>(axes.Series[0]);
        Assert.IsType<ScatterSeries>(axes.Series[1]);
        Assert.IsType<BarSeries>(axes.Series[2]);
    }

    [Fact]
    public void Title_CanBeSet()
    {
        var axes = new Axes { Title = "My Plot" };
        Assert.Equal("My Plot", axes.Title);
    }

    [Fact]
    public void Series_IsReadOnly()
    {
        var axes = new Axes();
        Assert.IsAssignableFrom<IReadOnlyList<ISeries>>(axes.Series);
    }
}
