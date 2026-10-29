// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Indicators.Streaming;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>The volume-weighted average price anchored at a bar: the cumulative mean of the typical price, (high + low + close) / 3,
/// weighted by volume, summed from the anchor bar on instead of from the first (an anchored VWAP, as trading platforms draw one
/// from a swing, a gap or an earnings day). Before the anchor there is no value (NaN); at the anchor it is that bar's own typical
/// price (or, with no volume, the price); anchor 0 is the cumulative VWAP the indicator always was.</summary>
public class AnchoredVwapTests
{
    /// <summary>Bars whose high, low and close are one number, so the typical price is that number exactly: (3p) / 3 = p for the
    /// whole numbers here.</summary>
    private static OhlcvBars Flat(double[] prices, double[] volumes) => OhlcvBars.Hlcv(prices, prices, prices, volumes);

    private static readonly double[] Prices = [10, 20, 30, 40];

    private static readonly double[] Volumes = [1, 1, 2, 2];

    // ---- the anchored value -----------------------------------------------------------------------------------------------

    /// <summary>Anchored at bar 1: no value at bar 0; 20 at the anchor; (20 + 60) / 3 = 26.666666666666668 after two bars; and
    /// (20 + 60 + 80) / 5 = 32 after three. The first bar, outside the anchor, does not count.</summary>
    [Fact]
    public void AnchoredAtOne_SumsFromTheAnchor_AndIsNaNBeforeIt()
    {
        double[] vwap = new Vwap(Flat(Prices, Volumes), anchor: 1).Compute().Values;

        Assert.True(double.IsNaN(vwap[0]));
        Assert.Equal([20.0, 26.666666666666668, 32.0], vwap[1..]);
    }

    /// <summary>Anchored at the first bar the indicator is the cumulative VWAP it always was, bit for bit, over typical prices that
    /// are not round numbers: the same as the constructor that takes the prices and the volumes.</summary>
    [Fact]
    public void AnchoredAtZero_IsTheCumulativeVwap_BitForBit()
    {
        var random = new Random(20261029);
        double[] high = [.. Enumerable.Range(0, 150).Select(_ => 101 + random.NextDouble() * 3)];
        double[] low = [.. high.Select(h => h - random.NextDouble() * 2)];
        double[] close = [.. high.Select((h, i) => low[i] + random.NextDouble() * (h - low[i]))];
        double[] volume = [.. Enumerable.Range(0, 150).Select(i => i % 13 == 5 ? 0 : 100 + random.Next(2000))];
        double[] typical = [.. high.Select((h, i) => (h + low[i] + close[i]) / 3.0)];

        double[] old = new Vwap(typical, volume).Compute().Values;
        double[] anchored = new Vwap(OhlcvBars.Hlcv(high, low, close, volume)).Compute().Values;
        double[] explicitZero = new Vwap(OhlcvBars.Hlcv(high, low, close, volume), 0).Compute().Values;

        Assert.Equal(old, anchored);
        Assert.Equal(old, explicitZero);
    }

    /// <summary>The anchor bar with no volume reads its own price, and a bar after it that has volume weighs in from there.</summary>
    [Fact]
    public void AZeroVolumeAtTheAnchor_GivesThePrice()
    {
        double[] vwap = new Vwap(Flat([10, 20, 30, 40], [5, 0, 3, 1]), anchor: 1).Compute().Values;

        Assert.True(double.IsNaN(vwap[0]));
        Assert.Equal(20.0, vwap[1]);
        Assert.Equal(30.0, vwap[2]);
        Assert.Equal((90.0 + 40) / 4, vwap[3]);
    }

    // ---- the anchor must be a bar ------------------------------------------------------------------------------------------

    /// <summary>An anchor below 0, or at or past the last bar, is no bar: refused, naming the parameter.</summary>
    [Theory]
    [InlineData(-1)]
    [InlineData(4)]
    [InlineData(9)]
    public void AnAnchorThatIsNoBar_IsRefused(int anchor)
    {
        var error = Assert.Throws<ArgumentOutOfRangeException>(() => new Vwap(Flat(Prices, Volumes), anchor));

        Assert.Equal("anchor", error.ParamName);
    }

    [Fact]
    public void NoBars_HaveNoAnchor()
    {
        var error = Assert.Throws<ArgumentOutOfRangeException>(() => new Vwap(Flat([], []), 0));

        Assert.Equal("anchor", error.ParamName);
    }

    /// <summary>The last bar is an anchor like any other: one value, its own price.</summary>
    [Fact]
    public void TheLastBar_CanBeTheAnchor()
    {
        double[] vwap = new Vwap(Flat(Prices, Volumes), anchor: 3).Compute().Values;

        Assert.Equal(3, vwap.Count(double.IsNaN));
        Assert.Equal(40.0, vwap[3]);
    }

