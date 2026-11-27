// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Buffers;
using System.Runtime.InteropServices;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Rendering;

/// <summary>Transforms data coordinates to pixel coordinates and vice versa within a plot area.
/// Optionally aware of axis breaks (data values inside break regions return NaN) and axis scales
/// (Log / SymLog map data through the scale's Forward function before pixel scaling). A break is cut out of the axis in the
/// space the axis is drawn in, by the one rule of the axis' breaks, so a log axis stays logarithmic on either side
/// of it.</summary>
public sealed partial class DataTransform : ITickPlacement
{
    // The two directions are one rule (PixelDirection): X spread left to right, Y spread upward by a negative rate. Each holds
    // where its range starts (DataXMin, DataYMin), the value every other one is measured from. Everything kept per direction is one
    // pair, which a direction picks its member of (AlongExtensions), so the transform never picks one by hand.
    private readonly AxisPair<PixelDirection> _dir;

    /// <summary>Where each direction's range ends in the drawn space (<see cref="DataXMax"/>, <see cref="DataYMax"/>).</summary>
    private readonly AxisPair<double> _dataMax;

    /// <summary>The range each direction shows in the data's own unit, as it was handed over (<see cref="ShownAlong"/>).</summary>
    private readonly AxisPair<Range1D> _shown;

    private readonly Rect _plotBounds;

    /// <summary>Gets the minimum X value in data space (compressed if breaks active).</summary>
    public double DataXMin => _dir.X.Anchor;

    /// <summary>Gets the maximum X value in data space (compressed if breaks active).</summary>
    public double DataXMax => _dataMax.X;

    /// <summary>Gets the minimum Y value in data space (compressed if breaks active).</summary>
    public double DataYMin => _dir.Y.Anchor;

    /// <summary>Gets the maximum Y value in data space (compressed if breaks active).</summary>
    public double DataYMax => _dataMax.Y;

    /// <summary>The lowest X value the axis shows, in the space the DATA lives in.</summary>
    /// <remarks>
    /// On a log or symlog axis <see cref="DataXMin"/> is the SCALED bound — 0 for data that starts at 1, because
    /// the pixel mapping works in log space. Anything that compares the axis range against raw sample values, such
    /// as viewport culling, must use this instead, or it cuts away every point above 10^DataXMax while the axis
    /// goes on drawing ticks for them. On a linear axis the two are the same number. On a reversed axis (an X limit set
    /// high to low) it is still the lowest value: the low end of the drawn range's <see cref="Range1D.Interval"/>.
    /// </remarks>
    internal double UnscaledXMin => _shown.X.Interval.Lo;

    /// <summary>The highest X value the axis shows, in the space the DATA lives in.</summary>
    /// <inheritdoc cref="UnscaledXMin"/>
    internal double UnscaledXMax => _shown.X.Interval.Hi;

    /// <summary>The ends the X axis (<paramref name="alongX"/>) or the Y axis shows, in the data's own unit, as the range was handed
    /// over (high to low on a reversed axis): the view before the scale and the breaks.</summary>
    /// <param name="alongX">The X axis when <see langword="true"/>, the Y axis otherwise.</param>
    /// <returns>The shown range.</returns>
    internal Range1D ShownAlong(bool alongX) => _shown.Along(alongX);

    /// <summary>The pixel-space rectangle the data range is mapped onto.</summary>
    internal Rect PlotBounds => _plotBounds;

    /// <summary>As a tick placement a transform stands a tick at its own value, as every axis that is not linked does: shown
    /// when, through the axis' scale, it lies on the range the axis shows through that scale (before the breaks: a value inside
    /// one has no pixel), at the pixel <see cref="DataToPixel"/> gives it along the edge. The value goes through the scale
    /// once, and the coordinate across the edge is not worked out.</summary>
    double? ITickPlacement.OnPlot(double value, bool alongX) => _dir.Along(alongX).OnPlot(value);

    /// <summary>As a tick placement a transform draws the ticks on the scale it maps that coordinate through.</summary>
    ScaleRule ITickPlacement.ScaleAlong(bool alongX) => RuleAlong(alongX);

    /// <summary>The rule of the scale this transform maps a coordinate through along X (<see cref="XRule"/>) or along Y
    /// (<see cref="YRule"/>).</summary>
    /// <param name="alongX">Whether the coordinate is an X; a Y otherwise.</param>
    /// <returns>The rule, bound to its threshold.</returns>
    internal ScaleRule RuleAlong(bool alongX) => _dir.Along(alongX).Rule;

    /// <summary>As a tick placement a transform shows the range it was handed in the data's own unit (<see cref="ShownAlong"/>).</summary>
    Range1D? ITickPlacement.ViewAlong(bool alongX) => ShownAlong(alongX);

    /// <summary>The pixel along one edge where <paramref name="value"/> stands, as <see cref="DataToPixel"/> places it, without
    /// asking whether the plot shows it: where a linked axis stands the tick whose value maps back to it.</summary>
    /// <param name="value">The value, in this transform's data units.</param>
    /// <param name="alongX">Whether the value is an X; a Y otherwise.</param>
    /// <returns>The pixel coordinate along that edge; NaN where the axis has no place for the value.</returns>
    internal double PixelAlong(double value, bool alongX) => _dir.Along(alongX).Place(value);

