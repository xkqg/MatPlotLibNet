// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>An arc is a path segment the SVG backend writes as an <c>A</c> command and a canvas without arcs (MAUI's path
/// builder takes lines and cubic curves) has to follow with curves. The conversion is one rule for every backend: at most a
/// quarter turn to a curve, whose control points are the tangents at its ends scaled by 4/3·tan(quarter of its sweep). It is
/// close enough to a circle that the error is a fraction of a thousandth of the radius.</summary>
public class ArcSegmentBezierTests
{
    private const double K = 0.5522847498307936;   // 4/3 * tan(pi/8)

    private static ArcSegment Arc(double start, double end, double rx = 10, double ry = 10, double cx = 0, double cy = 0) =>
        new(new Point(cx, cy), rx, ry, start, end);

    [Fact]
    public void AQuarterTurn_IsOneCurve_WithTheFamiliarControlPoints()
    {
        var curve = Assert.Single(Arc(0, 90).ToBeziers());

        Assert.Equal(10, curve.Control1.X, 9);
        Assert.Equal(10 * K, curve.Control1.Y, 9);
        Assert.Equal(10 * K, curve.Control2.X, 9);
        Assert.Equal(10, curve.Control2.Y, 9);
        Assert.Equal(0, curve.End.X, 9);
        Assert.Equal(10, curve.End.Y, 9);
    }

    [Fact]
    public void ASweepAcrossTheRim_IsCutIntoQuarterTurns()
    {
        Assert.Equal(2, Arc(0, 180).ToBeziers().Count);
        Assert.Equal(4, Arc(0, 360).ToBeziers().Count);
        Assert.Equal(8, Arc(0, 720).ToBeziers().Count);
        Assert.Equal(2, Arc(0, 100).ToBeziers().Count);   // more than a quarter: two curves of fifty degrees
    }

    [Fact]
    public void TheLastCurve_EndsOnTheRimAtTheEndAngle()
    {
        var curves = Arc(30, 250, rx: 12, ry: 8, cx: 5, cy: -3).ToBeziers();
        double end = 250 * Math.PI / 180;

        Assert.Equal(5 + 12 * Math.Cos(end), curves[^1].End.X, 9);
        Assert.Equal(-3 + 8 * Math.Sin(end), curves[^1].End.Y, 9);
    }

    [Fact]
    public void EveryCurve_StartsWhereTheOneBeforeEnded_AndStaysOnTheEllipse()
    {
        var curves = Arc(15, 300, rx: 12, ry: 8, cx: 5, cy: -3).ToBeziers();

        foreach (var curve in curves)
        {
            double u = (curve.End.X - 5) / 12, v = (curve.End.Y + 3) / 8;
            Assert.Equal(1.0, u * u + v * v, 9);   // the end point of a curve is on the ellipse
        }
    }

    [Fact]
    public void AnArcTheOtherWay_FollowsTheRimBackwards()
    {
        var curve = Assert.Single(Arc(90, 0).ToBeziers());

        Assert.Equal(10, curve.End.X, 9);
        Assert.Equal(0, curve.End.Y, 9);
        Assert.Equal(10 * K, curve.Control1.X, 9);    // leaves (0, 10) towards +X
        Assert.Equal(10, curve.Control1.Y, 9);
        Assert.Equal(10, curve.Control2.X, 9);
        Assert.Equal(10 * K, curve.Control2.Y, 9);
    }

    [Fact]
    public void NoSweepAndNoNumber_HaveNoCurves()
    {
        Assert.Empty(Arc(40, 40).ToBeziers());
        Assert.Empty(Arc(0, double.NaN).ToBeziers());
        Assert.Empty(Arc(0, double.PositiveInfinity).ToBeziers());
    }
}
