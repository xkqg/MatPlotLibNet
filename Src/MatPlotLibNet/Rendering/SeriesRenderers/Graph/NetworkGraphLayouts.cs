// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Pure-function graph layout algorithms used by
/// <c>NetworkGraphSeriesRenderer</c>. Each algorithm takes the input nodes (and edges
/// where it needs structural information) and returns a fresh <see cref="GraphNode"/>
/// array with positioned <see cref="GraphNode.X"/> and <see cref="GraphNode.Y"/>
/// coordinates. All other node properties (Id, Label, ColorScalar, SizeScalar) are
/// preserved verbatim.</summary>
/// <remarks>v1.10 PR 1 ships the three deterministic layouts (<see cref="ApplyManual"/>,
/// <see cref="ApplyCircular"/>, <see cref="ApplyHierarchical"/>). The fourth —
/// <see cref="ApplyForceDirected"/> — lands in PR 2; until then it falls back to
/// <see cref="ApplyManual"/> so any DTO with <see cref="GraphLayout.ForceDirected"/>
/// already round-trips through serialisation without breaking.</remarks>
internal static class NetworkGraphLayouts
{
    /// <summary>Pass-through: each node's <see cref="GraphNode.X"/> / <see cref="GraphNode.Y"/>
    /// is preserved verbatim. Useful when the caller has pre-computed positions
    /// (e.g. from a bespoke layout algorithm or a previously-saved figure).</summary>
    internal static GraphNode[] ApplyManual(IReadOnlyList<GraphNode> nodes)
    {
        var result = new GraphNode[nodes.Count];
        for (int i = 0; i < nodes.Count; i++) result[i] = nodes[i];
        return result;
    }

    /// <summary>Place every node on the unit circle at evenly-spaced angles.
    /// Node <c>i</c> lands at <c>(cos(2π·i/N), sin(2π·i/N))</c>. The single-node
    /// case lands at angle 0 → <c>(1, 0)</c>.</summary>
    internal static GraphNode[] ApplyCircular(IReadOnlyList<GraphNode> nodes)
    {
        int n = nodes.Count;
        var result = new GraphNode[n];
        if (n == 0) return result;

        double step = 2 * Math.PI / n;
        for (int i = 0; i < n; i++)
        {
            double angle = step * i;
            result[i] = nodes[i] with { X = Math.Cos(angle), Y = Math.Sin(angle) };
        }
        return result;
    }

    /// <summary>BFS top-down layering. Node 0 is the root (depth 0); each edge advances
    /// the BFS frontier by one depth. Within a depth, nodes spread evenly along the X
    /// axis. Disconnected components default to depth 0. Cycles are tolerated via the
    /// visited set. Edges are treated as undirected for the purpose of frontier expansion
    /// (<see cref="GraphEdge.IsDirected"/> is ignored here — directionality only matters
    /// at render time for arrowhead emission).</summary>
    internal static GraphNode[] ApplyHierarchical(
        IReadOnlyList<GraphNode> nodes,
        IReadOnlyList<GraphEdge> edges)
    {
        int n = nodes.Count;
        var result = new GraphNode[n];
        if (n == 0) return result;

        // Build undirected adjacency by node ID.
        var idToIndex = new Dictionary<string, int>(n);
        for (int i = 0; i < n; i++) idToIndex[nodes[i].Id] = i;

        var adj = new List<int>[n];
        for (int i = 0; i < n; i++) adj[i] = [];
        foreach (var e in edges)
        {
            if (idToIndex.TryGetValue(e.From, out int u) && idToIndex.TryGetValue(e.To, out int v))
            {
                adj[u].Add(v);
                adj[v].Add(u); // undirected for layering
            }
        }

        // BFS from node 0; disconnected nodes stay at depth 0.
        var depth = new int[n];
        for (int i = 0; i < n; i++) depth[i] = 0;
        var visited = new bool[n];
        visited[0] = true;
        var queue = new Queue<int>();
        queue.Enqueue(0);
        while (queue.Count > 0)
        {
            int u = queue.Dequeue();
            foreach (int v in adj[u])
            {
                if (visited[v]) continue;
                visited[v] = true;
                depth[v] = depth[u] + 1;
                queue.Enqueue(v);
            }
        }

        // Group node indices by depth and spread along X within each layer.
        var byDepth = new Dictionary<int, List<int>>();
        for (int i = 0; i < n; i++)
        {
            if (!byDepth.TryGetValue(depth[i], out var list))
                byDepth[depth[i]] = list = [];
            list.Add(i);
        }

        foreach (var (d, indices) in byDepth)
        {
            int count = indices.Count;
            for (int j = 0; j < count; j++)
            {
                int i = indices[j];
                // Centre layer around X = 0; single-node layer lands at X = 0.
                double x = count == 1 ? 0.0 : -1.0 + 2.0 * j / (count - 1);
                result[i] = nodes[i] with { X = x, Y = d };
            }
        }
        return result;
    }