    /// <summary>The range the axes show, in data units, in drawn orientation: X from the left edge to the right edge, Y
    /// from the bottom edge to the top edge. On a log or symlog axis these are the data values at the edges, not their
    /// scaled bounds (<see cref="DataXMin"/>); a reversed limit stays reversed.</summary>
    internal DataRange DrawnRange => new(_shown.X.Lo, _shown.X.Hi, _shown.Y.Lo, _shown.Y.Hi);

    /// <summary>Initializes a new linear transform mapping the specified data range onto the given pixel bounds.</summary>
    /// <param name="dataXMin">Minimum X value in data space.</param>
    /// <param name="dataXMax">Maximum X value in data space.</param>
    /// <param name="dataYMin">Minimum Y value in data space.</param>
    /// <param name="dataYMax">Maximum Y value in data space.</param>
    /// <param name="plotBounds">The pixel-space rectangle to map data onto.</param>
    public DataTransform(double dataXMin, double dataXMax, double dataYMin, double dataYMax, Rect plotBounds)
        : this(dataXMin, dataXMax, dataYMin, dataYMax, plotBounds, null, null,
               dataXMin, dataXMax, dataYMin, dataYMax,
               AxisScale.Linear, AxisScale.Linear, 1.0, 1.0) { }

    /// <summary>Initializes a break-aware linear transform.</summary>
    public DataTransform(
        double dataXMin, double dataXMax, double dataYMin, double dataYMax, Rect plotBounds,
        IReadOnlyList<AxisBreak>? xBreaks, IReadOnlyList<AxisBreak>? yBreaks,
        double fullXMin, double fullXMax, double fullYMin, double fullYMax)
        : this(dataXMin, dataXMax, dataYMin, dataYMax, plotBounds, xBreaks, yBreaks,
               fullXMin, fullXMax, fullYMin, fullYMax,
               AxisScale.Linear, AxisScale.Linear, 1.0, 1.0) { }

    /// <summary>Initializes a break-aware AND scale-aware transform. For Log/SymLog scales the
    /// passed <paramref name="dataYMin"/>..<paramref name="dataYMax"/> must already be in scaled
    /// space (i.e. <c>SymlogTransform.Forward(rawMin, linthresh)</c>) with the scaled width of every break taken out; raw
    /// values fed to <see cref="DataToPixel"/> are mapped through the scale's Forward function and then moved down by the
    /// scaled width of every break below them.</summary>
    public DataTransform(
        double dataXMin, double dataXMax, double dataYMin, double dataYMax, Rect plotBounds,
        IReadOnlyList<AxisBreak>? xBreaks, IReadOnlyList<AxisBreak>? yBreaks,
        double fullXMin, double fullXMax, double fullYMin, double fullYMax,
        AxisScale xAxisScale, AxisScale yAxisScale, double xLinThresh, double yLinThresh)
        : this(dataXMin, dataXMax, dataYMin, dataYMax, plotBounds, xBreaks, yBreaks, fullXMin, fullXMax, fullYMin, fullYMax,
               xAxisScale.RuleAt(xLinThresh), yAxisScale.RuleAt(yLinThresh)) { }

    /// <summary>The scale-aware constructor over the rules of the two scales, each bound to its threshold.</summary>
    private DataTransform(
        double dataXMin, double dataXMax, double dataYMin, double dataYMax, Rect plotBounds,
        IReadOnlyList<AxisBreak>? xBreaks, IReadOnlyList<AxisBreak>? yBreaks,
        double fullXMin, double fullXMax, double fullYMin, double fullYMax, ScaleRule xRule, ScaleRule yRule)
        : this(plotBounds, new AxisPair<DirectionSpan>(
               Spanned(new(dataXMin, dataXMax), new(fullXMin, fullXMax), xRule, xBreaks),
               Spanned(new(dataYMin, dataYMax), new(fullYMin, fullYMax), yRule, yBreaks))) { }

    /// <summary>A direction over the ends <paramref name="data"/> spreads in the drawn space and <paramref name="shown"/> shows in the
    /// data's own unit, its break map made from <paramref name="breaks"/> when it has any.</summary>
    private static DirectionSpan Spanned(Range1D data, Range1D shown, ScaleRule rule, IReadOnlyList<AxisBreak>? breaks) =>
        new(data, shown, rule, breaks is { Count: > 0 } ? new AxisBreakMap(breaks, shown.Lo, shown.Hi, rule) : null);

