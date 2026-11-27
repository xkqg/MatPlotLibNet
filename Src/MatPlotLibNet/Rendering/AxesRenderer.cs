// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.ColorBarRendering;
using MatPlotLibNet.Rendering.LegendRendering;
using MatPlotLibNet.Rendering.Lighting;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Rendering.TickFormatters;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Rendering;

/// <summary>Abstract base for coordinate-system-specific axes rendering. Subclass for Cartesian, Polar, 3D or Ternary.</summary>
public abstract class AxesRenderer : Layout.IAxesBands, ILayerHost
{
    /// <summary>The axes model this renderer draws.</summary>
    protected Axes Axes { get; }

    /// <summary>The pixel rectangle that bounds the plot, inside the spines.</summary>
    protected Rect PlotArea { get; }

    /// <summary>The drawing surface the renderer emits its lines, shapes and text onto.</summary>
    protected IRenderContext Ctx { get; }

    /// <summary>The active visual theme that supplies the default colours, fonts and line styles.</summary>
    protected Theme Theme { get; }

    /// <summary>Maximum rendered width of Y-axis tick labels. Set by subclasses during tick rendering; consumed by
    /// <see cref="RenderAxisLabels"/> to place the Y-axis title clear of the widest tick label,
    /// and by the Cartesian renderer to stack the left slots past the primary.</summary>
    protected double MeasuredYTickMaxWidth { get; set; }

    /// <summary>How far the X-axis tick labels, numbers or category names, reach below the mark and the pad as they are
    /// drawn: the tallest label upright, the farthest-reaching one turned.
    /// Set by subclasses during tick rendering; consumed by <see cref="RenderAxisLabels"/> to place the X-axis title clear
    /// of the tick labels.</summary>
    protected double MeasuredXTickMaxHeight { get; set; }

    /// <summary>The band drawn above the top spine below the axes title: the mirrored X tick labels
    /// or the secondary X axis that owns the edge (its tick labels, then
    /// its title). Set by subclasses while they draw it; <see cref="RenderTitle"/> raises the title above it. 0 when
    /// nothing was drawn there.</summary>
    protected double MeasuredTopTickBand { get; set; }

    /// <summary>Per-entry legend item bounds captured during <see cref="RenderLegend"/> or
    /// <see cref="ComputeLegendBounds"/>. Each entry maps a series index to its pixel-space
    /// bounding rectangle. Empty when the legend is hidden or has no labelled entries.</summary>
    internal List<LegendItemBounds> LegendBounds { get; } = [];

    /// <summary>Initializes the renderer with the rendering context.</summary>
    /// <param name="axes">The axes model to render.</param>
    /// <param name="plotArea">The pixel-space rectangle that bounds the plot.</param>
    /// <param name="ctx">The drawing surface to emit primitives onto.</param>
    /// <param name="theme">The active visual theme.</param>
    protected AxesRenderer(Axes axes, Rect plotArea, IRenderContext ctx, Theme theme)
    {
        Axes = axes;
        PlotArea = plotArea;
        Ctx = ctx;
        Theme = theme;
        Emit = SvgEmit.Of(axes, ctx);
    }

    /// <summary>The colour slot of each series of the axes in identity order (<see cref="SeriesSlotExtensions.ColorSlots"/>), made when the
    /// renderer first needs it and handed to everything that numbers a series by colour: the series passes, the part of every Y axis
    /// slot, the top X axis, the end labels and the legend rows. A renderer is made for one use (a draw, a measure, a hit-test) and reads
    /// the axes as they stand when it first asks; a renderer for a changed axes is made again, as every site in the library does.</summary>
    internal int[] ColorSlots => _colorSlots ??= Axes.ColorSlots(Theme);

    private int[]? _colorSlots;

    /// <summary>What this draw writes for the browser (<see cref="Svg.SvgEmit.Of(Models.Axes, IRenderContext)"/>): decided once, when the renderer
    /// is made for the context it draws into.</summary>
    internal SvgEmit Emit { get; }

    /// <summary>Renders the complete axes: background, grid, series, decorations, legend, colorbar, title, labels.</summary>
    public abstract void Render();

    /// <summary>Whether this renderer draws a series' name at the end of its line when the series asks for it
    /// (<see cref="IHasLabelPlacement"/>). Only then does such a series leave the legend; a renderer that draws no
    /// end labels keeps the legend entry, so the name is never lost. <see langword="false"/> here; the Cartesian
    /// renderer, which owns the end-label drawing, overrides it.</summary>
    internal virtual bool DrawsDirectLabels => false;

