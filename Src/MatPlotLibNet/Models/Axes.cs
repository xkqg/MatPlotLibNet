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

    /// <summary>Gets or sets whether native SVG tooltips are enabled for data elements.</summary>
    /// <remarks>When enabled, each data point in the SVG output is wrapped in a <c>&lt;g&gt;&lt;title&gt;...&lt;/title&gt;&lt;/g&gt;</c>
    /// element that browsers display as a hover tooltip. Has no effect on non-SVG transforms (PNG, PDF).</remarks>
    public bool EnableTooltips { get; set; }

    /// <summary>Gets or sets how multiple bar series on this axes are displayed.</summary>
    /// <remarks>When set to <see cref="Models.BarMode.Stacked"/>, bar values are cumulated per category
    /// so each bar sits on top of the previous one. Defaults to <see cref="Models.BarMode.Grouped"/>.</remarks>
    public BarMode BarMode { get; set; } = BarMode.Grouped;

    /// <summary>Gets the collection of data series plotted on this axes.</summary>
    public IReadOnlyList<ISeries> Series => _series;
    private readonly List<ISeries> _series = [];

    /// <summary>Gets the collection of text annotations on this axes.</summary>
    public IReadOnlyList<Annotation> Annotations => _annotations;
    private readonly List<Annotation> _annotations = [];

    /// <summary>Gets the collection of reference lines on this axes.</summary>
    public IReadOnlyList<ReferenceLine> ReferenceLines => _referenceLines;
    private readonly List<ReferenceLine> _referenceLines = [];

    /// <summary>Gets the collection of shaded span regions on this axes.</summary>
    public IReadOnlyList<SpanRegion> Spans => _spans;
    private readonly List<SpanRegion> _spans = [];

    /// <summary>Gets the collection of buy/sell signal markers on this axes.</summary>
    public IReadOnlyList<SignalMarker> Signals => _signals;
    private readonly List<SignalMarker> _signals = [];

    /// <summary>Gets the secondary Y-axis configuration, or null when no secondary axis is active.</summary>
    /// <remarks>Activated by calling <see cref="TwinX"/>. Series added via <see cref="PlotSecondary"/> or
    /// <see cref="ScatterSecondary"/> are rendered against this axis with independent Y scaling.
    /// The secondary axis renders tick marks and labels on the right side of the plot area.</remarks>
    public Axis? SecondaryYAxis { get; private set; }

    /// <summary>Gets the collection of data series plotted against the secondary Y-axis.</summary>
    /// <remarks>These series use a separate <see cref="Rendering.DataTransform"/> based on the secondary Y range,
    /// while sharing the primary X-axis transform.</remarks>
    public IReadOnlyList<ISeries> SecondarySeries => _secondarySeries;
    private readonly List<ISeries> _secondarySeries = [];

    /// <summary>Enables a secondary Y-axis (right side) on this axes, creating it if it does not already exist.</summary>
    /// <returns>This axes instance for chaining.</returns>
    public Axes TwinX()
    {
        SecondaryYAxis ??= new Axis();
        return this;
    }

    /// <summary>Adds a line series plotted against the secondary Y-axis.</summary>
    /// <remarks>Implicitly calls <see cref="TwinX"/> to ensure the secondary axis exists.</remarks>
    public LineSeries PlotSecondary(double[] x, double[] y)
    {
        ValidateMatchingLengths(x.Length, y.Length);
        TwinX();
        var series = new LineSeries(x, y);
        _secondarySeries.Add(series);
        return series;
    }

    /// <summary>Adds a scatter series plotted against the secondary Y-axis.</summary>
    /// <remarks>Implicitly calls <see cref="TwinX"/> to ensure the secondary axis exists.</remarks>
    public ScatterSeries ScatterSecondary(double[] x, double[] y)
    {
        ValidateMatchingLengths(x.Length, y.Length);
        TwinX();
        var series = new ScatterSeries(x, y);
        _secondarySeries.Add(series);
        return series;
    }

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

    /// <summary>Adds a radar (spider) chart series from the given categories and values.</summary>
    public RadarSeries Radar(string[] categories, double[] values)
    {
        ValidateMatchingLengths(categories.Length, values.Length);
        var series = new RadarSeries(categories, values);
        _series.Add(series);
        return series;
    }

    /// <summary>Adds a quiver (vector field) series from the given position and vector data.</summary>
    public QuiverSeries Quiver(double[] x, double[] y, double[] u, double[] v)
    {
        ValidateMatchingLengths(x.Length, y.Length);
        ValidateMatchingLengths(x.Length, u.Length);
        ValidateMatchingLengths(x.Length, v.Length);
        var series = new QuiverSeries(x, y, u, v);
        _series.Add(series);
        return series;
    }

    /// <summary>Adds a candlestick (OHLC) series from the given price data.</summary>
    public CandlestickSeries Candlestick(double[] open, double[] high, double[] low, double[] close, string[]? dateLabels = null)
    {
        ValidateMatchingLengths(open.Length, high.Length);
        ValidateMatchingLengths(open.Length, low.Length);
        ValidateMatchingLengths(open.Length, close.Length);
        var series = new CandlestickSeries(open, high, low, close) { DateLabels = dateLabels };
        _series.Add(series);
        return series;
    }

    /// <summary>Adds an error bar series from the given X, Y data and error magnitudes.</summary>
    public ErrorBarSeries ErrorBar(double[] x, double[] y, double[] yErrorLow, double[] yErrorHigh)
    {
        ValidateMatchingLengths(x.Length, y.Length);
        ValidateMatchingLengths(x.Length, yErrorLow.Length);
        ValidateMatchingLengths(x.Length, yErrorHigh.Length);
        var series = new ErrorBarSeries(x, y, yErrorLow, yErrorHigh);
        _series.Add(series);
        return series;
    }

    /// <summary>Adds a step-function series from the given X and Y data arrays.</summary>
    /// <param name="x">The X-axis data values.</param>
    /// <param name="y">The Y-axis data values.</param>
    /// <returns>The newly created <see cref="StepSeries"/> for further configuration.</returns>
    public StepSeries Step(double[] x, double[] y)
    {
        ValidateMatchingLengths(x.Length, y.Length);
        var series = new StepSeries(x, y);
        _series.Add(series);
        return series;
    }

    /// <summary>Adds a filled area series from the given X and Y data arrays.</summary>
    /// <param name="x">The X-axis data values.</param>
    /// <param name="y">The Y-axis data values (top line of the fill region).</param>
    /// <param name="y2">Optional secondary Y data. When provided, fills between the two curves; when null, fills down to y=0.</param>
    /// <returns>The newly created <see cref="AreaSeries"/> for further configuration.</returns>
    public AreaSeries FillBetween(double[] x, double[] y, double[]? y2 = null)
    {
        ValidateMatchingLengths(x.Length, y.Length);
        if (y2 is not null) ValidateMatchingLengths(x.Length, y2.Length);
        var series = new AreaSeries(x, y) { YData2 = y2 };
        _series.Add(series);
        return series;
    }

    /// <summary>Adds a donut chart series.</summary>
    public DonutSeries Donut(double[] sizes, string[]? labels = null)
    {
        var series = new DonutSeries(sizes) { Labels = labels };
        _series.Add(series);
        return series;
    }

    /// <summary>Adds a bubble chart series from X, Y, and size arrays.</summary>
    public BubbleSeries Bubble(double[] x, double[] y, double[] sizes)
    {
        ValidateMatchingLengths(x.Length, y.Length);
        ValidateMatchingLengths(x.Length, sizes.Length);
        var series = new BubbleSeries(x, y, sizes);
        _series.Add(series);
        return series;
    }

    /// <summary>Adds a traditional OHLC bar chart series from price data.</summary>
    public OhlcBarSeries OhlcBar(double[] open, double[] high, double[] low, double[] close, string[]? dateLabels = null)
    {
        ValidateMatchingLengths(open.Length, high.Length);
        ValidateMatchingLengths(open.Length, low.Length);
        ValidateMatchingLengths(open.Length, close.Length);
        var series = new OhlcBarSeries(open, high, low, close) { DateLabels = dateLabels };
        _series.Add(series);
        return series;
    }

    /// <summary>Adds a waterfall chart showing cumulative positive and negative changes.</summary>
    public WaterfallSeries Waterfall(string[] categories, double[] values)
    {
        ValidateMatchingLengths(categories.Length, values.Length);
        var series = new WaterfallSeries(categories, values);
        _series.Add(series);
        return series;
    }

    /// <summary>Adds a funnel chart showing progressive reduction through stages.</summary>
    public FunnelSeries Funnel(string[] labels, double[] values)
    {
        ValidateMatchingLengths(labels.Length, values.Length);
        var series = new FunnelSeries(labels, values);
        _series.Add(series);
        return series;
    }

    /// <summary>Adds a gauge (speedometer) chart displaying a single value within a range.</summary>
    public GaugeSeries Gauge(double value) { var s = new GaugeSeries(value); _series.Add(s); return s; }

    /// <summary>Adds a progress bar showing a value as a fraction (0.0 to 1.0).</summary>
    public ProgressBarSeries ProgressBar(double value) { var s = new ProgressBarSeries(value); _series.Add(s); return s; }

    /// <summary>Adds a sparkline — a tiny inline line chart with no axes or labels.</summary>
    public SparklineSeries Sparkline(double[] values) { var s = new SparklineSeries(values); _series.Add(s); return s; }

    /// <summary>Adds a Gantt chart showing task durations on a timeline.</summary>
    public GanttSeries Gantt(string[] tasks, double[] starts, double[] ends)
    {
        ValidateMatchingLengths(tasks.Length, starts.Length);
        ValidateMatchingLengths(tasks.Length, ends.Length);
        var series = new GanttSeries(tasks, starts, ends);
        _series.Add(series);
        return series;
    }

    /// <summary>Adds a buy or sell signal marker at the specified data coordinates.</summary>
    public SignalMarker AddSignal(double x, double y, SignalDirection direction)
    {
        var marker = new SignalMarker(x, y, direction);
        _signals.Add(marker);
        return marker;
    }

    /// <summary>Adds a text annotation at the specified data coordinates.</summary>
    public Annotation Annotate(string text, double x, double y)
    {
        var annotation = new Annotation(text, x, y);
        _annotations.Add(annotation);
        return annotation;
    }

    /// <summary>Adds a horizontal reference line at the specified Y value.</summary>
    public ReferenceLine AxHLine(double y)
    {
        var line = new ReferenceLine(y, Orientation.Horizontal);
        _referenceLines.Add(line);
        return line;
    }

    /// <summary>Adds a vertical reference line at the specified X value.</summary>
    public ReferenceLine AxVLine(double x)
    {
        var line = new ReferenceLine(x, Orientation.Vertical);
        _referenceLines.Add(line);
        return line;
    }

    /// <summary>Adds a horizontal shaded span between the specified Y values.</summary>
    public SpanRegion AxHSpan(double yMin, double yMax)
    {
        var span = new SpanRegion(yMin, yMax, Orientation.Horizontal);
        _spans.Add(span);
        return span;
    }

    /// <summary>Adds a vertical shaded span between the specified X values.</summary>
    public SpanRegion AxVSpan(double xMin, double xMax)
    {
        var span = new SpanRegion(xMin, xMax, Orientation.Vertical);
        _spans.Add(span);
        return span;
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

/// <summary>Specifies how multiple bar series on the same axes are displayed.</summary>
public enum BarMode
{
    /// <summary>Bars are placed side by side.</summary>
    Grouped,

    /// <summary>Bars are stacked on top of each other.</summary>
    Stacked
}
