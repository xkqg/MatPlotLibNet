// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Rendering.Svg.Interaction;

/// <summary>Behavioural coverage for <see cref="MatPlotLibNet.Rendering.Svg.SvgLegendDragScript"/>.
///
/// <para>Phase S (2026-04-19) — press-and-hold any legend item and drag to translate the entire
/// <c>&lt;g class="legend"&gt;</c> group; release to drop. Coexists with the legend-toggle script:
/// a press-without-drag still toggles series visibility; a drag does NOT (the click swallower in
/// the drag script's <c>pointerup</c> handler suppresses the synthetic click in real browsers via
/// capture-phase <c>stopPropagation</c>).</para>
///
/// <para><b>Harness gap (documented on <see cref="InteractionScriptHarness"/>):</b> the Jint stub
/// does NOT honour capture-vs-bubble ordering — all listeners fire in registration order. The
/// drag-suppresses-click contract therefore cannot be pinned in xUnit; it lives in the Phase S
/// Playwright harness (<c>c:/tmp/legend_repro.py</c>) instead. This file pins everything that the
/// harness CAN faithfully simulate: drag detection, threshold, transform mutation.</para></summary>
public class LegendDragTests
{
    private static InteractionScriptHarness BuildLegend() =>
        InteractionScriptHarness.FromBuilder(b => b
            .WithSize(600, 400)
            .WithLegendToggle()
            .Plot([1.0, 2.0, 3.0], [4.0, 5.0, 6.0], s => s.Label = "A")
            .Plot([1.0, 2.0, 3.0], [5.0, 6.0, 7.0], s => s.Label = "B"));

    [Fact]
    public void Drag_LegendItem_TranslatesLegendGroup_BeyondThreshold()
    {
        using var h = BuildLegend();
        var legend = h.Document.querySelector("g.legend")!;
        Assert.True(string.IsNullOrEmpty(legend.getAttribute("transform")),
            "pre-condition: legend has no transform yet");

        // Drag start (50, 50) → end (110, 70) → 60 px right, 20 px down. Easily clears
        // the 5-px² threshold (60² + 20² = 4000 ≫ 25).
        h.Simulate("[data-legend-index='0']", "pointerdown", e => { e.clientX = 50; e.clientY = 50; });
        h.Simulate("[data-legend-index='0']", "pointermove", e => { e.clientX = 110; e.clientY = 70; });
        h.Simulate("[data-legend-index='0']", "pointerup",   e => { e.clientX = 110; e.clientY = 70; });

        var t = legend.getAttribute("transform") ?? "";
        Assert.Contains("translate(60,20)", t);
    }

    [Fact]
    public void MovementBelowThreshold_DoesNotTranslateLegend()
    {
        using var h = BuildLegend();
        var legend = h.Document.querySelector("g.legend")!;

        // Move only 3 px (3² + 0² = 9 < 25 threshold) — must be treated as click, NOT drag.
        h.Simulate("[data-legend-index='0']", "pointerdown", e => { e.clientX = 100; e.clientY = 50; });
        h.Simulate("[data-legend-index='0']", "pointermove", e => { e.clientX = 103; e.clientY = 50; });
        h.Simulate("[data-legend-index='0']", "pointerup",   e => { e.clientX = 103; e.clientY = 50; });

        // No transform was set — legend stays where the renderer placed it.
        Assert.True(string.IsNullOrEmpty(legend.getAttribute("transform")),
            $"expected no transform, got '{legend.getAttribute("transform")}'");
    }

    [Fact]
    public void Drag_AfterPointerUp_NewPointerdownStartsFreshDrag()
    {
        using var h = BuildLegend();
        var legend = h.Document.querySelector("g.legend")!;

        // First drag: 50 px right.
        h.Simulate("[data-legend-index='0']", "pointerdown", e => { e.clientX = 50;  e.clientY = 50; });
        h.Simulate("[data-legend-index='0']", "pointermove", e => { e.clientX = 100; e.clientY = 50; });
        h.Simulate("[data-legend-index='0']", "pointerup",   e => { e.clientX = 100; e.clientY = 50; });
        Assert.Contains("translate(50,0)", legend.getAttribute("transform") ?? "");

        // Second drag: another 30 px right — must accumulate from previous (50 + 30 = 80).
        h.Simulate("[data-legend-index='0']", "pointerdown", e => { e.clientX = 200; e.clientY = 50; });
        h.Simulate("[data-legend-index='0']", "pointermove", e => { e.clientX = 230; e.clientY = 50; });
        h.Simulate("[data-legend-index='0']", "pointerup",   e => { e.clientX = 230; e.clientY = 50; });
        Assert.Contains("translate(80,0)", legend.getAttribute("transform") ?? "");
    }

    [Fact]
    public void Pointermove_WithoutPointerdown_DoesNotTranslateLegend()
    {
        // Phase R hover-poison lesson: bare pointermove (no prior pointerdown) must NOT
        // engage the drag — otherwise hovering over a legend item would translate it.
        using var h = BuildLegend();
        var legend = h.Document.querySelector("g.legend")!;

        h.Simulate("[data-legend-index='0']", "pointermove", e => { e.clientX = 200; e.clientY = 200; });
        h.Simulate("[data-legend-index='0']", "pointermove", e => { e.clientX = 400; e.clientY = 400; });

        Assert.True(string.IsNullOrEmpty(legend.getAttribute("transform")),
            $"hover must not translate the legend; got '{legend.getAttribute("transform")}'");
    }
}
