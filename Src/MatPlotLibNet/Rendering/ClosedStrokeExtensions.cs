// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering;

/// <summary>A closed outline drawn with a stroke: the rings and spines of a polar axes, the rings of a radar web. A solid outline is
/// the shape itself (a circle, a polygon); a shape's outline takes no dash, so a dotted or dashed one is a closed polyline through the
/// line drawing that dashes. The one place that decides it.</summary>
internal static class ClosedStrokeExtensions
{
    /// <summary>How many segments a circle that is not solid is cut into: a segment turns two degrees, so the polyline stands on its
    /// circle to a hundredth of a pixel at the radii a chart is drawn at.</summary>
    private const int CircleSegments = 180;

    extension(IRenderContext ctx)
    {
        /// <summary>Draws the circle of <paramref name="radius"/> about <paramref name="centre"/> in <paramref name="stroke"/>: a circle when
        /// the stroke is solid; otherwise a closed polyline of <see cref="CircleSegments"/> segments, walked as matplotlib walks the path
        /// of a ring (a gridline over the polar transform, and the arc spine, <c>Path.arc(0, 360)</c>): from theta 0, the east, with
        /// theta, which runs counter-clockwise on the screen, so the dashes of the stroke fall where they fall in matplotlib's picture.</summary>
        /// <param name="centre">The centre, in pixels.</param>
        /// <param name="radius">The radius, in pixels.</param>
        /// <param name="stroke">The stroke.</param>
        internal void DrawCircleStroke(Point centre, double radius, StrokeStyle stroke)
        {
            if (stroke.IsSolid) ctx.DrawCircle(centre, radius, ShapeStyle.Outlined(stroke.Color, stroke.Thickness));
            else ctx.DrawBrokenCircle(centre, radius, stroke);
        }

        /// <summary>A circle that is not solid: a closed polyline of <see cref="CircleSegments"/> segments, walked as matplotlib walks the
        /// path of a ring from theta 0, the east, counter-clockwise on the screen.</summary>
        private void DrawBrokenCircle(Point centre, double radius, StrokeStyle stroke)
        {
            var ring = new Point[CircleSegments + 1];
            for (int i = 0; i < CircleSegments; i++)
            {
                double angle = 2 * Math.PI * i / CircleSegments;
                ring[i] = centre.AtAngleCounterClockwise(radius, angle);
            }
            ring[^1] = ring[0];
            ctx.DrawLines(ring, stroke);
        }

        /// <summary>Draws the polygon <paramref name="ring"/> in <paramref name="stroke"/>: a polygon when the stroke is solid; otherwise the
        /// ring closed on its first point and drawn as a polyline.</summary>
        /// <param name="ring">The polygon's points, in order; a dashed one gets its first point again at its end.</param>
        /// <param name="stroke">The stroke.</param>
        internal void DrawPolygonStroke(List<Point> ring, StrokeStyle stroke)
        {
            if (stroke.IsSolid)
            {
                ctx.DrawPolygon(ring, ShapeStyle.Outlined(stroke.Color, stroke.Thickness));
                return;
            }

            ring.Add(ring[0]);
            ctx.DrawLines(ring, stroke);
        }
    }
}
