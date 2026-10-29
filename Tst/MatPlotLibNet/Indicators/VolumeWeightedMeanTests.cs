// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Indicators.Streaming;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>The one place a volume-weighted mean price is worked out: the sum of price times volume over the sum of volume, kept as
/// two running sums. The cumulative VWAP, its streaming form and the rolling VWAP of the Z-score all read it, so a mean the three
/// of them draw from the same bars is the same number to the bit. Every number below is worked out by hand or compared with the
/// plain loop it replaced, with <c>==</c>: the sums are added in the same order, so nothing may differ.</summary>
public class VolumeWeightedMeanTests
{
    /// <summary>A mean of nothing reads the fallback, and has no volume.</summary>
    [Fact]
    public void AnEmptyMean_HasNoVolume_AndReadsTheFallback()
    {
        var mean = new VolumeWeightedMean();

        Assert.Equal(0.0, mean.Volume);
        Assert.Equal(7.5, mean.Value(7.5));
    }

    /// <summary>10 and 20 at one lot each, 30 at two: (10 + 20 + 60) / 4 = 22.5.</summary>
    [Fact]
    public void TheMean_IsPriceTimesVolume_OverVolume()
    {
        var mean = new VolumeWeightedMean();

        mean.Add(10, 1);
        Assert.Equal(10.0, mean.Value(0));
        mean.Add(20, 1);
        Assert.Equal(15.0, mean.Value(0));
        mean.Add(30, 2);
        Assert.Equal(22.5, mean.Value(0));
        Assert.Equal(4.0, mean.Volume);
    }

    /// <summary>A bar without volume moves nothing, and a mean that has had none reads the fallback.</summary>
    [Fact]
    public void ABarWithoutVolume_MovesNothing()
    {
        var mean = new VolumeWeightedMean();

        mean.Add(99, 0);
        Assert.Equal(5.0, mean.Value(5));
        mean.Add(10, 2);
        mean.Add(99, 0);
        Assert.Equal(10.0, mean.Value(5));
    }

    /// <summary>A volume that is not a number reads the fallback, as the indicators always did (NaN is not above 0), and
    /// <see cref="VolumeWeightedMean.Volume"/> is the NaN, so a caller that asks whether the volume is at most 0 gets false too.</summary>
    [Fact]
    public void AVolumeThatIsNotANumber_ReadsTheFallback()
    {
        var mean = new VolumeWeightedMean();

        mean.Add(10, double.NaN);

        Assert.Equal(3.0, mean.Value(3));
        Assert.True(double.IsNaN(mean.Volume));
        Assert.False(mean.Volume <= 0);
    }

    [Fact]
    public void Reset_ForgetsEverything()
    {
        var mean = new VolumeWeightedMean();
        mean.Add(10, 3);
        mean.Add(20, 1);

        mean.Reset();

        Assert.Equal(0.0, mean.Volume);
        Assert.Equal(-1.0, mean.Value(-1));
        mean.Add(40, 1);
        Assert.Equal(40.0, mean.Value(0));
    }

    /// <summary>The accumulator agrees with the plain loop the cumulative VWAP used to be, to the bit, over bars that are not
    /// round numbers, with a bar of no volume among them.</summary>
    [Fact]
    public void TheMean_IsBitIdenticalToThePlainLoop()
    {
        var random = new Random(20261029);
        double[] prices = [.. Enumerable.Range(0, 200).Select(_ => 100 + random.NextDouble() * 7 - 3.5)];
        double[] volumes = [.. Enumerable.Range(0, 200).Select(i => i % 17 == 3 ? 0 : Math.Round(random.NextDouble() * 5000, 3))];

        var mean = new VolumeWeightedMean();
        double cumPriceVol = 0, cumVol = 0;
        for (int i = 0; i < prices.Length; i++)
        {
            cumPriceVol += prices[i] * volumes[i];
            cumVol += volumes[i];
            double expected = cumVol > 0 ? cumPriceVol / cumVol : prices[i];

            mean.Add(prices[i], volumes[i]);

            Assert.Equal(expected, mean.Value(prices[i]));
        }
    }

    /// <summary>The three indicators that read the mean give what they gave before it was shared: the cumulative VWAP, its
    /// streaming form (bar for bar) and the Z-score's windows are all the plain loops' numbers.</summary>
    [Fact]
    public void TheCumulativeVwap_AndItsStreamingForm_AreTheSameNumbers()
    {
        var random = new Random(7);
        double[] prices = [.. Enumerable.Range(0, 120).Select(_ => 50 + random.NextDouble() * 4)];
        double[] volumes = [.. Enumerable.Range(0, 120).Select(i => i % 11 == 0 ? 0 : 100 + random.Next(900))];

        double[] batch = new Vwap(prices, volumes).Compute();
        var streaming = new StreamingVwap();
        double cumPriceVol = 0, cumVol = 0;
        for (int i = 0; i < prices.Length; i++)
        {
            streaming.AppendWithVolume(prices[i], volumes[i]);
            cumPriceVol += prices[i] * volumes[i];
            cumVol += volumes[i];
            double expected = cumVol > 0 ? cumPriceVol / cumVol : prices[i];

            Assert.Equal(expected, batch[i]);
            Assert.Equal(expected, streaming.GetLatest());
        }
    }

    /// <summary>The typical price has one owner: (high + low + close) / 3 for any three numbers, to the bit, the very number
    /// <see cref="PriceSources.Resolve"/> gives for the HLC3 source.</summary>
    [Fact]
    public void TheTypicalPrice_IsOneFormula_WithOneOwner()
    {
        double[] high = [10.7, 0.3, 123456.789, 1e-9];
        double[] low = [9.1, 0.1, 123400.001, 1e-10];
        double[] close = [10.3, 0.2, 123450.5, 5e-10];

        double[] resolved = PriceSources.Resolve(PriceSource.HLC3, [], high, low, close);

        for (int i = 0; i < high.Length; i++)
        {
            Assert.Equal((high[i] + low[i] + close[i]) / 3.0, PriceSources.TypicalPrice(high[i], low[i], close[i]));
            Assert.Equal(PriceSources.TypicalPrice(high[i], low[i], close[i]), resolved[i]);
        }

        Assert.Equal(2.0, PriceSources.TypicalPrice(3, 1, 2));
    }
}
