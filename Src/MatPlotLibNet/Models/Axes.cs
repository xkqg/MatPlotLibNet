// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Patches;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Models.Tools;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Lighting;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models;

/// <summary>Represents a set of axes within a figure, onto which data series are plotted. Holds axes state (title,
/// grid, spines, 3D camera/lighting, insets, the Y axis container and the top X axis, breaks) and the collections a series or decoration is
/// added to (the overlays are held by an internal owner of their own, which the overlay members here forward to); the open-ended set of chart-type verbs (<c>Plot</c>, <c>Bar</c>, <c>Heatmap</c>, …) lives as extension
/// members grouped by series family under <c>Models/Series/&lt;family&gt;/</c> — adding a chart type is a new
/// extension member there, never a change here.</summary>
public sealed class Axes : IChangeTracked
{
    private long _stamp;

    /// <summary>Stores <paramref name="value"/> and marks this axes changed, unless it already holds an equal value.</summary>
    private void Change<T>(ref T slot, T value) => ChangeClock.Change(ref slot, value, ref _stamp);

    /// <summary>The <see cref="ChangeClock"/> mark of the newest change to anything this axes draws: its own settings, each of
    /// its axes, every series on every one of them, every overlay, patch and break, and every inset with all it holds. A draw
    /// that remembers this mark can tell whether the axes would draw the same again. What it shares with another axes (a scale
    /// through <see cref="ShareXWith"/>/<see cref="ShareYWith"/>) is that axes' own mark, not this one's.</summary>
    internal long LatestChange
    {
        get
        {
            long latest = ChangeClock.Latest(_stamp, XAxis.LatestChange);
            latest = ChangeClock.Latest(latest, YAxes.LatestChange);
            latest = ChangeClock.Latest(latest, ZAxis.LatestChange);
            if (SecondaryXAxis is { } top) latest = ChangeClock.Latest(latest, top.LatestChange);
            latest = ChangeClock.Latest(latest, _xSecondarySeries.LatestChange);
            latest = ChangeClock.Latest(latest, Overlays.LatestChange);
            if (_camera is { } camera) latest = ChangeClock.Latest(latest, camera.LatestChange);
            latest = ChangeClock.Latest(latest, Breaks.X.LatestChange);
            latest = ChangeClock.Latest(latest, Breaks.Y.LatestChange);
            latest = ChangeClock.Latest(latest, _insets.LatestChange);
            return ChangeClock.Latest(latest, ChangeClock.LatestOf(ZoomIndicator));
        }
    }

    long IChangeTracked.LatestChange => LatestChange;

    /// <summary>The owner of what is drawn over the data besides the series: the eight overlay lists, the lines, spans and
    /// annotations made into them, the threshold composed of those, and their share of <see cref="LatestChange"/>. The public
    /// overlay members of this axes forward to it.</summary>
    internal AxesOverlays Overlays { get; } = new();

    // The legend, grid, spines and panes every axes starts with. Records no one can change, so every axes shares them: a
    // change is a new record on that axes alone.
    private static readonly Legend RestingLegend = new();
    private static readonly GridStyle RestingGrid = new();
    private static readonly SpinesConfig RestingSpines = new();
    private static readonly Pane3DConfig RestingPane3D = new();

    /// <summary>The title drawn above the plot area. <see langword="null"/> (the default) draws none. A title with a pair of
    /// <c>$</c> delimiters is typeset as math, as matplotlib's mathtext is.</summary>
    public string? Title { get; set => Change(ref field, value); }

    /// <summary>Overrides of the title's font (size, weight, family, slant, colour) and its extra distance from the plot
    /// area (<see cref="TextStyle.Pad"/>); whatever it leaves unset follows the theme. <see langword="null"/> (the
    /// default) draws the title in the theme's title font.</summary>
    public TextStyle? TitleStyle { get; set => Change(ref field, value); }

    /// <summary>Where the title stands along the top of the plot area: centred by default, or flush with its left or right
    /// edge.</summary>
    public TitleLocation TitleLoc { get; set => Change(ref field, value); } = RestingTitleLoc;

    /// <summary>Where a title stands along the top of the plot when it is told otherwise.</summary>
    internal const TitleLocation RestingTitleLoc = TitleLocation.Center;

    /// <summary>The X axis: its label, limits, scale, tick settings and tick formatter or locator.</summary>
    public Axis XAxis { get; } = new();

