// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.SeriesRenderers;

namespace MatPlotLibNet.Rendering;

public sealed partial class DataTransform
{
    /// <summary>The edge of the plot an axis begins at, as the clip that cuts a path where it leaves the plot through it: the plot
    /// lies on the side the axis grows towards (<see cref="DataTransform.CutBeyondTheStart"/>).</summary>
    /// <param name="Edge">The pixel column (X) or row (Y) of the edge.</param>
    /// <param name="PerUnit">The axis' signed pixels per unit: the plot lies where the pixel moves from the edge the way a growing
    /// value moves.</param>
    /// <param name="AlongX">Whether the edge is a column, the start of the X axis; otherwise a row, the start of the Y axis.</param>
    private readonly record struct StartEdge(double Edge, double PerUnit, bool AlongX)
    {
        /// <summary><paramref name="path"/> cut at this edge. An open path keeps the parts inside, each part a run of its own: a NaN
        /// point stands between where it leaves and where it comes back, and a NaN point in it stays a gap. A closed path keeps its
        /// inside and runs along the edge between where it leaves and where it comes back (the Sutherland-Hodgman step for one edge);
        /// it holds no NaN point.</summary>
        /// <param name="path">The vertices, in pixels.</param>
        /// <param name="closed">Whether the path closes back to its first vertex.</param>
        /// <returns>The cut path, in a list of its own.</returns>
        internal List<Point> Cut(IReadOnlyList<Point> path, bool closed) => closed ? CutClosed(path) : CutOpen(path);

        private List<Point> CutOpen(IReadOnlyList<Point> path)
        {
            var cut = new List<Point>(path.Count + 2);
            var previous = default(Point);
            bool hasPrevious = false, previousInside = false;
            foreach (var point in path)
            {
                if (!point.IsDrawable())
                {
                    Gap(cut);
                    hasPrevious = false;
                    continue;
                }

                bool inside = Inside(point);
                if (hasPrevious && previousInside != inside) AddCross(cut, previous, point, inside);
                if (inside) cut.Add(point);
                else if (hasPrevious && previousInside) Gap(cut);
                previous = point;
                hasPrevious = true;
                previousInside = inside;
            }

            return cut;
        }

        private List<Point> CutClosed(IReadOnlyList<Point> path)
        {
            var cut = new List<Point>(path.Count + 2);
            for (int i = 0; i < path.Count; i++)
            {
                var point = path[i];
                var previous = path[i == 0 ? path.Count - 1 : i - 1];
                bool inside = Inside(point), previousInside = Inside(previous);
                if (inside != previousInside) AddCross(cut, previous, point, inside);
                if (inside) cut.Add(point);
            }

            return cut;
        }

        /// <summary>The segment from <paramref name="from"/> to <paramref name="to"/> cut at this edge: the part inside, its ends in
        /// the order given, or <see langword="null"/> when no part of it is. A segment that runs straight across the edge is cut on
        /// it right under its end, the pixel a base pinned to the edge stands at.</summary>
        /// <param name="from">One end, in pixels; finite.</param>
        /// <param name="to">The other end, in pixels; finite.</param>
        /// <returns>The part inside, or <see langword="null"/>.</returns>
        internal PixelSegment? Cut(Point from, Point to)
        {
            bool fromInside = Inside(from), toInside = Inside(to);
            if (fromInside && toInside) return new PixelSegment(from, to);
            if (!fromInside && !toInside) return null;
            var cross = Cross(from, to);
            return fromInside ? new PixelSegment(from, cross) : new PixelSegment(cross, to);
        }

        /// <summary>Adds where the segment between <paramref name="previous"/> and <paramref name="point"/> crosses the edge, unless
        /// its end inside lies on the edge itself: that end is where it is cut, and it is added once, as the vertex it is.</summary>
        private void AddCross(List<Point> cut, Point previous, Point point, bool pointInside)
        {
            if (OnEdge(pointInside ? point : previous)) return;
            cut.Add(Cross(previous, point));
        }

        /// <summary>Whether <paramref name="point"/> lies on the edge exactly.</summary>
        private bool OnEdge(Point point) => point.CoordinateAlong(AlongX) == Edge;

        /// <summary>A gap after the last point of a run: one NaN point, never two in a row.</summary>
        private static void Gap(List<Point> cut)
        {
            if (cut.Count > 0 && cut[^1].IsDrawable()) cut.Add(new Point(double.NaN, double.NaN));
        }

        /// <summary>Whether <paramref name="point"/> lies on the plot's side of the edge, or on it.</summary>
        private bool Inside(Point point) => (point.CoordinateAlong(AlongX) - Edge) * PerUnit >= 0;

        /// <summary>Where the segment from <paramref name="from"/> to <paramref name="to"/> crosses the edge: on it exactly, the other
        /// coordinate taken along the segment, so a segment that runs straight across the edge is cut right under its end.</summary>
        private Point Cross(Point from, Point to)
        {
            double t = (Edge - from.CoordinateAlong(AlongX)) / (to.CoordinateAlong(AlongX) - from.CoordinateAlong(AlongX));
            double across = from.CoordinateAcross(AlongX);
            return Point.Along(AlongX, Edge, across + t * (to.CoordinateAcross(AlongX) - across));
        }
    }
}
