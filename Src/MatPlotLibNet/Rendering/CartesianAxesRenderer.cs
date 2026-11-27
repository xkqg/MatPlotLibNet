// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.CartesianParts;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Rendering.TextMeasurement;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering;

/// <summary>Renders axes using the standard Cartesian (X, Y) coordinate system.</summary>
/// <remarks>Not sealed so that inset axes can answer the end-label question differently
/// (<c>CartesianInsetAxesRenderer</c>); the drawing itself (<see cref="Render"/>) is sealed. The renderer keeps the order things are drawn
/// in; what is drawn is each part's: every axis through its axis part, the data layer through the one loop of a series
/// pass hosted by the data layer, which draws its other layers, the marks a
/// legend keeps clear of, and the opposite edges.</remarks>
public partial class CartesianAxesRenderer : AxesRenderer
{
    /// <summary>Initializes a new Cartesian axes renderer.</summary>
    public CartesianAxesRenderer(Axes axes, Rect plotArea, IRenderContext ctx, Theme theme)
        : base(axes, plotArea, ctx, theme) { }

    /// <inheritdoc />
    /// <remarks>Cartesian axes draw end-of-line labels, so a series that asked for one leaves the legend here.</remarks>
    internal override bool DrawsDirectLabels => true;

    /// <inheritdoc />
    /// <remarks>Cartesian axes draw their patches (<c>CartesianPatchesPart</c>), an inset's included, so a labelled patch has a
    /// row in their legend.</remarks>
    internal override bool DrawsPatches => true;

    /// <summary>The spine a map's outline is drawn as under a theme that frames maps as cartopy does: the theme's own colour and
    /// width (cartopy's <c>spines['geo']</c> reads <c>axes.edgecolor</c> and <c>axes.linewidth</c>).</summary>
    private static readonly SpineConfig MapSpine = new();

    /// <summary>The end labels of the axes being rendered; placed by <see cref="Render"/> before the series pass.</summary>
    private CartesianDirectLabelsPart? _directLabels;

    /// <summary>The lines the primary X and Y axes hang their ticks from in this render (the bottom and the left spine wherever they were
    /// moved to): their titles stand past the tick labels drawn there.</summary>
    private double _xTitleLine, _yTitleLine;

    /// <summary>Where the axes of this render stand (<see cref="CartesianAxisPlacement"/>): the ranges, transforms, ticks and
    /// slot bands the drawing, the measuring and the views read. A value, made on the stack where it is asked for.</summary>
    private CartesianAxisPlacement Placement => new(Axes, PlotArea, Ctx, Theme, Ranges);

    /// <summary>What every part of this render draws with, through <paramref name="transform"/>.</summary>
    private CartesianPartContext PartContext(DataTransform transform) => new(Axes, Pass, ColorSlots, transform);

    /// <inheritdoc />
    /// <remarks>Draws the series' end label, when it asked for one, inside the series' own group, whatever axis the series
    /// is plotted against. A series drawn under the axis' labels (<see cref="UnderTheLabelsFirst"/>: a table below the axes) is drawn
    /// before the end labels are placed, and has none.</remarks>
    internal override void DrawInsideSeriesGroup(int seriesIndex) => _directLabels?.DrawLabelOf(seriesIndex);

    /// <inheritdoc />
    /// <remarks>The bottom spine wherever it was moved to (<see cref="PlacedAxis.EdgeLine"/>), so the title stands below the tick labels
    /// a moved spine took along, as matplotlib places its X label.</remarks>
    private protected override double XTitleLine => _xTitleLine;

    /// <inheritdoc />
    /// <remarks>The left spine wherever it was moved to (<see cref="PlacedAxis.EdgeLine"/>).</remarks>
    private protected override double YTitleLine => _yTitleLine;

    /// <summary>What the mirrored Y tick labels reach past the plot's right edge in this render, hung from the right spine wherever it
    /// stands; 0 when none were drawn. The end labels start past it.</summary>
    private double _mirroredYTickBand;

    /// <summary>Every Y axis slot this render placed, the primary first; <see langword="null"/> before the slots are placed.</summary>
    private List<PlacedAxis>? _placedSlots;

    /// <inheritdoc />
    /// <remarks>The right slots as this render placed them, stacked, or the mirrored Y tick labels, then the end labels by the rule the
    /// layout reserves them with (<see cref="Layout.AxesEdgeNeeds.MeasureRightBand"/>): <see cref="Layout.RightBand.Placed"/>.
    /// Before a render the slots are placed and the mirrored labels measured as the render will place and draw them
    /// (<see cref="MeasureSideBands"/>), for the figure's bar, which stands past the last column.</remarks>
    internal override double PastRightSpine()
    {
        List<PlacedAxis> slots;
        double mirrored;
        if (_placedSlots is { } drawn)
        {
            slots = drawn;
            mirrored = _mirroredYTickBand;
        }
        else
        {
            var placement = Placement;
            var range = placement.ComputeDataRanges();
            slots = placement.PlaceYSlots(placement.PlacePrimary(range, placement.PrimaryView(range)));
            mirrored = SpineInsetsAt(PlotArea).PastRight(MeasureSideBands().Right);
        }

        return Layout.RightBand.Placed(Axes, Theme, slots, mirrored, new Layout.DirectLabelMetrics(Axes, Theme, Ctx).Band()).Past;
    }

