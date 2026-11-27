// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Rendering.Svg;

/// <summary>Verifies SVG interactivity (zoom/pan) behavior, and that the data attributes (data-series-index, data-legend-index) are emitted
/// when any figure-level interactivity flag is enabled.</summary>
public class SvgInteractivityTests
{
    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        // Verifies that enabling zoom/pan injects a script element into the SVG output.
        new DrawnToken("EnableZoomPan_InjectsScriptElement", () => TestFigures.TwoPointSvg(b => b.WithZoomPan()), "<script"),
        // Verifies that without zoom/pan enabled, no script element is present in the SVG output.
        new DrawnToken("DisabledZoomPan_NoScriptElement", () => TestFigures.TwoPointSvg(b => b)) { Absent = ["<script"] },
        // Verifies that the injected script contains viewBox manipulation via setAttribute.
        new DrawnToken("ScriptContains_ViewBoxManipulation", () => TestFigures.TwoPointSvg(b => b.WithZoomPan()), "viewBox", "setAttribute"),
        // Verifies that the injected script includes a wheel event handler for zoom.
        new DrawnToken("ScriptContains_WheelEventHandler", () => TestFigures.TwoPointSvg(b => b.WithZoomPan()), "wheel"),
        // Phase 0.1 — root cause of the v1.7.2 "wheel scrolls page instead of zooming" bug. Modern browsers (Chrome 56+, Firefox 31+,
        // Safari 11+) treat wheel listeners as { passive: true } by default; calling e.preventDefault() in a passive listener is silently
        // ignored. The wheel listener MUST be registered with { passive: false } for the zoom to actually override the browser's default
        // scroll behaviour.
        new DrawnToken("ScriptContains_NonPassiveWheelListener", () => TestFigures.TwoPointSvg(b => b.WithZoomPan()), "passive: false"),
        // Verifies that enabling legend-toggle causes data-series-index to appear in SVG.
        new DrawnToken("EnableLegendToggle_EmitsDataSeriesIndex", () => TestFigures.TwoPointSvg(b => b.WithLegendToggle()), "data-series-index"),
        // Verifies that enabling highlight causes data-series-index to appear in SVG.
        new DrawnToken("EnableHighlight_EmitsDataSeriesIndex", () => TestFigures.TwoPointSvg(b => b.WithHighlight()), "data-series-index"),
        // Verifies that enabling selection causes data-series-index to appear in SVG.
        new DrawnToken("EnableSelection_EmitsDataSeriesIndex", () => TestFigures.TwoPointSvg(b => b.WithSelection()), "data-series-index"),
        // Verifies that enabling rich tooltips causes data-series-index to appear in SVG.
        new DrawnToken("EnableRichTooltips_EmitsDataSeriesIndex", () => TestFigures.TwoPointSvg(b => b.WithRichTooltips()), "data-series-index"),
        // Verifies that without any interactivity flag, no data-series-index attribute is emitted.
        new DrawnToken("NoInteractivityFlags_NoDataSeriesIndex", () => TestFigures.TwoPointSvg(b => b)) { Absent = ["data-series-index"] },
        // Verifies that data-series-index values are sequential starting from 0.
        new DrawnToken("MultiSeries_DataSeriesIndexIsSequential", () => Plt.Create()
            .WithHighlight()
            .Plot([1.0, 2.0], [3.0, 4.0])
            .Plot([1.0, 2.0], [5.0, 6.0])
            .ToSvg(), "data-series-index=\"0\"", "data-series-index=\"1\""),
        // Verifies that labeled series produce data-legend-index attributes when interactivity is enabled.
        new DrawnToken("LabeledSeries_WithInteractivity_EmitsDataLegendIndex", () => Plt.Create()
            .WithLegendToggle()
            .Plot([1.0, 2.0], [3.0, 4.0], s => s.Label = "Series A")
            .ToSvg(), "data-legend-index"),
        // Verifies that without interactivity, labeled series do not produce data-legend-index.
        new DrawnToken("LabeledSeries_NoInteractivity_NoDataLegendIndex", () => Plt.Create()
            .Plot([1.0, 2.0], [3.0, 4.0], s => s.Label = "Series A")
            .ToSvg()) { Absent = ["data-legend-index"] },
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();
}
