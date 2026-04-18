// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace MatPlotLibNet.Tests.Rendering.Svg.Interaction;

/// <summary>Phase G.2 of the v1.7.2 follow-on plan — keyboard parity for 3D
/// rotate / zoom / reset. Pins the handler in <c>Svg3DRotationScript.cs</c>
/// (keydown branch) against every documented shortcut so keyboard users
/// match what the wiki Keyboard-Shortcuts page advertises.
///
/// <para>Shortcuts under test (mirrors matplotlib axes3d interactive keymap
/// extended for the browser): Arrows = ±5° az/el, +/- = ±0.5 distance,
/// Home = restore initial el/az/distance.</para></summary>
public class ThreeDKeyboardTests
{
    private static InteractionScriptHarness Build3D() =>
        InteractionScriptHarness.FromBuilder(b => b
            .WithSize(600, 500)
            .WithBrowserInteraction()
            .AddSubPlot(1, 1, 1, ax => ax
                .WithCamera(elevation: 30, azimuth: -60, distance: 8)
                .Surface([0.0, 1.0], [0.0, 1.0], new double[,] { { 0, 1 }, { 1, 0 } })));

    private static double Read(InteractionScriptHarness h, string attr) =>
        double.Parse(h.GetAttribute(".mpl-3d-scene", attr)!, CultureInfo.InvariantCulture);

    public static IEnumerable<object[]> RotationKeyCases() =>
    [
        // key, expectedAzimDelta, expectedElevDelta
        ["ArrowLeft",  -5.0,  0.0],
        ["ArrowRight",  5.0,  0.0],
        ["ArrowUp",     0.0,  5.0],
        ["ArrowDown",   0.0, -5.0],
    ];

    [Theory]
    [MemberData(nameof(RotationKeyCases))]
    public void ArrowKeys_RotateCameraByFiveDegrees(string key, double expectedDaz, double expectedDel)
    {
        using var h = Build3D();
        var az0 = Read(h, "data-azimuth");
        var el0 = Read(h, "data-elevation");

        h.Simulate(".mpl-3d-scene", "keydown", e => { e.key = key; });

        Assert.Equal(expectedDaz, Read(h, "data-azimuth") - az0, 3);
        Assert.Equal(expectedDel, Read(h, "data-elevation") - el0, 3);
    }

    public static IEnumerable<object[]> ZoomKeyCases() =>
    [
        // key, expectedDistanceDelta
        ["+", -0.5],  // zoom in → distance shrinks
        ["-",  0.5],
    ];

    [Theory]
    [MemberData(nameof(ZoomKeyCases))]
    public void PlusMinusKeys_ChangeCameraDistance(string key, double expectedDdist)
    {
        using var h = Build3D();
        var d0 = Read(h, "data-distance");

        h.Simulate(".mpl-3d-scene", "keydown", e => { e.key = key; });

        Assert.Equal(expectedDdist, Read(h, "data-distance") - d0, 3);
    }

    [Fact]
    public void HomeKey_RestoresInitialCameraState()
    {
        using var h = Build3D();
        var az0 = Read(h, "data-azimuth");
        var el0 = Read(h, "data-elevation");
        var d0  = Read(h, "data-distance");

        // Move camera via multiple keys then reset.
        h.Simulate(".mpl-3d-scene", "keydown", e => { e.key = "ArrowRight"; });
        h.Simulate(".mpl-3d-scene", "keydown", e => { e.key = "ArrowUp"; });
        h.Simulate(".mpl-3d-scene", "keydown", e => { e.key = "+"; });
        Assert.NotEqual(az0, Read(h, "data-azimuth"));
        Assert.NotEqual(el0, Read(h, "data-elevation"));
        Assert.NotEqual(d0,  Read(h, "data-distance"));

        h.Simulate(".mpl-3d-scene", "keydown", e => { e.key = "Home"; });

        Assert.Equal(az0, Read(h, "data-azimuth"), 3);
        Assert.Equal(el0, Read(h, "data-elevation"), 3);
        Assert.Equal(d0,  Read(h, "data-distance"), 3);
    }
}