    /// <summary>Whether this renderer draws the patches of its axes (<see cref="Axes.Patches"/>). Only then does a labelled patch
    /// have a row in the legend, so a legend never names a shape that is not drawn. <see langword="false"/> here, for a renderer of
    /// your own; every renderer the library ships (Cartesian, polar, ternary, 3-D) draws them and overrides it.</summary>
    internal virtual bool DrawsPatches => false;

    /// <summary>The rows of this axes' legend as this renderer draws it: the series rows
    /// (<see cref="SeriesLegendEntry.RowsOf"/>, by <see cref="DrawsDirectLabels"/>) and, where
    /// this renderer draws patches (<see cref="DrawsPatches"/>), a row for each labelled patch, in the order the series and the patches
    /// were added, as matplotlib lists them (<see cref="PatchLegendEntry.Among"/>).
    /// The ONE list the drawing (<see cref="RenderLegend"/>), the hit-test (<see cref="ComputeLegendBounds"/>), the margin reservation
    /// (<c>LegendLayoutCalculator.MeasureLegendBox</c>) and the legend of the figure (<see cref="FigureLegendExtensions"/>) read.</summary>
    /// <param name="legendValues">Whether each series row's label is suffixed with its series' last value; a patch row never is.</param>
    /// <returns>The rows in the order they are listed; empty when nothing has a row.</returns>
    internal IReadOnlyList<LegendEntry> LegendEntries(bool legendValues)
    {
        var series = SeriesLegendEntry.RowsOf(Axes, Theme, DrawsDirectLabels, legendValues, ColorSlots);
        return DrawsPatches ? PatchLegendEntry.Among(Axes, series) : series;
    }

    /// <summary>What is drawn past the four spines besides the titles of the axes and the end labels, measured before
    /// anything is drawn, so the layout can reserve it: the sides (<see cref="MeasureSideBands"/>) and the ends
    /// (<see cref="MeasureEndBands"/>) together.</summary>
    /// <returns>The bands of all four edges, with the insets of the spines at this renderer's plot.</returns>
    internal Layout.EdgeBands MeasureEdgeBands()
    {
        var sides = MeasureSideBands();
        return EndBands(SpineInsetsAt(PlotArea)) with { Left = sides.Left, Right = sides.Right };
    }

    /// <summary>What is drawn past the top and bottom spines as this renderer measures it (<see cref="MeasureEndBands"/>), with what the
    /// axes' tables hang below the plot and left of it (<see cref="SeriesRenderers.TableGrid.WithTablesOf"/>), whatever kind of axes it
    /// is: the one measure of the ends both layout paths, a colour bar's room and an outside legend read.</summary>
    /// <param name="insets">How far each spine stands in from its edge of the plot the bands are read at.</param>
    /// <returns>The bands.</returns>
    internal Layout.EdgeBands EndBands(SpineInsets insets) =>
        SeriesRenderers.TableGrid.WithTablesOf(Axes, MeasureEndBands(insets), PlotArea, Ctx, Theme, Stretch);

    /// <summary>How far each spine stands in from its edge of <paramref name="plot"/> (<see cref="SpineInsets"/>): the bands
    /// measured past the spines hang where they stand. None here: only the Cartesian renderer moves a spine off its edge.</summary>
    /// <param name="plot">The plot the axes is drawn at.</param>
    /// <returns>The insets; 0 on every edge here.</returns>
    internal virtual SpineInsets SpineInsetsAt(Rect plot) => default;

    /// <summary>What is drawn past the left and right spines: the primary Y tick labels and their mirror. The layout reads
    /// it before the plot's width is laid out (these bands do not depend on it). Nothing here: only the Cartesian renderer
    /// measures them.</summary>
    /// <returns>The bands with <see cref="Layout.EdgeBands.Left"/> and <see cref="Layout.EdgeBands.Right"/> set; 0 here.</returns>
    internal virtual Layout.EdgeBands MeasureSideBands() => default;

    /// <summary>What is drawn past the top spine below the title (the mirrored X tick labels or the secondary X axis) and
    /// below the bottom spine (the X tick labels and the X axis title). The layout reads it with the plot at the width it is
    /// drawn at, since the X tick labels turn by that width. Nothing here: the Cartesian renderer measures them, and the polar
    /// renderer the rise of its title above its square.</summary>
    /// <param name="insets">How far each spine stands in from its edge of the plot the bands are read at (<see cref="SpineInsetsAt"/>,
    /// or the plot a layout laid out): what hangs from a spine stands where the spine does, and what is stacked below the plot stands
    /// past it.</param>
    /// <returns>The bands with the top, bottom and bottom-title fields set and <paramref name="insets"/>; 0 here.</returns>
    internal virtual Layout.EdgeBands MeasureEndBands(SpineInsets insets) => default(Layout.EdgeBands) with { Insets = insets };

