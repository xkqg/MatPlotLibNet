// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>How far one pixel is from another, which way it lies and which way is square to that: worked out once, in the same
/// arithmetic every renderer wrote by hand, so a picture that drew from the hand-written sums does not move by a digit.</summary>
public class PointExtensionsTests
{
    [Fact]
    public void TheDistance_IsTheLengthOfTheLineBetweenThePoints()
    {
        Assert.Equal(5.0, new Point(1, 2).DistanceTo(new Point(4, 6)));
        Assert.Equal(0.0, new Point(3, 3).DistanceTo(new Point(3, 3)));
    }

    [Fact]
    public void TheDistance_IsTheSameBothWays()
    {
        var a = new Point(12.375, -4.0625);
        var b = new Point(-3.5, 9.125);

        Assert.Equal(a.DistanceTo(b), b.DistanceTo(a));
    }

    /// <summary>The distance is the square root of the sum of the two squares, with no other rounding: a drawing that compared or
    /// scaled by the hand-written sum stays as it was.</summary>
    [Theory]
    [InlineData(0.1, 0.2, 0.7, 1.9)]
    [InlineData(123.456789, 98.7654321, -45.6789, 3.14159265)]
    [InlineData(1e-9, 2e-9, 3e-9, 5e-9)]
    [InlineData(1e8, 1e8, -1e8, 3e8)]
    public void TheDistance_IsTheSquareRootOfTheSumOfTheSquares(double x1, double y1, double x2, double y2)
    {
        double dx = x2 - x1, dy = y2 - y1;

        Assert.Equal(Math.Sqrt(dx * dx + dy * dy), new Point(x1, y1).DistanceTo(new Point(x2, y2)));
        Assert.Equal(dx * dx + dy * dy, new Point(x1, y1).DistanceSquaredTo(new Point(x2, y2)));
    }

    /// <summary>A point read as a step from the origin (a quiver's vector, a force, an arrow's own step) has the one length every
    /// distance is: the square root of the sum of the two squares, digit for digit as the sums the sites wrote by hand.</summary>
    [Theory]
    [InlineData(3.0, 4.0)]
    [InlineData(0.1, -0.7)]
    [InlineData(-123.456789, 98.7654321)]
    [InlineData(1e-9, 2e-9)]
    public void TheLengthOfAStep_IsTheSquareRootOfTheSumOfTheSquares(double u, double v)
    {
        var step = new Point(u, v);

        Assert.Equal(u * u + v * v, step.SquaredLength);
        Assert.Equal(Math.Sqrt(u * u + v * v), step.Length);
    }

    [Fact]
    public void TheStepFromOnePointToAnother_IsTheirDifference_AndItsLengthIsTheirDistance()
    {
        var from = new Point(12.375, -4.0625);
        var to = new Point(-3.5, 9.125);

        var step = from.StepTo(to);

        Assert.Equal(new Point(to.X - from.X, to.Y - from.Y), step);
        Assert.Equal(from.DistanceTo(to), step.Length);
        Assert.Equal(from.DistanceSquaredTo(to), step.SquaredLength);
    }

    [Fact]
    public void TheSquaredDistance_NeedsNoRoot()
    {
        Assert.Equal(25.0, new Point(0, 0).DistanceSquaredTo(new Point(3, 4)));
    }

    [Fact]
    public void ADistanceToAPlaceThatIsNotANumber_IsNotANumber()
    {
        Assert.True(double.IsNaN(new Point(0, 0).DistanceTo(new Point(double.NaN, 1))));
    }

    [Fact]
    public void TheHeading_IsTheStepAlongBothAxesAndTheLength()
    {
        var heading = new Point(1, 2).HeadingTo(new Point(4, 6));

        Assert.Equal(3.0, heading.Dx);
        Assert.Equal(4.0, heading.Dy);
        Assert.Equal(5.0, heading.Length);
    }

    [Fact]
    public void TheDirection_IsTheStepOverTheLength()
    {
        var heading = new Point(1, 2).HeadingTo(new Point(4, 6));

        Assert.Equal(3.0 / 5.0, heading.Ux);
        Assert.Equal(4.0 / 5.0, heading.Uy);
        Assert.Equal(new Point(3.0 / 5.0, 4.0 / 5.0), heading.Unit);
    }

