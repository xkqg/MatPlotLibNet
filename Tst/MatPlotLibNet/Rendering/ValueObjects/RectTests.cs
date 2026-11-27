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

    /// <summary>The shorter side of a rectangle, the one length a round chart's circle, a 3-D box's square and a rounded corner's
    /// largest radius are measured from, whichever way the rectangle lies.</summary>
    [Theory]
    [InlineData(10.0, 20.0, 10.0)]
    [InlineData(37.5, 12.25, 12.25)]
    [InlineData(8.0, 8.0, 8.0)]
    public void ShorterSide_IsTheLesserOfWidthAndHeight(double width, double height, double shorter)
        => Assert.Equal(shorter, new Rect(3, 4, width, height).ShorterSide);

    /// <summary>Two rectangles that overlap intersect; two that only touch at an edge, or lie apart, do not.</summary>
    [Theory]
    [InlineData(5, 5, true)]
    [InlineData(10, 0, false)]
    [InlineData(20, 20, false)]
    public void Intersects_OnlyRectanglesThatOverlap(double x, double y, bool intersects)
        => Assert.Equal(intersects, new Rect(0, 0, 10, 10).Intersects(new Rect(x, y, 10, 10)));

    [Fact]
    public void Inflate_ExpandsBothDimensions()
    {
        var r = new Rect(10, 20, 30, 40).Inflate(2, 3);
        Assert.Equal(8, r.X);
        Assert.Equal(17, r.Y);
        Assert.Equal(34, r.Width);
        Assert.Equal(46, r.Height);
    }

    /// <summary>The four orders two opposite corners can be handed in give one rectangle, of positive size.</summary>
    [Theory]
    [InlineData(10, 20, 40, 60)]
    [InlineData(40, 60, 10, 20)]
    [InlineData(10, 60, 40, 20)]
    [InlineData(40, 20, 10, 60)]
    public void FromCorners_AnyTwoOppositeCorners_GiveTheRectangleBetweenThem(double ax, double ay, double bx, double by)
    {
        var r = Rect.FromCorners(new Point(ax, ay), new Point(bx, by));

        Assert.Equal(new Rect(10, 20, 30, 40), r);
    }

    /// <summary>Corners handed over top left first give the very numbers the construction every renderer used gives, to
    /// the last bit, so an upright axis draws the bytes it drew.</summary>
    [Theory]
    [InlineData(0.1, 0.2, 0.30000000000000004, 0.7)]
    [InlineData(62.000000000000014, 42, 449.99999999999994, 312.00000000000006)]
    [InlineData(-7.5, -1e-300, 1e300, 0)]
    public void FromCorners_TopLeftFirst_IsTheUprightConstruction_BitForBit(double ax, double ay, double bx, double by)
    {
        var r = Rect.FromCorners(new Point(ax, ay), new Point(bx, by));

        Assert.Equal(new Rect(ax, ay, bx - ax, by - ay), r);
    }

    /// <summary>Two corners on one line span a rectangle of no width, not of a negative one.</summary>
    [Fact]
    public void FromCorners_OnOneVerticalLine_HasNoWidth()
    {
        var r = Rect.FromCorners(new Point(5, 30), new Point(5, 10));

        Assert.Equal(new Rect(5, 10, 0, 20), r);
    }

    /// <summary>A corner the axes could not place (NaN) gives a rectangle that cannot be drawn either.</summary>
    [Fact]
    public void FromCorners_ACornerThatIsNotANumber_GivesARectangleThatIsNotFinite()
    {
        var r = Rect.FromCorners(new Point(double.NaN, 10), new Point(40, 60));

        Assert.True(double.IsNaN(r.X) && double.IsNaN(r.Width));
        Assert.Equal(10, r.Y);
        Assert.Equal(50, r.Height);
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

    /// <summary>The smallest rectangle that holds every rectangle of a list, as matplotlib's <c>Bbox.union</c> makes it.</summary>
    [Fact]
    public void Enclosing_HoldsEveryRectangle()
    {
        var box = Rect.Enclosing([new Rect(10, 20, 30, 40), new Rect(50, 5, 10, 10), new Rect(0, 30, 5, 5)]);

        Assert.Equal(new Rect(0, 5, 60, 55), box);
    }

    [Fact]
    public void Enclosing_OfNoRectangle_IsNothing() => Assert.Null(Rect.Enclosing([]));

    /// <summary>A rectangle moved by the transform that maps one box onto another, as matplotlib's <c>BboxTransform(from, to)</c>
    /// moves it: its position and its size are scaled by the ratio of the boxes, on each axis.</summary>
    [Fact]
    public void Remapped_ScalesPositionAndSizeByTheRatioOfTheBoxes()
    {
        var from = new Rect(100, 50, 600, 400);
        var to = new Rect(100, 50, 480, 200);

        Assert.Equal(new Rect(100 + 0.8 * 150, 50 + 0.5 * 100, 0.8 * 200, 0.5 * 80), new Rect(250, 150, 200, 80).Remapped(from, to));
        Assert.Equal(to, from.Remapped(from, to));
    }
}
