// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Rendering.Svg.Interaction;

/// <summary>Phase A of the v1.7.2 follow-on plan — fixes the 2D/3D event collision.
///
/// <para>Pre-fix bug: <c>WithBrowserInteraction()</c> on a 3D Surface enabled BOTH
/// <c>SvgInteractivityScript</c> (root-SVG pan/zoom) and <c>Svg3DRotationScript</c> (scene
/// rotate). On a drag, both handlers fired; the SVG-root handler called
/// <c>setPointerCapture</c> AFTER the scene handler, overriding the scene's capture
/// (last-call-wins per W3C Pointer Events spec) and stealing the drag for 2D pan.</para>
///
/// <para>Fix:
/// <list type="number">
/// <item>3D rotation script calls <c>e.stopPropagation()</c> on pointerdown + wheel (A.1).</item>
/// <item>2D zoom/pan script bails out at init when its owning SVG contains any
/// <c>.mpl-3d-scene</c> — matches matplotlib's NavigationToolbar2 disabling Pan/Zoom on
/// 3D axes (A.2).</item>
/// </list></para></summary>
public class ThreeDInteractionIsolationTests
{
    private static InteractionScriptHarness Build3DScene() =>
        InteractionScriptHarness.FromBuilder(b => b
            .WithSize(600, 500)
            .WithBrowserInteraction()
            .AddSubPlot(1, 1, 1, ax => ax
                .WithCamera(elevation: 30, azimuth: -60, distance: 8)
                .Surface([0.0, 1.0], [0.0, 1.0], new double[,] { { 0, 1 }, { 1, 0 } })));

    /// <summary>Fix A.2: when the SVG hosts a 3D scene, the 2D pan/zoom handler must not
    /// be wired. Firing a pointer drag on the SVG root therefore leaves the viewBox
    /// untouched (no pan).</summary>
    [Fact]
    public void Drag3DSceneViaSvgRoot_DoesNotPanViewBox()
    {
        using var h = Build3DScene();
        var initialVb = h.GetAttribute("svg", "viewBox");
        Assert.NotNull(initialVb);

        h.Simulate("svg", "pointerdown", e => { e.clientX = 100; e.clientY = 100; });
        h.Simulate("svg", "pointermove", e => { e.clientX = 200; e.clientY = 150; });
        h.Simulate("svg", "pointerup",   e => { e.clientX = 200; e.clientY = 150; });

        var afterVb = h.GetAttribute("svg", "viewBox");
        Assert.Equal(initialVb, afterVb);
    }

    /// <summary>Fix A.1 + A.2 + B.2 combined: dragging the 3D scene MUST reproject the
    /// data-v3d elements (rotation actually fires) and MUST NOT change the SVG viewBox
    /// (2D pan stayed out of the way). Observable: the <c>points</c> attribute on a
    /// data-v3d polygon shifts.</summary>
    [Fact]
    public void Drag3DSceneViaSceneGroup_RotatesAndLeavesViewBoxAlone()
    {
        using var h = Build3DScene();
        var initialPoints = h.GetAttribute("polygon[data-v3d]", "points");
        Assert.NotNull(initialPoints);
        var initialVb = h.GetAttribute("svg", "viewBox");

        h.Simulate(".mpl-3d-scene", "pointerdown", e => { e.clientX = 100; e.clientY = 100; });
        h.Simulate(".mpl-3d-scene", "pointermove", e => { e.clientX = 200; e.clientY = 100; });
        h.Simulate(".mpl-3d-scene", "pointerup",   e => { e.clientX = 200; e.clientY = 100; });

        var afterPoints = h.GetAttribute("polygon[data-v3d]", "points");
        var afterVb = h.GetAttribute("svg", "viewBox");

        Assert.NotEqual(initialPoints, afterPoints);
        Assert.Equal(initialVb, afterVb);
    }

    /// <summary>Fix A.1: scrolling on the 3D scene reprojects under perspective
    /// (camera distance changes the scale factor) and MUST NOT pan the SVG viewBox.
    /// Pre-fix the SVG-root wheel listener swallowed the wheel event before it could
    /// reach the scene.</summary>
    [Fact]
    public void Wheel3DScene_ChangesProjection_NotViewBox()
    {
        using var h = Build3DScene();
        var initialPoints = h.GetAttribute("polygon[data-v3d]", "points");
        Assert.NotNull(initialPoints);
        var initialVb = h.GetAttribute("svg", "viewBox");

        h.Simulate(".mpl-3d-scene", "wheel", e => { e.deltaY = 100; });

        var afterPoints = h.GetAttribute("polygon[data-v3d]", "points");
        var afterVb = h.GetAttribute("svg", "viewBox");

        Assert.NotEqual(initialPoints, afterPoints);
        Assert.Equal(initialVb, afterVb);
    }

    /// <summary>2D charts continue to pan on drag — the 3D-scene bail-out only triggers
    /// when an .mpl-3d-scene exists.</summary>
    [Fact]
    public void TwoDChart_StillPansOnDrag()
    {
        using var h = InteractionScriptHarness.FromBuilder(b => b
            .WithSize(600, 500)
            .WithBrowserInteraction()
            .Plot([1.0, 2.0, 3.0], [4.0, 5.0, 6.0]));

        var initialVb = h.GetAttribute("svg", "viewBox");

        h.Simulate("svg", "pointerdown", e => { e.clientX = 100; e.clientY = 100; });
        h.Simulate("svg", "pointermove", e => { e.clientX = 200; e.clientY = 200; });

        var afterVb = h.GetAttribute("svg", "viewBox");
        Assert.NotEqual(initialVb, afterVb);
    }
}
