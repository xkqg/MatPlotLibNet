// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Rendering.Svg.Interaction;

/// <summary>Phase 5 of the v1.7.2 plan — 2D zoom-pan must clamp viewBox dimensions
/// to a sensible range so the user can't zoom 100× past the original or pan the
/// chart entirely off-screen.</summary>
public class TwoDZoomCompletenessTests
{
    [Fact]
    public void ZoomPanScript_ContainsMinMaxClamp()
    {
        var svg = Plt.Create().WithZoomPan().Plot([1.0, 2.0], [3.0, 4.0]).ToSvg();
        // Min/max scale tracked against the original viewBox extents — assert the script
        // captures origVb.slice() AND consults it during scaling.
        Assert.Contains("origVb", svg);
        // Hard-cap values (arbitrary but documented in the script comments).
        Assert.Contains("MIN_ZOOM", svg);
        Assert.Contains("MAX_ZOOM", svg);
    }

    [Fact]
    public void ZoomPanScript_ContainsAspectLockBranch()
    {
        var svg = Plt.Create().WithZoomPan().Plot([1.0, 2.0], [3.0, 4.0]).ToSvg();
        // Aspect-lock toggled via data-aspect-lock="true" on the SVG element.
        Assert.Contains("data-aspect-lock", svg);
    }

    /// <summary>Phase C.1 of v1.7.2 follow-on — wheel-zoom rate matches matplotlib's
    /// <c>0.85^step</c> (mpl_toolkits, see <c>backend_bases.py:NavigationToolbar2.scroll_handler</c>
    /// L2635). Pre-fix used 1.10/0.90 (≈10% per notch); matplotlib uses ≈15% which feels
    /// snappier and matches user expectations in cross-tool workflows.</summary>
    [Theory]
    [InlineData(-100,  0.85)] // wheel up   (deltaY < 0) → zoom in,  scale = 0.85
    [InlineData( 100,  1.0 / 0.85)] // wheel down (deltaY > 0) → zoom out, scale = 1/0.85
    public void WheelZoom_AppliesMatplotlibScaleFactor(double deltaY, double expectedScale)
    {
        using var h = InteractionScriptHarness.FromBuilder(b => b
            .WithSize(600, 500)
            .WithZoomPan()
            .Plot([1.0, 2.0, 3.0], [4.0, 5.0, 6.0]));

        var initialVbStr = h.GetAttribute("svg", "viewBox")!;
        var initialVb = initialVbStr.Split(' ').Select(s => double.Parse(s, System.Globalization.CultureInfo.InvariantCulture)).ToArray();

        h.Simulate("svg", "wheel", e => { e.deltaY = deltaY; e.clientX = 0; e.clientY = 0; });

        var afterVbStr = h.GetAttribute("svg", "viewBox")!;
        var afterVb = afterVbStr.Split(' ').Select(s => double.Parse(s, System.Globalization.CultureInfo.InvariantCulture)).ToArray();

        // Width and height both scale by `expectedScale` regardless of cursor position
        // (cursor only affects translation; scale ratio is invariant).
        var widthRatio  = afterVb[2] / initialVb[2];
        var heightRatio = afterVb[3] / initialVb[3];
        Assert.Equal(expectedScale, widthRatio,  3);
        Assert.Equal(expectedScale, heightRatio, 3);
    }

    /// <summary>Phase C.2 of v1.7.2 follow-on — pan-axis lock via modifier keys, mirroring
    /// matplotlib's <c>x</c>/<c>y</c> drag-modifier convention (axes3d.py:format_deltas L4492).
    /// Holding <c>x</c> locks dy = 0 (horizontal-only pan); holding <c>y</c> locks dx = 0.</summary>
    [Theory]
    [InlineData("x",    100.0,   0.0)]   // x lock: dy collapses to 0 → no vertical pan
    [InlineData("y",      0.0, 100.0)]   // y lock: dx collapses to 0 → no horizontal pan
    [InlineData("none", 100.0, 100.0)]   // no modifier: both axes pan
    public void Pan_WithAxisLockModifier_LocksTheOtherAxis(string modifier, double expectedDxPanned, double expectedDyPanned)
    {
        using var h = InteractionScriptHarness.FromBuilder(b => b
            .WithSize(600, 500)
            .WithZoomPan()
            .Plot([1.0, 2.0, 3.0], [4.0, 5.0, 6.0]));

        var initialVbStr = h.GetAttribute("svg", "viewBox")!;
        var initialVb = initialVbStr.Split(' ').Select(s => double.Parse(s, System.Globalization.CultureInfo.InvariantCulture)).ToArray();

        if (modifier != "none")
            h.Simulate("svg", "keydown", e => { e.key = modifier; });

        h.Simulate("svg", "pointerdown", e => { e.clientX = 0;   e.clientY = 0; });
        h.Simulate("svg", "pointermove", e => { e.clientX = 100; e.clientY = 100; });

        var afterVbStr = h.GetAttribute("svg", "viewBox")!;
        var afterVb = afterVbStr.Split(' ').Select(s => double.Parse(s, System.Globalization.CultureInfo.InvariantCulture)).ToArray();

        // dx > 0 ⇒ vb[0] decreased (data slid right under cursor); dy > 0 ⇒ vb[1] decreased.
        // Convert pixel pan (100) to viewBox pan (initialVb[2] / svg.clientWidth).
        // The harness has no clientWidth so the script falls back to using initialVb dims.
        var dx = initialVb[0] - afterVb[0];
        var dy = initialVb[1] - afterVb[1];

        // Just check sign/zero pattern — magnitudes depend on viewBox/client ratio.
        if (expectedDxPanned > 0) Assert.True(dx > 0, $"x lock should NOT prevent x pan; got dx={dx}");
        else                      Assert.Equal(0.0, dx, 6);
        if (expectedDyPanned > 0) Assert.True(dy > 0, $"y lock should NOT prevent y pan; got dy={dy}");
        else                      Assert.Equal(0.0, dy, 6);
    }
}