    /// <summary>Whether every series fills the plot without Cartesian axes (<see cref="ChartSeries.DrawsWithoutAxes"/>: a
    /// radar, a pie, a treemap, a table in the plot, a stat tile): then no grid, ticks or frame are drawn. A series that stands outside
    /// the plot under the theme (<see cref="ChartSeries.StandsOutsideThePlot"/>: a table below the axes) leaves the plot to its axes, and a
    /// series the library does not know says nothing, so both keep the axes.</summary>
    private bool IsAxisLess
    {
        get
        {
            var all = Axes.Series;
            for (int i = 0; i < all.Count; i++)
                if (all[i] is not ChartSeries { DrawsWithoutAxes: true } series || series.StandsOutsideThePlot(Theme)) return false;
            return all.Count > 0;
        }
    }

    /// <inheritdoc />
    /// <remarks>Measured with what <see cref="Render"/> draws them with: the same edges (<see cref="CartesianEdges"/>), tick sources and
    /// values (the Y ticks for this renderer's plot height), formatter, label font and band rule (<see cref="TickConfig.TickLabelBand"/>).
    /// The left band is the primary Y axis' tick labels as the drawing places them, numbers and category names alike, so the layout
    /// reserves for the primary, left slot 0 of the container, the band it draws; the mirrored right band is the same labels past the
    /// right spine. Nothing is measured for axis-less series.</remarks>
    internal override Layout.EdgeBands MeasureSideBands()
    {
        var yMajor = Axes.YAxis.MajorTicksUnder(Theme);
        if (IsAxisLess || !yMajor.Visible) return default;
        var placement = Placement;
        var range = placement.ComputeDataRanges();
        var primary = placement.PlacePrimary(range, placement.PrimaryView(range));
        double band = yMajor.TickLabelBand(Ctx.MeasureAxisTicks(primary.Run, yMajor, primary.TickDraw(Theme, Axes.Spines)));
        // The left band holds the labels only where the axis labels its own side (TickConfig.LabelsOnOwnSide); the mirrored band always.
        return new Layout.EdgeBands(0, CartesianEdges.Of(Axes, Theme).Y.Majors.DrawsLabels ? band : 0, yMajor.LabelsOnOwnSide ? band : 0, 0);
    }

