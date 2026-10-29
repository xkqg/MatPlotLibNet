// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Numerics;

/// <summary>The constants of the X-bar and range charts for one subgroup size.</summary>
/// <param name="A2">The X-bar chart's limits stand A2 times the average range either side of the grand mean.</param>
/// <param name="D3">The range chart's lower limit is D3 times the average range (0 when three sigma of the range reaches below 0).</param>
/// <param name="D4">The range chart's upper limit is D4 times the average range.</param>
internal readonly record struct SubgroupConstants(double A2, double D3, double D4);

/// <summary>The constants control charts are drawn with, in one place: how many sigma the limits stand from the centre, the
/// constants of the individuals and moving-range pair, and the table of the X-bar and range charts.</summary>
internal static class ControlChartConstants
{
    /// <summary>How many sigma the control limits stand from the centre: three, Shewhart's limits.</summary>
    internal const double Sigmas = 3;

    /// <summary>d2 for ranges of two values: the expected range of two standard normal observations (2 / sqrt(pi), to the
    /// three decimals the control-chart tables give), by which the average moving range of an individuals chart is divided
    /// to estimate sigma.</summary>
    internal const double MovingRangeD2 = 1.128;

    /// <summary>D4 for ranges of two values, to the three decimals the control-chart tables give: the upper control limit of a
    /// moving range chart is this times the average moving range (its lower limit, D3, is 0).</summary>
    internal const double MovingRangeD4 = 3.267;

    /// <summary>The smallest subgroup that has a range.</summary>
    internal const int SmallestSubgroup = 2;

    /// <summary>The largest subgroup the table goes to.</summary>
    internal const int LargestSubgroup = 10;

    /// <summary>A2, D3 and D4 for subgroups of 2 to 10, to five decimals. They are the exact values, from the distribution of the
    /// range of n normal observations with mean d2 and standard deviation d3 (A2 = 3 / (d2 sqrt(n)), D3 = max(0, 1 - 3 d3 / d2),
    /// D4 = 1 + 3 d3 / d2), integrated numerically, and agree with the three decimals of NIST/SEMATECH e-Handbook of Statistical
    /// Methods (6.3.2) to within a unit of their last digit (NIST rounds loosely: its D4 for 3 is 2.574 where the exact value
    /// rounds to 2.575). d2 itself is not kept: the zones of the rules stand on the
    /// limits drawn, so nothing reads it.</summary>
    private static readonly SubgroupConstants[] Subgroups =
    [
        new(1.87997, 0.00000, 3.26653),   // 2
        new(1.02333, 0.00000, 2.57459),   // 3
        new(0.72860, 0.00000, 2.28205),   // 4
        new(0.57682, 0.00000, 2.11450),   // 5
        new(0.48325, 0.00000, 2.00383),   // 6
        new(0.41928, 0.07571, 1.92429),   // 7
        new(0.37253, 0.13617, 1.86383),   // 8
        new(0.33670, 0.18401, 1.81599),   // 9
        new(0.30826, 0.22302, 1.77698),   // 10
    ];

    /// <summary>The constants for subgroups of <paramref name="size"/> observations.</summary>
    /// <param name="size">The subgroup size, from <see cref="SmallestSubgroup"/> to <see cref="LargestSubgroup"/>.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="size"/> is outside the table.</exception>
    internal static SubgroupConstants ForSubgroup(int size)
    {
        if (size < SmallestSubgroup || size > LargestSubgroup)
        {
            throw new ArgumentOutOfRangeException(nameof(size), size, $"The table holds subgroups of {SmallestSubgroup} to {LargestSubgroup}.");
        }

        return Subgroups[size - SmallestSubgroup];
    }
}
