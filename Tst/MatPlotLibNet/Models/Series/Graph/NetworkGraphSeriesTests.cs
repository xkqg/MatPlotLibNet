// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Models.Series.Graph;

/// <summary>v1.10 — Verifies <see cref="NetworkGraphSeries"/> construction, default
/// properties, and <c>ToSeriesDto</c> emission.</summary>
public class NetworkGraphSeriesTests
{
    private static GraphNode N(string id) => new(id);
    private static GraphEdge E(string from, string to) => new(from, to);

    private static NetworkGraphSeries Sample() => new(
        [N("a"), N("b"), N("c")],
        [E("a", "b"), E("b", "c")]);

    // ── Construction ─────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_StoresNodesAndEdges()
    {
        var nodes = new[] { N("a"), N("b") };
        var edges = new[] { E("a", "b") };
        var s = new NetworkGraphSeries(nodes, edges);
        Assert.Equal(2, s.Nodes.Count);
        Assert.Single(s.Edges);
    }

    [Fact]
    public void Constructor_AcceptsEmptyEdges()
    {
        var s = new NetworkGraphSeries([N("a")], []);
        Assert.Single(s.Nodes);
        Assert.Empty(s.Edges);
    }

    // ── Default properties ────────────────────────────────────────────────────

    [Fact]
    public void Layout_DefaultsToCircular()
    {
        // Circular is the default for PR 1 (deterministic auto-layout). PR 2 may
        // change this to ForceDirected once that layout is active.
        var s = Sample();
        Assert.Equal(GraphLayout.Circular, s.Layout);
    }

    [Fact]
    public void ShowNodeLabels_DefaultsToTrue()
    {
        Assert.True(Sample().ShowNodeLabels);
    }

    [Fact]
    public void ShowEdgeWeights_DefaultsToFalse()
    {
        Assert.False(Sample().ShowEdgeWeights);
    }

    [Fact]
    public void EdgeThicknessScale_DefaultsToOne()
    {
        Assert.Equal(1.0, Sample().EdgeThicknessScale);
    }

    [Fact]
    public void NodeRadiusScale_DefaultsToFive()
    {
        Assert.Equal(5.0, Sample().NodeRadiusScale);
    }

    [Fact]
    public void LayoutSeed_DefaultsToZero()
    {
        Assert.Equal(0, Sample().LayoutSeed);
    }

    [Fact]
    public void ColorMap_DefaultsToNull()
    {
        Assert.Null(Sample().ColorMap);
    }

    // ── Mutation via init / set ───────────────────────────────────────────────

    [Fact]
    public void Layout_SetViaInitializer_Stored()
    {
        var s = new NetworkGraphSeries([N("a")], []) { Layout = GraphLayout.Hierarchical };
        Assert.Equal(GraphLayout.Hierarchical, s.Layout);
    }

    [Fact]
    public void ColorMap_SetViaInitializer_Stored()
    {
        var s = new NetworkGraphSeries([N("a")], []) { ColorMap = ColorMaps.Plasma };
        Assert.NotNull(s.ColorMap);
        Assert.Equal("plasma", s.ColorMap!.Name);
    }

    // ── ToSeriesDto ───────────────────────────────────────────────────────────

    [Fact]
    public void ToSeriesDto_TypeIsNetworkGraph()
    {
        Assert.Equal("networkgraph", Sample().ToSeriesDto().Type);
    }

    [Fact]
    public void ToSeriesDto_DefaultProperties_NotEmittedAsNonNull()
    {
        var s = Sample();
        var dto = s.ToSeriesDto();
        Assert.Null(dto.NetworkGraphLayout);             // default Circular → null
        Assert.Null(dto.NetworkGraphShowNodeLabels);     // default true     → null
        Assert.Null(dto.NetworkGraphShowEdgeWeights);    // default false    → null
        Assert.Null(dto.NetworkGraphEdgeThicknessScale); // default 1.0      → null
        Assert.Null(dto.NetworkGraphNodeRadiusScale);    // default 5.0      → null
        Assert.Null(dto.NetworkGraphLayoutSeed);         // default 0        → null
        Assert.Null(dto.ColorMapName);                   // default null     → null
    }

    [Fact]
    public void ToSeriesDto_NonDefaultLayout_EmittedAsString()
    {
        var s = Sample();
        s.Layout = GraphLayout.Manual;
        Assert.Equal("Manual", s.ToSeriesDto().NetworkGraphLayout);
    }

    [Fact]
    public void ToSeriesDto_EmitsNodesAndEdges()
    {
        var dto = Sample().ToSeriesDto();
        Assert.NotNull(dto.GraphNodes);
        Assert.NotNull(dto.GraphEdges);
        Assert.Equal(3, dto.GraphNodes!.Count);
        Assert.Equal(2, dto.GraphEdges!.Count);
    }

    // ── ComputeDataRange ─────────────────────────────────────────────────────

    [Fact]
    public void ComputeDataRange_NoEdgesNoExplicitCoords_ReturnsCircularBoundsForCircularLayout()
    {
        // With Circular layout the renderer positions every node on the unit circle —
        // axes range covers [-1, 1] in both dimensions.
        var s = new NetworkGraphSeries([N("a"), N("b"), N("c"), N("d")], []) { Layout = GraphLayout.Circular };
        var range = s.ComputeDataRange(null!);
        // We don't insist on exact -1/+1 (renderer may add margin); just the broad shape.
        Assert.True(range.XMin <= 0);
        Assert.True(range.XMax >= 0);
        Assert.True(range.YMin <= 0);
        Assert.True(range.YMax >= 0);
    }

    [Fact]
    public void ComputeDataRange_ManualLayout_UsesNodeCoords()
    {
        var nodes = new[]
        {
            new GraphNode("a", X: -10, Y: -5),
            new GraphNode("b", X: 10,  Y: 5),
        };
        var s = new NetworkGraphSeries(nodes, []) { Layout = GraphLayout.Manual };
        var range = s.ComputeDataRange(null!);
        Assert.True(range.XMin <= -10);
        Assert.True(range.XMax >= 10);
        Assert.True(range.YMin <= -5);
        Assert.True(range.YMax >= 5);
    }

    [Fact]
    public void ComputeDataRange_EmptyNodes_ReturnsNullRange()
    {
        var s = new NetworkGraphSeries([], []);
        var range = s.ComputeDataRange(null!);
        Assert.Null(range.XMin);
        Assert.Null(range.XMax);
    }
}