    /// <inheritdoc />
    /// <remarks>Measured with what <see cref="Render"/> draws them with: the same edges, tick sources and values (the X ticks for this
    /// renderer's plot width), formatter, label font, rotation and band rule (<see cref="TickConfig.TickLabelBand"/>). The X tick labels
    /// turn when the drawing turns them (<see cref="CartesianAxisPart.TurnedToFit"/>: their own rotation, or 30 degrees when upright labels
    /// would collide at this plot's width), so the layout hands this renderer the width the plot is drawn at before it reads these bands.
    /// An upright tick label's height depends on its font only, so upright numbers and dates, and the upright mirrored top band, are
    /// measured on one label; category names and turned labels over the labels the drawing places, turned as drawn
    /// (<see cref="TickDrawContext.LabelExtent"/>). A drawn top X axis owns the top edge, so the top band is then its band
    /// (<see cref="AxesTitleExtensions"/>' <c>TopBand</c>), placed by <see cref="CartesianAxisPlacement.PlaceDrawnTopAxis"/> as the
    /// drawing places it. The bottom band is measured for every X axis that draws tick labels; the X axis title below them is measured by
    /// the rule <see cref="AxesRenderer.RenderAxisLabels"/> places it by (<see cref="AxesTitleExtensions"/>' <c>BottomTitleExtent</c>), at
    /// its fixed offset when no tick label is drawn, axis-less series included. The linked X axes below the plot are measured below all
    /// of that as they are stacked (<see cref="CartesianAxisPlacement.PlaceBottomAxes"/>).</remarks>
    internal override Layout.EdgeBands MeasureEndBands(SpineInsets insets)
    {
        var xMajor = Axes.XAxis.MajorTicksUnder(Theme);
        var placement = Placement;
        bool axisLess = IsAxisLess;
        bool drawsBottomAxes = Axes.YAxes.BottomCount > 0;
        bool drawsXLabels = xMajor.Visible && !axisLess;
        // The range is read once, when anything measured here stands on it: the top axis, the bottom axes, or the X tick labels.
        var range = Axes.DrawnSecondaryXAxis is not null || drawsBottomAxes || drawsXLabels ? placement.ComputeDataRanges() : default;
        double top = placement.PlaceDrawnTopAxis(range) is { } topAxis
            ? topAxis.Axis.TopBand(Axes.TitleOf(topAxis.Axis, Theme), Ctx, Theme, topAxis.OwnSideLabelReach(Ctx, Theme, Axes.Spines))
            : 0;
        double tallestX = 0;
        var overhang = default(Layout.SideOverhang);
        if (drawsXLabels)
        {
            // The X tick labels as the drawing places them: the same ticks, and turned when the drawing turns them, which the plot's
            // width decides. Upright, a number's or a date's height is its font's, so it is measured on one label; category names and
            // turned labels are measured over the labels drawn.
            var xPlaced = placement.PlacePrimaryX(range, placement.PrimaryView(range));
            var xRun = xPlaced.Run;
            var xDraw = CartesianAxisPart.TurnedToFit(Ctx, xPlaced, Theme, Axes.Spines);
            var mirrored = CartesianEdges.Of(Axes, Theme).X.Majors.DrawsLabels ? xPlaced.OppositeDraw(xDraw, PlotArea, Axes.Spines, Theme) : (TickDrawContext?)null;
            // Upright, every number is a label's height high, at the major mark and pad; a label is drawn only where the plot holds its
            // tick. A labelled minor tick stands its label nearer (TickDrawContext.MinorNamed), so such labels are measured as drawn.
            bool uprightSameReach = xDraw.LabelRotation == 0 && xDraw.MinorNamed is null;
            bool uprightNumbers = uprightSameReach && !xPlaced.Source.NamesCategories;
            double xLabelHeight = xRun.HoldsAny() ? Ctx.LineExtentOf(xDraw.LabelFont(xMajor)).Height : 0;
            // No band below the plot for labels the axis does not draw on its own side (TickConfig.LabelsOnOwnSide).
            tallestX = !xMajor.LabelsOnOwnSide ? 0 : uprightNumbers ? xLabelHeight : Ctx.MeasureAxisTicks(xRun, xMajor, xDraw);
            if (mirrored is { } mirror)
                top = xMajor.TickLabelBand(uprightSameReach ? xLabelHeight : Ctx.MeasureAxisTicks(xRun, xMajor, mirror));

            // How far the first and the last label stand out sideways past the plot, at the rotation they are drawn at.
            overhang = xMajor.LabelsOnOwnSide ? CartesianAxisPart.OverhangOf(xRun, xDraw, PlotArea) : default;
            if (mirrored is { } mirror2)
            {
                var past = CartesianAxisPart.OverhangOf(xRun, mirror2, PlotArea);
                overhang = new Layout.SideOverhang(Math.Max(overhang.Left, past.Left), Math.Max(overhang.Right, past.Right));
            }
        }
        // No tick label is drawn for axis-less series: the title alone, at its fixed place.
        var below = Axes.XAxis.BandsBelow(Axes.TitleOf(Axes.XAxis, Theme), Ctx, Theme, tallestX);
        // The linked X axes below the plot stand past what the primary draws past the plot's edge, its spine wherever it stands.
        double pastEdge = insets.PastBottom(below.Extent);
        return new Layout.EdgeBands(top, 0, 0, below.TickLabels, below.Title, overhang,
            drawsBottomAxes ? BottomAxesExtent(placement.PlaceBottomAxes(range, pastEdge), pastEdge) : 0, insets);
    }

    /// <summary>From the plot's bottom edge to the far side of the lowest linked X axis below the plot, as <paramref name="stack"/> stands
    /// them, or what the primary X axis draws past that edge when that is lower.</summary>
    private static double BottomAxesExtent(PlacedAxis[] stack, double primaryPastEdge)
    {
        double lowest = primaryPastEdge;
        foreach (var axis in stack) lowest = Math.Max(lowest, axis.Offset + axis.Band);
        return lowest;
    }

    /// <summary>Says once a draw that a primary axis carries a link (<see cref="Axis.Link"/>), which it cannot draw: a primary axis
    /// has nothing to link to, so the link changes nothing in the picture.</summary>
    private void ReportPrimaryLinks()
    {
        if (Axes.XAxis.Link is not null) Emit("X");
        if (Axes.YAxis.Link is not null) Emit("Y");

        static void Emit(string axis) => Diagnostics.ChartDiagnostics.Emit(new Diagnostics.ChartDiagnostic(nameof(CartesianAxesRenderer),
            $"The primary {axis} axis carries a link, which it does not draw: a link belongs to an axis that shows the primary's range in other units (the top X axis, a Y axis stacked past the primary, a linked X axis below the plot).", null));
    }

