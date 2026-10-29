// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.Lighting;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.Lighting;

/// <summary>Verifies <see cref="DirectionalLight"/> intensity computation.</summary>
public class DirectionalLightTests
{
    [Fact]
    public void DirectionalLight_FacingLight_MaxIntensity()
    {
        // Light from +Z, face normal pointing +Z → max diffuse
        var light = new DirectionalLight(0, 0, 1, Ambient: 0.3, Diffuse: 0.7);
        double intensity = light.ComputeIntensity(0, 0, 1);
        Assert.Equal(1.0, intensity, 6);
    }

    [Fact]
    public void DirectionalLight_FacingAway_AmbientOnly()
    {
        // Light from +Z, face normal pointing -Z → no diffuse, only ambient
        var light = new DirectionalLight(0, 0, 1, Ambient: 0.3, Diffuse: 0.7);
        double intensity = light.ComputeIntensity(0, 0, -1);
        Assert.Equal(0.3, intensity, 6);
    }

    /// <summary>A face edge-on to the light gets the middle of the range, ambient plus half the diffuse: the intensity maps the
    /// dot product from [-1, 1] onto [Ambient, Ambient + Diffuse], which is matplotlib's mapping with its constants named (its
    /// 0.65 + 0.35 · dot is 0.3 + 0.7 · (dot + 1) / 2). Before, an edge-on face got the ambient alone, the Lambertian rule, which
    /// is not what the surfaces and the bars of the other renderers were shaded by.</summary>
    [Fact]
    public void DirectionalLight_Perpendicular_IsAmbientPlusHalfTheDiffuse()
    {
        // Light from +Z, face normal pointing +X → dot product = 0
        var light = new DirectionalLight(0, 0, 1, Ambient: 0.3, Diffuse: 0.7);
        double intensity = light.ComputeIntensity(1, 0, 0);
        Assert.Equal(0.65, intensity, 6);
    }

    /// <summary>The intensity is ambient + diffuse · (dot + 1) / 2, held to [0, 1], for any ambient and diffuse (they need not add
    /// up to 1): (0.1, 0.5) runs 0.1 to 0.6, (0.2, 0.2) 0.2 to 0.4, (0, 0) is black, and (1, 1) saturates at 1.</summary>
    [Theory]
    [InlineData(0.1, 0.5, 0, 0, 1, 0.6)]      // facing the light
    [InlineData(0.1, 0.5, 0, 0, -1, 0.1)]     // facing away
    [InlineData(0.1, 0.5, 1, 0, 0, 0.35)]     // edge-on
    [InlineData(0.2, 0.2, 0, 0, 1, 0.4)]
    [InlineData(0.2, 0.2, 0, 0, -1, 0.2)]
    [InlineData(0.0, 0.0, 0, 0, 1, 0.0)]      // no light at all: black
    [InlineData(1.0, 1.0, 0, 0, 1, 1.0)]      // 2 held to 1
    [InlineData(1.0, 1.0, 1, 0, 0, 1.0)]      // 1.5 held to 1
    public void DirectionalLight_Intensity_IsAmbientPlusDiffuseTimesHalfOnePlusDot(
        double ambient, double diffuse, double nx, double ny, double nz, double expected)
    {
        var light = new DirectionalLight(0, 0, 1, ambient, diffuse);

        Assert.Equal(expected, light.ComputeIntensity(nx, ny, nz), 9);
    }

    [Fact]
    public void DirectionalLight_AtItsDefaults_IsMatplotlibsShade()
    {
        var light = new DirectionalLight(0.5, -0.5, 1);
        foreach (var (nx, ny, nz) in new[] { (0.0, 0.0, 1.0), (0.3, 0.2, 0.9), (-1.0, 0.0, 0.0), (0.0, 1.0, -1.0), (0.7, -0.7, 0.1) })
        {
            double dot = (nx * 0.5 + ny * -0.5 + nz * 1) / (Math.Sqrt(nx * nx + ny * ny + nz * nz) * Math.Sqrt(0.5 * 0.5 + 0.5 * 0.5 + 1));
            Assert.Equal(0.65 + 0.35 * dot, light.ComputeIntensity(nx, ny, nz), 9);
        }
    }

    // ---- the light shades ------------------------------------------------------------------------------------------------

    /// <summary>A directional light shades a colour by rounding each channel scaled by the intensity (matplotlib's
    /// <c>_shade_colors</c> does), where the default shading of a light scales and truncates.</summary>
    [Fact]
    public void DirectionalLight_Shade_RoundsEachChannelTimesTheIntensity()
    {
        ILightSource light = new DirectionalLight(0, 0, 1, Ambient: 0.3, Diffuse: 0.7);
        var color = new Color(201, 100, 55, 90);

        var shaded = light.Shade(color, 1, 0, 0);          // intensity 0.65

        Assert.Equal(new Color((byte)Math.Round(201 * 0.65), (byte)Math.Round(100 * 0.65), (byte)Math.Round(55 * 0.65), 90), shaded);
    }

