// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.Layout;

namespace MatPlotLibNet.Tests.Rendering.Layout;

/// <summary>
/// Pins the order-preserving one-dimensional collision sweep that Sankey node packing and
/// direct end labels share: sort by centre, a forward pass that keeps <c>padding</c> between
/// neighbours, and a backward pass that pulls the stack back inside the upper bound.
/// </summary>
public class Interval1DSpreadTests
{
    private const double Eps = 1e-9;

    private static List<Interval1D> Spread(double padding, double lower, double upper, params Interval1D[] items)
    {
        var list = new List<Interval1D>(items);
        list.SpreadApart(padding, lower, upper);
        return list;
    }

    [Fact]
    public void Interval1D_ExposesCentreAndEnd()
    {
        var i = new Interval1D(10, 6);
        Assert.Equal(13, i.Centre, Eps);
        Assert.Equal(16, i.End, Eps);
    }

    [Fact]
    public void Interval1D_FromCentre_PlacesStartHalfAnExtentBelowTheCentre()
    {
        var i = Interval1D.FromCentre(50, 14);
        Assert.Equal(43, i.Start, Eps);
        Assert.Equal(14, i.Size, Eps);
        Assert.Equal(50, i.Centre, Eps);
    }

    [Fact]
    public void SpreadApart_EmptyList_StaysEmpty()
    {
        var list = new List<Interval1D>();
        list.SpreadApart(2, 0, 100);
        Assert.Empty(list);
    }

    [Fact]
    public void SpreadApart_SingleIntervalInsideBounds_IsUnchanged()
    {
        var r = Spread(2, 0, 100, new Interval1D(40, 10));
        Assert.Equal(new Interval1D(40, 10), r[0]);
    }

    [Fact]
    public void SpreadApart_SingleIntervalBelowLower_IsPulledToLower()
    {
        var r = Spread(2, 0, 100, new Interval1D(-30, 10));
        Assert.Equal(0, r[0].Start, Eps);
    }

    [Fact]
    public void SpreadApart_SingleIntervalPastUpper_IsPulledBackSoItsEndMeetsUpper()
    {
        var r = Spread(2, 0, 100, new Interval1D(120, 10));
        Assert.Equal(90, r[0].Start, Eps);
        Assert.Equal(100, r[0].End, Eps);
    }

    [Fact]
    public void SpreadApart_NoOverlap_IsUnchanged()
    {
        var a = new Interval1D(0, 10);
        var b = new Interval1D(20, 10);
        var c = new Interval1D(40, 10);
        var r = Spread(2, 0, 100, a, b, c);
        Assert.Equal([a, b, c], r);
    }

    [Fact]
    public void SpreadApart_TwoOverlapping_AreSeparatedByExactlyThePadding()
    {
        var r = Spread(2, 0, 100, new Interval1D(10, 10), new Interval1D(15, 10));
        Assert.Equal(10, r[0].Start, Eps);
        Assert.Equal(22, r[1].Start, Eps);
        Assert.Equal(2, r[1].Start - r[0].End, Eps);
    }

    [Fact]
    public void SpreadApart_PaddingZero_ButtsTheNeighboursEdgeToEdge()
    {
        var r = Spread(0, 0, 100, new Interval1D(0, 10), new Interval1D(5, 10));
        Assert.Equal(0, r[0].Start, Eps);
        Assert.Equal(10, r[1].Start, Eps);
    }

    [Fact]
    public void SpreadApart_KeepsTheListPositionsOfAnUnsortedInput_AndTheOrderByCentre()
    {
        // input order is [lower-centre-second]; the interval with the smaller centre stays first in the sweep
        var r = Spread(0, 0, 100, new Interval1D(5, 10), new Interval1D(0, 10));
        Assert.Equal(10, r[0].Start, Eps);
        Assert.Equal(0, r[1].Start, Eps);
    }

    [Fact]
    public void SpreadApart_EqualCentres_KeepTheirInputOrder()
    {
        var r = Spread(0, 0, 100, new Interval1D(20, 10), new Interval1D(20, 10));
        Assert.Equal(20, r[0].Start, Eps);
        Assert.Equal(30, r[1].Start, Eps);
    }

    [Fact]
    public void SpreadApart_BackwardPass_PullsAStackBackInsideTheUpperBoundWithoutCascade()
    {
        // forward: 0, 12, 35 -> end 45 > 40, overshoot 5; C moves to 30, B already clear of it
        var r = Spread(2, 0, 40, new Interval1D(0, 10), new Interval1D(12, 10), new Interval1D(35, 10));
        Assert.Equal(0, r[0].Start, Eps);
        Assert.Equal(12, r[1].Start, Eps);
        Assert.Equal(30, r[2].Start, Eps);
    }

    [Fact]
    public void SpreadApart_BackwardPass_CascadesThroughNeighboursThatNowCollide()
    {
        // forward: 20, 32, 44 -> end 54 > 40, overshoot 14; C -> 30, B -> 18, A -> 6
        var r = Spread(2, 0, 40, new Interval1D(20, 10), new Interval1D(21, 10), new Interval1D(22, 10));
        Assert.Equal(6, r[0].Start, Eps);
        Assert.Equal(18, r[1].Start, Eps);
        Assert.Equal(30, r[2].Start, Eps);
        Assert.Equal(40, r[2].End, Eps);
    }

    [Fact]
    public void SpreadApart_ClusterOfTwelveAtTheBottomEdge_StaysInsideTheBoundsWithEveryGapAtLeastThePadding()
    {
        var items = Enumerable.Range(0, 12).Select(i => Interval1D.FromCentre(395 + i * 0.3, 10)).ToArray();
        var r = Spread(2, 0, 400, items);

        Assert.All(r, x => Assert.InRange(x.Start, -Eps, 400 + Eps));
        Assert.True(r[^1].End <= 400 + Eps);
        for (int i = 1; i < r.Count; i++)
            Assert.True(r[i].Start - r[i - 1].End >= 2 - Eps, $"gap {i} = {r[i].Start - r[i - 1].End}");
    }

    [Fact]
    public void SpreadApart_BoundsTooSmallForAllIntervals_KeepsThePaddingAndSpillsBelowLower()
    {
        // what SankeyLayoutEngine does today: 3 x 10 + 2 x 2 = 34 > 20, the stack keeps its gaps and is
        // pulled up past the lower bound (forward 0/12/24, overshoot 14, backward -14/-2/10)
        var r = Spread(2, 0, 20, new Interval1D(0, 10), new Interval1D(5, 10), new Interval1D(10, 10));
        Assert.Equal(-14, r[0].Start, Eps);
        Assert.Equal(-2, r[1].Start, Eps);
        Assert.Equal(10, r[2].Start, Eps);
        Assert.Equal(20, r[2].End, Eps);
    }
}
