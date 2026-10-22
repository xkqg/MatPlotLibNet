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
        Assert.Equal([3.0, 1.0, 2.0], Gappy.Observations(2, "The sample", "a chart is drawn from", "values"));

    /// <summary>Sorted, the same observations ascending.</summary>
    [Fact]
    public void SortedObservations_AreTheSameObservations_Ascending() =>
        Assert.Equal([1.0, 2.0, 3.0], Gappy.SortedObservations(2, "The sample", "a chart is drawn from", "values"));

    /// <summary>Both refuse a sample with fewer observations than the chart needs, in the same words, naming the caller's
    /// parameter.</summary>
    [Fact]
    public void TooFewObservations_AreRefused_InOneWording()
    {
        double[] one = [double.NaN, 4.2];

        var inOrder = Assert.Throws<ArgumentException>(() => one.Observations(2, "The sample", "a chart is drawn from", "values"));
        var sorted = Assert.Throws<ArgumentException>(() => one.SortedObservations(2, "The sample", "a chart is drawn from", "values"));

        Assert.Equal("values", inOrder.ParamName);
        Assert.Equal("The sample has 1 observation that can be drawn; a chart is drawn from 2 or more (NaN and the infinities are left out). (Parameter 'values')", inOrder.Message);
        Assert.Equal(inOrder.Message, sorted.Message);
    }
}
