// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering;

/// <summary>The marks a chart has drawn on one axes, as what a legend or an annotation that moves out of the way must keep
/// clear of: the rectangles it filled (a bar, a text's box), the paths it stroked (a line's vertices in drawing order) and
/// the points it stamped (a scatter's centres). They are the pixel positions the marks have as drawn, handed over by the
/// renderers that draw them, so nothing is guessed from the data. Obstacles do not move; the thing that avoids them does.</summary>
/// <remarks>A default value holds nothing, as <see cref="None"/> does.</remarks>
internal readonly struct MarkObstacles
{
    private readonly IReadOnlyList<Rect>? _boxes;
    private readonly IReadOnlyList<IReadOnlyList<Point>>? _paths;
    private readonly IReadOnlyList<Point>? _points;

    private MarkObstacles(IReadOnlyList<Rect> boxes, IReadOnlyList<IReadOnlyList<Point>> paths, IReadOnlyList<Point> points)
    {
        _boxes = boxes;
        _paths = paths;
        _points = points;
    }

    /// <summary>No marks: a candidate is covered by nothing.</summary>
    internal static MarkObstacles None => default;

    /// <summary>The rectangles the marks filled.</summary>
    internal IReadOnlyList<Rect> Boxes => _boxes ?? [];

    /// <summary>The paths the marks stroked, each as its vertices in drawing order.</summary>
    internal IReadOnlyList<IReadOnlyList<Point>> Paths => _paths ?? [];

    /// <summary>The centres of the points the marks stamped.</summary>
    internal IReadOnlyList<Point> Points => _points ?? [];

    /// <summary>These marks with one more rectangle among them, as an annotation's text box stands among them.</summary>
    /// <param name="box">The rectangle to add.</param>
    /// <returns>A new value; this one is unchanged.</returns>
    internal MarkObstacles Including(Rect box) => new([.. Boxes, box], Paths, Points);

    /// <summary>Collects the marks one renderer pass draws.</summary>
    internal sealed class Builder
    {
        private readonly List<Rect> _boxes = [];
        private readonly List<IReadOnlyList<Point>> _paths = [];
        private readonly List<Point> _points = [];

        /// <summary>Adds a rectangle the marks filled.</summary>
        /// <param name="box">The rectangle, in pixels.</param>
        /// <returns>This builder.</returns>
        internal Builder AddBox(Rect box)
        {
            _boxes.Add(box);
            return this;
        }

        /// <summary>Adds a path the marks stroked.</summary>
        /// <param name="vertices">The path's vertices in drawing order, in pixels.</param>
        /// <returns>This builder.</returns>
        internal Builder AddPath(IReadOnlyList<Point> vertices)
        {
            _paths.Add(vertices);
            return this;
        }

        /// <summary>Adds the centre of a point the marks stamped.</summary>
        /// <param name="centre">The centre, in pixels.</param>
        /// <returns>This builder.</returns>
        internal Builder AddPoint(Point centre)
        {
            _points.Add(centre);
            return this;
        }

        /// <summary>The marks collected so far.</summary>
        /// <returns>An immutable value.</returns>
        internal MarkObstacles Build() => new([.. _boxes], [.. _paths], [.. _points]);
    }
}

/// <summary>How a candidate box is scored against <see cref="MarkObstacles"/>: matplotlib's rule for its <c>loc='best'</c>
/// legend, four integer terms, so the pick is the same on every platform (only comparisons, sums and products of the
/// pixel values, no transcendental function).</summary>
internal static class MarkObstacleExtensions
{
    extension(Rect box)
    {
        /// <summary>How many of <paramref name="points"/> lie strictly inside the box; a point on its edge, or one the axes
        /// could not place (not finite), is not inside.</summary>
        /// <param name="points">The points to test.</param>
        /// <returns>The count.</returns>
        internal int CountInside(IReadOnlyList<Point> points)
        {
            int inside = 0;
            foreach (var point in points)
                if (box.X < point.X && point.X < box.Right && box.Y < point.Y && point.Y < box.Bottom) inside++;
            return inside;
        }