    /// <summary>The primary Y axis: slot 0 of the left side of <see cref="YAxes"/>.</summary>
    public Axis YAxis => YAxes.Left[0].Axis;

    /// <summary>Every Y axis of this axes: one stack per side, the primary <see cref="YAxis"/> as left slot 0, each
    /// further axis stacked outward past the one before it on its side, as many as wanted.</summary>
    public YAxisContainer YAxes { get; }

    /// <summary>Creates an empty axes: Cartesian, with its primary Y axis and no series.</summary>
    public Axes() => YAxes = new(this);

    /// <summary>The Z axis of a three-dimensional axes: its label, limits and ticks. A flat axes never reads it.</summary>
    public Axis3D ZAxis { get; } = new();

    /// <summary>The legend settings: whether it is drawn, where it stands, how its entries are laid out and how its frame
    /// looks. The default is a visible legend where <see cref="Legend.Position"/> starts; a legend with no labelled series draws
    /// nothing either way.</summary>
    public Legend Legend { get; set => Change(ref field, value); } = RestingLegend;

    /// <summary>The grid this axes asks for. It takes effect only when its <see cref="GridStyle.Visible"/> is on; an axes
    /// that does not switch it on draws the theme's grid.</summary>
    public GridStyle Grid { get; set => Change(ref field, value); } = RestingGrid;

    /// <summary>What this axes asked to change about the grid, if anything: the properties it set, with their values. It is
    /// the CHANGE rather than the result, because the thing being changed is the theme's grid and the theme is not known
    /// when the subplot is built. It is data, so it is what the JSON carries and what a figure drawn under another theme
    /// applies over that theme's grid. An axes that asked nothing takes the theme's grid whole.</summary>
    internal GridChange? GridChange { get; set => Change(ref field, value); }

    /// <summary>The four spines (the border lines of the plot area): which are drawn, where each stands and how it looks.
    /// Read by Cartesian axes.</summary>
    public SpinesConfig Spines { get; set => Change(ref field, value); } = RestingSpines;

    /// <summary>The number of rows of the rows-by-columns grid this axes was added to with
    /// <c>AddSubPlot(rows, cols, index)</c>. 0 when the axes was placed another way, by a <see cref="GridSpec"/> or not
    /// at all.</summary>
    public int GridRows { get; internal set => Change(ref field, value); }

    /// <summary>The number of columns of the rows-by-columns grid this axes was added to; 0 when it was placed another
    /// way. See <see cref="GridRows"/>.</summary>
    public int GridCols { get; internal set => Change(ref field, value); }

    /// <summary>The one-based cell of that grid this axes occupies, counted along the rows from the top left (1 is the
    /// top-left cell); 0 when the axes was placed another way. See <see cref="GridRows"/>.</summary>
    public int GridIndex { get; internal set => Change(ref field, value); }

    /// <summary>The cells of the figure's <see cref="Models.GridSpec"/> this axes occupies, which can span several rows
    /// or columns. <see langword="null"/> when the axes was not placed through a grid specification.</summary>
    public GridPosition? GridPosition { get; internal set => Change(ref field, value); }

    /// <summary>Another axes whose X scale this one shares: the two draw the same X range, each contributing its data to it.
    /// <see langword="null"/> (the default) leaves the X range to this axes alone.</summary>
    public Axes? ShareXWith { get; internal set => Change(ref field, value); }

    /// <summary>Another axes whose Y scale this one shares: the two draw the same Y range, each contributing its data to it.
    /// <see langword="null"/> (the default) leaves the Y range to this axes alone.</summary>
    public Axes? ShareYWith { get; internal set => Change(ref field, value); }

    /// <summary>A name that identifies this axes within its figure, so that another axes can name it to share a scale with
    /// it and the JSON can restore that sharing. <see langword="null"/> (the default) names nothing.</summary>
    public string? Key { get; set => Change(ref field, value); }

    /// <summary>The key of the subplot whose X scale this axes asked to share (<c>AxesBuilder.ShareX</c>, a document's
    /// <c>shareXKey</c>), until the figure joins the two (<see cref="FigureShareExtensions.ShareByKeys"/>): the subplot it names may be
    /// added after this one. Not drawn and not written; <see cref="ShareXWith"/> is.</summary>
    internal string? ShareXKey { get; set; }

