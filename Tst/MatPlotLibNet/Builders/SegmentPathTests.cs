// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Builders;

/// <summary>The geometry every verb that draws many segments as ONE line shares: segment <c>i</c> contributes its two ends
/// and then a missing point that parts it from the next, a segment with an end that cannot be drawn is left out whole (never
/// drawn as a stub, never joined to its neighbour), and a bound given once stands for every segment.</summary>
public class SegmentPathTests
{
    private const double Gap = double.NaN;

    [Fact]
    public void OneSegment_IsItsTwoEnds_AndAGap()
    {
        var path = SegmentPath.Of([1.0], [0.0], [1.0], [5.0]);

        Assert.Equal([1.0, 1.0, Gap], path.X);
        Assert.Equal([0.0, 5.0, Gap], path.Y);
    }

    [Fact]
    public void ManySegments_FollowEachOther_EachPartedFromTheNextByAGap()
    {
        var path = SegmentPath.Of([1.0, 2.0], [0.0, 10.0], [4.0, 3.0], [5.0, 20.0]);

        Assert.Equal([1.0, 4.0, Gap, 2.0, 3.0, Gap], path.X);
        Assert.Equal([0.0, 5.0, Gap, 10.0, 20.0, Gap], path.Y);
    }

    [Theory]
    [InlineData(0, double.NaN)]
    [InlineData(1, double.NaN)]
    [InlineData(2, double.NaN)]
    [InlineData(3, double.NaN)]
    [InlineData(0, double.PositiveInfinity)]
    [InlineData(1, double.NegativeInfinity)]
    [InlineData(2, double.PositiveInfinity)]
    [InlineData(3, double.NegativeInfinity)]
    public void ASegmentWithAnEndThatCannotBeDrawn_IsLeftOutWhole(int which, double bad)
    {
        // Three segments; the middle one gets a value that is not finite in one of its four coordinates.
        double[][] bounds = [[1.0, 2.0, 3.0], [0.0, 0.0, 0.0], [1.0, 2.0, 3.0], [9.0, 9.0, 9.0]];
        bounds[which][1] = bad;

        var path = SegmentPath.Of(bounds[0], bounds[1], bounds[2], bounds[3]);

        // The first and the third survive, with their own separator each; the second leaves no point at all, not even a stub.
        Assert.Equal(6, path.X.Length);
        Assert.Equal(6, path.Y.Length);
        Assert.Equal([1.0, 1.0, Gap, 3.0, 3.0, Gap], path.X);
        Assert.Equal([0.0, 9.0, Gap, 0.0, 9.0, Gap], path.Y);
    }

    [Fact]
    public void WhenEverySegmentIsLeftOut_ThePathIsEmpty()
    {
        var path = SegmentPath.Of([Gap, 2.0], [0.0, double.PositiveInfinity], [1.0, 2.0], [5.0, 5.0]);

        Assert.Empty(path.X);
        Assert.Empty(path.Y);
    }

    [Fact]
    public void NoSegments_IsAnEmptyPath()
    {
        var path = SegmentPath.Of([], [], [], []);

        Assert.Empty(path.X);
        Assert.Empty(path.Y);
    }

    [Fact]
    public void AnEmptyArrayAmongBoundsGivenOnce_IsStillNoSegments()
    {
        var path = SegmentPath.Of([], [0.0], [], [5.0]);

        Assert.Empty(path.X);
        Assert.Empty(path.Y);
    }

    [Fact]
    public void ABoundGivenOnce_StandsForEverySegment()
    {
        var path = SegmentPath.Of([1.0, 2.0, 3.0], [0.0], [1.0, 2.0, 3.0], [5.0]);

        Assert.Equal([1.0, 1.0, Gap, 2.0, 2.0, Gap, 3.0, 3.0, Gap], path.X);
        Assert.Equal([0.0, 5.0, Gap, 0.0, 5.0, Gap, 0.0, 5.0, Gap], path.Y);
    }

    [Fact]
    public void APositionGivenOnce_StandsForEverySegment_WhateverCoordinateItIs()
    {
        // The same x for three segments of different heights: what VLines(x: 4, ...) means with one position.
        var path = SegmentPath.Of([4.0], [0.0, 1.0, 2.0], [4.0], [5.0, 6.0, 7.0]);

        Assert.Equal([4.0, 4.0, Gap, 4.0, 4.0, Gap, 4.0, 4.0, Gap], path.X);
        Assert.Equal([0.0, 5.0, Gap, 1.0, 6.0, Gap, 2.0, 7.0, Gap], path.Y);
    }

