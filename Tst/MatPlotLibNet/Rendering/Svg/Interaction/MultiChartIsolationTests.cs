// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Builders;

namespace MatPlotLibNet.Tests.Rendering.Svg.Interaction;

/// <summary>Phase 2 of the v1.7.2 interaction-hardening plan — verifies that with TWO
/// figures on one page, an event fired on figure-A's interactive elements does NOT
/// mutate figure-B. The pre-Phase-2 bug: 8 of 9 scripts use
/// <c>document.querySelector('svg')</c> which returns the FIRST svg in the document, so
/// every script's listeners (and queries) operate on chart-A regardless of which chart
/// the user actually interacts with — silent multi-chart cross-talk.</summary>
public class MultiChartIsolationTests
{
    private static readonly Action<FigureBuilder> SimpleLegendChart = b => b
        .WithLegendToggle()
        .Plot([1.0, 2.0], [3.0, 4.0], s => s.Label = "A");

    /// <summary>Both legend-toggle scripts must scope to their OWN owning SVG. If either
    /// script wired listeners against the WRONG svg, only one chart would respond — or
    /// chart-A's listener would mutate chart-B.</summary>
    [Fact]
    public void LegendToggle_ClickOnChart1_DoesNotAffectChart0()
    {
        using var h = InteractionScriptHarness.FromBuilders(SimpleLegendChart, SimpleLegendChart);

        // Pre-condition: nothing hidden on either chart.
        Assert.Null(h.GetStyle("#mpl-chart-0 [data-series-index='0']", "display"));
        Assert.Null(h.GetStyle("#mpl-chart-1 [data-series-index='0']", "display"));

        h.Simulate("#mpl-chart-1 [data-legend-index='0']", "click");

        // Chart-1 hidden; chart-0 untouched.
        Assert.Equal("none", h.GetStyle("#mpl-chart-1 [data-series-index='0']", "display"));
        Assert.Null(h.GetStyle("#mpl-chart-0 [data-series-index='0']", "display"));
    }

    /// <summary>Each script must apply its ARIA attributes to its OWN chart's legend
    /// items. Failure means a script saw the wrong SVG (or no SVG).</summary>
    [Fact]
    public void BothChartsLegendItems_GetAriaAttributes()
    {
        using var h = InteractionScriptHarness.FromBuilders(SimpleLegendChart, SimpleLegendChart);
        Assert.Equal("button", h.GetAttribute("#mpl-chart-0 [data-legend-index='0']", "role"));
        Assert.Equal("button", h.GetAttribute("#mpl-chart-1 [data-legend-index='0']", "role"));
    }

    /// <summary>Phase T (2026-04-19) — same isolation contract for the legend-drag script
    /// (Phase S). Dragging a legend item on chart-1 must translate ONLY chart-1's
    /// <c>g.legend</c>, never chart-0's. Pre-Phase-2 the script would have grabbed
    /// document.querySelector('svg') (the first one), so this regression-guards the
    /// per-chart self-location via document.currentScript.parentNode.</summary>
    [Fact]
    public void LegendDrag_OnChart1_DoesNotTranslateLegendOnChart0()
    {
        using var h = InteractionScriptHarness.FromBuilders(SimpleLegendChart, SimpleLegendChart);
        var legend0 = h.Document.querySelector("#mpl-chart-0 g.legend")!;
        var legend1 = h.Document.querySelector("#mpl-chart-1 g.legend")!;

        // Sanity: neither legend has a transform yet.
        Assert.True(string.IsNullOrEmpty(legend0.getAttribute("transform")));
        Assert.True(string.IsNullOrEmpty(legend1.getAttribute("transform")));

        // Drag chart-1's legend item past the 5-px² threshold.
        h.Simulate("#mpl-chart-1 [data-legend-index='0']", "pointerdown", e => { e.clientX = 50; e.clientY = 50; });
        h.Simulate("#mpl-chart-1 [data-legend-index='0']", "pointermove", e => { e.clientX = 110; e.clientY = 70; });
        h.Simulate("#mpl-chart-1 [data-legend-index='0']", "pointerup",   e => { e.clientX = 110; e.clientY = 70; });

        // Chart-1 translated, chart-0 untouched.
        Assert.Contains("translate", legend1.getAttribute("transform") ?? "");
        Assert.True(string.IsNullOrEmpty(legend0.getAttribute("transform")),
            $"chart-0 legend must NOT translate when chart-1 was dragged; got '{legend0.getAttribute("transform")}'");
    }
}
