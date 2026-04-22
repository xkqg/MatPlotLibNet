// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering;

/// <summary>
/// Painter's-algorithm depth sink shared by all 3-D series renderers on a single 3-D axes.
/// Each renderer pushes closures with a centroid depth; <see cref="Flush"/> sorts ascending
/// (lowest depth = farthest from camera) and invokes them in order so front primitives
/// paint over back ones — across series, not just within a single series.
/// </summary>
/// <remarks>
/// Without this sink each <see cref="SeriesRenderers.Bar3DSeriesRenderer"/> sorts only its
/// own faces and draws them immediately; a later series then paints over earlier ones
/// regardless of world-space depth. matplotlib has the same limitation for repeated
/// <c>ax.bar3d()</c> calls, requiring the user to add rows back-to-front manually.
/// The shared queue lifts that restriction so users can add series in any order.
/// </remarks>
public sealed class DepthQueue3D
{
    private readonly record struct DepthItem(double Depth, Action Draw);
    private readonly List<DepthItem> _items = new();

    /// <summary>Queues a drawing action to be executed during <see cref="Flush"/>.</summary>
    /// <param name="depth">Centroid depth of the primitive; lower = farther from camera.</param>
    /// <param name="draw">Closure that performs the actual draw when flushed.</param>
    public void Add(double depth, Action draw) => _items.Add(new(depth, draw));

    /// <summary>Sorts queued items ascending by depth and invokes each draw closure.</summary>
    public void Flush()
    {
        _items.Sort(static (a, b) => a.Depth.CompareTo(b.Depth));
        foreach (var item in _items) item.Draw();
        _items.Clear();
    }
}