    /// <summary>The key of the subplot whose Y scale this axes asked to share, until the figure joins the two.</summary>
    internal string? ShareYKey { get; set; }

    /// <summary>Whether the indicators added to this axes stand at the centres of bar slots (slot [i, i + 1], centre at i + 0.5),
    /// as <c>AxesBuilder.UseBarSlotX</c> says for a panel whose categories stand on another axes. Read where an indicator is placed
    /// (<see cref="AxesBuilderIndicatorVerbs"/>); not drawn and not written.</summary>
    internal bool BarSlotX { get; set; }

    /// <summary>Whether the data elements of this axes carry native SVG tooltips that show their values on hover.
    /// Off by default.</summary>
    public bool EnableTooltips { get; set => Change(ref field, value); }

    /// <summary>Whether each series is always drawn in an SVG group that carries its series index and label as attributes, so
    /// the interaction scripts (such as the legend toggle and the highlight) can find it. Off by default, which leaves the output
    /// plain; a figure that asks for any interactivity has its SVG carry the attributes anyway, for that draw only, without
    /// changing this property.</summary>
    public bool EnableInteractiveAttributes { get; set => Change(ref field, value); }

    /// <summary>How bar series that share these axes are laid out: side by side (<see cref="RestingBarMode"/>, the
    /// default) or piled on top of each other (<see cref="Models.BarMode.Stacked"/>).</summary>
    public BarMode BarMode { get; set => Change(ref field, value); } = RestingBarMode;

    /// <summary>How the bar series of a new axes are laid out: side by side.</summary>
    internal const BarMode RestingBarMode = BarMode.Grouped;

    /// <summary>The kind of coordinate system the axes is drawn in, which decides which renderer draws it: Cartesian by
    /// default, or polar, three-dimensional or ternary.</summary>
    public CoordinateSystem CoordinateSystem { get; set => Change(ref field, value); } = RestingCoordinateSystem;

    /// <summary>The coordinate system an axes is drawn in when no series asks for another.</summary>
    internal const CoordinateSystem RestingCoordinateSystem = CoordinateSystem.Cartesian;

    /// <summary>The radius a polar axes draws at its centre, as matplotlib's <c>set_rorigin</c>. <see langword="null"/> (the
    /// default) puts the low end of the radial axis there (matplotlib's <c>rmin</c>: 0, a limit set with <c>SetYLim</c>, or the
    /// radius below zero the data reaches); an origin below the low end leaves a hole at the centre, bounded by a spine drawn as the
    /// bottom spine is. Only a polar axes reads it.</summary>
    public double? ROrigin { get; set => Change(ref field, value); }

    /// <summary>Where a three-dimensional axes is seen from and what it is lit by: the owner its camera members
    /// (<see cref="Projection"/>, <see cref="Elevation"/>, <see cref="Azimuth"/>, <see cref="CameraDistance"/>,
    /// <see cref="LightSource"/>) forward to. Made when it is first used, as most axes are flat and never need one.</summary>
    internal Camera3D Camera => _camera ??= new();
    private Camera3D? _camera;

    /// <summary>Where the camera stands, without making one: the resting view for an axes whose camera was never used.</summary>
    internal CameraView CameraView => _camera?.View ?? CameraView.Resting;

    /// <summary>The projection a three-dimensional axes is currently drawn through. When set, its elevation and azimuth
    /// win over <see cref="Elevation"/> and <see cref="Azimuth"/>; <see langword="null"/> (the default) builds the
    /// projection from those two. A rotation or a Home resets it to <see langword="null"/> so the next draw builds it
    /// again.</summary>
    public Projection3D? Projection { get => _camera?.Projection; set => Camera.Projection = value; }

    /// <summary>Configuration for the three back-facing cube panes (floor, left wall, right wall) in 3D plots.</summary>
    public Pane3DConfig Pane3D { get; set => Change(ref field, value); } = RestingPane3D;

