// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Tests.Numerics;

/// <summary>The price arithmetic Renko, Point and Figure and Kagi share. The decimal rows are the point: a step of 0.1 or 0.01
/// is not exact in binary, and every one of these goes wrong with a plain floor, a running sum or a plain comparison.</summary>
public class PriceStepTests
{
    [Theory]
    [InlineData(0.3, 0.0, 0.1, 3)]       // 0.3 / 0.1 = 2.9999999999999996: a plain floor says 2
    [InlineData(0.57, 0.0, 0.01, 57)]    // 0.57 / 0.01 = 56.99999999999999: a plain floor says 56
    [InlineData(0.7, 0.0, 0.1, 7)]       // 0.7 / 0.1 = 6.999999999999999
    [InlineData(10.0, 10.0, 1.0, 0)]     // on the origin
    [InlineData(10.4, 10.0, 1.0, 0)]     // inside the first step
    [InlineData(11.0, 10.0, 1.0, 1)]     // on a boundary: at or above it
    [InlineData(12.99, 10.0, 1.0, 2)]
    [InlineData(9.5, 10.0, 1.0, -1)]     // below the origin
    [InlineData(-0.3, 0.0, 0.1, -3)]
    public void AFloorStep_CountsTheWholeStepsUnderThePrice_AcrossDecimalBoundaries(double price, double origin, double step, int expected)
    {
        Assert.Equal(expected, price.FloorStep(origin, step));
    }

    [Theory]
    [InlineData(0.30000000000000004, 0.0, 0.1, 3)]   // 0.1 * 3: a plain ceiling says 4
    [InlineData(0.3, 0.0, 0.1, 3)]
    [InlineData(0.31, 0.0, 0.1, 4)]
    [InlineData(10.0, 10.0, 1.0, 0)]
    [InlineData(10.4, 10.0, 1.0, 1)]
    [InlineData(9.5, 10.0, 1.0, 0)]
    [InlineData(-0.3, 0.0, 0.1, -3)]
    public void ACeilingStep_CountsTheWholeStepsToTheBoundaryAbove_AcrossDecimalBoundaries(double price, double origin, double step, int expected)
    {
        Assert.Equal(expected, price.CeilingStep(origin, step));
    }

    [Fact]
    public void ADistanceOfOneStepThatSubtractionRoundsDown_StillReachesTheStep()
    {
        Assert.True((0.3 - 0.2).AtLeast(0.1));        // 0.09999999999999998 >= 0.1 is false
        Assert.True((0.57 - 0.56).AtLeast(0.01));
    }

    [Fact]
    public void ADistanceThatFallsShortOfTheStep_DoesNotReachIt()
    {
        Assert.False(0.0999.AtLeast(0.1));
        Assert.False((0.3 - 0.21).AtLeast(0.1));
    }

    [Fact]
    public void AnIndex_IsPricedFromTheOriginEachTime_NotBySummingSteps()
    {
        Assert.Equal(10.0 + 3 * 0.1, 3.LevelFrom(10.0, 0.1));
        Assert.Equal(10.0, 0.LevelFrom(10.0, 0.1));
        Assert.Equal(9.0, (-10).LevelFrom(10.0, 0.1), 12);
    }

    [Fact]
    public void ALevelOfAnIndex_IsOnItsOwnBoundary_WhateverTheStep()
    {
        // The boundary an index is priced at counts back to the same index, on a step that is not exact in binary.
        foreach (double step in new[] { 0.1, 0.01, 0.05, 0.25, 1.0, 2.5 })
        {
            for (int index = -40; index <= 40; index++)
            {
                double level = index.LevelFrom(100.0, step);

                Assert.Equal(index, level.FloorStep(100.0, step));
                Assert.Equal(index, level.CeilingStep(100.0, step));
            }
        }
    }

    [Fact]
    public void ATolerance_IsOneBillionthOfAStep()
    {
        Assert.Equal(1e-9, PriceStepExtensions.Tolerance);
    }
}
