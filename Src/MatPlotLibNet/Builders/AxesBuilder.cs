// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet;

/// <summary>Fluent builder for configuring an <see cref="Models.Axes"/> instance within a subplot.</summary>
public sealed class AxesBuilder
{
    private readonly Axes _axes = new();

    /// <summary>Sets the axes title.</summary>
    public AxesBuilder WithTitle(string title) { _axes.Title = title; return this; }

    /// <summary>Sets the X-axis label.</summary>
    public AxesBuilder SetXLabel(string label) { _axes.XAxis.Label = label; return this; }

    /// <summary>Sets the Y-axis label.</summary>
    public AxesBuilder SetYLabel(string label) { _axes.YAxis.Label = label; return this; }

    /// <summary>Sets the X-axis data range limits.</summary>
    public AxesBuilder SetXLim(double min, double max) { _axes.XAxis.Min = min; _axes.XAxis.Max = max; return this; }

    /// <summary>Sets the Y-axis data range limits.</summary>
    public AxesBuilder SetYLim(double min, double max) { _axes.YAxis.Min = min; _axes.YAxis.Max = max; return this; }

    /// <summary>Sets the X-axis scale type (e.g., linear or logarithmic).</summary>
    public AxesBuilder SetXScale(AxisScale scale) { _axes.XAxis.Scale = scale; return this; }

    /// <summary>Sets the Y-axis scale type (e.g., linear or logarithmic).</summary>
    public AxesBuilder SetYScale(AxisScale scale) { _axes.YAxis.Scale = scale; return this; }

    /// <summary>Toggles grid line visibility on the axes.</summary>
    public AxesBuilder ShowGrid(bool visible = true) { _axes.Grid = _axes.Grid with { Visible = visible }; return this; }

    /// <summary>Adds a line series to the axes.</summary>
    public AxesBuilder Plot(double[] x, double[] y, Action<LineSeries>? configure = null)
        => AddSeries(ax => ax.Plot(x, y), configure);

    /// <summary>Adds a scatter series to the axes.</summary>
    public AxesBuilder Scatter(double[] x, double[] y, Action<ScatterSeries>? configure = null)
        => AddSeries(ax => ax.Scatter(x, y), configure);

    /// <summary>Adds a bar series to the axes.</summary>
    public AxesBuilder Bar(string[] categories, double[] values, Action<BarSeries>? configure = null)
        => AddSeries(ax => ax.Bar(categories, values), configure);

    /// <summary>Adds a histogram series to the axes.</summary>
    public AxesBuilder Hist(double[] data, int bins = 10, Action<HistogramSeries>? configure = null)
        => AddSeries(ax => ax.Hist(data, bins), configure);

    /// <summary>Adds a pie series to the axes.</summary>
    public AxesBuilder Pie(double[] sizes, string[]? labels = null, Action<PieSeries>? configure = null)
        => AddSeries(ax => ax.Pie(sizes, labels), configure);

    /// <summary>Adds a heatmap series to the axes.</summary>
    public AxesBuilder Heatmap(double[,] data, Action<HeatmapSeries>? configure = null)
        => AddSeries(ax => ax.Heatmap(data), configure);

    /// <summary>Adds a box plot series to the axes.</summary>
    public AxesBuilder BoxPlot(double[][] datasets, Action<BoxSeries>? configure = null)
        => AddSeries(ax => ax.BoxPlot(datasets), configure);

    /// <summary>Adds a violin series to the axes.</summary>
    public AxesBuilder Violin(double[][] datasets, Action<ViolinSeries>? configure = null)
        => AddSeries(ax => ax.Violin(datasets), configure);

    /// <summary>Adds a contour series to the axes.</summary>
    public AxesBuilder Contour(double[] x, double[] y, double[,] z, Action<ContourSeries>? configure = null)
        => AddSeries(ax => ax.Contour(x, y, z), configure);

    /// <summary>Adds a stem series to the axes.</summary>
    public AxesBuilder Stem(double[] x, double[] y, Action<StemSeries>? configure = null)
        => AddSeries(ax => ax.Stem(x, y), configure);

    private AxesBuilder AddSeries<T>(Func<Axes, T> factory, Action<T>? configure) where T : ISeries
    {
        var series = factory(_axes);
        configure?.Invoke(series);
        return this;
    }

    internal Axes Build(int rows, int cols, int index)
    {
        _axes.GridRows = rows;
        _axes.GridCols = cols;
        _axes.GridIndex = index;
        return _axes;
    }
}