    /// <summary>How much longer one unit of Y is than one unit of X on the page, as matplotlib's <c>set_aspect</c> says it:
    /// 1 makes a square of data a square on the page, 2 makes a unit of Y twice as long as a unit of X.
    /// <see langword="null"/> (the default) leaves it to the series: an image under the matplotlib themes asks 1 (matplotlib's
    /// <c>image.aspect</c> <c>'equal'</c>), a map 1, and any other plot fills the cell it is given. A plot with an aspect is drawn in the
    /// largest rectangle of the shape that aspect asks for that fits the cell, centred in it, so the ticks, the labels, the
    /// colour bar and the hit-testing all follow the rectangle that is drawn. A Cartesian axes only; a polar axes has its
    /// own geometry and a 3-D axes its box aspect, and both leave this alone. Has to be positive and finite to take effect: 0
    /// (<see cref="AxesBuilderFrameVerbs.SetAspectAuto"/>, matplotlib's <c>'auto'</c>) lets the plot fill its cell whatever its series ask.</summary>
    public double? Aspect { get; set => Change(ref field, value); }

    /// <summary>Camera elevation above the XY plane in degrees. Default <see cref="Camera3D.RestingElevation"/>.</summary>
    public double Elevation { get => CameraView.Elevation; set => Camera.Elevation = value; }

    /// <summary>Camera azimuth rotation around the Z axis in degrees. Default <see cref="Camera3D.RestingAzimuth"/>.</summary>
    public double Azimuth { get => CameraView.Azimuth; set => Camera.Azimuth = value; }

    /// <summary>The camera's distance from the centre of a 3-D box, at least 2; null (the default) keeps matplotlib's default
    /// camera, at <see cref="Camera3D.RestingDistance"/>. The projection is a perspective one either way: a closer camera bends the
    /// box more and keeps its size, as matplotlib's focal length does (a distance d draws as focal length d / 10).</summary>
    public double? CameraDistance { get => CameraView.Distance; set => Camera.Distance = value; }

    /// <summary>The light that shades the faces of 3-D surfaces and bars. Null (the default) leaves a surface unshaded and lights
    /// a 3-D bar as matplotlib's <c>bar3d</c> does by default: from an azimuth of 225 degrees and an altitude of 19.47.</summary>
    public ILightSource? LightSource { get => _camera?.Light; set => Camera.Light = value; }

    /// <summary>When true, a 3-D axes always writes the normalized vertex data of its faces as <c>data-v3d</c> attributes for
    /// interactive rotation. Off by default; a figure with <see cref="Figure.Enable3DRotation"/> has its SVG carry them anyway,
    /// for that draw only, without changing this property.</summary>
    public bool Emit3DVertexData { get; set => Change(ref field, value); }

    /// <summary>The colour bar drawn beside this axes' plot area, or <see langword="null"/> (the default) for none. It is
    /// drawn only when its <see cref="Models.ColorBar.Visible"/> is on.</summary>
    public ColorBar? ColorBar { get; set => Change(ref field, value); }

    /// <summary>Gets the collection of data series plotted on this axes: the series of the primary <see cref="YAxis"/>,
    /// the same list as left slot 0's <see cref="YAxisSlot.Series"/>.</summary>
    public IReadOnlyList<ISeries> Series => PrimarySeriesList;
    internal StampedList<ISeries> PrimarySeriesList => YAxes.Left[0].SeriesList;

    /// <summary>Every series drawn on this subplot, in draw order: every Y axis' own series in
    /// <see cref="YAxisContainer.IdentityOrder"/> (<see cref="Series"/>, then every right slot's, then every left slot's
    /// past the primary), then <see cref="XSecondarySeries"/>.
    /// This is the answer to "what does the picture show" — the data table reads it, and so does anything that
    /// describes the subplot. It is NOT <see cref="Series.IAxesContext.AllSeries"/>, which names only the series
    /// that share ONE axis pair (the set a stacked bar sums over); the two differ on purpose, and each is
    /// documented against the other.</summary>
    public IEnumerable<ISeries> AllSeries =>
        YAxes.IdentityOrder.SelectMany(s => s.Series).Concat(_xSecondarySeries);

    /// <summary>Adds a pre-constructed series to the axes, plotted against the primary Y axis (left slot 0, whose
    /// <see cref="YAxisSlot.AddSeries{TSeries}"/> this is).</summary>
    /// <param name="series">The series to add.</param>
    /// <returns>The same series, so the caller can keep configuring it.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="series"/> is null: it is refused here, at the call, and not
    /// left to fail during the draw.</exception>
    /// <remarks>A kind drawn in a coordinate system of its own (<c>ChartSeries.DrawnIn</c>: polar, ternary, 3-D) puts the axes in it, as
    /// the verb that makes it does (at every seat).</remarks>
    public T AddSeries<T>(T series) where T : ISeries => YAxes.Left[0].AddSeries(series);