    /// <summary>What the axes draws past the right spine (<see cref="Layout.RightBand.Past"/>): the secondary Y axes stacked or the
    /// mirrored Y tick labels, then the end labels; a colour bar beside the plot in the library's margin stands past it. Asked while
    /// the axes' own colour bar is drawn, after everything on that side, and before a draw for the figure's bar. Nothing here: the
    /// Cartesian renderer draws those.</summary>
    /// <returns>The band, in pixels; 0 here.</returns>
    internal virtual double PastRightSpine() => 0;

    /// <summary>The end bands as this renderer measures them (<see cref="EndBands"/>), with the insets of the spines at its plot,
    /// for a colour bar's room.</summary>
    Layout.EdgeBands Layout.IAxesBands.Ends() => EndBands(SpineInsetsAt(PlotArea));

    /// <summary>The bands of all four edges as this renderer measures them (<see cref="MeasureEdgeBands"/>), for a colour bar's
    /// room.</summary>
    Layout.EdgeBands Layout.IAxesBands.Edges() => MeasureEdgeBands();

    /// <summary>What this renderer draws past the right spine (<see cref="PastRightSpine"/>), for a colour bar's room.</summary>
    double Layout.IAxesBands.PastRightSpine() => PastRightSpine();

    /// <summary>Draws what belongs to the series at identity slot <paramref name="seriesIndex"/> beyond its own marks, while
    /// that series' group is still open, so it is shown, hidden and highlighted together with the series. Called by
    /// every series pass after the series is drawn, on any axis. Nothing here; the Cartesian renderer draws the series'
    /// end label.</summary>
    /// <param name="seriesIndex">The series' identity slot (<see cref="SeriesSlotExtensions.SlotIndex"/>): its place in
    /// <see cref="Models.Axes.AllSeries"/>, which for a primary series is its index in <see cref="Models.Axes.Series"/>.</param>
    internal virtual void DrawInsideSeriesGroup(int seriesIndex) { }

    /// <summary>The transform the primary series were drawn with in the last <see cref="Render"/>: the plot area and the
    /// range the axes actually show (after autoscale, margins and limits). <see langword="null"/> before a render and
    /// for axes that draw no Cartesian range (polar, 3-D); the Cartesian renderer sets it.</summary>
    internal DataTransform? DrawnTransform { get; private protected set; }

    /// <summary>Where the last <see cref="Render"/> placed a point of the axes' data, when it was not its <see cref="DrawnTransform"/>:
    /// the angle and radius of a polar axes (<see cref="PolarPlacement"/>), made when asked. <see langword="null"/> here, for every
    /// other axes; an inset's zoom indicator then reads the transform (<see cref="Rendering.ChartRenderer.DrawnView.Placement"/>), and a 3-D axes has
    /// neither.</summary>
    internal virtual IDataPlacement? DrawnPlacement => null;

    /// <summary>The transform <see cref="Render"/> draws the primary series with for the axes as they are now, computed
    /// without drawing or measuring anything: the plot area, the range after autoscale, margins and limits, and the
    /// axes' scales. The interaction layout (<see cref="Interaction.ChartLayout"/>) converts the pointer with it, so a
    /// control and the picture share one rule. <see langword="null"/> here, for axes that draw no Cartesian range
    /// (polar, 3-D); the Cartesian renderer overrides it.</summary>
    internal virtual DataTransform? ComputeViewTransform() => null;

    /// <summary>The transform of every Y axis stacked past the primary, computed the same way as <see cref="ComputeViewTransform"/>
    /// — without drawing or measuring anything — so an interaction modifier can convert a pointer position through a
    /// specific axis, not only the primary. Empty here, for a renderer with no Y axes of this kind; the Cartesian renderer
    /// overrides it.</summary>
    internal virtual IReadOnlyList<SecondaryAxisTransform> ComputeSecondaryViewTransforms() => [];

    /// <summary>Writes the axes' view for the server-interaction script, when the figure asked the context for one
    /// (<see cref="SvgEmit.ServerViewIndex"/>, which only a surface that writes markup is asked): the axes' index, its plot
    /// area and the range it drew (<see cref="IMarkupRenderContext.WriteAxesView"/>). Called once the axes is drawn.</summary>
    internal void WriteServerView()
    {
        if (Emit.ServerViewIndex is int index)
            Ctx.WriteAxesView(index, PlotArea, DrawnTransform, ComputeSecondaryViewTransforms());
    }

    /// <summary>Returns the inner data-plot bounds (after axis-label and tick reservations).
    /// Used by <see cref="ChartRenderer"/> to position inset axes within the data area.
    /// The default implementation returns the full <see cref="PlotArea"/>.</summary>
    public virtual Rect ComputeInnerBounds() => PlotArea;

