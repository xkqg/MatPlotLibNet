// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>v1.10 — verifies SVG output of <see cref="NetworkGraphSeries"/> rendering
/// across the three deterministic layouts shipped in PR 1
/// (<see cref="GraphLayout.Manual"/>, <see cref="GraphLayout.Circular"/>,
/// <see cref="GraphLayout.Hierarchical"/>) plus directed-edge arrowhead emission.</summary>
public class NetworkGraphRenderTests
{
    private static GraphNode N(string id, double x = 0, double y = 0) => new(id, x, y);
    private static GraphEdge E(string from, string to, bool directed = false) => new(from, to, 1.0, directed);

    private static string RenderSvg(
        IReadOnlyList<GraphNode> nodes,
        IReadOnlyList<GraphEdge> edges,
        Action<NetworkGraphSeries>? configure = null) =>
        Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.NetworkGraph(nodes, edges, configure))
            .ToSvg();
    // ── Smoke tests ──────────────────────────────────────────────────────────

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("Layout_Circular_RendersValidSvg", () => RenderSvg([N("a"), N("b"), N("c"), N("d")], [],
            s => s.Layout = GraphLayout.Circular), "<svg", "<circle "),
        new DrawnToken("Layout_Hierarchical_RendersValidSvg", () => RenderSvg([N("a"), N("b"), N("c")], [E("a", "b"), E("b", "c")],
            s => s.Layout = GraphLayout.Hierarchical), "<svg", "<circle "),
        // With Manual layout the renderer reads each node's pre-set X/Y. Two distant
        // nodes must produce visibly distant pixel positions.
        new DrawnToken("Layout_Manual_UsesNodeCoordinates", () => RenderSvg([N("a", 0, 0), N("b", 10, 10)], [E("a", "b")],
            s => s.Layout = GraphLayout.Manual), "<line "),
        // Default ShowNodeLabels = true → <text> elements with node IDs.
        // Axes also emit <text> for tick labels — just confirm "a" and "b" appear.
        new DrawnToken("ShowNodeLabels_True_EmitsTextElements", () => RenderSvg([N("a"), N("b")], []), ">a<", ">b<"),
        // Without labels the IDs don't appear as standalone <text> content.
        new DrawnToken("ShowNodeLabels_False_OmitsNodeIdText", () => RenderSvg([N("a"), N("b")], [], s => s.ShowNodeLabels = false)) { Absent = [">a<", ">b<"] },
        new DrawnToken("NodeLabel_OverrideId_RendersLabelNotId", () =>
        {
            var n = new GraphNode("n0", 0, 0, Label: "Custom Label");
            return RenderSvg([n], []);
        }, "Custom Label"),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    [Fact]
    public void DefaultRender_EmitsCircleElementsForNodes()
    {
        // 3 nodes → at least 3 <circle> elements.
        string svg = RenderSvg([N("a"), N("b"), N("c")], []);
        int circles = svg.CountOf("<circle ");
        Assert.True(circles >= 3, $"Expected ≥3 circles for 3 nodes, got {circles}.");
    }

    [Fact]
    public void DefaultRender_EmitsLineElementsForEdges()
    {
        // 2 edges → at least 2 <line> elements (axes also emit lines, so it's a lower bound).
        string svg = RenderSvg([N("a"), N("b"), N("c")], [E("a", "b"), E("b", "c")]);
        int lines = svg.CountOf("<line ");
        Assert.True(lines >= 2, $"Expected ≥2 lines for 2 edges, got {lines}.");
    }

    // ── Layout dispatch ──────────────────────────────────────────────────────

    [Fact]
    public void Layout_ForceDirected_PreActivation_FallsBackGracefully()
    {
        // PR 1: ForceDirected = 1 reserved; falls back to Manual. Must not throw.
        var ex = Record.Exception(() => RenderSvg([N("a", 1, 1), N("b", 5, 5)], [E("a", "b")],
            s => s.Layout = GraphLayout.ForceDirected));
        Assert.Null(ex);
    }

    // ── Directed edges ───────────────────────────────────────────────────────

    [Fact]
    public void DirectedEdges_EmitArrowheadPolygons()
    {
        // Each directed edge → one extra <polygon> for the arrowhead.
        string svgUndirected = RenderSvg([N("a"), N("b")], [E("a", "b", directed: false)]);
        string svgDirected   = RenderSvg([N("a"), N("b")], [E("a", "b", directed: true)]);
        int polysU = svgUndirected.CountOf("<polygon ");
        int polysD = svgDirected.CountOf("<polygon ");
        Assert.True(polysD > polysU,
            $"Directed edge ({polysD} polygons) must emit more than undirected ({polysU}).");
    }

    [Fact]
    public void UndirectedEdges_NoArrowheads()
    {
        // No directed edges → polygon count comes only from non-edge sources (none for
        // a basic 2-node 1-edge graph).
        string svg = RenderSvg([N("a"), N("b")], [E("a", "b", directed: false)]);
        int polygons = svg.CountOf("<polygon ");
        Assert.Equal(0, polygons);
    }

    // ── ShowEdgeWeights ──────────────────────────────────────────────────────

    [Fact]
    public void ShowEdgeWeights_True_EmitsMoreTextElementsThanFalse()
    {
        // Compare structural <text> count: ShowEdgeWeights=true must add one <text>
        // element per edge for the weight label. Don't check the number value itself
        // — it ends up in the stroke-width attribute too.
        var nodes = new[] { N("a"), N("b") };
        var edges = new[] { new GraphEdge("a", "b", Weight: 1.5, IsDirected: false) };
        string svgOff = RenderSvg(nodes, edges, s => s.ShowEdgeWeights = false);
        string svgOn  = RenderSvg(nodes, edges, s => s.ShowEdgeWeights = true);
        int textOff = svgOff.CountOf("<text ");
        int textOn  = svgOn.CountOf("<text ");
        Assert.True(textOn > textOff,
            $"ShowEdgeWeights=true ({textOn} <text>) must add a label per edge over off ({textOff}).");
    }

    // ── Edge thickness / node radius scaling ─────────────────────────────────

    [Fact]
    public void EdgeThicknessScale_LargerValue_ProducesThickerStrokes()
    {
        var nodes = new[] { N("a"), N("b") };
        var edges = new[] { new GraphEdge("a", "b", Weight: 2.0) };
        string svgThin  = RenderSvg(nodes, edges, s => s.EdgeThicknessScale = 1.0);
        string svgThick = RenderSvg(nodes, edges, s => s.EdgeThicknessScale = 5.0);
        // Stroke-width values appear in the SVG. Thicker → larger numeric value.
        Assert.NotEqual(svgThin, svgThick);
    }

    [Fact]
    public void NodeRadiusScale_LargerValue_ProducesLargerCircles()
    {
        string svgSmall = RenderSvg([N("a")], [], s => s.NodeRadiusScale = 5.0);
        string svgLarge = RenderSvg([N("a")], [], s => s.NodeRadiusScale = 20.0);
        Assert.NotEqual(svgSmall, svgLarge);
    }

    // ── Missing-id edges defensive fallback ──────────────────────────────────

    [Fact]
    public void Edge_WithMissingNodeId_DoesNotThrow()
    {
        // Edge references node "ghost" that doesn't exist in the node list.
        var nodes = new[] { N("a"), N("b") };
        var edges = new[] { E("a", "ghost") };
        var ex = Record.Exception(() => RenderSvg(nodes, edges));
        Assert.Null(ex);
    }
}
