// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>An outline in data units (a patch's) is drawn through the transform of its axes and ranged over its true extent: the
/// one rule for both, written over the path segments the outline is made of, so a curved outline needs no second one.
/// <list type="bullet">
/// <item>Every point of the outline, a curve's control points included, is mapped through the axes' transform: on a log axis the
/// control points move, as matplotlib maps them, and a straight edge stays straight.</item>
/// <item>A finite coordinate a log axis cannot place (0 or below) is pinned to the edge where that axis begins, matplotlib's
/// <c>nonpositive='clip'</c> (matplotlib pushes it a thousand decades out, which its axes clip at the same edge).</item>
/// <item>A point the axes cannot place at all (inside an axis break) is left out, and the outline resumes with a move at the next
/// point it can place: the piece after the gap is a subpath of its own, and an outline that was broken is not closed across the gap.</item>
/// <item>The extent of a curve is where the curve reaches, not where its control points stand.</item>
/// </list></summary>
public class PathSegmentExtensionsTests
{
    private static readonly Rect Plot = new(0, 0, 400, 300);

    /// <summary>X and Y from 0 to 10: 40 px per unit across, 30 px per unit up, 10 at the top.</summary>
    private static DataTransform Linear() => new(0, 10, 0, 10, Plot);

    /// <summary>A log Y axis from 1 (the bottom edge) to 100 (the top edge), 150 px per decade.</summary>
    private static DataTransform LogY() =>
        new(0, 10, 0, 2, Plot, null, null, 0, 10, 1, 100, AxisScale.Linear, AxisScale.Log, 1, 1);

    /// <summary>A Y axis from 0 to 100 with 40 to 60 broken out: 80 units over 300 px, 3.75 px per unit.</summary>
    private static DataTransform BrokenY() =>
        new(0, 10, 0, 80, Plot, null, [new AxisBreak(40, 60)], 0, 10, 0, 100);

    private static MoveToSegment M(double x, double y) => new(new Point(x, y));

    private static LineToSegment L(double x, double y) => new(new Point(x, y));

    private static BezierSegment C(double x1, double y1, double x2, double y2, double x, double y) =>
        new(new Point(x1, y1), new Point(x2, y2), new Point(x, y));

    private static readonly CloseSegment Z = new();

    // ---- to pixels ---------------------------------------------------------------------------------------------------

    [Fact]
    public void AStraightOutline_IsMappedVertexByVertex()
    {
        IReadOnlyList<PathSegment> outline = [M(1, 1), L(3, 1), L(3, 2), Z];

        Assert.Equal([M(40, 270), L(120, 270), L(120, 240), Z], outline.ToPixels(Linear()));
    }

    [Fact]
    public void ACurve_MapsItsControlPointsAndItsEnd()
    {
        IReadOnlyList<PathSegment> outline = [M(0, 0), C(1, 0, 2, 10, 10, 10)];

        Assert.Equal([M(0, 300), C(40, 300, 80, 0, 400, 0)], outline.ToPixels(Linear()));
    }

    [Fact]
    public void OnALogAxis_ACoordinateItCannotPlace_IsPinnedToWhereTheAxisBegins()
    {
        IReadOnlyList<PathSegment> outline = [M(1, 10), L(5, 0), L(9, -3), C(9, 0, 1, 100, 1, 10), Z];

        Assert.Equal([M(40, 150), L(200, 300), L(360, 300), C(360, 300, 40, 0, 40, 150), Z], outline.ToPixels(LogY()));
    }

    [Fact]
    public void OnAReversedLogAxis_ThePinnedCoordinate_IsAtTheTop()
    {
        var reversed = new DataTransform(0, 10, 2, 0, Plot, null, null, 0, 10, 100, 1, AxisScale.Linear, AxisScale.Log, 1, 1);
        IReadOnlyList<PathSegment> outline = [M(1, 10), L(5, 0), L(9, 10), Z];

        Assert.Equal([M(40, 150), L(200, 0), L(360, 150), Z], outline.ToPixels(reversed));
    }