    // ---- bars that do not pair up ------------------------------------------------------------------------------------------

    /// <summary>Prices and volumes of different lengths are refused instead of silently cut to the shorter (what the constructor
    /// that takes the arrays used to do, and what every other indicator in the library has refused since 1.24.0).</summary>
    [Fact]
    public void PricesAndVolumesThatDoNotPairUp_AreRefused()
    {
        var error = Assert.Throws<ArgumentException>(() => new Vwap([10, 20, 30], [1, 1]));

        Assert.Equal("Array lengths must match. Got 3 and 2.", error.Message);
    }

    /// <summary>Bars made by hand with a volume of another length are refused too, whichever array is the odd one.</summary>
    [Fact]
    public void BarsWhoseArraysDoNotPairUp_AreRefused()
    {
        double[] three = [1, 2, 3], two = [1, 2];

        Assert.Throws<ArgumentException>(() => new Vwap(new OhlcvBars(three, three, three, [], two)));
        Assert.Throws<ArgumentException>(() => new Vwap(new OhlcvBars(three, two, three, [], three)));
        Assert.Throws<ArgumentException>(() => new Vwap(new OhlcvBars(three, three, two, [], three)));
    }

    // ---- the streaming form ------------------------------------------------------------------------------------------------

    /// <summary>Anchoring a stream at a bar starts a new VWAP there: from the anchor on, bar for bar, it gives the batch form
    /// anchored at the same bar. The values before it stay as they were (a stream does not take back what it has said).</summary>
    [Fact]
    public void AnchorHere_StartsANewVwap_ThatMatchesTheBatchFormFromThere()
    {
        double[] prices = [10, 11, 13, 12, 15, 16, 14];
        double[] volumes = [4, 2, 3, 0, 5, 1, 2];
        double[] batch = new Vwap(Flat(prices, volumes), anchor: 3).Compute().Values;
        var streaming = new StreamingVwap();

        for (int i = 0; i < prices.Length; i++)
        {
            if (i == 3) streaming.AnchorHere();
            streaming.AppendWithVolume(prices[i], volumes[i]);
            if (i >= 3) Assert.Equal(batch[i], streaming.GetLatest());
        }

        Assert.Equal(prices.Length, streaming.ProcessedCount);
    }

    [Fact]
    public void WithoutAnchorHere_TheStreamIsTheCumulativeVwap()
    {
        double[] batch = new Vwap(Flat(Prices, Volumes)).Compute().Values;
        var streaming = new StreamingVwap();

        for (int i = 0; i < Prices.Length; i++)
        {
            streaming.AppendWithVolume(Prices[i], Volumes[i]);
            Assert.Equal(batch[i], streaming.GetLatest());
        }
    }

    // ---- the shortcut on the axes ------------------------------------------------------------------------------------------

    /// <summary>The line starts at the anchor: it is drawn from the x position of the anchor bar to the last bar, with no
    /// stretch of NaN before it, so the axes do not make room for bars the VWAP says nothing about.</summary>
    [Fact]
    public void TheShortcut_DrawsTheLineFromTheAnchorBar()
    {
        var axes = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Vwap(Flat(Prices, Volumes), anchor: 1)).Build().SubPlots[0];

        var line = Assert.IsType<LineSeries>(Assert.Single(axes.Series));
        Assert.Equal([1.0, 2.0, 3.0], line.XData);
        Assert.Equal([20.0, 26.666666666666668, 32.0], line.YData);
        Assert.Equal("VWAP", line.Label);
    }

    /// <summary>Without an anchor it is drawn from the first bar, as the indicator is drawn through <c>Indicator</c>.</summary>
    [Fact]
    public void TheShortcut_WithoutAnAnchor_DrawsFromTheFirstBar_LikeTheIndicator()
    {
        var byShortcut = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Vwap(Flat(Prices, Volumes))).Build().SubPlots[0];
        var byIndicator = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Indicator(new Vwap(Flat(Prices, Volumes)))).Build().SubPlots[0];

        var shortcut = Assert.IsType<LineSeries>(Assert.Single(byShortcut.Series));
        var indicator = Assert.IsType<LineSeries>(Assert.Single(byIndicator.Series));
        Assert.Equal(indicator.XData, shortcut.XData);
        Assert.Equal(indicator.YData, shortcut.YData);
    }

    [Fact]
    public void TheShortcut_RunsItsCallbackOnTheIndicator_Once()
    {
        int calls = 0;

        var axes = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Vwap(Flat(Prices, Volumes), 2, vwap => { calls++; vwap.Label = "from the gap"; })).Build().SubPlots[0];

        Assert.Equal(1, calls);
        Assert.Equal("from the gap", Assert.Single(axes.Series).Label);
    }
}
