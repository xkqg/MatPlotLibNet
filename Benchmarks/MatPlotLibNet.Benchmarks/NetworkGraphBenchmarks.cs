// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;
using MatPlotLibNet;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Benchmarks;

/// <summary>v1.10 — quantifies the cost of <see cref="NetworkGraphSeries"/> rendering
/// across the four layouts. Force-Directed (Fruchterman–Reingold) is O(N² × iters) for
/// repulsion + O(E × iters) for spring forces; the deterministic layouts are
/// O(N) or O(N log N). The benchmark matrix surfaces the N-cliff that drives the
/// "DO NOT EXCEED N≈X" cookbook rule for users about to throw 5000 assets into a
/// correlation network.</summary>
/// <remarks>
/// The 6 verfijningen baked in:
/// <list type="number">
/// <item><b>Edge-density variants</b> — at N=200, vary E ∈ {N, 5N, N²/2}: surfaces
/// whether the bottleneck is repulsion (O(N²), edge-independent) or spring-force
/// (O(E)). Repulsion dominates for dense graphs.</item>
/// <item><b>[MemoryDiagnoser]</b> — F-R reuses position arrays across iterations,
/// but the layout output array + per-iter allocations matter at N=500 × 100 iters.</item>
/// <item><b>Seed-stability</b> — every benchmark uses LayoutSeed = 42 so iter
/// counts to convergence are identical across repeats; eliminates noise from
/// variable convergence behaviour.</item>
/// <item><b>N=500 max realistic</b> — N=5000 → minutes per render. The cookbook
/// rule "DO NOT EXCEED N≈500 with ForceDirected; switch to Hierarchical above"
/// is derived from the N-cliff visible in the side-by-side benchmarks.</item>
/// <item><b>Fixed-iters vs convergence-mode</b> — separate benchmark for each.
/// Convergence-mode wins on sparse / well-separated topologies; fixed-iters wins
/// on dense graphs where energy never converges below threshold within sensible
/// iteration limits.</item>
/// <item><b>Side-by-side layouts at N={100, 500, 1000}</b> — three deterministic
/// O(N) layouts stay flat while ForceDirected exhibits the cliff.</item>
/// </list>
/// </remarks>
[MemoryDiagnoser]
public class NetworkGraphBenchmarks
{
    // Per-N graphs with fixed edge density E ≈ 5N (sparse k-NN style network).
    private GraphNode[] _nodes100  = default!;
    private GraphNode[] _nodes500  = default!;
    private GraphNode[] _nodes1000 = default!;
    private GraphEdge[] _edges100  = default!;
    private GraphEdge[] _edges500  = default!;
    private GraphEdge[] _edges1000 = default!;

    // Edge-density variants at N=200: sparse / 5N / dense (≈ N²/2)
    private GraphNode[] _nodes200       = default!;
    private GraphEdge[] _edges200Sparse = default!;
    private GraphEdge[] _edges200Mid    = default!;
    private GraphEdge[] _edges200Dense  = default!;

    [GlobalSetup]
    public void Setup()
    {
        var rng = new Random(42);

        _nodes100  = MakeNodes(100);
        _nodes500  = MakeNodes(500);
        _nodes1000 = MakeNodes(1000);
        _edges100  = MakeEdges(100,  edgeCount:  500, rng);
        _edges500  = MakeEdges(500,  edgeCount: 2500, rng);
        _edges1000 = MakeEdges(1000, edgeCount: 5000, rng);

        _nodes200       = MakeNodes(200);
        _edges200Sparse = MakeEdges(200, edgeCount: 200, rng);                  // E = N
        _edges200Mid    = MakeEdges(200, edgeCount: 1000, rng);                 // E = 5N
        _edges200Dense  = MakeEdges(200, edgeCount: 200 * 199 / 2, rng);        // E = N²/2 (complete)
    }