    /// <summary>The size of the whole figure in pixels, for a renderer of your own that needs it; set by the factory when the
    /// figure is known, <see langword="null"/> otherwise. The built-in renderers draw in their plot area alone (a 3-D axes is
    /// given its square by the layout).</summary>
    protected Size? FigureSize { get; set; }

    /// <summary>The colour of the figure's ground (<see cref="Figure.Ground"/>), set when the renderer draws a subplot of a figure;
    /// <see langword="null"/> for a renderer made outside one, which takes the theme's background.</summary>
    internal Color? FigureGround { get; set; }

    private static readonly NamedTable<CoordinateSystem, Func<Axes, Rect, IRenderContext, Theme, AxesRenderer>>
        RendererFactories = BuiltInRendererFactories();

    private static NamedTable<CoordinateSystem, Func<Axes, Rect, IRenderContext, Theme, AxesRenderer>> BuiltInRendererFactories()
    {
        var table = new NamedTable<CoordinateSystem, Func<Axes, Rect, IRenderContext, Theme, AxesRenderer>>();
        // Inset axes draw like any Cartesian axes but have no right margin of their own (CartesianInsetAxesRenderer).
        table.Set(CoordinateSystem.Cartesian, (a, p, c, t) => a.InsetBounds is null
            ? new CartesianAxesRenderer(a, p, c, t)
            : new CartesianInsetAxesRenderer(a, p, c, t));
        table.Set(CoordinateSystem.Polar, (a, p, c, t) => new PolarAxesRenderer(a, p, c, t));
        table.Set(CoordinateSystem.ThreeD, (a, p, c, t) => new ThreeDAxesRenderer(a, p, c, t));
        table.Set(CoordinateSystem.Ternary, (a, p, c, t) => new TernaryAxesRenderer(a, p, c, t));
        return table;
    }

    /// <summary>Registers a custom renderer factory for a coordinate system.</summary>
    /// <param name="system">The coordinate system whose renderer to replace.</param>
    /// <param name="factory">A factory delegate that constructs the custom renderer given axes, plot area, context, and theme.</param>
    public static void RegisterRenderer(CoordinateSystem system, Func<Axes, Rect, IRenderContext, Theme, AxesRenderer> factory)
    {
        RendererFactories.Set(system, factory);
        Interlocked.Increment(ref s_registrations);
    }

    private static long s_registrations;

    /// <summary>How many renderers have been registered in the process: a subplot kept from a draw before a registration is
    /// drawn again, since the renderer that drew it may no longer be the one that draws it.</summary>
    internal static long Registrations => Interlocked.Read(ref s_registrations);

    /// <summary>Creates the appropriate renderer for the axes coordinate system.</summary>
    /// <param name="axes">The axes model to render.</param>
    /// <param name="plotArea">The pixel-space rectangle that bounds the plot.</param>
    /// <param name="ctx">The drawing surface to emit primitives onto.</param>
    /// <param name="theme">The active visual theme.</param>
    /// <param name="figureSize">Optional figure size, handed to the renderer as <see cref="FigureSize"/>.</param>
    /// <returns>An <see cref="AxesRenderer"/> instance for the axes' coordinate system.</returns>
    public static AxesRenderer Create(Axes axes, Rect plotArea, IRenderContext ctx, Theme theme, Size? figureSize = null) =>
        Create(axes, plotArea, ctx, theme, figureSize, null);

    /// <summary>Creates the renderer for the axes' coordinate system as <see cref="Create(Axes, Rect, IRenderContext, Theme, Size?)"/>
    /// does, for a draw that shares the data range of each subplot among its renderers (<paramref name="ranges"/>).</summary>
    /// <param name="axes">The axes model to render.</param>
    /// <param name="plotArea">The pixel-space rectangle that bounds the plot.</param>
    /// <param name="ctx">The drawing surface to emit primitives onto.</param>
    /// <param name="theme">The active visual theme.</param>
    /// <param name="figureSize">The figure size (<see cref="FigureSize"/>); <see langword="null"/> when not known.</param>
    /// <param name="ranges">The data ranges of the draw the renderer belongs to; <see langword="null"/> outside a draw.</param>
    /// <returns>An <see cref="AxesRenderer"/> instance for the axes' coordinate system.</returns>
    internal static AxesRenderer Create(Axes axes, Rect plotArea, IRenderContext ctx, Theme theme, Size? figureSize, FigureRanges? ranges)
    {
        // A coordinate system nobody registered draws as the Cartesian one, through the table's own entry (so an inset
        // axes still gets its inset renderer and a replaced Cartesian renderer is the one used).
        var factory = RendererFactories.Get(axes.CoordinateSystem) ?? RendererFactories.Get(CoordinateSystem.Cartesian)!;
        var renderer = factory(axes, plotArea, ctx, theme);
        if (figureSize.HasValue)
            renderer.FigureSize = figureSize.Value;
        renderer.Ranges = ranges;
        return renderer;
    }

