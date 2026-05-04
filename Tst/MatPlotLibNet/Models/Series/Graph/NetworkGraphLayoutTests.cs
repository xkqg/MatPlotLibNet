// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.SeriesRenderers;

namespace MatPlotLibNet.Tests.Models.Series.Graph;

/// <summary>v1.10 — pure-function tests for the deterministic graph layouts (Manual,
/// Circular, Hierarchical). ForceDirected (and its seeded-RNG convergence tests) is
/// covered separately in the v1.10 PR 2 follow-up.</summary>
public class NetworkGraphLayoutTests
{
    private static GraphNode N(string id, double x = 0, double y = 0) => new(id, x, y);
    private static GraphEdge E(string from, string to, bool directed = false) => new(from, to, 1.0, directed);

    // ── Manual: pass-through ──────────────────────────────────────────────────

    [Fact]
    public void Manual_PassesNodeCoordsThroughVerbatim()
    {
        var nodes = new[] { N("a", 1.5, 2.5), N("b", -3, 4), N("c", 0, 0) };
        var positioned = NetworkGraphLayouts.ApplyManual(nodes);

        Assert.Equal(3, positioned.Length);
        Assert.Equal(1.5, positioned[0].X);
        Assert.Equal(2.5, positioned[0].Y);
        Assert.Equal(-3,  positioned[1].X);
        Assert.Equal(4,   positioned[1].Y);
    }

    [Fact]
    public void Manual_EmptyInput_ReturnsEmpty()
    {
        var positioned = NetworkGraphLayouts.ApplyManual([]);
        Assert.Empty(positioned);
    }

    [Fact]
    public void Manual_PreservesIdLabelAndScalars()
    {
        var nodes = new[]
        {
            new GraphNode("alpha", 1, 1, ColorScalar: 0.5, SizeScalar: 2.0, Label: "A"),
        };
        var positioned = NetworkGraphLayouts.ApplyManual(nodes);
        Assert.Equal("alpha", positioned[0].Id);
        Assert.Equal("A",     positioned[0].Label);
        Assert.Equal(0.5,     positioned[0].ColorScalar);
        Assert.Equal(2.0,     positioned[0].SizeScalar);
    }

    // ── Circular: nodes on unit circle, evenly spaced ─────────────────────────

    [Fact]
    public void Circular_SingleNode_AtFirstAngle()
    {
        var positioned = NetworkGraphLayouts.ApplyCircular([N("a")]);
        Assert.Single(positioned);
        // angle 0 → (1, 0)
        Assert.Equal(1.0, positioned[0].X, precision: 10);
        Assert.Equal(0.0, positioned[0].Y, precision: 10);
    }

    [Fact]
    public void Circular_FourNodes_OnCardinalAngles()
    {
        var positioned = NetworkGraphLayouts.ApplyCircular([N("a"), N("b"), N("c"), N("d")]);
        // 0°, 90°, 180°, 270°
        Assert.Equal( 1.0, positioned[0].X, precision: 10); Assert.Equal( 0.0, positioned[0].Y, precision: 10);
        Assert.Equal( 0.0, positioned[1].X, precision: 10); Assert.Equal( 1.0, positioned[1].Y, precision: 10);
        Assert.Equal(-1.0, positioned[2].X, precision: 10); Assert.Equal( 0.0, positioned[2].Y, precision: 10);
        Assert.Equal( 0.0, positioned[3].X, precision: 10); Assert.Equal(-1.0, positioned[3].Y, precision: 10);
    }

    [Fact]
    public void Circular_AnyN_AllPositionsOnUnitCircle()
    {
        var nodes = Enumerable.Range(0, 17).Select(i => N($"n{i}")).ToArray();
        var positioned = NetworkGraphLayouts.ApplyCircular(nodes);
        foreach (var p in positioned)
        {
            double r = Math.Sqrt(p.X * p.X + p.Y * p.Y);
            Assert.Equal(1.0, r, precision: 9);
        }
    }

