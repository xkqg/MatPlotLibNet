// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Data;
using MatPlotLibNet.Indicators.Streaming;

namespace MatPlotLibNet.Tests.Indicators.Streaming;

/// <summary>Coverage uplift Batch D — completes coverage for <see cref="StreamingAtr"/>
/// (was 91% line / 100% branch). The remaining gap was the <c>WarmupPeriod</c> getter and
/// the unused <c>ComputeNext</c> scalar path. These tests also pin Wilder's smoothing
/// against hand-derived ATR values from a tiny period-2 sequence.</summary>
public sealed class StreamingAtrTests : OhlcStreamingIndicatorTests<StreamingAtr>
{
    protected override StreamingAtr CreateIndicator(int period, int capacity = 256)
        => new(period: period, capacity: capacity);

    [Fact]
    public void Construction_DefaultsAreCorrect()
    {
        var atr = new StreamingAtr();
        Assert.Equal(15, atr.WarmupPeriod);   // period + 1 = 14 + 1
        Assert.Equal("ATR(14)", atr.Label);
        Assert.Single(atr.OutputSeries);
    }

    [Fact]
    public void Construction_CustomPeriod_PropagatesLabel()
    {
        var atr = new StreamingAtr(period: 7);
        Assert.Equal(8, atr.WarmupPeriod);
        Assert.Equal("ATR(7)", atr.Label);
    }

    [Fact]
    public void Warmup_OutputIsNaN_UntilPeriodPlusOne()
    {
        var atr = new StreamingAtr(period: 3);
        atr.AppendCandle(new OhlcBar(10, 11, 9, 10));
        atr.AppendCandle(new OhlcBar(10, 12, 9, 11));
        atr.AppendCandle(new OhlcBar(11, 13, 10, 12));

        var y = atr.OutputSeries[0].CreateSnapshot().YData;
        Assert.True(double.IsNaN(y[0]));
        Assert.True(double.IsNaN(y[1]));
        Assert.True(double.IsNaN(y[2]));
    }

    [Fact]
    public void HandDerivedSequence_MatchesWilderSmoothing()
    {
        // period = 2  ⇒  WarmupPeriod = 3. The first bar has no close before it, so it has no true range (the batch Atr
        // leaves it out too): the seed is the mean of the true ranges of bars 2 and 3, and bar 3 emits ATR.
        // TR walk:
        //   bar 1 (H=10,L=8,C=9):  no TR.                                          out NaN.
        //   bar 2 (H=11,L=9,C=10): TR = max(11-9, |11-9|, |9-9|) = 2.  trSum = 2.   out NaN.
        //   bar 3 (H=12,L=10,C=11):TR = max(12-10,|12-10|,|10-10|)=2.  trSum = 4.
        //                           ATR = trSum / period = 4 / 2 = 2.
        //   bar 4 (H=15,L=11,C=14):TR = max(15-11,|15-11|,|11-11|)=4.
        //                           ATR = (2*(2-1) + 4) / 2 = 6 / 2 = 3.
        var atr = new StreamingAtr(period: 2);
        atr.AppendCandle(new OhlcBar(10, 10, 8, 9));
        atr.AppendCandle(new OhlcBar(10, 11, 9, 10));
        atr.AppendCandle(new OhlcBar(11, 12, 10, 11));
        atr.AppendCandle(new OhlcBar(11, 15, 11, 14));

        var y = atr.OutputSeries[0].CreateSnapshot().YData;
        Assert.True(double.IsNaN(y[0]));
        Assert.True(double.IsNaN(y[1]));
        Assert.Equal(2.0, y[2], 1e-9);
        Assert.Equal(3.0, y[3], 1e-9);
    }

    [Fact]
    public void IsWarmedUp_FlipsAtPeriodPlusOne()
    {
        var atr = new StreamingAtr(period: 2);
        atr.AppendCandle(new OhlcBar(10, 11, 9, 10));
        Assert.False(atr.IsWarmedUp);
        atr.AppendCandle(new OhlcBar(10, 12, 9, 11));
        Assert.False(atr.IsWarmedUp);
        atr.AppendCandle(new OhlcBar(11, 13, 10, 12));
        Assert.True(atr.IsWarmedUp);
    }

    [Fact]
    public void TrueRange_RespectsGapHigh_AbovePrevClose()
    {
        // period=2; second bar has a gap up: H above prev-close ⇒ TR = max(range, |H-prevClose|).
        // bar1 (H=10,L=8,C=9):   no TR (nothing before it).
        // bar2 (H=20,L=18,C=19): TR = max(20-18, |20-9|, |18-9|) = max(2,11,9) = 11. trSum=11.
        // bar3 (H=20,L=18,C=19): TR = max(20-18, |20-19|, |18-19|) = 2. trSum=13.
        //   ATR = 13 / 2 = 6.5.
        var atr = new StreamingAtr(period: 2);
        atr.AppendCandle(new OhlcBar(8,  10, 8, 9));
        atr.AppendCandle(new OhlcBar(18, 20, 18, 19));
        atr.AppendCandle(new OhlcBar(18, 20, 18, 19));

        Assert.Equal(6.5, atr.OutputSeries[0].CreateSnapshot().YData[2], 1e-9);
    }

    [Fact]
    public void FlatBars_AtrEqualsZero_AfterWarmup()
    {
        // High==Low for every bar ⇒ TR = 0 throughout ⇒ ATR = 0.
        var atr = new StreamingAtr(period: 2);
        foreach (var bar in StreamingTestData.FlatBars(5, price: 100))
            atr.AppendCandle(bar);

        var y = atr.OutputSeries[0].CreateSnapshot().YData;
        Assert.True(double.IsNaN(y[0]));
        Assert.True(double.IsNaN(y[1]));
        Assert.Equal(0.0, y[2]);
        Assert.Equal(0.0, y[3]);
        Assert.Equal(0.0, y[4]);
    }

}
