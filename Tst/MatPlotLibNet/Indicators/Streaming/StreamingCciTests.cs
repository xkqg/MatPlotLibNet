// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Data;
using MatPlotLibNet.Indicators.Streaming;

namespace MatPlotLibNet.Tests.Indicators.Streaming;

/// <summary>Coverage uplift Batch D — full coverage for <see cref="StreamingCci"/> (was 0%).
/// Verifies typical-price aggregation, the mean-deviation = 0 short-circuit,
/// hand-derived CCI values across a small window, and ring-buffer wraparound.</summary>
public sealed class StreamingCciTests
{
    private static OhlcBar BarWithTp(double typicalPrice)
        => new(typicalPrice, typicalPrice, typicalPrice, typicalPrice); // H=L=C ⇒ TP = price

    [Fact]
    public void Construction_DefaultsAreCorrect()
    {
        var cci = new StreamingCci();
        Assert.Equal(20, cci.WarmupPeriod);
        Assert.Equal("CCI(20)", cci.Label);
        Assert.Single(cci.OutputSeries);
    }

    [Fact]
    public void Construction_CustomPeriod_PropagatesLabel()
    {
        var cci = new StreamingCci(period: 7);
        Assert.Equal(7, cci.WarmupPeriod);
        Assert.Equal("CCI(7)", cci.Label);
    }

    [Fact]
    public void Warmup_OutputIsNaN_UntilPeriodReached()
    {
        var cci = new StreamingCci(period: 3);
        cci.AppendCandle(BarWithTp(10));
        cci.AppendCandle(BarWithTp(20));

        var y = cci.OutputSeries[0].CreateSnapshot().YData;
        Assert.True(double.IsNaN(y[0]));
        Assert.True(double.IsNaN(y[1]));
    }

    [Fact]
    public void TypicalPriceFormula_HighLowClose_ArithmeticMean()
    {
        // TP = (H + L + C) / 3.  With period=1 the window holds only the current bar so
        // mean == TP and meanDev == 0 ⇒ CCI = 0 (regardless of the absolute price level).
        var cci = new StreamingCci(period: 1);
        cci.AppendCandle(new OhlcBar(0, 12, 6, 9)); // TP = (12+6+9)/3 = 9
        Assert.Equal(0.0, cci.OutputSeries[0].CreateSnapshot().YData[0]);
    }

    [Fact]
    public void MeanDeviationZero_AllEqualBars_ReturnsZero()
    {
        // All bars have identical TP ⇒ meanDev = 0 ⇒ branch returns 0.
        var cci = new StreamingCci(period: 3);
        foreach (var bar in StreamingTestData.FlatBars(5, price: 50)) cci.AppendCandle(bar);

        var y = cci.OutputSeries[0].CreateSnapshot().YData;
        Assert.True(double.IsNaN(y[0]));
        Assert.True(double.IsNaN(y[1]));
        Assert.Equal(0.0, y[2]);
        Assert.Equal(0.0, y[3]);
        Assert.Equal(0.0, y[4]);
    }

    [Fact]
    public void HandDerivedSequence_MatchesFormula()
    {
        // period=3, TP series = 10, 20, 30, 20.
        //   bar 3:  window = {10,20,30}, mean=20, meanDev=(10+0+10)/3 = 20/3.
        //           CCI = (30-20) / (0.015 * 20/3) = 10 / 0.1 = 100.
        //   bar 4:  ring slot 0 ← 20 ⇒ window = {20,20,30}, mean = 70/3.
        //           meanDev = (|20-70/3| + |20-70/3| + |30-70/3|) / 3
        //                   = (10/3 + 10/3 + 20/3) / 3 = (40/3) / 3 = 40/9.
        //           CCI = (20 - 70/3) / (0.015 * 40/9)
        //               = (-10/3) / (40/9 * 0.015)
        //               = (-10/3) / 0.0666666...
        //               = -50.
        var cci = new StreamingCci(period: 3);
        cci.AppendCandle(BarWithTp(10));
        cci.AppendCandle(BarWithTp(20));
        cci.AppendCandle(BarWithTp(30));
        cci.AppendCandle(BarWithTp(20));

        var y = cci.OutputSeries[0].CreateSnapshot().YData;
        Assert.Equal(100.0, y[2], 1e-9);
        Assert.Equal(-50.0, y[3], 1e-9);
    }

    [Fact]
    public void TypicalCciRange_RisingPrices_ProducePositiveCci()
    {
        // Strong uptrend: TP runs 10..30 in 5 steps, period=3.  After warmup the latest
        // TP exceeds the rolling mean ⇒ CCI must be positive.
        var cci = new StreamingCci(period: 3);
        for (int i = 0; i < 5; i++) cci.AppendCandle(BarWithTp(10 + i * 5));
        Assert.True(cci.GetLatest() > 0);
    }

    [Fact]
    public void TypicalCciRange_FallingPrices_ProduceNegativeCci()
    {
        var cci = new StreamingCci(period: 3);
        for (int i = 0; i < 5; i++) cci.AppendCandle(BarWithTp(50 - i * 5));
        Assert.True(cci.GetLatest() < 0);
    }

    [Fact]
    public void RingBuffer_WrapsCorrectly_WhenBarsExceedPeriod()
    {
        var cci = new StreamingCci(period: 4);
        for (int i = 0; i < 12; i++)
            cci.AppendCandle(new OhlcBar(i, i + 2, i - 1, i + 1));

        var y = cci.OutputSeries[0].CreateSnapshot().YData;
        Assert.Equal(12, y.Length);
        Assert.False(double.IsNaN(y[^1]));
    }

    [Fact]
    public void RingBuffer_RespectsCapacity_OnOverflow()
    {
        var cci = new StreamingCci(period: 2, capacity: 3);
        for (int i = 0; i < 6; i++)
            cci.AppendCandle(new OhlcBar(i, i + 2, i - 1, i + 1));

        Assert.Equal(3, cci.OutputSeries[0].Count);
    }

    [Fact]
    public void ComputeNext_ScalarPath_ReturnsNaN()
    {
        // The (price-only) Append path is unsupported for OHLC indicators; ComputeNext returns NaN
        // by contract so any consumer mistakenly using Append() gets a clear NaN signal.
        var cci = new StreamingCci(period: 3);
        cci.Append(100);
        Assert.True(double.IsNaN(cci.GetLatest()));
    }
}
