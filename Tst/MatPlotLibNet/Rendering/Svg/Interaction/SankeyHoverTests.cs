// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.Rendering.Svg.Interaction;

/// <summary>Phase G.7 of the v1.7.2 follow-on plan — behavioural coverage for
/// <see cref="MatPlotLibNet.Rendering.Svg.SvgSankeyHoverScript"/>. Exercises
/// the BFS-traversal highlight path (mouseenter / focus) and the restore path
/// (mouseleave / blur).
///
/// <para>Pre-G.7 the Sankey hover script had <b>zero</b> coverage of any kind
/// — no static emission test, no behavioural test, no Playground example.
/// Phase G.7 adds all three so every other interaction surface stays ahead.</para></summary>
public class SankeyHoverTests
{
    // 3-layer flow: 0=Coal, 1=Grid, 2=Homes.  Hovering Grid must touch both.
    private static readonly SankeyNode[] Nodes = new[]
    {
        new SankeyNode("Coal"),
        new SankeyNode("Gas"),
        new SankeyNode("Grid"),
        new SankeyNode("Homes"),
        new SankeyNode("Industry"),
    };
    private static readonly SankeyLink[] Links = new[]
    {
        new SankeyLink(0, 2, 50), // Coal → Grid
        new SankeyLink(1, 2, 30), // Gas → Grid
        new SankeyLink(2, 3, 40), // Grid → Homes
        new SankeyLink(2, 4, 40), // Grid → Industry
    };

    private static InteractionScriptHarness Build() =>
        InteractionScriptHarness.FromBuilder(b => b
            .WithSize(600, 400)
            .WithSankeyHover()
            .AddSubPlot(1, 1, 1, ax => ax.Sankey(Nodes, Links).HideAllAxes()));

    [Fact]
    public void HoverNode_DimsUnrelatedLinks()
    {
        using var h = Build();

        // Baseline: links are at base fill-opacity (1 or their stored data-base-opacity).
        var linksBefore = h.Document.QuerySelectorAllRaw("[data-sankey-link-source]")
            .Select(l => l.getAttribute("fill-opacity") ?? "1").ToArray();

        // Hover the Grid node (index 2). Every link should stay at base opacity (Grid
        // reaches all four via BFS).
        var grid = h.Document.querySelector("[data-sankey-node-id='2']");
        Assert.NotNull(grid);
        grid!.Fire(new DomEvent("mouseenter") { target = grid });

        var linksAfterGrid = h.Document.QuerySelectorAllRaw("[data-sankey-link-source]")
            .Select(l => l.getAttribute("fill-opacity") ?? "1").ToArray();
        // All links should be at base (non-dim) opacity since Grid reaches every link.
        Assert.All(linksAfterGrid, op => Assert.NotEqual("0.08", op));

        // Now leave, and hover Coal (index 0). Only Coal→Grid and downstream from
        // Grid (Grid→Homes, Grid→Industry) should stay at base; Gas→Grid should
        // dim to 0.08.
        grid.Fire(new DomEvent("mouseleave") { target = grid });
        var coal = h.Document.querySelector("[data-sankey-node-id='0']")!;
        coal.Fire(new DomEvent("mouseenter") { target = coal });

        var gasGridLink = h.Document.QuerySelectorAllRaw("[data-sankey-link-source='1']").Single();
        Assert.Equal("0.08", gasGridLink.getAttribute("fill-opacity"));
    }

    [Fact]
    public void MouseLeave_RestoresBaseOpacity()
    {
        using var h = Build();

        var coal = h.Document.querySelector("[data-sankey-node-id='0']")!;
        coal.Fire(new DomEvent("mouseenter") { target = coal });
        // One link is dimmed.
        var gasGridLink = h.Document.QuerySelectorAllRaw("[data-sankey-link-source='1']").Single();
        Assert.Equal("0.08", gasGridLink.getAttribute("fill-opacity"));

        coal.Fire(new DomEvent("mouseleave") { target = coal });
        // After leave, restored to base.
        Assert.Equal(gasGridLink.getAttribute("data-base-opacity"), gasGridLink.getAttribute("fill-opacity"));
    }

    [Fact]
    public void FocusNode_MirrorsHover()
    {
        // WCAG 2.1 AA: keyboard users get the same emphasis as mouse hover.
        using var h = Build();

        var coal = h.Document.querySelector("[data-sankey-node-id='0']")!;
        coal.Fire(new DomEvent("focus") { target = coal });
        var gasGridLink = h.Document.QuerySelectorAllRaw("[data-sankey-link-source='1']").Single();
        Assert.Equal("0.08", gasGridLink.getAttribute("fill-opacity"));
    }

    [Fact]
    public void NodesReceiveTabindex_ForKeyboardAccess()
    {
        using var h = Build();
        var nodes = h.Document.QuerySelectorAllRaw("[data-sankey-node-id]");
        Assert.NotEmpty(nodes);
        foreach (var n in nodes)
            Assert.Equal("0", n.getAttribute("tabindex"));
    }
}
