// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Tests.Numerics;

/// <summary>The order that puts values from the largest to the smallest, given as the indices to read them in, so a verb
/// can reorder several parallel arrays (names and counts, every group's values) by one permutation. The sort is stable:
/// equal values keep the order they were given in, so ties never reshuffle between runs or platforms.</summary>
public class SortOrderExtensionsTests
{
    /// <summary>Distinct values come out from the largest to the smallest.</summary>
    [Fact]
    public void DistinctValues_ComeOutFromTheLargest()
    {
        double[] counts = [45, 142, 9, 98, 67];

        int[] order = counts.DescendingOrder();

        Assert.Equal([1, 3, 4, 0, 2], order);
        Assert.Equal([142.0, 98, 67, 45, 9], order.Select(i => counts[i]));
    }

    /// <summary>Ties keep the order they were given in: of the three 2s the first given comes first, then the next.</summary>
    [Fact]
    public void Ties_KeepTheOrderTheyWereGivenIn()
    {
        double[] values = [1, 2, 2, 1, 2];

        Assert.Equal([1, 2, 4, 0, 3], values.DescendingOrder());
    }

    /// <summary>Ties keep their order in a sample long enough that an unstable sort partitions it (Array.Sort sorts 16
    /// values or fewer by insertion, which is stable by accident): forty values cycling 1, 2, 3 come out as every 3 in the
    /// order given, then every 2, then every 1.</summary>
    [Fact]
    public void Ties_KeepTheirOrder_InALongSample()
    {
        double[] values = [.. Enumerable.Range(0, 40).Select(i => (double)(i % 3 + 1))];
        int[] expected = [.. new[] { 3.0, 2.0, 1.0 }.SelectMany(v => Enumerable.Range(0, 40).Where(i => values[i] == v))];

        Assert.Equal(expected, values.DescendingOrder());
    }

    /// <summary>A missing value (NaN) comes last, behind every number, minus infinity included; missing values among
    /// themselves keep their order too.</summary>
    [Fact]
    public void AMissingValue_ComesLast()
    {
        double[] values = [3, double.NaN, 5, 3, double.NegativeInfinity, double.NaN];

        Assert.Equal([2, 0, 3, 4, 1, 5], values.DescendingOrder());
    }

    /// <summary>The order is a permutation: every index once, also for one value and for none.</summary>
    [Theory]
    [InlineData(new double[0])]
    [InlineData(new[] { 7.0 })]
    [InlineData(new[] { 4.0, 4.0, 4.0, 4.0 })]
    public void TheOrder_HoldsEveryIndexOnce(double[] values)
    {
        Assert.Equal(Enumerable.Range(0, values.Length), values.DescendingOrder().Order());
    }

    /// <summary>No values at all is refused by the parameter's name.</summary>
    [Fact]
    public void NoValues_AreRefusedByName()
    {
        var error = Assert.Throws<ArgumentNullException>(() => ((double[])null!).DescendingOrder());

        Assert.Equal("values", error.ParamName);
    }
}
