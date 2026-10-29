// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Models.Tools;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Lighting;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models;

/// <summary>Represents a set of axes within a figure, onto which data series are plotted. Holds axes state (title,
/// grid, spines, 3D camera/lighting, insets, the Y axis container and the top X axis, breaks) and the collections a series or decoration is
/// added to; the open-ended set of chart-type verbs (<c>Plot</c>, <c>Bar</c>, <c>Heatmap</c>, …) lives as extension
/// members grouped by series family under <c>Models/Series/&lt;family&gt;/</c> — adding a chart type is a new
/// extension member there, never a change here.</summary>
public sealed class Axes
{
    public string? Title { get; set; }

    public TextStyle? TitleStyle { get; set; }

    public TitleLocation TitleLoc { get; set; } = TitleLocation.Center;

    public Axis XAxis { get; } = new();

    /// <summary>The primary Y axis: slot 0 of the left side of <see cref="YAxes"/>.</summary>
    public Axis YAxis => YAxes.Left[0].Axis;

    /// <summary>Every Y axis of this axes: one stack per side, the primary <see cref="YAxis"/> as left slot 0, each
    /// further axis stacked outward past the one before it on its side, as many as wanted.</summary>
    public YAxisContainer YAxes { get; } = new();

    public Axis3D ZAxis { get; } = new();

    public Legend Legend { get; set; } = new();

    public GridStyle Grid { get; set; } = new();

    /// <summary>What this axes asked to change about the grid, if anything: the properties it set, with their values. It is
    /// the CHANGE rather than the result, because the thing being changed is the theme's grid and the theme is not known
    /// when the subplot is built. It is data, so it is what the JSON carries and what a figure drawn under another theme
    /// applies over that theme's grid. An axes that asked nothing takes the theme's grid whole.</summary>
    internal GridChange? GridChange { get; set; }

    /// <summary>The grid this axes draws under <paramref name="theme"/>: the theme's grid with the axes' change when it
    /// asked for one (<see cref="GridChange"/>), its own <see cref="Grid"/> when that is switched on, and the theme's
    /// grid otherwise. The one rule every renderer draws a grid by and the JSON writer resolves a change with.</summary>
    /// <param name="theme">The theme the axes is drawn in.</param>
    internal GridStyle GridUnder(Theme theme) =>
        GridChange is { } change ? change.ApplyTo(theme.DefaultGrid) : Grid.Visible ? Grid : theme.DefaultGrid;

    /// <summary>The grid of an axes whose grid is part of its shape (the rings and spokes of a polar axes, the three back
    /// walls of a 3-D box): <see cref="GridUnder"/>, but drawn unless the axes said otherwise, whatever the theme's grid says.
    /// A chart read by its rings, or standing in a box, is not a chart under a theme that draws no grid; only an explicit
    /// "no grid" (a change that sets <see cref="GridStyle.Visible"/> to false) removes it.</summary>
    /// <param name="theme">The theme the axes is drawn in.</param>
    internal GridStyle ShapeGridUnder(Theme theme) =>
        GridUnder(theme) with { Visible = GridChange?.Visible ?? true };

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

    /// <summary>When true (set by the SVG transform for a figure with the server data cursor), every interactive series
    /// group also carries the mapping its series was drawn with — the plot area and the data range of the series' own
    /// axes — so a click converts to the units of the axis the clicked line belongs to.</summary>
    internal bool EmitDataCursorMapping { get; set; }

    /// <summary>This axes' index in <see cref="Figure.SubPlots"/> while the figure is rendered with server interaction
    /// (set by the SVG transform), otherwise <see langword="null"/>. With an index the renderer writes the axes' view —
    /// its plot area and the range it actually drew — so the page converts hover, brush select, zoom and pan with the
    /// axes under the pointer and names that axes in what it sends.</summary>
    internal int? ServerViewIndex { get; set; }

    public BarMode BarMode { get; set; } = BarMode.Grouped;

    public CoordinateSystem CoordinateSystem { get; set; } = CoordinateSystem.Cartesian;

    public Projection3D? Projection { get; set; }

    /// <summary>Configuration for the three back-facing cube panes (floor, left wall, right wall) in 3D plots.</summary>
    public Pane3DConfig Pane3D { get; set; } = new();

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

