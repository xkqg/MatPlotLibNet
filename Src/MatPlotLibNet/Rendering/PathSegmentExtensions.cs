// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering;

/// <summary>The one rule for an outline in data units (a patch's, <c>Patch.Outline</c>): how it is drawn through the transform of
/// its axes, and how far it reaches for the automatic range. An outline is a list of path segments whose points are data values:
/// moves, straight lines, cubic curves (their control points in data units too) and closes. An <see cref="ArcSegment"/> is in
/// pixels by its definition, so no outline holds one.</summary>
internal static class PathSegmentExtensions
{
    extension(IReadOnlyList<PathSegment> outline)
    {
        /// <summary>The outline in pixels: every point, a curve's control points included, mapped through
        /// <paramref name="transform"/>, so on a log axis a curve's control points move as matplotlib moves them and a straight
        /// edge stays straight. A finite coordinate the axis' scale cannot place (0 or below on a log axis) is pinned to the edge
        /// where that axis begins (<see cref="DataTransform.DataToPixelClipped"/>). A point the axes cannot place at all (inside an
        /// axis break) is left out with the segment it ends, and the outline resumes with a move at the next point it can place:
        /// a subpath of its own, and an outline broken that way is not closed across the gap. A segment no outline in data units
        /// holds (an arc) is not placed either.</summary>
        /// <param name="transform">The transform of the axes the outline is drawn on.</param>
        /// <returns>The path in pixels; empty when no line and no curve is left between two points that can be placed.</returns>
        internal IReadOnlyList<PathSegment> ToPixels(DataTransform transform)
        {
            var path = new List<PathSegment>(outline.Count);
            bool hasPoint = false;   // the path stands at a point the next segment can continue from
            bool broken = false;     // a point of the current subpath was left out
            bool draws = false;      // a line or a curve was placed

            foreach (var segment in outline)
            {
                switch (segment)
                {
                    case MoveToSegment move when Placed(move.Point, out var to):
                        path.Add(new MoveToSegment(to));
                        hasPoint = true;
                        broken = false;
                        break;
                    case LineToSegment line when Placed(line.Point, out var to):
                        path.Add(hasPoint ? new LineToSegment(to) : new MoveToSegment(to));
                        draws |= hasPoint;
                        hasPoint = true;
                        break;
                    case BezierSegment curve when Placed(curve.Control1, out var c1) && Placed(curve.Control2, out var c2) && Placed(curve.End, out var end):
                        path.Add(hasPoint ? new BezierSegment(c1, c2, end) : new MoveToSegment(end));
                        draws |= hasPoint;
                        hasPoint = true;
                        break;
                    case CloseSegment when hasPoint && !broken:
                        path.Add(segment);
                        break;
                    case CloseSegment:
                        hasPoint = false;
                        break;
                    default:
                        // A point that cannot be placed, or an arc: the pen lifts, and the subpath has lost a point.
                        hasPoint = false;
                        broken = true;
                        break;
                }
            }

            return draws ? path : [];

            bool Placed(Point data, out Point pixel)
            {
                pixel = transform.DataToPixelClipped(data.X, data.Y);
                return pixel.IsDrawable();
            }
        }

        /// <summary>The points of the outline that bound it: every vertex, and inside each curve the points where it turns back
        /// along X or Y (where its derivative along that axis is 0), so the extent is where the curve reaches, not where its control
        /// points stand. matplotlib's data limits of a patch take the same extremes. A curve starts at the point the path stands at:
        /// the end of the segment before it, or the start of its subpath after a close. An arc adds nothing.</summary>
        /// <returns>The bounding points, in data units, in the order the outline reaches them.</returns>
        internal IEnumerable<Point> ExtremePoints()
        {
            var current = default(Point);
            var start = default(Point);
            foreach (var segment in outline)
            {
                switch (segment)
                {
                    case MoveToSegment move:
                        current = start = move.Point;
                        yield return current;
                        break;
                    case LineToSegment line:
                        current = line.Point;
                        yield return current;
                        break;
                    case BezierSegment curve:
                        foreach (double t in TurningPoints(current.X, curve.Control1.X, curve.Control2.X, curve.End.X)
                                     .Concat(TurningPoints(current.Y, curve.Control1.Y, curve.Control2.Y, curve.End.Y)))
                        {
                            yield return new Point(
                                At(t, current.X, curve.Control1.X, curve.Control2.X, curve.End.X),
                                At(t, current.Y, curve.Control1.Y, curve.Control2.Y, curve.End.Y));
                        }

                        current = curve.End;
                        yield return current;
                        break;
                    case CloseSegment:
                        current = start;
                        break;
                }
            }
        }
    }

    /// <summary>Where, strictly between its ends, the cubic through <paramref name="p0"/>..<paramref name="p3"/> along one axis turns
    /// back: the roots in (0, 1) of its derivative, 3·[(1-t)²(p1-p0) + 2(1-t)t(p2-p1) + t²(p3-p2)], a quadratic in t solved in the
    /// form that loses no digits when its two roots differ greatly in size.</summary>
    private static IEnumerable<double> TurningPoints(double p0, double p1, double p2, double p3)
    {
        double a = p1 - p0, b = p2 - p1, c = p3 - p2;
        double quadratic = a - 2 * b + c, linear = 2 * (b - a), constant = a;

        if (quadratic == 0)
        {
            if (linear != 0 && Inside(-constant / linear)) yield return -constant / linear;
            yield break;
        }

        double discriminant = linear * linear - 4 * quadratic * constant;
        if (discriminant < 0) yield break;

        double q = -0.5 * (linear + Math.CopySign(Math.Sqrt(discriminant), linear));
        if (Inside(q / quadratic)) yield return q / quadratic;
        if (q != 0 && Inside(constant / q)) yield return constant / q;

        static bool Inside(double t) => t is > 0 and < 1;
    }

    /// <summary>The cubic through <paramref name="p0"/>..<paramref name="p3"/> along one axis, at <paramref name="t"/>.</summary>
    private static double At(double t, double p0, double p1, double p2, double p3)
    {
        double s = 1 - t;
        return s * s * s * p0 + 3 * s * s * t * p1 + 3 * s * t * t * p2 + t * t * t * p3;
    }
}