    [Fact]
    public void APointInsideABreak_IsLeftOut_AndTheOutlineResumesWithAMove_Unclosed()
    {
        IReadOnlyList<PathSegment> outline = [M(1, 10), L(5, 50), L(9, 10), L(9, 30), Z];

        Assert.Equal([M(40, 262.5), M(360, 262.5), L(360, 187.5)], outline.ToPixels(BrokenY()));
    }

    [Fact]
    public void AStartInsideABreak_LetsTheOutlineBeginAtItsFirstPointThatCanBePlaced()
    {
        IReadOnlyList<PathSegment> outline = [M(1, 50), L(5, 10), L(9, 10), Z];

        Assert.Equal([M(200, 262.5), L(360, 262.5)], outline.ToPixels(BrokenY()));
    }

    /// <summary>Each subpath starts afresh: a break in the first does not keep the second from closing.</summary>
    [Fact]
    public void ABreakInOneSubpath_LeavesTheNextOneClosed()
    {
        IReadOnlyList<PathSegment> outline = [M(1, 10), L(2, 50), L(3, 10), L(3, 20), Z, M(5, 10), L(7, 10), L(7, 30), Z];

        Assert.Equal(
            [M(40, 262.5), M(120, 262.5), L(120, 225), M(200, 262.5), L(280, 262.5), L(280, 187.5), Z],
            outline.ToPixels(BrokenY()));
    }

    [Fact]
    public void ACurveWithAPointInsideABreak_IsLeftOut_AndACurveAfterTheGap_StartsWithAMoveToItsEnd()
    {
        IReadOnlyList<PathSegment> outline = [M(0, 10), C(1, 50, 2, 10, 3, 10), C(4, 10, 5, 20, 6, 20), L(8, 20)];

        Assert.Equal([M(0, 262.5), M(240, 225), L(320, 225)], outline.ToPixels(BrokenY()));
    }

    /// <summary>An outline with nothing left to draw (no line and no curve between two points that can be placed) is empty.</summary>
    [Fact]
    public void AnOutlineWithNothingLeftToDraw_IsEmpty()
    {
        IReadOnlyList<PathSegment> noOutline = [];
        IReadOnlyList<PathSegment> allInTheBreak = [M(1, 45), L(5, 50), L(9, 55), Z];
        IReadOnlyList<PathSegment> onePointLeft = [M(1, 10), L(5, 50), Z];
        IReadOnlyList<PathSegment> aLoneClose = [Z];

        Assert.Empty(noOutline.ToPixels(Linear()));
        Assert.Empty(allInTheBreak.ToPixels(BrokenY()));
        Assert.Empty(onePointLeft.ToPixels(BrokenY()));
        Assert.Empty(aLoneClose.ToPixels(Linear()));
    }

    /// <summary>An arc segment is in pixels by its definition (0° is +X, clockwise on a screen whose Y points down), so an outline in
    /// data units holds none; one that does is not placed, as a point that cannot be placed is not.</summary>
    [Fact]
    public void AnArcSegment_IsNotPlaced()
    {
        IReadOnlyList<PathSegment> outline = [M(1, 1), new ArcSegment(new Point(2, 2), 1, 1, 0, 90), L(3, 1), L(3, 2)];

        Assert.Equal([M(40, 270), M(120, 270), L(120, 240)], outline.ToPixels(Linear()));
    }

    // ---- the extent ----------------------------------------------------------------------------------------------------

    [Fact]
    public void AStraightOutline_ReachesItsVertices()
    {
        IReadOnlyList<PathSegment> outline = [M(1, 2), L(3, 2), L(3, 5), Z];

        Assert.Equal([new Point(1, 2), new Point(3, 2), new Point(3, 5)], outline.ExtremePoints());
    }

    /// <summary>A bump from (0, 0) to (1, 0) with its control points at height 1 rises to 0.75 at its middle: the extent is
    /// the curve's, not its control points'.</summary>
    [Fact]
    public void ACurve_ReachesItsTurningPoint_NotItsControlPoints()
    {
        IReadOnlyList<PathSegment> outline = [M(0, 0), C(0, 1, 1, 1, 1, 0)];

        var points = outline.ExtremePoints().ToList();

        Assert.Equal([new Point(0, 0), new Point(0.5, 0.75), new Point(1, 0)], points);
    }

