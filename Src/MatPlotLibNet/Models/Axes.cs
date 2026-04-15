// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Lighting;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models;

/// <summary>Represents a set of axes within a figure, onto which data series are plotted.</summary>
public sealed class Axes
{
    public string? Title { get; set; }

    public TextStyle? TitleStyle { get; set; }

    public TitleLocation TitleLoc { get; set; } = TitleLocation.Center;

    public Axis XAxis { get; } = new();

    public Axis YAxis { get; } = new();

    public Axis3D ZAxis { get; } = new();

    public Legend Legend { get; set; } = new();

    public GridStyle Grid { get; set; } = new();

    public SpinesConfig Spines { get; set; } = new();

    public int GridRows { get; internal set; }

    public int GridCols { get; internal set; }

    public int GridIndex { get; internal set; }

    public GridPosition? GridPosition { get; internal set; }

    public Axes? ShareXWith { get; internal set; }

    public Axes? ShareYWith { get; internal set; }

    public string? Key { get; set; }

    public bool EnableTooltips { get; set; }

    public bool EnableInteractiveAttributes { get; set; }

    public BarMode BarMode { get; set; } = BarMode.Grouped;

    public CoordinateSystem CoordinateSystem { get; set; } = CoordinateSystem.Cartesian;

    public Projection3D? Projection { get; set; }

    /// <summary>Camera elevation above the XY plane in degrees. Default is 30.</summary>
    public double Elevation { get; set; } = 30;

    /// <summary>Camera azimuth rotation around the Z axis in degrees. Default is -60.</summary>
    public double Azimuth { get; set; } = -60;

    /// <summary>Camera distance for perspective projection. Null (default) = orthographic. Minimum 2.0.</summary>
    public double? CameraDistance { get; set; }

    /// <summary>Optional light source for per-face shading on 3D surfaces and bars. Null = no lighting.</summary>
    public ILightSource? LightSource { get; set; }

    /// <summary>When true, 3D renderers emit normalized vertex data as data-v3d attributes for interactive rotation.</summary>
    public bool Emit3DVertexData { get; set; }

    public ColorBar? ColorBar { get; set; }

    /// <summary>Gets the collection of data series plotted on this axes.</summary>
    public IReadOnlyList<ISeries> Series => _series;
    private readonly List<ISeries> _series = [];

    /// <summary>Adds a pre-constructed series to the axes.</summary>
    public T AddSeries<T>(T series) where T : ISeries
    {
        _series.Add(series);
        return series;
    }

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

    /// <summary>Gets the collection of inset axes rendered within this axes.</summary>
    public IReadOnlyList<Axes> Insets => _insets;
    private readonly List<Axes> _insets = [];

    /// <summary>Gets the axis break regions on the X-axis.</summary>
    public IReadOnlyList<AxisBreak> XBreaks => _xBreaks;
    private readonly List<AxisBreak> _xBreaks = [];

    /// <summary>Gets the axis break regions on the Y-axis.</summary>
    public IReadOnlyList<AxisBreak> YBreaks => _yBreaks;
    private readonly List<AxisBreak> _yBreaks = [];

    /// <summary>Adds a discontinuous (broken) region to the X-axis, hiding data between
    /// <paramref name="from"/> and <paramref name="to"/> and compressing the scale.</summary>
    public Axes AddXBreak(double from, double to, BreakStyle style = BreakStyle.Zigzag)
    {
        _xBreaks.Add(new AxisBreak(from, to, style));
        return this;
    }

    /// <summary>Adds a discontinuous (broken) region to the Y-axis, hiding data between
    /// <paramref name="from"/> and <paramref name="to"/> and compressing the scale.</summary>
    public Axes AddYBreak(double from, double to, BreakStyle style = BreakStyle.Zigzag)
    {
        _yBreaks.Add(new AxisBreak(from, to, style));
        return this;
    }

    public InsetBounds? InsetBounds { get; internal set; }

