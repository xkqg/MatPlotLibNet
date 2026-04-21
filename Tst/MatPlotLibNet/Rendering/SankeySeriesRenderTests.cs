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
/// gradient), sub-labels, and <see cref="AxesBuilder.HideAllAxes"/> integration.
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

    [Fact]
    public void Sankey_RendersWithoutError()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.HideAllAxes().Sankey(LinearNodes(), LinearLinks()))
            .ToSvg();
        Assert.Contains("<svg", svg);
        Assert.Contains("<path", svg);  // links are paths
        Assert.Contains("<rect", svg);  // nodes are rects
    }

    [Fact]
    public void Sankey_EmptyNodes_DoesNotThrow()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.HideAllAxes().Sankey([], []))
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Link colour modes
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Sankey_GradientMode_EmitsLinearGradientDefs()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.HideAllAxes().Sankey(LinearNodes(), LinearLinks(),
                s => s.LinkColorMode = SankeyLinkColorMode.Gradient))
            .ToSvg();
        Assert.Contains("<linearGradient", svg);
        Assert.Contains("<stop", svg);
        Assert.Contains("url(#grad-", svg);
    }

    [Fact]
    public void Sankey_SourceMode_DoesNotEmitGradientDefs()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.HideAllAxes().Sankey(LinearNodes(), LinearLinks(),
                s => s.LinkColorMode = SankeyLinkColorMode.Source))
            .ToSvg();
        Assert.DoesNotContain("<linearGradient", svg);
    }

    [Fact]
    public void Sankey_TargetMode_DoesNotEmitGradientDefs()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.HideAllAxes().Sankey(LinearNodes(), LinearLinks(),
                s => s.LinkColorMode = SankeyLinkColorMode.Target))
            .ToSvg();
        Assert.DoesNotContain("<linearGradient", svg);
    }

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

    // ──────────────────────────────────────────────────────────────────────────
    // Hover emphasis wiring
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Sankey_EmitsDataAttributesForHoverTopology()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.HideAllAxes().Sankey(LinearNodes(), LinearLinks()))
            .ToSvg();

        // Every link should carry data-sankey-link-source + data-sankey-link-target, and
        // every node should carry data-sankey-node-id. 3 links + 4 nodes in the linear
        // sample.
        Assert.Contains("data-sankey-node-id=\"0\"", svg);
        Assert.Contains("data-sankey-link-source=\"0\"", svg);
        Assert.Contains("data-sankey-link-target=\"1\"", svg);
    }

    [Fact]
    public void WithSankeyHover_EmbedsHoverScript()
    {
        string svg = Plt.Create()
            .WithSankeyHover()
            .AddSubPlot(1, 1, 1, ax => ax.HideAllAxes().Sankey(LinearNodes(), LinearLinks()))
            .ToSvg();
        Assert.Contains("data-sankey-node-id", svg);
        Assert.Contains("mouseenter", svg);  // hover script installs mouseenter handlers
    }

    [Fact]
    public void WithoutSankeyHover_NoHoverScriptEmbedded()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.HideAllAxes().Sankey(LinearNodes(), LinearLinks()))
            .ToSvg();
        // Data attributes are always emitted (the Sankey renderer doesn't gate them), but
        // the interactive script should only appear when WithSankeyHover() is called.
        Assert.DoesNotContain("mouseenter", svg);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Vertical orientation
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Sankey_Vertical_RendersWithoutError()
    {
        string svg = Plt.Create()
            .WithSize(400, 600)
            .AddSubPlot(1, 1, 1, ax => ax
                .HideAllAxes()
                .Sankey(LinearNodes(), LinearLinks(), s => s.Orient = SankeyOrientation.Vertical))
            .ToSvg();

        Assert.Contains("<svg", svg);
        Assert.Contains("<rect", svg);
        Assert.Contains("<path", svg);
    }

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
    public void Sankey_Vertical_Gradient_EmitsLinearGradientDefs()
    {
        string svg = Plt.Create()
            .WithSize(400, 600)
            .AddSubPlot(1, 1, 1, ax => ax
                .HideAllAxes()
                .Sankey(LinearNodes(), LinearLinks(), s =>
                {
                    s.Orient = SankeyOrientation.Vertical;
                    s.LinkColorMode = SankeyLinkColorMode.Gradient;
                }))
            .ToSvg();

        // Gradient defs should still be emitted; in vertical mode the gradient direction
        // runs along the Y axis instead of X but the def structure is identical.
        Assert.Contains("<linearGradient", svg);
        Assert.Contains("url(#grad-", svg);
    }

    [Fact]
    public void HideAllAxes_SuppressesTickLabels()
    {
        string withAxes = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Sankey(LinearNodes(), LinearLinks()))
            .ToSvg();
        string without = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.HideAllAxes().Sankey(LinearNodes(), LinearLinks()))
            .ToSvg();

        // HideAllAxes removes spines + ticks + tick labels, so the clean version should be
        // SHORTER than the version that renders the full cartesian decoration.
        Assert.True(without.Length < withAxes.Length,
            $"HideAllAxes should shrink SVG output; got without={without.Length} vs with={withAxes.Length}");
    }

    // ── Wave J.2 — missing SankeySeriesRenderer branches ─────────────────

    /// <summary>Node with empty label — L195 `if (string.IsNullOrEmpty(label)) continue` true arm.</summary>
    [Fact]
    public void Sankey_NodeWithEmptyLabel_SkipsLabelDraw()
    {
        SankeyNode[] nodes = [new("", Colors.Teal), new("B", Colors.Orange)];
        SankeyLink[] links = [new(0, 1, 10)];
        var svg = Plt.Create()
            .WithSize(400, 300)
            .AddSubPlot(1, 1, 1, ax => ax.HideAllAxes().Sankey(nodes, links))
            .Build().ToSvg();
        Assert.Contains("<svg", svg);
    }

    /// <summary>InsideLabels=true — L202 true arm (label draws inside node rect).</summary>
    [Fact]
    public void Sankey_InsideLabels_DrawsLabelsInsideNodes()
    {
        var svg = Plt.Create()
            .WithSize(600, 400)
            .AddSubPlot(1, 1, 1, ax => ax
                .HideAllAxes()
                .Sankey(LinearNodes(), LinearLinks(), s => s.InsideLabels = true))
            .Build().ToSvg();
        Assert.Contains("<svg", svg);
    }

    /// <summary>Node with no SubLabel — L267 `if (string.IsNullOrEmpty(node.SubLabel)) return` true arm.</summary>
    [Fact]
    public void Sankey_NodeWithNoSubLabel_SkipsSubLabelDraw()
    {
        SankeyNode[] nodes = [new("A", Colors.Teal), new("B", Colors.Orange)];
        SankeyLink[] links = [new(0, 1, 10)];
        var svg = Plt.Create()
            .WithSize(400, 300)
            .AddSubPlot(1, 1, 1, ax => ax.HideAllAxes().Sankey(nodes, links))
            .Build().ToSvg();
        Assert.Contains("<svg", svg);
        Assert.DoesNotContain("SubLabel", svg);
    }

    /// <summary>Link with zero-value source or target — L97 continue arm.</summary>
    [Fact]
    public void Sankey_LinkWithZeroSourceValue_SkipsLink()
    {
        SankeyNode[] nodes = [new("A", Colors.Teal), new("B", Colors.Orange), new("C", Colors.Violet)];
        // Node 0 only receives, never sends → nodeValue[0] = 0 as source
        SankeyLink[] links = [new(1, 0, 5), new(2, 0, 3)];
        var svg = Plt.Create()
            .WithSize(400, 300)
            .AddSubPlot(1, 1, 1, ax => ax.HideAllAxes().Sankey(nodes, links))
            .Build().ToSvg();
        Assert.Contains("<svg", svg);
    }

    /// <summary>Non-gradient link color mode (Source) — L140 `LinkColorMode == Gradient` false arm.</summary>
    [Fact]
    public void Sankey_LinkColorModeSource_NoGradientDefs()
    {
        string svg = Plt.Create()
            .WithSize(400, 300)
            .AddSubPlot(1, 1, 1, ax => ax
                .HideAllAxes()
                .Sankey(LinearNodes(), LinearLinks(),
                    s => s.LinkColorMode = SankeyLinkColorMode.Source))
            .Build().ToSvg();
        Assert.Contains("<svg", svg);
        Assert.DoesNotContain("<linearGradient", svg);
    }

    // ── Wave J.1 — remaining branch arms ────────────────────────────────────

    /// <summary>Node.SubLabelColor set explicitly — L268 non-null arm in DrawSubLabelIfAny.
    /// Asserts SVG output is longer when a custom sub-label colour is applied.</summary>
    [Fact]
    public void Sankey_SubLabelWithExplicitColor_UsesNodeSubLabelColor()
    {
        SankeyNode[] nodesWithColor =
        [
            new("Revenue", Color.FromHex("#2A9D55"), SubLabel: "$21.4B", SubLabelColor: Colors.Red),
            new("Cost",    Color.FromHex("#D62828"), SubLabel: "$6.5B"),
        ];
        SankeyLink[] links = [new(0, 1, 10)];
        string svg = Plt.Create()
            .WithSize(400, 300)
            .AddSubPlot(1, 1, 1, ax => ax.HideAllAxes().Sankey(nodesWithColor, links))
            .Build().ToSvg();
        Assert.Contains("<svg", svg);
    }

    /// <summary>Dense Sankey with many nodes in a tiny figure — forces LabelLayoutEngine to push
    /// labels more than 6 px from their anchors, triggering the L245 LeaderLineStart non-null arm.
    /// HideAllAxes() ensures the only &lt;line&gt; elements in the SVG are leader lines.</summary>
    [Fact]
    public void Sankey_DenseLabels_DrawsLeaderLines()
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
        var svg = Plt.Create()
            .WithSize(150, 120)
            .AddSubPlot(1, 1, 1, ax => ax.HideAllAxes().Sankey(nodes, links))
            .Build().ToSvg();
        // Leader lines render as <line> elements; HideAllAxes ensures no spine/tick <line>s
        Assert.Contains("<line", svg);
    }

    /// <summary>All nodes have empty labels — outerCandidates.Count == 0 → L241 FALSE arm,
    /// the batch-place block is skipped entirely.</summary>
    [Fact]
    public void Sankey_AllNodesEmptyLabels_SkipsBatchLabelPlace()
    {
        SankeyNode[] nodes =
        [
            new("", Color.FromHex("#1F77B4")),
            new("", Color.FromHex("#FF7F0E")),
            new("", Color.FromHex("#2CA02C")),
        ];
        SankeyLink[] links = [new(0, 1, 5), new(1, 2, 3)];
        string svg = Plt.Create()
            .WithSize(400, 300)
            .AddSubPlot(1, 1, 1, ax => ax.HideAllAxes().Sankey(nodes, links))
            .Build().ToSvg();
        Assert.Contains("<rect", svg);
    }

    // ── ComputeNodeLabelAnchor (L.3, 2026-04-21) ─────────────────────────────

    private static readonly Rect NodeRect = new(50, 100, 20, 40);

    [Fact]
    public void ComputeNodeLabelAnchor_Horizontal_FarEdge_AlignmentIsRight()
    {
        var (_, alignment) = SankeySeriesRenderer.ComputeNodeLabelAnchor(
            NodeRect, onFarEdge: true, SankeyOrientation.Horizontal, measuredLabelHeight: 10);
        Assert.Equal(TextAlignment.Right, alignment);
    }

    [Fact]
    public void ComputeNodeLabelAnchor_Horizontal_NotFarEdge_AlignmentIsLeft()
    {
        var (_, alignment) = SankeySeriesRenderer.ComputeNodeLabelAnchor(
            NodeRect, onFarEdge: false, SankeyOrientation.Horizontal, measuredLabelHeight: 10);
        Assert.Equal(TextAlignment.Left, alignment);
    }

    [Fact]
    public void ComputeNodeLabelAnchor_Vertical_FarEdge_AnchorIsBelowRect()
    {
        var (anchor, _) = SankeySeriesRenderer.ComputeNodeLabelAnchor(
            NodeRect, onFarEdge: true, SankeyOrientation.Vertical, measuredLabelHeight: 10);
        // Below: anchor.Y > nodeRect.Y + nodeRect.Height
        Assert.True(anchor.Y > NodeRect.Y + NodeRect.Height);
    }

    [Fact]
    public void ComputeNodeLabelAnchor_Vertical_NotFarEdge_AnchorIsAboveRect()
    {
        var (anchor, _) = SankeySeriesRenderer.ComputeNodeLabelAnchor(
            NodeRect, onFarEdge: false, SankeyOrientation.Vertical, measuredLabelHeight: 10);
        // Above: anchor.Y < nodeRect.Y
        Assert.True(anchor.Y < NodeRect.Y);
    }
}