    /// <summary>The data ranges of the draw this renderer belongs to (<see cref="FigureRanges"/>): every renderer a draw makes for
    /// a subplot, to measure it or to draw it, reads the subplot's range there, so the range is worked out once a draw.
    /// <see langword="null"/> for a renderer made outside a draw, which works the range out itself.</summary>
    internal FigureRanges? Ranges { get; set; }

    /// <summary>The stretch given this renderer (<see cref="Stretch"/>); 0 when none was given, a stretch being never 0.</summary>
    private double _stretch;

    /// <summary>How far a layout stretched the axes from where the figure's subplot parameters put it, which what keeps a share of its
    /// axes' height follows (a table's rows, <see cref="Styling.TableLook.CellHeightOver"/>): the one given, else the draw's for the plot
    /// this renderer stands at (<see cref="FigureRanges.StretchOf"/>), 1 outside a draw. A pass of the layout, whose renderer stands at
    /// the height of the axes' cell, gives the stretch of the plot the pass before laid out (<see cref="Layout.AxesEdgeNeeds.EdgeBandsOf"/>),
    /// and an inset is given its parent's: it stands at a share of its parent.</summary>
    internal double Stretch
    {
        get => _stretch > 0 ? _stretch : Ranges?.StretchOf(Axes, PlotArea.Height) ?? 1;
        set => _stretch = value;
    }

    // --- Shared rendering helpers (available to all subclasses) ---

    /// <summary>Fills the plot area with the theme's axes background: the first thing a rectangular plot draws.</summary>
    private protected void FillAxesBackground() => Ctx.DrawRectangle(PlotArea, ShapeStyle.Filled(Theme.AxesBackground));

    /// <summary>The plot area, the context and the theme every pass of this renderer draws into.</summary>
    private protected RenderPass Pass => new(PlotArea, Ctx, Theme) { Stretch = Stretch };

    /// <summary>The patches a pass of this renderer draws among its series: the axes' own where this renderer draws patches
    /// (<see cref="DrawsPatches"/>), none for a renderer of your own.</summary>
    private IReadOnlyList<Models.Patches.Patch>? DrawnPatches => DrawsPatches ? Axes.Patches : null;

    /// <summary>Renders all series on these axes through the visitor pattern, by their own z-order, over the unit square.
    /// </summary>
    protected void RenderSeries() => Draw(SeriesPass.OnUnitSquare(Axes, Pass, DrawnPatches));

    /// <summary>Draws <paramref name="pass"/> with this renderer as its host (<see cref="ILayerHost"/>).</summary>
    private void Draw(in SeriesPass pass)
    {
        AxesRenderer host = this;
        pass.Draw(ref host);
    }

    /// <summary>Renders the data layer of a polar or ternary axes: its grid, its patches and its series, each at the z-order the theme
    /// gives it, as a Cartesian axes orders its data layer (<see cref="SeriesPass.Layered"/>); the grid and the patches are drawn by the
    /// renderer's <see cref="DrawLayerAt"/>.</summary>
    /// <param name="radial">The radial scale the series of a polar axes are drawn on; <see langword="null"/> on any other.</param>
    /// <param name="clip">Where each series' marks are cut off under an explicit limit (<see cref="SeriesPass.Clip"/>);
    /// <see langword="null"/> for none.</param>
    private protected void RenderLayers(RadialScale? radial, ClipRegion? clip) =>
        Draw(SeriesPass.Layered(Axes, Pass, DrawnPatches) with { Radial = radial, Clip = clip });

    /// <summary>Renders all series using a unified 3D projection (for ThreeD axes).</summary>
    /// <param name="projection">The shared projection used by all 3D series renderers.</param>
    /// <param name="lightSource">Optional light source for per-face lighting.</param>
    /// <param name="depthQueue">Optional shared depth queue for cross-series back-to-front compositing.</param>
    protected void RenderSeries(Projection3D projection, ILightSource? lightSource = null,
        DepthQueue3D? depthQueue = null) =>
        Draw(SeriesPass.OnUnitSquare(Axes, Pass, DrawnPatches) with
        {
            Projection = projection,
            Light = lightSource,
            Emit3D = Emit.VertexData3D,
            DepthQueue = depthQueue,
        });

    /// <summary>Draws step <paramref name="at"/> of a pass, a step that is not a series (<see cref="ILayerHost.DrawLayerAt"/>). Nothing
    /// here: the polar renderer draws its grid and its patches, the ternary renderer its grid lines and its patches, the 3-D renderer its
    /// patches and the Cartesian renderer every layer of its data.</summary>
    /// <param name="pass">The pass the steps belong to.</param>
    /// <param name="steps">The steps, in the order they are drawn in.</param>
    /// <param name="at">The step to draw.</param>
    /// <returns>The index of the last step drawn.</returns>
    private protected virtual int DrawLayerAt(in SeriesPass pass, ReadOnlySpan<LayerStep> steps, int at) => at;

