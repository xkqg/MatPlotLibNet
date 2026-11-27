// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering;

/// <summary>The marks a chart has drawn on one axes, as what a legend or an annotation that moves out of the way must keep
/// clear of: the rectangles it filled (a bar, a text's box), the paths it stroked (a line's vertices in drawing order) and
/// the points it stamped (a scatter's centres). They are the pixel positions the marks have as drawn, handed over by the
/// renderers that draw them, so nothing is guessed from the data. Obstacles do not move; the thing that avoids them does.</summary>
/// <remarks>A default value holds nothing, as <see cref="None"/> does. The marks are held as arrays, so scoring a candidate
/// walks them without a call or an allocation per mark (<see cref="Badness"/>). A path whose X never falls (the line of a series,
/// a step line) is marked so when the marks are made, and a candidate looks only at the run of its vertices whose X reaches the
/// candidate's (<see cref="MarkObstacleExtensions"/>' <c>RunAlongX</c>): ten candidates over a 200-point line read a handful of
/// vertices each instead of all of them, with the same score.</remarks>
internal readonly struct MarkObstacles
{
    private readonly Rect[]? _boxes;
    private readonly Point[][]? _paths;
    private readonly Point[]? _points;

    /// <summary>For each path, whether its X is finite and never falls, so the vertices a box can reach stand in one run.</summary>
    private readonly bool[]? _alongX;

    private MarkObstacles(Rect[] boxes, Point[][] paths, Point[] points) : this(boxes, paths, points, AlongXOf(paths)) { }

    private MarkObstacles(Rect[] boxes, Point[][] paths, Point[] points, bool[] alongX)
    {
        _boxes = boxes;
        _paths = paths;
        _points = points;
        _alongX = alongX;
    }

    /// <summary>Whether each of <paramref name="paths"/> runs along X: every X finite and none below the one before it.</summary>
    private static bool[] AlongXOf(Point[][] paths)
    {
        var along = new bool[paths.Length];
        for (int p = 0; p < paths.Length; p++)
        {
            var path = paths[p];
            bool sorted = true;
            for (int i = 0; i < path.Length && sorted; i++)
                sorted = double.IsFinite(path[i].X) && (i == 0 || path[i].X >= path[i - 1].X);
            along[p] = sorted;
        }
        return along;
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
    internal MarkObstacles Including(Rect box) => new([.. _boxes.AsSpan(), box], _paths ?? [], _points ?? [], _alongX ?? []);

    /// <summary>How badly a candidate box stands on the marks, by matplotlib's rule for its <c>loc='best'</c> legend: the
    /// rectangles that overlap it, plus the point centres strictly inside it, plus, for each path, the vertices strictly
    /// inside it and one more when the path touches it. Zero is a box that covers nothing. Four integer terms, so the pick is
    /// the same on every platform (only comparisons, sums and products of the pixel values, no transcendental function).</summary>
    /// <param name="candidate">The candidate box, in pixels.</param>
    /// <param name="stopAt">Where the count may stop: a score that reaches it is returned as this value, since a candidate
    /// that scores this much has already lost (<see cref="FirstLeastCovered"/> passes the best score so far). Left out, the
    /// score is counted in full.</param>
    /// <returns>The score, or <paramref name="stopAt"/> when the score is at least that; lower is better.</returns>
    internal int Badness(Rect candidate, int stopAt = int.MaxValue)
    {
        int badness = 0;
        foreach (var box in _boxes.AsSpan())
            if (candidate.Overlaps(box) && ++badness >= stopAt) return stopAt;

        badness += candidate.CountInside(_points, stopAt - badness);
        if (badness >= stopAt) return stopAt;

        var paths = _paths.AsSpan();
        for (int p = 0; p < paths.Length; p++)
        {
            var path = paths[p];
            var run = _alongX![p] ? candidate.RunAlongX(path) : new VertexRun(0, path.Length);
            if (candidate.Touches(path, run) && ++badness >= stopAt) return stopAt;
            badness += candidate.CountInside(path.AsSpan(run.From, run.To - run.From), stopAt - badness);
            if (badness >= stopAt) return stopAt;
        }
        return badness;
    }

    /// <summary>The index of the first candidate with the lowest score: the search stops at the first one that covers
    /// nothing, and a later candidate must score strictly lower than the best so far to replace it, so the earlier of
    /// two equals wins, which is the order the candidates are given in. A later candidate is scored only until it reaches the
    /// best score so far (<see cref="Badness"/>), which it could not beat, so the pick is the one the full scores make.</summary>
    /// <param name="candidates">The candidate boxes, in the order to try them.</param>
    /// <returns>The index of the chosen candidate; 0 when there are no candidates.</returns>
    internal int FirstLeastCovered(IReadOnlyList<Rect> candidates)
    {
        if (candidates.Count == 0) return 0;
        int best = 0, bestScore = Badness(candidates[0]);
        for (int i = 1; i < candidates.Count && bestScore > 0; i++)
        {
            int score = Badness(candidates[i], bestScore);
            if (score < bestScore)
            {
                best = i;
                bestScore = score;
            }
        }
        return best;
    }

    /// <summary>Collects the marks one renderer pass draws.</summary>
    internal sealed class Builder
    {
        private readonly List<Rect> _boxes = [];
        private readonly List<Point[]> _paths = [];
        private readonly List<Point> _points = [];

        /// <summary>A builder that holds no marks yet.</summary>
        internal Builder() { }

        /// <summary>A builder that starts with the marks of <paramref name="from"/>, in their order, so marks collected once can
        /// be added to for one reader without changing them for another; <paramref name="from"/> is left as it was.</summary>
        /// <param name="from">The marks to start with.</param>
        internal Builder(MarkObstacles from)
        {
            _boxes.AddRange(from._boxes ?? []);
            _paths.AddRange(from._paths ?? []);
            _points.AddRange(from._points ?? []);
        }

        /// <summary>Adds a rectangle the marks filled.</summary>
        /// <param name="box">The rectangle, in pixels.</param>
        /// <returns>This builder.</returns>
        internal Builder AddBox(Rect box)
        {
            _boxes.Add(box);
            return this;
        }

        /// <summary>Adds a path the marks stroked. An array is held as it is given, as the caller's list always was; any other
        /// list is copied into one.</summary>
        /// <param name="vertices">The path's vertices in drawing order, in pixels.</param>
        /// <returns>This builder.</returns>
        internal Builder AddPath(IReadOnlyList<Point> vertices)
        {
            _paths.Add(vertices as Point[] ?? [.. vertices]);
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

/// <summary>The geometry a candidate box is scored by against <see cref="MarkObstacles"/> (<see cref="MarkObstacles.Badness"/>):
/// whether a point stands inside it, a rectangle overlaps it or a path touches it.</summary>
internal static class MarkObstacleExtensions
{
    extension(Rect box)
    {
        /// <summary>How many of <paramref name="points"/> lie strictly inside the box; a point on its edge, or one the axes
        /// could not place (not finite), is not inside.</summary>
        /// <param name="points">The points to test.</param>
        /// <param name="atMost">Where the count may stop: once it reaches this, the rest is not looked at. Left out, every
        /// point is counted.</param>
        /// <returns>The count, at most <paramref name="atMost"/>.</returns>
        internal int CountInside(ReadOnlySpan<Point> points, int atMost = int.MaxValue)
        {
            double left = box.X, right = box.Right, top = box.Y, bottom = box.Bottom;
            int inside = 0;
            foreach (var point in points)
                if (left < point.X && point.X < right && top < point.Y && point.Y < bottom && ++inside >= atMost) break;
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

        /// <summary>Whether a path touches the box: its first vertex lies in the box (an edge counts), or a segment crosses it, its
        /// segments looked at only around <paramref name="run"/>: the segments with a vertex in the run and the one that leads into it,
        /// which are every segment that can reach the box when the run holds every vertex whose X can (<see cref="RunAlongX"/>). The
        /// first vertex is the path's own. The segment test is matplotlib's (a separating-axis test of the segment against the box, in
        /// the strict form its <c>_path.h</c> carries), so a segment that ends exactly on the box's edge without entering does not
        /// touch it; a segment with an end the axes could not place is skipped.</summary>
        /// <param name="path">The path's vertices in drawing order.</param>
        /// <param name="run">The vertices to look around; the whole path for a path that does not run along X.</param>
        internal bool Touches(ReadOnlySpan<Point> path, VertexRun run)
        {
            if (path.IsEmpty) return false;
            var first = path[0];
            if (box.X <= first.X && first.X <= box.Right && box.Y <= first.Y && first.Y <= box.Bottom) return true;

            double cx = box.Center.X, cy = box.Center.Y, w = box.Width, h = box.Height;
            int last = Math.Min(run.To, path.Length - 1);
            for (int i = Math.Max(0, run.From - 1); i < last; i++)
            {
                double x1 = path[i].X, y1 = path[i].Y, x2 = path[i + 1].X, y2 = path[i + 1].Y;
                if (Math.Abs(x1 + x2 - 2 * cx) < Math.Abs(x1 - x2) + w
                    && Math.Abs(y1 + y2 - 2 * cy) < Math.Abs(y1 - y2) + h
                    && 2 * Math.Abs((x1 - cx) * (y1 - y2) - (y1 - cy) * (x1 - x2)) < w * Math.Abs(y1 - y2) + h * Math.Abs(x1 - x2))
                    return true;
            }
            return false;
        }

        /// <summary>The run of a path's vertices whose X can reach the box, for a path whose X is finite and never falls: every
        /// vertex with an X within <see cref="AlongXSlack"/> of the box's left and right edges or between them, found by halving.
        /// A vertex outside it is not inside the box, and a segment with neither end in it, other than the one that leads into it,
        /// lies wholly left or right of the box, so the score over the run is the score over the path.</summary>
        /// <param name="path">The path's vertices, their X finite and never falling.</param>
        /// <returns>The vertices <c>[From, To)</c>.</returns>
        internal VertexRun RunAlongX(ReadOnlySpan<Point> path) =>
            new(FirstAbove(path, box.X - AlongXSlack), FirstAbove(path, box.Right + AlongXSlack));
    }

    /// <summary>How far past a box's left and right edges, in pixels, a vertex still counts as one whose X can reach it: far more than
    /// the rounding of the segment test's sums, so no segment that test could let through is left out of a run.</summary>
    private const double AlongXSlack = 1e-6;

    /// <summary>The index of the first vertex of a path whose X never falls that stands past <paramref name="x"/>; the path's
    /// length when none does.</summary>
    private static int FirstAbove(ReadOnlySpan<Point> path, double x)
    {
        int low = 0, high = path.Length;
        while (low < high)
        {
            int middle = (low + high) >>> 1;
            if (path[middle].X > x) high = middle;
            else low = middle + 1;
        }
        return low;
    }
}

/// <summary>A run of a path's vertices, <c>[From, To)</c>, that a candidate box is scored over
/// (<see cref="MarkObstacleExtensions"/>' <c>RunAlongX</c>).</summary>
/// <param name="From">The first vertex of the run.</param>
/// <param name="To">The vertex after the last.</param>
internal readonly record struct VertexRun(int From, int To);
