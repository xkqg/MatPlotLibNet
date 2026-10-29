// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.Layout;

namespace MatPlotLibNet.Tests.Rendering.Layout;

public class AxisStackingExtensionsTests
{
    [Fact]
    public void StackedOffsets_Empty_ReturnsEmpty()
    {
        Assert.Empty(Array.Empty<double>().StackedOffsets(4));
    }

    [Fact]
    public void StackedOffsets_OneBand_IsZero()
    {
        var offsets = new double[] { 42 }.StackedOffsets(4);
        Assert.Equal([0.0], offsets);
    }

    [Fact]
    public void StackedOffsets_TwoBands_SecondStandsPastTheFirst()
    {
        var offsets = new double[] { 10, 20 }.StackedOffsets(0);
        Assert.Equal([0.0, 10.0], offsets);
    }

    [Fact]
    public void StackedOffsets_ThreeBands_EachStandsPastTheSumBeforeIt()
    {
        var offsets = new double[] { 10, 20, 5 }.StackedOffsets(0);
        Assert.Equal([0.0, 10.0, 30.0], offsets);
    }

    /// <summary>The first slot stands at the edge; each slot after it stands the gap past the far side of the one before
    /// it, so the gap separates the outermost ink of a slot from the tick marks of the next.</summary>
    [Fact]
    public void StackedOffsets_WithAGap_EachStandsTheGapPastTheBandsBeforeIt()
    {
        var offsets = new double[] { 10, 20, 5 }.StackedOffsets(4);
        Assert.Equal([0.0, 10.0 + 4, 10.0 + 4 + 20 + 4], offsets);
    }

    [Fact]
    public void TotalStackedBand_IsTheSumOfEveryBand()
    {
        Assert.Equal(35.0, new double[] { 10, 20, 5 }.TotalStackedBand(0));
    }

    /// <summary>The total is where the last slot's band ends: every band and one gap between each pair, none past the
    /// last.</summary>
    [Fact]
    public void TotalStackedBand_WithAGap_AddsOneGapBetweenEachPair()
    {
        double[] bands = [10, 20, 5];

        Assert.Equal(35.0 + 2 * 4, bands.TotalStackedBand(4));
        Assert.Equal(bands.StackedOffsets(4)[^1] + bands[^1], bands.TotalStackedBand(4));
        Assert.Equal(42.0, new double[] { 42 }.TotalStackedBand(4));
    }

    [Fact]
    public void TotalStackedBand_Empty_IsZero()
    {
        Assert.Equal(0.0, Array.Empty<double>().TotalStackedBand(4));
    }
}
