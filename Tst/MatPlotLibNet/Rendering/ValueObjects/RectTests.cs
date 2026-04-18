// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Rendering.ValueObjects;

/// <summary>Verifies <see cref="Rect"/> derived properties and methods.</summary>
public class RectTests
{
    [Fact]
    public void Right_IsXPlusWidth()
        => Assert.Equal(15.0, new Rect(5, 0, 10, 0).Right);

    [Fact]
    public void Bottom_IsYPlusHeight()
        => Assert.Equal(8.0, new Rect(0, 3, 0, 5).Bottom);

    [Fact]
    public void Center_IsMidpoint()
    {
        var c = new Rect(0, 0, 10, 20).Center;
        Assert.Equal(5, c.X);
        Assert.Equal(10, c.Y);
    }

    [Fact]
    public void Intersects_OverlappingRects_ReturnsTrue()
        => Assert.True(new Rect(0, 0, 10, 10).Intersects(new Rect(5, 5, 10, 10)));

    [Fact]
    public void Intersects_TouchingEdges_ReturnsFalse()
        => Assert.False(new Rect(0, 0, 10, 10).Intersects(new Rect(10, 0, 10, 10)));

    [Fact]
    public void Intersects_DisjointRects_ReturnsFalse()
        => Assert.False(new Rect(0, 0, 5, 5).Intersects(new Rect(20, 20, 5, 5)));

    [Fact]
    public void Inflate_ExpandsBothDimensions()
    {
        var r = new Rect(10, 20, 30, 40).Inflate(2, 3);
        Assert.Equal(8, r.X);
        Assert.Equal(17, r.Y);
        Assert.Equal(34, r.Width);
        Assert.Equal(46, r.Height);
    }

    [Fact]
    public void Inflate_NegativeValues_Shrinks()
    {
        var r = new Rect(0, 0, 10, 10).Inflate(-1, -2);
        Assert.Equal(1, r.X);
        Assert.Equal(2, r.Y);
        Assert.Equal(8, r.Width);
        Assert.Equal(6, r.Height);
    }
}