    private static GraphNode[] MakeNodes(int n)
    {
        var nodes = new GraphNode[n];
        for (int i = 0; i < n; i++) nodes[i] = new GraphNode($"n{i}");
        return nodes;
    }

    private static GraphEdge[] MakeEdges(int nodeCount, int edgeCount, Random rng)
    {
        var edges = new GraphEdge[edgeCount];
        for (int i = 0; i < edgeCount; i++)
        {
            int u = rng.Next(nodeCount);
            int v = rng.Next(nodeCount);
            if (u == v) v = (v + 1) % nodeCount;
            edges[i] = new GraphEdge($"n{u}", $"n{v}");
        }
        return edges;
    }

    private static string Render(GraphNode[] nodes, GraphEdge[] edges, GraphLayout layout,
        int seed = 42, int iterations = 50, double? convergenceThreshold = null) =>
        Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.NetworkGraph(nodes, edges, s =>
            {
                s.Layout = layout;
                s.LayoutSeed = seed;
                s.LayoutIterations = iterations;
                s.ConvergenceThreshold = convergenceThreshold;
            }))
            .ToSvg();

    // ── Side-by-side layouts at N ∈ {100, 500, 1000} ──────────────────────────
    // These show the deterministic-vs-quadratic cliff. Manual is pass-through cheap;
    // Circular is O(N); Hierarchical is O(N + E); ForceDirected is O(N² × iters).

    [Benchmark] public string Circular_N100()      => Render(_nodes100,  _edges100,  GraphLayout.Circular);
    [Benchmark] public string Circular_N500()      => Render(_nodes500,  _edges500,  GraphLayout.Circular);
    [Benchmark] public string Circular_N1000()     => Render(_nodes1000, _edges1000, GraphLayout.Circular);

    [Benchmark] public string Hierarchical_N100()  => Render(_nodes100,  _edges100,  GraphLayout.Hierarchical);
    [Benchmark] public string Hierarchical_N500()  => Render(_nodes500,  _edges500,  GraphLayout.Hierarchical);
    [Benchmark] public string Hierarchical_N1000() => Render(_nodes1000, _edges1000, GraphLayout.Hierarchical);

    [Benchmark] public string ForceDirected_N100()  => Render(_nodes100,  _edges100,  GraphLayout.ForceDirected);
    [Benchmark] public string ForceDirected_N500()  => Render(_nodes500,  _edges500,  GraphLayout.ForceDirected);
    // N=1000 is included for cliff documentation but expect tens of seconds per call.
    [Benchmark] public string ForceDirected_N1000() => Render(_nodes1000, _edges1000, GraphLayout.ForceDirected);

    // ── Edge-density variants at N=200 ────────────────────────────────────────
    // For a fixed N, varying E shows whether the bottleneck is repulsion (O(N²),
    // edge-independent) or spring-force (O(E)). Expect Sparse ≈ Mid << Dense.

    [Benchmark] public string ForceDirected_N200_EdgesSparse()
        => Render(_nodes200, _edges200Sparse, GraphLayout.ForceDirected);
    [Benchmark] public string ForceDirected_N200_EdgesMid()
        => Render(_nodes200, _edges200Mid, GraphLayout.ForceDirected);
    [Benchmark] public string ForceDirected_N200_EdgesDense()
        => Render(_nodes200, _edges200Dense, GraphLayout.ForceDirected);

    // ── Fixed-iterations vs convergence-mode at N=200 ─────────────────────────
    // Sparse graph stabilises quickly → convergence-mode wins. Dense graph never
    // settles below the threshold → both modes hit the iteration cap, costs match.

    [Benchmark] public string ForceDirected_N200_FixedIters()
        => Render(_nodes200, _edges200Mid, GraphLayout.ForceDirected, iterations: 100);
    [Benchmark] public string ForceDirected_N200_ConvergenceMode()
        => Render(_nodes200, _edges200Mid, GraphLayout.ForceDirected, iterations: 100, convergenceThreshold: 0.5);
}
