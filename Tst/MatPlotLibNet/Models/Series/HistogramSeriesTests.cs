// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="HistogramSeries"/> behaviour; what it starts from and the settings it stores are held by <see cref="Serialization.RestingValueCorpusTests"/> and the drawn-property round trips.</summary>
public class HistogramSeriesTests
{
    /// <summary>Verifies that ComputeBins returns valid counts, min, and bin width.</summary>
    [Fact]
    public void ComputeBins_ReturnsValidResult()
    {
        var series = new HistogramSeries([1.0, 2.0, 3.0, 4.0, 5.0]) { Bins = 5 };
        var bins = series.ComputeBins();
        Assert.Equal(5, bins.Counts.Length);
        Assert.Equal(1.0, bins.Min);
        Assert.True(bins.BinWidth > 0);
    }

    /// <summary>numpy drops a missing value and an infinity with its weight: <c>np.histogram([1, 2, nan, -inf, 3], bins=2, range=(1, 3),
    /// weights=[1, 10, 100, 1000, 10000])</c> is <c>[1, 10010]</c>. The weighted heights leave out what the counts leave out (a missing
    /// value was weighed into the first bin, and an infinity below the data threw).</summary>
    [Fact]
    public void AWeightedHistogram_LeavesOutTheWeightOfAValueItDoesNotCount()
    {
        var series = new HistogramSeries([1.0, 2.0, double.NaN, double.NegativeInfinity, 3.0])
        {
            Bins = 2,
            Weights = [1, 10, 100, 1000, 10000],
        };

        Assert.Equal([1.0, 10010], series.Heights(series.ComputeBins()));
    }
}