    /// <summary>The transform that maps <paramref name="range"/> onto <paramref name="plotBounds"/>, each direction by its
    /// <see cref="AxisMapping"/>: the ONE rule the Cartesian renderer builds every transform by (the primary's, every Y slot's
    /// and the top axis'). A direction with breaks is drawn over its <see cref="AxisBreakMap.CompressedRange"/>, one on a log or
    /// symlog axis over its range in the scaled space (<see cref="ScaleRule.ForwardRange"/>), so that ticks at 100,
    /// 1000 and 10000 land evenly spaced; with neither in either direction, the plain linear transform. The break map of a
    /// direction is made once, here, and both the range and every placed value read it.</summary>
    /// <param name="range">The range mapped onto the plot, in data units.</param>
    /// <param name="plotBounds">The pixel-space rectangle to map data onto.</param>
    /// <param name="x">How the X direction maps its values.</param>
    /// <param name="y">How the Y direction maps its values.</param>
    /// <returns>The transform.</returns>
    internal static DataTransform Of(DataRange range, Rect plotBounds, AxisMapping x, AxisMapping y)
    {
        if (!x.IsShaped && !y.IsShaped)
            return new DataTransform(range.XMin, range.XMax, range.YMin, range.YMax, plotBounds);

        return new DataTransform(plotBounds, new AxisPair<AxisMapping>(x, y).Select(range, static (mapping, alongX, r) => Mapped(mapping, r.Along(alongX))));

        // A direction with breaks is drawn over its compressed range, one on a shaping scale over its range in the scaled space.
        static DirectionSpan Mapped(AxisMapping mapping, Range1D shown)
        {
            var map = mapping.MapOver(shown.Lo, shown.Hi);
            var (min, max) = map?.CompressedRange ?? mapping.Rule.ForwardRange(shown.Lo, shown.Hi);
            return new DirectionSpan(new Range1D(min, max), shown, mapping.Rule, map);
        }
    }

    /// <summary>What one direction of a transform is made from: the ends it spreads over the plot in the drawn space, the ends it shows
    /// in the data's own unit, the rule of its scale and its breaks, already mapped.</summary>
    /// <param name="Data">The ends in the drawn space (scaled on a log axis, compressed by the breaks).</param>
    /// <param name="Shown">The ends shown, in the data's own unit, as handed over.</param>
    /// <param name="Rule">The rule of the scale, bound to its threshold.</param>
    /// <param name="Breaks">The breaks cut out of the drawn space; <see langword="null"/> when the axis has none.</param>
    private readonly record struct DirectionSpan(Range1D Data, Range1D Shown, ScaleRule Rule, AxisBreakMap? Breaks);

    /// <summary>The one constructor every other one ends in: each direction already spanned.</summary>
    private DataTransform(Rect plotBounds, AxisPair<DirectionSpan> spans)
    {
        _plotBounds = plotBounds;
        _dataMax = spans.Select(0, static (span, _, _) => span.Data.Hi);
        _shown = spans.Select(0, static (span, _, _) => span.Shown);
        _dir = spans.Select(plotBounds, static (span, alongX, plot) => Spread(span, alongX, plot));
    }

    /// <summary>One direction spread over the plot: X runs right from the plot's left edge at its range's start; Y runs up from its
    /// bottom edge, so its rate per unit is negative (<see cref="AlongExtensions"/>' <c>ValueGrowthAlong</c>). A range of no width puts
    /// every value at the middle of its edge.</summary>
    private static PixelDirection Spread(DirectionSpan span, bool alongX, Rect plot)
    {
        double range = span.Data.Hi - span.Data.Lo;
        double scale = range == 0 ? 0 : plot.LengthAlong(alongX) / range;
        return new PixelDirection(
            scale == 0 ? 0 : Rect.ValueGrowthAlong(alongX) * scale, span.Data.Lo,
            scale == 0 ? plot.CentreAlong(alongX) : plot.ValueStartAlong(alongX),
            span.Breaks, span.Rule,
            new Range1D(span.Rule.Forward(span.Shown.Lo), span.Rule.Forward(span.Shown.Hi)));
    }

    /// <summary>The edge where the axis of a direction begins, at its lowest value: where values laid along the plot start (its left
    /// edge along X, its bottom edge along Y), or where they end on a reversed axis, whose rate per pixel runs against the way a value
    /// grows on the screen (<see cref="AlongExtensions"/>' <c>ValueGrowthAlong</c>); with the direction's rate per unit, so a path cut
    /// there knows which way the axis runs.</summary>
    private static StartEdge StartOf(in PixelDirection dir, bool alongX, Rect plot) =>
        new(dir.PerUnit * Rect.ValueGrowthAlong(alongX) < 0 ? plot.ValueEndAlong(alongX) : plot.ValueStartAlong(alongX), dir.PerUnit, alongX);

    /// <summary>The edge where the axis along X (<paramref name="alongX"/>) or along Y begins (<see cref="StartOf"/>): where a base the
    /// scale cannot place is pinned and where a path that reaches beyond the axis is cut. Worked out when a cut or a pin asks for it, not
    /// held: a transform is made for every axes on every draw, and almost none is ever cut there.</summary>
    private StartEdge StartAlong(bool alongX) => StartOf(_dir.Along(alongX), alongX, _plotBounds);

    /// <summary>Converts data-space coordinates to pixel-space coordinates.</summary>
    /// <param name="x">The X value in data space.</param>
    /// <param name="y">The Y value in data space.</param>
    /// <returns>The corresponding pixel position. X or Y may be NaN if the input falls inside a break region.</returns>
    // The axis scale (Log / SymLog), then the breaks cut out in that scaled space: a value in a break is NaN
    public Point DataToPixel(double x, double y) => new(_dir.X.Place(x), _dir.Y.Place(y));

