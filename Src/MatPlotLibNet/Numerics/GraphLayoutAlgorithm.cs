// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Numerics;

/// <summary>How a <see cref="NetworkGraphSeries"/> places its nodes: one sealed leaf for each <see cref="GraphLayout"/>, chosen
/// in one place (<see cref="For"/>). A leaf takes the input nodes (and the edges where it needs the structure) and returns a fresh
/// <see cref="GraphNode"/> array with <see cref="GraphNode.X"/> and <see cref="GraphNode.Y"/> placed; every other node property
/// (Id, Label, ColorScalar, SizeScalar) is kept as it is.</summary>
/// <remarks>Four leaves are deterministic arithmetic on the nodes; <see cref="ForceDirectedGraphLayout"/> runs a spring embedder
/// from seeded random positions, so it is deterministic too for a given seed. The series works its positions out once for each
/// change (<see cref="NetworkGraphSeries.Placement"/>), so its data range and its drawing read the same placement.</remarks>
internal abstract class GraphLayoutAlgorithm
{
    /// <summary>The layout that keeps the coordinates of the nodes.</summary>
    internal static readonly ManualGraphLayout Manual = new();

    /// <summary>The layout on the unit circle.</summary>
    internal static readonly CircularGraphLayout Circular = new();

    /// <summary>The layered layout.</summary>
    internal static readonly HierarchicalGraphLayout Hierarchical = new();

    /// <summary>The arc diagram.</summary>
    internal static readonly ArcGraphLayout Arc = new();

    /// <summary>The algorithm <paramref name="series"/> asks for by its <see cref="NetworkGraphSeries.Layout"/>: the one place
    /// the layout is mapped to the algorithm. The spring embedder takes the series' seed, iteration count and convergence
    /// threshold; a layout the enum does not declare keeps the coordinates of the nodes, as <see cref="GraphLayout.Manual"/>
    /// does, rather than throwing.</summary>
    /// <param name="series">The graph to place.</param>
    internal static GraphLayoutAlgorithm For(NetworkGraphSeries series) => series.Layout switch
    {
        GraphLayout.Circular      => Circular,
        GraphLayout.Hierarchical  => Hierarchical,
        GraphLayout.ForceDirected => new ForceDirectedGraphLayout(series.LayoutSeed, series.LayoutIterations, series.ConvergenceThreshold),
        GraphLayout.Arc           => Arc,
        _                         => Manual,
    };

    /// <summary>Places the nodes.</summary>
    /// <param name="nodes">The graph's nodes.</param>
    /// <param name="edges">The graph's edges.</param>
    /// <returns>A fresh array, one placed node for each node, in the same order.</returns>
    internal abstract GraphNode[] Place(IReadOnlyList<GraphNode> nodes, IReadOnlyList<GraphEdge> edges);

    /// <summary>Whether this layout draws every edge as an arc over a row of nodes instead of as a straight line.</summary>
    internal virtual bool DrawsEdgesAsArcs => false;

    /// <summary>The top of the data range: <paramref name="yMax"/>, the highest node, unless the edges this layout draws reach
    /// higher.</summary>
    /// <param name="positioned">The placed nodes.</param>
    /// <param name="edges">The graph's edges.</param>
    /// <param name="yMax">The Y of the highest placed node.</param>
    internal virtual double Top(IReadOnlyList<GraphNode> positioned, IReadOnlyList<GraphEdge> edges, double yMax) => yMax;
}

/// <summary>Pass-through: each node's <see cref="GraphNode.X"/> / <see cref="GraphNode.Y"/> is kept as it is. Useful when the
/// caller has worked the positions out (with a layout of their own, or from a figure saved before).</summary>
internal sealed class ManualGraphLayout : GraphLayoutAlgorithm
{
    /// <inheritdoc />
    internal override GraphNode[] Place(IReadOnlyList<GraphNode> nodes, IReadOnlyList<GraphEdge> edges)
    {
        var result = new GraphNode[nodes.Count];
        for (int i = 0; i < nodes.Count; i++) result[i] = nodes[i];
        return result;
    }
}