    /// <inheritdoc />
    int[] ILayerHost.ColorSlots => ColorSlots;

    /// <inheritdoc />
    void ILayerHost.DrawInsideSeriesGroup(int seriesIndex) => DrawInsideSeriesGroup(seriesIndex);

    /// <inheritdoc />
    int ILayerHost.DrawLayerAt(in SeriesPass pass, ReadOnlySpan<LayerStep> steps, int at) => DrawLayerAt(pass, steps, at);

    /// <summary>Renders all series with a specific DataTransform, the bars stood where this draw puts them first (stacked, then
    /// side by side), by their own z-order, clipped to the plot area.
    /// </summary>
    /// <param name="transform">The coordinate transform mapping data space to pixel space.</param>
    protected void RenderSeries(DataTransform transform)
    {
        Axes.ArrangeBars();
        Draw(SeriesPass.OverPlot(Axes, Pass, transform) with { OrderedBy = Theme.Default });
    }

    /// <summary>The marks this axes has drawn or is about to draw, as a legend at <see cref="LegendPosition.Best"/> must keep
    /// clear of them, worked out without drawing or measuring anything beyond text: each series adds the rectangles, paths
    /// and points it draws (<see cref="SeriesRenderers.SeriesRenderer{T}.AddMarks"/>) at the pixels the draw pass puts
    /// them at. Nothing here: a coordinate system that collects no marks leaves a Best legend in the upper right, as it
    /// always stood.</summary>
    /// <returns>The marks, or <see cref="MarkObstacles.None"/>.</returns>
    internal virtual MarkObstacles LegendObstacles() => MarkObstacles.None;

    /// <summary>Adds the marks of <paramref name="series"/>, as they are drawn through <paramref name="transform"/> (and
    /// <paramref name="projection"/>, for 3-D series), to <paramref name="marks"/>, which it hands back: the series visitor in its
    /// collecting mode, which draws nothing.</summary>
    /// <param name="series">The series, hidden ones too.</param>
    /// <param name="transform">The transform the series are drawn with.</param>
    /// <param name="marks">The collection the marks go into.</param>
    /// <param name="projection">The projection the 3-D series are drawn through; <see langword="null"/> for others.</param>
    /// <param name="radial">The radial scale the polar series are drawn on; <see langword="null"/> for others.</param>
    internal MarkObstacles.Builder CollectMarks(IEnumerable<Models.Series.ISeries> series, DataTransform transform, MarkObstacles.Builder marks,
        Projection3D? projection = null, RadialScale? radial = null)
    {
        var collector = new SvgSeriesRenderer(transform, Ctx, default, Theme, plotArea: PlotArea, projection: projection)
        {
            Marks = marks,
            RadialScale = radial,
        };

        // A series of your own that draws itself in Accept, outside the visitor, draws through the area it is handed: that area must
        // hold a context that draws nothing, or the series would be drawn into the picture a second time. What such a series draws
        // is not collected. Every series of the library, a package's included, is drawn by its renderer, which this visitor asks for
        // its marks instead.
        var area = new RenderArea(PlotArea, new MarkCollectingContext(Ctx, new MarkObstacles.Builder()));
        foreach (var one in series) one.Accept(collector, area);
        return marks;
    }

    /// <summary>The legend as this renderer lays it out: the entries it lists and the box they stand in.</summary>
    private readonly record struct ResolvedLegend(IReadOnlyList<LegendEntry> Entries, LegendLayout Layout);

    /// <summary>The legend of this axes, drawn by the one legend drawer (<see cref="LegendRenderer"/>) on this surface.</summary>
    private LegendRenderer AxesLegend() => new(Axes.Legend, Theme, Ctx);

