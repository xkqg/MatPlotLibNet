// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Patches;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>How far a curved patch reaches: where its curves turn back, not where their control points stand
/// (<see cref="PathSegmentExtensions.ExtremePoints"/>), which is what matplotlib's <c>add_patch</c> puts in the data limits. The
/// automatic range of an axes reads that extent (<c>AxesRangeExtensions.PatchContributions</c>), so a curve is framed where it is
/// drawn.</summary>
public class PatchExtentTests
{
    private const double Tolerance = 1e-12;

    /// <summary>The box a patch's bounding points span, in data units.</summary>
    private readonly record struct Box(double XMin, double YMin, double XMax, double YMax)
    {
        public static Box Of(Patch patch)
        {
            var points = patch.Outline().ExtremePoints().ToList();
            return new(points.Min(p => p.X), points.Min(p => p.Y), points.Max(p => p.X), points.Max(p => p.Y));
        }

        public void IsNear(Box expected)
        {
            Assert.True(
                Math.Abs(XMin - expected.XMin) <= Tolerance && Math.Abs(YMin - expected.YMin) <= Tolerance
                && Math.Abs(XMax - expected.XMax) <= Tolerance && Math.Abs(YMax - expected.YMax) <= Tolerance,
                $"expected {expected}, reached {this}");
        }
    }

    private static double ControlPointsMaxX(Patch patch) =>
        patch.Outline().OfType<BezierSegment>().SelectMany(curve => new[] { curve.Control1.X, curve.Control2.X }).Max();

    /// <summary>An arc from 310 to 40 on the unit circle turns back along X inside its second curve, at 0: its extent reaches the
    /// curve there, not the control points beyond it. matplotlib 3.11.2: <c>Arc((0, 0), 2, 2, theta1=310, theta2=40)</c> added to an
    /// axes gives the data limits x 0.6427876096865393 to 0.9999982414994397 (the curve, 1.8e-6 inside the circle), y
    /// -0.7660444431189781 to 0.6427876096865393; its control points reach x 1.0193009730887572.</summary>
    [Fact]
    public void AnArcsExtent_IsItsCurveExtremum()
    {
        var arc = new Arc(new(0, 0), 2, 2) { Theta1 = 310, Theta2 = 40 };

        Box.Of(arc).IsNear(new(0.6427876096865393, -0.7660444431189781, 0.9999982414994397, 0.6427876096865393));
        Assert.Equal(1.0193009730887572, ControlPointsMaxX(arc), Tolerance);
    }

    /// <summary>A circle reaches its radius on every side, and an ellipse half its width and half its height.</summary>
    [Fact]
    public void ACirclesAndAnEllipsesExtent_IsTheirRadiusAboutTheCentre()
    {
        Box.Of(new Circle(new(2, 3), 1.5)).IsNear(new(0.5, 1.5, 3.5, 4.5));
        Box.Of(new Ellipse(new(-1, 1), 4, 1)).IsNear(new(-3, 0.5, 1, 1.5));
    }

    /// <summary>A wedge reaches its centre as well as its arc: a quarter of the disc from 0 to 90 spans the square from its centre
    /// to its radius.</summary>
    [Fact]
    public void AWedgesExtent_TakesItsCentreIn()
    {
        Box.Of(new Wedge(new(1, 2), 2, 0, 90)).IsNear(new(1, 2, 3, 4));
    }

    /// <summary>The automatic range of an axes that holds an arc frames the arc's curve, as it would frame a line over the same extent.</summary>
    [Fact]
    public void TheRangeOfAnAxesOfAnArc_IsTheRangeOfALineOverItsCurvesExtent()
    {
        var withArc = new Axes();
        withArc.AddPatch(new Arc(new(0, 0), 2, 2) { Theta1 = 310, Theta2 = 40 });
        var withLine = new Axes();
        withLine.Plot([0.6427876096865393, 0.9999982414994397], [-0.7660444431189781, 0.6427876096865393]);

        var arc = withArc.ComputeDataRanges(Theme.MatplotlibV2);
        var line = withLine.ComputeDataRanges(Theme.MatplotlibV2);

        Assert.Equal(line.XMin, arc.XMin, Tolerance);
        Assert.Equal(line.XMax, arc.XMax, Tolerance);
        Assert.Equal(line.YMin, arc.YMin, Tolerance);
        Assert.Equal(line.YMax, arc.YMax, Tolerance);
    }
}