    [Fact]
    public void Circular_PreservesIdLabelAndScalars()
    {
        var n = new GraphNode("n0", 999, 999, ColorScalar: 0.7, SizeScalar: 3.0, Label: "label");
        var positioned = NetworkGraphLayouts.ApplyCircular([n]);
        Assert.Equal("n0",    positioned[0].Id);
        Assert.Equal("label", positioned[0].Label);
        Assert.Equal(0.7,     positioned[0].ColorScalar);
        Assert.Equal(3.0,     positioned[0].SizeScalar);
    }

    [Fact]
    public void Circular_EmptyInput_ReturnsEmpty()
    {
        Assert.Empty(NetworkGraphLayouts.ApplyCircular([]));
    }

    // ── Hierarchical: BFS layering top-down from first node ───────────────────

    [Fact]
    public void Hierarchical_LinearChain_DepthsZeroOneTwo()
    {
        // a → b → c (3 layers, 1 node each)
        var nodes = new[] { N("a"), N("b"), N("c") };
        var edges = new[] { E("a", "b"), E("b", "c") };
        var positioned = NetworkGraphLayouts.ApplyHierarchical(nodes, edges);

        Assert.Equal(3, positioned.Length);
        // Y is depth: a (depth 0) at top, c (depth 2) at bottom.
        // We don't assert exact Y values, only ordering.
        Assert.True(positioned[0].Y < positioned[1].Y);
        Assert.True(positioned[1].Y < positioned[2].Y);
    }

    [Fact]
    public void Hierarchical_Star_RootAtDepthZero_LeavesAtDepthOne()
    {
        // a → {b, c, d}: 1 root, 3 leaves
        var nodes = new[] { N("a"), N("b"), N("c"), N("d") };
        var edges = new[] { E("a", "b"), E("a", "c"), E("a", "d") };
        var positioned = NetworkGraphLayouts.ApplyHierarchical(nodes, edges);

        Assert.Equal(4, positioned.Length);
        // Root (a) Y < all leaves' Y.
        double rootY = positioned[0].Y;
        Assert.True(positioned[1].Y > rootY);
        Assert.True(positioned[2].Y > rootY);
        Assert.True(positioned[3].Y > rootY);
        // The 3 leaves are at the same depth (same Y).
        Assert.Equal(positioned[1].Y, positioned[2].Y, precision: 10);
        Assert.Equal(positioned[2].Y, positioned[3].Y, precision: 10);
        // And spread horizontally — distinct X.
        Assert.NotEqual(positioned[1].X, positioned[2].X);
        Assert.NotEqual(positioned[2].X, positioned[3].X);
    }

    [Fact]
    public void Hierarchical_NoEdges_AllNodesAtDepthZero()
    {
        var nodes = new[] { N("a"), N("b"), N("c") };
        var positioned = NetworkGraphLayouts.ApplyHierarchical(nodes, []);
        // Without edges, every node is at depth 0.
        Assert.Equal(positioned[0].Y, positioned[1].Y, precision: 10);
        Assert.Equal(positioned[1].Y, positioned[2].Y, precision: 10);
    }

    [Fact]
    public void Hierarchical_Cycle_HandledGracefully()
    {
        // a → b → c → a — cycle. BFS must not infinite-loop.
        var nodes = new[] { N("a"), N("b"), N("c") };
        var edges = new[] { E("a", "b"), E("b", "c"), E("c", "a") };
        var ex = Record.Exception(() => NetworkGraphLayouts.ApplyHierarchical(nodes, edges));
        Assert.Null(ex);
    }

    [Fact]
    public void Hierarchical_DisconnectedComponent_PlacedAtDepthZero()
    {
        // a → b reachable from a; c is disconnected
        var nodes = new[] { N("a"), N("b"), N("c") };
        var edges = new[] { E("a", "b") };
        var positioned = NetworkGraphLayouts.ApplyHierarchical(nodes, edges);
        Assert.Equal(3, positioned.Length);
        // c (disconnected) defaults to depth 0 like a, not depth 1.
        Assert.Equal(positioned[0].Y, positioned[2].Y, precision: 10);
    }

    [Fact]
    public void Hierarchical_EmptyInput_ReturnsEmpty()
    {
        Assert.Empty(NetworkGraphLayouts.ApplyHierarchical([], []));
    }

    // ── Apply (dispatch) — pre-PR2 ForceDirected falls back to Manual ─────────

