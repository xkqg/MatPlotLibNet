// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering;

/// <summary>The one place that cuts a straight line to the area it is drawn in. matplotlib clips every line of an axes to
/// the axes; a line drawn here between two pixels that the range the axes draw does not hold (a trendline beyond the data,
/// an extended trendline steeper than the range, a reference line or a level beyond a limit the caller set) would otherwise
/// run on over the margins, the tick labels and the title.</summary>
internal static class LineSegmentExtensions
{
    /// <summary>The part of <paramref name="segment"/> that lies inside <paramref name="area"/> (the Liang-Barsky cut). An end
    /// inside the area, or on its edge, is handed back as it was, bit for bit, so a segment inside the area comes back
    /// unchanged; an end outside is moved along the segment to the edge it crosses. The ends must be finite (a caller tests
    /// <see cref="MissingValueExtensions.CanDrawLineTo"/> first).</summary>
    /// <param name="segment">The segment, in pixels.</param>
    /// <param name="area">The area it may be drawn in, in pixels.</param>
    /// <returns>The part inside, in the direction of <paramref name="segment"/>; <see langword="null"/> when no part of it is
    /// inside, or it only touches the area in one point (a corner, or an end on an edge as it runs away).</returns>
    internal static LineSegment? ClippedTo(this LineSegment segment, Rect area)
    {
        var (from, to) = segment;
        double dx = to.X - from.X, dy = to.Y - from.Y;

        // The segment is from + t·(dx, dy) for t in [0, 1]; each edge keeps the t for which p·t <= q.
        var inside = new Stretch(0, 1)
            .Within(-dx, from.X - area.X)?
            .Within(dx, area.Right - from.X)?
            .Within(-dy, from.Y - area.Y)?
            .Within(dy, area.Bottom - from.Y);

        if (inside is not { } kept || kept.Enter >= kept.Leave) return null;

        Point At(double t) => new(from.X + t * dx, from.Y + t * dy);
        return new LineSegment(kept.Enter > 0 ? At(kept.Enter) : from, kept.Leave < 1 ? At(kept.Leave) : to);
    }

    /// <summary>A line across the whole width of <paramref name="area"/> at the height <paramref name="y"/>, as a horizontal
    /// reference line, a level and a Fibonacci level are drawn, cut to the area: handed back as it is when it lies inside the
    /// area or on its edge, <see langword="null"/> when it lies above or below it (a value beyond a limit the caller set), so
    /// it is not drawn over the margins, the tick labels or the title.</summary>
    /// <param name="area">The plot area, in pixels.</param>
    /// <param name="y">The height of the line, in pixels; finite.</param>
    /// <returns>The line, or <see langword="null"/> when it is outside the area.</returns>
    internal static LineSegment? HorizontalAcross(this Rect area, double y) =>
        new LineSegment(new Point(area.X, y), new Point(area.X + area.Width, y)).ClippedTo(area);

    /// <summary>A line across the whole height of <paramref name="area"/> at <paramref name="x"/>, as a vertical reference line
    /// is drawn, cut to the area: handed back as it is when it lies inside the area or on its edge, <see langword="null"/>
    /// when it lies left or right of it.</summary>
    /// <param name="area">The plot area, in pixels.</param>
    /// <param name="x">The place of the line, in pixels; finite.</param>
    /// <returns>The line, or <see langword="null"/> when it is outside the area.</returns>
    internal static LineSegment? VerticalAcross(this Rect area, double x) =>
        new LineSegment(new Point(x, area.Y), new Point(x, area.Y + area.Height)).ClippedTo(area);

    /// <summary>The stretch of a segment, as the parameter t runs along it, that is still inside every edge looked at so far:
    /// from <see cref="Enter"/> to <see cref="Leave"/>.</summary>
    private readonly record struct Stretch(double Enter, double Leave)
    {
        /// <summary>This stretch cut to the side of one edge the area lies on, where <c>p·t &lt;= q</c>.</summary>
        /// <param name="p">How fast the segment moves out across the edge as t grows (negative: it moves in).</param>
        /// <param name="q">How far the segment's start stands inside the edge.</param>
        /// <returns>The narrower stretch, or <see langword="null"/> when nothing of it is on the area's side.</returns>
        internal Stretch? Within(double p, double q)
        {
            // Parallel to the edge: wholly on the area's side of it, or wholly beyond.
            if (p == 0) return q >= 0 ? this : null;

            double t = q / p;
            if (p < 0)
            {
                // It comes in across this edge at t.
                if (t > Leave) return null;
                return t > Enter ? this with { Enter = t } : this;
            }

            // It goes out across this edge at t.
            if (t < Enter) return null;
            return t < Leave ? this with { Leave = t } : this;
        }
    }
}