    /// <summary>The ONE place the legend is laid out. The draw pass (<see cref="RenderLegend"/>) and the hit-test pass
    /// (<see cref="ComputeLegendBounds"/>, which feeds the interactive heads on every paint with no drawing) both read it,
    /// so the box a pointer is tested against is the box that is drawn. The rows are this renderer's
    /// (<see cref="LegendEntries(bool)"/>), and the marks a Best legend keeps clear of are the axes'
    /// (<see cref="LegendObstacles"/>, collected only when asked for); <see cref="LegendRenderer"/> lays them out in the plot area.</summary>
    /// <param name="obstacles">The marks a Best legend keeps clear of, asked for only when the legend stands at Best:
    /// <see cref="LegendObstacles"/> outside a draw, the marks the draw collected inside one.</param>
    /// <returns>The legend's entries and layout; <see langword="null"/> when the legend is hidden or lists nothing.</returns>
    private ResolvedLegend? ResolveLegend(Func<MarkObstacles> obstacles)
    {
        if (!Axes.Legend.Visible) return null;

        var entries = LegendEntries(Axes.Legend.LegendValues);
        // A legend outside the plot stands its gap past what the axes draws on its edge, as the layout kept the room for it.
        var drawnPast = LegendPositionStrategyFactory.Create(Axes.Legend.PositionUnder(Theme)).StandsOutside
            ? new Layout.AxesEdgeNeeds(Layout.LayoutPads.Library, Ranges).DrawnPast(Axes, Ctx, Theme, MeasureEdgeBands(), PlotArea, Ranges?.CellOf(Axes))
            : default;
        return AxesLegend().Place(entries, PlotArea, obstacles, drawnPast) is { } layout ? new ResolvedLegend(entries, layout) : null;
    }

    /// <summary>The legend resolved by <see cref="PlaceLegend"/> for the pass being drawn; read once by <see cref="RenderLegend"/>.</summary>
    private ResolvedLegend? _placedLegend;
    private bool _legendPlaced;

    /// <summary>Resolves the legend now, ahead of the annotations that keep clear of its box, so the box they are laid out
    /// around is the one <see cref="RenderLegend"/> draws a moment later (the legend is still drawn after them, so the
    /// stacking order is unchanged). The place a Best legend takes depends on the marks and on where the annotations were put,
    /// never on where they ended up, so nothing is circular.</summary>
    /// <param name="obstacles">The marks a Best legend keeps clear of, as the draw collected them; asked for only at Best.</param>
    /// <returns>The box the legend will stand in; <see langword="null"/> when there is none.</returns>
    internal Rect? PlaceLegend(Func<MarkObstacles> obstacles)
    {
        _placedLegend = ResolveLegend(obstacles);
        _legendPlaced = true;
        return _placedLegend is { } placed ? new Rect(placed.Layout.BoxX, placed.Layout.BoxY, placed.Layout.BoxWidth, placed.Layout.BoxHeight) : null;
    }

    /// <summary>Renders the legend if any series have labels.</summary>
    protected void RenderLegend()
    {
        var placed = _legendPlaced ? _placedLegend : ResolveLegend(LegendObstacles);
        _legendPlaced = false;
        _placedLegend = null;
        if (placed is not { } resolved) return;

        var bounds = AxesLegend().Draw(resolved.Entries, resolved.Layout, Emit.InteractiveAttributes);
        LegendBounds.Clear();
        LegendBounds.AddRange(bounds);
    }

    /// <summary>Computes legend item bounds without rendering any visuals. Called by
    /// <see cref="ChartRenderer.ComputeLayout"/> to populate <see cref="LayoutResult.LegendItems"/>
    /// for interactive hit-testing.</summary>
    internal void ComputeLegendBounds()
    {
        LegendBounds.Clear();
        if (ResolveLegend(LegendObstacles) is not { } resolved) return;

        LegendBounds.AddRange(LegendRenderer.ItemBounds(resolved.Entries, resolved.Layout));
    }

    /// <summary>Renders a color bar gradient alongside the plot area if configured.
    /// Delegates to <see cref="ColorBarRendering.ColorBarRendererFactory"/> which selects
    /// horizontal colour bar renderer or
    /// vertical colour bar renderer by orientation.</summary>
    protected void RenderColorBar()
    {
        if (Axes.ColorBar is not { Visible: true } cb) return;

        var bar = cb.Resolve(Axes.Series, Theme);

        Ctx.BeginAccessibleGroup("colorbar", "Color bar");

        // A bar in a margin stands past what the axes draws on its side (ColorBarRoom.ClearanceOf), in every layout, which keeps the room
        // for it, and past an inset's own marks too. The room asks this render for only the bands it stands past.
        var room = ColorBarRendering.ColorBarRoom.Of(Ranges, Theme);
        double clearance = room.ClearanceOf(cb, this);
        ColorBarRendering.ColorBarRendererFactory
            .Create(cb, bar.Map, bar.Range.Min, bar.Range.Max, bar.Normalizer, PlotArea, Ctx, Theme, Ranges?.CellOf(Axes), bar.Steps,
                clearance, bar.Lines, room)
            .Render();

        Ctx.EndGroup();
    }

