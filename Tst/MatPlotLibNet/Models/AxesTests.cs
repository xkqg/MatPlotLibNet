// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models;

/// <summary>Verifies <see cref="Axes"/> behavior.</summary>
public class AxesTests
{
    /// <summary>Verifies that a default Axes has an empty series collection.</summary>
    [Fact]
    public void DefaultAxes_HasEmptySeries()
    {
        var axes = new Axes();
        Assert.Empty(axes.Series);
    }

    /// <summary>Verifies that a default Axes has non-null XAxis and YAxis.</summary>
    [Fact]
    public void DefaultAxes_HasAxes()
    {
        var axes = new Axes();
        Assert.NotNull(axes.XAxis);
        Assert.NotNull(axes.YAxis);
    }

    /// <summary>Verifies that Plot adds a LineSeries to the series collection.</summary>
    [Fact]
    public void Plot_AddsLineSeries()
    {
        var axes = new Axes();
        var series = axes.Plot([1.0, 2.0], [3.0, 4.0]);
        Assert.IsType<LineSeries>(series);
        Assert.Single(axes.Series);
    }

    /// <summary>Verifies that Plot stores X and Y data arrays correctly.</summary>
    [Fact]
    public void Plot_StoresDataCorrectly()
    {
        var axes = new Axes();
        double[] x = [1, 2, 3], y = [4, 5, 6];
        var series = axes.Plot(x, y);
        Assert.Equal(x, series.XData);
        Assert.Equal(y, series.YData);
    }

    /// <summary>Verifies that Plot throws when X and Y arrays have different lengths.</summary>
    [Fact]
    public void Plot_ThrowsOnMismatchedArrayLengths()
    {
        var axes = new Axes();
        Assert.Throws<ArgumentException>(() =>
            axes.Plot([1.0, 2.0], [3.0, 4.0, 5.0]));
    }

    /// <summary>Verifies that Scatter adds a ScatterSeries to the series collection.</summary>
    [Fact]
    public void Scatter_AddsScatterSeries()
    {
        var axes = new Axes();
        var series = axes.Scatter([1.0], [2.0]);
        Assert.IsType<ScatterSeries>(series);
        Assert.Single(axes.Series);
    }

    /// <summary>Verifies that Scatter throws when X and Y arrays have different lengths.</summary>
    [Fact]
    public void Scatter_ThrowsOnMismatchedArrayLengths()
    {
        var axes = new Axes();
        Assert.Throws<ArgumentException>(() =>
            axes.Scatter([1.0], [2.0, 3.0]));
    }

    /// <summary>Verifies that Bar adds a BarSeries to the series collection.</summary>
    [Fact]
    public void Bar_AddsBarSeries()
    {
        var axes = new Axes();
        var series = axes.Bar(["A", "B"], [1.0, 2.0]);
        Assert.IsType<BarSeries>(series);
        Assert.Single(axes.Series);
    }

    /// <summary>Verifies that Bar throws when labels and values arrays have different lengths.</summary>
    [Fact]
    public void Bar_ThrowsOnMismatchedLengths()
    {
        var axes = new Axes();
        Assert.Throws<ArgumentException>(() =>
            axes.Bar(["A"], [1.0, 2.0]));
    }

    /// <summary>Verifies that Barh adds a horizontal BarSeries.</summary>
    [Fact]
    public void Barh_AddsHorizontalBarSeries()
    {
        var axes = new Axes();
        var series = axes.Barh(["A"], [1.0]);
        Assert.IsType<BarSeries>(series);
        Assert.Equal(BarOrientation.Horizontal, series.Orientation);
    }

    /// <summary>Verifies that Hist adds a HistogramSeries to the series collection.</summary>
    [Fact]
    public void Hist_AddsHistogramSeries()
    {
        var axes = new Axes();
        var series = axes.Hist([1.0, 2.0, 3.0, 4.0]);
        Assert.IsType<HistogramSeries>(series);
    }

    /// <summary>Verifies that Hist defaults to 10 bins.</summary>
    [Fact]
    public void Hist_DefaultBinsIs10()
    {
        var axes = new Axes();
        var series = axes.Hist([1.0, 2.0]);
        Assert.Equal(10, series.Bins);
    }

    /// <summary>Verifies that Pie adds a PieSeries to the series collection.</summary>
    [Fact]
    public void Pie_AddsPieSeries()
    {
        var axes = new Axes();
        var series = axes.Pie([30.0, 70.0]);
        Assert.IsType<PieSeries>(series);
    }

    /// <summary>Verifies that Pie stores the provided labels.</summary>
    [Fact]
    public void Pie_WithLabels()
    {
        var axes = new Axes();
        var series = axes.Pie([30.0, 70.0], ["A", "B"]);
        Assert.Equal(["A", "B"], series.Labels!);
    }

    /// <summary>Verifies that Heatmap adds a HeatmapSeries to the series collection.</summary>
    [Fact]
    public void Heatmap_AddsHeatmapSeries()
    {
        var axes = new Axes();
        var data = new double[,] { { 1, 2 }, { 3, 4 } };
        var series = axes.Heatmap(data);
        Assert.IsType<HeatmapSeries>(series);
    }

    /// <summary>Verifies that BoxPlot adds a BoxSeries to the series collection.</summary>
    [Fact]
    public void BoxPlot_AddsBoxSeries()
    {
        var axes = new Axes();
        var series = axes.BoxPlot([[1.0, 2.0, 3.0]]);
        Assert.IsType<BoxSeries>(series);
    }

    /// <summary>Verifies that Violin adds a ViolinSeries to the series collection.</summary>
    [Fact]
    public void Violin_AddsViolinSeries()
    {
        var axes = new Axes();
        var series = axes.Violin([[1.0, 2.0, 3.0]]);
        Assert.IsType<ViolinSeries>(series);
    }

    /// <summary>Verifies that Stem adds a StemSeries to the series collection.</summary>
    [Fact]
    public void Stem_AddsStemSeries()
    {
        var axes = new Axes();
        var series = axes.Stem([1.0, 2.0], [3.0, 4.0]);
        Assert.IsType<StemSeries>(series);
    }

    /// <summary>Verifies that multiple series are maintained in insertion order.</summary>
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

    /// <summary>Verifies that the Title property can be set and retrieved.</summary>
    [Fact]
    public void Title_CanBeSet()
    {
        var axes = new Axes { Title = "My Plot" };
        Assert.Equal("My Plot", axes.Title);
    }

    /// <summary>Verifies that the Series collection is read-only.</summary>
    [Fact]
    public void Series_IsReadOnly()
    {
        var axes = new Axes();
        Assert.IsAssignableFrom<IReadOnlyList<ISeries>>(axes.Series);
    }
}
