// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Serialization;

/// <summary>v1.10 — JSON round-trip coverage for <see cref="NetworkGraphSeries"/>.</summary>
public class NetworkGraphSerializationTests
{
    private static readonly GraphNode[] SampleNodes =
    [
        new GraphNode("a", X: 1, Y: 2, ColorScalar: 0.3, SizeScalar: 1.5, Label: "Alpha"),
        new GraphNode("b", X: 3, Y: 4, ColorScalar: 0.7, SizeScalar: 2.0),
    ];

    private static readonly GraphEdge[] SampleEdges =
    [
        new GraphEdge("a", "b", Weight: 1.5, IsDirected: true),
    ];

    private static NetworkGraphSeries Roundtrip(Action<NetworkGraphSeries>? configure = null)
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.NetworkGraph(SampleNodes, SampleEdges, configure))
            .Build();
        var json = new ChartSerializer().ToJson(figure);
        var restored = new ChartSerializer().FromJson(json);
        return restored.SubPlots[0].Series.OfType<NetworkGraphSeries>().First();
    }

    // ── Type tag ─────────────────────────────────────────────────────────────

    [Fact]
    public void RoundTrip_TypeTagIsNetworkGraph()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.NetworkGraph(SampleNodes, SampleEdges))
            .Build();
        var json = new ChartSerializer().ToJson(figure);
        Assert.Contains("\"type\":\"networkgraph\"", json);
    }

    // ── Defaults not emitted ─────────────────────────────────────────────────

    [Fact]
    public void RoundTrip_DefaultProperties_NotEmittedToJson()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.NetworkGraph(SampleNodes, SampleEdges))
            .Build();
        var json = new ChartSerializer().ToJson(figure);
        Assert.DoesNotContain("\"networkGraphLayout\"", json);
        Assert.DoesNotContain("\"networkGraphShowNodeLabels\"", json);
        Assert.DoesNotContain("\"networkGraphShowEdgeWeights\"", json);
        Assert.DoesNotContain("\"networkGraphEdgeThicknessScale\"", json);
        Assert.DoesNotContain("\"networkGraphNodeRadiusScale\"", json);
        Assert.DoesNotContain("\"networkGraphLayoutSeed\"", json);
        Assert.DoesNotContain("\"colorMapName\"", json);
    }

    // ── Nodes / edges ────────────────────────────────────────────────────────

    [Fact]
    public void RoundTrip_PreservesNodeData()
    {
        var s = Roundtrip();
        Assert.Equal(2, s.Nodes.Count);
        Assert.Equal("a",     s.Nodes[0].Id);
        Assert.Equal(1,       s.Nodes[0].X);
        Assert.Equal(2,       s.Nodes[0].Y);
        Assert.Equal(0.3,     s.Nodes[0].ColorScalar);
        Assert.Equal(1.5,     s.Nodes[0].SizeScalar);
        Assert.Equal("Alpha", s.Nodes[0].Label);
    }

    [Fact]
    public void RoundTrip_PreservesEdgeData()
    {
        var s = Roundtrip();
        Assert.Single(s.Edges);
        Assert.Equal("a",  s.Edges[0].From);
        Assert.Equal("b",  s.Edges[0].To);
        Assert.Equal(1.5,  s.Edges[0].Weight);
        Assert.True(       s.Edges[0].IsDirected);
    }

    // ── Property round-trip ──────────────────────────────────────────────────

    [Fact]
    public void RoundTrip_PreservesLayout_Hierarchical()
    {
        var s = Roundtrip(s => s.Layout = GraphLayout.Hierarchical);
        Assert.Equal(GraphLayout.Hierarchical, s.Layout);
    }

    [Fact]
    public void RoundTrip_PreservesLayout_Manual()
    {
        var s = Roundtrip(s => s.Layout = GraphLayout.Manual);
        Assert.Equal(GraphLayout.Manual, s.Layout);
    }

    [Fact]
    public void RoundTrip_PreservesLayout_ForceDirected_ReservedOrdinal()
    {
        // ForceDirected = 1 is reserved (PR 2 activates it). DTO must round-trip the
        // enum value cleanly even before the layout body is implemented.
        var s = Roundtrip(s => s.Layout = GraphLayout.ForceDirected);
        Assert.Equal(GraphLayout.ForceDirected, s.Layout);
    }

    [Fact]
    public void RoundTrip_PreservesShowNodeLabels_False()
    {
        var s = Roundtrip(s => s.ShowNodeLabels = false);
        Assert.False(s.ShowNodeLabels);
    }

    [Fact]
    public void RoundTrip_PreservesShowEdgeWeights_True()
    {
        var s = Roundtrip(s => s.ShowEdgeWeights = true);
        Assert.True(s.ShowEdgeWeights);
    }

    [Fact]
    public void RoundTrip_PreservesEdgeThicknessScale()
    {
        var s = Roundtrip(s => s.EdgeThicknessScale = 3.5);
        Assert.Equal(3.5, s.EdgeThicknessScale);
    }

    [Fact]
    public void RoundTrip_PreservesNodeRadiusScale()
    {
        var s = Roundtrip(s => s.NodeRadiusScale = 12.0);
        Assert.Equal(12.0, s.NodeRadiusScale);
    }

    [Fact]
    public void RoundTrip_PreservesLayoutSeed()
    {
        var s = Roundtrip(s => s.LayoutSeed = 42);
        Assert.Equal(42, s.LayoutSeed);
    }

    [Fact]
    public void RoundTrip_PreservesLayoutIterations()
    {
        var s = Roundtrip(s => s.LayoutIterations = 200);
        Assert.Equal(200, s.LayoutIterations);
    }

    [Fact]
    public void RoundTrip_PreservesConvergenceThreshold()
    {
        var s = Roundtrip(s => s.ConvergenceThreshold = 0.001);
        Assert.Equal(0.001, s.ConvergenceThreshold);
    }

    [Fact]
    public void RoundTrip_PreservesColorMap()
    {
        var s = Roundtrip(s => s.ColorMap = ColorMaps.Plasma);
        Assert.NotNull(s.ColorMap);
        Assert.Equal("plasma", s.ColorMap!.Name);
    }

    // ── Empty graph round-trips cleanly ──────────────────────────────────────

    [Fact]
    public void RoundTrip_EmptyNodesAndEdges_PreservesShape()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.NetworkGraph([], []))
            .Build();
        var json = new ChartSerializer().ToJson(figure);
        var restored = new ChartSerializer().FromJson(json);
        var s = restored.SubPlots[0].Series.OfType<NetworkGraphSeries>().First();
        Assert.Empty(s.Nodes);
        Assert.Empty(s.Edges);
    }
}
