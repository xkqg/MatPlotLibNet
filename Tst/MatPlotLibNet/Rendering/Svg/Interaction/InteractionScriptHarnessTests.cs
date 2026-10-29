// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Rendering.Svg.Interaction;

/// <summary>Smoke tests for the Phase-1 behavioural harness — confirms the Jint engine,
/// DOM stub, CSS-selector translator, and event dispatcher all hang together by
/// driving a real script (the legend toggle) end-to-end.</summary>
public class InteractionScriptHarnessTests
{
    /// <summary>Click on a legend item must hide every series tagged with the matching
    /// <c>data-series-index</c>. This is the canonical proof the harness can:
    /// (a) parse and run an embedded IIFE,
    /// (b) translate CSS attribute selectors,
    /// (c) dispatch a click event to the right element,
    /// (d) observe the resulting <c>style.display = "none"</c> mutation.</summary>
    [Fact]
    public void LegendToggle_Click_HidesMatchingSeries()
    {
        using var h = InteractionScriptHarness.FromBuilder(b => b
            .WithLegendToggle()
            .Plot([1.0, 2.0], [3.0, 4.0], s => s.Label = "A")
            .Plot([1.0, 2.0], [5.0, 6.0], s => s.Label = "B"));

        // Pre-condition: nothing is hidden yet.
        Assert.Null(h.GetStyle("[data-series-index='0']", "display"));

        // Click the first legend item.
        var n = h.Simulate("[data-legend-index='0']", "click");
        Assert.True(n > 0, "Selector matched no legend item — script never wired up?");

        // Post-condition: series 0 is hidden, series 1 is not.
        Assert.Equal("none", h.GetStyle("[data-series-index='0']", "display"));
        Assert.NotEqual("none", h.GetStyle("[data-series-index='1']", "display"));
    }

    /// <summary>The legend entry of a series carries that SERIES' index, not the entry's position in the legend.
    /// With series 0 unlabelled and series 1 labelled "B", B's entry is the legend's first row, and a click on it
    /// must hide series 1 — before the legend carried one list for drawing, hit-testing and layout, the group read
    /// <c>data-legend-index="0"</c> and the click hid the unlabelled series 0 instead.</summary>
    [Fact]
    public void LegendToggle_ClickOnTheEntryOfSeriesB_HidesSeriesB_whenAnEarlierSeriesHasNoEntry()
    {
        using var h = InteractionScriptHarness.FromBuilder(b => b
            .WithLegendToggle()
            .Plot([1.0, 2.0], [3.0, 4.0])
            .Plot([1.0, 2.0], [5.0, 6.0], s => s.Label = "B"));

        Assert.Null(h.GetStyle("[data-series-index='1']", "display"));

        var n = h.Simulate("[data-legend-index]", "click");
        Assert.Equal(1, n);

        Assert.Equal("none", h.GetStyle("[data-series-index='1']", "display"));
        Assert.NotEqual("none", h.GetStyle("[data-series-index='0']", "display"));
    }

