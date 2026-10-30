// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Tests.Numerics;

/// <summary>The largest remainder method: whole cells in proportion to the values, the leftovers to the largest remainders,
/// ties to the value that stands first.</summary>
public class LargestRemainderTests
{
    public static TheoryData<double[], int, int[]> Cases { get; } = new()
    {
        { [37, 33, 30], 100, [37, 33, 30] },             // exact shares
        { [1, 1, 1], 10, [4, 3, 3] },                    // a three-way tie: the extra cell goes to the first
        { [2, 1], 10, [7, 3] },                          // 6.67 and 3.33: the larger remainder gets the cell
        { [1, 1], 5, [3, 2] },                           // a two-way tie
        { [1, 2, 3], 6, [1, 2, 3] },
        { [5], 7, [7] },                                 // one value takes every cell
        { [0.5, 0.25, 0.25], 10, [5, 3, 2] },            // 5, 2.5, 2.5: the tie of the last two goes to the earlier
        { [3, 3, 3, 1], 100, [30, 30, 30, 10] },
        { [1, 1, 1, 1, 1, 1, 1], 100, [15, 15, 14, 14, 14, 14, 14] },   // 14.28...: two extra cells for the first two
        { [0, 5, 5], 10, [0, 5, 5] },                    // a zero takes nothing
        { [1e-9, 1, 1], 100, [0, 50, 50] },              // a share below one cell with a small remainder takes nothing
        { [1, 1, 1], 2, [1, 1, 0] },                     // 0.67 each: rounding would hand out three cells, the method hands out two
        { [3, 3, 1], 3, [1, 1, 1] },                     // 1.29, 1.29, 0.43: the floors leave one cell, which goes to the largest remainder (the third)
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public void TheCells_AreDividedByTheLargestRemainder_TiesToTheEarlier(double[] values, int seats, int[] expected)
    {
        Assert.Equal(expected, values.Apportion(seats));
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void TheCells_AlwaysAddUpToTheSeats(double[] values, int seats, int[] _)
    {
        Assert.Equal(seats, values.Apportion(seats).Sum());
    }

    [Fact]
    public void AValueThatIsNotAFiniteNumberAboveZero_CountsAsNothing()
    {
        Assert.Equal([0, 0, 10, 0, 0], new double[] { double.NaN, -3, 4, double.PositiveInfinity, 0 }.Apportion(10));
    }

    [Fact]
    public void WhenNoValueCounts_NothingIsHandedOut()
    {
        Assert.Equal([0, 0], new double[] { 0, double.NaN }.Apportion(10));
        Assert.Equal([0, 0, 0], new double[] { -1, -2, -3 }.Apportion(10));
        Assert.Empty(Array.Empty<double>().Apportion(10));
    }

    [Fact]
    public void WithNoSeats_NothingIsHandedOut()
    {
        Assert.Equal([0, 0], new double[] { 1, 2 }.Apportion(0));
    }

    [Fact]
    public void EachValue_IsAtMostOneCellFromItsExactShare()
    {
        double[] values = [13, 7, 29, 3, 31, 17];
        double total = values.Sum();

        var cells = values.Apportion(100);

        for (int i = 0; i < values.Length; i++)
        {
            Assert.InRange(cells[i], values[i] / total * 100 - 1, values[i] / total * 100 + 1);
        }
    }
}
