// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Data;
using MatPlotLibNet.Indicators.Streaming;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series.Streaming;

namespace MatPlotLibNet.Tests.Indicators.Streaming;

public sealed class StreamingIntegrationTests
{
    [Fact]
    public void AttachSmaToCandles_AutoAppends()
    {
        var axes = new Axes();
        var candles = axes.AddSeries(new StreamingCandlestickSeries(100));
        var sma = candles.WithStreamingSma(axes, 3);

        candles.AppendBar(100, 110, 95, 105);
        candles.AppendBar(105, 115, 100, 110);
        candles.AppendBar(110, 120, 105, 115);

        Assert.Equal(3, sma.ProcessedCount);
        Assert.False(double.IsNaN(sma.GetLatest()));
        Assert.Equal(110.0, sma.GetLatest(), 6); // avg(105, 110, 115)
    }

    [Fact]
    public void AttachBollingerToCandles_Creates3OutputSeries()
    {
        var axes = new Axes();
        var candles = axes.AddSeries(new StreamingCandlestickSeries(100));
        var bb = candles.WithStreamingBollinger(axes, 5, 2.0);

        // 3 band series added to axes
        Assert.Equal(4, axes.Series.Count); // candles + 3 bands
    }

    [Fact]
    public void MultipleIndicators_AllUpdateOnAppend()
    {
        var axes = new Axes();
        var candles = axes.AddSeries(new StreamingCandlestickSeries(100));
        var sma = candles.WithStreamingSma(axes, 3);
        var ema = candles.WithStreamingEma(axes, 3);

        for (int i = 0; i < 10; i++)
            candles.AppendBar(100 + i, 110 + i, 95 + i, 105 + i);

        Assert.Equal(10, sma.ProcessedCount);
        Assert.Equal(10, ema.ProcessedCount);
        Assert.False(double.IsNaN(sma.GetLatest()));
        Assert.False(double.IsNaN(ema.GetLatest()));
    }

    [Fact]
    public void AttachAtrToCandles_ComputesOnOhlc()
    {
        var axes = new Axes();
        var candles = axes.AddSeries(new StreamingCandlestickSeries(100));
        var atr = candles.WithStreamingAtr(axes, 3);

        for (int i = 0; i < 10; i++)
            candles.AppendBar(100 + i, 112 + i, 95 + i, 105 + i);

        Assert.Equal(10, atr.ProcessedCount);
        Assert.True(atr.GetLatest() > 0);
    }

    [Fact]
    public void AttachStochasticToCandles_Produces2Series()
    {
        var axes = new Axes();
        var candles = axes.AddSeries(new StreamingCandlestickSeries(100));
        var stoch = candles.WithStreamingStochastic(axes, 5, 3);

        // 2 output series (%K, %D) + candles
        Assert.Equal(3, axes.Series.Count);
    }

    [Fact]
    public void WarmupNaN_NotPlottedAsValidData()
    {
        var axes = new Axes();
        var candles = axes.AddSeries(new StreamingCandlestickSeries(100));
        var sma = candles.WithStreamingSma(axes, 5);

        // Append only 3 bars (warmup = 5)
        for (int i = 0; i < 3; i++)
            candles.AppendBar(100, 110, 95, 105);

        var snap = sma.OutputSeries[0].CreateSnapshot();
        Assert.Equal(3, snap.YData.Length);
        Assert.True(double.IsNaN(snap.YData[0]));
        Assert.True(double.IsNaN(snap.YData[1]));
        Assert.True(double.IsNaN(snap.YData[2]));
    }

    [Fact]
    public void FullPipeline_AxesToIndicatorToDataVersion()
    {
        var figure = new Figure();
        var axes = figure.AddSubPlot();
        var candles = axes.AddSeries(new StreamingCandlestickSeries(1000));
        candles.WithStreamingSma(axes, 5);
        candles.WithStreamingBollinger(axes, 10, 2.0);
        var sf = new MatPlotLibNet.Models.Streaming.StreamingFigure(figure);

        var rng = new Random(42);
        double price = 100;
        for (int i = 0; i < 50; i++)
        {
            double change = (rng.NextDouble() - 0.48) * 3;
            candles.AppendBar(new OhlcBar(price, price + 5, price - 3, price + change));
            price += change;
        }

        Assert.Equal(50, candles.Count);
        Assert.True(sf.DataVersion > 0);
        sf.Dispose();
    }
}