        /// <summary>Whether <paramref name="inner"/> lies wholly inside the box; its edges may touch the box's.</summary>
        /// <param name="inner">The rectangle to test.</param>
        /// <returns><see langword="true"/> when it is inside.</returns>
        internal bool Contains(Rect inner) =>
            box.X <= inner.X && inner.Right <= box.Right && box.Y <= inner.Y && inner.Bottom <= box.Bottom;

        /// <summary>Whether the box shares area with <paramref name="other"/>: rectangles that only touch along an edge or at
        /// a corner do not overlap.</summary>
        /// <param name="other">The rectangle to test.</param>
        /// <returns><see langword="true"/> when they overlap.</returns>
        internal bool Overlaps(Rect other) => box.Intersects(other);

        /// <summary>Whether a path touches the box: its first vertex lies in the box (an edge counts), or any of its
        /// segments crosses it. The segment test is matplotlib's (a separating-axis test of the segment against the box, in
        /// the strict form its <c>_path.h</c> carries), so a segment that ends exactly on the box's edge without entering does
        /// not touch it. A segment with an end the axes could not place is skipped.</summary>
        /// <param name="path">The path's vertices in drawing order.</param>
        /// <returns><see langword="true"/> when the path touches the box.</returns>
        internal bool Touches(IReadOnlyList<Point> path)
        {
            if (path.Count == 0) return false;
            var first = path[0];
            if (box.X <= first.X && first.X <= box.Right && box.Y <= first.Y && first.Y <= box.Bottom) return true;

            double cx = box.X + box.Width / 2, cy = box.Y + box.Height / 2, w = box.Width, h = box.Height;
            for (int i = 0; i + 1 < path.Count; i++)
            {
                double x1 = path[i].X, y1 = path[i].Y, x2 = path[i + 1].X, y2 = path[i + 1].Y;
                if (Math.Abs(x1 + x2 - 2 * cx) < Math.Abs(x1 - x2) + w
                    && Math.Abs(y1 + y2 - 2 * cy) < Math.Abs(y1 - y2) + h
                    && 2 * Math.Abs((x1 - cx) * (y1 - y2) - (y1 - cy) * (x1 - x2)) < w * Math.Abs(y1 - y2) + h * Math.Abs(x1 - x2))
                    return true;
            }
            return false;
        }
    }

    extension(MarkObstacles marks)
    {
        /// <summary>How badly a candidate box stands on the marks: the path vertices strictly inside it, plus the point centres
        /// strictly inside it, plus the rectangles that overlap it, plus one for each path that touches it. Zero is a box
        /// that covers nothing.</summary>
        /// <param name="candidate">The candidate box, in pixels.</param>
        /// <returns>The score; lower is better.</returns>
        internal int Badness(Rect candidate)
        {
            int badness = candidate.CountInside(marks.Points);
            foreach (var path in marks.Paths)
            {
                badness += candidate.CountInside(path);
                if (candidate.Touches(path)) badness++;
            }
            foreach (var box in marks.Boxes)
                if (candidate.Overlaps(box)) badness++;
            return badness;
        }

        /// <summary>The index of the first candidate with the lowest score: the search stops at the first one that covers
        /// nothing, and a later candidate must score strictly lower than the best so far to replace it, so the earlier of
        /// two equals wins, which is the order the candidates are given in.</summary>
        /// <param name="candidates">The candidate boxes, in the order to try them.</param>
        /// <returns>The index of the chosen candidate; 0 when there are no candidates.</returns>
        internal int FirstLeastCovered(IReadOnlyList<Rect> candidates)
        {
            if (candidates.Count == 0) return 0;
            int best = 0, bestScore = marks.Badness(candidates[0]);
            for (int i = 1; i < candidates.Count && bestScore > 0; i++)
            {
                int score = marks.Badness(candidates[i]);
                if (score < bestScore)
                {
                    best = i;
                    bestScore = score;
                }
            }
            return best;
        }
    }
}