    /// <summary>The outline of a map as a polygon: cut by the plot when a zoom has made it reach beyond it (the outline of a map at its
    /// own limits stands inside the plot and needs no cut).</summary>
    private void DrawMapOutline(IReadOnlyList<Point> outline, ShapeStyle style)
    {
        using var clip = ReachesPastThePlot(outline) ? Ctx.ClippedTo(PlotArea) : default;
        Ctx.DrawPolygon(outline, style);
    }

    /// <summary>Whether a map's outline reaches past the plot (a zoom moved it there).</summary>
    private bool ReachesPastThePlot(IReadOnlyList<Point> outline) =>
        outline.Any(point => point.X < PlotArea.X - 1e-6 || point.X > PlotArea.Right + 1e-6
            || point.Y < PlotArea.Y - 1e-6 || point.Y > PlotArea.Bottom + 1e-6);

    /// <summary>The outline inside the plot area the marks of the primary series are cut off at: the outline of a map that stands inside
    /// it, which is the axes' patch cartopy clips every artist of a map to; <see langword="null"/> for the plot area alone (no map, or a map
    /// zoomed past the plot, which is cut off at the plot). A Y axis stacked past the primary and the top X axis cut their marks off at the
    /// plot area, their own patch, as matplotlib's twin axes clip to their own rectangle.</summary>
    private IReadOnlyList<PathSegment>? MarksOutline(IReadOnlyList<Point>? mapOutline)
    {
        if (mapOutline is not { Count: > 2 } outline || ReachesPastThePlot(outline)) return null;
        var path = new PathSegment[outline.Count + 1];
        path[0] = new MoveToSegment(outline[0]);
        for (int i = 1; i < outline.Count; i++) path[i] = new LineToSegment(outline[i]);
        path[^1] = new CloseSegment();
        return path;
    }

