// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.Rendering.Svg.Interaction;

/// <summary>Behavioural coverage for <see cref="MatPlotLibNet.Rendering.Svg.SvgTreemapDrilldownScript"/>.
///
/// <para>Phase P (2026-04-18) — the script is now expand/collapse per parent (click a
/// parent rect to toggle its direct children's visibility). Multiple parents can be
/// expanded at once; clicking a leaf does nothing.</para>
///
/// <para>Phase W follow-up (2026-04-19, "steady pictures") — initial state is now
/// EVERY parent expanded so the interactive view is pixel-identical to the static SVG
/// on first paint (no visual jump entering interactive mode). Click semantics flipped:
/// click now COLLAPSES a previously-expanded subtree; click again re-expands. Z-order
/// (children paint over parents) ensures the deepest visible label wins in any region
/// regardless of which subtrees are collapsed.</para>
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
    public void InitialState_AllParentsExpanded()
    {
        using var h = BuildTreemap();

        // Phase W follow-up ("steady pictures"): every node visible on first paint
        // — interactive view = static SVG. Z-order ensures deeper labels win.
        var root = h.Document.querySelector("rect[data-treemap-node='0']")!;
        var electronics = h.Document.querySelector("rect[data-treemap-node='0.0']")!;
        var phones = h.Document.querySelector("rect[data-treemap-node='0.0.0']")!;
        var apparel = h.Document.querySelector("rect[data-treemap-node='0.1']")!;
        var fresh = h.Document.querySelector("rect[data-treemap-node='0.2.0']")!;
        Assert.NotEqual("none", root.style.display);
        Assert.NotEqual("none", electronics.style.display);
        Assert.NotEqual("none", phones.style.display);
        Assert.NotEqual("none", apparel.style.display);
        Assert.NotEqual("none", fresh.style.display);
    }

    [Fact]
    public void ClickParent_CollapsesItsDirectChildren()
    {
        using var h = BuildTreemap();
        var svg = h.Document.QuerySelectorAllRaw("svg").Single();

        // Click Electronics (depth-1 parent) — its direct children hide.
        var electronics = h.Document.querySelector("rect[data-treemap-node='0.0']")!;
        svg.Fire(new DomEvent("click") { target = electronics });

        var phones = h.Document.querySelector("rect[data-treemap-node='0.0.0']")!;
        var laptops = h.Document.querySelector("rect[data-treemap-node='0.0.1']")!;
        var tvs = h.Document.querySelector("rect[data-treemap-node='0.0.2']")!;
        Assert.Equal("none", phones.style.display);
        Assert.Equal("none", laptops.style.display);
        Assert.Equal("none", tvs.style.display);

        // Siblings of Electronics stay expanded — this collapse is independent of others.
        var mens = h.Document.querySelector("rect[data-treemap-node='0.1.0']")!;
        Assert.NotEqual("none", mens.style.display);
    }

    [Fact]
    public void ClickParent_Twice_RestoresExpandedChildren()
    {
        using var h = BuildTreemap();
        var svg = h.Document.QuerySelectorAllRaw("svg").Single();

        var electronics = h.Document.querySelector("rect[data-treemap-node='0.0']")!;
        svg.Fire(new DomEvent("click") { target = electronics });
        svg.Fire(new DomEvent("click") { target = electronics });

        // After collapse → re-expand, Phones is back to visible.
        var phones = h.Document.querySelector("rect[data-treemap-node='0.0.0']")!;
        Assert.NotEqual("none", phones.style.display);
    }

    [Fact]
    public void ClickLeaf_DoesNothing()
    {
        using var h = BuildTreemap();
        var svg = h.Document.QuerySelectorAllRaw("svg").Single();

        // Clicking a leaf (Phones) must NOT change any state — leaves aren't toggleable.
        var phones = h.Document.querySelector("rect[data-treemap-node='0.0.0']")!;
        svg.Fire(new DomEvent("click") { target = phones });

        // Phones still visible (default-expanded); other leaves untouched.
        Assert.NotEqual("none", phones.style.display);
        Assert.NotEqual("none", h.Document.querySelector("rect[data-treemap-node='0.1.0']")!.style.display);
    }

    [Fact]
    public void MultipleParents_CanBeCollapsedIndependently()
    {
        using var h = BuildTreemap();
        var svg = h.Document.QuerySelectorAllRaw("svg").Single();

        var electronics = h.Document.querySelector("rect[data-treemap-node='0.0']")!;
        var grocery = h.Document.querySelector("rect[data-treemap-node='0.2']")!;
        svg.Fire(new DomEvent("click") { target = electronics });
        svg.Fire(new DomEvent("click") { target = grocery });

        // Both sets of children hidden; Apparel's stay visible (independent).
        Assert.Equal("none", h.Document.querySelector("rect[data-treemap-node='0.0.0']")!.style.display);
        Assert.Equal("none", h.Document.querySelector("rect[data-treemap-node='0.2.0']")!.style.display);
        Assert.NotEqual("none", h.Document.querySelector("rect[data-treemap-node='0.1.0']")!.style.display);
    }

    [Fact]
    public void ParentRect_HasPointerCursor()
    {
        using var h = BuildTreemap();
        var electronics = h.Document.querySelector("rect[data-treemap-node='0.0']")!;
        Assert.Equal("pointer", electronics.style.cursor);
    }

    // Phase W (2026-04-19) — depth-3 tree. Phones is now a parent of iPhone/Galaxy/Pixel
    // (depth-3 leaves); Laptops stays a depth-2 leaf (mixed-depth). Used to pin that
    // expand state at depth 2 is independent of depth 1, and depth-3 nodes are hidden
    // until their depth-2 ancestor is expanded.
    private static TreeNode SampleRootDepth3 => new()
    {
        Label = "Catalog",
        Children =
        [
            new()
            {
                Label = "Electronics", Value = 80,
                Children =
                [
                    new()
                    {
                        Label = "Phones", Value = 50,
                        Children =
                        [
                            new() { Label = "iPhone", Value = 30 },
                            new() { Label = "Galaxy", Value = 15 },
                            new() { Label = "Pixel",  Value = 5 },
                        ]
                    },
                    new() { Label = "Laptops", Value = 30 },
                ]
            },
            new() { Label = "Apparel", Value = 20 },
        ]
    };

    /// <summary>Phase W follow-up regression — transitive hide. Pre-fix the script only
    /// checked the immediate parent's expanded flag, so collapsing root hid depth-1
    /// rects but left depth-2+ visible (their own parent's expanded flag was still
    /// true). Surfaced by the Playwright T4 step on 2026-04-19. Fix: visibility is now
    /// an ANCESTRY walk — a node is visible iff every ancestor up to root is expanded.
    /// Collapsing any interior node hides its entire subtree in one click.</summary>
    [Fact]
    public void ClickRootParent_TransitivelyHidesEntireSubtree()
    {
        using var h = InteractionScriptHarness.FromBuilder(b =>
        {
            b.WithSize(700, 500).WithTreemapDrilldown();
            b.AddSubPlot(1, 1, 1, ax => ax.Treemap(SampleRootDepth3).HideAllAxes());
        });
        var svg = h.Document.QuerySelectorAllRaw("svg").Single();

        // Sanity: depth-3 iPhone visible by default.
        var iphone = h.Document.querySelector("rect[data-treemap-node='0.0.0.0']")!;
        Assert.NotEqual("none", iphone.style.display);

        // Click ROOT (Catalog, depth-0). Its direct children (Electronics, Apparel) hide;
        // Electronics' children (Phones, Laptops) MUST also hide; Phones' children
        // (iPhone, Galaxy, Pixel) MUST also hide. All three depths collapse together.
        var root = h.Document.querySelector("rect[data-treemap-node='0']")!;
        svg.Fire(new DomEvent("click") { target = root });

        Assert.Equal("none", h.Document.querySelector("rect[data-treemap-node='0.0']")!.style.display);
        Assert.Equal("none", h.Document.querySelector("rect[data-treemap-node='0.0.0']")!.style.display);
        Assert.Equal("none", iphone.style.display);
        Assert.Equal("none", h.Document.querySelector("rect[data-treemap-node='0.1']")!.style.display);
    }

    [Fact]
    public void Depth3_AllVisible_ByDefault_ClickPhonesCollapsesIts3Leaves()
    {
        using var h = InteractionScriptHarness.FromBuilder(b =>
        {
            b.WithSize(700, 500).WithTreemapDrilldown();
            b.AddSubPlot(1, 1, 1, ax => ax.Treemap(SampleRootDepth3).HideAllAxes());
        });
        var svg = h.Document.QuerySelectorAllRaw("svg").Single();

        // Phase W "steady pictures": every depth visible on first paint, including
        // depth-3 iPhone/Galaxy/Pixel — z-order ensures the deepest label wins in
        // any region where rects overlap.
        var iphone = h.Document.querySelector("rect[data-treemap-node='0.0.0.0']");
        Assert.NotNull(iphone);
        Assert.NotEqual("none", iphone.style.display);
        var phones = h.Document.querySelector("rect[data-treemap-node='0.0.0']")!;
        Assert.NotEqual("none", phones.style.display);

        // Click Phones (depth-2) — its three depth-3 leaves collapse but Phones itself
        // stays visible. Independent of Electronics' / Apparel's expansion state.
        svg.Fire(new DomEvent("click") { target = phones });
        Assert.NotEqual("none", phones.style.display);
        Assert.Equal("none", iphone.style.display);
        Assert.Equal("none", h.Document.querySelector("rect[data-treemap-node='0.0.0.1']")!.style.display);
        Assert.Equal("none", h.Document.querySelector("rect[data-treemap-node='0.0.0.2']")!.style.display);
    }

    /// <summary>Regression for the v1.7.2 Phase R second-layer click bug, surfaced by the
    /// real-browser pixel-compare repro on 2026-04-19. The pan/zoom script
    /// (<see cref="MatPlotLibNet.Rendering.Svg.SvgInteractivityScript"/>) calls
    /// <c>svg.setPointerCapture(e.pointerId)</c> on every <c>pointerdown</c>. In
    /// Chromium this redirects the synthetic <c>click</c> derived from <c>pointerup</c>
    /// to the SVG root rather than the rect under the cursor. Pre-fix the treemap script
    /// walked up from <c>e.target</c> only — finding no <c>data-treemap-node</c> on the
    /// SVG root, it returned null and the toggle never fired. Fix: when the walk-up
    /// returns null AND <c>document.elementFromPoint</c> is available, hit-test the
    /// click coordinates to recover the real target.</summary>
    /// <summary>Regression for the v1.7.2 Phase W root-hidden bug, surfaced by the user
    /// 2026-04-19 from a real-browser screenshot ("why without browser interactive you
    /// see Revenue and not with browser interactive"). The drilldown script seeds
    /// <c>expanded = { '0': true }</c>, then <c>applyVisibility()</c> sets each element's
    /// display to <c>expanded[parent] ? '' : 'none'</c>. The ROOT rect's
    /// <c>data-treemap-parent</c> is the empty string, so <c>expanded['']</c> is
    /// <c>undefined</c> → falsy → root gets <c>display:none</c> on init. The renderer's
    /// header-strip (~18 px reserved for the root label) becomes empty white space and
    /// the children appear shifted up relative to the chart frame even though their SVG
    /// coordinates are unchanged. Fix: seed <c>expanded[''] = true</c> alongside
    /// <c>expanded['0'] = true</c> so the root is always visible.</summary>
    [Fact]
    public void RootRect_IsVisible_OnInitialState()
    {
        using var h = BuildTreemap();
        var root = h.Document.querySelector("rect[data-treemap-node='0']")!;
        Assert.NotNull(root);
        Assert.NotEqual("none", root.style.display);
    }

    [Fact]
    public void Click_RedirectedToSvgRoot_FallsBackTo_ElementFromPoint()
    {
        using var h = BuildTreemap();
        var svg = h.Document.QuerySelectorAllRaw("svg").Single();
        var electronics = h.Document.querySelector("rect[data-treemap-node='0.0']")!;
        var phones = h.Document.querySelector("rect[data-treemap-node='0.0.0']")!;

        // Stub elementFromPoint so any hit-test resolves to Electronics — simulates the
        // physical click landed over the Electronics tile, then setPointerCapture
        // retargeted the click event to the SVG root.
        h.Document.StubElementFromPoint((x, y) => electronics);

        // Pre-click sanity: Phones is visible by default ("steady pictures" — every
        // parent expanded on first paint). The toggle must FLIP this to hidden via
        // the elementFromPoint fallback path even when click target is the SVG root.
        Assert.NotEqual("none", phones.style.display);

        // Click event with target=SVG (not the rect) — the post-redirection shape.
        svg.Fire(new DomEvent("click") { target = svg, clientX = 200, clientY = 300 });

        // Toggle fired through the fallback path → Electronics' direct children collapsed.
        Assert.Equal("none", phones.style.display);
    }

    /// <summary>Regression for the v1.7.2 Phase P click bug: a hover (pointermove without
    /// any prior pointerdown) latched <c>pointerMoved=true</c> in the drag-suppression
    /// flag, then the next click was dropped by <c>if (pointerMoved) return;</c>. Latent
    /// in v1.7.2 since 2026-04-18; surfaced by user testing 2026-04-19. Root-fix gates the
    /// move-threshold check on an <c>isPointerDown</c> flag that is only true between
    /// <c>pointerdown</c> and <c>pointerup</c>/<c>pointercancel</c>.</summary>
    [Fact]
    public void HoverWithoutButtonDown_DoesNotPoisonClickHandler()
    {
        using var h = BuildTreemap();
        var svg = h.Document.QuerySelectorAllRaw("svg").Single();
        var electronics = h.Document.querySelector("rect[data-treemap-node='0.0']")!;
        var phones = h.Document.querySelector("rect[data-treemap-node='0.0.0']")!;

        // Pre-click: Phones is visible by default (steady-pictures initial state).
        Assert.NotEqual("none", phones.style.display);

        // Hover the SVG (pointermove without any prior pointerdown) — must NOT poison
        // the drag-suppression flag. Two moves at >5 px from origin (0,0) so the buggy
        // pre-fix threshold (dx*dx + dy*dy > 25 against initial pointerDownX=0) trips.
        svg.Fire(new DomEvent("pointermove") { clientX = 200, clientY = 300 });
        svg.Fire(new DomEvent("pointermove") { clientX = 250, clientY = 350 });

        // Click the parent — toggle MUST fire (collapse Electronics) even after hovering.
        svg.Fire(new DomEvent("click") { target = electronics });

        Assert.Equal("none", phones.style.display);
    }
}