    /// <summary>Gets the collection of data series plotted on this axes: the series of the primary <see cref="YAxis"/>,
    /// the same list as left slot 0's <see cref="YAxisSlot.Series"/>.</summary>
    public IReadOnlyList<ISeries> Series => PrimarySeriesList;
    private List<ISeries> PrimarySeriesList => YAxes.Left[0].SeriesList;

    /// <summary>Every series drawn on this subplot, in draw order: every Y axis' own series in
    /// <see cref="YAxisContainer.IdentityOrder"/> (<see cref="Series"/>, then every right slot's, then every left slot's
    /// past the primary), then <see cref="XSecondarySeries"/>.
    /// This is the answer to "what does the picture show" — the data table reads it, and so does anything that
    /// describes the subplot. It is NOT <see cref="Series.IAxesContext.AllSeries"/>, which names only the series
    /// that share ONE axis pair (the set a stacked bar sums over); the two differ on purpose, and each is
    /// documented against the other.</summary>
    public IEnumerable<ISeries> AllSeries =>
        YAxes.IdentityOrder.SelectMany(s => s.Series).Concat(_xSecondarySeries);

    /// <summary>A fresh <see cref="Series.ChartSeries.ColorGroup"/> value: one more than the highest already held by
    /// a series of these axes, or 0 when none holds one. Derived fresh every call, never stored — a counter field
    /// would keep counting past a series removed or an axes read back from JSON, handing out a group number nothing
    /// uses while a lower one sits free. A verb calls this once per group it builds, then sets the same value on
    /// every member of that group.</summary>
    internal int NextColorGroup() =>
        AllSeries.OfType<Models.Series.ChartSeries>().Select(s => s.ColorGroup).Where(g => g.HasValue).Select(g => g!.Value)
            .DefaultIfEmpty(-1).Max() + 1;

    /// <summary>Adds a pre-constructed series to the axes.</summary>
    public T AddSeries<T>(T series) where T : ISeries
    {
        PrimarySeriesList.Add(series);
        return series;
    }

    /// <summary>Removes and returns the LAST primary series, or <see langword="null"/> when there is none. The seat-aware
    /// JSON restore path (<c>AxesSerializationExtensions.CreateOnSlot</c>) needs this: every registered
    /// <c>SeriesRegistry</c> factory lands its series here through its own primary verb (<c>axes.Plot</c>, <c>axes.Bar</c>,
    /// …), so restoring one onto a secondary slot or the top axis means building it normally, then moving it — not
    /// rewriting 70 factories to take a seat parameter.</summary>
    internal ISeries? TakeLastSeries()
    {
        var series = PrimarySeriesList;
        if (series.Count == 0) return null;
        var last = series[^1];
        series.RemoveAt(series.Count - 1);
        return last;
    }

    /// <summary>Gets the collection of text annotations on this axes.</summary>
    public IReadOnlyList<Annotation> Annotations => _annotations;
    private readonly List<Annotation> _annotations = [];

    /// <summary>True when every series on this axes fixes the vertical range itself (a state timeline), so pan and
    /// zoom have nothing to move vertically. An axes with no series, or with one series that does not, is free.</summary>
    internal bool HasFixedYRange => Series.HasFixedYRange();

    /// <summary>True when any axis of this axes (X, Y, any outward-stacked Y, or the secondary X) has a limit set,
    /// by <c>SetXLim</c>/<c>SetYLim</c> or by a zoom or pan applied to it. A set limit is exact, so data can then lie
    /// past the plot area and the series are clipped to it; a range that scaled itself encloses every series and
    /// needs no clip.</summary>
    internal bool HasExplicitLimit =>
        XAxis.HasLimit || YAxes.IdentityOrder.Any(s => s.Axis.HasLimit) || SecondaryXAxis is { HasLimit: true };

    /// <summary>Gets the collection of reference lines on this axes.</summary>
    public IReadOnlyList<ReferenceLine> ReferenceLines => _referenceLines;
    private readonly List<ReferenceLine> _referenceLines = [];

    /// <summary>Gets the collection of trendlines drawn on this axes.</summary>
    public IReadOnlyList<Trendline> Trendlines => _trendlines;
    private readonly List<Trendline> _trendlines = [];

    /// <summary>Gets the collection of horizontal price levels drawn on this axes.</summary>
    public IReadOnlyList<HorizontalLevel> HorizontalLevels => _horizontalLevels;
    private readonly List<HorizontalLevel> _horizontalLevels = [];

