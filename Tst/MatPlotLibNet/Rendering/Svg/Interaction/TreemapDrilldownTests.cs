// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.Rendering.Svg.Interaction;

/// <summary>Behavioural coverage for <see cref="MatPlotLibNet.Rendering.Svg.SvgTreemapDrilldownScript"/>.
///
/// <para>Phase P (2026-04-18) — the script is now expand/collapse per parent (click a
/// parent rect to toggle its direct children's visibility). Multiple parents can be
/// expanded at once; clicking a leaf does nothing. The old drill-zoom + Escape-pops
/// model was replaced after three iterations of UX rework.</para>
///
/// <para>Click dispatch: the zoom/pan script's <c>setPointerCapture</c> redirects the
/// click to the SVG root, so these tests fire the click at the SVG element with
/// <c>e.target</c> set to the clicked rect — matching the delegation path the script
/// relies on.</para></summary>
public class TreemapDrilldownTests
{
    // 3-level tree: root → 3 groups, each with 3 leaves. The groups have nodeIds
    // "0.0" (Electronics), "0.1" (Apparel), "0.2" (Grocery). Leaves are "0.0.0" etc.
    private static TreeNode SampleRoot => new()
    {
        Label = "Revenue",
        Children =
        [
            new()
            {
                Label = "Electronics", Value = 42,
                Children =
                [
                    new() { Label = "Phones",  Value = 22 },
                    new() { Label = "Laptops", Value = 14 },
                    new() { Label = "TVs",     Value = 6 },
                ]
            },
            new()
            {
                Label = "Apparel", Value = 28,
                Children =
                [
                    new() { Label = "Men's",   Value = 11 },
                    new() { Label = "Women's", Value = 13 },
                    new() { Label = "Kids'",   Value = 4 },
                ]
            },
            new()
            {
                Label = "Grocery", Value = 30,
                Children =
                [
                    new() { Label = "Fresh",   Value = 13 },
                    new() { Label = "Frozen",  Value = 9 },
                    new() { Label = "Pantry",  Value = 8 },
                ]
            },
        ]
    };

    private static InteractionScriptHarness BuildTreemap() =>
        InteractionScriptHarness.FromBuilder(b =>
        {
            b.WithSize(600, 500).WithTreemapDrilldown();
            b.AddSubPlot(1, 1, 1, ax => ax.Treemap(SampleRoot).HideAllAxes());
        });

    [Fact]
    public void InitialState_OnlyTopLevelParentsVisible()
    {
        using var h = BuildTreemap();

        // Depth-1 rects (parent groups) must be visible by default.
        var electronics = h.Document.querySelector("rect[data-treemap-node='0.0']")!;
        Assert.NotEqual("none", electronics.style.display);

        // Depth-2 rects (leaves under the parents) must be hidden until expanded.
        var phones = h.Document.querySelector("rect[data-treemap-node='0.0.0']")!;
        Assert.Equal("none", phones.style.display);
    }

    [Fact]
    public void ClickParent_ExpandsItsDirectChildren()
    {
        using var h = BuildTreemap();
        var svg = h.Document.QuerySelectorAllRaw("svg").Single();

        // Click Electronics (depth-1 parent).
        var electronics = h.Document.querySelector("rect[data-treemap-node='0.0']")!;
        svg.Fire(new DomEvent("click") { target = electronics });

        // Electronics' direct children become visible.
        var phones = h.Document.querySelector("rect[data-treemap-node='0.0.0']")!;
        var laptops = h.Document.querySelector("rect[data-treemap-node='0.0.1']")!;
        var tvs = h.Document.querySelector("rect[data-treemap-node='0.0.2']")!;
        Assert.NotEqual("none", phones.style.display);
        Assert.NotEqual("none", laptops.style.display);
        Assert.NotEqual("none", tvs.style.display);

        // Siblings of Electronics stay collapsed — this expand is independent of others.
        var mens = h.Document.querySelector("rect[data-treemap-node='0.1.0']")!;
        Assert.Equal("none", mens.style.display);
    }

    [Fact]
    public void ClickParent_Twice_CollapsesChildrenBack()
    {
        using var h = BuildTreemap();
        var svg = h.Document.QuerySelectorAllRaw("svg").Single();

        var electronics = h.Document.querySelector("rect[data-treemap-node='0.0']")!;
        svg.Fire(new DomEvent("click") { target = electronics });
        svg.Fire(new DomEvent("click") { target = electronics });

        var phones = h.Document.querySelector("rect[data-treemap-node='0.0.0']")!;
        Assert.Equal("none", phones.style.display);
    }

    [Fact]
    public void ClickLeaf_DoesNothing()
    {
        using var h = BuildTreemap();
        var svg = h.Document.QuerySelectorAllRaw("svg").Single();

        // First expand Electronics so its leaves become clickable targets.
        var electronics = h.Document.querySelector("rect[data-treemap-node='0.0']")!;
        svg.Fire(new DomEvent("click") { target = electronics });

        // Clicking a leaf (Phones) must NOT change any state — leaves aren't toggleable.
        var phones = h.Document.querySelector("rect[data-treemap-node='0.0.0']")!;
        svg.Fire(new DomEvent("click") { target = phones });

        // Phones remains visible, all other leaves remain as they were.
        Assert.NotEqual("none", phones.style.display);
        Assert.Equal("none", h.Document.querySelector("rect[data-treemap-node='0.1.0']")!.style.display);
    }

    [Fact]
    public void MultipleParents_CanBeExpandedIndependently()
    {
        using var h = BuildTreemap();
        var svg = h.Document.QuerySelectorAllRaw("svg").Single();

        var electronics = h.Document.querySelector("rect[data-treemap-node='0.0']")!;
        var grocery = h.Document.querySelector("rect[data-treemap-node='0.2']")!;
        svg.Fire(new DomEvent("click") { target = electronics });
        svg.Fire(new DomEvent("click") { target = grocery });

        // Both sets of children now visible; Apparel's stay hidden.
        Assert.NotEqual("none", h.Document.querySelector("rect[data-treemap-node='0.0.0']")!.style.display);
        Assert.NotEqual("none", h.Document.querySelector("rect[data-treemap-node='0.2.0']")!.style.display);
        Assert.Equal("none", h.Document.querySelector("rect[data-treemap-node='0.1.0']")!.style.display);
    }

    [Fact]
    public void ParentRect_HasPointerCursor()
    {
        using var h = BuildTreemap();
        var electronics = h.Document.querySelector("rect[data-treemap-node='0.0']")!;
        Assert.Equal("pointer", electronics.style.cursor);
    }
}