    [Fact]
    public void Apply_Manual_DispatchesToManual()
    {
        var nodes = new[] { N("a", 5, 5) };
        var positioned = NetworkGraphLayouts.Apply(GraphLayout.Manual, nodes, []);
        Assert.Equal(5, positioned[0].X);
    }

    [Fact]
    public void Apply_Circular_DispatchesToCircular()
    {
        var positioned = NetworkGraphLayouts.Apply(GraphLayout.Circular, [N("a", 99, 99)], []);
        // Circular ignores the input X/Y — so X must NOT be 99.
        Assert.NotEqual(99, positioned[0].X);
    }

    [Fact]
    public void Apply_Hierarchical_DispatchesToHierarchical()
    {
        var positioned = NetworkGraphLayouts.Apply(GraphLayout.Hierarchical,
            [N("a"), N("b")], [E("a", "b")]);
        Assert.True(positioned[0].Y < positioned[1].Y);
    }

    // ── ForceDirected (Fruchterman–Reingold) — PR 2 activation ────────────────
    //
    // The algorithm: O(N²) per iteration repulsive force between every pair of nodes,
    // O(E) attractive spring force on connected pairs, temperature-cooled step size.
    // Final positions are normalised to [-1, 1] in both axes. Seeded RNG gives
    // bit-identical determinism across runs at the same seed.

    [Fact]
    public void ForceDirected_SameSeed_ProducesIdenticalPositions()
    {
        var nodes = new[] { N("a"), N("b"), N("c"), N("d"), N("e") };
        var edges = new[] { E("a", "b"), E("b", "c"), E("c", "d"), E("d", "e"), E("e", "a") };
        var p1 = NetworkGraphLayouts.ApplyForceDirected(nodes, edges, seed: 42);
        var p2 = NetworkGraphLayouts.ApplyForceDirected(nodes, edges, seed: 42);
        Assert.Equal(p1.Length, p2.Length);
        for (int i = 0; i < p1.Length; i++)
        {
            Assert.Equal(p1[i].X, p2[i].X, precision: 12);
            Assert.Equal(p1[i].Y, p2[i].Y, precision: 12);
        }
    }

    [Fact]
    public void ForceDirected_DifferentSeeds_ProduceDifferentPositions()
    {
        var nodes = new[] { N("a"), N("b"), N("c"), N("d"), N("e") };
        var edges = new[] { E("a", "b"), E("b", "c") };
        var p1 = NetworkGraphLayouts.ApplyForceDirected(nodes, edges, seed: 1);
        var p2 = NetworkGraphLayouts.ApplyForceDirected(nodes, edges, seed: 999);
        // At least one node must land at a different position.
        bool anyDifferent = false;
        for (int i = 0; i < p1.Length; i++)
            if (Math.Abs(p1[i].X - p2[i].X) > 1e-6 || Math.Abs(p1[i].Y - p2[i].Y) > 1e-6)
                anyDifferent = true;
        Assert.True(anyDifferent, "Different seeds must produce visibly different layouts.");
    }

    [Fact]
    public void ForceDirected_PositionsStayInUnitSquare()
    {
        var nodes = Enumerable.Range(0, 20).Select(i => N($"n{i}")).ToArray();
        var edges = Enumerable.Range(0, 19).Select(i => E($"n{i}", $"n{i + 1}")).ToArray();
        var positioned = NetworkGraphLayouts.ApplyForceDirected(nodes, edges, seed: 7);
        foreach (var p in positioned)
        {
            Assert.True(p.X >= -1.0 - 1e-9 && p.X <= 1.0 + 1e-9, $"X out of [-1,1]: {p.X}");
            Assert.True(p.Y >= -1.0 - 1e-9 && p.Y <= 1.0 + 1e-9, $"Y out of [-1,1]: {p.Y}");
        }
    }

    [Fact]
    public void ForceDirected_PreservesIdLabelAndScalars()
    {
        var nodes = new[]
        {
            new GraphNode("alpha", 999, 999, ColorScalar: 0.5, SizeScalar: 2.0, Label: "A"),
            new GraphNode("beta",  0,   0,   ColorScalar: 0.3, SizeScalar: 1.0, Label: "B"),
        };
        var positioned = NetworkGraphLayouts.ApplyForceDirected(nodes, [E("alpha", "beta")], seed: 0);
        Assert.Equal("alpha", positioned[0].Id);
        Assert.Equal("A",     positioned[0].Label);
        Assert.Equal(0.5,     positioned[0].ColorScalar);
        Assert.Equal(2.0,     positioned[0].SizeScalar);
    }