    /// <summary>Gets the collection of Fibonacci retracement overlays on this axes.</summary>
    public IReadOnlyList<FibonacciRetracement> FibonacciRetracements => _fibonacciRetracements;
    private readonly List<FibonacciRetracement> _fibonacciRetracements = [];

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

    /// <summary>Adds an inset axes at the specified bounds within this axes. This is the
    /// canonical form; the loose-doubles <see cref="AddInset(double,double,double,double)"/>
    /// overload forwards here.</summary>
    /// <param name="bounds">The fractional position and size of the inset within this axes.</param>
    /// <returns>The newly created inset <see cref="Axes"/> instance.</returns>
    public Axes AddInset(InsetBounds bounds)
    {
        var inset = new Axes { InsetBounds = bounds };
        _insets.Add(inset);
        return inset;
    }

    /// <summary>Adds an inset axes at the specified fractional position within this axes.
    /// Convenience overload that forwards to <see cref="AddInset(InsetBounds)"/>.</summary>
    /// <param name="x">Horizontal position as a fraction of parent width (0-1).</param>
    /// <param name="y">Vertical position as a fraction of parent height (0-1).</param>
    /// <param name="width">Width as a fraction of parent width.</param>
    /// <param name="height">Height as a fraction of parent height.</param>
    /// <returns>The newly created inset <see cref="Axes"/> instance.</returns>
    public Axes AddInset(double x, double y, double width, double height)
        => AddInset(new InsetBounds(x, y, width, height));

    /// <summary>Adds an inset axes at the given fractional position. Alias for
    /// <see cref="AddInset(InsetBounds)"/>.</summary>
    /// <param name="x">Left edge of the inset as a fraction of the parent axes width [0, 1].</param>
    /// <param name="y">Bottom edge of the inset as a fraction of the parent axes height [0, 1].</param>
    /// <param name="width">Width of the inset as a fraction of the parent axes width [0, 1].</param>
    /// <param name="height">Height of the inset as a fraction of the parent axes height [0, 1].</param>
    /// <returns>The new inset <see cref="Axes"/> instance.</returns>
    public Axes InsetAxes(double x, double y, double width, double height)
        => AddInset(x, y, width, height);

    /// <summary>Gets the first (innermost) Y axis on the right, <see langword="null"/> while the right side has none:
    /// right slot 0 of <see cref="YAxes"/>.</summary>
    /// <remarks>Obsolete: read <c>Axes.YAxes.Right[0].Axis</c> instead (<see cref="YAxisStack.FirstOrAdd"/> creates the
    /// slot when there is none). Kept so code written for the one right-hand axis this side had before the container
    /// compiles.</remarks>
    [Obsolete("Use Axes.YAxes.Right[0].Axis, the first right-hand Y axis of the YAxes container (YAxes.Right.FirstOrAdd() creates it when there is none).")]
    public Axis? SecondaryYAxis => YAxes.Right.Count > 0 ? YAxes.Right[0].Axis : null;

    /// <summary>Gets the series plotted against the first right-hand Y axis (right slot 0 only — a further right axis
    /// keeps its series on its own slot).</summary>
    /// <remarks>Obsolete: read <c>Axes.YAxes.Right[0].Series</c> instead (<see cref="YAxisSlot.Series"/>). These series
    /// use a separate <see cref="Rendering.DataTransform"/> based on that axis' own Y range, while sharing the primary
    /// X-axis transform.</remarks>
    [Obsolete("Use Axes.YAxes.Right[0].Series, the series of the first right-hand Y axis of the YAxes container.")]
    public IReadOnlyList<ISeries> SecondarySeries => YAxes.Right.Count > 0 ? YAxes.Right[0].Series : [];

    /// <summary>Gets the Y axes on the right of the plot, index 0 nearest the plot and each further one stacked past the
    /// ones before it: a live view of <see cref="YAxes"/>' right stack.</summary>
    /// <remarks>Obsolete: use <see cref="YAxisContainer.Right"/> (<c>Axes.YAxes.Right</c>), the same axes in the same
    /// order as <see cref="YAxisSlot"/> values.</remarks>
    [Obsolete("Use Axes.YAxes.Right, the stack of right-hand Y axes: the same axes in the same order.")]
    public IReadOnlyList<SecondaryAxisSlot> RightYAxes => _rightYAxes ??= new(YAxes.Right);
    [Obsolete("Only the obsolete Axes.RightYAxes and Axes.LeftYAxes use this view.")]
    private SecondaryAxisSlotView? _rightYAxes;