    /// <summary>Gets the collection of text annotations on this axes.</summary>
    public IReadOnlyList<Annotation> Annotations => Overlays.Annotations;

    /// <summary>True when the frame of these axes is fixed by what it draws, so a zoom or a pan has nothing to move: the
    /// triangle of a ternary diagram holds every composition there is. The events that move a view leave such an axes alone
    /// (<see cref="Interaction.FigureInteractionEvent"/>), whoever sent them and by whichever route they came.</summary>
    internal bool HasFixedFrame => CoordinateSystem == CoordinateSystem.Ternary;

    /// <summary>True when every series on this axes fixes the vertical range itself (a state timeline), so pan and
    /// zoom have nothing to move vertically. An axes with no series, or with one series that does not, is free.</summary>
    internal bool HasFixedYRange => Series.HasFixedYRange();

    /// <summary>True when any axis of this axes (X, Y, any outward-stacked Y, or the secondary X) has a limit set,
    /// by <c>SetXLim</c>/<c>SetYLim</c> or by a zoom or pan applied to it. A set limit is exact, so data can then lie
    /// past the plot area and the series are clipped to it; a range that scaled itself encloses every series and
    /// needs no clip. A linked axis (<see cref="Axis.Link"/>) has no range of its own, so a limit it carries is none of the
    /// axes' (<see cref="Axis.LimitsItsOwnRange"/>); the primary axes' limits always count. A polar axes ranges along its radius alone
    /// (its Y axis): a limit on the angle cuts nothing. Asked of the axes as they stand, with nothing kept between calls and nothing
    /// allocated: the clip rule asks it once per series per draw.</summary>
    internal bool HasExplicitLimit => CoordinateSystem == CoordinateSystem.Polar
        ? YAxis.HasLimit
        : XAxis.HasLimit || YAxis.HasLimit || YAxes.Left.AnyLimitsItsOwnRange || YAxes.Right.AnyLimitsItsOwnRange
          || SecondaryXAxis is { LimitsItsOwnRange: true };

    /// <summary>Gets the collection of reference lines on this axes.</summary>
    public IReadOnlyList<ReferenceLine> ReferenceLines => Overlays.ReferenceLines;

    /// <summary>Gets the collection of trendlines drawn on this axes.</summary>
    public IReadOnlyList<Trendline> Trendlines => Overlays.Trendlines;

    /// <summary>Gets the collection of horizontal price levels drawn on this axes.</summary>
    public IReadOnlyList<HorizontalLevel> HorizontalLevels => Overlays.HorizontalLevels;

    /// <summary>Gets the collection of Fibonacci retracement overlays on this axes.</summary>
    public IReadOnlyList<FibonacciRetracement> FibonacciRetracements => Overlays.FibonacciRetracements;

    /// <summary>Gets the collection of shaded span regions on this axes.</summary>
    public IReadOnlyList<SpanRegion> Spans => Overlays.Spans;

    /// <summary>Gets the collection of buy/sell signal markers on this axes.</summary>
    public IReadOnlyList<SignalMarker> Signals => Overlays.Signals;

    /// <summary>The patches of this axes (matplotlib's <c>Axes.patches</c>): shapes in data coordinates, in the order they were
    /// added with <see cref="AddPatch{TPatch}"/>.</summary>
    public IReadOnlyList<Patch> Patches => Overlays.Patches;

    /// <summary>Adds a patch the caller has made (a <see cref="Rectangle"/>, a <see cref="Polygon"/>, an <see cref="Arrow"/>) to
    /// this axes, as matplotlib's <c>add_patch</c> does. The axes holds that very patch, so a change made to it afterwards is
    /// drawn.</summary>
    /// <typeparam name="TPatch">The kind of patch, so the caller keeps the concrete type.</typeparam>
    /// <param name="patch">The patch to add.</param>
    /// <returns>The same patch, so the caller can keep configuring it.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="patch"/> is null.</exception>
    /// <example>
    /// <code>
    /// var axes = new Figure().AddSubPlot();
    /// var circle = axes.AddPatch(new Circle(new(2, 3), 1.5));
    /// circle.Label = "Reach";
    /// </code>
    /// </example>
    public TPatch AddPatch<TPatch>(TPatch patch) where TPatch : Patch => Overlays.AddPatch(patch, Series.Count);

