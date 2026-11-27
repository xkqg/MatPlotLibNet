// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.SeriesRenderers;

namespace MatPlotLibNet.Tests.Rendering.SeriesRenderers;

/// <summary>The path of a <see cref="RingWedge"/>: a solid slice from the centre when it has no inner radius, a ring wedge
/// between two arcs when it has one.</summary>
public class RingWedgeTests
{
    [Fact]
    public void Path_SolidSlice_FirstSegmentIsMoveToCentre()
    {
        // innerR == 0 → solid wedge starts from the centre of the circle.
        var segments = new RingWedge(100, 150, 0, 60, 0, 90).Path();

        var move = Assert.IsType<MoveToSegment>(segments[0]);
        Assert.Equal(100, move.Point.X, 1e-6);
        Assert.Equal(150, move.Point.Y, 1e-6);
    }

    [Fact]
    public void Path_SolidSlice_RunsCentreOuterStartOuterArcClose()
    {
        var segments = new RingWedge(100, 150, 0, 60, 0, 90).Path();

        Assert.Equal(4, segments.Count);
        var line = Assert.IsType<LineToSegment>(segments[1]);
        Assert.Equal(160, line.Point.X, 1e-6);   // cx + outerR * cos(0)
        Assert.Equal(150, line.Point.Y, 1e-6);
        var arc = Assert.IsType<ArcSegment>(segments[2]);
        Assert.Equal(60, arc.RadiusX);
        Assert.Equal(0, arc.StartAngle);
        Assert.Equal(90, arc.EndAngle);
        Assert.IsType<CloseSegment>(segments[3]);
    }

    [Fact]
    public void Path_RingWedge_FirstSegmentIsOnInnerArc()
    {
        // innerR > 0 → ring wedge starts from the inner-arc start point, NOT the centre.
        double cx = 200, cy = 200, innerR = 30;
        var segments = new RingWedge(cx, cy, innerR, 60, 0, 90).Path();

        var move = Assert.IsType<MoveToSegment>(segments[0]);
        // At startAngle 0°: inner arc start = (cx + innerR, cy)
        Assert.NotEqual(cx, move.Point.X, 1.0);  // must not be the centre
        Assert.Equal(cx + innerR, move.Point.X, 1e-6);
        Assert.Equal(cy, move.Point.Y, 1e-6);
    }

    [Fact]
    public void Path_HalfCircle_LineToInnerArcEndMatchesEndAngle()
    {
        // startAngle=0°, endAngle=180°, innerR=30
        // Ring path: [MoveToSegment, LineToSegment(outerStart), ArcSegment(outer),
        //             LineToSegment(innerEnd), ArcSegment(inner back), CloseSegment]
        // The 4th segment (index 3) is LineToSegment at inner arc end.
        // innerR * cos(180°) = -30, innerR * sin(180°) ≈ 0  →  (cx - 30, cy)
        double cx = 100, cy = 100, innerR = 30;
        var segments = new RingWedge(cx, cy, innerR, 60, 0, 180).Path();

        Assert.Equal(6, segments.Count);
        var line = Assert.IsType<LineToSegment>(segments[3]);
        Assert.Equal(cx - innerR, line.Point.X, 1e-6);
        Assert.Equal(cy, line.Point.Y, 1e-6);
        var back = Assert.IsType<ArcSegment>(segments[4]);
        Assert.Equal(innerR, back.RadiusX);
        Assert.Equal(180, back.StartAngle);   // the inner arc runs back from the end angle to the start
        Assert.Equal(0, back.EndAngle);
        Assert.IsType<CloseSegment>(segments[5]);
    }

    [Fact]
    public void Path_ANegativeInnerRadius_IsASolidSlice()
    {
        Assert.Equal(4, new RingWedge(0, 0, -5, 10, 0, 45).Path().Count);
    }

    // ── The wedge as a polygon: the one resolution every chart that draws a sector as a polyline draws it at ──────────

    [Theory]
    [InlineData(360, 120)]
    [InlineData(180, 60)]
    [InlineData(90, 30)]
    [InlineData(-90, 30)]
    [InlineData(1, 3)]
    [InlineData(0, 3)]
    [InlineData(12.000000000000004, 4)]   // a thirtieth of a turn, a rounding error over
    [InlineData(11.999999999999998, 4)]
    [InlineData(12.5, 5)]
    public void TheStepsOfASweep_AreItsShareOfATurnAtTheNamedResolution_NeverFewerThanThree(double sweepDegrees, int steps)
    {
        Assert.Equal(steps, RingWedge.StepsFor(sweepDegrees));
    }

    [Fact]
    public void TheResolution_IsOneHundredAndTwentyStepsToTheTurn()
    {
        Assert.Equal(120, RingWedge.StepsPerTurn);
    }