    /// <summary>A secondary-axis series is a legend entry too, and its entry carries the index it has in the whole
    /// legend list (primary series first, then the secondary ones). The secondary line is drawn inside a series group
    /// carrying that same index, so a click on the entry hides that line — and never a primary series that happens to
    /// sit at the entry's position.</summary>
    [Fact]
    public void LegendToggle_ClickOnARightEntry_HidesThatRightSeries_AndNoPrimarySeries()
    {
        using var h = InteractionScriptHarness.FromBuilder(b => b
            .WithLegendToggle()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2.0], [3.0, 4.0], s => s.Label = "A")
                .Plot([1.0, 2.0], [5.0, 6.0])
                .WithAddRightYAxis(y2 => y2.Plot([1.0, 2.0], [50.0, 60.0], s => s.Label = "S"))));

        var entry = System.Text.RegularExpressions.Regex.Match(
            h.Svg, "data-legend-index=\"(?<i>\\d+)\"[^>]*aria-label=\"S\"");
        Assert.True(entry.Success, "the secondary series has no legend entry");
        string index = entry.Groups["i"].Value;
        Assert.Equal("2", index);

        h.Simulate($"[data-legend-index='{index}']", "click");

        Assert.Equal("none", h.GetStyle($"[data-series-index='{index}']", "display"));
        Assert.NotEqual("none", h.GetStyle("[data-series-index='0']", "display"));
        Assert.NotEqual("none", h.GetStyle("[data-series-index='1']", "display"));
    }

    /// <summary>A secondary line's end label is drawn inside the group of its own series, the way a primary one is:
    /// the group carries the series' slot (primary series first, then the secondary ones), so whatever hides that index
    /// takes the label along, and a click on another entry — here the other secondary line's — leaves it shown.</summary>
    [Fact]
    public void LegendToggle_ARightEndLabel_LivesInItsOwnSeriesGroup_AndAnotherEntryLeavesItShown()
    {
        using var h = InteractionScriptHarness.FromBuilder(b => b
            .WithLegendToggle()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2.0], [3.0, 4.0], s => s.Label = "A")
                .WithAddRightYAxis(y2 => y2
                    .Plot([1.0, 2.0], [50.0, 60.0], s => s.Label = "S")
                    .Plot([1.0, 2.0], [70.0, 80.0], s => s.Label = "T").WithDirectLabel())));

        var label = System.Xml.Linq.XDocument.Parse(h.Svg).Descendants()
            .Single(e => e.Name.LocalName == "text" && e.Value == "T");
        var group = label.Parent!;
        Assert.Equal("2", (string?)group.Attribute("data-series-index"));
        Assert.Single(group.Descendants(), e => e.Name.LocalName == "polyline");

        h.Simulate("[data-legend-index='1']", "click");

        Assert.Equal("none", h.GetStyle("[data-series-index='1']", "display"));
        Assert.NotEqual("none", h.GetStyle("[data-series-index='2']", "display"));
        Assert.NotEqual("none", h.GetStyle("[data-series-index='0']", "display"));
    }

    /// <summary>The hover highlight dims every series group but the one under the pointer. A secondary line has a
    /// group of its own, so hovering it dims the primary line and keeps the secondary one at full opacity.</summary>
    [Fact]
    public void Highlight_HoverOnARightLine_DimsThePrimaryLine_AndKeepsTheRightOne()
    {
        using var h = InteractionScriptHarness.FromBuilder(b => b
            .WithHighlight()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2.0], [3.0, 4.0], s => s.Label = "A")
                .WithAddRightYAxis(y2 => y2.Plot([1.0, 2.0], [50.0, 60.0], s => s.Label = "S"))));

        int hovered = h.Simulate("[data-series-index='1']", "mouseenter");

        Assert.Equal(1, hovered);
        Assert.Equal("1", h.GetStyle("[data-series-index='1']", "opacity"));
        Assert.Equal("0.3", h.GetStyle("[data-series-index='0']", "opacity"));
    }

    /// <summary>An end label is drawn inside the group of its own series, so the toggle script — which hides every
    /// element carrying the clicked <c>data-series-index</c> — takes the label along with its line, and a click on
    /// ANOTHER series' entry leaves the label shown. (A directly-labelled series has no legend entry of its own; the
    /// server-side <c>LegendToggleEvent</c> hides it by its <c>Visible</c> flag, and a hidden series draws no label.)</summary>
    [Fact]
    public void LegendToggle_HidesTheGroupThatHoldsTheEndLabel_AndOnlyThatOne()
    {
        using var h = InteractionScriptHarness.FromBuilder(b => b
            .WithLegendToggle()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2.0], [3.0, 4.0], s => s.Label = "A")
                .Plot([1.0, 2.0], [5.0, 6.0], s => s.Label = "B").WithDirectLabel()));

        var label = System.Xml.Linq.XDocument.Parse(h.Svg).Descendants()
            .Single(e => e.Name.LocalName == "text" && e.Value == "B");
        Assert.Equal("1", (string?)label.Parent!.Attribute("data-series-index"));

        h.Simulate("[data-legend-index='0']", "click");

        Assert.Equal("none", h.GetStyle("[data-series-index='0']", "display"));
        Assert.NotEqual("none", h.GetStyle("[data-series-index='1']", "display"));
    }

    /// <summary>Toggling the same legend item twice restores visibility — proves the
    /// script's hidden-state branch works in both directions.</summary>
    [Fact]
    public void LegendToggle_DoubleClick_RestoresSeries()
    {
        using var h = InteractionScriptHarness.FromBuilder(b => b
            .WithLegendToggle()
            .Plot([1.0, 2.0], [3.0, 4.0], s => s.Label = "A"));

        h.Simulate("[data-legend-index='0']", "click");
        Assert.Equal("none", h.GetStyle("[data-series-index='0']", "display"));

        h.Simulate("[data-legend-index='0']", "click");
        Assert.NotEqual("none", h.GetStyle("[data-series-index='0']", "display"));
    }

    /// <summary>Each legend item gets <c>tabindex="0"</c>, <c>role="button"</c>,
    /// <c>aria-pressed</c> applied by the script — proves <c>setAttribute</c> writes through.</summary>
    [Fact]
    public void LegendToggle_AppliesAriaAttributes()
    {
        using var h = InteractionScriptHarness.FromBuilder(b => b
            .WithLegendToggle()
            .Plot([1.0, 2.0], [3.0, 4.0], s => s.Label = "A"));

        Assert.Equal("0", h.GetAttribute("[data-legend-index='0']", "tabindex"));
        Assert.Equal("button", h.GetAttribute("[data-legend-index='0']", "role"));
        Assert.Equal("false", h.GetAttribute("[data-legend-index='0']", "aria-pressed"));
    }

    /// <summary>Phase S regression for the v1.7.2 "plot disappears" bug
    /// (2026-04-19, NOT specific to 3D). Pre-fix, <see cref="MatPlotLibNet.Rendering.Svg.SvgLegendToggleScript"/>
    /// fired <c>toggle()</c> on <c>pointerdown</c> — the moment the user pressed a legend item, the matching
    /// series hid before they could release. Single- or two-series charts visibly emptied; users couldn't even
    /// hold-down to drag (Phase S feature) without losing data on screen. The fix moves toggle to <c>click</c>
    /// only. This test pins the new contract: a bare <c>pointerdown</c> with NO subsequent <c>pointerup</c>
    /// must leave every series untouched.</summary>
    [Fact]
    public void LegendItem_PointerdownAlone_DoesNotToggleSeries()
    {
        using var h = InteractionScriptHarness.FromBuilder(b => b
            .WithLegendToggle()
            .Plot([1.0, 2.0], [3.0, 4.0], s => s.Label = "A"));

        Assert.Null(h.GetStyle("[data-series-index='0']", "display"));

        // Press without ever releasing — must NOT trigger toggle.
        h.Simulate("[data-legend-index='0']", "pointerdown", e => { e.clientX = 100; e.clientY = 50; });

        Assert.NotEqual("none", h.GetStyle("[data-series-index='0']", "display") ?? "");
        Assert.Equal("false", h.GetAttribute("[data-legend-index='0']", "aria-pressed"));
    }

    /// <summary>Phase G.3 of v1.7.2 follow-on — ARIA-button keyboard parity
    /// (WCAG 2.1 AA: Enter and Space must trigger the same action as click on
    /// any element with <c>role="button"</c>). Pins the <c>keydown</c> branch
    /// in <see cref="MatPlotLibNet.Rendering.Svg.SvgLegendToggleScript"/>.</summary>
    [Theory]
    [InlineData("Enter")]
    [InlineData(" ")]
    public void LegendToggle_EnterOrSpaceKey_HidesMatchingSeries(string key)
    {
        using var h = InteractionScriptHarness.FromBuilder(b => b
            .WithLegendToggle()
            .Plot([1.0, 2.0], [3.0, 4.0], s => s.Label = "A")
            .Plot([1.0, 2.0], [5.0, 6.0], s => s.Label = "B"));

        Assert.Null(h.GetStyle("[data-series-index='0']", "display"));

        h.Simulate("[data-legend-index='0']", "keydown", e => { e.key = key; });

        Assert.Equal("none", h.GetStyle("[data-series-index='0']", "display"));
        Assert.Equal("true", h.GetAttribute("[data-legend-index='0']", "aria-pressed"));
        Assert.NotEqual("none", h.GetStyle("[data-series-index='1']", "display"));
    }
}
