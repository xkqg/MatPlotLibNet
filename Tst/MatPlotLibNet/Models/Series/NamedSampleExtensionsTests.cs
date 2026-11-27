// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>The one check every verb estimated from a sample holds it to: a missing observation (NaN) and an infinity are
/// left out, and a sample with too few observations left is refused in one wording. A verb that reads the sample in its
/// order (a control chart's moving range) takes the observations in the order given; a verb that ranks them takes them
/// sorted.</summary>
public class NamedSampleExtensionsTests
{
    private static readonly double[] Gappy = [3, double.NaN, 1, double.PositiveInfinity, 2, double.NegativeInfinity];

    /// <summary>In the order given, without the missing and infinite observations.</summary>
    [Fact]
    public void Observations_KeepTheOrderGiven_WithoutMissingOrInfiniteOnes() =>
        Assert.Equal([3.0, 1.0, 2.0], Gappy.Observations(2, "The sample", "a chart", "values"));

    /// <summary>Sorted, the same observations ascending.</summary>
    [Fact]
    public void SortedObservations_AreTheSameObservations_Ascending() =>
        Assert.Equal([1.0, 2.0, 3.0], Gappy.SortedObservations(2, "The sample", "a chart", "values"));

    /// <summary>Both refuse a sample with fewer observations than the chart needs, in the same words, naming the caller's
    /// parameter.</summary>
    [Fact]
    public void TooFewObservations_AreRefused_InOneWording()
    {
        double[] one = [double.NaN, 4.2];

        var inOrder = Assert.Throws<ArgumentException>(() => one.Observations(2, "The sample", "a chart", "values"));
        var sorted = Assert.Throws<ArgumentException>(() => one.SortedObservations(2, "The sample", "a chart", "values"));

        Assert.Equal("values", inOrder.ParamName);
        Assert.Equal("The sample holds 1 observation; a chart needs at least 2; NaN and the infinities are not counted. (Parameter 'values')", inOrder.Message);
        Assert.Equal(inOrder.Message, sorted.Message);
    }

    /// <summary>Zeros of both signs (twenty, more than a short array's insertion sort), a missing value and the infinities: the
    /// sample every owner of "the observations, ascending" is held to.</summary>
    private static readonly double[] ZerosOfBothSigns =
    [
        0.0, -0.0, 3, -0.0, 0.0, double.NaN, -0.0, 0.0, 1, -0.0, 0.0, -0.0, 0.0, double.PositiveInfinity, -0.0, 0.0,
        -0.0, 0.0, -2, -0.0, 0.0, -0.0, double.NegativeInfinity, 0.0, -0.0,
    ];

    private static long[] Bits(double[] values) => [.. values.Select(BitConverter.DoubleToInt64Bits)];

    /// <summary>The ridgeline, the Q-Q plot and the stem-and-leaf display read the same ascending observations as the box, the
    /// violin, the density curve and the ECDF: one rule, one order of the zeros of either sign, down to the bits.</summary>
    [Fact]
    public void SortedObservations_AreTheOwnersObservations_BitForBit()
    {
        double[] owner = ZerosOfBothSigns.SortedDrawableValues();

        double[] observations = ZerosOfBothSigns.SortedObservations(2, "The sample", "a chart", "values");

        Assert.Equal(Bits(owner), Bits(observations));
        Assert.Equal(-2.0, observations[0]);
        Assert.Equal(3.0, observations[^1]);
        Assert.Equal(22, observations.Length);
    }

    /// <summary>Whatever order the zeros of either sign come in, the observations are the same numbers ascending, and the sample
    /// is left as it was.</summary>
    [Fact]
    public void SortedObservations_AreAscending_AndLeaveTheSampleAlone()
    {
        double[] before = [.. ZerosOfBothSigns];

        double[] observations = ZerosOfBothSigns.SortedObservations(2, "The sample", "a chart", "values");

        Assert.Equal(Bits(before), Bits(ZerosOfBothSigns));
        Assert.Equal(observations.Order().ToArray(), observations);
    }

    /// <summary>A group is read through the same rule as a bare sample.</summary>
    [Fact]
    public void ANamedSample_IsReadThroughTheSameRule()
    {
        var group = new NamedSample("North", ZerosOfBothSigns);

        Assert.Equal(Bits(ZerosOfBothSigns.SortedDrawableValues()), Bits(group.SortedObservations(2, "groups")));
    }
}
