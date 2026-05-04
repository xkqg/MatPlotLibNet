// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models.Series;

/// <summary>Layout algorithm selector for <see cref="NetworkGraphSeries"/>.</summary>
/// <remarks>Ordinals are explicit and append-only. <see cref="ForceDirected"/> is
/// reserved at ordinal <c>1</c> for the v1.10 PR 2 follow-up — until activated, the
/// internal layout dispatcher falls back to <see cref="Manual"/> (pass-through) so
/// existing serialised JSON keeps deserialising without throwing.</remarks>
public enum GraphLayout
{
    /// <summary>Use the <see cref="GraphNode.X"/> / <see cref="GraphNode.Y"/> coordinates
    /// supplied on each node verbatim — no automatic placement.</summary>
    Manual = 0,

    /// <summary>Fruchterman–Reingold spring-embedder layout with seeded RNG. Reserved —
    /// activated in v1.10 PR 2; until then the layout dispatcher falls back to
    /// <see cref="Manual"/>.</summary>
    ForceDirected = 1,

    /// <summary>Place every node on the unit circle at evenly-spaced angles.
    /// O(N), deterministic, ignores edges and input <see cref="GraphNode.X"/>/<see cref="GraphNode.Y"/>.</summary>
    Circular = 2,

    /// <summary>BFS top-down layering from the first node: depth = Y, within-depth order = X.
    /// Disconnected components default to depth 0. Cycles are tolerated.</summary>
    Hierarchical = 3,
}
