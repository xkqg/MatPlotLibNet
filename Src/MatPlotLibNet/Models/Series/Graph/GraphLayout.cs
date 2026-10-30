// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models.Series;

/// <summary>Layout algorithm selector for <see cref="NetworkGraphSeries"/>.</summary>
/// <remarks>Ordinals are explicit and append-only, so a layout written to JSON keeps reading back as
/// the same layout. <see cref="NetworkGraphSeries.Layout"/> defaults to <see cref="Circular"/>.</remarks>
public enum GraphLayout
{
    /// <summary>Use the <see cref="GraphNode.X"/> / <see cref="GraphNode.Y"/> coordinates
    /// supplied on each node verbatim — no automatic placement.</summary>
    Manual = 0,

    /// <summary>Fruchterman–Reingold spring-embedder layout: every pair of nodes repels, every edge
    /// pulls its two nodes together, and the moves cool down over
    /// <see cref="NetworkGraphSeries.LayoutIterations"/> iterations (or stop earlier at
    /// <see cref="NetworkGraphSeries.ConvergenceThreshold"/>). The starting positions come from a
    /// seeded random generator (<see cref="NetworkGraphSeries.LayoutSeed"/>), so the same graph and
    /// seed give the same picture. The nodes stay inside [-1, 1] in both axes; edges are treated as
    /// undirected, and the node coordinates you supplied are ignored.</summary>
    ForceDirected = 1,

    /// <summary>Place every node on the unit circle at evenly-spaced angles.
    /// O(N), deterministic, ignores edges and input <see cref="GraphNode.X"/>/<see cref="GraphNode.Y"/>.</summary>
    Circular = 2,

    /// <summary>BFS top-down layering from the first node: depth = Y, within-depth order = X.
    /// Disconnected components default to depth 0. Cycles are tolerated.</summary>
    Hierarchical = 3,

    /// <summary>An arc diagram: the nodes in a row along the X axis, node <c>i</c> at <c>(i, 0)</c> in the order the nodes are
    /// stored, and every edge drawn as a semicircle above the row from one of its nodes to the other (its two cubic Bézier
    /// curves in data space, so the arc is a half circle when the plot has an aspect of one, <c>SetAspect(1)</c>, and a half
    /// ellipse otherwise). An edge between two nodes <c>d</c> apart rises <c>d / 2</c>, so the arcs always fit the range from 0
    /// to <c>n - 1</c> along X and from 0 to <c>(n - 1) / 2</c> along Y, which the series states as its data range. A
    /// self-loop has no arc to draw. The edges and the coordinates you supplied do not move the nodes.</summary>
    Arc = 4,
}
