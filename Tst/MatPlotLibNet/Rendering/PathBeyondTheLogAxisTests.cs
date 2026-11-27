// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.SeriesRenderers;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Where a path (a line, the outline of a fill) goes through a value a log axis cannot place. matplotlib 3.11.2's log
/// scale clips such a value (<c>LogTransform</c>, <c>nonpositive='clip'</c>: <c>out[values &lt;= 0] = -1000</c>), so the vertex
/// stands a thousand decades below the axis and the axes clip cuts every segment to it where it leaves the plot. Printed with
/// python: <c>ax.plot([1, 2, 3], [1, 0, 10]); ax.set_yscale('log')</c> gives the line pixels
/// <c>[[96.14, 58.0], [307.5, -279942.0], [518.86, 338.0]]</c> on an axes from y 44 to 352 (display pixels, upward): the line
/// dives from (96.14, 58) to the bottom edge and climbs from it to (518.86, 338). A line breaks there; a filled outline runs
/// along the edge between the two cuts.</summary>
public sealed class PathBeyondTheLogAxisTests
{
    private static readonly Rect Bounds = new(0, 0, 400, 300);

    private const double BeyondY = 300 + 1000 * 150.0;

    /// <summary>Where the segment from <paramref name="a"/> to <paramref name="b"/> crosses the row <paramref name="edge"/>, as
    /// the axes clip cuts it.</summary>
    private static Point CrossY(Point a, Point b, double edge) => new(a.X + (edge - a.Y) / (b.Y - a.Y) * (b.X - a.X), edge);

    private static Point CrossX(Point a, Point b, double edge) => new(edge, a.Y + (edge - a.X) / (b.X - a.X) * (b.Y - a.Y));

    private static List<IReadOnlyList<Point>> Runs(IReadOnlyList<Point> path) => [.. path.LineRuns()];

    private static void AssertPoint(Point expected, Point actual)
    {
        Assert.Equal(expected.X, actual.X, 9);
        Assert.Equal(expected.Y, actual.Y, 9);
    }

    // ---- the place --------------------------------------------------------------------------------------------------

    [Fact]
    public void ALogDirection_PutsAValueItCannotPlace_AThousandDecadesBelowWhereItBegins()
    {
        var log = new PixelDirection(100, 0, 50, null, ScaleRule.Log, new Range1D(0, 4));

        Assert.Equal(50 - 1000 * 100, log.Beyond);
        Assert.Equal(log.Beyond, log.PlaceUnclipped(0));
        Assert.Equal(log.Beyond, log.PlaceUnclipped(-3));
        Assert.Equal(150, log.PlaceUnclipped(10));
        Assert.True(double.IsNaN(log.PlaceUnclipped(double.NaN)));
    }

    /// <summary>Only a log scale clips: a linear and a symmetric-log axis place every finite value, and a logit axis masks what
    /// it cannot place (<c>LogitTransform(nonpositive='mask')</c>). A direction with no width has no "below" either.</summary>
    [Fact]
    public void OnlyALogDirectionWithAWidth_HasAPlaceBeyond()
    {
        Assert.True(double.IsNaN(new PixelDirection(100, 0, 50, null, ScaleRule.Linear, new Range1D(0, 4)).Beyond));
        Assert.True(double.IsNaN(new PixelDirection(100, 0, 50, null, ScaleRule.SymLog, new Range1D(0, 4)).Beyond));
        Assert.True(double.IsNaN(new PixelDirection(100, 0, 50, null, ScaleRule.Logit, new Range1D(0, 4)).Beyond));
        Assert.True(double.IsNaN(new PixelDirection(0, 1, 250, null, ScaleRule.Log, new Range1D(1, 1)).Beyond));
        Assert.True(double.IsNaN(new PixelDirection(0, 1, 250, null, ScaleRule.Log, new Range1D(1, 1)).PlaceUnclipped(-1)));
    }

    [Fact]
    public void AWholeArray_IsPlacedAsEachOfItsValues_AValueTheLogAxisCannotPlaceBeyondIt()
    {
        var transform = Bounds.LogY();
        double[] values = [10, 0, -2, double.NaN, 100];

        double[] pixels = transform.TransformUnclippedAlong(false, values);

        Assert.Equal([150, BeyondY, BeyondY, double.NaN, 0], pixels);
        Assert.Equal(transform.TransformX(values), transform.TransformUnclippedAlong(true, values));
    }

    // ---- a line -----------------------------------------------------------------------------------------------------

