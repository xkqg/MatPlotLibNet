// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.Rendering.Svg.Interaction;

/// <summary>Phase G.6 of the v1.7.2 follow-on plan — behavioural coverage for
/// <see cref="MatPlotLibNet.Rendering.Svg.SvgTreemapDrilldownScript"/>, including
/// the sliding (animated) viewBox transition driven by the themable
/// <c>InteractionTheme.TreemapTransitionMs</c> token.
///
/// <para>Pre-G.6 only a static-emission test ("does the hint text appear?")
/// existed. This file drives the full drilldown lifecycle: click-to-drill,
/// Esc-to-zoom-out, hint toggle, and the themable transition duration.</para></summary>
public class TreemapDrilldownTests
{
    private static TreeNode SampleRoot => new()
    {
        Label = "Revenue",
        Children =
        [
            new() { Label = "Electronics", Value = 42 },
            new() { Label = "Apparel", Value = 28 },
            new() { Label = "Grocery", Value = 30 },
        ]
    };

    private static InteractionScriptHarness BuildTreemap(InteractionTheme? theme = null) =>
        InteractionScriptHarness.FromBuilder(b =>
        {
            b.WithSize(600, 500).WithTreemapDrilldown();
            if (theme is not null) b.WithInteractionTheme(theme);
            b.AddSubPlot(1, 1, 1, ax => ax.Treemap(SampleRoot).HideAllAxes());
        });

    [Fact]
    public void Click_Tile_AnimatesViewBoxToTileBounds()
    {
        using var h = BuildTreemap();

        // Pre-click: SVG has its original viewBox and a transition CSS already set
        // (the script primes svg.style.transition at init).
        var svg = h.Document.QuerySelectorAllRaw("svg").Single();
        Assert.StartsWith("all", svg.style.transition);
        var originalVb = svg.getAttribute("viewBox")!;

        var tile = h.Document.querySelector("rect[data-treemap-node='0.0']")!;
        tile.Fire(new DomEvent("click") { target = tile });

        var afterVb = svg.getAttribute("viewBox")!;
        Assert.NotEqual(originalVb, afterVb);

        // afterVb should equal the tile's x y w h rect.
        var expected = $"{tile.getAttribute("x")} {tile.getAttribute("y")} {tile.getAttribute("width")} {tile.getAttribute("height")}";
        Assert.Equal(expected, afterVb);
    }

    [Fact]
    public void Click_Tile_RespectsThemableTransitionMs()
    {
        using var h = BuildTreemap(new InteractionTheme(TreemapTransitionMs: 600));
        var svg = h.Document.QuerySelectorAllRaw("svg").Single();
        // Script must read data-mpl-treemap-transition-ms and honour it in
        // svg.style.transition. Pre-fix hard-codes 0.35s; post-fix uses 600ms.
        Assert.Contains("600", svg.style.transition);
    }

    [Fact]
    public void Escape_PopsStackAndRestoresParentViewBox()
    {
        using var h = BuildTreemap();
        var svg = h.Document.QuerySelectorAllRaw("svg").Single();
        var rootVb = svg.getAttribute("viewBox")!;

        // Drill into Electronics.
        var tile = h.Document.querySelector("rect[data-treemap-node='0.0']")!;
        tile.Fire(new DomEvent("click") { target = tile });
        Assert.NotEqual(rootVb, svg.getAttribute("viewBox"));

        // Escape on the tile pops.
        tile.Fire(new DomEvent("keydown") { target = tile, key = "Escape" });
        Assert.Equal(rootVb, svg.getAttribute("viewBox"));
    }

    [Fact]
    public void Hint_AppearsWhenDrilled_HidesAtRoot()
    {
        using var h = BuildTreemap();

        // At root: no hint yet (ensureHint is called on first drill).
        Assert.Null(h.Document.querySelector("text[data-mpl-treemap-hint]"));

        var tile = h.Document.querySelector("rect[data-treemap-node='0.0']")!;
        tile.Fire(new DomEvent("click") { target = tile });

        // After drill: hint is present with display:block.
        var hint = h.Document.querySelector("text[data-mpl-treemap-hint]");
        Assert.NotNull(hint);
        Assert.Equal("block", hint!.style.display);

        // Escape zooms out to root and hides the hint.
        tile.Fire(new DomEvent("keydown") { target = tile, key = "Escape" });
        Assert.Equal("none", h.Document.querySelector("text[data-mpl-treemap-hint]")!.style.display);
    }

    [Fact]
    public void EnterKey_OnFocusedTile_Drills()
    {
        using var h = BuildTreemap();
        var svg = h.Document.QuerySelectorAllRaw("svg").Single();
        var rootVb = svg.getAttribute("viewBox")!;

        var tile = h.Document.querySelector("rect[data-treemap-node='0.1']")!;
        tile.Fire(new DomEvent("keydown") { target = tile, key = "Enter" });

        Assert.NotEqual(rootVb, svg.getAttribute("viewBox"));
    }
}
