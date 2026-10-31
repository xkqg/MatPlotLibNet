// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Models.Series.XY;
using MatPlotLibNet.Rendering.Downsampling;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Abstract base for all series renderers. Provides virtual access to the rendering context
/// and shared helper methods that subclasses can override to customize behavior.</summary>
internal abstract class SeriesRenderer
{
    protected SeriesRenderContext Context { get; }

    /// <summary>Gets the coordinate transform. Override to apply custom transformations.</summary>
    protected virtual DataTransform Transform => Context.Transform;

    /// <summary>Gets the drawing target. Override to intercept or wrap drawing calls. While the renderer hands its marks to a
    /// legend by drawing them (<see cref="SeriesRenderer{T}.AddMarksByDrawing"/>) it is a context that draws nothing.</summary>
    protected virtual IRenderContext Ctx => Collector ?? Context.Ctx;

    /// <summary>The context that collects what is drawn instead of drawing it, while a pass that collects marks runs;
    /// <see langword="null"/> otherwise.</summary>
    private protected IRenderContext? Collector { get; set; }

    /// <summary>Gets the theme cycle color for this series. Override to change default coloring logic.</summary>
    protected virtual Color SeriesColor => Context.SeriesColor;

    /// <summary>Gets the plot area bounds. Override for custom layout behavior.</summary>
    protected virtual RenderArea Area => Context.Area;

    /// <summary>Gets whether tooltips are enabled. Override to force enable/disable per renderer.</summary>
    protected virtual bool TooltipsEnabled => Context.TooltipsEnabled;

    /// <summary>Initializes a new renderer with the supplied rendering context.</summary>
    /// <param name="context">The context containing transform, draw target, and theme data for this render pass.</param>
    protected SeriesRenderer(SeriesRenderContext context) => Context = context;

    /// <summary>Returns the series-specific color if set, otherwise the theme cycle color. Override to change fallback logic.</summary>
    protected virtual Color ResolveColor(Color? seriesColor) => seriesColor ?? SeriesColor;

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
        explicitColors is not null && index < explicitColors.Length ? explicitColors[index] : PartColor(index);

    /// <summary>The slot of this series: its colour and the cycler's dash, marker and width. The one owner of how a series
    /// is stroked, which the legend swatch asks as well, so a renderer does not decide a style of its own.</summary>
    protected SeriesSlot Slot => Context.Slot;

    /// <summary>Opens a tooltip wrapper around subsequent drawing calls. Override to customize tooltip format.</summary>
    protected virtual void BeginTooltip(string text)
    {
        if (TooltipsEnabled && Ctx is SvgRenderContext svgCtx)
            svgCtx.BeginTooltipGroup(text);
    }

    /// <summary>Closes the tooltip wrapper. Override if BeginTooltip was overridden.</summary>
    protected virtual void EndTooltip()
    {
        if (TooltipsEnabled && Ctx is SvgRenderContext svgCtx)
            svgCtx.EndTooltipGroup();
    }

    /// <summary>Draws a line through <paramref name="points"/>, broken at every point that cannot be drawn: a missing
    /// value (NaN), an infinity, or a value the transform could not place (a non-positive value on a log axis, a value
    /// inside an axis break). Each unbroken run of two or more points is one polyline; a point alone between two gaps
    /// joins nothing and draws no line, as in matplotlib. The one place a line breaks, so every render context receives
    /// only points it can draw. A stroke of <see cref="LineStyle.None"/> draws no line at all (matplotlib's
    /// <c>linestyle='None'</c>): the markers of a series that names no line are all there is of it.</summary>
    /// <param name="points">The points of the line, in drawing order.</param>
    /// <param name="stroke">The stroke every run is drawn with.</param>
    protected void DrawPolyline(IReadOnlyList<Point> points, StrokeStyle stroke)
    {
        if (stroke.Style == LineStyle.None)
        {
            return;
        }

        foreach (var run in points.DrawableRuns())
        {
            if (run.Count >= 2)
            {
                Ctx.DrawLines(run, stroke);
            }
        }
    }

