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
}
