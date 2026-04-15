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
    /// Snapshots every series' <see cref="DataRangeContribution"/> into an array, evaluating
    /// <c>ComputeDataRange</c> exactly once per series. Critical for performance: histogram
    /// and KDE series rebuild bins on every call, and the legacy code in
    /// <see cref="CartesianAxesRenderer.ComputeDataRanges"/> called them three times per render
    /// (once for aggregation, once for sticky clamp, once for sticky-flag collection).
    /// </summary>
    public static DataRangeContribution[] SnapshotContributions(this Axes axes)
    {
        var context = new AxesContextAdapter(axes);
        var result = new DataRangeContribution[axes.Series.Count];
        for (int i = 0; i < axes.Series.Count; i++)
            result[i] = axes.Series[i].ComputeDataRange(context);
        return result;
    }

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
