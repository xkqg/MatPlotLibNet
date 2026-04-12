// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.Lighting;

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

    [Fact]
    public void DirectionalLight_Perpendicular_AmbientOnly()
    {
        // Light from +Z, face normal pointing +X → dot product = 0 → only ambient
        var light = new DirectionalLight(0, 0, 1, Ambient: 0.3, Diffuse: 0.7);
        double intensity = light.ComputeIntensity(1, 0, 0);
        Assert.Equal(0.3, intensity, 6);
    }

    [Fact]
    public void DirectionalLight_DefaultValues_AreCorrect()
    {
        var light = new DirectionalLight(0, 0, 1);
        Assert.Equal(0.3, light.Ambient);
        Assert.Equal(0.7, light.Diffuse);
    }
}