    /// <summary>Default iteration count for the Fruchterman–Reingold spring-embedder.
    /// Balances visual quality against O(N²) per-iteration cost. Override via
    /// <see cref="ApplyForceDirected"/> when callers need more or fewer iterations.</summary>
    internal const int DefaultForceDirectedIterations = 50;

    /// <summary>Fruchterman–Reingold (1991) spring-embedder layout.
    /// Repulsive force <c>k²/d</c> between every pair of nodes; attractive spring
    /// force <c>d²/k</c> on each edge; <c>k = √(area/N)</c> = optimal node distance
    /// in the [-1, 1]² canvas. Step size is capped by a temperature schedule that
    /// cools linearly from <c>t₀ = 0.1</c> to 0 over <paramref name="iterations"/>.
    /// Final positions are normalised to fit [-1, 1] in both axes.</summary>
    /// <remarks>O(N² + E) per iteration. The algorithm is non-deterministic without
    /// a seed (initial positions are random); pass <paramref name="seed"/> for
    /// reproducible output. Convergence-mode early-stop is available via
    /// <paramref name="convergenceThreshold"/> — when total displacement-energy
    /// across one iteration drops below the threshold, the loop exits early.
    /// Self-loops and disconnected components are tolerated.</remarks>
    /// <param name="nodes">The graph's nodes.</param>
    /// <param name="edges">The graph's edges. Treated as undirected.</param>
    /// <param name="seed">Seed for the initial-position RNG. Same seed → identical output across runs.</param>
    /// <param name="iterations">Maximum iteration count. Default <see cref="DefaultForceDirectedIterations"/>.</param>
    /// <param name="convergenceThreshold">Optional energy threshold for early-stop.
    /// When non-null and per-iter total displacement falls below this value, the
    /// loop exits before <paramref name="iterations"/>. Useful for sparse or
    /// well-separated graphs that stabilise quickly.</param>
    internal static GraphNode[] ApplyForceDirected(
        IReadOnlyList<GraphNode> nodes,
        IReadOnlyList<GraphEdge> edges,
        int seed = 0,
        int iterations = DefaultForceDirectedIterations,
        double? convergenceThreshold = null)
    {
        int n = nodes.Count;
        var result = new GraphNode[n];
        if (n == 0) return result;
        if (n == 1)
        {
            result[0] = nodes[0] with { X = 0.0, Y = 0.0 };
            return result;
        }

        // Build adjacency by node ID for the attractive-force pass.
        var idToIndex = new Dictionary<string, int>(n);
        for (int i = 0; i < n; i++) idToIndex[nodes[i].Id] = i;
        var edgePairs = new List<(int U, int V)>(edges.Count);
        foreach (var e in edges)
        {
            if (!idToIndex.TryGetValue(e.From, out int u)) continue;
            if (!idToIndex.TryGetValue(e.To,   out int v)) continue;
            if (u == v) continue; // skip self-loops — d=0 would explode the repulsion
            edgePairs.Add((u, v));
        }

        // Random initial positions in [-1, 1]² via seeded RNG.
        var rng = new NpRandom(seed);
        var initX = rng.Uniform(-1.0, 1.0, n);
        var initY = rng.Uniform(-1.0, 1.0, n);
        var px = new double[n];
        var py = new double[n];
        for (int i = 0; i < n; i++) { px[i] = initX[i]; py[i] = initY[i]; }

        // F-R constants. Canvas is [-1, 1]² → area = 4. Optimal node distance
        // k = √(area / N). Initial temperature 0.1 ≈ 5% of canvas width per step.
        const double area = 4.0;
        const double tStart = 0.1;
        const double epsilon = 1e-9;
        double k = Math.Sqrt(area / n);
        double k2 = k * k;

        // Reuse displacement arrays across iterations (zero-alloc steady state).
        var dx = new double[n];
        var dy = new double[n];

        int actualIters = 0;
        for (int iter = 0; iter < iterations; iter++)
        {
            actualIters++;
            for (int i = 0; i < n; i++) { dx[i] = 0; dy[i] = 0; }

            // Repulsive forces: O(N²) over all pairs.
            for (int i = 0; i < n; i++)
            for (int j = i + 1; j < n; j++)
            {
                double rx = px[i] - px[j];
                double ry = py[i] - py[j];
                double d  = Math.Sqrt(rx * rx + ry * ry);
                if (d < epsilon) { d = epsilon; rx = epsilon; ry = 0; }
                double f  = k2 / d;
                double ux = rx / d, uy = ry / d;
                dx[i] += ux * f; dy[i] += uy * f;
                dx[j] -= ux * f; dy[j] -= uy * f;
            }

            // Attractive forces: O(E) over edges.
            foreach (var (u, v) in edgePairs)
            {
                double rx = px[u] - px[v];
                double ry = py[u] - py[v];
                double d  = Math.Sqrt(rx * rx + ry * ry);
                if (d < epsilon) continue;
                double f  = (d * d) / k;
                double ux = rx / d, uy = ry / d;
                dx[u] -= ux * f; dy[u] -= uy * f;
                dx[v] += ux * f; dy[v] += uy * f;
            }

            // Cooling: temperature drops linearly from tStart to 0.
            double t = tStart * (1.0 - (double)iter / iterations);
            double totalEnergy = 0;
            for (int i = 0; i < n; i++)
            {
                double mag = Math.Sqrt(dx[i] * dx[i] + dy[i] * dy[i]);
                if (mag < epsilon) continue;
                // Cap step by temperature.
                double scale = Math.Min(mag, t) / mag;
                px[i] += dx[i] * scale;
                py[i] += dy[i] * scale;
                // Clamp inside canvas.
                if (px[i] < -1.0) px[i] = -1.0; else if (px[i] > 1.0) px[i] = 1.0;
                if (py[i] < -1.0) py[i] = -1.0; else if (py[i] > 1.0) py[i] = 1.0;
                totalEnergy += Math.Min(mag, t);
            }

            // Convergence-mode early-stop.
            if (convergenceThreshold.HasValue && totalEnergy < convergenceThreshold.Value) break;
        }
        _ = actualIters; // available to callers via a future overload if needed

        for (int i = 0; i < n; i++)
            result[i] = nodes[i] with { X = px[i], Y = py[i] };
        return result;
    }

    /// <summary>Dispatches to the layout algorithm matching <paramref name="kind"/>.
    /// Unknown kinds (none currently exist; the enum is exhaustive) fall back to
    /// <see cref="ApplyManual"/> rather than throwing. The
    /// <paramref name="seed"/>, <paramref name="iterations"/>, and
    /// <paramref name="convergenceThreshold"/> parameters apply only to
    /// <see cref="GraphLayout.ForceDirected"/>; deterministic layouts ignore them.</summary>
    internal static GraphNode[] Apply(
        GraphLayout kind,
        IReadOnlyList<GraphNode> nodes,
        IReadOnlyList<GraphEdge> edges,
        int seed = 0,
        int iterations = DefaultForceDirectedIterations,
        double? convergenceThreshold = null) => kind switch
    {
        GraphLayout.Manual        => ApplyManual(nodes),
        GraphLayout.Circular      => ApplyCircular(nodes),
        GraphLayout.Hierarchical  => ApplyHierarchical(nodes, edges),
        GraphLayout.ForceDirected => ApplyForceDirected(nodes, edges, seed, iterations, convergenceThreshold),
        _                         => ApplyManual(nodes),
    };
}
