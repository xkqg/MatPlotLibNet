// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Tests.Numerics;

/// <summary>The constants of the X-bar and range charts for subgroups of 2 to 10, to five decimals: A2 (the X-bar chart's limits
/// stand A2 times the average range either side of the grand mean), D3 and D4 (the range chart's lower and upper limits are D3
/// and D4 times the average range). They are the exact values, from the distribution of the range of n normal observations
/// (A2 = 3 / (d2 sqrt(n)), D3 = max(0, 1 - 3 d3 / d2), D4 = 1 + 3 d3 / d2 with d2 and d3 the mean and standard deviation of that
/// range, integrated numerically with scipy 1.18.1 to 1e-13), and agree with the three decimals NIST/SEMATECH e-Handbook of
/// Statistical Methods prints (section 6.3.2) to within a unit of its last digit.</summary>
public class ControlChartConstantsTests
{
    /// <summary>NIST's table, subgroup sizes 2 to 10: A2, D3, D4.</summary>
    private static readonly Dictionary<int, double[]> Nist = new()
    {
        [2] = [1.880, 0.000, 3.267],
        [3] = [1.023, 0.000, 2.574],
        [4] = [0.729, 0.000, 2.282],
        [5] = [0.577, 0.000, 2.114],
        [6] = [0.483, 0.000, 2.004],
        [7] = [0.419, 0.076, 1.924],
        [8] = [0.373, 0.136, 1.864],
        [9] = [0.337, 0.184, 1.816],
        [10] = [0.308, 0.223, 1.777],
    };

    /// <summary>The exact values, integrated to 1e-13 (scipy 1.18.1): A2, D3, D4.</summary>
    private static readonly Dictionary<int, double[]> Exact = new()
    {
        [2] = [1.8799712059732476, 0.0, 3.2665319192886013],
        [3] = [1.0233267079464892, 0.0, 2.5745912897911567],
        [4] = [0.7285971858947051, 0.0, 2.2820515614107126],
        [5] = [0.5768193340850872, 0.0, 2.114499145095885],
        [6] = [0.48324602427050933, 0.0, 2.003829817080793],
        [7] = [0.41928396411410973, 0.07570774242525924, 1.9242922575747408],
        [8] = [0.3725273755827946, 0.13617099584353642, 1.8638290041564636],
        [9] = [0.3366973524033659, 0.18401301573161377, 1.8159869842683862],
        [10] = [0.3082637252366101, 0.2230226557387338, 1.7769773442612662],
    };

    public static TheoryData<int> Sizes { get; } = new(Enumerable.Range(2, 9));

    private static double[] Read(SubgroupConstants constants) => [constants.A2, constants.D3, constants.D4];

    /// <summary>Each constant is within a unit of NIST's last digit (1e-3). NIST rounds loosely: its D4 for 3 is 2.574 where the
    /// exact value, 2.57459, rounds to 2.575, 5.9e-4 away.</summary>
    [Theory]
    [MemberData(nameof(Sizes))]
    public void TheTable_AgreesWithNistToAUnitOfItsLastDigit(int size)
    {
        double[] table = Read(ControlChartConstants.ForSubgroup(size));

        for (int k = 0; k < 3; k++)
        {
            Assert.True(Math.Round(Math.Abs(table[k] - Nist[size][k]), 9) <= 1e-3, $"n = {size}, constant {k}: {table[k]} against NIST {Nist[size][k]}");
        }
    }

    /// <summary>Each constant is the exact value rounded to five decimals: within half a unit of the fifth (5e-6).</summary>
    [Theory]
    [MemberData(nameof(Sizes))]
    public void TheTable_IsTheExactValueToFiveDecimals(int size)
    {
        double[] table = Read(ControlChartConstants.ForSubgroup(size));

        for (int k = 0; k < 3; k++)
        {
            Assert.True(Math.Round(Math.Abs(table[k] - Exact[size][k]), 12) <= 5e-6, $"n = {size}, constant {k}: {table[k]} against {Exact[size][k]:R}");
        }
    }

    /// <summary>A range cannot be negative: D3 is 0 for subgroups of 2 to 6, where 3 sigma of the range reaches below 0, and
    /// positive from 7; D4 falls with the size, A2 too.</summary>
    [Fact]
    public void TheTable_HasTheShapeTheDistributionHas()
    {
        Assert.All(Enumerable.Range(2, 5), n => Assert.Equal(0.0, ControlChartConstants.ForSubgroup(n).D3));
        Assert.All(Enumerable.Range(7, 4), n => Assert.True(ControlChartConstants.ForSubgroup(n).D3 > 0));
        for (int n = 3; n <= 10; n++)
        {
            Assert.True(ControlChartConstants.ForSubgroup(n).A2 < ControlChartConstants.ForSubgroup(n - 1).A2);
            Assert.True(ControlChartConstants.ForSubgroup(n).D4 < ControlChartConstants.ForSubgroup(n - 1).D4);
        }
    }

    /// <summary>The smallest subgroup that has a range is 2 and the table stops at 10; outside it there is no row.</summary>
    [Theory]
    [InlineData(1)]
    [InlineData(0)]
    [InlineData(11)]
    [InlineData(-3)]
    public void ASizeOutsideTheTable_HasNoRow(int size)
    {
        var error = Assert.Throws<ArgumentOutOfRangeException>(() => ControlChartConstants.ForSubgroup(size));

        Assert.Equal("size", error.ParamName);
        Assert.Equal(2, ControlChartConstants.SmallestSubgroup);
        Assert.Equal(10, ControlChartConstants.LargestSubgroup);
    }

    /// <summary>The I-MR pair keeps the three-decimal constants every textbook prints for ranges of two, 1.128 and 3.267, which the
    /// first row of the table (1.12838 and 3.26653 by the exact integral) differs from in the fourth decimal.</summary>
    [Fact]
    public void TheMovingRangePair_KeepsItsTextbookConstants()
    {
        Assert.Equal(1.128, ControlChartConstants.MovingRangeD2);
        Assert.Equal(3.267, ControlChartConstants.MovingRangeD4);
        Assert.Equal(3.26653, ControlChartConstants.ForSubgroup(2).D4);
    }
}