    /// <inheritdoc />
    public sealed override void Render()
    {
        ReportPrimaryLinks();

        // Compute data ranges, and the transform the primary series are drawn with
        var placement = Placement;
        var drawnRange = placement.RangeOfAxes();
        var range = drawnRange.Range;
        var transform = placement.PrimaryView(range);
        DrawnTransform = transform;
        var context = PartContext(transform);

        // A map axes under a theme that frames maps as cartopy does: its background is the area inside the map's outline (ax.patch),
        // and the outline is its spine, drawn below over the map. The outline is placed by the transform of the axes, as the map is.
        var mapOutline = Theme.Looks.Map.LikeCartopy && Axes.Map is { } map ? map.OutlineIn(transform) : null;

        // An axes that draws no axes paints no background under a theme that says so (matplotlib's axis('off') draws no axes patch),
        // unless a series on it keeps one (a Sankey diagram's frame).
        if (mapOutline is not null)
            DrawMapOutline(mapOutline, ShapeStyle.Filled(Theme.AxesBackground));
        else if (!(IsAxisLess && Theme.Looks.Frame.NoBackgroundWithoutAxes && Axes.Series.All(s => s is ChartSeries { KeepsAxesBackground: false })))
            FillAxesBackground();

        // The primary axes, placed once for grid + ticks (respects TickLocator / Spacing / plot size; an axis that carries categories ticks
        // them, so the grid runs through the categories and every tick setting reaches their names; ticks inside a break are left out).
        // The primary Y axis is left slot 0 of the container and is drawn by the part that draws every axis.
        var xPlaced = placement.PlacePrimaryX(range, transform);
        var primary = placement.PlacePrimary(range, transform);
        _xTitleLine = xPlaced.EdgeLine;
        _yTitleLine = primary.EdgeLine;

        // Skip standard grid/ticks/frame for axis-less series (radar, pie, treemap, table).
        // These fill the plot area and don't use a Cartesian coordinate system.
        bool radarOnly = IsAxisLess;

        // Grid — an axes that asked for something gets the theme's grid WITH that change applied; one that asked nothing gets
        // the theme's grid whole, so a theme that draws one can still be told "no grid".
        var effectiveGrid = Axes.GridUnder(Theme);
        // The major grid stands on the major ticks alone, and the minor grid on the minor ticks a scale places over them, a labelled
        // minor tick among them (a log axis' named multiple under the matplotlib themes); worked out only for a grid that is drawn.
        bool drawsGrid = !radarOnly && effectiveGrid.Visible;
        var xGridTicks = drawsGrid ? xPlaced.Source.MajorsAmong(xPlaced.Ticks, xPlaced.Run.View, Theme) : xPlaced.Ticks;
        var yGridTicks = drawsGrid ? primary.Source.MajorsAmong(primary.Ticks, primary.Run.View, Theme) : primary.Ticks;

        // The data in the order it is drawn in: the grid, the spans, each patch, the reference lines, the drawing tools, each
        // series and the tick marks, at the z-order the theme gives each (DataLayers; matplotlib's under the matplotlib themes, the grid
        // under everything and the marks with their labels under the library's own). A layer with nothing to draw is not listed, so the
        // draw does not make its part.
        var order = DataLayers(drawsGrid, drawsTicks: !radarOnly);
        List<PlacedAxis> placed;
        try
        {
            var steps = order.Steps;

            // A grid nothing stands under is drawn first, before the tick marks and labels, where it always stood.
            int next = steps.Length > 0 && steps[0].Layer == DataLayer.Grid ? 1 : 0;
            if (next == 1)
                Ctx.RenderGrid(effectiveGrid, Axes, xGridTicks, yGridTicks, transform, Theme);

            // The data layer and the primary axes: the X axis' labels turned to fit when it draws ticks at all.
            var layer = new CartesianDataLayer(this, context, effectiveGrid, xGridTicks, yGridTicks, range, drawnRange.PatchesMayReachPastPlot)
            {
                X = radarOnly ? default : CartesianAxisPart.PrimaryX(context, xPlaced, this),
                Y = CartesianAxisPart.Of(context, primary, this),
                Edges = CartesianEdges.Of(Axes, Theme),
            };

            // What stands outside the plot under the axis is drawn first, as matplotlib draws it before the axis: a table below the axes
            // (at 0) hangs where the X axis draws its labels, which stand over its cells.
            var pass = SeriesPass.OverPlot(Axes, Pass, transform, MarksOutline(mapOutline));
            int under = radarOnly ? 0 : UnderTheLabelsFirst(steps[next..]);
            pass.DrawSteps(steps.Slice(next, under), ref layer);
            next += under;

            // Tick labels, and the marks with them unless the theme places the marks among the data (a step of the layers)
            if (!radarOnly)
                RenderTicks(layer, Theme.TickMarksZ is null ? TickInk.Both : TickInk.Labels);

            // Every Y axis slot of the container is placed ONCE for this render (CartesianAxisPlacement.PlaceYSlots): its range, its
            // transform, its ticks, where its series stand in the identity order, its band and its offset. A slot without series is
            // still placed and charged its band, by the rule the layout reserves the sides by, so the slot past it stands where the
            // layout made room for it; only a slot that is drawn (PlacedAxis.IsDrawn: with series, or linked to the primary) is
            // drawn. The primary, left slot 0, comes first, with the band of the tick labels it drew.
            placed = placement.PlaceYSlots(primary with
            {
                Band = Axes.YAxis.YTitleExtent(Axes.TitleOf(Axes.YAxis, Theme),
                    Axes.YAxes.TitleInputUnder(Theme, AxisSide.Left, Axes.YAxis.MajorTicksUnder(Theme).TickLabelBand(MeasuredYTickMaxWidth))).Band,
            });

            // End-of-line labels: placed once, across every series, before any is drawn, each at the height its own slot puts
            // the end of its line at. Each label is drawn inside its own series' group — by the series pass of its axis, through
            // DrawInsideSeriesGroup — so whatever hides the series hides its label.
            _placedSlots = placed;
            _directLabels = new CartesianDirectLabelsPart(context, placed, _mirroredYTickBand);

            // The rest of the data layer, the bars stacked and grouped first, through the one loop of the primary series' pass.
            Axes.ArrangeBars();
            pass.DrawSteps(steps[next..], ref layer);
        }
        finally
        {
            order.Return();
        }

        // Axis break marks, drawn on top of the series over the break region; the part is made only when there is a break
        if (Axes.XBreaks.Count > 0 || Axes.YBreaks.Count > 0)
            new CartesianBreakMarkersPart(context, FigureGround ?? Theme.Background).Render();

        // Every Y axis slot past the primary that is drawn (it has series, or it is linked), right slots then left slots,
        // nearest the plot first, each at its own offset, through the part that draws every axis (the primary's steps are
        // drawn above and below).
        for (int i = 1; i < placed.Count; i++)
            if (placed[i].IsDrawn) CartesianAxisPart.Of(context, placed[i], this).Render();

        // The top X axis, drawn when it has series or is linked, as PlaceDrawnTopAxis places it, the placement the layout measured its band
        // by. It owns the top edge, so the band it draws there is what the axes title rises by.
        var top = placement.PlaceDrawnTopAxis(range);
        if (top is { } topAxis)
        {
            double reach = CartesianAxisPart.Of(context, topAxis, this).Render();
            MeasuredTopTickBand = topAxis.Axis.TopBand(Axes.TitleOf(topAxis.Axis, Theme), Ctx, Theme, reach);
        }

        // The marks of every series of every seat, hidden ones too, where this render put them: the bars stacked and grouped
        // as drawn, each slot and the top axis through the transform it was drawn with. Collected at most once, when a legend at
        // Best or a text that moves out of the way asks, and shared by both.
        var marks = new CartesianSeriesMarks(this, context);
        MarkObstacles? seriesMarks = null;
        MarkObstacles SeriesMarks() => seriesMarks ??= marks.OfSeries(placed.Skip(1), top);

        // The legend is placed now, ahead of the annotations, so a text that asked to move out of the way keeps clear of the box
        // the legend is drawn in a moment later; the marks it keeps clear of are the series' and that box.
        var legendBox = PlaceLegend(() => marks.WithTextBoxes(SeriesMarks()));
        var obstacles = MarkObstacles.None;
        if (Axes.Annotations.Any(a => a.AvoidOverlap))
            obstacles = legendBox is { } box ? SeriesMarks().Including(box) : SeriesMarks();

        if (Axes.Annotations.Count > 0)
            new CartesianAnnotationsPart(context) { Obstacles = obstacles }.Render();

        if (Axes.Signals.Count > 0)
            new CartesianSignalsPart(context).Render();

        // Axes spines — rendered after series so they appear on top of fills/areas
        if (!radarOnly)
            Ctx.RenderSpines(Axes.Spines, transform, Theme);

        // A map's outline, its spine (cartopy's spines['geo']): the colour and the width of the theme's spines, over the map.
        if (mapOutline is not null)
        {
            var spine = MapSpine.StrokeIn(Theme);
            DrawMapOutline(mapOutline, ShapeStyle.Outlined(spine.Color, spine.Thickness));
        }

        RenderLegend();
        RenderColorBar();
        RenderTitle();
        RenderAxisLabels();

        // The linked X axes below the plot, below everything the primary X axis drew, stacked as the layout reserved them.
        if (Axes.YAxes.BottomCount > 0)
        {
            double pastEdge = SpineInsetsAt(PlotArea).PastBottom(Axes.XAxis.BandsBelow(Axes.TitleOf(Axes.XAxis, Theme), Ctx, Theme, MeasuredXTickMaxHeight).Extent);
            foreach (var bottom in placement.PlaceBottomAxes(range, pastEdge))
                CartesianAxisPart.Of(context, bottom, this).Render();
        }
    }


