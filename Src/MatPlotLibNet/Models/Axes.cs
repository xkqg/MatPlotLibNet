// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models;

/// <summary>Represents a set of axes within a figure, onto which data series are plotted.</summary>
public sealed class Axes
{
    /// <summary>Gets or sets the axes title.</summary>
    public string? Title { get; set; }

    /// <summary>Gets the X-axis configuration.</summary>
    public Axis XAxis { get; } = new();

    /// <summary>Gets the Y-axis configuration.</summary>
    public Axis YAxis { get; } = new();

    /// <summary>Gets or sets the legend configuration for this axes.</summary>
    public Legend Legend { get; set; } = new();

    /// <summary>Gets or sets the grid style configuration for this axes.</summary>
    public GridStyle Grid { get; set; } = new();

    /// <summary>Gets the number of rows in the subplot grid layout.</summary>
    public int GridRows { get; internal set; }

    /// <summary>Gets the number of columns in the subplot grid layout.</summary>
    public int GridCols { get; internal set; }

    /// <summary>Gets the one-based index of this axes within the subplot grid.</summary>
    public int GridIndex { get; internal set; }

    /// <summary>Gets the collection of data series plotted on this axes.</summary>
    public IReadOnlyList<ISeries> Series => _series;
    private readonly List<ISeries> _series = [];

    /// <summary>Adds a line series from the given X and Y data arrays.</summary>
    /// <param name="x">The X-axis data values.</param>
    /// <param name="y">The Y-axis data values.</param>
    /// <returns>The newly created <see cref="LineSeries"/> for further configuration.</returns>
    public LineSeries Plot(double[] x, double[] y)
    {
        ValidateMatchingLengths(x.Length, y.Length);
        var series = new LineSeries(x, y);
        _series.Add(series);
        return series;
    }

    /// <summary>Adds a scatter series from the given X and Y data arrays.</summary>
    /// <param name="x">The X-axis data values.</param>
    /// <param name="y">The Y-axis data values.</param>
    /// <returns>The newly created <see cref="ScatterSeries"/> for further configuration.</returns>
    public ScatterSeries Scatter(double[] x, double[] y)
    {
        ValidateMatchingLengths(x.Length, y.Length);
        var series = new ScatterSeries(x, y);
        _series.Add(series);
        return series;
    }

    /// <summary>Adds a vertical bar series from the given categories and values.</summary>
    /// <param name="categories">The category labels for each bar.</param>
    /// <param name="values">The numeric values for each bar.</param>
    /// <returns>The newly created <see cref="BarSeries"/> for further configuration.</returns>
    public BarSeries Bar(string[] categories, double[] values)
    {
        ValidateMatchingLengths(categories.Length, values.Length);
        var series = new BarSeries(categories, values);
        _series.Add(series);
        return series;
    }

    /// <summary>Adds a horizontal bar series from the given categories and values.</summary>
    /// <param name="categories">The category labels for each bar.</param>
    /// <param name="values">The numeric values for each bar.</param>
    /// <returns>The newly created <see cref="BarSeries"/> with horizontal orientation.</returns>
    public BarSeries Barh(string[] categories, double[] values)
    {
        ValidateMatchingLengths(categories.Length, values.Length);
        var series = new BarSeries(categories, values) { Orientation = BarOrientation.Horizontal };
        _series.Add(series);
        return series;
    }

    /// <summary>Adds a histogram series from the given data array.</summary>
    /// <param name="data">The raw data values to bin into a histogram.</param>
    /// <param name="bins">The number of histogram bins.</param>
    /// <returns>The newly created <see cref="HistogramSeries"/> for further configuration.</returns>
    public HistogramSeries Hist(double[] data, int bins = 10)
    {
        var series = new HistogramSeries(data) { Bins = bins };
        _series.Add(series);
        return series;
    }

    /// <summary>Adds a pie chart series from the given slice sizes.</summary>
    /// <param name="sizes">The numeric sizes of each pie slice.</param>
    /// <param name="labels">Optional labels for each pie slice.</param>
    /// <returns>The newly created <see cref="PieSeries"/> for further configuration.</returns>
    public PieSeries Pie(double[] sizes, string[]? labels = null)
    {
        var series = new PieSeries(sizes) { Labels = labels };
        _series.Add(series);
        return series;
    }

    /// <summary>Adds a heatmap series from the given two-dimensional data array.</summary>
    /// <param name="data">The 2D data matrix to render as a heatmap.</param>
    /// <returns>The newly created <see cref="HeatmapSeries"/> for further configuration.</returns>
    public HeatmapSeries Heatmap(double[,] data)
    {
        var series = new HeatmapSeries(data);
        _series.Add(series);
        return series;
    }

    /// <summary>Adds a box plot series from the given array of datasets.</summary>
    /// <param name="datasets">An array of datasets, each containing the values for one box.</param>
    /// <returns>The newly created <see cref="BoxSeries"/> for further configuration.</returns>
    public BoxSeries BoxPlot(double[][] datasets)
    {
        var series = new BoxSeries(datasets);
        _series.Add(series);
        return series;
    }

    /// <summary>Adds a violin plot series from the given array of datasets.</summary>
    /// <param name="datasets">An array of datasets, each containing the values for one violin.</param>
    /// <returns>The newly created <see cref="ViolinSeries"/> for further configuration.</returns>
    public ViolinSeries Violin(double[][] datasets)
    {
        var series = new ViolinSeries(datasets);
        _series.Add(series);
        return series;
    }

    /// <summary>Adds a contour series from the given X, Y, and Z data.</summary>
    /// <param name="x">The X-axis grid coordinates.</param>
    /// <param name="y">The Y-axis grid coordinates.</param>
    /// <param name="z">The 2D matrix of Z values at each grid point.</param>
    /// <returns>The newly created <see cref="ContourSeries"/> for further configuration.</returns>
    public ContourSeries Contour(double[] x, double[] y, double[,] z)
    {
        var series = new ContourSeries(x, y, z);
        _series.Add(series);
        return series;
    }

    /// <summary>Adds a stem plot series from the given X and Y data arrays.</summary>
    /// <param name="x">The X-axis data values.</param>
    /// <param name="y">The Y-axis data values.</param>
    /// <returns>The newly created <see cref="StemSeries"/> for further configuration.</returns>
    public StemSeries Stem(double[] x, double[] y)
    {
        ValidateMatchingLengths(x.Length, y.Length);
        var series = new StemSeries(x, y);
        _series.Add(series);
        return series;
    }

    private static void ValidateMatchingLengths(int a, int b)
    {
        if (a != b)
            throw new ArgumentException($"Array lengths must match. Got {a} and {b}.");
    }
}

/// <summary>Configures the legend display for an axes.</summary>
public sealed record Legend
{
    /// <summary>Gets whether the legend is visible.</summary>
    public bool Visible { get; init; } = true;

    /// <summary>Gets the position of the legend within the axes.</summary>
    public LegendPosition Position { get; init; } = LegendPosition.Best;
}

/// <summary>Specifies the position of a legend within the axes.</summary>
public enum LegendPosition
{
    /// <summary>Automatically choose the best position to minimize overlap.</summary>
    Best,

    /// <summary>Place the legend in the upper-right corner.</summary>
    UpperRight,

    /// <summary>Place the legend in the upper-left corner.</summary>
    UpperLeft,

    /// <summary>Place the legend in the lower-right corner.</summary>
    LowerRight,

    /// <summary>Place the legend in the lower-left corner.</summary>
    LowerLeft
}
