// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Rendering.Svg.Interaction;

/// <summary>Phase P (2026-04-18) — treemap interaction rewrote from drill-zoom to
/// expand/collapse, so the old "Press Esc to zoom out" hint was retired entirely.
/// The new UX surfaces its affordance through <c>cursor: pointer</c> on parent rects
/// (a direct visual cue) rather than a hint text element.</summary>
public class UxHintsTests
{
    [Fact]
    public void TreemapDrilldownScript_IndicatesParentsAreClickableViaCursor()
    {
        // The new expand/collapse script emits "cursor = 'pointer'" on parent rects.
        // A static string assertion guarantees this affordance ships in every build.
        var svg = Plt.Create()
            .WithTreemapDrilldown()
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg();
        Assert.Contains("cursor = 'pointer'", svg);
        // Guard against accidental reintroduction of the retired hint element.
        Assert.DoesNotContain("data-mpl-treemap-hint", svg);
    }

    [Fact]
    public void TooltipScript_FocusUsesElementBoundsNotZeroZero()
    {
        var svg = Plt.Create()
            .WithRichTooltips()
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg();
        // Phase 12: focus handler must use getBoundingClientRect, not (0, 0).
        Assert.Contains("getBoundingClientRect", svg);
    }
}
