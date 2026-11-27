// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.CartesianParts;

/// <summary>
/// Renders horizontal/vertical span regions (filled rectangles behind series).
/// Draws each SpanRegion's fill + optional border +
/// optional label.
/// <para>A span is clipped to the plot area, as matplotlib's <c>axhspan</c> is: an edge beyond the drawn range runs to the
/// plot edge, and a span wholly beyond the range covers none of the plot and draws nothing. This is what lets
/// <see cref="Axes.AddThreshold"/> store its zone as a span to ±<see cref="double.MaxValue"/>, and what keeps a finite span
/// wider than the range off the margins, the title and the labels. <see cref="ExtentOf"/> is the one place that says so, for
/// horizontal and vertical spans, normal and reversed axes alike.</para>
/// </summary>
internal sealed class CartesianSpansPart : CartesianAxesPart
{
    private readonly DataRange _range;

    /// <summary>Constructs a Spans part. Requires the computed data range for
    /// positioning span rectangles within the plot area.</summary>
    /// <param name="context">What the render draws with: the axes, the plot area, the surface, the theme and the primary transform.</param>
    /// <param name="range">The range the axes shows.</param>
    internal CartesianSpansPart(in CartesianPartContext context, DataRange range)
        : base(context)
    {
        _range = range;
    }

    /// <summary>One edge of a span as it is drawn: where it stands in pixels along the span's axis, and whether it is the
    /// span's own edge (<see langword="true"/>) or the plot edge the span was cut at (<see langword="false"/>), where a
    /// border has no line, as the axes clip the outline of matplotlib's rectangle.</summary>
    private readonly record struct SpanEdge(double Pixel, bool IsOwn);

    /// <summary>The part of a span that lies in the plot, as its two edges in pixel order: <see cref="Near"/> is the one
    /// with the smaller pixel (the top of a horizontal span, the left of a vertical one), whichever way the axis runs.</summary>
    private readonly record struct SpanExtent(SpanEdge Near, SpanEdge Far);

    /// <inheritdoc />
    public override void Render()
    {
        foreach (var span in Axes.Spans)
        {
            var spanColor = span.FillUnder(Theme).WithOpacityGiven(span.Alpha);
            // The border is the span's line (IStrokedTool): its edge colour, else its fill; the label wears it too.
            var border = span.StrokeUnder(Theme);
            // A span runs along X (horizontal) or along Y over the whole plot; its edges stand across it, on the other axis.
            bool alongX = span.Orientation.RunsAlongX;
            if (ExtentOf(span, _range.Along(!alongX), value => Transform.PixelAlong(value, !alongX)) is not { } extent) continue;
            var (near, far) = extent;
            double start = PlotArea.StartAlong(alongX), end = PlotArea.EndAlong(alongX);
            Ctx.DrawRectangle(Rect.Along(alongX, start, PlotArea.LengthAlong(alongX), near.Pixel, far.Pixel - near.Pixel), ShapeStyle.Filled(spanColor));
            if (span.LineStyle != LineStyle.None)
            {
                if (near.IsOwn) Ctx.DrawLine(Point.Along(alongX, start, near.Pixel), Point.Along(alongX, end, near.Pixel), border);
                if (far.IsOwn) Ctx.DrawLine(Point.Along(alongX, start, far.Pixel), Point.Along(alongX, end, far.Pixel), border);
                Ctx.DrawLine(Point.Along(alongX, start, near.Pixel), Point.Along(alongX, start, far.Pixel), border);
                Ctx.DrawLine(Point.Along(alongX, end, near.Pixel), Point.Along(alongX, end, far.Pixel), border);
            }
            if (span.Label is null) continue;
            // The label of a span that runs along X stands at its top left, that of one along Y centred over its top.
            if (alongX)
                Ctx.DrawLineLabel(span.Label, LineLabelPlacement.InsideTopLeft(PlotArea.X, near.Pixel), Theme, border.Color);
            else
                Ctx.DrawLineLabel(span.Label, LineLabelPlacement.InsideTopCentre((near.Pixel + far.Pixel) / 2, PlotArea.Y),
                    Theme, border.Color);
        }
    }

    /// <summary>The part of <paramref name="span"/> that lies in the plot: the span cut to the range the axis draws, in data
    /// units, then placed. An edge beyond the range (a finite one, an infinity, ±<see cref="double.MaxValue"/>, 0 or below on
    /// a log axis) runs to the plot edge. The edges are placed first and sorted after, so a reversed axis, where the larger
    /// value is the lower edge on screen, draws the same area.</summary>
    /// <param name="span">The span, in data units, its edges in either order.</param>
    /// <param name="range">The range the span's axis draws, from its low end to its high end or the other way round (a
    /// reversed limit).</param>
    /// <param name="toPixel">Places a value on the span's axis, in pixels.</param>
    /// <returns>The two edges as drawn, or <see langword="null"/> when the span covers none of the plot (it lies beyond the
    /// range, or only touches its edge), an edge is missing (NaN), or an edge stands where its axis cannot place it (inside
    /// an axis break). A span of no width inside the range is a line and is kept.</returns>
    private static SpanExtent? ExtentOf(SpanRegion span, Range1D range, Func<double, double> toPixel)
    {
        var drawn = range.Interval;
        double low = Math.Min(span.Min, span.Max), high = Math.Max(span.Min, span.Max);
        double from = Math.Max(low, drawn.Lo), to = Math.Min(high, drawn.Hi);
        if (from > to || (from == to && low != high)) return null;

        // A missing edge (NaN) stays NaN through the cut and the placement, and goes out below with an edge the axis cannot
        // place (inside a break): one test for both.
        var first = new SpanEdge(toPixel(from), low >= drawn.Lo);
        var second = new SpanEdge(toPixel(to), high <= drawn.Hi);
        if (!first.Pixel.IsDrawable() || !second.Pixel.IsDrawable()) return null;

        return first.Pixel <= second.Pixel ? new SpanExtent(first, second) : new SpanExtent(second, first);
    }
}