    /// <summary>Converts a point one of whose coordinates is the BASE a mark stands on to pixels — the zero baseline of an area or a
    /// density fill and the start of a step histogram's outline (a base along Y), the foot of a box's whisker on either axis, the first
    /// corner of a grid (each coordinate in turn): as <see cref="DataToPixel"/>, except that a finite base the log axis it lies on cannot
    /// place (0 or below) is pinned to the edge where that axis begins: the plot's lower edge along Y (its upper edge on a reversed axis),
    /// its left edge along X (its right edge on a reversed axis). matplotlib's log scale puts such a value a thousand decades below the
    /// axis (<c>nonpositive='clip'</c>) and the plot cuts the mark at that edge; for a base the mark runs straight across the axis to,
    /// that cut is this very pixel (<see cref="CutBeyondTheStart"/> in closed form). Each coordinate is placed once.</summary>
    /// <remarks>A missing value, an infinity, a value inside an axis break and a value a logit axis masks are not pinned: they
    /// stay NaN.</remarks>
    /// <param name="alongX">Whether the base is the X value; the Y value otherwise.</param>
    /// <param name="x">The X value in data space.</param>
    /// <param name="y">The Y value in data space.</param>
    /// <returns>The pixel position, with the base pinned.</returns>
    internal Point DataToPixelClippedAlong(bool alongX, double x, double y)
    {
        var data = new Point(x, y);
        return Point.Along(alongX,
            _dir.Along(alongX).PlaceClipped(data.CoordinateAlong(alongX), StartAlong(alongX).Edge),
            _dir.Along(!alongX).Place(data.CoordinateAcross(alongX)));
    }

    /// <summary><see cref="DataToPixelClippedAlong"/> for a mark whose base is its Y (an area's zero baseline, a histogram's floor), for
    /// a caller whose direction is fixed.</summary>
    /// <param name="x">The X value in data space.</param>
    /// <param name="y">The base, in data space.</param>
    /// <returns>The pixel position, with the base pinned.</returns>
    internal Point DataToPixelClippedY(double x, double y) => DataToPixelClippedAlong(false, x, y);

    /// <summary><see cref="DataToPixelClippedAlong"/> for a mark whose base is its X, for a caller whose direction is fixed.</summary>
    /// <param name="x">The base, in data space.</param>
    /// <param name="y">The Y value in data space.</param>
    /// <returns>The pixel position, with the base pinned.</returns>
    internal Point DataToPixelClippedX(double x, double y) => DataToPixelClippedAlong(true, x, y);

    /// <summary>Converts a point to pixels as matplotlib's <c>transData</c> places it, before the plot cuts anything: as
    /// <see cref="DataToPixel"/>, except that a finite value at or below zero on a log axis stands a thousand decades beyond that
    /// axis (its <c>nonpositive='clip'</c>; <see cref="LiesBeyond"/> knows the pixel). A mark that runs through such a point is cut
    /// where it leaves the plot (<see cref="CutBeyondTheStart"/>, <see cref="CutSegment"/>); a mark that is not cut by the plot
    /// (an inset's zoom indicator) is drawn from it as it is.</summary>
    /// <param name="x">The X value in data space.</param>
    /// <param name="y">The Y value in data space.</param>
    /// <returns>The pixel position; NaN in a coordinate the axis masks (a logit axis outside 0 to 1, a value inside a break).</returns>
    internal Point DataToPixelUnclipped(double x, double y) => new(_dir.X.PlaceUnclipped(x), _dir.Y.PlaceUnclipped(y));

    /// <summary>Batch-transforms the values of the vertices of a path (a line, the outline of a fill) on the X axis along X, on the Y
    /// axis otherwise, to pixels as <see cref="DataToPixelUnclipped"/> places them: a value the log scale cannot place stands a thousand
    /// decades beyond where the axis begins, for <see cref="CutBeyondTheStart"/> to cut the path there. On any other scale the result is
    /// <see cref="TransformX"/>'s (<see cref="TransformY"/>'s), bit for bit.</summary>
    /// <param name="alongX">Whether the values lie on the X axis.</param>
    /// <param name="values">Data-space values.</param>
    /// <returns>Pixel-space values, one per input element.</returns>
    internal double[] TransformUnclippedAlong(bool alongX, ReadOnlySpan<double> values) =>
        _dir.Along(alongX).TransformUnclipped(values);

    /// <summary><see cref="TransformUnclippedAlong"/> on the X axis, for a caller whose direction is fixed.</summary>
    /// <param name="xData">Data-space X values.</param>
    /// <returns>Pixel-space X values, one per input element.</returns>
    internal double[] TransformXUnclipped(ReadOnlySpan<double> xData) => TransformUnclippedAlong(true, xData);

    /// <summary><see cref="TransformUnclippedAlong"/> on the Y axis, for a caller whose direction is fixed.</summary>
    /// <param name="yData">Data-space Y values.</param>
    /// <returns>Pixel-space Y values, one per input element.</returns>
    internal double[] TransformYUnclipped(ReadOnlySpan<double> yData) => TransformUnclippedAlong(false, yData);

    /// <summary><see cref="TransformClippedAlong"/> on the Y axis (the bases of an area, a stacked layer's floor), for a caller whose
    /// direction is fixed.</summary>
    /// <param name="yData">The bases, in data space.</param>
    /// <returns>Pixel-space Y values, one per input element.</returns>
    internal double[] TransformYClipped(ReadOnlySpan<double> yData) => TransformClippedAlong(false, yData);

