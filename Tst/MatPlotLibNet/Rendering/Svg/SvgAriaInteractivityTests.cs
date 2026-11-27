// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Rendering.Svg;

/// <summary>Verifies ARIA attributes and keyboard navigation in the five interactive JS scripts.</summary>
public class SvgAriaInteractivityTests
{
    // ── Legend toggle ──────────────────────────────────────────────────────────

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("LegendToggle_Script_ContainsTabindex", () => Plt.Create().WithLegendToggle().Plot([1.0, 2.0], [3.0, 4.0]).ToSvg(), "tabindex"),
        new DrawnToken("LegendToggle_Script_ContainsAriaPressed", () => Plt.Create().WithLegendToggle().Plot([1.0, 2.0], [3.0, 4.0]).ToSvg(), "aria-pressed"),
        new DrawnToken("LegendToggle_Script_ContainsKeydownListener", () => Plt.Create().WithLegendToggle().Plot([1.0, 2.0], [3.0, 4.0]).ToSvg(), "keydown"),
        new DrawnToken("LegendToggle_Script_ContainsRoleButton", () => Plt.Create().WithLegendToggle().Plot([1.0, 2.0], [3.0, 4.0]).ToSvg(), "role", "button"),
        new DrawnToken("Highlight_Script_ContainsFocusListener", () => Plt.Create().WithHighlight().Plot([1.0, 2.0], [3.0, 4.0]).ToSvg(), "focus"),
        new DrawnToken("Highlight_Script_ContainsBlurListener", () => Plt.Create().WithHighlight().Plot([1.0, 2.0], [3.0, 4.0]).ToSvg(), "blur"),
        new DrawnToken("Highlight_Script_AddsTabindex", () => Plt.Create().WithHighlight().Plot([1.0, 2.0], [3.0, 4.0]).ToSvg(), "tabindex"),
        // '+' or '=' for zoom in
        new DrawnToken("ZoomPan_Script_ContainsKeyboardZoom", () => Plt.Create().WithZoomPan().Plot([1.0, 2.0], [3.0, 4.0]).ToSvg(), "keydown"),
        new DrawnToken("ZoomPan_Script_ContainsArrowKeyPan", () => Plt.Create().WithZoomPan().Plot([1.0, 2.0], [3.0, 4.0]).ToSvg(), "ArrowLeft"),
        new DrawnToken("ZoomPan_Script_ContainsHomeReset", () => Plt.Create().WithZoomPan().Plot([1.0, 2.0], [3.0, 4.0]).ToSvg(), "Home"),
        new DrawnToken("Selection_Script_ContainsEscapeCancel", () => Plt.Create().WithSelection().Plot([1.0, 2.0], [3.0, 4.0]).ToSvg(), "Escape"),
        new DrawnToken("Tooltip_Script_ContainsAriaLive", () => Plt.Create().WithRichTooltips().Plot([1.0, 2.0], [3.0, 4.0]).ToSvg(), "aria-live"),
        new DrawnToken("Tooltip_Script_ContainsRoleTooltip", () => Plt.Create().WithRichTooltips().Plot([1.0, 2.0], [3.0, 4.0]).ToSvg(), "tooltip"),
        new DrawnToken("LegendToggle_FullSvg_ContainsAriaPressed", () => Plt.Create()
            .WithLegendToggle()
            .Plot([1.0, 2.0], [3.0, 4.0], s => s.Label = "Sales")
            .ToSvg(), "aria-pressed"),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

}