    /// <summary>Adds an inset axes at the specified fractional position within this axes.</summary>
    /// <param name="x">Horizontal position as a fraction of parent width (0-1).</param>
    /// <param name="y">Vertical position as a fraction of parent height (0-1).</param>
    /// <param name="width">Width as a fraction of parent width.</param>
    /// <param name="height">Height as a fraction of parent height.</param>
    /// <returns>The newly created inset <see cref="Axes"/> instance.</returns>
    public Axes AddInset(double x, double y, double width, double height)
    {
        var inset = new Axes { InsetBounds = new InsetBounds(x, y, width, height) };
        _insets.Add(inset);
        return inset;
    }

    /// <summary>Adds an inset axes at the specified bounds.</summary>
    public Axes AddInset(InsetBounds bounds)
    {
        var inset = new Axes { InsetBounds = bounds };
        _insets.Add(inset);
        return inset;
    }

    /// <summary>Adds an inset axes at the given fractional position. Alias for
    /// <see cref="AddInset(double,double,double,double)"/>.</summary>
    /// <param name="x">Left edge of the inset as a fraction of the parent axes width [0, 1].</param>
    /// <param name="y">Bottom edge of the inset as a fraction of the parent axes height [0, 1].</param>
    /// <param name="width">Width of the inset as a fraction of the parent axes width [0, 1].</param>
    /// <param name="height">Height of the inset as a fraction of the parent axes height [0, 1].</param>
    /// <returns>The new inset <see cref="Axes"/> instance.</returns>
    public Axes InsetAxes(double x, double y, double width, double height)
        => AddInset(x, y, width, height);

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

    // ── TwinY — secondary X-axis (top edge) ─────────────────────────────────

    public Axis? SecondaryXAxis { get; private set; }

    /// <summary>Gets the collection of data series plotted against the secondary X-axis.</summary>
    public IReadOnlyList<ISeries> XSecondarySeries => _xSecondarySeries;
    private readonly List<ISeries> _xSecondarySeries = [];

    /// <summary>Enables a secondary X-axis (top edge) on this axes, creating it if it does not already exist.</summary>
    /// <returns>This axes instance for chaining.</returns>
    public Axes TwinY()
    {
        SecondaryXAxis ??= new Axis();
        return this;
    }

    /// <summary>Adds a line series plotted against the secondary X-axis.</summary>
    /// <remarks>Implicitly calls <see cref="TwinY"/> to ensure the secondary axis exists.</remarks>
    public LineSeries PlotXSecondary(double[] x, double[] y)
    {
        ValidateMatchingLengths(x.Length, y.Length);
        TwinY();
        var series = new LineSeries(x, y);
        _xSecondarySeries.Add(series);
        return series;
    }