    /// <summary>The order a Cartesian axes draws its data in (<see cref="LayerOrder"/>): the grid, the spans, each patch, the reference
    /// lines, the drawing tools, each primary series and, under the matplotlib themes, the tick marks, each at the z-order the theme gives it
    /// (<see cref="ZOrderExtensions"/>), sorted once a draw. They are listed in the order they were drawn in before they had a z-order, so a
    /// tie keeps it and nothing at rest moves under the library's own themes. A layer with nothing to draw is not listed.</summary>
    /// <param name="drawsGrid">Whether the grid is drawn.</param>
    /// <param name="drawsTicks">Whether the axes draws tick marks: then, under a theme that places them among the data
    /// (<see cref="ZOrderExtensions"/>' <c>TickMarksZ</c>), they are a step, listed right after the grid they share the axis' z-order with,
    /// as matplotlib draws a tick's grid line before its marks; no series or patch stands at the axis' 1.5 or 2.5 to tie them (a given
    /// z-order is a whole number, a kind's 0, 1, 1.1 or 2), and a list of lines over them is then already in order and is not sorted.</param>
    /// <returns>The order; the caller hands it back.</returns>
    internal LayerOrder DataLayers(bool drawsGrid, bool drawsTicks)
    {
        var axes = Axes;
        var theme = Theme;
        bool tools = axes.Trendlines.Count > 0 || axes.HorizontalLevels.Count > 0 || axes.FibonacciRetracements.Count > 0;
        double? marksZ = drawsTicks ? theme.TickMarksZ : null;
        var order = new LayerOrder((drawsGrid ? 1 : 0) + (axes.Spans.Count > 0 ? 1 : 0) + axes.Patches.Count
            + (axes.ReferenceLines.Count > 0 ? 1 : 0) + (tools ? 1 : 0) + axes.Series.Count + (marksZ is null ? 0 : 1));
        if (drawsGrid) order.Add(theme.GridZ, DataLayer.Grid, 0);
        if (marksZ is { } z) order.Add(z, DataLayer.TickMarks, 0);
        if (axes.Spans.Count > 0) order.Add(theme.SpansZ, DataLayer.Spans, 0);
        for (int i = 0; i < axes.Patches.Count; i++) order.Add(theme.ZOf(axes.Patches[i]), DataLayer.Patch, i);
        if (axes.ReferenceLines.Count > 0) order.Add(theme.OverlayLinesZ, DataLayer.ReferenceLines, 0);
        if (tools) order.Add(theme.OverlayLinesZ, DataLayer.Tools, 0);
        for (int i = 0; i < axes.Series.Count; i++) order.Add(theme.ZOf(axes.Series[i]), DataLayer.Series, i);
        order.Sort();
        return order;
    }


