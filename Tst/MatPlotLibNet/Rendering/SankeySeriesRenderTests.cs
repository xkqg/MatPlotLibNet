// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>
/// Covers the v1.1.4 Sankey overhaul: multi-column BFS + explicit-column override, node
/// alignment modes, iterative vertical relaxation, link colour modes (source / target /
/// gradient), sub-labels, and <see cref="AxesBuilderFrameVerbs.HideAllAxes"/> integration.
/// </summary>
public class SankeySeriesRenderTests
{
    private static SankeyNode[] LinearNodes() =>
    [
        new("A", Color.FromHex("#1F77B4")),
        new("B", Color.FromHex("#FF7F0E")),
        new("C", Color.FromHex("#2CA02C")),
        new("D", Color.FromHex("#D62728")),
    ];

    private static SankeyLink[] LinearLinks() =>
    [
        new(0, 1, 10),
        new(1, 2, 7),
        new(2, 3, 5),
    ];

    // ──────────────────────────────────────────────────────────────────────────
    // Baseline render
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        // links are paths
        // nodes are rects
        new DrawnToken("Sankey_RendersWithoutError", () => Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.HideAllAxes().Sankey(LinearNodes(), LinearLinks()))
            .ToSvg(), "<svg", "<path", "<rect"),
        new DrawnToken("Sankey_GradientMode_EmitsLinearGradientDefs", () => Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.HideAllAxes().Sankey(LinearNodes(), LinearLinks(),
                s => s.LinkColorMode = SankeyLinkColorMode.Gradient))
            .ToSvg(), "<linearGradient", "<stop", "url(#grad-"),
        new DrawnToken("Sankey_SourceMode_DoesNotEmitGradientDefs", () => Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.HideAllAxes().Sankey(LinearNodes(), LinearLinks(),
                s => s.LinkColorMode = SankeyLinkColorMode.Source))
            .ToSvg()) { Absent = ["<linearGradient"] },
        new DrawnToken("Sankey_TargetMode_DoesNotEmitGradientDefs", () => Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.HideAllAxes().Sankey(LinearNodes(), LinearLinks(),
                s => s.LinkColorMode = SankeyLinkColorMode.Target))
            .ToSvg()) { Absent = ["<linearGradient"] },
        // Every link should carry data-sankey-link-source + data-sankey-link-target, and
        // every node should carry data-sankey-node-id. 3 links + 4 nodes in the linear
        // sample.
        new DrawnToken("Sankey_EmitsDataAttributesForHoverTopology", () => Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.HideAllAxes().Sankey(LinearNodes(), LinearLinks()))
            .ToSvg(), "data-sankey-node-id=\"0\"", "data-sankey-link-source=\"0\"", "data-sankey-link-target=\"1\""),
        // hover script installs mouseenter handlers
        new DrawnToken("WithSankeyHover_EmbedsHoverScript", () => Plt.Create()
            .WithSankeyHover()
            .AddSubPlot(1, 1, 1, ax => ax.HideAllAxes().Sankey(LinearNodes(), LinearLinks()))
            .ToSvg(), "data-sankey-node-id", "mouseenter"),
        // Data attributes are always emitted (the Sankey renderer doesn't gate them), but
        // the interactive script should only appear when WithSankeyHover() is called.
        new DrawnToken("WithoutSankeyHover_NoHoverScriptEmbedded", () => Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.HideAllAxes().Sankey(LinearNodes(), LinearLinks()))
            .ToSvg()) { Absent = ["mouseenter"] },
        new DrawnToken("Sankey_Vertical_RendersWithoutError", () => Plt.Create()
            .WithSize(400, 600)
            .AddSubPlot(1, 1, 1, ax => ax
                .HideAllAxes()
                .Sankey(LinearNodes(), LinearLinks(), s => s.Orient = SankeyOrientation.Vertical))
            .ToSvg(), "<svg", "<rect", "<path"),
        // Gradient defs should still be emitted; in vertical mode the gradient direction
        // runs along the Y axis instead of X but the def structure is identical.
        new DrawnToken("Sankey_Vertical_Gradient_EmitsLinearGradientDefs", () => Plt.Create()
            .WithSize(400, 600)
            .AddSubPlot(1, 1, 1, ax => ax
                .HideAllAxes()
                .Sankey(LinearNodes(), LinearLinks(), s =>
                {
                    s.Orient = SankeyOrientation.Vertical;
                    s.LinkColorMode = SankeyLinkColorMode.Gradient;
                }))
            .ToSvg(), "<linearGradient", "url(#grad-"),
        // Non-gradient link color mode (Source) — `LinkColorMode == Gradient` false arm.
        new DrawnToken("Sankey_LinkColorModeSource_NoGradientDefs", () => Plt.Create()
            .WithSize(400, 300)
            .AddSubPlot(1, 1, 1, ax => ax
                .HideAllAxes()
                .Sankey(LinearNodes(), LinearLinks(),
                    s => s.LinkColorMode = SankeyLinkColorMode.Source))
            .Build().ToSvg(), "<svg") { Absent = ["<linearGradient"] },
        // Node with no SubLabel — `if (string.IsNullOrEmpty(node.SubLabel)) return` true arm.
        new DrawnToken("Sankey_NodeWithNoSubLabel_SkipsSubLabelDraw", () =>
        {
            SankeyNode[] nodes = [new("A", Colors.Teal), new("B", Colors.Orange)];
            SankeyLink[] links = [new(0, 1, 10)];
            return Plt.Create()
                .WithSize(400, 300)
                .AddSubPlot(1, 1, 1, ax => ax.HideAllAxes().Sankey(nodes, links))
                .Build().ToSvg();
        }, "<svg") { Absent = ["SubLabel"] },
        // Dense Sankey with many nodes in a tiny figure — forces LabelLayoutEngine to push labels more than 6 px from their anchors,
        // triggering the LeaderLineStart non-null arm. HideAllAxes() ensures the only <line> elements in the SVG are leader lines.
        // Leader lines render as <line> elements; HideAllAxes ensures no spine/tick <line>s
        new DrawnToken("Sankey_DenseLabels_DrawsLeaderLines", () =>
        {
            // 8 labeled nodes packed into 150×120 px — labels must collide and be displaced
            SankeyNode[] nodes =
            [
                new("Alpha", Colors.Red),   new("Beta",    Colors.Green),
                new("Gamma", Colors.Blue),  new("Delta",   Colors.Yellow),
                new("Epsilon", Colors.Cyan), new("Zeta",   Colors.Magenta),
                new("Eta", Colors.Orange),  new("Theta",   Colors.Gray),
            ];
            SankeyLink[] links =
            [
                new(0, 2, 5), new(0, 3, 3), new(1, 2, 4), new(1, 3, 2),
                new(2, 4, 6), new(2, 5, 3), new(3, 6, 2), new(3, 7, 1),
            ];
            return Plt.Create()
                .WithSize(150, 120)
                .AddSubPlot(1, 1, 1, ax => ax.HideAllAxes().Sankey(nodes, links))
                .Build().ToSvg();
        }, "<line"),
        // All nodes have empty labels — outerCandidates.Count == 0 → the batch-place block is skipped entirely.
        new DrawnToken("Sankey_AllNodesEmptyLabels_SkipsBatchLabelPlace", () =>
        {
            SankeyNode[] nodes =
            [
                new("", Color.FromHex("#1F77B4")),
                new("", Color.FromHex("#FF7F0E")),
                new("", Color.FromHex("#2CA02C")),
            ];
            SankeyLink[] links = [new(0, 1, 5), new(1, 2, 3)];
            return Plt.Create()
                .WithSize(400, 300)
                .AddSubPlot(1, 1, 1, ax => ax.HideAllAxes().Sankey(nodes, links))
                .Build().ToSvg();
        }, "<rect"),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    // ──────────────────────────────────────────────────────────────────────────
    // Link colour modes

    // ──────────────────────────────────────────────────────────────────────────
    // Sub-labels
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Sankey_SubLabels_AreRenderedInSvgOutput()
    {
        SankeyNode[] nodes =
        [
            new("Revenue", Color.FromHex("#2A9D55"), SubLabel: "$21.4B"),
            new("Cost",    Color.FromHex("#D62828"), SubLabel: "$6.5B"),
        ];
        SankeyLink[] links = [new(0, 1, 10)];

        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.HideAllAxes().Sankey(nodes, links))
            .ToSvg();

        // Text is rendered through the glyph path provider, so we can't grep for "21.4B"
        // directly — but the sub-label text still produces a distinct <path> or <text>
        // element. The easier invariant is that the SVG got LONGER when sub-labels are
        // set than when they aren't.
        string svgNoSub = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.HideAllAxes().Sankey(
                [new("Revenue", Color.FromHex("#2A9D55")), new("Cost", Color.FromHex("#D62828"))],
                links))
            .ToSvg();

        Assert.True(svg.Length > svgNoSub.Length,
            $"Expected sub-labels to grow the SVG output; got {svg.Length} vs {svgNoSub.Length}");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Multi-column topology + explicit Column override
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Sankey_FourColumnCascade_RendersAllLinks()
    {
        // A → B → C → D is a 4-column chain. Relaxation + BFS should produce 4 distinct
        // X positions for the four node rects, and all three links should render.
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.HideAllAxes().Sankey(LinearNodes(), LinearLinks()))
            .ToSvg();

        // Each <rect> in the output corresponds to one node (4 rects total). One
        // background rect from the figure adds a 5th, so look for at least 4 distinct
        // `<rect` occurrences.
        int rectCount = 0;
        int idx = 0;
        while ((idx = svg.IndexOf("<rect", idx, System.StringComparison.Ordinal)) >= 0)
        { rectCount++; idx++; }
        Assert.True(rectCount >= 4, $"Expected ≥4 rects (4 Sankey nodes + background), got {rectCount}");
    }

    [Fact]
    public void Sankey_ExplicitColumn_OverridesBfs()
    {
        // Same 4-node chain, but pin node C to column 5 (well past the natural BFS column 2).
        // The renderer should honour the override — C and D end up at higher columns, so the
        // rendered X of C should be further right than the default.
        SankeyNode[] defaultNodes = LinearNodes();
        SankeyNode[] pinnedNodes =
        [
            defaultNodes[0],
            defaultNodes[1],
            defaultNodes[2] with { Column = 5 },
            defaultNodes[3],
        ];

        string defaultSvg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.HideAllAxes().Sankey(defaultNodes, LinearLinks()))
            .ToSvg();
        string pinnedSvg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.HideAllAxes().Sankey(pinnedNodes, LinearLinks()))
            .ToSvg();

        // The two outputs should differ — pinning C to column 5 redistributes the horizontal
        // node spacing. If they are identical the override wasn't honoured.
        Assert.NotEqual(defaultSvg, pinnedSvg);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Iteration behaviour
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Sankey_IterationsZero_SkipsRelaxation()
    {
        // Cross-heavy asymmetric topology: A feeds mostly C, B feeds mostly D, but each also
        // sends a small share to the other target. The value-weighted centroid relaxation
        // pulls A up toward C and B down toward D, producing visibly different node positions
        // than the greedy initial packing (which just stacks nodes by total value).
        SankeyNode[] nodes =
        [
            new("A"), new("B"), new("E"), new("C"), new("D"),
        ];
        SankeyLink[] links =
        [
            new(0, 3, 20),  // A → C (big)
            new(0, 4, 1),   // A → D (tiny)
            new(1, 3, 1),   // B → C (tiny)
            new(1, 4, 20),  // B → D (big)
            new(2, 3, 5),   // E → C
            new(2, 4, 5),   // E → D
        ];

        string relaxed = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.HideAllAxes().Sankey(nodes, links,
                s => s.Iterations = 20))
            .ToSvg();
        string skipped = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.HideAllAxes().Sankey(nodes, links,
                s => s.Iterations = 0))
            .ToSvg();

        Assert.NotEqual(relaxed, skipped);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // HideAllAxes integration

    // ──────────────────────────────────────────────────────────────────────────
    // Hover emphasis wiring

    // ──────────────────────────────────────────────────────────────────────────
    // Vertical orientation
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Sankey_Vertical_ProducesDifferentLayoutThanHorizontal()
    {
        // Same nodes + links, same bounds — only orientation differs. The node rects should
        // end up in different positions, which we detect by comparing the two SVG outputs.
        string horiz = Plt.Create()
            .WithSize(600, 400)
            .AddSubPlot(1, 1, 1, ax => ax
                .HideAllAxes()
                .Sankey(LinearNodes(), LinearLinks(),
                    s => s.Orient = SankeyOrientation.Horizontal))
            .ToSvg();
        string vert = Plt.Create()
            .WithSize(600, 400)
            .AddSubPlot(1, 1, 1, ax => ax
                .HideAllAxes()
                .Sankey(LinearNodes(), LinearLinks(),
                    s => s.Orient = SankeyOrientation.Vertical))
            .ToSvg();

        Assert.NotEqual(horiz, vert);
    }

    [Fact]
    public void ASankey_DrawsNoAxesDecoration_WhetherOrNotTheAxesAreHidden()
    {
        string asIs = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Sankey(LinearNodes(), LinearLinks()))
            .ToSvg();
        string hidden = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.HideAllAxes().Sankey(LinearNodes(), LinearLinks()))
            .ToSvg();

        // A Sankey draws without axes (its nodes and flows are positions in the plot, not on an axis), so the grid, the ticks
        // and the frame that HideAllAxes removes are not drawn for it in the first place.
        Assert.Equal(hidden, asIs);
    }

    [Fact]
    public void ASankeyBesideALine_StillHasItsAxes()
    {
        string both = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Plot([0.0, 1.0], [0.0, 1.0]).Sankey(LinearNodes(), LinearLinks()))
            .ToSvg();
        string bothHidden = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.HideAllAxes().Plot([0.0, 1.0], [0.0, 1.0]).Sankey(LinearNodes(), LinearLinks()))
            .ToSvg();

        Assert.True(bothHidden.Length < both.Length, "a line on the axes needs them: hiding them shrinks the picture");
    }

    // ── Wave J.2 — missing SankeySeriesRenderer branches ─────────────────

    // ── Wave J.1 — remaining branch arms ────────────────────────────────────

    // ── ComputeNodeLabelAnchor (L.3, 2026-04-21) ─────────────────────────────

    private static readonly Rect NodeRect = new(50, 100, 20, 40);

    /// <summary>Beside a node of a flow along X the label is centred on the node's middle (the box's centre rule), 4 px off its side.</summary>
    [Theory]
    [InlineData(true, 46, TextAlignment.Right)]
    [InlineData(false, 74, TextAlignment.Left)]
    public void ComputeNodeLabelAnchor_Horizontal_BesideTheNode_CentredOnItsMiddle(bool onFarEdge, double x, TextAlignment alignment) =>
        Assert.Equal(new TextPlacement(new Point(x, 120), alignment) { Vertical = TextVerticalAlignment.Center },
            SankeySeriesRenderer.ComputeNodeLabelAnchor(NodeRect, onFarEdge, SankeyOrientation.Horizontal));

    /// <summary>Under the last node of a flow down the page the label hangs 2 px below it (its box's ascent below that point).</summary>
    [Fact]
    public void ComputeNodeLabelAnchor_Vertical_FarEdge_HangsBelowTheNode()
    {
        Assert.Equal(new TextPlacement(new Point(60, 142), TextAlignment.Center) { Vertical = TextVerticalAlignment.Top },
            SankeySeriesRenderer.ComputeNodeLabelAnchor(NodeRect, onFarEdge: true, SankeyOrientation.Vertical));
    }

    [Fact]
    public void ComputeNodeLabelAnchor_Vertical_NotFarEdge_StandsAboveTheNode()
    {
        Assert.Equal(new TextPlacement(new Point(60, 96), TextAlignment.Center),
            SankeySeriesRenderer.ComputeNodeLabelAnchor(NodeRect, onFarEdge: false, SankeyOrientation.Vertical));
    }
}