    /// <summary>Draws the straight line from <paramref name="from"/> to <paramref name="to"/> when both its ends can be
    /// drawn, and nothing otherwise: a segment with an end the axes cannot place is masked, as a line is.</summary>
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
        foreach (var run in points.DrawableRuns())
        {
            if (run.Count >= 2) marks.AddPath(run);
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
    /// <paramref name="bounds"/> and says where each ended up. The one place a renderer asks it.</summary>
    /// <param name="candidates">The labels, each with the point it would stand at.</param>
    /// <param name="bounds">The area the labels stay inside.</param>
    /// <returns>Where each label stands, in the order given.</returns>
    protected static IReadOnlyList<LabelPlacement> PlaceLabels(IReadOnlyList<LabelCandidate> candidates, Rect bounds) =>
        candidates.Count == 0 ? [] : LabelLayoutEngine.Place(candidates, bounds, FontMetricsDefault.Current);

    /// <summary>Draws one placed label: a leader line from where it was meant to stand when the layout moved it, then its text.</summary>
    /// <param name="placement">The label as the layout placed it.</param>
    protected void DrawPlacedLabel(LabelPlacement placement)
    {
        if (placement.LeaderLineStart is { } anchor)
            CalloutBoxRenderer.DrawLeaderLine(Ctx, anchor, placement.FinalPoint, Context.Theme.ForegroundText);
        Ctx.DrawText(placement.Text, placement.FinalPoint, placement.Font, placement.Alignment);
    }

    /// <summary>Draws every placed label (<see cref="DrawPlacedLabel"/>), in order.</summary>
    /// <param name="placements">The labels as the layout placed them.</param>
    protected void DrawPlacedLabels(IReadOnlyList<LabelPlacement> placements)
    {
        foreach (var placement in placements) DrawPlacedLabel(placement);
    }

    /// <summary>The projection a 3-D series is drawn through: the 3-D axes' own; for a 3-D series placed on 2-D axes, one
    /// fitted to the extent of its drawable data, which the caller ranges through the missing-value owner. An axis with
    /// nothing drawable spans 0..1; one of no width is centred by <see cref="Projection3D"/> itself.</summary>
    /// <param name="x">The drawable x extent, or <see langword="null"/>.</param>
    /// <param name="y">The drawable y extent, or <see langword="null"/>.</param>
    /// <param name="z">The drawable z extent, or <see langword="null"/>.</param>
    /// <returns>The projection to draw through.</returns>
    protected Projection3D ProjectionFor(Range1D? x, Range1D? y, Range1D? z) =>
        Context.Projection3D ?? new Projection3D(30, -60, Area.PlotBounds,
            x?.Lo ?? 0, x?.Hi ?? 1, y?.Lo ?? 0, y?.Hi ?? 1, z?.Lo ?? 0, z?.Hi ?? 1);

    /// <summary>Converts a normalized alpha (0.0–1.0) to a byte and applies it to the color.</summary>
    protected Color ApplyAlpha(Color color, double alpha) =>
        color.WithAlpha((byte)Math.Round(Math.Clamp(alpha, 0.0, 1.0) * 255));

    /// <summary>The colour with its own opacity scaled by <paramref name="alpha"/>: a colour that is already translucent (the
    /// end of a fade to nothing) stays that much more translucent, where <see cref="ApplyAlpha"/> would set the opacity.</summary>
    protected Color ScaleAlpha(Color color, double alpha) =>
        color.WithAlpha((byte)Math.Round(color.A * Math.Clamp(alpha, 0.0, 1.0)));

    /// <summary>Applies viewport culling and LTTB downsampling to XY data when maxPoints is set.</summary>
    protected XYData ApplyDownsampling(double[] x, double[] y, int? maxPoints)
    {
        if (maxPoints is null || x.Length <= maxPoints.Value) return new(x, y);
        // The unscaled range, not DataXMin/DataXMax: the culler compares against raw sample values, and on a log
        // or symlog axis the transform's range is in scaled space.
        var culled = ViewportCuller.Cull(x, y, Transform.UnscaledXMin, Transform.UnscaledXMax);
        if (culled.X.Length <= maxPoints.Value) return culled;
        return new LttbDownsampler().Downsample(culled.X, culled.Y, maxPoints.Value);
    }

    /// <summary>Slices a monotonic-X series to the visible viewport via O(1)/O(log n) index arithmetic,
    /// then applies LTTB downsampling when the slice exceeds <paramref name="maxPoints"/>.</summary>
    protected XYData ApplyMonotonicDownsampling<T>(T src, int? maxPoints)
        where T : IMonotonicXY =>
        MonotonicViewportSlicer.Slice(src, Transform.UnscaledXMin, Transform.UnscaledXMax, maxPoints);

    /// <summary>Scans <paramref name="data"/> for its value range over the values a colour scale can place (a missing
    /// value, NaN, and an infinity are left out, as for an image or a scatter's colours), then resolves the colour map
    /// and normalizer from the series. With no such value the range is 0..1. The <c>min == max</c> edge case is guarded
    /// with <c>max = min + 1</c> so downstream normalizers never divide by zero.</summary>
    protected static ColormapContext ResolveColormapping(double[,] data, IColormappable cm, INormalizable norm)
    {
        var range = data.DrawableRange() ?? new Range1D(0, 1);
        double min = range.Lo, max = range.Hi;
        if (min == max) max = min + 1;
        return new(cm.GetColorMapOrDefault(ColorMaps.Viridis), norm.NormalizerOrLinear(), min, max);
    }

    /// <inheritdoc cref="ResolveColormapping(double[,],IColormappable,INormalizable)"/>
    protected static ColormapContext ResolveColormapping(int[,] data, IColormappable cm, INormalizable norm)
    {
        double min = double.MaxValue, max = double.MinValue;
        foreach (int v in data)
        {
            if (v < min) min = v;
            if (v > max) max = v;
        }
        if (min == max) max = min + 1;
        return new(cm.GetColorMapOrDefault(ColorMaps.Viridis), norm.NormalizerOrLinear(), min, max);
    }
}

/// <summary>Resolved colour-map configuration used by grid-series renderers.</summary>
internal readonly record struct ColormapContext(IColorMap ColorMap, INormalizer Normalizer, double Min, double Max);

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