    /// <summary>Batch-transforms the bases of a mark (an area's lower edge, a stacked layer's floor) on the X axis along X, on the Y
    /// axis otherwise, to pixels: as <see cref="TransformX"/> (<see cref="TransformY"/>), with every base the axis' scale cannot place
    /// clipped to the edge where the axis begins, by the rule of <see cref="DataToPixelClippedAlong"/>. On an axis that places every
    /// finite value the result is the plain transform's, bit for bit.</summary>
    /// <param name="alongX">Whether the bases lie on the X axis.</param>
    /// <param name="values">The bases, in data space.</param>
    /// <returns>Pixel-space values, one per input element.</returns>
    internal double[] TransformClippedAlong(bool alongX, ReadOnlySpan<double> values) =>
        _dir.Along(alongX).TransformClipped(values, StartAlong(alongX).Edge);

    /// <summary>The line through these data points as matplotlib draws it on these axes: each point where
    /// <see cref="TransformBatch"/> places it, and a point at a value the log scale cannot place a thousand decades beyond where
    /// that axis begins, the line cut where it crosses that edge (<see cref="CutBeyondTheStart"/>), so it dives to the edge and
    /// climbs back from it. A missing value and an infinity still break the line (a NaN point, which
    /// <c>MissingValueExtensions.LineRuns</c> splits it at).</summary>
    /// <param name="xData">Data-space X values.</param>
    /// <param name="yData">Data-space Y values; as many as <paramref name="xData"/>.</param>
    /// <returns><see cref="TransformBatch"/>'s array itself when no point lies beyond an axis, so such a line is placed as every
    /// point is; otherwise the cut line.</returns>
    /// <remarks>On a log axis the placed points are read once more, for a point the scale clips; a line with none is not read a
    /// third time for the cut.</remarks>
    internal IReadOnlyList<Point> TransformPath(ReadOnlySpan<double> xData, ReadOnlySpan<double> yData)
    {
        var placed = TransformBatch(xData, yData);
        var beyond = PlaceBeyond(placed, xData, yData);
        return beyond.Any || HasRooms ? CutAtTheStart(placed, closed: false, beyond) : placed;
    }

    /// <summary>The vertices of a path as <see cref="DataToPixelUnclipped"/> places them, a whole array at once: each point where
    /// <see cref="TransformBatch"/> places it, and a point at a value the log scale cannot place a thousand decades beyond where
    /// that axis begins, for <see cref="CutBeyondTheStart"/> to cut the path there. On axes with no log scale the result is
    /// <see cref="TransformBatch"/>'s, bit for bit.</summary>
    /// <param name="xData">Data-space X values.</param>
    /// <param name="yData">Data-space Y values; as many as <paramref name="xData"/>.</param>
    /// <returns>One pixel per point.</returns>
    internal Point[] TransformBatchUnclipped(ReadOnlySpan<double> xData, ReadOnlySpan<double> yData)
    {
        var placed = TransformBatch(xData, yData);
        PlaceBeyond(placed, xData, yData);
        return placed;
    }

    /// <summary>Moves every point of <paramref name="placed"/> that <see cref="TransformBatch"/> masked but a log axis clips to
    /// where <see cref="DataToPixelUnclipped"/> puts it, a thousand decades beyond that axis.</summary>
    /// <returns>The axes such a point stands beyond; none on axes with no log scale, which are not read at all.</returns>
    private AxisPair<bool> PlaceBeyond(Point[] placed, ReadOnlySpan<double> xData, ReadOnlySpan<double> yData)
    {
        var beyond = default(AxisPair<bool>);
        if (double.IsNaN(_dir.X.Beyond) && double.IsNaN(_dir.Y.Beyond)) return beyond;

        for (int i = 0; i < placed.Length; i++)
        {
            if (!placed[i].IsDrawable() && DataToPixelUnclipped(xData[i], yData[i]) is var unclipped && unclipped.IsDrawable())
            {
                placed[i] = unclipped;
                beyond |= BeyondOf(unclipped);
            }
        }

        return beyond;
    }

    /// <summary>Whether <paramref name="point"/> stands where a value a log axis cannot place stands
    /// (<see cref="PixelDirection.Beyond"/>), a thousand decades beyond where that axis begins: a mark with no extent there (a
    /// marker) lies outside the plot and is not drawn.</summary>
    /// <param name="point">A pixel, from <see cref="DataToPixelUnclipped"/> or the unclipped batches.</param>
    /// <returns><see langword="true"/> when either coordinate stands there.</returns>
    internal bool LiesBeyond(Point point) => BeyondOf(point).Any;

    /// <summary><paramref name="point"/> with each coordinate that lies beyond its axis (<see cref="LiesBeyond"/>) moved onto the
    /// edge where that axis begins: where a segment that runs straight across the axis to it (a bar's side, an error bar, a stem) is
    /// cut, which <see cref="CutBeyondTheStart"/> gives for such a segment too.</summary>
    /// <param name="point">A pixel, from <see cref="DataToPixelUnclipped"/> or the unclipped batches.</param>
    /// <returns>The point, on the edge in a coordinate that lay beyond it.</returns>
    internal Point AtTheStart(Point point) => new(_dir.X.AtTheStart(point.X, StartAlong(true).Edge), _dir.Y.AtTheStart(point.Y, StartAlong(false).Edge));

