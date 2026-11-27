// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Tests.Numerics;

/// <summary>A value in whole steps (<see cref="WholeStepExtensions"/>): numpy's <c>divmod</c>, and the whole multiple of a step at or
/// below and at or above a value, a remainder within a tolerance of a whole step counted as one. The one place the ladders of ticks
/// (matplotlib's <c>_Edge_integer</c>) and the bricks and boxes of a price chart count whole steps.</summary>
public class WholeStepTests
{
    /// <summary>numpy's <c>divmod</c>: the remainder takes the divisor's sign, and a quotient a hair past a whole number is
    /// that number (<c>np.divmod(-4.977554436330596, 0.8)</c> is (-7.0, 0.6224455636694042)).</summary>
    [Fact]
    public void DivMod_IsNumpys()
    {
        Assert.Equal(new WholeStepDivision(-7.0, 0.6224455636694042), (-4.977554436330596).DivMod(0.8));
        Assert.Equal(new WholeStepDivision(-2.0, 0.0), (-1.6).DivMod(0.8));
        Assert.Equal(new WholeStepDivision(0.0, 0.0), 0.0.DivMod(0.8));
        Assert.Equal(new WholeStepDivision(-1.0, 0.5), (-0.5).DivMod(1.0));
    }

    [Theory]
    [InlineData(0.3, 0.1, 3)]              // 0.3 is 2.9999999999999996 steps: within the tolerance of 3
    [InlineData(0.30000000000000004, 0.1, 3)]
    [InlineData(0.35, 0.1, 3)]
    [InlineData(-0.35, 0.1, -4)]
    [InlineData(2.0, 1.0, 2)]
    public void StepsAtOrBelow_IsTheLargestWholeMultipleAtOrBelow(double value, double step, double expected)
    {
        Assert.Equal(expected, value.StepsAtOrBelow(step, 1e-10));
    }

    [Theory]
    [InlineData(0.3, 0.1, 3)]
    [InlineData(0.30000000000000004, 0.1, 3)]   // a hair past 3 steps: within the tolerance of 3
    [InlineData(0.35, 0.1, 4)]
    [InlineData(-0.35, 0.1, -3)]
    [InlineData(2.0, 1.0, 2)]
    public void StepsAtOrAbove_IsTheSmallestWholeMultipleAtOrAbove(double value, double step, double expected)
    {
        Assert.Equal(expected, value.StepsAtOrAbove(step, 1e-10));
    }

    [Fact]
    public void TheTolerance_IsAShareOfAStep()
    {
        // Five ten-billionths of a step below 2: inside a tolerance of a billionth, outside one of a ten-billionth.
        double value = 2 - 5e-10;

        Assert.Equal(2, value.StepsAtOrBelow(1, 1e-9));
        Assert.Equal(1, value.StepsAtOrBelow(1, 1e-10));
        Assert.Equal(2, (2 + 5e-10).StepsAtOrAbove(1, 1e-9));
        Assert.Equal(3, (2 + 5e-10).StepsAtOrAbove(1, 1e-10));
    }
}
