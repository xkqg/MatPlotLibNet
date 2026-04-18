// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Rendering.Svg.Interaction;

/// <summary>Phase 12 of the v1.7.2 plan — UX hint text + accessible focus tooltip
/// position. The hint <c>&lt;text&gt;</c> appears inside the SVG when the drill stack is
/// non-empty so users know Escape works.</summary>
public class UxHintsTests
{
    [Fact]
    public void TreemapDrilldownScript_ContainsZoomOutHint()
    {
        var svg = Plt.Create()
            .WithTreemapDrilldown()
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg();
        Assert.Contains("Press Esc to zoom out", svg);
        Assert.Contains("data-mpl-treemap-hint", svg);
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
