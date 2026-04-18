// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Rendering.Svg.Interaction;

/// <summary>Phase 4 of the v1.7.2 plan — every mouse-listening interaction script must
/// also wire pointer-events listeners (pointerdown/pointermove/pointerup) so touch +
/// pen + mouse all dispatch through a unified path. Eight of the nine scripts were
/// mouse-only at v1.7.1 (only the SignalR variant used the pointer API).
///
/// <para>The tests below assert the SCRIPT SOURCE includes the pointer event names —
/// behavioural simulation of touch events through Jint stub-DOM is tracked separately.</para></summary>
public class PointerApiTests
{
    [Theory]
    [InlineData("WithZoomPan",       "pointerdown", "SvgInteractivityScript")]
    [InlineData("WithZoomPan",       "pointermove", "SvgInteractivityScript")]
    [InlineData("WithZoomPan",       "pointerup",   "SvgInteractivityScript")]
    [InlineData("WithLegendToggle",  "pointerdown", "SvgLegendToggleScript")]
    [InlineData("WithSelection",     "pointerdown", "SvgSelectionScript")]
    [InlineData("WithSelection",     "pointermove", "SvgSelectionScript")]
    [InlineData("WithSelection",     "pointerup",   "SvgSelectionScript")]
    [InlineData("With3DRotation",    "pointerdown", "Svg3DRotationScript")]
    [InlineData("With3DRotation",    "pointermove", "Svg3DRotationScript")]
    [InlineData("With3DRotation",    "pointerup",   "Svg3DRotationScript")]
    public void Script_RegistersPointerEventListener(string method, string eventName, string scriptName)
    {
        _ = scriptName; // for theory display
        var fb = Plt.Create().Plot([1.0, 2.0], [3.0, 4.0]);
        // Apply the requested builder method via reflection (keeps the test data-driven).
        typeof(MatPlotLibNet.FigureBuilder)
            .GetMethod(method, [typeof(bool)])!
            .Invoke(fb, [true]);
        var svg = fb.ToSvg();
        Assert.Contains(eventName, svg);
    }

    /// <summary>Pinch-to-zoom in <see cref="MatPlotLibNet.Rendering.Svg.SvgInteractivityScript"/>:
    /// requires tracking 2 active pointers. Asserts the script captures pointerId
    /// (the only practical way to track multiple concurrent pointers).</summary>
    [Fact]
    public void ZoomPan_TracksPointerIdForPinch()
    {
        var svg = Plt.Create().WithZoomPan().Plot([1.0, 2.0], [3.0, 4.0]).ToSvg();
        Assert.Contains("pointerId", svg);
    }
}
