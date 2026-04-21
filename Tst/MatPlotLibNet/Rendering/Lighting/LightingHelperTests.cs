// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.Lighting;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.Lighting;

/// <summary>Verifies <see cref="LightingHelper"/> face normals and <see cref="ColorExtensions"/>
/// color lighting operations.</summary>
public class LightingHelperTests
{
    [Fact]
    public void ComputeFaceNormal_XYPlane_ReturnsZAxis()
    {
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
    public void Modulate_FullIntensity_NoChange()
    {
        var color = Color.FromHex("#FF8040");
        var result = color.Modulate(1.0);
        Assert.Equal(color.R, result.R);
        Assert.Equal(color.G, result.G);
        Assert.Equal(color.B, result.B);
    }

    [Fact]
    public void Modulate_HalfIntensity_HalvesRGB()
    {
        var color = new Color(200, 100, 50, 255);
        var result = color.Modulate(0.5);
        Assert.Equal(100, result.R);
        Assert.Equal(50, result.G);
        Assert.Equal(25, result.B);
    }

    [Fact]
    public void Modulate_ZeroIntensity_Black()
    {
        var color = Color.FromHex("#FFFFFF");
        var result = color.Modulate(0.0);
        Assert.Equal(0, result.R);
        Assert.Equal(0, result.G);
        Assert.Equal(0, result.B);
    }

    [Fact]
    public void Modulate_PreservesAlpha()
    {
        var color = new Color(255, 255, 255, 128);
        var result = color.Modulate(0.5);
        Assert.Equal(128, result.A);
    }

    /// <summary>Shade's zero-length-vector guard: degenerate normal OR light direction
    /// returns the unmodified color (no shading possible without a defined orientation).</summary>
    [Theory]
    [InlineData(0.0, 0.0, 0.0, 0.0, 0.0, 1.0)]   // zero normal
    [InlineData(1.0, 0.0, 0.0, 0.0, 0.0, 0.0)]   // zero light direction
    public void Shade_DegenerateVector_ReturnsBaseColorUnchanged(
        double nx, double ny, double nz, double lx, double ly, double lz)
    {
        var baseColor = new Color(200, 150, 100, 255);
        var result = baseColor.Shade(nx, ny, nz, lx, ly, lz);
        Assert.Equal(baseColor.R, result.R);
        Assert.Equal(baseColor.G, result.G);
        Assert.Equal(baseColor.B, result.B);
        Assert.Equal(baseColor.A, result.A);
    }

    /// <summary>Front-facing normal (dot==1) → k=1.0; back-facing (dot==-1) → k=0.3
    /// (matplotlib ambient floor); perpendicular (dot==0) → k=0.65.</summary>
    [Theory]
    [InlineData(0.0, 0.0,  1.0, 1.0)]    // front-facing
    [InlineData(0.0, 0.0, -1.0, 0.3)]    // back-facing
    [InlineData(1.0, 0.0,  0.0, 0.65)]   // perpendicular
    public void Shade_DotProduct_MapsToMatplotlibK(double nx, double ny, double nz, double expectedK)
    {
        var baseColor = new Color(200, 200, 200, 255);
        var result = baseColor.Shade(nx, ny, nz, 0.0, 0.0, 1.0);
        Assert.Equal((byte)Math.Round(200 * expectedK), result.R);
    }
}
