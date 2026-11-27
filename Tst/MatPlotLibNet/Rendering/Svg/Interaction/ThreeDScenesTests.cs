// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Rendering.Svg.Interaction;

public sealed class ThreeDScenesTests
{
    [Fact]
    public void TheCameraAngles_AreFiveRows_OfElevationAndAzimuth()
    {
        var rows = new CameraAngles().Select(row => (row.Data.Item1, row.Data.Item2)).ToArray();

        Assert.Equal(5, rows.Length);
        Assert.Equal((30, -60), rows[0]);
        Assert.Equal((60, -30), rows[^1]);
    }

    [Fact]
    public void AThreeDScene_HoldsOneScene_AtTheCameraGiven()
    {
        using var h = InteractionScriptHarness.ThreeDScene(elevation: 35, azimuth: -50, distance: 9);

        Assert.Contains("mpl-3d-scene", h.Svg, StringComparison.Ordinal);
        Assert.Equal(35.0, double.Parse(h.GetAttribute(".mpl-3d-scene", "data-elevation")!, System.Globalization.CultureInfo.InvariantCulture));
    }
}
