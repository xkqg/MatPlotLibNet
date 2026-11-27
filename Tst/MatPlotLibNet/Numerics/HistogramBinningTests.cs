// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Tests.Numerics;

/// <summary>The grid a histogram counts in (<see cref="HistogramBinning"/>, <see cref="HistogramGrid"/>): which samples it takes and the
/// bins they span when there is nothing to span.</summary>
public sealed class HistogramBinningTests
{
    [Fact]
    public void NoSample_GivesNoBin() => Assert.Empty(HistogramBinning.Compute([], 4).Counts);

    [Fact]
    public void OnlyMissingValues_GiveEmptyBinsFromZero()
    {
        var bins = HistogramBinning.Compute([double.NaN, double.PositiveInfinity], 3);

        Assert.Equal([0, 0, 0], bins.Counts);
        Assert.Equal(0, bins.Min);
        Assert.Equal(1, bins.BinWidth);
    }

    /// <summary>Samples that are all the same are binned as numpy bins them: the bins span the value and half a unit either side, and the
    /// value falls in the bin it stands in. <c>numpy.histogram([5, 5, 5], 3)</c> (numpy 2.5.3): counts [0, 3, 0], edges 4.5 to 5.5.</summary>
    [Fact]
    public void EqualSamples_AreBinnedOverTheValueAndHalfAUnitEitherSide()
    {
        var bins = HistogramBinning.Compute([5.0, 5.0, double.NaN, 5.0], 3);

        Assert.Equal([0, 3, 0], bins.Counts);
        Assert.Equal(4.5, bins.Min);
        Assert.Equal(1.0 / 3, bins.BinWidth, 15);
    }

    [Fact]
    public void ACountOfNoBins_IsReadAsOne() => Assert.Equal([2], HistogramBinning.Compute([1.0, 2.0], 0).Counts);

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.NegativeInfinity)]
    public void AValueAHistogramDoesNotCount_FallsInNoBin(double value) =>
        Assert.Equal(-1, HistogramBinning.GridOf([1.0, 2.0], 2).BinOf(value));

    [Fact]
    public void TheEdgesOfTheBinsDrawn_StepByTheWidthFromTheFirst()
    {
        var bins = HistogramBinning.Compute([1.0, 4.0], 3);

        Assert.Equal([1.0, 2, 3, 4], Enumerable.Range(0, 4).Select(bins.EdgeAt));
    }
}
