// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.Rendering.Svg.Interaction;

/// <summary>Phase G.9 of the v1.7.2 follow-on plan — cursor-feedback coverage
/// across every interaction script. Cursor shape is the primary visual cue
/// that an element is interactive (the `grab`/`grabbing`/`pointer` signals
/// are load-bearing UX affordances). Without them users don't know what's
/// clickable / draggable.
///
/// <para>Stacked Theory asserts the expected cursor on the expected element
/// selector for each interaction. Each row drives one builder method.</para></summary>
public class CursorVisibilityTests
{
    [Fact]
    public void ZoomPan_SetsGrabCursor_OnSvgRoot()
    {
        using var h = InteractionScriptHarness.FromBuilder(b => b
            .WithZoomPan()
            .Plot([1.0, 2.0], [3.0, 4.0]));
        // SvgInteractivityScript sets cursor via inline `svg.style.cursor = 'grab'`
        // (during pan) and back to 'default' on release. The initial state is
        // inherited from the figure — the script doesn't set one at init, but it
        // DOES install a pointer-based listener that writes 'grabbing' during drag.
        h.Simulate("svg", "pointerdown", e => { e.clientX = 10; e.clientY = 10; });
        Assert.Equal("grabbing", h.GetStyle("svg", "cursor"));

        h.Simulate("svg", "pointerup",   e => { e.clientX = 10; e.clientY = 10; });
        Assert.Equal("default", h.GetStyle("svg", "cursor"));
    }

    [Fact]
    public void ThreeDRotation_SetsGrabCursor_OnSceneGroup()
    {
        using var h = InteractionScriptHarness.FromBuilder(b => b
            .WithBrowserInteraction()
            .AddSubPlot(1, 1, 1, ax => ax
                .WithCamera(elevation: 30, azimuth: -60)
                .Surface([0.0, 1.0], [0.0, 1.0], new double[,] { { 0, 1 }, { 1, 0 } })));
        // Script sets cursor at init — grab affordance always visible.
        Assert.Equal("grab", h.GetStyle(".mpl-3d-scene", "cursor"));
    }

    [Fact]
    public void LegendToggle_SetsPointerCursor_OnLegendItem()
    {
        using var h = InteractionScriptHarness.FromBuilder(b => b
            .WithLegendToggle()
            .Plot([1.0, 2.0], [3.0, 4.0], s => s.Label = "A"));
        Assert.Equal("pointer", h.GetStyle("[data-legend-index='0']", "cursor"));
    }

    [Fact]
    public void TreemapDrilldown_SetsPointerCursor_OnTiles()
    {
        var root = new TreeNode { Label = "Root", Children = [
            new() { Label = "A", Value = 10 },
            new() { Label = "B", Value = 5 },
        ]};
        using var h = InteractionScriptHarness.FromBuilder(b => b
            .WithTreemapDrilldown()
            .AddSubPlot(1, 1, 1, ax => ax.Treemap(root).HideAllAxes()));
        Assert.Equal("pointer", h.GetStyle("rect[data-treemap-node='0.0']", "cursor"));
    }

    [Fact]
    public void SankeyHover_SetsPointerCursor_OnNodes()
    {
        var nodes = new[] { new SankeyNode("A"), new SankeyNode("B") };
        var links = new[] { new SankeyLink(0, 1, 5) };
        using var h = InteractionScriptHarness.FromBuilder(b => b
            .WithSankeyHover()
            .AddSubPlot(1, 1, 1, ax => ax.Sankey(nodes, links).HideAllAxes()));
        Assert.Equal("pointer", h.GetStyle("[data-sankey-node-id='0']", "cursor"));
    }
}
