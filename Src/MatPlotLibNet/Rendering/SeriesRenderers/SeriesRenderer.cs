// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Models.Series.XY;
using MatPlotLibNet.Rendering.Downsampling;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Rendering.TextMeasurement;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Abstract base for all series renderers: the rendering context a renderer draws through (its transform, drawing target,
/// plot area and tooltips) and the one rule for a series' colour (<see cref="ColorOf"/>). A renderer is a series' drawing and
/// replaces none of it; what differs between series is a value of the series or of its theme, never a member overridden here.</summary>
internal abstract class SeriesRenderer
{
    protected SeriesRenderContext Context { get; }

    /// <summary>The coordinate transform from data space to pixel space.</summary>
    protected DataTransform Transform => Context.Transform;

    /// <summary>The drawing target. While the renderer hands its marks to a legend by drawing them
    /// (<see cref="SeriesRenderer{T}.AddMarksByDrawing"/>) it is a context that draws nothing.</summary>
    protected IRenderContext Ctx => Collector ?? Context.Ctx;

    /// <summary>The context that collects what is drawn instead of drawing it, while a pass that collects marks runs;
    /// <see langword="null"/> otherwise.</summary>
    private protected IRenderContext? Collector { get; set; }

    /// <summary>The colour of the series' slot, which a series that names no colour (and a part of it that names none) is drawn in:
    /// the colour its verb chose under the library's own themes, the theme's cycle colour at its slot otherwise
    /// (<see cref="SeriesSlot.UnnamedColorOf"/>). A series' own colour is <see cref="ColorOf"/>, which reads this last.</summary>
    protected Color SeriesColor => Context.SeriesColor;

    /// <summary>The plot area bounds.</summary>
    protected RenderArea Area => Context.Area;

    /// <summary>Whether tooltips are enabled.</summary>
    protected bool TooltipsEnabled => Context.TooltipsEnabled;

    /// <summary>Initializes a new renderer with the supplied rendering context.</summary>
    /// <param name="context">The context containing transform, draw target, and theme data for this render pass.</param>
    protected SeriesRenderer(SeriesRenderContext context) => Context = context;

    /// <summary>The colour <paramref name="series"/> is drawn in: the colour it names, else the colour its theme gives a series of its
    /// kind, else the colour its verb chose or its slot's (<see cref="SeriesSlot.ColorOf"/>). The one entry every renderer takes its
    /// series' colour from, and the one the legend swatch asks the slot for, so the picture and the legend cannot disagree. A part of
    /// a series that names none wears <see cref="SeriesColor"/> or the theme's colour at its place (<see cref="PartColor(int)"/>).</summary>
    /// <param name="series">The series being drawn.</param>
    /// <returns>The colour to draw it in.</returns>
    protected Color ColorOf(ISeries series) => Context.Slot.ColorOf(series);

    /// <summary>The colour of the part at <paramref name="index"/> of a series that colours its own parts (a layer of a
    /// stacked area, a wedge of a donut, a stage of a funnel) when it names no colour for it: what the figure's theme
    /// hands out at that slot, asked from the one owner of series colours
    /// (<see cref="SeriesSlotExtensions.SeriesSlot(Theme, int)"/>), so a part wears the theme's palette (and its cycler,
    /// when it has one) exactly as a series does. The slot is the first slot the series holds
    /// (<see cref="SeriesRenderContext.FirstColorSlot"/>) plus the part's own position, so the first part wears the colour
    /// of the series' own slot: the theme's first colour for a series that stands first on its axes.</summary>
    /// <param name="index">The position of the part within its series.</param>
    /// <returns>The theme's colour at that slot.</returns>
    protected Color PartColor(int index) => Context.Theme.SeriesSlot(Context.FirstColorSlot + index).CycleColor;

    /// <summary>The colour of the part at <paramref name="index"/>: the one the series names for it in
    /// <paramref name="explicitColors"/> when that array reaches it, otherwise the theme's (<see cref="PartColor(int)"/>).</summary>
    /// <param name="explicitColors">The colours the series names, one per part; <see langword="null"/> or shorter than the
    /// series when it names none or only the first few.</param>
    /// <param name="index">The position of the part within its series.</param>
    /// <returns>The series' own colour for the part, else the theme's.</returns>
    protected Color PartColor(Color[]? explicitColors, int index) =>
        explicitColors.ElementOrNull(index) ?? PartColor(index);

    /// <summary>The colour of a group of a series whose groups are one colour unless the theme says otherwise (a strip plot, a swarm
    /// plot): the colour the series names, which colours every group; otherwise, under a theme whose groups wear the cycle
    /// (<see cref="CycleLook.GroupsWearTheCycle"/>), the theme's colour at the group's position, from the first colour on, as seaborn
    /// colours the groups of a frame given as columns from its palette whatever the axes drew before; otherwise the series' own.</summary>
    /// <param name="named">The colour the series names, <see langword="null"/> when none.</param>
    /// <param name="group">The position of the group among the series' datasets.</param>
    /// <returns>The colour of the group.</returns>
    protected Color GroupColor(Color? named, int group) =>
        named ?? (Context.Theme.Looks.Cycle.GroupsWearTheCycle ? Context.Theme.SeriesSlot(group).CycleColor : SeriesColor);

    /// <summary>The slot of this series: its colour and the cycler's dash, marker and width. The one owner of how a series
    /// is stroked, which the legend swatch asks as well, so a renderer does not decide a style of its own.</summary>
    protected SeriesSlot Slot => Context.Slot;

    /// <summary>Opens a tooltip wrapper around subsequent drawing calls.</summary>
    protected void BeginTooltip(string text)
    {
        if (TooltipsEnabled)
            Ctx.BeginTooltipGroup(text);
    }

    /// <summary>Closes the tooltip wrapper.</summary>
    protected void EndTooltip()
    {
        if (TooltipsEnabled)
            Ctx.EndTooltipGroup();
    }

    /// <summary>Draws a line through <paramref name="points"/>, broken at every point that cannot be drawn: a missing
    /// value (NaN), an infinity, or a value the transform could not place (a non-positive value on a log axis, a value
    /// inside an axis break). Each unbroken run of two or more points is one polyline; a point alone between two gaps
    /// joins nothing and draws no line, as in matplotlib. The one place a line breaks, so every render context receives
    /// only points it can draw. A stroke of <see cref="LineStyle.None"/> draws no line at all (matplotlib's
    /// <c>linestyle='None'</c>): the markers of a series that names no line are all there is of it. A line of horizontal and
    /// vertical segments is asked of the surface's pixel grid first, as matplotlib's Agg backend snaps a <c>Line2D</c> (a raster export
    /// under the matplotlib themes has one; every other surface leaves the line where it is).</summary>
    /// <param name="points">The points of the line, in drawing order.</param>
    /// <param name="stroke">The stroke every run is drawn with.</param>
    protected void DrawPolyline(IReadOnlyList<Point> points, StrokeStyle stroke)
    {
        if (stroke.Style == LineStyle.None)
        {
            return;
        }

        foreach (var run in Ctx.OnPixelGrid(points, stroke.Thickness).LineRuns())
        {
            Ctx.DrawLines(run, stroke);
        }
    }

    /// <summary>Draws the line of a series that is one stroked line and nothing else (a signal, an ECDF, a polar or a ternary line) through
    /// <paramref name="path"/>: in its colour (<see cref="ColorOf"/>), stroked as its kind is (<c>Slot.StrokeOf(color, series)</c>, <see cref="SeriesSlotExtensions"/>).</summary>
    /// <typeparam name="TS">The kind of series.</typeparam>
    /// <param name="path">The points of the line, in drawing order.</param>
    /// <param name="series">The series being drawn.</param>
    protected void DrawSeriesLine<TS>(IReadOnlyList<Point> path, TS series) where TS : class, ISeries, IGivenStroked =>
        DrawSeriesLine(path, series, ColorOf(series));

    /// <summary>Draws the line of <paramref name="series"/> through <paramref name="path"/> in <paramref name="color"/>, the colour the renderer
    /// already asked for its other parts (a band, a fill), stroked as its kind is (<c>Slot.StrokeOf(color, series)</c>, <see cref="SeriesSlotExtensions"/>):
    /// the one place a series' line meets its stroke.</summary>
    /// <typeparam name="TS">The kind of series.</typeparam>
    /// <param name="path">The points of the line, in drawing order.</param>
    /// <param name="series">The series being drawn.</param>
    /// <param name="color">The colour the series is drawn in.</param>
    protected void DrawSeriesLine<TS>(IReadOnlyList<Point> path, TS series, Color color) where TS : class, ISeries, IGivenStroked =>
        DrawPolyline(path, Slot.StrokeOf(color, series));

    /// <summary>Draws the line of a kind that takes a width from its author and no dash (a sparkline) through <paramref name="path"/> in
    /// <paramref name="color"/>, solid, as wide as its kind draws it (<c>Slot.StrokeOf(color, series)</c>, <see cref="SeriesSlotExtensions"/>).</summary>
    /// <param name="path">The points of the line, in drawing order.</param>
    /// <param name="series">The kind that states its given width and its rule.</param>
    /// <param name="color">The colour the series is drawn in.</param>
    protected void DrawSeriesLine(IReadOnlyList<Point> path, IWidthGiven series, Color color) =>
        DrawPolyline(path, Slot.StrokeOf(color, series));

    /// <summary>Draws the straight line from <paramref name="from"/> to <paramref name="to"/> when both its ends can be
    /// drawn, and nothing otherwise: a segment with an end the axes cannot place is masked, as a line is (one that runs to a value
    /// a log axis clips is cut first, <see cref="DataTransform.SegmentBetween"/>).</summary>
    /// <param name="from">One end, in pixels.</param>
    /// <param name="to">The other end, in pixels.</param>
    /// <param name="stroke">The stroke to draw it with.</param>
    protected void DrawSegment(Point from, Point to, StrokeStyle stroke)
    {
        if (from.CanDrawLineTo(to))
        {
            Ctx.DrawLine(from, to, stroke);
        }
    }

    /// <summary>Adds the line <see cref="DrawPolyline"/> draws through <paramref name="points"/> to <paramref name="marks"/>:
    /// each run of two or more points the axes can place is one path, the points that break it are no part of any.</summary>
    /// <param name="marks">The collection the paths go into.</param>
    /// <param name="points">The points of the line, in drawing order.</param>
    protected static void AddPolyline(MarkObstacles.Builder marks, IReadOnlyList<Point> points)
    {
        foreach (var run in points.LineRuns())
        {
            marks.AddPath(run);
        }
    }

    /// <summary>Adds the segment <see cref="DrawSegment"/> draws from <paramref name="from"/> to <paramref name="to"/> to
    /// <paramref name="marks"/>, when both its ends can be drawn: a segment with an end the axes cannot place is masked,
    /// as a line is.</summary>
    /// <param name="marks">The collection the path goes into.</param>
    /// <param name="from">One end, in pixels.</param>
    /// <param name="to">The other end, in pixels.</param>
    protected static void AddSegment(MarkObstacles.Builder marks, Point from, Point to)
    {
        if (from.CanDrawLineTo(to)) marks.AddPath([from, to]);
    }

    /// <summary>Places a batch of labels through the label layout, which moves the few that would stand on one another or leave
    /// <paramref name="bounds"/> and says where each ended up. The layout measures each label as the surface does
    /// (<see cref="SurfaceFontMetrics"/>), math as math, as <see cref="DrawPlacedLabel"/> draws it. The one place a renderer asks it.</summary>
    /// <param name="candidates">The labels, each with the point it would stand at.</param>
    /// <param name="bounds">The area the labels stay inside.</param>
    /// <returns>Where each label stands, in the order given.</returns>
    protected IReadOnlyList<LabelPlacement> PlaceLabels(IReadOnlyList<LabelCandidate> candidates, Rect bounds) =>
        candidates.Count == 0 ? [] : LabelLayoutEngine.Place(candidates, bounds, new SurfaceFontMetrics(Ctx));

    /// <summary>Draws one placed label: a leader line from where it was meant to stand when the layout moved it, then its text.</summary>
    /// <param name="placement">The label as the layout placed it.</param>
    protected void DrawPlacedLabel(LabelPlacement placement)
    {
        if (placement.LeaderLineStart is { } anchor)
            Ctx.DrawLeaderLine(anchor, placement.FinalPoint, Context.Theme.ForegroundText);
        Ctx.Draw(TextLabel.Of(placement.Text), placement.Font, new TextPlacement(placement.FinalPoint, placement.Alignment) { Vertical = placement.VerticalAlignment });
    }

    /// <summary>Draws every placed label (<see cref="DrawPlacedLabel"/>), in order.</summary>
    /// <param name="placements">The labels as the layout placed them.</param>
    protected void DrawPlacedLabels(IReadOnlyList<LabelPlacement> placements)
    {
        foreach (var placement in placements) DrawPlacedLabel(placement);
    }

    /// <summary>Applies viewport culling and LTTB downsampling to XY data when maxPoints is set.</summary>
    protected XYData ApplyDownsampling(double[] x, double[] y, int? maxPoints)
    {
        if (maxPoints is null || x.Length <= maxPoints.Value) return new(x, y);
        return CulledToView(x, y).AtMost(maxPoints);
    }

    /// <summary>The samples of <paramref name="x"/> and <paramref name="y"/> the view of the x axis keeps (<see cref="ViewportCuller"/>):
    /// the one cull of the view a line's point budget and a scatter both take.</summary>
    /// <param name="x">The x values, in data units.</param>
    /// <param name="y">The y values, one per x.</param>
    /// <returns>The samples kept, re-based to index 0.</returns>
    protected XYData CulledToView(double[] x, double[] y) =>
        // The unscaled range, not DataXMin/DataXMax: the culler compares against raw sample values, and on a log or symlog axis the
        // transform's range is in scaled space.
        ViewportCuller.Cull(x, y, Transform.UnscaledXMin, Transform.UnscaledXMax);

    /// <summary>Slices a monotonic-X series to the visible viewport via O(1)/O(log n) index arithmetic,
    /// then applies LTTB downsampling when the slice exceeds <paramref name="maxPoints"/>.</summary>
    protected XYData ApplyMonotonicDownsampling<T>(T src, int? maxPoints)
        where T : IMonotonicXY =>
        MonotonicViewportSlicer.Slice(src, Transform.UnscaledXMin, Transform.UnscaledXMax, maxPoints);
}

/// <summary>Generic typed series renderer. Each concrete renderer handles exactly one <typeparamref name="T"/> series type.</summary>
/// <typeparam name="T">The series type this renderer handles.</typeparam>
internal abstract class SeriesRenderer<T> : SeriesRenderer where T : ISeries
{
    /// <inheritdoc />
    protected SeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <summary>Renders the given series using the context provided at construction time.</summary>
    public abstract void Render(T series);

    /// <summary>Adds to <paramref name="marks"/> what this series draws, as the rectangles, paths and points those marks
    /// have in pixels: the marks a legend must keep clear of. It does the geometry <see cref="Render"/> does (a renderer
    /// that contributes computes it once, in one place, for both), draws nothing and issues no call to the drawing target
    /// other than measuring. A series that fills its axes (a heat map, a treemap) has no mark to keep clear of and
    /// contributes nothing, which is what this does.</summary>
    /// <param name="series">The series, as it is about to be drawn.</param>
    /// <param name="marks">The collection the marks go into.</param>
    internal virtual void AddMarks(T series, MarkObstacles.Builder marks) { }

    /// <summary>Adds the marks of <paramref name="series"/> by drawing it into a context that draws nothing and collects what
    /// it is given (<see cref="MarkCollectingContext"/>): its marks are exactly the ones it draws. For a renderer whose marks
    /// are few, where working the pixels out twice in two places would cost more than it pays.</summary>
    /// <param name="series">The series, as it is about to be drawn.</param>
    /// <param name="marks">The collection the marks go into.</param>
    protected void AddMarksByDrawing(T series, MarkObstacles.Builder marks)
    {
        Collector = new MarkCollectingContext(Context.Ctx, marks);
        try { Render(series); }
        finally { Collector = null; }
    }
}