    /// <summary>Gets the collection of inset axes rendered within this axes.</summary>
    public IReadOnlyList<Axes> Insets => _insets;
    private readonly StampedList<Axes> _insets = new();

    /// <summary>Gets the axis break regions on the X-axis.</summary>
    public IReadOnlyList<AxisBreak> XBreaks => Breaks.X;

    /// <summary>Gets the axis break regions on the Y-axis.</summary>
    public IReadOnlyList<AxisBreak> YBreaks => Breaks.Y;

    /// <summary>The breaks of each axis, one list per direction (<see cref="XBreaks"/>, <see cref="YBreaks"/>), added to through
    /// <see cref="AxesSeatExtensions.AddBreak"/>.</summary>
    internal AxisPair<StampedList<AxisBreak>> Breaks { get; } = new(new(), new());

    /// <summary>Adds a discontinuous (broken) region to the X-axis, hiding data between
    /// <paramref name="from"/> and <paramref name="to"/> and compressing the scale.</summary>
    public Axes AddXBreak(double from, double to, BreakStyle style = AxisBreak.RestingStyle) => this.AddBreak(isX: true, from, to, style);

    /// <summary>Adds a discontinuous (broken) region to the Y-axis, hiding data between
    /// <paramref name="from"/> and <paramref name="to"/> and compressing the scale.</summary>
    public Axes AddYBreak(double from, double to, BreakStyle style = AxisBreak.RestingStyle) => this.AddBreak(isX: false, from, to, style);

    /// <summary>Where this axes stands inside its parent, as fractions of the parent's plot area, when it is an inset
    /// (<see cref="AddInset(InsetBounds)"/> sets it). <see langword="null"/> for an axes that is not an inset.</summary>
    public InsetBounds? InsetBounds { get; internal set => Change(ref field, value); }

    /// <summary>The zoom this axes indicates on its parent when it is an inset (matplotlib's <c>indicate_inset_zoom</c>): the
    /// parent draws a rectangle over the region this inset shows, and the lines from it to this inset's frame.
    /// <see langword="null"/> (the default) draws none. Drawn only for an inset (<see cref="InsetBounds"/> set) of a Cartesian
    /// parent; on any other axes it is kept and draws nothing. <c>AxesBuilder.IndicateZoom</c> on the inset's builder sets it.</summary>
    public ZoomIndicator? ZoomIndicator { get; set => Change(ref field, value); }

    /// <summary>Adds an inset axes at the specified bounds within this axes, the form the overloads forward to.</summary>
    /// <param name="bounds">The fractional position and size of the inset within this axes.</param>
    /// <returns>The newly created inset <see cref="Axes"/> instance.</returns>
    public Axes AddInset(InsetBounds bounds)
    {
        var inset = new Axes { InsetBounds = bounds };
        _insets.Add(inset);
        return inset;
    }

    /// <summary>Adds an inset axes at the specified fractional position within this axes.</summary>
    /// <param name="x">Left edge of the inset as a fraction of the parent width, counted from the parent's left edge (0-1).</param>
    /// <param name="y">Top edge of the inset as a fraction of the parent height, counted down from the parent's top edge
    /// (0 is the top), as <see cref="Models.InsetBounds"/> counts it; matplotlib's <c>inset_axes</c> counts up from the bottom.</param>
    /// <param name="width">Width as a fraction of parent width.</param>
    /// <param name="height">Height as a fraction of parent height.</param>
    /// <returns>The newly created inset <see cref="Axes"/> instance.</returns>
    public Axes AddInset(double x, double y, double width, double height)
        => AddInset(new InsetBounds(x, y, width, height));

    /// <summary>Adds an inset axes at the given fractional position: an alias of <see cref="AddInset(double,double,double,double)"/>.</summary>
    /// <inheritdoc cref="AddInset(double,double,double,double)"/>
    public Axes InsetAxes(double x, double y, double width, double height)
        => AddInset(x, y, width, height);

    // ── TwinY — secondary X-axis (top edge) ─────────────────────────────────

    /// <summary>The secondary X axis along the top edge, or <see langword="null"/> until <see cref="TwinY"/> (or adding a
    /// series to it) creates it.</summary>
    public Axis? SecondaryXAxis { get; private set => Change(ref field, value); }

