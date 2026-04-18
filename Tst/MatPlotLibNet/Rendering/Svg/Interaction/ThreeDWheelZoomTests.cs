// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace MatPlotLibNet.Tests.Rendering.Svg.Interaction;

/// <summary>Phase F.3 of the v1.7.2 follow-on plan — 3D wheel zoom must work
/// on every chart, not only the ones that explicitly set <c>distance:</c> in
/// <c>WithCamera(...)</c>.
///
/// <para>Pre-fix root cause: server-side <c>Projection3D</c> always runs
/// perspective with <c>dist = _distance ?? DefaultDist = 10</c> (there is no
/// "orthographic mode" at render time), but <c>SvgRenderContext.Begin3DSceneGroup</c>
/// only emits <c>data-distance</c> when the caller explicitly set it, and
/// <c>Svg3DRotationScript</c>'s wheel handler bails with
/// <c>if (distance === null) return;</c>. Result: for every 3D chart where
/// the caller used the default <c>WithCamera(el, az)</c> — including the
/// playground's 3D Surface — wheel is a silent no-op. But the cube IS
/// already perspective, just not zoomable. User-visible symptom:
/// "zoom does not work in 3D".</para>
///
/// <para>Fix: server always emits <c>data-distance</c> (10 when caller didn't
/// set one); JS defaults distance to 10 when the attribute is missing and
/// stops bailing. Both changes are defence-in-depth — either alone fixes
/// the user bug, together they remove the server/client asymmetry entirely.</para></summary>
public class ThreeDWheelZoomTests
{
    [Fact]
    public void Wheel_OnChartWithNoExplicitDistance_ChangesProjection()
    {
        using var h = InteractionScriptHarness.FromBuilder(b => b
            .WithSize(600, 500)
            .WithBrowserInteraction()
            .AddSubPlot(1, 1, 1, ax => ax
                .WithCamera(elevation: 35, azimuth: -50)   // no distance — default path
                .Surface([0.0, 1.0], [0.0, 1.0], new double[,] { { 0, 1 }, { 1, 0 } })));

        var initialPoints = h.GetAttribute("polygon[data-v3d]", "points");
        Assert.NotNull(initialPoints);

        h.Simulate(".mpl-3d-scene", "wheel", e => { e.deltaY = 100; });

        var afterPoints = h.GetAttribute("polygon[data-v3d]", "points");
        Assert.NotEqual(initialPoints, afterPoints);
    }

    [Fact]
    public void Wheel_OnChartWithNoExplicitDistance_PersistsDataDistanceAttribute()
    {
        using var h = InteractionScriptHarness.FromBuilder(b => b
            .WithSize(600, 500)
            .WithBrowserInteraction()
            .AddSubPlot(1, 1, 1, ax => ax
                .WithCamera(elevation: 35, azimuth: -50)
                .Surface([0.0, 1.0], [0.0, 1.0], new double[,] { { 0, 1 }, { 1, 0 } })));

        // Pre-fix: data-distance is absent entirely (server skips emission when null).
        // Post-fix: server always emits data-distance = 10 (the render-time default).
        var initialDist = h.GetAttribute(".mpl-3d-scene", "data-distance");
        Assert.NotNull(initialDist);
        var parsedInitial = double.Parse(initialDist, CultureInfo.InvariantCulture);
        Assert.Equal(10.0, parsedInitial, 1);

        h.Simulate(".mpl-3d-scene", "wheel", e => { e.deltaY = 100; });

        var afterDist = double.Parse(h.GetAttribute(".mpl-3d-scene", "data-distance")!, CultureInfo.InvariantCulture);
        Assert.NotEqual(parsedInitial, afterDist);
    }

    [Fact]
    public void Wheel_OnChartWithExplicitDistance_StillWorks()
    {
        // Regression guard: callers who already set distance explicitly must keep working.
        using var h = InteractionScriptHarness.FromBuilder(b => b
            .WithSize(600, 500)
            .WithBrowserInteraction()
            .AddSubPlot(1, 1, 1, ax => ax
                .WithCamera(elevation: 35, azimuth: -50, distance: 8)
                .Surface([0.0, 1.0], [0.0, 1.0], new double[,] { { 0, 1 }, { 1, 0 } })));

        var initialDist = double.Parse(h.GetAttribute(".mpl-3d-scene", "data-distance")!, CultureInfo.InvariantCulture);
        Assert.Equal(8.0, initialDist, 1);

        h.Simulate(".mpl-3d-scene", "wheel", e => { e.deltaY = 100; });

        var afterDist = double.Parse(h.GetAttribute(".mpl-3d-scene", "data-distance")!, CultureInfo.InvariantCulture);
        Assert.NotEqual(initialDist, afterDist);
    }
}
