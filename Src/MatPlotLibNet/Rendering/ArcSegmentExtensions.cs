// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering;

/// <summary>Follows an <see cref="ArcSegment"/> with cubic curves, for a canvas whose path builder takes lines and curves
/// and no arcs.</summary>
public static class ArcSegmentExtensions
{
    extension(ArcSegment arc)
    {
        /// <summary>The cubic Béziers that follow the arc from its start angle to its end angle, in the convention of
        /// <see cref="ArcSegment"/> (0° is +X, positive angles sweep clockwise on a screen whose Y points down): at most a
        /// quarter turn to a curve, each with control points on the tangents at its ends, 4/3·tan(¼ of its sweep) of the
        /// way along them. The arc's start point is the current point of the path it belongs to, as it is for the SVG
        /// <c>A</c> command, so the first curve is not given it.</summary>
        /// <returns>The curves in drawing order; none for an arc with no sweep or with an angle that is not a number.</returns>
        public IReadOnlyList<BezierSegment> ToBeziers()
        {
            double sweep = arc.EndAngle - arc.StartAngle;
            if (!double.IsFinite(sweep) || sweep == 0 || !double.IsFinite(arc.StartAngle))
            {
                return [];
            }

            int pieces = (int)Math.Ceiling(Math.Abs(sweep) / 90.0 - 1e-9);
            double step = sweep / pieces * Math.PI / 180.0;
            double k = 4.0 / 3.0 * Math.Tan(step / 4.0);
            double angle = arc.StartAngle * Math.PI / 180.0;

            var curves = new List<BezierSegment>(pieces);
            for (int i = 0; i < pieces; i++)
            {
                double next = angle + step;
                var from = RimPoint(arc, angle);
                var to = RimPoint(arc, next);
                var leaving = Tangent(arc, angle);
                var arriving = Tangent(arc, next);
                curves.Add(new BezierSegment(
                    new Point(from.X + k * leaving.X, from.Y + k * leaving.Y),
                    new Point(to.X - k * arriving.X, to.Y - k * arriving.Y),
                    to));
                angle = next;
            }

            return curves;
        }
    }

    private static Point RimPoint(ArcSegment arc, double radians) =>
        new(arc.Center.X + arc.RadiusX * Math.Cos(radians), arc.Center.Y + arc.RadiusY * Math.Sin(radians));

    private static Point Tangent(ArcSegment arc, double radians) =>
        new(-arc.RadiusX * Math.Sin(radians), arc.RadiusY * Math.Cos(radians));
}
