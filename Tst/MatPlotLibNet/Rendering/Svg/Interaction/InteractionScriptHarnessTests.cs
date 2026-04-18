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
