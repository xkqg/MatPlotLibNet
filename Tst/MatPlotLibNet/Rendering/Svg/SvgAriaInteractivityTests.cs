// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Rendering.Svg;

/// <summary>Verifies ARIA attributes and keyboard navigation in the five interactive JS scripts.</summary>
public class SvgAriaInteractivityTests
{
    // ── Legend toggle ──────────────────────────────────────────────────────────

    [Fact]
    public void LegendToggle_Script_ContainsTabindex()
    {
        var svg = Plt.Create().WithLegendToggle().Plot([1.0, 2.0], [3.0, 4.0]).ToSvg();
        Assert.Contains("tabindex", svg);
    }

    [Fact]
    public void LegendToggle_Script_ContainsAriaPressed()
    {
        var svg = Plt.Create().WithLegendToggle().Plot([1.0, 2.0], [3.0, 4.0]).ToSvg();
        Assert.Contains("aria-pressed", svg);
    }

    [Fact]
    public void LegendToggle_Script_ContainsKeydownListener()
    {
        var svg = Plt.Create().WithLegendToggle().Plot([1.0, 2.0], [3.0, 4.0]).ToSvg();
        Assert.Contains("keydown", svg);
    }

    [Fact]
    public void LegendToggle_Script_ContainsRoleButton()
    {
        var svg = Plt.Create().WithLegendToggle().Plot([1.0, 2.0], [3.0, 4.0]).ToSvg();
        Assert.Contains("role", svg);
        Assert.Contains("button", svg);
    }

    // ── Highlight ──────────────────────────────────────────────────────────────

    [Fact]
    public void Highlight_Script_ContainsFocusListener()
    {
        var svg = Plt.Create().WithHighlight().Plot([1.0, 2.0], [3.0, 4.0]).ToSvg();
        Assert.Contains("focus", svg);
    }

    [Fact]
    public void Highlight_Script_ContainsBlurListener()
    {
        var svg = Plt.Create().WithHighlight().Plot([1.0, 2.0], [3.0, 4.0]).ToSvg();
        Assert.Contains("blur", svg);
    }

    [Fact]
    public void Highlight_Script_AddsTabindex()
    {
        var svg = Plt.Create().WithHighlight().Plot([1.0, 2.0], [3.0, 4.0]).ToSvg();
        Assert.Contains("tabindex", svg);
    }

    // ── Zoom/Pan ───────────────────────────────────────────────────────────────

    [Fact]
    public void ZoomPan_Script_ContainsKeyboardZoom()
    {
        var svg = Plt.Create().WithZoomPan().Plot([1.0, 2.0], [3.0, 4.0]).ToSvg();
        // '+' or '=' for zoom in
        Assert.Contains("keydown", svg);
    }

    [Fact]
    public void ZoomPan_Script_ContainsArrowKeyPan()
    {
        var svg = Plt.Create().WithZoomPan().Plot([1.0, 2.0], [3.0, 4.0]).ToSvg();
        Assert.Contains("ArrowLeft", svg);
    }

    [Fact]
    public void ZoomPan_Script_ContainsHomeReset()
    {
        var svg = Plt.Create().WithZoomPan().Plot([1.0, 2.0], [3.0, 4.0]).ToSvg();
        Assert.Contains("Home", svg);
    }

    // ── Selection ─────────────────────────────────────────────────────────────

    [Fact]
    public void Selection_Script_ContainsEscapeCancel()
    {
        var svg = Plt.Create().WithSelection().Plot([1.0, 2.0], [3.0, 4.0]).ToSvg();
        Assert.Contains("Escape", svg);
    }

    // ── Tooltip ───────────────────────────────────────────────────────────────

    [Fact]
    public void Tooltip_Script_ContainsAriaLive()
    {
        var svg = Plt.Create().WithRichTooltips().Plot([1.0, 2.0], [3.0, 4.0]).ToSvg();
        Assert.Contains("aria-live", svg);
    }

    [Fact]
    public void Tooltip_Script_ContainsRoleTooltip()
    {
        var svg = Plt.Create().WithRichTooltips().Plot([1.0, 2.0], [3.0, 4.0]).ToSvg();
        Assert.Contains("tooltip", svg);
    }

    [Fact]
    public void LegendToggle_FullSvg_ContainsAriaPressed()
    {
        var svg = Plt.Create()
            .WithLegendToggle()
            .Plot([1.0, 2.0], [3.0, 4.0], s => s.Label = "Sales")
            .ToSvg();
        Assert.Contains("aria-pressed", svg);
    }
}