    /// <summary>A straight mark (an error bar, a stem, a violin's centre bar or tick) cut where it leaves the plot through the
    /// edge an axis begins at, by the rule of <see cref="CutBeyondTheStart"/>, without building a path: a segment that runs
    /// straight across the axis to a value it cannot place is cut on the edge right under that end, the pixel a base pinned there
    /// stands at.</summary>
    /// <param name="from">One end, from <see cref="DataToPixelUnclipped"/>.</param>
    /// <param name="to">The other end, from <see cref="DataToPixelUnclipped"/>.</param>
    /// <returns>The part that is drawn, its ends in the order given; <see langword="null"/> when an end is missing (NaN, an
    /// infinity) or both lie beyond one axis.</returns>
    internal SeriesRenderers.PixelSegment? CutSegment(Point from, Point to)
    {
        if (!from.CanDrawLineTo(to)) return null;
        SeriesRenderers.PixelSegment? segment = new SeriesRenderers.PixelSegment(from, to);
        return CutAt(CutAt(segment, StartAlong(true)), StartAlong(false));

        // The segment cut at the edge where an axis begins when an end lies beyond that axis.
        SeriesRenderers.PixelSegment? CutAt(SeriesRenderers.PixelSegment? part, StartEdge edge) =>
            part is { } s && (LiesBeyondAlong(s.From, edge.AlongX) || LiesBeyondAlong(s.To, edge.AlongX)) ? edge.Cut(s.From, s.To) : part;
    }

    /// <summary>The straight mark from (<paramref name="x1"/>, <paramref name="y1"/>) to (<paramref name="x2"/>, <paramref name="y2"/>)
    /// in data units as the plot leaves it: both ends placed as matplotlib's <c>transData</c> places them
    /// (<see cref="DataToPixelUnclipped"/>), then cut (<see cref="CutSegment"/>).</summary>
    /// <param name="x1">The X value of the first end.</param>
    /// <param name="y1">The Y value of the first end.</param>
    /// <param name="x2">The X value of the second end.</param>
    /// <param name="y2">The Y value of the second end.</param>
    /// <returns>The part that is drawn; <see langword="null"/> when nothing of it is.</returns>
    internal SeriesRenderers.PixelSegment? SegmentBetween(double x1, double y1, double x2, double y2) =>
        CutSegment(DataToPixelUnclipped(x1, y1), DataToPixelUnclipped(x2, y2));

    /// <summary>Whether <paramref name="point"/>'s coordinate along a direction stands where a value that axis' log scale cannot place
    /// stands (<see cref="PixelDirection.LiesBeyond"/>).</summary>
    private bool LiesBeyondAlong(Point point, bool alongX) => _dir.Along(alongX).LiesBeyond(point.CoordinateAlong(alongX));

    /// <summary>A path cut where it leaves the plot through the edge an axis begins at, as the axes clip cuts what matplotlib's log
    /// scale puts a thousand decades below the axis. Only a path with a vertex standing there (<see cref="LiesBeyond"/>) is cut, and
    /// only at the edge of the axis it lies beyond; every other path is handed back as given. An open path (a line) leaves a gap (a
    /// NaN point) between where it leaves the plot and where it comes back; a closed one (the outline of a fill) runs along the edge
    /// between the two. A vertex on the edge (a base pinned there) lies inside and is kept.</summary>
    /// <param name="path">The vertices in pixels; an open path may hold NaN points (gaps), a closed one holds none.</param>
    /// <param name="closed">Whether the path closes back to its first vertex.</param>
    /// <returns>The cut path; <paramref name="path"/> itself when nothing of it lies beyond an axis.</returns>
    internal IReadOnlyList<Point> CutBeyondTheStart(IReadOnlyList<Point> path, bool closed)
    {
        double beyondX = _dir.X.Beyond, beyondY = _dir.Y.Beyond;
        if (double.IsNaN(beyondX) && double.IsNaN(beyondY)) return CutAtTheStart(path, closed, default);

        // The vertices are read as a span where the list is an array or a List (every caller's), not through the indexer.
        var beyond = path switch
        {
            Point[] array => BeyondIn(array, beyondX, beyondY),
            List<Point> list => BeyondIn(CollectionsMarshal.AsSpan(list), beyondX, beyondY),
            _ => BeyondIn([.. path], beyondX, beyondY),
        };

        return CutAtTheStart(path, closed, beyond);

        static AxisPair<bool> BeyondIn(ReadOnlySpan<Point> points, double beyondX, double beyondY)
        {
            bool cutX = false, cutY = false;
            foreach (var point in points)
            {
                cutX |= point.X == beyondX;
                cutY |= point.Y == beyondY;
            }

            return new AxisPair<bool>(cutX, cutY);
        }
    }

    /// <summary><paramref name="path"/> cut at the edge where each axis <paramref name="beyond"/> names begins, by the rule of
    /// <see cref="CutBeyondTheStart"/>: for a path whose vertices are not where a value beyond the axis stands themselves, though
    /// it was drawn through one (a curve flattened in pixels through a control point a log axis cannot place). On axes drawn in parts
    /// (<see cref="HasRooms"/>) it is then cut where it crosses a room (<see cref="Rendering.DataTransform.RoomCut.Cut"/>).</summary>
    /// <param name="path">The vertices in pixels.</param>
    /// <param name="closed">Whether the path closes back to its first vertex.</param>
    /// <param name="beyond">The axes the path reaches beyond.</param>
    /// <returns>The cut path; <paramref name="path"/> itself when <paramref name="beyond"/> names no axis and no axis is drawn in parts.</returns>
    internal IReadOnlyList<Point> CutAtTheStart(IReadOnlyList<Point> path, bool closed, AxisPair<bool> beyond)
    {
        if (beyond.X) path = StartAlong(true).Cut(path, closed);
        if (beyond.Y) path = StartAlong(false).Cut(path, closed);
        return HasRooms ? RoomCut.Cut(path, closed, _dir) : path;
    }