/// <summary>Every node on the unit circle at evenly spaced angles: node <c>i</c> lands at <c>(cos(2π·i/N), sin(2π·i/N))</c>, a
/// single node at angle 0, <c>(1, 0)</c>.</summary>
internal sealed class CircularGraphLayout : GraphLayoutAlgorithm
{
    /// <inheritdoc />
    internal override GraphNode[] Place(IReadOnlyList<GraphNode> nodes, IReadOnlyList<GraphEdge> edges)
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
}

/// <summary>An arc diagram's row: node <c>i</c> lands at <c>(i, 0)</c>, in the order the nodes are stored; the edges and the
/// coordinates you supplied move nothing. The edges are the arcs the renderer draws over the row, and the highest of them is the
/// top of the series' data range.</summary>
internal sealed class ArcGraphLayout : GraphLayoutAlgorithm
{
    /// <inheritdoc />
    internal override GraphNode[] Place(IReadOnlyList<GraphNode> nodes, IReadOnlyList<GraphEdge> edges)
    {
        var result = new GraphNode[nodes.Count];
        for (int i = 0; i < result.Length; i++) result[i] = nodes[i] with { X = i, Y = 0 };
        return result;
    }

    /// <inheritdoc />
    internal override bool DrawsEdgesAsArcs => true;

    /// <inheritdoc />
    /// <remarks>The arcs rise over the row of nodes: the range holds the highest of them.</remarks>
    internal override double Top(IReadOnlyList<GraphNode> positioned, IReadOnlyList<GraphEdge> edges, double yMax) =>
        Math.Max(yMax, Height(positioned, edges));

    /// <summary>How high an arc rises over the row: half the distance between its two nodes, so the arc is a half circle when
    /// the plot has an aspect of one. The one rule the renderer draws an arc by and the series states its data range by.</summary>
    /// <param name="xFrom">Where the arc's first node stands along the row.</param>
    /// <param name="xTo">Where the arc's second node stands.</param>
    /// <returns>The radius of the semicircle, in data units.</returns>
    internal static double Radius(double xFrom, double xTo) => Math.Abs(xTo - xFrom) / 2;

    /// <summary>The height of the highest arc of an arc diagram: the radius of the longest edge between two different nodes. A
    /// self-loop has no arc and an edge to a node that is not there is not drawn, so neither counts.</summary>
    /// <param name="positioned">The nodes, already laid out in a row.</param>
    /// <param name="edges">The graph's edges.</param>
    /// <returns>The height the arcs reach above the row; 0 when there is no arc.</returns>
    internal static double Height(IReadOnlyList<GraphNode> positioned, IReadOnlyList<GraphEdge> edges)
    {
        var index = new Dictionary<string, int>(positioned.Count);
        for (int i = 0; i < positioned.Count; i++) index[positioned[i].Id] = i;

        double height = 0;
        foreach (var edge in edges)
        {
            if (index.TryGetValue(edge.From, out int u) && index.TryGetValue(edge.To, out int v) && u != v)
            {
                height = Math.Max(height, Radius(positioned[u].X, positioned[v].X));
            }
        }

        return height;
    }
}

/// <summary>Breadth-first layering from the top. Node 0 is the root (depth 0); each edge advances the frontier by one depth.
/// Within a depth the nodes spread evenly along X. Disconnected nodes stay at depth 0, and cycles are tolerated through the
/// visited set. Edges are treated as undirected for the layering (<see cref="GraphEdge.IsDirected"/> is ignored here: the
/// direction only matters for the arrowheads the renderer draws).</summary>
internal sealed class HierarchicalGraphLayout : GraphLayoutAlgorithm
{
    /// <inheritdoc />
    internal override GraphNode[] Place(IReadOnlyList<GraphNode> nodes, IReadOnlyList<GraphEdge> edges)
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
}

