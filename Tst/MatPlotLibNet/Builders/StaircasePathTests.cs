// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Builders;

/// <summary>The geometry <c>Stairs</c> draws, in one place: bin <c>i</c> runs from edge <c>i</c> to edge <c>i + 1</c> at its
/// value, so it contributes two points at the same height, and the next bin begins where this one ends, which is the riser.
/// A bin whose value or either edge cannot be drawn is a gap: both its points are missing, so neither the outline nor the fill
/// bridges it and no stub is left of the bin next to a missing edge.</summary>
public class StaircasePathTests
{
    private const double Gap = double.NaN;

    [Fact]
    public void OneBin_IsItsTwoEdges_AtItsValue()
    {
        var path = StaircasePath.Of([5.0], [0.0, 1.0]);

        Assert.Equal([0.0, 1.0], path.X);
        Assert.Equal([5.0, 5.0], path.Y);
    }

    [Fact]
    public void ManyBins_FollowEachOther_TheRiserBeingTheStepFromOneValueToTheNext()
    {
        var path = StaircasePath.Of([5.0, 3.0, 7.0], [0.0, 1.0, 2.0, 3.0]);

        Assert.Equal([0.0, 1.0, 1.0, 2.0, 2.0, 3.0], path.X);
        Assert.Equal([5.0, 5.0, 3.0, 3.0, 7.0, 7.0], path.Y);
    }

    [Fact]
    public void TheBinsNeedNotBeEqualInWidth_TheEdgesAreWhereTheyAre()
    {
        var path = StaircasePath.Of([1.0, 2.0], [0.0, 0.5, 4.0]);

        Assert.Equal([0.0, 0.5, 0.5, 4.0], path.X);
        Assert.Equal([1.0, 1.0, 2.0, 2.0], path.Y);
    }

    [Fact]
    public void AZeroValue_IsADrawnBin_NotAGap()
    {
        var path = StaircasePath.Of([0.0], [0.0, 1.0]);

        Assert.Equal([0.0, 0.0], path.Y);
    }

    [Fact]
    public void AMissingValue_MakesAGapOfItsBin_AndKeepsTheEdgesItStandsBetween()
    {
        var path = StaircasePath.Of([5.0, Gap, 7.0], [0.0, 1.0, 2.0, 3.0]);

        Assert.Equal([0.0, 1.0, 1.0, 2.0, 2.0, 3.0], path.X);
        Assert.Equal([5.0, 5.0, Gap, Gap, 7.0, 7.0], path.Y);
    }

    [Fact]
    public void AMissingEdge_MasksBothBinsNextToIt()
    {
        // Edge 2 is missing: bin 1 ends in it and bin 2 begins with it, so neither can be placed.
        var path = StaircasePath.Of([5.0, 3.0, 7.0, 2.0], [0.0, 1.0, Gap, 3.0, 4.0]);

        Assert.Equal([0.0, 1.0, 1.0, Gap, Gap, 3.0, 3.0, 4.0], path.X);
        Assert.Equal([5.0, 5.0, Gap, Gap, Gap, Gap, 2.0, 2.0], path.Y);
    }

    [Theory]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void AnInfinityIsNotDrawable_SoItMakesTheSameGapAsAMissingValue(double bad)
    {
        var inValue = StaircasePath.Of([5.0, bad, 7.0], [0.0, 1.0, 2.0, 3.0]);
        var inEdge = StaircasePath.Of([5.0, 3.0, 7.0, 2.0], [0.0, 1.0, bad, 3.0, 4.0]);

        Assert.Equal([5.0, 5.0, Gap, Gap, 7.0, 7.0], inValue.Y);
        Assert.Equal([5.0, 5.0, Gap, Gap, Gap, Gap, 2.0, 2.0], inEdge.Y);
    }

    [Fact]
    public void NoBins_AreAnEmptyPath_WhenTheEdgesAreTheOneEdgeTheyNeed()
    {
        var path = StaircasePath.Of([], [3.0]);

        Assert.Empty(path.X);
        Assert.Empty(path.Y);
    }

    [Fact]
    public void ThePath_HoldsTwoPointsForEveryBin_MissingOrNot()
    {
        var path = StaircasePath.Of([1.0, Gap, 3.0, 4.0], [0.0, 1.0, 2.0, Gap, 4.0]);

        Assert.Equal(8, path.X.Length);
        Assert.Equal(8, path.Y.Length);
    }

    [Fact]
    public void FewerEdgesThanTheBinsNeed_IsRefused_ByTheNameOfTheEdges()
    {
        double[] values = [1.0, 2.0, 3.0];
        double[] edges = [0.0, 1.0, 2.0];

        var thrown = Assert.Throws<ArgumentException>(() => StaircasePath.Of(values, edges));

        Assert.Equal("edges", thrown.ParamName);
        Assert.Contains("edges has 3 values", thrown.Message);
        Assert.Contains("values has 3", thrown.Message);
    }

    [Fact]
    public void MoreEdgesThanTheBinsNeed_IsRefused_ByTheNameOfTheEdges()
    {
        double[] values = [1.0, 2.0];
        double[] edges = [0.0, 1.0, 2.0, 3.0];

        var thrown = Assert.Throws<ArgumentException>(() => StaircasePath.Of(values, edges));

        Assert.Equal("edges", thrown.ParamName);
        Assert.Contains("edges has 4 values", thrown.Message);
        Assert.Contains("values has 2", thrown.Message);
    }

    [Fact]
    public void TheEdgesOfNoBins_AreOneEdge_NotNone()
    {
        double[] values = [];
        double[] edges = [];

        Assert.Equal("edges", Assert.Throws<ArgumentException>(() => StaircasePath.Of(values, edges)).ParamName);
    }

    [Fact]
    public void ANullArray_IsRefused_ByItsName()
    {
        double[] values = [1.0];
        double[] edges = [0.0, 1.0];
        double[]? missing = null;

        // The name is the caller's own name for the argument, so a verb that passes its parameters is refused in its own words.
        Assert.Equal("missing", Assert.Throws<ArgumentNullException>(() => StaircasePath.Of(missing!, edges)).ParamName);
        Assert.Equal("missing", Assert.Throws<ArgumentNullException>(() => StaircasePath.Of(values, missing!)).ParamName);
    }

    [Fact]
    public void TheArraysTheCallerGave_AreNotKept()
    {
        double[] values = [1.0, 2.0];
        double[] edges = [0.0, 1.0, 2.0];
        var path = StaircasePath.Of(values, edges);

        values[0] = 99;
        edges[0] = 99;

        Assert.Equal(0.0, path.X[0]);
        Assert.Equal(1.0, path.Y[0]);
    }
}
