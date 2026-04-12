// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies <see cref="PolarTransform"/> coordinate conversion.</summary>
public class PolarTransformTests
{
    private static PolarTransform CreateTransform() =>
        new(new Rect(0, 0, 400, 400), 10.0);

    /// <summary>Verifies that r=0 maps to the center of the plot area.</summary>
    [Fact]
    public void PolarToPixel_AtOrigin_ReturnsCenter()
    {
        var t = CreateTransform();
        var pt = t.PolarToPixel(0, 0);
        Assert.Equal(t.CenterX, pt.X, 1);
        Assert.Equal(t.CenterY, pt.Y, 1);
    }

    /// <summary>Verifies that r=rMax, theta=0 maps to the right edge.</summary>
    [Fact]
    public void PolarToPixel_MaxR_Theta0_MapsToRight()
    {
        var t = CreateTransform();
        var pt = t.PolarToPixel(10, 0);
        Assert.True(pt.X > t.CenterX);
        Assert.Equal(t.CenterY, pt.Y, 1);
    }

    /// <summary>Verifies that theta=PI/2 maps upward (Y decreases in pixel coords).</summary>
    [Fact]
    public void PolarToPixel_Theta90_MapsUp()
    {
        var t = CreateTransform();
        var pt = t.PolarToPixel(10, Math.PI / 2);
        Assert.Equal(t.CenterX, pt.X, 1);
        Assert.True(pt.Y < t.CenterY);
    }

    /// <summary>Verifies that theta=PI maps to the left.</summary>
    [Fact]
    public void PolarToPixel_Theta180_MapsLeft()
    {
        var t = CreateTransform();
        var pt = t.PolarToPixel(10, Math.PI);
        Assert.True(pt.X < t.CenterX);
        Assert.Equal(t.CenterY, pt.Y, 1);
    }

    /// <summary>Verifies that r values are clamped to rMax.</summary>
    [Fact]
    public void PolarToPixel_BeyondMax_IsClamped()
    {
        var t = CreateTransform();
        var atMax = t.PolarToPixel(10, 0);
        var beyond = t.PolarToPixel(20, 0);
        Assert.Equal(atMax.X, beyond.X, 1);
    }

    /// <summary>Verifies that CenterX and CenterY are at the midpoint.</summary>
    [Fact]
    public void Center_IsAtMidpoint()
    {
        var t = CreateTransform();
        Assert.Equal(200, t.CenterX, 1);
        Assert.Equal(200, t.CenterY, 1);
    }
}