    /// <summary>A curve that never turns back between its ends reaches no further than them, though its control points stand
    /// beyond the curve (a turning point at an end, where a quarter circle meets an axis, may be found a rounding error inside it).</summary>
    [Fact]
    public void AQuarterOfACircle_ReachesNoFurtherThanItsEnds()
    {
        const double k = 0.5522847498;
        IReadOnlyList<PathSegment> outline = [M(1, 0), C(1, k, k, 1, 0, 1)];

        var points = outline.ExtremePoints().ToList();

        Assert.Equal(new Point(1, 0), points[0]);
        Assert.Equal(new Point(0, 1), points[^1]);
        Assert.All(points, p => Assert.True(p.X is >= -1e-12 and <= 1 + 1e-12 && p.Y is >= -1e-12 and <= 1 + 1e-12, $"{p} lies beyond the ends"));
    }

    /// <summary>How many points inside a curve its X turns back at, for the X of its four points (Y stays 0): every arm of the
    /// rule that finds where the derivative is 0, a turning point at either end left out.</summary>
    [Theory]
    [InlineData(0, 1, 2, 3, 0)]       // evenly spaced: the derivative never changes, it is a straight line
    [InlineData(0, 1, 1.4, 2.4, 0)]   // the derivative is a parabola that never reaches 0
    [InlineData(0, 0, 0, 1, 0)]       // the derivative is 0 at the start only
    [InlineData(0, 0, 1, 1, 0)]       // the derivative is 0 at both ends only
    [InlineData(0, 1, 1, 0, 1)]       // turns once, half way
    [InlineData(0, 1, 1.5, 1.5, 0)]   // the derivative falls in a straight line to 0 at the end
    [InlineData(0, 2, -1, 1, 2)]      // turns twice
    [InlineData(0, 1, -1, -1, 1)]     // turns at 0.2 and stops at the end: the smaller root is inside
    [InlineData(0, -0.05, -0.3, 0.25, 1)]  // turns at 0.5; the other root lies before the start
    [InlineData(0, 0, 0, 0, 0)]       // a point
    public void ACurveTurnsBack_WhereItsDerivativeIsZero(double x0, double x1, double x2, double x3, int turns)
    {
        IReadOnlyList<PathSegment> outline = [M(x0, 0), C(x1, 0, x2, 0, x3, 0)];

        Assert.Equal(2 + turns, outline.ExtremePoints().Count());
    }

    [Fact]
    public void TheTurningPoints_AreOnTheCurve()
    {
        IReadOnlyList<PathSegment> outline = [M(0, 0), C(2, 0, -1, 0, 1, 0)];

        double[] xs = [.. outline.ExtremePoints().Select(p => p.X)];

        // x(t) = 6t - 15t² + 10t³ turns where 6 - 30t + 30t² = 0, at t = (5 ∓ √5) / 10.
        Assert.Equal(4, xs.Length);
        Assert.Equal(XAt((5 - Math.Sqrt(5)) / 10), xs.Skip(1).Take(2).Max(), 12);
        Assert.Equal(XAt((5 + Math.Sqrt(5)) / 10), xs.Skip(1).Take(2).Min(), 12);

        static double XAt(double t) => 6 * t - 15 * t * t + 10 * t * t * t;
    }

    /// <summary>A curve after a close starts where the closed subpath started, as a path's current point does.</summary>
    [Fact]
    public void ACurveAfterAClose_StartsWhereTheSubpathStarted()
    {
        IReadOnlyList<PathSegment> outline = [M(0, 0), L(4, 0), Z, C(0, 2, 0, 2, 0, 0)];

        Assert.Contains(new Point(0, 1.5), outline.ExtremePoints());
    }

    [Fact]
    public void AnArcSegment_AddsNothingToTheExtent()
    {
        IReadOnlyList<PathSegment> outline = [M(1, 1), new ArcSegment(new Point(50, 50), 40, 40, 0, 180), L(2, 2)];

        Assert.Equal([new Point(1, 1), new Point(2, 2)], outline.ExtremePoints());
    }
}
