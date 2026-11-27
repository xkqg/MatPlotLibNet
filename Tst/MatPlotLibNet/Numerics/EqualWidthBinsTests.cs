// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Tests.Numerics;

/// <summary>The one answer to "which of these equal bins does a value fall in", as numpy answers it (<c>numpy.histogram</c>, which
/// matplotlib's <c>hist</c> and <c>hist2d</c> call): the edges are <c>numpy.linspace(low, high, count + 1)</c>, a bin holds its low
/// edge and not its high one, the last bin holds both. The expected numbers were printed by numpy 2.5.3; each value lies on an inner
/// edge or an ulp beside one, where an index read off the arithmetic alone lands a bin early or late.</summary>
public sealed class EqualWidthBinsTests
{
    /// <summary>The library's rule for a violin's body: Silverman's bandwidth on matplotlib's 100 heights.</summary>
    private static readonly DensityRule Silverman = new(GaussianKde.SilvermanFactor, ViolinSeries.Heights);

    /// <summary>A range, a count of bins, a value in it and the bin numpy puts the value in.</summary>
    public static TheoryData<double, double, int, double, int> NumpyBins { get; } = new()
    {
        { -26.862, -12.421999999999999, 12, -24.455333333333332, 2 },
        { 46.5, 100.5, 10, 95.1, 9 },
        { -18.31, 75.69, 12, -2.643333333333333, 1 },
        { 39.1, 135.1, 4, 87.1, 2 },
        { 21.84, 111.54, 34, 69.32823529411765, 18 },
        { -0.66, 12.02, 11, 3.9509090909090907, 4 },
        { -47.6, 50.4, 3, 17.73333333333333, 2 },
        { -8.61, 43.39, 9, 31.834444444444443, 7 },
        { 0.0, 1.0, 10, 0.3, 2 },
        { 0.0, 1.0, 10, 0.7, 6 },
        { 0.0, 1.0, 10, 1.0, 9 },
        { 0.0, 1.0, 10, 0.0, 0 },
        { -3.0, 5.0, 7, 5.0, 6 },
    };

    [Theory]
    [MemberData(nameof(NumpyBins))]
    public void AValue_FallsInTheBinNumpyPutsItIn(double low, double high, int count, double value, int bin) =>
        Assert.Equal(bin, new EqualWidthBins(low, high, count).IndexOf(value));

    [Fact]
    public void TheEdges_AreNumpysLinspace_TheLastOneTheHighEndItself()
    {
        var bins = new EqualWidthBins(0.0, 1.0, 10);

        double[] numpy = [0.0, 0.1, 0.2, 0.30000000000000004, 0.4, 0.5, 0.6000000000000001, 0.7000000000000001, 0.8, 0.9, 1.0];
        Assert.Equal(numpy, Enumerable.Range(0, 11).Select(bins.EdgeAt));
        Assert.Equal(-12.421999999999999, new EqualWidthBins(-26.862, -12.421999999999999, 12).EdgeAt(12));
        Assert.Equal(-13.625333333333332, new EqualWidthBins(-26.862, -12.421999999999999, 12).EdgeAt(11));
    }

    [Theory]
    [InlineData(-1.0, 0)]
    [InlineData(2.0, 9)]
    public void AValuePastAnEnd_FallsInTheBinAtThatEnd(double value, int bin) =>
        Assert.Equal(bin, new EqualWidthBins(0.0, 1.0, 10).IndexOf(value));

    [Fact]
    public void TheWidth_IsTheRangeOverTheCount() => Assert.Equal(24, new EqualWidthBins(39.1, 135.1, 4).Width, 12);

    /// <summary>Evenly spaced values are the edges of equal bins: <c>np.linspace(0.1, 1.7, 100)</c> holds 0.21313131313131314 at 7
    /// and 0.43939393939393945 at 21, where stepping by the share of the range (low + range * i / 99) lands an ulp away.</summary>
    [Fact]
    public void TheEdges_AreNumpysEvenlySpacedValues()
    {
        double[] edges = new EqualWidthBins(0.1, 1.7, 99).Edges();

        Assert.Equal(100, edges.Length);
        Assert.Equal(0.1, edges[0]);
        Assert.Equal(0.21313131313131314, edges[7]);
        Assert.Equal(0.43939393939393945, edges[21]);
        Assert.Equal(1.7, edges[^1]);
    }

    /// <summary>matplotlib's violinplot evaluates the density at <c>np.linspace(min, max, points)</c>.</summary>
    [Fact]
    public void AViolinBody_StandsOnNumpysEvenlySpacedHeights()
    {
        var outline = ViolinSeries.Outline(new[] { 0.1, 0.5, 1.7 }, 1.0, Silverman);

        Assert.Equal(0.21313131313131314, outline[7].Y);
        Assert.Equal(0.43939393939393945, outline[21].Y);
    }

    /// <summary>The volume profile's edges are numpy's: <c>np.linspace(10.3, 13.3, 8)[3]</c> is 11.585714285714285.</summary>
    [Fact]
    public void AVolumeProfile_StandsOnNumpysEdges()
    {
        double[] close = [10.3, 10.7, 11.1, 11.9, 12.6, 13.3, 10.9, 12.2];

        var profile = new VolumeProfile(OhlcvBars.Cv(close, [.. close.Select(_ => 1.0)]), 7).Compute();

        Assert.Equal(11.585714285714285, profile.BinEdges[3]);
        Assert.Equal(close.Length, profile.Volumes.Sum());
    }

    /// <summary>numpy: <c>np.histogram([39.1, 87.1, 135.1], bins=4)</c> is <c>[1, 0, 1, 1]</c>: 87.1 is the third bin's low edge.</summary>
    [Fact]
    public void AHistogram_CountsAValueOnAnEdge_InTheBinItOpens() =>
        Assert.Equal([1, 0, 1, 1], HistogramBinning.Compute([39.1, 87.1, 135.1], 4).Counts);

    /// <summary>numpy: <c>np.histogram2d([39.1, 87.1, 87.2, 135.1], [0, 0, 1, 1], bins=4)</c> puts 87.1 and 87.2 in the third column.</summary>
    [Fact]
    public void AHistogramInTwoDimensions_CountsAValueOnAnEdge_InTheBinItOpens()
    {
        var counts = new Histogram2DSeries([39.1, 87.1, 87.2, 135.1], [0.0, 0, 1, 1]) { BinsX = 4, BinsY = 4 }.ComputeBinCounts();

        Assert.Equal(1, counts[0, 2]);
        Assert.Equal(1, counts[3, 2]);
        Assert.Equal(0, counts[0, 1]);
    }

    [Fact]
    public void APointsDensity_CountsAValueOnAnEdge_InTheCellItOpens() =>
        Assert.Equal([1.0, 2, 2, 1], PointDensity.PerPoint([39.1, 87.1, 87.2, 135.1], [0.0, 0, 0, 0], bins: 4));

    [Fact]
    public void NumpysHistogram_CountsAValueOnAnEdge_InTheBinItOpens() =>
        Assert.Equal([1.0, 0, 1, 1], NpStats.Histogram(new Vec([39.1, 87.1, 135.1]), 4).Counts);
}