    /// <summary>Gets the Y axes stacked to the left of the plot past the primary <see cref="YAxis"/>: a live view of
    /// <see cref="YAxes"/>' left stack without its slot 0.</summary>
    /// <remarks>Obsolete: use <see cref="YAxisContainer.Left"/> (<c>Axes.YAxes.Left</c>), whose slot 0 is the primary Y
    /// axis, so the axes this list holds start at index 1 there.</remarks>
    [Obsolete("Use Axes.YAxes.Left, the stack of left-hand Y axes; its slot 0 is the primary Y axis, so the axes this list holds start at index 1.")]
    public IReadOnlyList<SecondaryAxisSlot> LeftYAxes => _leftYAxes ??= new(YAxes.Left);
    [Obsolete("Only the obsolete Axes.RightYAxes and Axes.LeftYAxes use this view.")]
    private SecondaryAxisSlotView? _leftYAxes;

    /// <summary>Makes sure the right side has its first Y axis, creating it if it does not already exist: right slot 0
    /// of <see cref="YAxes"/>. It never stacks.</summary>
    /// <returns>This axes instance for chaining.</returns>
    /// <remarks>Obsolete: use <see cref="YAxisStack.FirstOrAdd"/> on <c>Axes.YAxes.Right</c>, which also hands back the
    /// slot, and <see cref="YAxisStack.Add"/> to stack another.</remarks>
    [Obsolete("Use Axes.YAxes.Right.FirstOrAdd(), which returns the first right-hand Y axis and creates it when there is none; YAxes.Right.Add() stacks another one.")]
    public Axes TwinX()
    {
        YAxes.Right.FirstOrAdd();
        return this;
    }

    /// <summary>Stacks one more Y axis outward on <paramref name="side"/>, past every axis already there on that side.
    /// Every call adds one; there is no cap. On a right side with no axis yet, the axis it adds is right slot 0.</summary>
    /// <param name="side">Which side to stack the new axis on.</param>
    /// <returns>A view of the new axis' slot, the outermost on that side.</returns>
    /// <remarks>Obsolete: use <see cref="YAxisStack.Add"/> on <c>Axes.YAxes.Right</c> or <c>Axes.YAxes.Left</c>, which
    /// returns the <see cref="YAxisSlot"/> itself.</remarks>
    [Obsolete("Use Axes.YAxes.Right.Add() or Axes.YAxes.Left.Add(), which stack one more Y axis on that side and return its YAxisSlot.")]
    public SecondaryAxisSlot AddOutwardYAxis(AxisSide side) => new(YAxes.StackOn(side).Add());

