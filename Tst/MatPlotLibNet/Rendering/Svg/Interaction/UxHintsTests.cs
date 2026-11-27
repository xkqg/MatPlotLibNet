// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Rendering.Svg.Interaction;

/// <summary>Phase P (2026-04-18) — treemap interaction rewrote from drill-zoom to
/// expand/collapse, so the old "Press Esc to zoom out" hint was retired entirely.
/// The new UX surfaces its affordance through <c>cursor: pointer</c> on parent rects
/// (a direct visual cue) rather than a hint text element.</summary>
public class UxHintsTests
{
    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        // The new expand/collapse script emits "cursor = 'pointer'" on parent rects.
        // A static string assertion guarantees this affordance ships in every build.
        // Guard against accidental reintroduction of the retired hint element.
        new DrawnToken("TreemapDrilldownScript_IndicatesParentsAreClickableViaCursor", () => Plt.Create()
            .WithTreemapDrilldown()
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg(), "cursor = 'pointer'") { Absent = ["data-mpl-treemap-hint"] },
        // Phase 12: focus handler must use getBoundingClientRect, not (0, 0).
        new DrawnToken("TooltipScript_FocusUsesElementBoundsNotZeroZero", () => Plt.Create()
            .WithRichTooltips()
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg(), "getBoundingClientRect"),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();
}