    [Fact]
    public void ALineThroughAValueTheLogAxisCannotPlace_DivesToTheBottomEdge_AndClimbsFromIt()
    {
        var path = Bounds.LogY().TransformPath([1.0, 2, 3], [10.0, 0, 10]);

        Point a = new(40, 150), beyond = new(80, BeyondY), b = new(120, 150);
        var runs = Runs(path);
        Assert.Equal(2, runs.Count);
        Assert.Equal(2, runs[0].Count);
        AssertPoint(a, runs[0][0]);
        AssertPoint(CrossY(a, beyond, 300), runs[0][1]);
        Assert.Equal(2, runs[1].Count);
        AssertPoint(CrossY(beyond, b, 300), runs[1][0]);
        AssertPoint(b, runs[1][1]);
        Assert.All(path.Where(p => p.IsDrawable()), p => Assert.True(p.Y <= 300, $"{p} lies below the plot"));
    }

    /// <summary>The segments between two values the axis cannot place lie wholly below the plot: nothing of them is drawn.</summary>
    [Fact]
    public void ARunOfValuesTheLogAxisCannotPlace_IsCutOutWhole()
    {
        var runs = Runs(Bounds.LogY().TransformPath([1.0, 2, 3, 4], [10.0, 0, -1, 10]));

        Assert.Equal(2, runs.Count);
        Assert.Equal(300, runs[0][^1].Y, 9);
        Assert.Equal(300, runs[1][0].Y, 9);
        Assert.Empty(Runs(Bounds.LogY().TransformPath([1.0, 2], [0.0, -1])));
    }

    /// <summary>A missing value and an infinity still break the line: matplotlib's path drops a vertex that is not finite.</summary>
    [Fact]
    public void AMissingOrInfiniteValue_StillBreaksTheLine()
    {
        var runs = Runs(Bounds.LogY().TransformPath([1.0, 2, 3, 4, 5], [10.0, double.NaN, 10, double.PositiveInfinity, 10]));

        Assert.Empty(runs);
    }

    /// <summary>A line with no value beyond the axis is the placement itself, bit for bit, so its drawing does not move.</summary>
    [Fact]
    public void ALineWithNothingBeyondTheAxis_IsThePlacementItself()
    {
        double[] x = [1.0, 2, 3], y = [10.0, 20, 30];

        var path = Bounds.LogY().TransformPath(x, y);

        Assert.Equal(Bounds.LogY().TransformBatch(x, y), path);
        var linear = new DataTransform(0, 10, 0, 10, Bounds);
        Assert.Equal(linear.TransformBatch(x, [-1.0, 0, 1]), linear.TransformPath(x, [-1.0, 0, 1]));
    }

    [Fact]
    public void OnAReversedLogAxis_TheLineIsCutAtTheTopEdge_WhereTheAxisBegins()
    {
        var transform = Bounds.LogYReversed();
        var runs = Runs(transform.TransformPath([1.0, 2], [10.0, 0]));

        var run = Assert.Single(runs);
        Assert.Equal(0, run[^1].Y, 9);
        Assert.Equal(transform.DataToPixel(1, 10), run[0]);
    }

    [Fact]
    public void OnALogXAxis_TheLineIsCutAtTheLeftEdge()
    {
        var transform = Bounds.LogX();
        var a = transform.DataToPixel(10, 1);
        var beyond = new Point(0 - 1000 * 200.0, transform.DataToPixel(10, 2).Y);

        var run = Assert.Single(Runs(transform.TransformPath([10.0, 0], [1.0, 2])));

        AssertPoint(a, run[0]);
        AssertPoint(CrossX(a, beyond, 0), run[1]);
    }

    /// <summary><c>ax.plot([1, 0, 10], [1, 0, 10])</c> on log X and log Y: the vertex (0, 0) lies a thousand decades beyond both
    /// axes (printed: <c>[-422631.14, -279942.0]</c>); the line is cut where it first leaves the plot.</summary>
    [Fact]
    public void OnLogAxesBothWays_TheLineIsCutWhereItFirstLeavesThePlot()
    {
        var transform = Bounds.LogLog();
        var a = transform.DataToPixel(2, 10);
        var beyond = new Point(-1000 * 200.0, BeyondY);

        var run = Runs(transform.TransformPath([2.0, 0, 10], [10.0, 0, 10]))[0];

        // From (60.2, 150) the segment reaches the left edge (t = 60.2 / 200 060) before the bottom edge (t = 150 / 150 150).
        AssertPoint(CrossX(a, beyond, 0), run[^1]);
        Assert.InRange(run[^1].Y, 0, 300);
    }

    // ---- a filled outline ---------------------------------------------------------------------------------------------

    /// <summary><c>ax.fill([1, 2, 3], [10, 0, 10])</c> on a log Y axis: the polygon dives to the bottom edge, runs along it and
    /// climbs back; the part below the plot is cut off, so the outline holds the two cuts joined along the edge.</summary>
    [Fact]
    public void AFilledOutlineThroughAValueTheLogAxisCannotPlace_RunsAlongTheBottomEdgeBetweenTheCuts()
    {
        var transform = Bounds.LogY();
        Point a = new(40, 150), beyond = new(80, BeyondY), b = new(120, 150);

        var polygon = transform.CutBeyondTheStart([a, beyond, b], closed: true);

        Assert.Equal(4, polygon.Count);
        Assert.All(polygon, p => Assert.True(p.IsDrawable()));
        AssertPoint(a, polygon[0]);
        AssertPoint(CrossY(a, beyond, 300), polygon[1]);
        AssertPoint(CrossY(beyond, b, 300), polygon[2]);
        AssertPoint(b, polygon[3]);
    }