    /// <summary>Adds a scatter series plotted against the secondary X-axis.</summary>
    /// <remarks>Implicitly calls <see cref="TwinY"/> to ensure the secondary axis exists.</remarks>
    public ScatterSeries ScatterXSecondary(double[] x, double[] y)
    {
        ValidateMatchingLengths(x.Length, y.Length);
        TwinY();
        var series = new ScatterSeries(x, y);
        _xSecondarySeries.Add(series);
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

    /// <summary>Adds an ECDF (empirical cumulative distribution function) series from the given data.</summary>
    public EcdfSeries Ecdf(double[] data)
    {
        var series = new EcdfSeries(data);
        _series.Add(series);
        return series;
    }

    /// <summary>Adds a stacked area (stackplot) series from the given shared X values and multiple Y datasets.</summary>
    /// <param name="x">The shared X-axis data values.</param>
    /// <param name="ySets">An array of Y arrays, one per stacked layer.</param>
    /// <returns>The newly created <see cref="StackedAreaSeries"/> for further configuration.</returns>
    public StackedAreaSeries StackPlot(double[] x, double[][] ySets)
    {
        var series = new StackedAreaSeries(x, ySets);
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

    /// <summary>Adds an image series from the given two-dimensional data array (imshow).</summary>
    /// <param name="data">The 2D data matrix to render as a colored image.</param>
    /// <returns>The newly created <see cref="ImageSeries"/> for further configuration.</returns>
    public ImageSeries Image(double[,] data)
    {
        var series = new ImageSeries(data);
        _series.Add(series);
        return series;
    }

    /// <summary>Adds a 2D histogram (density) series from X,Y scatter data.</summary>
    /// <param name="x">The X-axis data values.</param>
    /// <param name="y">The Y-axis data values.</param>
    /// <param name="bins">The number of bins along both axes.</param>
    /// <returns>The newly created <see cref="Histogram2DSeries"/> for further configuration.</returns>
    public Histogram2DSeries Histogram2D(double[] x, double[] y, int bins = 20)
    {
        ValidateMatchingLengths(x.Length, y.Length);
        var series = new Histogram2DSeries(x, y, bins, bins);
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

    /// <summary>Adds a hexagonal binning series from the given X and Y scatter data.</summary>
    /// <param name="x">The X data values.</param>
    /// <param name="y">The Y data values.</param>
    /// <returns>The newly created <see cref="HexbinSeries"/> for further configuration.</returns>
    public HexbinSeries Hexbin(double[] x, double[] y)
    {
        var series = new HexbinSeries(x, y);
        _series.Add(series);
        return series;
    }

    /// <summary>Adds a polynomial regression series fitted to the given X and Y data.</summary>
    /// <param name="x">The X data values.</param>
    /// <param name="y">The Y data values.</param>
    /// <returns>The newly created <see cref="RegressionSeries"/> for further configuration.</returns>
    public RegressionSeries Regression(double[] x, double[] y)
    {
        var series = new RegressionSeries(x, y);
        _series.Add(series);
        return series;
    }

    /// <summary>Adds a kernel density estimation (KDE) series for the given data sample.</summary>
    /// <param name="data">The data values used to estimate the density.</param>
    /// <returns>The newly created <see cref="KdeSeries"/> for further configuration.</returns>
    public KdeSeries Kde(double[] data)
    {
        var series = new KdeSeries(data);
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

    /// <summary>Adds a filled contour series from the given X, Y, and Z data.</summary>
    /// <param name="x">The X-axis grid coordinates.</param>
    /// <param name="y">The Y-axis grid coordinates.</param>
    /// <param name="z">The 2D matrix of Z values at each grid point.</param>
    /// <returns>The newly created <see cref="ContourfSeries"/> for further configuration.</returns>
    public ContourfSeries Contourf(double[] x, double[] y, double[,] z)
    {
        var series = new ContourfSeries(x, y, z);
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

    /// <summary>Adds a streamplot (vector field streamlines) series from the given grid and velocity data.</summary>
    public StreamplotSeries Streamplot(double[] x, double[] y, double[,] u, double[,] v)
    {
        var series = new StreamplotSeries(x, y, u, v);
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

    /// <summary>Adds a treemap series to the axes.</summary>
    public TreemapSeries Treemap(TreeNode root)
    {
        var series = new TreemapSeries(root);
        _series.Add(series);
        return series;
    }

    /// <summary>Adds a sunburst series to the axes.</summary>
    public SunburstSeries Sunburst(TreeNode root)
    {
        var series = new SunburstSeries(root);
        _series.Add(series);
        return series;
    }

    /// <summary>Adds a Sankey diagram series to the axes.</summary>
    public SankeySeries Sankey(SankeyNode[] nodes, SankeyLink[] links)
    {
        var series = new SankeySeries(nodes, links);
        _series.Add(series);
        return series;
    }

    /// <summary>Adds a polar line series and sets coordinate system to Polar.</summary>
    public PolarLineSeries PolarPlot(double[] r, double[] theta)
    {
        CoordinateSystem = CoordinateSystem.Polar;
        var series = new PolarLineSeries(r, theta);
        _series.Add(series);
        return series;
    }

    /// <summary>Adds a polar scatter series and sets coordinate system to Polar.</summary>
    public PolarScatterSeries PolarScatter(double[] r, double[] theta)
    {
        CoordinateSystem = CoordinateSystem.Polar;
        var series = new PolarScatterSeries(r, theta);
        _series.Add(series);
        return series;
    }

    /// <summary>Adds a polar bar series and sets coordinate system to Polar.</summary>
    public PolarBarSeries PolarBar(double[] r, double[] theta)
    {
        CoordinateSystem = CoordinateSystem.Polar;
        var series = new PolarBarSeries(r, theta);
        _series.Add(series);
        return series;
    }

    /// <summary>Adds a polar heatmap series and sets coordinate system to Polar.
    /// Each cell is a wedge defined by a theta bin and a radial bin.</summary>
    /// <param name="data">2D data matrix [thetaBins, rBins].</param>
    /// <param name="thetaBins">Number of angular divisions.</param>
    /// <param name="rBins">Number of radial divisions.</param>
    public PolarHeatmapSeries PolarHeatmap(double[,] data, int thetaBins, int rBins)
    {
        CoordinateSystem = CoordinateSystem.Polar;
        var series = new PolarHeatmapSeries(data, thetaBins, rBins);
        _series.Add(series);
        return series;
    }

    /// <summary>Adds a 3D surface series and sets coordinate system to ThreeD.</summary>
    public SurfaceSeries Surface(double[] x, double[] y, double[,] z)
    {
        CoordinateSystem = CoordinateSystem.ThreeD;
        var series = new SurfaceSeries(x, y, z);
        _series.Add(series);
        return series;
    }

    /// <summary>Adds a 3D wireframe series and sets coordinate system to ThreeD.</summary>
    public WireframeSeries Wireframe(double[] x, double[] y, double[,] z)
    {
        CoordinateSystem = CoordinateSystem.ThreeD;
        var series = new WireframeSeries(x, y, z);
        _series.Add(series);
        return series;
    }

    /// <summary>Adds a 3D scatter series and sets coordinate system to ThreeD.</summary>
    public Scatter3DSeries Scatter3D(double[] x, double[] y, double[] z)
    {
        CoordinateSystem = CoordinateSystem.ThreeD;
        var series = new Scatter3DSeries(x, y, z);
        _series.Add(series);
        return series;
    }

    /// <summary>Adds a 3D stem series and sets coordinate system to ThreeD.</summary>
    public Stem3DSeries Stem3D(Numerics.Vec x, Numerics.Vec y, Numerics.Vec z)
    {
        CoordinateSystem = CoordinateSystem.ThreeD;
        var series = new Stem3DSeries(x, y, z);
        _series.Add(series);
        return series;
    }

    /// <summary>Adds a 3D bar series and sets coordinate system to ThreeD.</summary>
    public Bar3DSeries Bar3D(Numerics.Vec x, Numerics.Vec y, Numerics.Vec z)
    {
        CoordinateSystem = CoordinateSystem.ThreeD;
        var series = new Bar3DSeries(x, y, z);
        _series.Add(series);
        return series;
    }

    /// <summary>Adds a planar 3D bar series (flat translucent rectangles in Y-planes —
    /// matplotlib's "2D bars in different planes" pattern) and sets coordinate system to ThreeD.</summary>
    public PlanarBar3DSeries PlanarBar3D(Numerics.Vec x, Numerics.Vec y, Numerics.Vec z)
    {
        CoordinateSystem = CoordinateSystem.ThreeD;
        var series = new PlanarBar3DSeries(x, y, z);
        _series.Add(series);
        return series;
    }

    // -------------------------------------------------------------------------
    // v0.8.0 series
    // -------------------------------------------------------------------------

    /// <summary>Adds a rug plot series showing individual data values as tick marks along the X axis.</summary>
    /// <param name="data">The data values to display as rug ticks.</param>
    /// <returns>The newly created <see cref="RugplotSeries"/> for further configuration.</returns>
    public RugplotSeries Rugplot(Numerics.Vec data)
    {
        var series = new RugplotSeries(data);
        _series.Add(series);
        return series;
    }

    /// <summary>Adds a strip plot series showing individual data points per category with random jitter.</summary>
    /// <param name="datasets">An array of datasets, each containing values for one category.</param>
    /// <returns>The newly created <see cref="StripplotSeries"/> for further configuration.</returns>
    public StripplotSeries Stripplot(double[][] datasets)
    {
        var series = new StripplotSeries(datasets);
        _series.Add(series);
        return series;
    }

    /// <summary>Adds an event plot series showing event positions as vertical tick lines per row.</summary>
    /// <param name="positions">An array of event position sets, one per row.</param>
    /// <returns>The newly created <see cref="EventplotSeries"/> for further configuration.</returns>
    public EventplotSeries Eventplot(double[][] positions)
    {
        var series = new EventplotSeries(positions);
        _series.Add(series);
        return series;
    }

    /// <summary>Adds a broken bar series showing horizontal bars with gaps per row.</summary>
    /// <param name="ranges">An array of (Start, Width) range sets, one per row.</param>
    /// <returns>The newly created <see cref="BrokenBarSeries"/> for further configuration.</returns>
    public BrokenBarSeries BrokenBarH((double Start, double Width)[][] ranges)
    {
        var series = new BrokenBarSeries(ranges);
        _series.Add(series);
        return series;
    }

    /// <summary>Adds a count plot series showing the frequency of raw categorical values as bars.</summary>
    /// <param name="values">The raw categorical values to count and display.</param>
    /// <returns>The newly created <see cref="CountSeries"/> for further configuration.</returns>
    public CountSeries Countplot(string[] values)
    {
        var series = new CountSeries(values);
        _series.Add(series);
        return series;
    }

    /// <summary>Adds a pseudocolor mesh series on a non-uniform rectangular grid.</summary>
    /// <param name="x">The M+1 X-axis edge coordinates.</param>
    /// <param name="y">The N+1 Y-axis edge coordinates.</param>
    /// <param name="c">The N×M data matrix.</param>
    /// <returns>The newly created <see cref="PcolormeshSeries"/> for further configuration.</returns>
    public PcolormeshSeries Pcolormesh(Numerics.Vec x, Numerics.Vec y, double[,] c)
    {
        var series = new PcolormeshSeries(x, y, c);
        _series.Add(series);
        return series;
    }

    /// <summary>Adds a residual plot series showing regression residuals as a scatter plot.</summary>
    /// <param name="x">The X data values.</param>
    /// <param name="y">The Y data values.</param>
    /// <returns>The newly created <see cref="ResidualSeries"/> for further configuration.</returns>
    public ResidualSeries Residplot(Numerics.Vec x, Numerics.Vec y)
    {
        var series = new ResidualSeries(x, y);
        _series.Add(series);
        return series;
    }

    /// <summary>Adds a point plot series showing mean ± confidence interval per category.</summary>
    /// <param name="datasets">An array of datasets, one per category.</param>
    /// <returns>The newly created <see cref="PointplotSeries"/> for further configuration.</returns>
    public PointplotSeries Pointplot(double[][] datasets)
    {
        var series = new PointplotSeries(datasets);
        _series.Add(series);
        return series;
    }

    /// <summary>Adds a swarm plot series showing non-overlapping dots per category.</summary>
    /// <param name="datasets">An array of datasets, one per category.</param>
    /// <returns>The newly created <see cref="SwarmplotSeries"/> for further configuration.</returns>
    public SwarmplotSeries Swarmplot(double[][] datasets)
    {
        var series = new SwarmplotSeries(datasets);
        _series.Add(series);
        return series;
    }

    /// <summary>Adds a spectrogram series displaying the time-frequency content of a signal.</summary>
    /// <param name="signal">The input signal values.</param>
    /// <param name="sampleRate">The sample rate in Hz.</param>
    /// <returns>The newly created <see cref="SpectrogramSeries"/> for further configuration.</returns>
    public SpectrogramSeries Spectrogram(Numerics.Vec signal, int sampleRate = 1)
    {
        var series = new SpectrogramSeries(signal) { SampleRate = sampleRate };
        _series.Add(series);
        return series;
    }

    /// <summary>Adds a table series rendered inside the plot area.</summary>
    /// <param name="cellData">2D array of cell text values (rows × columns).</param>
    /// <returns>The newly created <see cref="TableSeries"/> for further configuration.</returns>
    public TableSeries Table(string[][] cellData)
    {
        var series = new TableSeries(cellData);
        _series.Add(series);
        return series;
    }

    /// <summary>Adds a contour series on an unstructured triangular mesh.</summary>
    public TricontourSeries Tricontour(Numerics.Vec x, Numerics.Vec y, Numerics.Vec z)
    {
        var series = new TricontourSeries(x, y, z);
        _series.Add(series);
        return series;
    }

    /// <summary>Adds a pseudocolor series on a triangular mesh.</summary>
    public TripcolorSeries Tripcolor(Numerics.Vec x, Numerics.Vec y, Numerics.Vec z)
    {
        var series = new TripcolorSeries(x, y, z);
        _series.Add(series);
        return series;
    }

    /// <summary>Adds a quiver key (reference arrow) series.</summary>
    public QuiverKeySeries QuiverKey(double x, double y, double u, string label)
    {
        var series = new QuiverKeySeries(x, y, u, label);
        _series.Add(series);
        return series;
    }

    /// <summary>Adds a wind barb series.</summary>
    public BarbsSeries Barbs(Numerics.Vec x, Numerics.Vec y, Numerics.Vec speed, Numerics.Vec direction)
    {
        var series = new BarbsSeries(x, y, speed, direction);
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

    /// <summary>Adds a <see cref="SignalXYSeries"/> with monotonically ascending X values.</summary>
    /// <param name="x">Monotonically ascending X values. Must match the length of <paramref name="y"/>.</param>
    /// <param name="y">Y values parallel to <paramref name="x"/>.</param>
    public SignalXYSeries SignalXY(double[] x, double[] y)
    {
        ValidateMatchingLengths(x.Length, y.Length);
        var series = new SignalXYSeries(x, y);
        _series.Add(series);
        return series;
    }

    /// <summary>Adds a <see cref="SignalSeries"/> with uniform sample rate.</summary>
    /// <param name="y">Y values sampled at <paramref name="sampleRate"/> Hz starting at <paramref name="xStart"/>.</param>
    /// <param name="sampleRate">Samples per X-unit (e.g., Hz for time-domain data). Defaults to 1.</param>
    /// <param name="xStart">X value of the first sample. Defaults to 0.</param>
    public SignalSeries Signal(double[] y, double sampleRate = 1.0, double xStart = 0.0)
    {
        var series = new SignalSeries(y, sampleRate, xStart);
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
    public bool Visible { get; init; } = true;

    public LegendPosition Position { get; init; } = LegendPosition.Best;

    public int NCols { get; init; } = 1;

    public double? FontSize { get; init; }

    public string? Title { get; init; }

    public double? TitleFontSize { get; init; }

    public bool FrameOn { get; init; } = true;

    public double FrameAlpha { get; init; } = 0.8;

    public bool FancyBox { get; init; }

    public bool Shadow { get; init; }

    public Color? EdgeColor { get; init; }

    public Color? FaceColor { get; init; }

    public double MarkerScale { get; init; } = 1.0;

    public double LabelSpacing { get; init; } = 0.5;

    public double ColumnSpacing { get; init; } = 2.0;
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
    LowerLeft,

    /// <summary>Place the legend along the right edge, vertically centered.</summary>
    Right,

    /// <summary>Place the legend along the left edge, vertically centered.</summary>
    CenterLeft,

    /// <summary>Place the legend along the right edge, vertically centered (alias for Right).</summary>
    CenterRight,

    /// <summary>Place the legend along the bottom edge, horizontally centered.</summary>
    LowerCenter,

    /// <summary>Place the legend along the top edge, horizontally centered.</summary>
    UpperCenter,

    /// <summary>Place the legend at the center of the plot area.</summary>
    Center,

    /// <summary>Place the legend OUTSIDE the plot area, to the right of it. The
    /// constrained-layout engine reserves enough right margin on the figure to fit the
    /// legend box; without <c>TightLayout()</c> or <c>ConstrainedLayout()</c> the legend
    /// may be clipped by the figure edge.</summary>
    OutsideRight,

    /// <summary>Place the legend OUTSIDE the plot area, to the left of it. Requires
    /// <c>TightLayout()</c> / <c>ConstrainedLayout()</c> for the engine to reserve left margin.</summary>
    OutsideLeft,

    /// <summary>Place the legend OUTSIDE the plot area, above it (below the subplot title).
    /// Requires <c>TightLayout()</c> / <c>ConstrainedLayout()</c> for the engine to reserve top margin.</summary>
    OutsideTop,

    /// <summary>Place the legend OUTSIDE the plot area, below the X-axis labels. Requires
    /// <c>TightLayout()</c> / <c>ConstrainedLayout()</c> for the engine to reserve bottom margin.</summary>
    OutsideBottom,
}

/// <summary>Specifies how multiple bar series on the same axes are displayed.</summary>
public enum BarMode
{
    /// <summary>Bars are placed side by side.</summary>
    Grouped,

    /// <summary>Bars are stacked on top of each other.</summary>
    Stacked
}
