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

    /// <summary>Enables or disables native SVG tooltips for data elements.</summary>
    public AxesBuilder WithTooltips(bool enabled = true) { _axes.EnableTooltips = enabled; return this; }

    /// <summary>Configures a secondary Y-axis and adds series to it.</summary>
    public AxesBuilder WithSecondaryYAxis(Action<SecondaryAxisBuilder> configure)
    {
        _axes.TwinX();
        var builder = new SecondaryAxisBuilder(_axes);
        configure(builder);
        return this;
    }

    /// <summary>Adds a text annotation at the specified data coordinates.</summary>
    public AxesBuilder Annotate(string text, double x, double y, Action<Annotation>? configure = null)
    {
        var annotation = _axes.Annotate(text, x, y);
        configure?.Invoke(annotation);
        return this;
    }

    /// <summary>Adds a horizontal reference line at the specified Y value.</summary>
    public AxesBuilder AxHLine(double y, Action<ReferenceLine>? configure = null)
    {
        var line = _axes.AxHLine(y);
        configure?.Invoke(line);
        return this;
    }

    /// <summary>Adds a vertical reference line at the specified X value.</summary>
    public AxesBuilder AxVLine(double x, Action<ReferenceLine>? configure = null)
    {
        var line = _axes.AxVLine(x);
        configure?.Invoke(line);
        return this;
    }

    /// <summary>Adds a horizontal shaded span between the specified Y values.</summary>
    public AxesBuilder AxHSpan(double yMin, double yMax, Action<SpanRegion>? configure = null)
    {
        var span = _axes.AxHSpan(yMin, yMax);
        configure?.Invoke(span);
        return this;
    }

    /// <summary>Adds a vertical shaded span between the specified X values.</summary>
    public AxesBuilder AxVSpan(double xMin, double xMax, Action<SpanRegion>? configure = null)
    {
        var span = _axes.AxVSpan(xMin, xMax);
        configure?.Invoke(span);
        return this;
    }

    /// <summary>Sets the bar mode (grouped or stacked) for multiple bar series.</summary>
    public AxesBuilder SetBarMode(BarMode mode) { _axes.BarMode = mode; return this; }

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

    /// <summary>Adds a radar (spider) chart series to the axes.</summary>
    public AxesBuilder Radar(string[] categories, double[] values, Action<RadarSeries>? configure = null)
        => AddSeries(ax => ax.Radar(categories, values), configure);

    /// <summary>Adds a quiver (vector field) series to the axes.</summary>
    public AxesBuilder Quiver(double[] x, double[] y, double[] u, double[] v, Action<QuiverSeries>? configure = null)
        => AddSeries(ax => ax.Quiver(x, y, u, v), configure);

    /// <summary>Adds a candlestick (OHLC) series to the axes.</summary>
    public AxesBuilder Candlestick(double[] open, double[] high, double[] low, double[] close, string[]? dateLabels = null, Action<CandlestickSeries>? configure = null)
        => AddSeries(ax => ax.Candlestick(open, high, low, close, dateLabels), configure);

    /// <summary>Adds an error bar series to the axes.</summary>
    public AxesBuilder ErrorBar(double[] x, double[] y, double[] yErrorLow, double[] yErrorHigh, Action<ErrorBarSeries>? configure = null)
        => AddSeries(ax => ax.ErrorBar(x, y, yErrorLow, yErrorHigh), configure);

    /// <summary>Adds a step-function series to the axes.</summary>
    public AxesBuilder Step(double[] x, double[] y, Action<StepSeries>? configure = null)
        => AddSeries(ax => ax.Step(x, y), configure);

    /// <summary>Adds a filled area (fill-between) series to the axes.</summary>
    public AxesBuilder FillBetween(double[] x, double[] y, double[]? y2 = null, Action<AreaSeries>? configure = null)
        => AddSeries(ax => ax.FillBetween(x, y, y2), configure);

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

/// <summary>Fluent builder for configuring a secondary Y-axis and adding series that scale against it.</summary>
/// <remarks>Obtained via <see cref="AxesBuilder.WithSecondaryYAxis"/>. The secondary axis renders ticks and
/// labels on the right side of the plot and uses an independent Y-axis data range.</remarks>
public sealed class SecondaryAxisBuilder
{
    private readonly Axes _axes;

    internal SecondaryAxisBuilder(Axes axes) => _axes = axes;

    /// <summary>Sets the label displayed alongside the right-side Y-axis.</summary>
    public SecondaryAxisBuilder SetYLabel(string label) { _axes.SecondaryYAxis!.Label = label; return this; }

    /// <summary>Sets explicit min/max limits for the secondary Y-axis data range.</summary>
    public SecondaryAxisBuilder SetYLim(double min, double max) { _axes.SecondaryYAxis!.Min = min; _axes.SecondaryYAxis!.Max = max; return this; }

    /// <summary>Adds a line series plotted against the secondary Y-axis.</summary>
    public SecondaryAxisBuilder Plot(double[] x, double[] y, Action<LineSeries>? configure = null)
    {
        var series = _axes.PlotSecondary(x, y);
        configure?.Invoke(series);
        return this;
    }

    /// <summary>Adds a scatter series plotted against the secondary Y-axis.</summary>
    public SecondaryAxisBuilder Scatter(double[] x, double[] y, Action<ScatterSeries>? configure = null)
    {
        var series = _axes.ScatterSecondary(x, y);
        configure?.Invoke(series);
        return this;
    }
}