    [Fact]
    public void Polygon_OfARingWedge_RunsTheOuterArcThenTheInnerArcBack()
    {
        var points = new RingWedge(100, 150, 30, 60, 0, 90).Polygon();

        int steps = RingWedge.StepsFor(90);
        Assert.Equal(2 * (steps + 1), points.Count);
        Assert.Equal(160, points[0].X, 1e-9);               // outer arc, at the start angle
        Assert.Equal(150, points[0].Y, 1e-9);
        Assert.Equal(100, points[steps].X, 1e-9);           // ... to the end angle (clockwise on a screen: Y downwards)
        Assert.Equal(210, points[steps].Y, 1e-9);
        Assert.Equal(100, points[steps + 1].X, 1e-9);       // the inner arc back, from the end angle
        Assert.Equal(180, points[steps + 1].Y, 1e-9);
        Assert.Equal(130, points[^1].X, 1e-9);              // ... to the start angle
        Assert.Equal(150, points[^1].Y, 1e-9);
    }

    [Fact]
    public void Polygon_OfASolidSlice_StartsAtTheCentreAndRunsTheOuterArc()
    {
        var points = new RingWedge(100, 150, 0, 60, 0, 90).Polygon();

        Assert.Equal(1 + RingWedge.StepsFor(90) + 1, points.Count);
        Assert.Equal(new Point(100, 150), points[0]);
        Assert.Equal(160, points[1].X, 1e-9);
        Assert.Equal(150, points[1].Y, 1e-9);
    }

    [Fact]
    public void Polygon_OfASweepRunBackwards_RunsTheSameArcTheOtherWay()
    {
        var forward = new RingWedge(0, 0, 0, 10, 0, 90).Polygon();
        var backward = new RingWedge(0, 0, 0, 10, 90, 0).Polygon();

        Assert.Equal(forward.Count, backward.Count);
        Assert.Equal(forward[1].X, backward[^1].X, 1e-9);
        Assert.Equal(forward[1].Y, backward[^1].Y, 1e-9);
    }

    /// <summary>A pie, a donut, a dial and a polar axes lay an angle in radians counter-clockwise from the right with Y up; the
    /// wedge's own angles run clockwise on a screen whose Y points down, in degrees.</summary>
    [Fact]
    public void CounterClockwise_LaysAnAngleAsAPieAndAPolarAxesDo()
    {
        var wedge = RingWedge.CounterClockwise(new Point(100, 150), 0, 60, 0, Math.PI / 2);

        Assert.Equal(0, wedge.StartAngleDegrees, 1e-9);
        Assert.Equal(-90, wedge.EndAngleDegrees, 1e-9);
        var first = wedge.Polygon()[1];
        var expected = new Point(100, 150).AtAngleCounterClockwise(60, 0);
        Assert.Equal(expected.X, first.X, 1e-9);
        Assert.Equal(expected.Y, first.Y, 1e-9);
        var last = wedge.Polygon()[^1];
        var top = new Point(100, 150).AtAngleCounterClockwise(60, Math.PI / 2);
        Assert.Equal(top.X, last.X, 1e-9);
        Assert.Equal(top.Y, last.Y, 1e-9);
    }

    /// <summary>A wedge's path begins on the outer arc and walks the inner arc back (a ring), or runs to the point where the inner radius
    /// is none: a move to the first point, lines to the rest, and a close.</summary>
    [Theory]
    [InlineData(30)]
    [InlineData(0)]
    public void PolygonPath_IsMoveToTheFirstPointLinesToTheRestAndClose(double innerRadius)
    {
        var wedge = new RingWedge(100, 150, innerRadius, 60, 0, 90);
        var points = wedge.Polygon();

        var segments = wedge.PolygonPath();

        Assert.Equal(points.Count + 1, segments.Count);
        Assert.Equal(points[0], Assert.IsType<MoveToSegment>(segments[0]).Point);
        for (int i = 1; i < points.Count; i++) Assert.Equal(points[i], Assert.IsType<LineToSegment>(segments[i]).Point);
        Assert.IsType<CloseSegment>(segments[^1]);
    }

    [Fact]
    public void FromBearings_TwelveOClockIsMinusNinetyDegrees_AndQuarterTurnsFollowClockwise()
    {
        var wedge = RingWedge.FromBearings(new Point(100, 150), 30, 60, 0, Math.PI / 2);

        Assert.Equal(-90.0, wedge.StartAngleDegrees, 9);   // twelve o'clock on a screen whose Y points down
        Assert.Equal(0.0, wedge.EndAngleDegrees, 9);        // three o'clock
        Assert.Equal(100, wedge.Cx);
        Assert.Equal(150, wedge.Cy);
        Assert.Equal(30, wedge.InnerRadius);
        Assert.Equal(60, wedge.OuterRadius);
    }

    [Fact]
    public void OuterArc_IsTheOuterArcOfThePath_FromTheStartAngleToTheEndAngle()
    {
        var wedge = new RingWedge(100, 150, 30, 60, 20, 110);

        var arc = wedge.OuterArc();

        Assert.Equal(new Point(100, 150), arc.Center);
        Assert.Equal(60, arc.RadiusX);
        Assert.Equal(60, arc.RadiusY);
        Assert.Equal(20, arc.StartAngle);
        Assert.Equal(110, arc.EndAngle);
        Assert.Equal(arc, wedge.Path().OfType<ArcSegment>().First());
    }
}