    [Fact]
    public void ForceDirected_EmptyInput_ReturnsEmpty()
    {
        Assert.Empty(NetworkGraphLayouts.ApplyForceDirected([], [], seed: 0));
    }

    [Fact]
    public void ForceDirected_SingleNode_ReturnsCenterPosition()
    {
        var positioned = NetworkGraphLayouts.ApplyForceDirected([N("solo")], [], seed: 0);
        Assert.Single(positioned);
        // Single-node graph has no forces; just confirm it lands somewhere finite.
        Assert.True(double.IsFinite(positioned[0].X));
        Assert.True(double.IsFinite(positioned[0].Y));
    }

    [Fact]
    public void ForceDirected_DisconnectedComponents_DoNotThrow()
    {
        // Two disconnected cliques: {a,b,c} fully connected, {d,e,f} fully connected,
        // no cross-edges.
        var nodes = new[] { N("a"), N("b"), N("c"), N("d"), N("e"), N("f") };
        var edges = new[]
        {
            E("a", "b"), E("b", "c"), E("c", "a"),
            E("d", "e"), E("e", "f"), E("f", "d"),
        };
        var ex = Record.Exception(() => NetworkGraphLayouts.ApplyForceDirected(nodes, edges, seed: 0));
        Assert.Null(ex);
    }

    [Fact]
    public void ForceDirected_AllSelfLoops_DoNotProduceNaN()
    {
        // Self-loops would normally cause d=0 division. Defensive code path.
        var nodes = new[] { N("a"), N("b") };
        var edges = new[] { E("a", "a"), E("b", "b"), E("a", "b") };
        var positioned = NetworkGraphLayouts.ApplyForceDirected(nodes, edges, seed: 0);
        foreach (var p in positioned)
        {
            Assert.True(double.IsFinite(p.X));
            Assert.True(double.IsFinite(p.Y));
        }
    }

    [Fact]
    public void ForceDirected_ConvergenceMode_HighThreshold_ProducesFinitePositions()
    {
        // High threshold → loop will likely exit very early, but result must still be valid.
        var nodes = new[] { N("a"), N("b"), N("c"), N("d") };
        var edges = new[] { E("a", "b"), E("b", "c"), E("c", "d") };
        var positioned = NetworkGraphLayouts.ApplyForceDirected(
            nodes, edges, seed: 0, iterations: 100, convergenceThreshold: 1e3);
        foreach (var p in positioned)
        {
            Assert.True(double.IsFinite(p.X));
            Assert.True(double.IsFinite(p.Y));
        }
    }

    [Fact]
    public void ForceDirected_FewerIterations_StillProducesValidPositions()
    {
        // Only 5 iterations → coarse layout, but no NaN or out-of-bounds.
        var nodes = new[] { N("a"), N("b"), N("c") };
        var edges = new[] { E("a", "b"), E("b", "c") };
        var positioned = NetworkGraphLayouts.ApplyForceDirected(nodes, edges, seed: 0, iterations: 5);
        foreach (var p in positioned)
        {
            Assert.True(double.IsFinite(p.X));
            Assert.True(double.IsFinite(p.Y));
            Assert.True(p.X >= -1.0 - 1e-9 && p.X <= 1.0 + 1e-9);
            Assert.True(p.Y >= -1.0 - 1e-9 && p.Y <= 1.0 + 1e-9);
        }
    }

    [Fact]
    public void Apply_ForceDirected_DispatchesToFruchtermanReingold()
    {
        // After PR 2 the Apply dispatcher must call the F-R body, not Manual.
        // Verifies by checking that input X/Y is NOT preserved (Manual would pass through).
        var positioned = NetworkGraphLayouts.Apply(GraphLayout.ForceDirected, [N("a", 9999, 9999), N("b", -9999, -9999)], [E("a", "b")]);
        Assert.NotEqual(9999, positioned[0].X);
        Assert.NotEqual(-9999, positioned[1].X);
    }
}