    [Fact]
    public void EveryBoundGivenOnce_IsOneSegment()
    {
        var path = SegmentPath.Of([1.0], [2.0], [3.0], [4.0]);

        Assert.Equal([1.0, 3.0, Gap], path.X);
        Assert.Equal([2.0, 4.0, Gap], path.Y);
    }

    [Fact]
    public void ABroadcastBound_ThatCannotBeDrawn_LeavesOutEverySegment()
    {
        var path = SegmentPath.Of([1.0, 2.0], [Gap], [1.0, 2.0], [5.0]);

        Assert.Empty(path.X);
    }

    [Fact]
    public void TheArraysTheCallerGave_AreNotKept()
    {
        double[] x = [1.0, 2.0];
        var path = SegmentPath.Of(x, [0.0], x, [5.0]);

        x[0] = 99;

        Assert.Equal(1.0, path.X[0]);
    }

    [Fact]
    public void ABoundOfAnotherLengthThanTheRest_IsRefused_NamingThatArgument()
    {
        double[] positions = [1.0, 2.0, 3.0];
        double[] lows = [0.0, 1.0];
        double[] highs = [5.0, 6.0, 7.0];

        var thrown = Assert.Throws<ArgumentException>(() => SegmentPath.Of(positions, lows, positions, highs));

        // The name is the caller's own name for the argument, so a verb's refusal names the parameter the caller passed.
        Assert.Equal("lows", thrown.ParamName);
        Assert.Contains("2", thrown.Message);
        Assert.Contains("3", thrown.Message);
        Assert.Contains("lows", thrown.Message);
        Assert.Contains("positions", thrown.Message);
    }

    [Fact]
    public void TheArgumentNamedIsTheOneThatDisagrees_NotTheOneThatCameFirst()
    {
        double[] first = [1.0, 2.0];
        double[] second = [0.0];
        double[] third = [5.0, 6.0, 7.0];

        var thrown = Assert.Throws<ArgumentException>(() => SegmentPath.Of(first, second, first, third));

        // `first` has two values and `second` has one (a bound given once); `third` is the one that disagrees with `first`.
        Assert.Equal("third", thrown.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void ArraysOfDifferentLengths_AreRefused_WhicheverOfTheFourIsTheOddOne(int odd)
    {
        double[][] bounds = [[1.0, 2.0, 3.0], [1.0, 2.0, 3.0], [1.0, 2.0, 3.0], [1.0, 2.0, 3.0]];
        bounds[odd] = [1.0, 2.0];

        var thrown = Assert.Throws<ArgumentException>(() => SegmentPath.Of(bounds[0], bounds[1], bounds[2], bounds[3]));

        // The first array sets the length the others must have, so when it is the odd one the second is the one that disagrees.
        // The name is the text of the argument, here an element of the list.
        Assert.Equal(odd == 0 ? "bounds[1]" : $"bounds[{odd}]", thrown.ParamName);
    }

    [Fact]
    public void AnEmptyArrayAgainstAFullOne_IsRefused()
    {
        double[] none = [];
        double[] some = [1.0, 2.0];

        var thrown = Assert.Throws<ArgumentException>(() => SegmentPath.Of(some, some, none, some));

        Assert.Equal("none", thrown.ParamName);
    }

    [Fact]
    public void ANullArray_IsRefused_ByItsName()
    {
        double[] x = [1.0];
        double[]? missing = null;

        Assert.Equal("missing", Assert.Throws<ArgumentNullException>(() => SegmentPath.Of(x, missing!, x, x)).ParamName);
        Assert.Equal("missing", Assert.Throws<ArgumentNullException>(() => SegmentPath.Of(missing!, x, x, x)).ParamName);
        Assert.Equal("missing", Assert.Throws<ArgumentNullException>(() => SegmentPath.Of(x, x, missing!, x)).ParamName);
        Assert.Equal("missing", Assert.Throws<ArgumentNullException>(() => SegmentPath.Of(x, x, x, missing!)).ParamName);
    }
}
