// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders a <see cref="SwarmplotSeries"/> using beeswarm layout for non-overlapping dots per category.</summary>
internal sealed class SwarmplotSeriesRenderer : SeriesRenderer<SwarmplotSeries>
{
    /// <inheritdoc />
    public SwarmplotSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    internal override void AddMarks(SwarmplotSeries series, MarkObstacles.Builder marks)
    {
        foreach (var dot in Dots(series))
            if (dot.Pixel.IsDrawable()) marks.AddPoint(dot.Pixel);
    }

    /// <summary>How far from its category a dot may stand, in categories (seaborn's gutter: <c>0.8 / 2</c> of a category).</summary>
    private const double Gutter = 0.4;

    /// <summary>The pixel of every observation and the group it belongs to: the one geometry <see cref="Render"/> draws and
    /// <see cref="AddMarks"/> hands to a legend. The matplotlib themes place the dots of a group as seaborn's <c>Beeswarm</c> does, in
    /// pixels (<see cref="Rendering.SeriesRenderers.SwarmplotSeriesRenderer.OnPixels"/>); every other theme packs them in data units (<see cref="Rendering.SeriesRenderers.SwarmplotSeriesRenderer.Compute"/>).</summary>
    private IEnumerable<GroupedDot> Dots(SwarmplotSeries series)
    {
        double r = series.DiameterUnder(Context.Theme, Slot) / 2.0;
        return Context.Theme.Looks.Dots.LikeSeaborn ? PackedInPixels(series, r) : PackedInData(series, r);
    }

    private IEnumerable<GroupedDot> PackedInPixels(SwarmplotSeries series, double radius)
    {
        for (int i = 0; i < series.Datasets.Length; i++)
        {
            double[] values = series.Datasets[i];
            double midline = Transform.DataToPixel(i, 0).X;
            double halfWidth = Math.Abs(Transform.DataToPixel(i + Gutter, 0).X - midline);

            // matplotlib's pixels count upwards and the picture's downwards.
            var heights = new double[values.Length];
            for (int j = 0; j < values.Length; j++)
                heights[j] = -Transform.DataToPixel(i, values[j]).Y;

            double[] x = OnPixels(midline, heights, radius, halfWidth);
            for (int j = 0; j < values.Length; j++)
                yield return new GroupedDot(new(x[j], -heights[j]), i);
        }
    }

    private IEnumerable<GroupedDot> PackedInData(SwarmplotSeries series, double r)
    {
        // Estimate markerRadius in data units from pixel radius
        double dataPerPixelX = Math.Abs(Transform.DataXMax - Transform.DataXMin) /
                               Math.Max(1, Area.PlotBounds.Width);
        double markerRadiusData = r * dataPerPixelX;

        for (int i = 0; i < series.Datasets.Length; i++)
        {
            var sorted = series.Datasets[i].ToArray();
            Array.Sort(sorted);
            double[] xPositions = Compute(sorted, markerRadiusData, i);

            for (int j = 0; j < sorted.Length; j++)
                yield return new GroupedDot(Transform.DataToPixel(xPositions[j], sorted[j]), i);
        }
    }

    /// <inheritdoc />
    public override void Render(SwarmplotSeries series)
    {
        double r = series.DiameterUnder(Context.Theme, Slot) / 2.0;
        int group = -1;
        double alpha = series.OpacityUnder(Context.Theme);
        var style = default(ShapeStyle);
        foreach (var dot in Dots(series))
        {
            // The dots come group by group, so a group's style is made once.
            if (dot.Group != group) { group = dot.Group; style = ShapeStyle.Filled(GroupColor(series.Color, group).WithOpacityGiven(alpha)); }
            Ctx.DrawCircle(dot.Pixel, r, style);
        }
    }

    /// <summary>The number of dots a group may hold before the layout stops packing and spreads them by a seeded jitter.</summary>
    private const int PackingLimit = 1000;

    /// <summary>The most candidates for one dot that are ordered by an insertion sort, on the stack; more are ordered by LINQ.</summary>
    private const int InsertionSortLimit = 32;

