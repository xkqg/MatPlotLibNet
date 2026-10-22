// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Rendering;

/// <summary>
/// Extension methods on <see cref="Axes"/> that support the data-range pipeline consumed by
/// <see cref="CartesianAxesRenderer.ComputeDataRanges"/>. Kept as extensions rather than
/// static helpers on the renderer so the operations can be reused from other renderers
/// (<see cref="PolarAxesRenderer"/>, <see cref="ThreeDAxesRenderer"/>) without duplication.
/// </summary>
internal static class AxesRangeExtensions
{
    /// <summary>
    /// Snapshots what takes part in the automatic range of the axes into an array: every series'
    /// <see cref="DataRangeContribution"/>, evaluating <c>ComputeDataRange</c> exactly once per series, then one
    /// contribution for each line drawn across the plot at a data value (<see cref="LineContributions"/>). Critical for
    /// performance: histogram and KDE series rebuild bins on every call, and the legacy code in
    /// <see cref="CartesianAxesRenderer.ComputeDataRanges"/> called them three times per render
    /// (once for aggregation, once for sticky clamp, once for sticky-flag collection).
    /// </summary>
    public static DataRangeContribution[] SnapshotContributions(this Axes axes)
    {
        var context = new AxesContextAdapter(axes);
        var result = new List<DataRangeContribution>(axes.Series.Count + axes.ReferenceLines.Count);
        for (int i = 0; i < axes.Series.Count; i++)
            result.Add(axes.Series[i].ComputeDataRange(context));
        result.AddRange(axes.LineContributions());
        return [.. result];
    }

    /// <summary>The lines drawn across the plot at a data value, each as a data value there on its own axis: a reference
    /// line (<see cref="Axes.AxHLine"/> on the Y axis, <see cref="Axes.AxVLine"/> on the X axis), a horizontal level and the
    /// levels of a Fibonacci retracement. matplotlib's <c>axhline</c> and <c>axvline</c> update the data limits along their
    /// own axis, so the automatic range shows a line beyond the data instead of drawing it off the plot. A value the axis
    /// cannot place (a missing value, an infinity, 0 or less on a log axis) adds nothing, the rule a series' values follow.
    /// A line carries no sticky edge.</summary>
    /// <param name="axes">The axes whose lines are taken in.</param>
    /// <returns>One contribution per line; an empty one for a line at a value its axis cannot place.</returns>
    private static IEnumerable<DataRangeContribution> LineContributions(this Axes axes)
    {
        foreach (var line in axes.ReferenceLines)
        {
            yield return line.Orientation == Orientation.Horizontal ? axes.AtHeight(line.Value) : axes.AtPlace(line.Value);
        }

        foreach (var level in axes.HorizontalLevels)
        {
            yield return axes.AtHeight(level.Value);
        }

        foreach (var retracement in axes.FibonacciRetracements)
        {
            foreach (var level in retracement.Levels)
            {
                yield return axes.AtHeight(level.Price);
            }
        }
    }

    /// <summary>A data value <paramref name="y"/> on the Y axis, when the Y axis can place it.</summary>
    private static DataRangeContribution AtHeight(this Axes axes, double y) =>
        Range1D.Empty.Including(y, axes.YAxis.Scale).OrNone() is { } r ? new(null, null, r.Lo, r.Hi) : default;

    /// <summary>A data value <paramref name="x"/> on the X axis, when the X axis can place it.</summary>
    private static DataRangeContribution AtPlace(this Axes axes, double x) =>
        Range1D.Empty.Including(x, axes.XAxis.Scale).OrNone() is { } r ? new(r.Lo, r.Hi, null, null) : default;

    /// <summary>Folds every contribution's X range into <paramref name="seed"/>.</summary>
    public static Range1D FoldXRange(this IReadOnlyList<DataRangeContribution> contribs, Range1D seed)
    {
        var running = seed;
        foreach (var c in contribs)
            running = running.Merge(c.XMin, c.XMax);
        return running;
    }

    /// <summary>Folds every contribution's Y range into <paramref name="seed"/>.</summary>
    public static Range1D FoldYRange(this IReadOnlyList<DataRangeContribution> contribs, Range1D seed)
    {
        var running = seed;
        foreach (var c in contribs)
            running = running.Merge(c.YMin, c.YMax);
        return running;
    }

    /// <summary>
    /// Aggregates the X range from this axes' own series AND every linked axes in its
    /// <see cref="Axes.ShareXWith"/> chain. Cycle-safe via a visited set — a reciprocal
    /// <c>ax1.ShareXWith = ax2; ax2.ShareXWith = ax1</c> pair will not infinite-loop.
    /// </summary>
    public static Range1D AggregateXRangeWithSharedAxes(this Axes axes, Range1D seed)
    {
        var running = axes.SnapshotContributions().FoldXRange(seed);
        if (axes.ShareXWith is null) return running;

        var visited = new HashSet<Axes> { axes };
        for (var cur = axes.ShareXWith; cur is not null && visited.Add(cur); cur = cur.ShareXWith)
            running = cur.SnapshotContributions().FoldXRange(running);
        return running;
    }

    /// <summary>
    /// Aggregates the Y range from this axes' own series AND every linked axes in its
    /// <see cref="Axes.ShareYWith"/> chain. Cycle-safe.
    /// </summary>
    public static Range1D AggregateYRangeWithSharedAxes(this Axes axes, Range1D seed)
    {
        var running = axes.SnapshotContributions().FoldYRange(seed);
        if (axes.ShareYWith is null) return running;

        var visited = new HashSet<Axes> { axes };
        for (var cur = axes.ShareYWith; cur is not null && visited.Add(cur); cur = cur.ShareYWith)
            running = cur.SnapshotContributions().FoldYRange(running);
        return running;
    }

    /// <summary>True when any contribution registers a sticky edge on the X axis.</summary>
    public static bool HasAnyStickyX(this IReadOnlyList<DataRangeContribution> contribs)
    {
        foreach (var c in contribs)
            if (c.StickyXMin.HasValue || c.StickyXMax.HasValue) return true;
        return false;
    }

    /// <summary>True when any contribution registers a sticky edge on the Y axis.</summary>
    public static bool HasAnyStickyY(this IReadOnlyList<DataRangeContribution> contribs)
    {
        foreach (var c in contribs)
            if (c.StickyYMin.HasValue || c.StickyYMax.HasValue) return true;
        return false;
    }
}