    /// <summary>A light of the caller's own that says nothing about shading scales the colour by its intensity.</summary>
    [Fact]
    public void ALightOfYourOwn_ShadesByItsIntensity_ByDefault()
    {
        ILightSource light = new Half();

        Assert.Equal(new Color(100, 50, 25, 255), light.Shade(new Color(200, 100, 50, 255), 0, 0, 1));
    }

    private sealed class Half : ILightSource
    {
        public double ComputeIntensity(double nx, double ny, double nz) => 0.5;
    }

    /// <summary>The public six-argument <c>Color.Shade</c> is the light's own shading: the same colour, whichever way it is asked.</summary>
    [Fact]
    public void ColorShade_IsTheLightsShade()
    {
        var color = new Color(180, 140, 60, 255);
        ILightSource light = new DirectionalLight(1, 1, 1);

        Assert.Equal(light.Shade(color, 0.2, -0.4, 0.9), color.Shade(0.2, -0.4, 0.9, 1, 1, 1));
    }

    /// <summary>At the defaults the shade equals the old constants (0.65 + 0.35 · dot, held to [0.3, 1]) to one step of one channel,
    /// and differs only where the product lands exactly between two steps, which the two ways of working it out round
    /// differently.</summary>
    [Fact]
    public void AtTheDefaults_TheShade_EqualsTheOldConstants_ExceptAtAnExactTie()
    {
        ILightSource light = new DirectionalLight(0.4, -0.3, 0.8);
        double lLen = Math.Sqrt(0.4 * 0.4 + 0.3 * 0.3 + 0.8 * 0.8);
        int differing = 0;
        foreach (var (nx, ny, nz) in new[] { (0.0, 0.0, 1.0), (0.0, 0.0, -1.0), (1.0, 0.0, 0.0), (-1.0, 0.0, 0.0), (0.0, 1.0, 0.0), (0.0, -1.0, 0.0), (0.3, 0.2, 0.9) })
        {
            double dot = (nx * 0.4 + ny * -0.3 + nz * 0.8) / (Math.Sqrt(nx * nx + ny * ny + nz * nz) * lLen);
            double oldK = Math.Clamp(0.65 + 0.35 * dot, 0.3, 1.0);
            for (int channel = 0; channel < 256; channel++)
            {
                byte old = (byte)Math.Round(channel * oldK);
                byte now = light.Shade(new Color((byte)channel, 0, 0), nx, ny, nz).R;
                if (old == now) continue;
                differing++;
                Assert.True(Math.Abs(old - now) == 1 && Math.Abs(channel * oldK - Math.Floor(channel * oldK) - 0.5) < 1e-6,
                    $"channel {channel}, normal ({nx}, {ny}, {nz}): old {old}, now {now}, product {channel * oldK}");
            }
        }
        Assert.True(differing < 40, $"{differing} channel values moved");
    }

    [Fact]
    public void DirectionalLight_DefaultValues_AreCorrect()
    {
        var light = new DirectionalLight(0, 0, 1);
        Assert.Equal(0.3, light.Ambient);
        Assert.Equal(0.7, light.Diffuse);
    }

    /// <summary>Phase X.2.b (v1.7.2, 2026-04-19) — zero-length light-direction vector
    /// trips the `lLen &lt; 1e-10` guard arm at line 21, returning Ambient + Diffuse.
    /// Pre-X this branch was unhit (DirectionalLight pinned at 100%L / 50%B).</summary>
    [Theory]
    [InlineData(0.0, 0.0, 0.0, 1.0, 0.0, 0.0)]   // zero light direction (lLen == 0)
    [InlineData(1.0, 0.0, 0.0, 0.0, 0.0, 0.0)]   // zero normal vector (nLen == 0)
    [InlineData(0.0, 0.0, 0.0, 0.0, 0.0, 0.0)]   // both zero — guard short-circuits on first
    public void DirectionalLight_DegenerateVector_IsFullLight(
        double dx, double dy, double dz, double nx, double ny, double nz)
    {
        var light = new DirectionalLight(dx, dy, dz, Ambient: 0.3, Diffuse: 0.7);
        double intensity = light.ComputeIntensity(nx, ny, nz);
        Assert.Equal(1.0, intensity, 6);    // no direction to light by: the colour as it is
    }

    /// <summary>A face or a light with no direction is not shaded, whatever the ambient and the diffuse are.</summary>
    [Fact]
    public void DirectionalLight_DegenerateVector_ShadesNothing_WhateverTheAmbientAndTheDiffuse()
    {
        ILightSource light = new DirectionalLight(0, 0, 0, Ambient: 0.1, Diffuse: 0.2);

        Assert.Equal(new Color(10, 20, 30, 40), light.Shade(new Color(10, 20, 30, 40), 0, 0, 1));
    }
}