    /// <summary>Computes X offsets for a set of values centered at a category position.</summary>
    /// <param name="sortedValues">Data values sorted in ascending order (one per point).</param>
    /// <param name="markerRadius">Radius of each dot in data units.</param>
    /// <param name="categoryCenter">The X position of the category axis tick.</param>
    /// <returns>An array of X positions (one per input value), non-overlapping.</returns>
    internal static double[] Compute(double[] sortedValues, double markerRadius, double categoryCenter)
    {
        int n = sortedValues.Length;
        if (n == 0) return [];
        if (n == 1) return [categoryCenter];

        // For large datasets fall back to deterministic random jitter
        if (n > PackingLimit) return Scattered(n, categoryCenter, markerRadius * 3);

        double diameter = markerRadius * 2;
        var placed = new List<Point>(n);
        var result = new double[n];

        for (int i = 0; i < n; i++)
        {
            double y = sortedValues[i];
            double bestOffset = 0;

            // Try offsets 0, ±diameter, ±2*diameter, … until no overlap
            bool placed2 = false;
            for (int step = 0; step <= n; step++)
            {
                double[] candidates = step == 0 ? [categoryCenter] :
                    [categoryCenter + step * diameter, categoryCenter - step * diameter];

                foreach (double cx in candidates)
                {
                    bool overlaps = false;
                    foreach (var p in placed)
                    {
                        if (p.DistanceSquaredTo(new Point(cx, y)) < diameter * diameter)
                        {
                            overlaps = true;
                            break;
                        }
                    }
                    if (!overlaps)
                    {
                        bestOffset = cx;
                        placed2 = true;
                        break;
                    }
                }
                if (placed2) break;
            }

            placed.Add(new(bestOffset, y));
            result[i] = bestOffset;
        }

        return result;
    }

    /// <summary>Places the dots of one group as seaborn's <c>Beeswarm</c> does: in pixels, lowest dot first, each dot at the position
    /// closest to the midline that touches none of the dots already placed within a diameter of it, and no further from the midline than
    /// the gutter.</summary>
    /// <remarks>The candidates for a dot are its own position (on the midline) and, beside every neighbour that could overlap it, the two
    /// positions where it touches that neighbour (a gap of 5 % more than touching), the left one first for the first neighbour, the right
    /// one first for the second, and so on. They are tried in order of their distance from the midline, a tie in the order they were
    /// listed. A dot that fits at none of them takes the last (seaborn raises an error where this is unreachable). A group of more than
    /// 1,000 dots is spread by a seeded jitter instead, as <see cref="Rendering.SeriesRenderers.SwarmplotSeriesRenderer.Compute"/> does.</remarks>
    /// <param name="midline">The pixel position of the group's centre across the group axis.</param>
    /// <param name="along">The position of every dot along the value axis, in pixels, a higher value standing higher on the plot (matplotlib's
    /// direction, so a plot whose pixels count downwards passes the negative); a dot with no finite value is left out and answered by NaN.</param>
    /// <param name="radius">The radius of every dot, in pixels.</param>
    /// <param name="halfWidth">How far from the midline a dot may stand, in pixels.</param>
    /// <returns>The pixel position of every dot across the group axis, in the order of <paramref name="along"/>.</returns>
    internal static double[] OnPixels(double midline, double[] along, double radius, double halfWidth)
    {
        var result = new double[along.Length];
        Array.Fill(result, double.NaN);
        int[] order = [.. Enumerable.Range(0, along.Length).Where(i => double.IsFinite(along[i]))];
        if (order.Length > PackingLimit)
        {
            double[] scattered = Scattered(order.Length, midline, radius * 3);
            for (int k = 0; k < order.Length; k++) result[order[k]] = Math.Clamp(scattered[k], midline - halfWidth, midline + halfWidth);
            return result;
        }

        // seaborn sorts with np.argsort, whose order for equal heights is not defined; a stable sort is the one answer.
        order = [.. order.OrderBy(i => along[i])];
        double diameter = radius * 2;
        var swarmX = new List<double>(order.Length);
        var swarmY = new List<double>(order.Length);
        foreach (int dot in order)
        {
            double y = along[dot];
            double x = swarmX.Count == 0 ? midline : FirstFitting(midline, y, diameter, swarmX, swarmY);
            swarmX.Add(x);
            swarmY.Add(y);
            result[dot] = Math.Clamp(x, midline - halfWidth, midline + halfWidth);
        }

        return result;
    }