    /// <summary>Whether either axis is drawn in parts that stand room apart (<see cref="AxisBreakMap.IsDrawnInParts"/>).</summary>
    internal bool HasRooms => _dir.X.Breaks is { IsDrawnInParts: true } || _dir.Y.Breaks is { IsDrawnInParts: true };

    /// <summary>The axes <paramref name="point"/> stands beyond: each coordinate where a value its axis' log scale cannot place stands
    /// (<see cref="PixelDirection.LiesBeyond"/>).</summary>
    /// <param name="point">A pixel, from <see cref="DataToPixelUnclipped"/>.</param>
    /// <returns>The axes the pixel stands beyond.</returns>
    internal AxisPair<bool> BeyondOf(Point point) => new(_dir.X.LiesBeyond(point.X), _dir.Y.LiesBeyond(point.Y));

    /// <summary>The rule of the X axis' scale this transform maps through, bound to its threshold, picked once when it was made;
    /// <see cref="DataXMin"/>/<see cref="DataXMax"/> are in its drawn space.</summary>
    internal ScaleRule XRule => _dir.X.Rule;

    /// <summary>The rule of the Y axis' scale this transform maps through; <see cref="DataYMin"/>/<see cref="DataYMax"/> are
    /// in its drawn space.</summary>
    internal ScaleRule YRule => _dir.Y.Rule;

    /// <summary>Batch-transforms X data coordinates to pixel X values, a vector of values at a time; each lands on the bits
    /// <see cref="DataToPixel"/> gives it.</summary>
    /// <param name="xData">Data-space X values.</param>
    /// <returns>Pixel-space X values, one per input element.</returns>
    public double[] TransformX(ReadOnlySpan<double> xData) => _dir.X.Transform(xData);

    /// <summary>Batch-transforms Y data coordinates to pixel Y values, a vector of values at a time; each lands on the bits
    /// <see cref="DataToPixel"/> gives it. Note: Y axis is inverted — larger data values map to smaller pixel Y.</summary>
    /// <remarks>The orientation is the range's: a Y range handed over high to low (a limit set high to low, or an axis
    /// drawn <see cref="Models.Axis.Inverted"/>, which the renderer turns into such a range before it builds this transform)
    /// maps through a scale of the other sign, so larger values land lower. Downstream renderers receive pixel coordinates
    /// in the drawn orientation and must not apply a second inversion.</remarks>
    /// <param name="yData">Data-space Y values.</param>
    /// <returns>Pixel-space Y values, one per input element.</returns>
    public double[] TransformY(ReadOnlySpan<double> yData) => _dir.Y.Transform(yData);

    /// <summary>Batch-transforms paired X/Y data coordinates to pixel-space <see cref="Point"/> values.</summary>
    /// <param name="xData">Data-space X values.</param>
    /// <param name="yData">Data-space Y values; must be the same length as <paramref name="xData"/>.</param>
    /// <returns>Array of pixel-space points.</returns>
    /// <remarks>Uses AVX SIMD on x86-64, four points a step written straight into the memory of the <c>Point[]</c>, and a scalar
    /// loop for the points left over and on other processors. On an axis drawn in a space of its own (log, symlog, logit) the values
    /// are first mapped through its scale in one call to its rule, into a buffer borrowed from the shared pool; only an axis with
    /// breaks places point by point. Every point lands on the bits <see cref="DataToPixel"/> gives it, whichever path places it.</remarks>
    public Point[] TransformBatch(ReadOnlySpan<double> xData, ReadOnlySpan<double> yData)
    {
        int n = xData.Length;
        var result = new Point[n];

        if (_dir.X.Breaks is not null || _dir.Y.Breaks is not null)
        {
            for (int i = 0; i < n; i++)
                result[i] = DataToPixel(xData[i], yData[i]);
            return result;
        }

        var pixels = MemoryMarshal.Cast<Point, double>((Span<Point>)result);
        if (!_dir.X.IsShaped && !_dir.Y.IsShaped)
        {
            pixels.SpreadPairs(xData, yData, _dir.X, _dir.Y);
            return result;
        }

        double[] scratch = ArrayPool<double>.Shared.Rent(2 * n);
        try
        {
            var room = scratch.AsSpan(0, 2 * n);
            pixels.SpreadPairs(_dir.X.PlacedAll(xData, room[..n]), _dir.Y.PlacedAll(yData, room[n..]), _dir.X, _dir.Y);
        }
        finally
        {
            ArrayPool<double>.Shared.Return(scratch);
        }

        return result;
    }