    /// <summary>Two points that are one place have no direction: it is nothing, not a division by zero that is not a number.</summary>
    [Fact]
    public void ALineOfNoLength_HasNoDirection()
    {
        var heading = new Point(7, 7).HeadingTo(new Point(7, 7));

        Assert.Equal(0.0, heading.Length);
        Assert.Equal(0.0, heading.Ux);
        Assert.Equal(0.0, heading.Uy);
    }

    /// <summary>A step that is not a number stays not a number, so a line to a missing place is not drawn as though it ran nowhere.</summary>
    [Fact]
    public void ADirectionToAPlaceThatIsNotANumber_IsNotANumber()
    {
        var heading = new Point(0, 0).HeadingTo(new Point(double.NaN, 2));

        Assert.True(double.IsNaN(heading.Ux));
    }

    /// <summary>The unit step is the hand-written step over the hand-written length, digit for digit.</summary>
    [Fact]
    public void TheDirection_IsTheHandWrittenQuotient()
    {
        var from = new Point(0.3, 0.7);
        var to = new Point(11.1, -2.9);
        double dx = to.X - from.X, dy = to.Y - from.Y, len = Math.Sqrt(dx * dx + dy * dy);

        var heading = from.HeadingTo(to);

        Assert.Equal(dx / len, heading.Ux);
        Assert.Equal(dy / len, heading.Uy);
    }

    /// <summary>A line to the right has the square to it pointing up the Y axis on the left and down on the right.</summary>
    [Fact]
    public void TheNormals_AreAQuarterTurnEitherWay()
    {
        var heading = new Point(0, 0).HeadingTo(new Point(2, 0));

        Assert.Equal(new Point(0, 1), heading.LeftNormal);
        Assert.Equal(new Point(0, -1), heading.RightNormal);
    }

    [Fact]
    public void TheNormals_AreUnitAndSquareToTheLine()
    {
        var heading = new Point(1, 1).HeadingTo(new Point(4, 5));

        var left = heading.LeftNormal;
        var right = heading.RightNormal;

        Assert.Equal(1.0, Math.Sqrt(left.X * left.X + left.Y * left.Y), 12);
        Assert.Equal(0.0, left.X * heading.Ux + left.Y * heading.Uy, 12);
        Assert.Equal(-left.X, right.X);
        Assert.Equal(-left.Y, right.Y);
    }

    /// <summary>A point on the rim of a circle or an ellipse round a centre, worked out once for each way an angle is laid: as the
    /// screen lays it (clockwise, the way an SVG arc and a wedge's path turn), as a polar axes, a pie and a gauge lay it
    /// (counter-clockwise, Y upward), and as a bearing (clockwise from the top, a chord diagram's). Each is the hand-written sum, bit for
    /// bit: the centre plus the radius times the cosine or the sine, in that order.</summary>
    [Theory]
    [InlineData(10.0, 20.0, 7.5, 0.3)]
    [InlineData(-3.25, 400.0, 120.0, 2.9)]
    [InlineData(0.0, 0.0, 1e-3, -1.1)]
    public void APointOnTheRim_IsTheCentrePlusTheRadiusAlongTheAngle_ForEachWayAnAngleIsLaid(double cx, double cy, double r, double a)
    {
        var centre = new Point(cx, cy);

        Assert.Equal(new Point(cx + r * Math.Cos(a), cy + r * Math.Sin(a)), centre.AtAngle(r, a));
        Assert.Equal(new Point(cx + r * Math.Cos(a), cy + 2 * r * Math.Sin(a)), centre.OnRim(r, 2 * r, a));
        Assert.Equal(new Point(cx + r * Math.Cos(a), cy - r * Math.Sin(a)), centre.AtAngleCounterClockwise(r, a));
        Assert.Equal(new Point(cx + r * Math.Sin(a), cy - r * Math.Cos(a)), centre.AtBearing(r, a));
        Assert.Equal(new Point(cx + r * Math.Sin(a), cy + r * Math.Cos(a)), centre.BelowTurnedBy(r, a));
    }
}