/// <summary>Fruchterman–Reingold (1991) spring embedder. A repulsive force <c>k²/d</c> between every pair of nodes, an
/// attractive spring force <c>d²/k</c> on each edge, <c>k = √(area/N)</c> the best distance between nodes in the [-1, 1]²
/// canvas. The step is capped by a temperature that cools linearly from <c>t₀ = 0.1</c> to 0 over <paramref name="iterations"/>.
/// Positions stay inside [-1, 1] in both axes: a step that would leave the canvas stops at its edge.</summary>
/// <remarks>O(N² + E) per iteration. The initial positions are random but come from a generator seeded with
/// <paramref name="seed"/>, so the same input and seed give the same layout; another seed gives another start. When
/// <paramref name="convergenceThreshold"/> is set and the total displacement of one iteration drops below it, the loop stops early.
/// Self-loops and disconnected components are tolerated.</remarks>
/// <param name="seed">Seed for the initial positions. The same seed gives the same output on every run.</param>
/// <param name="iterations">The most iterations to run.</param>
/// <param name="convergenceThreshold">The displacement below which an iteration ends the loop early; <see langword="null"/> runs
/// every iteration.</param>
internal sealed class ForceDirectedGraphLayout(int seed, int iterations, double? convergenceThreshold) : GraphLayoutAlgorithm
{
    /// <summary>The iteration count a new series runs: it balances the look of the layout against the O(N²) cost of each
    /// iteration.</summary>
    internal const int DefaultIterations = 50;

    /// <summary>The smallest distance or displacement the embedder works with: two nodes on one spot are pushed apart along X as
    /// if they stood this far apart, and a node that would move less stays where it is.</summary>
    private const double Epsilon = 1e-9;

    /// <inheritdoc />
    internal override GraphNode[] Place(IReadOnlyList<GraphNode> nodes, IReadOnlyList<GraphEdge> edges)
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
        var edgePairs = new List<EdgePair>(edges.Count);
        foreach (var e in edges)
        {
            if (!idToIndex.TryGetValue(e.From, out int u)) continue;
            if (!idToIndex.TryGetValue(e.To,   out int v)) continue;
            if (u == v) continue; // skip self-loops — d=0 would explode the repulsion
            edgePairs.Add(new EdgePair(u, v));
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
        double k = Math.Sqrt(area / n);
        double k2 = k * k;

        // Reuse displacement arrays across iterations (zero-alloc steady state).
        var dx = new double[n];
        var dy = new double[n];

        for (int iter = 0; iter < iterations; iter++)
        {
            for (int i = 0; i < n; i++) { dx[i] = 0; dy[i] = 0; }

            // Repulsive forces: O(N²) over all pairs.
            for (int i = 0; i < n; i++)
            for (int j = i + 1; j < n; j++)
            {
                double rx = px[i] - px[j];
                double ry = py[i] - py[j];
                double d  = new Point(rx, ry).Length;
                if (d < Epsilon) { d = Epsilon; rx = Epsilon; ry = 0; }
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
                double d  = new Point(rx, ry).Length;
                if (d < Epsilon) continue;
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
                double mag = new Point(dx[i], dy[i]).Length;
                if (mag < Epsilon) continue;
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

        for (int i = 0; i < n; i++)
            result[i] = nodes[i] with { X = px[i], Y = py[i] };
        return result;
    }

    /// <summary>An edge between two nodes, by the index of each in the node list.</summary>
    private readonly record struct EdgePair(int U, int V);
}

/// <summary>The positions of a network graph's nodes, worked out by <paramref name="Algorithm"/> at the change
/// <paramref name="Stamp"/> of the series: one immutable value, so the stamp and the nodes it stands for are always read
/// together.</summary>
/// <param name="Stamp">The series' <see cref="Models.ChangeClock"/> mark the nodes were placed at.</param>
/// <param name="Algorithm">The layout that placed them.</param>
/// <param name="Nodes">The placed nodes.</param>
internal sealed record GraphPlacement(long Stamp, GraphLayoutAlgorithm Algorithm, GraphNode[] Nodes);
