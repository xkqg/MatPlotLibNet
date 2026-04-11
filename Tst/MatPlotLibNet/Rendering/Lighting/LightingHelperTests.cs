// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.Lighting;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.Lighting;

/// <summary>Verifies <see cref="LightingHelper"/> face normal and color modulation.</summary>
public class LightingHelperTests
{
    [Fact]
    public void ComputeFaceNormal_XYPlane_ReturnsZAxis()
    {
        // Three points in the XY plane → normal should point along Z
        var (nx, ny, nz) = LightingHelper.ComputeFaceNormal(
            (0, 0, 0), (1, 0, 0), (0, 1, 0));
        Assert.Equal(0.0, nx, 6);
        Assert.Equal(0.0, ny, 6);
        Assert.Equal(1.0, nz, 6);
    }

    [Fact]
    public void ComputeFaceNormal_ReversedOrder_OppositeNormal()
    {
        var (nx1, ny1, nz1) = LightingHelper.ComputeFaceNormal(
            (0, 0, 0), (1, 0, 0), (0, 1, 0));
        var (nx2, ny2, nz2) = LightingHelper.ComputeFaceNormal(
            (0, 0, 0), (0, 1, 0), (1, 0, 0));
        Assert.Equal(-nx1, nx2, 6);
        Assert.Equal(-ny1, ny2, 6);
        Assert.Equal(-nz1, nz2, 6);
    }

    [Fact]
    public void ModulateColor_FullIntensity_NoChange()
    {
        var color = Color.FromHex("#FF8040");
        var result = LightingHelper.ModulateColor(color, 1.0);
        Assert.Equal(color.R, result.R);
        Assert.Equal(color.G, result.G);
        Assert.Equal(color.B, result.B);
    }

    [Fact]
    public void ModulateColor_HalfIntensity_HalvesRGB()
    {
        var color = new Color(200, 100, 50, 255);
        var result = LightingHelper.ModulateColor(color, 0.5);
        Assert.Equal(100, result.R);
        Assert.Equal(50, result.G);
        Assert.Equal(25, result.B);
    }

    [Fact]
    public void ModulateColor_ZeroIntensity_Black()
    {
        var color = Color.FromHex("#FFFFFF");
        var result = LightingHelper.ModulateColor(color, 0.0);
        Assert.Equal(0, result.R);
        Assert.Equal(0, result.G);
        Assert.Equal(0, result.B);
    }

    [Fact]
    public void ModulateColor_PreservesAlpha()
    {
        var color = new Color(255, 255, 255, 128);
        var result = LightingHelper.ModulateColor(color, 0.5);
        Assert.Equal(128, result.A);
    }
}
