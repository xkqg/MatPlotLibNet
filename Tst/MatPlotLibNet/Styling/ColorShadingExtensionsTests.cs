// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Styling;

/// <summary>J.0.b — TDD tests for <c>Color.Shade</c> and <c>Color.Modulate</c>
/// extension methods (refactored from <c>LightingHelper.ShadeColor/ModulateColor</c>).</summary>
public class ColorShadingExtensionsTests
{
    [Fact]
    public void Shade_ZeroNormal_ReturnsBaseColor()
    {
        var result = Colors.Red.Shade(0, 0, 0, 1, 0, 0);
        Assert.Equal(Colors.Red, result);
    }

    [Fact]
    public void Shade_FrontFacing_BrighterThanBackFacing()
    {
        var front = Colors.White.Shade(0, 0, 1, 0, 0, 1);
        var back  = Colors.White.Shade(0, 0, -1, 0, 0, 1);
        Assert.True(front.R >= back.R);
    }

    [Fact]
    public void Shade_ResultPreservesAlpha()
    {
        var color = new Color(200, 100, 50, 128);
        var result = color.Shade(0, 0, 1, 0, 0, 1);
        Assert.Equal(128, result.A);
    }

    [Fact]
    public void Modulate_ZeroIntensity_ReturnsBlack()
    {
        var result = Colors.Red.Modulate(0.0);
        Assert.Equal(0, result.R);
        Assert.Equal(0, result.G);
        Assert.Equal(0, result.B);
        Assert.Equal(Colors.Red.A, result.A);
    }

    [Fact]
    public void Modulate_FullIntensity_ReturnsSameColor()
    {
        var result = Colors.Green.Modulate(1.0);
        Assert.Equal(Colors.Green.R, result.R);
        Assert.Equal(Colors.Green.G, result.G);
        Assert.Equal(Colors.Green.B, result.B);
    }

    [Fact]
    public void Modulate_ClampedAboveOne_TreatedAsOne()
    {
        var clamped = Colors.White.Modulate(2.0);
        var full    = Colors.White.Modulate(1.0);
        Assert.Equal(full, clamped);
    }

    [Fact]
    public void Modulate_ResultPreservesAlpha()
    {
        var color = new Color(100, 100, 100, 200);
        Assert.Equal(200, color.Modulate(0.5).A);
    }

    /// <summary>A shade is each channel times the intensity at the nearest step, as matplotlib's <c>_shade_colors</c> multiplies the
    /// channels and its writers round them (<c>to_hex</c>, Agg): 255 at 0.67 is 170.85, drawn 171.</summary>
    [Theory]
    [InlineData(255, 0.67, 171)]
    [InlineData(255, 0.5, 128)]
    [InlineData(101, 0.5, 50)]
    public void Modulate_RoundsEachChannelToTheNearestStep(int channel, double intensity, int expected)
    {
        var shaded = new Color((byte)channel, (byte)channel, (byte)channel, 77).Modulate(intensity);

        Assert.Equal(new Color((byte)expected, (byte)expected, (byte)expected, 77), shaded);
    }

    /// <summary>The directional light shades by the one rule: the colour modulated at the light's intensity for the face.</summary>
    [Fact]
    public void ADirectionalLight_ShadesAsModulateAtItsIntensity()
    {
        var light = new MatPlotLibNet.Rendering.Lighting.DirectionalLight(0.3, -0.5, 0.8);
        var colour = new Color(201, 97, 33, 140);

        foreach (var (nx, ny, nz) in new[] { (0.0, 0.0, 1.0), (1.0, 0.2, 0.1), (-0.4, 0.7, -0.6) })
            Assert.Equal(colour.Modulate(light.ComputeIntensity(nx, ny, nz)), light.Shade(colour, nx, ny, nz));
    }
}