    /// <summary>A base pinned on the edge lies on it, inside the plot: a filled outline that stands on such a base keeps it.</summary>
    [Fact]
    public void AVertexOnTheEdge_IsKept()
    {
        var transform = Bounds.LogY();
        Point[] polygon = [new(40, 150), new(80, BeyondY), new(120, 150), new(120, 300), new(40, 300)];

        var cut = transform.CutBeyondTheStart(polygon, closed: true);

        Assert.Contains(new Point(120, 300), cut);
        Assert.Contains(new Point(40, 300), cut);
        Assert.All(cut, p => Assert.True(p.Y <= 300));
    }

    /// <summary>A vertex standing exactly on the edge, next to one beyond it, is where the path is cut: it is kept once, not
    /// doubled by a cut at the same place.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AVertexOnTheEdgeNextToOneBeyond_IsNotDoubled(bool closed)
    {
        var transform = Bounds.LogY();
        Point[] path = [new(40, 150), new(80, 300), new(120, BeyondY), new(160, 300), new(200, 150)];

        var cut = transform.CutBeyondTheStart(path, closed);

        Assert.Equal(closed ? 4 : 5, cut.Count);
        Assert.Equal(2, cut.Count(p => p.Y == 300));
    }

    /// <summary>A segment that runs straight across the axis to a value it cannot place (a bar's side, an error bar, a stem) is
    /// cut right on the edge under its end: the same pixel as the base pinned there. Both ends beyond: nothing is left.</summary>
    [Fact]
    public void ASegmentAcrossTheAxis_IsCutOnTheEdgeUnderItsEnd_OrDroppedWhenBothEndsLieBeyond()
    {
        var transform = Bounds.LogY();

        Assert.Equal(new PixelSegment(new Point(80, 300), new Point(80, 150)), transform.CutSegment(new(80, BeyondY), new(80, 150)));
        Assert.Equal(new PixelSegment(new Point(80, 150), new Point(80, 300)), transform.CutSegment(new(80, 150), new(80, BeyondY)));
        Assert.Null(transform.CutSegment(new(40, BeyondY), new(80, BeyondY)));
        Assert.Equal(new PixelSegment(new Point(40, 150), new Point(80, 140)), transform.CutSegment(new(40, 150), new(80, 140)));
        Assert.Null(transform.CutSegment(new(40, double.NaN), new(80, 140)));
    }

    /// <summary>The axes two marks reach beyond are the axes either reaches beyond.</summary>
    [Fact]
    public void TheAxesTwoMarksReachBeyond_AreTheAxesEitherReaches()
    {
        var x = new AxisPair<bool>(true, false);
        var y = new AxisPair<bool>(false, true);

        Assert.Equal(new AxisPair<bool>(true, true), x | y);
        Assert.Equal(new AxisPair<bool>(true, true), y | x);
        Assert.Equal(x, x | default(AxisPair<bool>));
        Assert.True(x.Any);
        Assert.True(y.Any);
        Assert.False(default(AxisPair<bool>).Any);
    }

    /// <summary>The axes both ends of one mark reach beyond: an axis only one end lies beyond is not one the mark lies wholly beyond.</summary>
    [Fact]
    public void TheAxesBothEndsReachBeyond_AreTheAxesEachReaches()
    {
        var x = new AxisPair<bool>(true, false);
        var both = new AxisPair<bool>(true, true);

        Assert.Equal(x, x & both);
        Assert.Equal(x, both & x);
        Assert.False((x & new AxisPair<bool>(false, true)).Any);
        Assert.Equal(both, both & both);
    }

    [Fact]
    public void APathWithNothingBeyondTheAxis_IsHandedBackAsGiven()
    {
        Point[] path = [new(40, 150), new(80, 400), new(120, 150)];

        Assert.Same(path, Bounds.LogY().CutBeyondTheStart(path, closed: false));
        Assert.Same(path, new DataTransform(0, 10, 0, 10, Bounds).CutBeyondTheStart(path, closed: true));
    }

    [Fact]
    public void APointBeyondTheAxis_IsKnownByItsPlace()
    {
        var transform = Bounds.LogY();

        Assert.True(transform.LiesBeyond(new Point(80, BeyondY)));
        Assert.False(transform.LiesBeyond(new Point(80, 300)));
        Assert.False(transform.LiesBeyond(new Point(double.NaN, double.NaN)));
        Assert.False(new DataTransform(0, 10, 0, 10, Bounds).LiesBeyond(new Point(80, BeyondY)));
    }
}