    /// <summary>Converts pixel-space coordinates back to data-space coordinates.</summary>
    /// <remarks>The pixel is interpolated across the drawn range (in the axis' scaled space on a log or symlog axis, with
    /// its breaks cut out), then read back by the rule that placed it: the breaks put back and the axis scale inverted
    /// so the result is in data units on
    /// every scale, past a break too. The pixel where a break is cut reads as where the break begins. The
    /// server-interaction script converts the pointer by the same rule. It starts from where the way there starts, the start of
    /// the range, so a value placed and read back is that value again, in a narrow range far from 0 too.</remarks>
    /// <param name="pixel">The pixel-space point to convert.</param>
    /// <returns>The corresponding data-space point.</returns>
    public Point PixelToData(Point pixel)
    {
        var data = new AxisPair<double>(pixel.X, pixel.Y).Select(this, static (coordinate, alongX, transform) => transform.DataAt(coordinate, alongX));
        return new Point(data.X, data.Y);
    }

    /// <summary>The data value a pixel coordinate along a direction stands for (<see cref="PixelToData"/>): its share of the plot's
    /// length, counted the way a value grows on the screen, across the drawn range from its start, then read back past the breaks
    /// and through the scale.</summary>
    private double DataAt(double pixel, bool alongX)
    {
        ref readonly var dir = ref _dir.Along(alongX);
        double drawn = DrawnAt(pixel, alongX);
        return dir.Breaks?.Inverse(drawn) ?? dir.Rule.Inverse(drawn);
    }

    /// <summary>Where a pixel coordinate along a direction stands in its drawn space: its share of the plot's length, counted the way a
    /// value grows on the screen, across the drawn range from its start.</summary>
    private double DrawnAt(double pixel, bool alongX)
    {
        ref readonly var dir = ref _dir.Along(alongX);
        return dir.Anchor + _plotBounds.FractionAt(alongX, pixel) * (_dataMax.Along(alongX) - dir.Anchor);
    }

    /// <summary>The range a zoom by <paramref name="scale"/> about <paramref name="pixel"/> shows, low to high along each direction, so the
    /// value under the pointer stays under it: the edges of the plot moved toward the pointer (a scale below 1) or away from it, read
    /// through this view; a direction drawn in parts (<see cref="AxisBreakMap.Room"/>) is zoomed in the space of its parts
    /// (<see cref="AxisBreakMap.WithoutRooms"/>), whose rooms grow and shrink with the parts. The one rule a wheel zooms by.</summary>
    /// <param name="pixel">The pointer, in pixels.</param>
    /// <param name="scale">The share of the range the zoom keeps.</param>
    /// <returns>The range, in data units.</returns>
    internal DataRange ZoomedAbout(Point pixel, double scale)
    {
        var range = new AxisPair<double>(pixel.X, pixel.Y).Select(this, (at, alongX, transform) => transform.ZoomedAlong(at, scale, alongX));
        return new DataRange(range.X.Lo, range.X.Hi, range.Y.Lo, range.Y.Hi);
    }

    /// <summary>The range a zoom about <paramref name="at"/> shows along one direction (<see cref="ZoomedAbout"/>).</summary>
    private Range1D ZoomedAlong(double at, double scale, bool alongX)
    {
        ref readonly var dir = ref _dir.Along(alongX);
        if (dir.Breaks is { IsDrawnInParts: true } map)
        {
            double centre = map.WithoutRooms(DrawnAt(at, alongX));
            double low = map.WithoutRooms(dir.Anchor), high = map.WithoutRooms(_dataMax.Along(alongX));
            return new Range1D(map.InverseWithoutRooms(centre - (centre - low) * scale), map.InverseWithoutRooms(centre + (high - centre) * scale));
        }

        double start = _plotBounds.ValueStartAlong(alongX), end = _plotBounds.ValueEndAlong(alongX);
        return new Range1D(DataAt(at - (at - start) * scale, alongX), DataAt(at + (end - at) * scale, alongX));
    }

    /// <summary>The breaks of the X axis as they are cut out of the drawn space; <see langword="null"/> when it has none.</summary>
    internal AxisBreakMap? XBreaks => _dir.X.Breaks;

    /// <summary>The breaks of the Y axis as they are cut out of the drawn space; <see langword="null"/> when it has none.</summary>
    internal AxisBreakMap? YBreaks => _dir.Y.Breaks;

    /// <summary>The breaks of the axis along X (<paramref name="alongX"/>) or along Y as they are cut out of the drawn space;
    /// <see langword="null"/> when it has none.</summary>
    /// <param name="alongX">The X axis when <see langword="true"/>.</param>
    /// <returns>The map.</returns>
    internal AxisBreakMap? BreaksAlong(bool alongX) => _dir.Along(alongX).Breaks;

    /// <summary>The rooms the axis along X (<paramref name="alongX"/>) or along Y leaves between its parts on the plot, in screen order
    /// (<see cref="PixelDirection.PixelRooms"/>); none when the axis is drawn in one piece.</summary>
    internal PixelRoom[] RoomsAlong(bool alongX) => _dir.Along(alongX).PixelRooms();

    /// <summary>The room at <paramref name="index"/> of the axis along X (<paramref name="alongX"/>) or along Y, in the order its break
    /// map holds them (<see cref="AxisBreakMap.Rooms"/>), on the plot (<see cref="PixelDirection.RoomAt"/>); the axis must be drawn in parts.</summary>
    internal PixelRoom RoomAlong(bool alongX, int index) => _dir.Along(alongX).RoomAt(index);
}