    /// <summary>The position of the next dot of a swarm: the first of its candidates, nearest the midline first, that touches no neighbour.</summary>
    private static double FirstFitting(double midline, double y, double diameter, List<double> swarmX, List<double> swarmY)
    {
        // The swarm's dots within a diameter below the new one, found from the top of the swarm down, kept in swarm order.
        int first = swarmX.Count;
        while (first > 0 && y - swarmY[first - 1] < diameter) first--;

        int count = swarmX.Count - first;
        int needed = 1 + 2 * count;
        // A swarm's candidates are a handful: they and their order live on the stack up to InsertionSortLimit, where a dot's two arrays
        // were a heap allocation each for every dot of the group.
        Span<double> candidates = needed <= InsertionSortLimit ? stackalloc double[InsertionSortLimit] : new double[needed];
        candidates = candidates[..needed];
        candidates[0] = midline;
        bool leftFirst = true;
        for (int k = 0; k < count; k++)
        {
            double dy = y - swarmY[first + k];
            double dx = Math.Sqrt(Math.Max(diameter * diameter - dy * dy, 0)) * 1.05;
            double left = swarmX[first + k] - dx, right = swarmX[first + k] + dx;
            candidates[1 + 2 * k] = leftFirst ? left : right;
            candidates[2 + 2 * k] = leftFirst ? right : left;
            leftFirst = !leftFirst;
        }

        // Nearest the midline first; a tie keeps the order the candidates were listed in (an insertion sort is stable; a crowd of equal
        // heights, a thousand dots at most, is sorted by LINQ's stable OrderBy).
        Span<int> byDistance = needed <= InsertionSortLimit ? stackalloc int[InsertionSortLimit] : new int[needed];
        byDistance = byDistance[..needed];
        for (int c = 0; c < needed; c++) byDistance[c] = c;
        if (needed > InsertionSortLimit)
        {
            double[] kept = candidates.ToArray();
            int[] ordered = [.. Enumerable.Range(0, needed).OrderBy(c => Math.Abs(kept[c] - midline))];
            ordered.CopyTo(byDistance);
        }
        else
        {
            for (int c = 1; c < needed; c++)
            {
                int moving = byDistance[c];
                double distance = Math.Abs(candidates[moving] - midline);
                int at = c - 1;
                while (at >= 0 && Math.Abs(candidates[byDistance[at]] - midline) > distance)
                {
                    byDistance[at + 1] = byDistance[at];
                    at--;
                }

                byDistance[at + 1] = moving;
            }
        }

        int pick = 0;
        while (pick < byDistance.Length - 1 && !Fits(candidates[byDistance[pick]], y, diameter, swarmX, swarmY, first)) pick++;
        return candidates[byDistance[pick]];
    }

    /// <summary>Whether a dot at (<paramref name="x"/>, <paramref name="y"/>) keeps a diameter from every swarm dot from <paramref name="first"/> on.</summary>
    private static bool Fits(double x, double y, double diameter, List<double> swarmX, List<double> swarmY, int first)
    {
        for (int k = first; k < swarmX.Count; k++)
        {
            if (new Point(swarmX[k], swarmY[k]).DistanceSquaredTo(new Point(x, y)) < diameter * diameter) return false;
        }

        return true;
    }

    /// <summary>The positions of <paramref name="count"/> dots spread by a seeded jitter of at most <paramref name="spread"/> around
    /// <paramref name="centre"/>.</summary>
    private static double[] Scattered(int count, double centre, double spread)
    {
        var rng = new Random(count);
        var scattered = new double[count];
        for (int i = 0; i < count; i++)
            scattered[i] = centre + (rng.NextDouble() * 2 - 1) * spread;
        return scattered;
    }
}
