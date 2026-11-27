// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>The line an annotation draws from its text to its target: one <see cref="ConnectionPath"/> for each
/// <see cref="ConnectionStyle"/>, chosen in one place, and the geometry of each.</summary>
public class ConnectionPathTests
{
    [Theory]
    [InlineData(ConnectionStyle.Straight, typeof(StraightConnection))]
    [InlineData(ConnectionStyle.Arc3, typeof(Arc3Connection))]
    [InlineData(ConnectionStyle.Angle, typeof(AngleConnection))]
    [InlineData(ConnectionStyle.Angle3, typeof(Angle3Connection))]
    [InlineData((ConnectionStyle)99, typeof(StraightConnection))]
    public void EveryStyle_HasItsPath(ConnectionStyle style, Type path) => Assert.IsType(path, ConnectionPath.Of(style));

    /// <summary>Verifies that Straight returns a MoveTo followed by a LineTo segment.</summary>
    [Fact]
    public void Straight_ReturnsMoveAndLine()
    {
        var from = new Point(0, 0);
        var to   = new Point(100, 50);

        var path = ConnectionPath.Of(ConnectionStyle.Straight).Between(from, to);

        Assert.Equal(2, path.Count);
        Assert.IsType<MoveToSegment>(path[0]);
        Assert.IsType<LineToSegment>(path[1]);
        Assert.Equal(from, ((MoveToSegment)path[0]).Point);
        Assert.Equal(to,   ((LineToSegment)path[1]).Point);
    }

    /// <summary>Verifies that Arc3 returns a MoveTo followed by a BezierSegment.</summary>
    [Fact]
    public void Arc3_ReturnsMoveAndBezier()
    {
        var from = new Point(0, 0);
        var to   = new Point(100, 0);

        var path = ConnectionPath.Of(ConnectionStyle.Arc3).Between(from, to);

        Assert.Equal(2, path.Count);
        Assert.IsType<MoveToSegment>(path[0]);
        Assert.IsType<BezierSegment>(path[1]);
    }

    /// <summary>Verifies that Arc3 control points are offset from the midpoint perpendicular to the line.</summary>
    [Fact]
    public void Arc3_ControlPointsOffsetFromMidpoint()
    {
        var from = new Point(0, 0);
        var to   = new Point(100, 0);
        double rad = 0.3;

        var path = ConnectionPath.Of(ConnectionStyle.Arc3).Between(from, to, rad);

        var bezier = (BezierSegment)path[1];
        double midX = 50;
        double expectedOffset = rad * 100; // rad * length

        // Control points should be offset from midpoint along the perpendicular (Y axis here)
        Assert.Equal(midX, bezier.Control1.X, precision: 3);
        Assert.Equal(midX, bezier.Control2.X, precision: 3);
        Assert.InRange(Math.Abs(bezier.Control1.Y), expectedOffset * 0.9, expectedOffset * 1.1);
    }

    /// <summary>Verifies that Angle returns exactly three segments (MoveTo + 2×LineTo).</summary>
    [Fact]
    public void Angle_ReturnsThreeSegments()
    {
        var from = new Point(0, 0);
        var to   = new Point(100, 50);

        var path = ConnectionPath.Of(ConnectionStyle.Angle).Between(from, to);

        Assert.Equal(3, path.Count);
        Assert.IsType<MoveToSegment>(path[0]);
        Assert.IsType<LineToSegment>(path[1]);
        Assert.IsType<LineToSegment>(path[2]);
    }

    /// <summary>Verifies that the Angle corner point is at (to.X, from.Y).</summary>
    [Fact]
    public void Angle_CornerAtExpectedPosition()
    {
        var from = new Point(10, 20);
        var to   = new Point(80, 60);

        var path = ConnectionPath.Of(ConnectionStyle.Angle).Between(from, to);

        var corner = ((LineToSegment)path[1]).Point;
        Assert.Equal(to.X,   corner.X, precision: 3);
        Assert.Equal(from.Y, corner.Y, precision: 3);
    }

    /// <summary>Verifies that Angle3 returns a smooth multi-segment path (at least 3 segments).</summary>
    [Fact]
    public void Angle3_ReturnsSmoothPath()
    {
        var from = new Point(0, 0);
        var to   = new Point(100, 50);

        var path = ConnectionPath.Of(ConnectionStyle.Angle3).Between(from, to);

        // Must have at least MoveTo + 2 bezier segments for a smooth right-angle
        Assert.True(path.Count >= 3, $"Expected ≥3 segments, got {path.Count}");
        Assert.IsType<MoveToSegment>(path[0]);
    }

    /// <summary>Verifies that Straight with zero-length (from == to) does not throw.</summary>
    [Fact]
    public void Straight_ZeroLength_NoException()
    {
        var pt = new Point(50, 50);
        var path = ConnectionPath.Of(ConnectionStyle.Straight).Between(pt, pt);
        Assert.NotNull(path);
    }
}