    /// <summary>Moves to the front of <paramref name="steps"/>, in their order, the series that stand outside the plot
    /// (<see cref="ChartSeries.StandsOutsideThePlot"/>: a table below the axes) under the z-order of the axis' labels, where a theme
    /// draws the labels among the data's z-orders (<see cref="ZOrderExtensions"/>' <c>TickMarksZ</c>, matplotlib's axis at 1.5 or 2.5):
    /// matplotlib draws every artist under its axis first, and those are the ones its labels stand over. The rest keep their order.</summary>
    /// <param name="steps">The steps still to draw, in their order.</param>
    /// <returns>How many steps were moved to the front, to be drawn before the labels; 0 under a theme that draws the labels before the
    /// data, and on an axes with no such series.</returns>
    private int UnderTheLabelsFirst(Span<LayerStep> steps)
    {
        if (Theme.TickMarksZ is not { } axisZ) return 0;
        int moved = 0;
        for (int i = 0; i < steps.Length; i++)
        {
            var step = steps[i];
            if (step.Layer != DataLayer.Series || step.Z >= axisZ
                || Axes.Series[step.Index] is not ChartSeries series || !series.StandsOutsideThePlot(Theme)) continue;
            steps[moved..i].CopyTo(steps[(moved + 1)..(i + 1)]);
            steps[moved++] = step;
        }

        return moved;
    }

    /// <inheritdoc />
    /// <remarks>Every series of every seat, hidden ones too (a legend that toggles a series must not move from under the
    /// pointer): the primary's through the view transform, each Y slot's through the transform
    /// <see cref="CartesianAxisPlacement.PlaceSlot"/> gives it and the top axis' through the one
    /// <see cref="CartesianAxisPlacement.PlaceDrawnTopAxis"/> gives it, the three <see cref="Render"/> draws them with, none of it
    /// drawn (<see cref="Rendering.CartesianAxesRenderer.CartesianSeriesMarks"/>).</remarks>
    internal override MarkObstacles LegendObstacles()
    {
        var placement = Placement;
        var range = placement.ComputeDataRanges();

        // The bars stand where they are drawn: stacked, and grouped beside one another.
        Axes.ArrangeBars();

        var seats = Axes.YSlotSeats();
        var slots = new List<PlacedAxis>(seats.Count - 1);
        for (int i = 1; i < seats.Count; i++) slots.Add(placement.PlaceSlot(seats[i].Slot, range));
        var marks = new CartesianSeriesMarks(this, PartContext(placement.PrimaryView(range)));
        return marks.WithTextBoxes(marks.OfSeries(slots, placement.PlaceDrawnTopAxis(range)));
    }

    /// <inheritdoc />
    /// <remarks>The range and the transform <see cref="Render"/> draws the primary series with
    /// (<see cref="CartesianAxisPlacement.PrimaryView"/>), worked out before it draws anything.</remarks>
    internal override DataTransform ComputeViewTransform()
    {
        var placement = Placement;
        return placement.PrimaryView(placement.ComputeDataRanges());
    }

    /// <inheritdoc />
    /// <remarks>Every slot past the primary placed where <see cref="Render"/> places it
    /// (<see cref="CartesianAxisPlacement.PlaceSlot"/>, against the primary X range every slot shares), without drawing or
    /// measuring anything; numbered outward, right slots then left slots.</remarks>
    internal override IReadOnlyList<SecondaryAxisTransform> ComputeSecondaryViewTransforms()
    {
        var placement = Placement;
        var range = placement.ComputeDataRanges();
        var views = new List<SecondaryAxisTransform>();
        foreach (var stack in new[] { Axes.YAxes.Right, Axes.YAxes.Left })
        {
            for (int i = 0; stack.SlotAtOutward(i) is { } slot; i++)
                views.Add(new SecondaryAxisTransform(stack.Side, i, placement.PlaceSlot(slot, range).Transform));
        }
        return views;
    }