    /// <summary>Renders the axes title above the plot area.</summary>
    protected void RenderTitle()
    {
        if (Axes.Title is null) return;
        var font = Axes.TitleFont(Theme);

        // Horizontal alignment based on TitleLoc; the title stands its lift (the gap and its style's pad, or its descent where the theme
        // lifts it over what is below) above what is drawn above the top spine (the mirrored X tick labels or the secondary X axis)
        double titleY = Axes.TitleBaseline(PlotArea.Y, MeasuredTopTickBand, Axes.TitleLift(Ctx, Theme));
        double titleX = Axes.TitleLoc switch
        {
            TitleLocation.Left  => PlotArea.X,
            TitleLocation.Right => PlotArea.X + PlotArea.Width,
            _                   => PlotArea.Center.X
        };
        var alignment = Axes.TitleLoc switch
        {
            TitleLocation.Left  => TextAlignment.Left,
            TitleLocation.Right => TextAlignment.Right,
            _                   => TextAlignment.Center
        };
        var point = new Point(titleX, titleY);

        Ctx.Draw(TextLabel.Of(Axes.Title), font, new TextPlacement(point, alignment));
    }

    /// <summary>Renders X and Y axis labels, each by its edge's title rule past the tick labels of
    /// the line its ticks hang from: the X title's baseline a clear gap below the
    /// tallest label drawn, numbers or category names alike, the Y title's ink the container's gap past the widest one, each its style's
    /// pad further out; the edge's place without labels when no tick label was drawn (hidden ticks, axis-less series).</summary>
    protected void RenderAxisLabels()
    {
        EdgeTerms.Bottom.DrawTitle(Ctx, Axes, Axes.XAxis, Theme, PlotArea, XTitleLine, MeasuredXTickMaxHeight);
        EdgeTerms.Left.DrawTitle(Ctx, Axes, Axes.YAxis, Theme, PlotArea, YTitleLine, MeasuredYTickMaxWidth);
    }

    /// <summary>The line the X axis' ticks hang from, which its title stands below: the plot's bottom edge here; the Cartesian renderer's
    /// bottom spine wherever it was moved to.</summary>
    private protected virtual double XTitleLine => PlotArea.Y + PlotArea.Height;

    /// <summary>The line the Y axis' ticks hang from, which its title stands beside: the plot's left edge here; the Cartesian renderer's
    /// left spine wherever it was moved to.</summary>
    private protected virtual double YTitleLine => PlotArea.X;

    // --- Font factories ---
    // All themed fonts delegate to the theme font members (ThemeFontExtensions), the single source of truth shared
    // with ConstrainedLayoutEngine + LegendLayoutCalculator.MeasureLegendBox. These thin protected wrappers exist
    // only so existing parameterless call sites continue to compile; add a new font role
    // in ThemeFontExtensions, not here.

    /// <summary>Creates a title font from the theme (axes title — 2 pt larger than labels).</summary>
    protected Font TitleFont() => Theme.TitleFont();

    /// <summary>Creates a tick label font from the theme (same size as axis labels — matches matplotlib).</summary>
    protected Font TickFont() => Theme.TickFont();

    /// <summary>Creates an axis label font from the theme.</summary>
    protected Font LabelFont() => Theme.LabelFont();

    /// <summary>Formats a tick value for display.</summary>
    /// <param name="value">The numeric tick value to format.</param>
    /// <returns>A compact string representation using SI notation for very large or very small values.</returns>
    protected static string FormatTick(double value) => NumericTickFormatter.FormatValue(value);

    /// <summary>Computes aesthetically-spaced tick values using the default nice-number algorithm.</summary>
    /// <param name="min">The minimum value of the visible data range.</param>
    /// <param name="max">The maximum value of the visible data range.</param>
    /// <param name="targetCount">The desired number of tick intervals; the algorithm snaps to a nearby nice number. Defaults to 8, the
    /// most the library's own placement asks for.</param>
    /// <returns>The tick values.</returns>
    protected static double[] ComputeTickValues(double min, double max, int targetCount = 8)
        => new AutoLocator(targetCount).LocateInterval(min, max);

    /// <summary>Computes tick values respecting any <see cref="Axis.TickLocator"/> or
    /// <see cref="TickConfig.Spacing"/>, falling back to the library's own nice-number placement (the one its own themes use).
    /// Scales the target tick count to <paramref name="plotPixels"/> so short plot areas
    /// (sparklines, multi-panel dashboards) don't cram labels. Asks the one rule every 2-D axis asks, under the library's own theme.</summary>
    /// <param name="min">Visible range min.</param>
    /// <param name="max">Visible range max.</param>
    /// <param name="axis">Axis config (locator / spacing / formatter).</param>
    /// <param name="plotPixels">Plot area length in pixels along the tick direction.</param>
    /// <returns>The tick values.</returns>
    protected static double[] ComputeTickValues(double min, double max, Axis axis, double plotPixels) =>
        axis.TickRequestOver(axis.TickLocator, new Range1D(min, max), plotPixels, Theme.Default, isX: false).Majors();
}
