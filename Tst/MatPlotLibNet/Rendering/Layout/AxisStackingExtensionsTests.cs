// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.Layout;

namespace MatPlotLibNet.Tests.Rendering.Layout;

public class AxisStackingExtensionsTests
{
    [Fact]
    public void StackedOffsets_Empty_ReturnsEmpty()
    {
        Assert.Empty(Array.Empty<double>().StackedOffsets());
    }

    [Fact]
    public void StackedOffsets_OneBand_IsZero()
    {
        var offsets = new double[] { 42 }.StackedOffsets();
        Assert.Equal([0.0], offsets);
    }

    [Fact]
    public void StackedOffsets_TwoBands_SecondStandsPastTheFirst()
    {
        var offsets = new double[] { 10, 20 }.StackedOffsets();
        Assert.Equal([0.0, 10.0], offsets);
    }

    [Fact]
    public void StackedOffsets_ThreeBands_EachStandsPastTheSumBeforeIt()
    {
        var offsets = new double[] { 10, 20, 5 }.StackedOffsets();
        Assert.Equal([0.0, 10.0, 30.0], offsets);
    }

    [Fact]
    public void TotalStackedBand_IsTheSumOfEveryBand()
    {
        Assert.Equal(35.0, new double[] { 10, 20, 5 }.TotalStackedBand());
    }

    [Fact]
    public void TotalStackedBand_Empty_IsZero()
    {
        Assert.Equal(0.0, Array.Empty<double>().TotalStackedBand());
    }
}