    /// <summary>Renders one pass over the tick marks and tick labels of the primary axes, minor ticks included, through the part that draws
    /// every axis (<see cref="CartesianAxisPart.RenderTicks"/>): the X axis' on the bottom edge and what the top edge draws of them, then the
    /// Y axis' on the left edge and what the right edge draws of them (<see cref="CartesianEdges"/>).</summary>
    /// <remarks>One walk for both passes, so the marks and the labels stand on the same ticks in the same order: the library's own
    /// themes draw both in one pass, before the data (<see cref="TickInk.Both"/>, each mark beside its label); a theme that places the
    /// marks among the data (<see cref="ZOrderExtensions"/>' <c>TickMarksZ</c>) draws the labels before the data, measured as they are
    /// drawn (the Y slots and the end labels read their band), and the marks at the axis' step of the data layer. The X labels' turn is
    /// worked out once, when the X part is made, and read by both passes.</remarks>
    /// <param name="layer">The data layer of this render, which holds the parts of the primary axes and their opposite edges.</param>
    /// <param name="ink">What this pass draws.</param>
    internal void RenderTicks(in CartesianDataLayer layer, TickInk ink)
    {
        var x = layer.X.RenderTicks(ink, layer.Edges.X);
        var y = layer.Y.RenderTicks(ink, layer.Edges.Y);
        if (!ink.DrawsLabels) return;

        // What the labels reach is where the X axis title stands clear of them (RenderAxisLabels), what the mirrored X labels reach past
        // the plot's top is what the axes title rises by, and the end labels start past what the mirrored Y labels reach past its right:
        // the mirrored labels hang from the top and the right spine wherever they stand.
        var insets = SpineInsetsAt(PlotArea);
        MeasuredXTickMaxHeight = x.OwnReach;
        MeasuredTopTickBand = insets.PastTop(x.OppositeBand);
        MeasuredYTickMaxWidth = y.OwnReach;
        _mirroredYTickBand = insets.PastRight(y.OppositeBand);
    }

    /// <inheritdoc />
    /// <remarks>Read off the spines where the drawing stands them (<see cref="EdgeTerms.InsetOf"/>), through the primary's transform at
    /// <paramref name="plot"/> for a spine at a data value. The top spine's inset is 0 where a top X axis owns the edge: the top band is
    /// then that axis', which stands at the plot's edge. Nothing is worked out while every spine stands on its edge.</remarks>
    internal override SpineInsets SpineInsetsAt(Rect plot)
    {
        var spines = Axes.Spines;
        if (!spines.AnyOffItsEdge) return default;
        var placement = new CartesianAxisPlacement(Axes, plot, Ctx, Theme, Ranges);
        var transform = placement.PrimaryView(placement.ComputeDataRanges());
        return new SpineInsets(
            EdgeTerms.Left.InsetOf(spines, plot, transform),
            EdgeTerms.Right.InsetOf(spines, plot, transform),
            Axes.DrawnSecondaryXAxis is null ? EdgeTerms.Top.InsetOf(spines, plot, transform) : 0,
            EdgeTerms.Bottom.InsetOf(spines, plot, transform));
    }

    /// <summary>The marks a legend at <see cref="Models.LegendPosition.Best"/> and a text that moves out of the way keep clear of on a Cartesian
    /// axes: the marks of every series of every seat, hidden ones too (a legend that toggles a series must not move from under the pointer), where
    /// a render puts them, then the annotations' texts. The renderer asks for them while it draws and when it places its legend outside a
    /// draw, through the same placed axes, so both keep clear of the same marks.</summary>
    /// <param name="host">The renderer whose series visitor collects the marks (<see cref="AxesRenderer.CollectMarks"/>).</param>
    /// <param name="context">What the render draws with; its transform is the one the primary series and the annotations are placed by.</param>
    private readonly struct CartesianSeriesMarks(AxesRenderer host, CartesianPartContext context)
    {
        /// <summary>The marks of every series: the primary's through the primary transform, each Y axis past the primary through the transform
        /// it is placed with, in identity order, then the top axis' through its own. The bars must stand where they are drawn
        /// (<see cref="Models.Series.BarArrangementExtensions"/>' <c>ArrangeBars</c>) before this is asked.</summary>
        /// <param name="slots">Every Y axis past the primary, placed, in identity order.</param>
        /// <param name="top">The top axis placed when it is drawn; <see langword="null"/> otherwise.</param>
        /// <returns>The marks.</returns>
        internal MarkObstacles OfSeries(IEnumerable<PlacedAxis> slots, PlacedAxis? top)
        {
            var marks = new MarkObstacles.Builder();
            host.CollectMarks(context.Axes.Series, context.Transform, marks);
            foreach (var slot in slots)
                host.CollectMarks(slot.Series, slot.Transform, marks);
            if (top is { } topAxis)
                host.CollectMarks(topAxis.Series, topAxis.Transform, marks);
            return marks.Build();
        }

        /// <summary><paramref name="seriesMarks"/>, then the annotations' texts at the places they are put at (a text that moves out of the way
        /// is laid out around the legend afterwards, so where it ends up does not decide where the legend stands).</summary>
        /// <param name="seriesMarks">The marks of every series (<see cref="Rendering.CartesianAxesRenderer.CartesianSeriesMarks.OfSeries"/>).</param>
        /// <returns>The series' marks with the annotations' text boxes after them; <paramref name="seriesMarks"/> is left as it was.</returns>
        internal MarkObstacles WithTextBoxes(MarkObstacles seriesMarks)
        {
            var marks = new MarkObstacles.Builder(seriesMarks);
            if (context.Axes.Annotations.Count > 0)
                new CartesianAnnotationsPart(context).AddTextBoxes(marks);
            return marks.Build();
        }
    }
}
