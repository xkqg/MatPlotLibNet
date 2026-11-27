// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering;

/// <summary>The one walk of a path and of a polyline into an <see cref="IPathSink"/>: where the kinds of segment are read, so no
/// target of a path holds a switch over them and no target builds a polyline by hand.</summary>
internal static class PathSinkExtensions
{
    extension(IReadOnlyList<PathSegment> segments)
    {
        /// <summary>Tells <paramref name="sink"/> what each segment is, in the order they are written. A segment of a kind the
        /// library does not draw (an application's own) is left out.</summary>
        /// <typeparam name="TSink">The kind of sink, so a sink that is a value is not boxed.</typeparam>
        /// <param name="sink">The target of the path; it holds what it builds, so it is taken by reference.</param>
        internal void WalkInto<TSink>(ref TSink sink) where TSink : struct, IPathSink
        {
            foreach (var segment in segments)
            {
                switch (segment)
                {
                    case MoveToSegment move: sink.MoveTo(move.Point); break;
                    case LineToSegment line: sink.LineTo(line.Point); break;
                    case BezierSegment curve: sink.CubicTo(curve.Control1, curve.Control2, curve.End); break;
                    case ArcSegment arc: sink.ArcTo(arc); break;
                    case CloseSegment: sink.Close(); break;
                }
            }
        }
    }

    extension(IReadOnlyList<Point> points)
    {
        /// <summary>Whether the points are a line: a polyline of fewer than two points has no segment to stroke, on any surface. A polygon
        /// has no such rule: it is drawn from the points it is given, the outline of two points being the line between them (matplotlib
        /// draws it so, and so does the SVG surface).</summary>
        internal bool FormsALine => points.Count >= 2;

        /// <summary>Tells <paramref name="sink"/> a polyline: a move to the first point, a line to each other point and, when
        /// <paramref name="closed"/>, a close. No point tells it nothing, closed or not.</summary>
        /// <typeparam name="TSink">The kind of sink, so a sink that is a value is not boxed.</typeparam>
        /// <param name="sink">The target of the polyline; it holds what it builds, so it is taken by reference.</param>
        /// <param name="closed">Whether the polyline closes back to its first point (a polygon) or stays open (a line).</param>
        internal void WalkPolyline<TSink>(ref TSink sink, bool closed) where TSink : struct, IPathSink
        {
            if (points.Count == 0) return;

            sink.MoveTo(points[0]);
            for (int i = 1; i < points.Count; i++) sink.LineTo(points[i]);
            if (closed) sink.Close();
        }
    }
}