    /// <summary>Adds an already-constructed series to the first right-hand Y axis (right slot 0).</summary>
    /// <remarks>Obsolete: use <c>Axes.YAxes.Right.FirstOrAdd().AddSeries(series)</c> (<see cref="YAxisStack.FirstOrAdd"/>,
    /// <see cref="YAxisSlot.AddSeries{TSeries}"/>), or <c>AddSeries</c> on the slot of the right-hand axis you mean.
    /// It creates right slot 0 when it does not exist yet. This is the seam a STREAMING series arrives through: its ring
    /// buffer is filled after the figure is built, so it cannot be handed in as two arrays the way a verb takes them.</remarks>
    /// <param name="series">The series to plot against the right-hand axis.</param>
    /// <returns>The same series, so the caller can keep appending to it.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="series"/> is null.</exception>
    [Obsolete("Use Axes.YAxes.Right.FirstOrAdd().AddSeries(series), or AddSeries on the slot of the right-hand Y axis you mean.")]
    public TSeries AddSecondarySeries<TSeries>(TSeries series) where TSeries : ISeries
    {
        ArgumentNullException.ThrowIfNull(series);
        return YAxes.Right.FirstOrAdd().AddSeries(series);
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

    /// <summary>Adds an already-constructed series to the secondary X-axis.</summary>
    /// <remarks>Implicitly calls <see cref="TwinY"/> to ensure the secondary axis exists. The
    /// <see cref="_xSecondarySeries"/> counterpart of <see cref="YAxisSlot.AddSeries{TSeries}"/>, which a Y axis slot
    /// adds its series through.</remarks>
    /// <param name="series">The series to plot against the top axis.</param>
    /// <returns>The same series, so the caller can keep appending to it.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="series"/> is null.</exception>
    public TSeries AddXSecondarySeries<TSeries>(TSeries series) where TSeries : ISeries
    {
        ArgumentNullException.ThrowIfNull(series);
        TwinY();
        _xSecondarySeries.Add(series);
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

    /// <summary>Adds a horizontal reference line at the specified Y value.
    /// <para>The automatic range of the Y axis holds the line, as matplotlib's <c>axhline</c> updates the data limits,
    /// so a line beyond the data is drawn inside the plot; a line beyond a limit you set is not drawn, nor its
    /// label.</para></summary>
    public ReferenceLine AxHLine(double y)
    {
        var line = new ReferenceLine(y, Orientation.Horizontal);
        _referenceLines.Add(line);
        return line;
    }

    /// <summary>Adds a vertical reference line at the specified X value.
    /// <para>The automatic range of the X axis holds the line, as matplotlib's <c>axvline</c> updates the data limits,
    /// so a line beyond the data is drawn inside the plot; a line beyond a limit you set is not drawn, nor its
    /// label.</para></summary>
    public ReferenceLine AxVLine(double x)
    {
        var line = new ReferenceLine(x, Orientation.Vertical);
        _referenceLines.Add(line);
        return line;
    }

    /// <summary>Adds a trendline between two data-coordinate points.</summary>
    public Trendline AddTrendline(double x1, double y1, double x2, double y2)
    {
        var line = new Trendline(x1, y1, x2, y2);
        _trendlines.Add(line);
        return line;
    }

    internal void AddTrendline(Trendline line) => _trendlines.Add(line);

    internal void RemoveTrendline(Trendline line) => _trendlines.Remove(line);

    /// <summary>Adds a horizontal price level (support/resistance) at the given value.
    /// <para>The automatic range of the Y axis holds the level, so a level beyond the data is drawn inside the plot; a
    /// level beyond a limit you set is not drawn, nor its label.</para></summary>
    public HorizontalLevel AddLevel(double value)
    {
        var level = new HorizontalLevel(value);
        _horizontalLevels.Add(level);
        return level;
    }

    internal void AddLevel(HorizontalLevel level) => _horizontalLevels.Add(level);

    internal void RemoveLevel(HorizontalLevel level) => _horizontalLevels.Remove(level);

    /// <summary>Adds a Fibonacci retracement overlay between a price high and a price low.
    /// <para>The automatic range of the Y axis holds every level, from the high to the low; a level beyond a limit you
    /// set is not drawn, nor its label.</para></summary>
    public FibonacciRetracement AddFibonacci(double priceHigh, double priceLow)
    {
        var fib = new FibonacciRetracement(priceHigh, priceLow);
        _fibonacciRetracements.Add(fib);
        return fib;
    }

    internal void AddFibonacci(FibonacciRetracement fib) => _fibonacciRetracements.Add(fib);

    internal void RemoveFibonacci(FibonacciRetracement fib) => _fibonacciRetracements.Remove(fib);

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

    /// <summary>Adds a threshold marker composed of a dashed <see cref="ReferenceLine"/> at
    /// <paramref name="value"/> and a shaded <see cref="SpanRegion"/> covering the breach zone
    /// on the <paramref name="breach"/> side. An optional text <paramref name="label"/> is
    /// added as an <see cref="Annotation"/> anchored at the threshold value.
    /// <para>The span runs from the threshold to ±<see cref="double.MaxValue"/>; the renderer clips a span to the plot area,
    /// so the zone fills the plot from the threshold line to the plot edge on the breach side. The automatic range
    /// holds the threshold's line; a threshold beyond a limit set on the axis draws neither its line nor its
    /// zone.</para></summary>
    /// <param name="value">The threshold value in data coordinates.</param>
    /// <param name="orientation">Horizontal (constant Y) or Vertical (constant X).</param>
    /// <param name="breach">Which side of the threshold is the breach zone.</param>
    /// <param name="color">Optional color for the reference line and span. When null the renderer's cycle color is used.</param>
    /// <param name="label">Optional text annotation placed at the threshold value. When null no annotation is added.</param>
    public void AddThreshold(double value, Orientation orientation, ThresholdBreach breach,
        Color? color = null, string? label = null)
    {
        // (a) Dashed reference line at the threshold value
        var line = orientation == Orientation.Horizontal
            ? AxHLine(value)
            : AxVLine(value);
        line.LineStyle = LineStyle.Dashed;
        if (color is not null) line.Color = color;

        // (b) Shaded span covering the breach zone
        double spanMin = breach == ThresholdBreach.Above ? value : double.MinValue;
        double spanMax = breach == ThresholdBreach.Above ? double.MaxValue : value;
        var span = orientation == Orientation.Horizontal
            ? AxHSpan(spanMin, spanMax)
            : AxVSpan(spanMin, spanMax);
        if (color is not null) span.Color = color;

        // (c) Optional text annotation
        if (label is not null)
        {
            double annotX = orientation == Orientation.Horizontal ? 0 : value;
            double annotY = orientation == Orientation.Horizontal ? value : 0;
            Annotate(label, annotX, annotY);
        }
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

    /// <summary>When <see langword="true"/>, each legend entry's label is suffixed with the
    /// series' last data value formatted as <c>" = {value:F2}"</c> (InvariantCulture).
    /// Only <see cref="Models.Series.XYSeries"/> instances yield a value; all other series types
    /// fall back to the label text only. Default <see langword="false"/>.</summary>
    public bool LegendValues { get; init; }
}

/// <summary>Specifies the position of a legend within the axes.</summary>
/// <remarks><b>Append-only ordinal contract:</b> never reorder,
/// remove, or renumber.</remarks>
public enum LegendPosition
{
    /// <summary>The place that covers the fewest marks, as matplotlib's <c>loc='best'</c> chooses it: the first of ten places
    /// (upper right, upper left, lower left, lower right, right, centre left, centre right, lower centre, upper centre, centre)
    /// with the lowest score, where a place scores the vertices of lines and the centres of points inside the legend's box, the
    /// bars and boxes that overlap it and the paths that cross it. Every series counts, a hidden one too, and so does the
    /// text of an annotation at its point. Cartesian, polar and 3-D axes look for room; the legend of a map stands in the
    /// upper right. A legend that names no position is at <c>Best</c>, and the theme says what that means:
    /// <see cref="Styling.Theme.DefaultLegendPosition"/> (the upper right under the classic theme, <c>Best</c> otherwise).</summary>
    Best = 0,

    /// <summary>Place the legend in the upper-right corner.</summary>
    UpperRight = 1,

    /// <summary>Place the legend in the upper-left corner.</summary>
    UpperLeft = 2,

    /// <summary>Place the legend in the lower-right corner.</summary>
    LowerRight = 3,

    /// <summary>Place the legend in the lower-left corner.</summary>
    LowerLeft = 4,

    /// <summary>Place the legend along the right edge, vertically centered.</summary>
    Right = 5,

    /// <summary>Place the legend along the left edge, vertically centered.</summary>
    CenterLeft = 6,

    /// <summary>Place the legend along the right edge, vertically centered (alias for Right).</summary>
    CenterRight = 7,

    /// <summary>Place the legend along the bottom edge, horizontally centered.</summary>
    LowerCenter = 8,

    /// <summary>Place the legend along the top edge, horizontally centered.</summary>
    UpperCenter = 9,

    /// <summary>Place the legend at the center of the plot area.</summary>
    Center = 10,

    /// <summary>Place the legend OUTSIDE the plot area, to the right of it. The
    /// constrained-layout engine reserves enough right margin on the figure to fit the
    /// legend box; without <c>TightLayout()</c> or <c>ConstrainedLayout()</c> the legend
    /// may be clipped by the figure edge.</summary>
    OutsideRight = 11,

    /// <summary>Place the legend OUTSIDE the plot area, to the left of it. Requires
    /// <c>TightLayout()</c> / <c>ConstrainedLayout()</c> for the engine to reserve left margin.</summary>
    OutsideLeft = 12,

    /// <summary>Place the legend OUTSIDE the plot area, above it (below the subplot title).
    /// Requires <c>TightLayout()</c> / <c>ConstrainedLayout()</c> for the engine to reserve top margin.</summary>
    OutsideTop = 13,

    /// <summary>Place the legend OUTSIDE the plot area, below the X-axis labels. Requires
    /// <c>TightLayout()</c> / <c>ConstrainedLayout()</c> for the engine to reserve bottom margin.</summary>
    OutsideBottom = 14,
}

/// <summary>Specifies how multiple bar series on the same axes are displayed.</summary>
/// <remarks><b>Append-only ordinal contract:</b> never reorder,
/// remove, or renumber.</remarks>
public enum BarMode
{
    /// <summary>Bars are placed side by side.</summary>
    Grouped = 0,

    /// <summary>Bars are stacked on top of each other.</summary>
    Stacked = 1,
}