    /// <summary>The secondary X axis when it is drawn (<see cref="Axis.IsDrawnWith"/>: it has series, or it is linked), the one that owns
    /// the top edge; <see langword="null"/> when the axes has none or it is placed nowhere it is seen.</summary>
    internal Axis? DrawnSecondaryXAxis => SecondaryXAxis is { } top && top.IsDrawnWith(XSecondarySeries) ? top : null;

    /// <summary>Gets the collection of data series plotted against the secondary X-axis.</summary>
    public IReadOnlyList<ISeries> XSecondarySeries => _xSecondarySeries;
    private readonly StampedList<ISeries> _xSecondarySeries = new();

    /// <summary>Enables a secondary X-axis (top edge) on this axes, creating it if it does not already exist.</summary>
    /// <returns>This axes instance for chaining.</returns>
    public Axes TwinY()
    {
        SecondaryXAxis ??= new Axis();
        return this;
    }

    /// <summary>Adds an already-constructed series to the secondary X-axis.</summary>
    /// <remarks>Implicitly calls <see cref="TwinY"/> to ensure the secondary axis exists. The
    /// top X axis' counterpart of <see cref="YAxisSlot.AddSeries{TSeries}"/>, which a Y axis slot
    /// adds its series through.</remarks>
    /// <param name="series">The series to plot against the top axis.</param>
    /// <returns>The same series, so the caller can keep appending to it.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="series"/> is null.</exception>
    public TSeries AddXSecondarySeries<TSeries>(TSeries series) where TSeries : ISeries
    {
        ArgumentNullException.ThrowIfNull(series);
        TwinY();
        this.TakeTheSystemOf(series);
        _xSecondarySeries.Add(series);
        return series;
    }

    /// <summary>Adds a buy or sell signal marker at the specified data coordinates.</summary>
    public SignalMarker AddSignal(double x, double y, SignalDirection direction) => Overlays.AddSignal(x, y, direction);

    /// <summary>Adds a text annotation at the specified data coordinates.</summary>
    public Annotation Annotate(string text, double x, double y) => Overlays.Annotate(text, x, y);

    /// <summary>Adds a horizontal reference line at the specified Y value.
    /// <para>The automatic range of the Y axis holds the line, as matplotlib's <c>axhline</c> updates the data limits,
    /// so a line beyond the data is drawn inside the plot; a line beyond a limit you set is not drawn, nor its
    /// label.</para></summary>
    public ReferenceLine AxHLine(double y) => Overlays.AddLine(y, Orientation.Horizontal);

    /// <summary>Adds a vertical reference line at the specified X value.
    /// <para>The automatic range of the X axis holds the line, as matplotlib's <c>axvline</c> updates the data limits,
    /// so a line beyond the data is drawn inside the plot; a line beyond a limit you set is not drawn, nor its
    /// label.</para></summary>
    public ReferenceLine AxVLine(double x) => Overlays.AddLine(x, Orientation.Vertical);

    /// <summary>Adds a trendline between two data-coordinate points.</summary>
    public Trendline AddTrendline(double x1, double y1, double x2, double y2) => Overlays.Add(new Trendline(x1, y1, x2, y2));

    /// <summary>Adds a horizontal price level (support/resistance) at the given value.
    /// <para>The automatic range of the Y axis holds the level, so a level beyond the data is drawn inside the plot; a
    /// level beyond a limit you set is not drawn, nor its label.</para></summary>
    public HorizontalLevel AddLevel(double value) => Overlays.Add(new HorizontalLevel(value));

    /// <summary>Adds a Fibonacci retracement overlay between a price high and a price low.
    /// <para>The automatic range of the Y axis holds every level, from the high to the low; a level beyond a limit you
    /// set is not drawn, nor its label.</para></summary>
    public FibonacciRetracement AddFibonacci(double priceHigh, double priceLow) => Overlays.Add(new FibonacciRetracement(priceHigh, priceLow));

    /// <summary>Adds a horizontal shaded span between the specified Y values.</summary>
    public SpanRegion AxHSpan(double yMin, double yMax) => Overlays.AddSpan(yMin, yMax, Orientation.Horizontal);

    /// <summary>Adds a vertical shaded span between the specified X values.</summary>
    public SpanRegion AxVSpan(double xMin, double xMax) => Overlays.AddSpan(xMin, xMax, Orientation.Vertical);

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
        Color? color = null, string? label = null) =>
        Overlays.AddThreshold(value, orientation, breach, color, label);
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
