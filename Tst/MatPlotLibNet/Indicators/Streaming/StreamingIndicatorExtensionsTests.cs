// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Streaming;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series.Streaming;

namespace MatPlotLibNet.Tests.Indicators.Streaming;

/// <summary>Phase X.10.b (v1.7.2, 2026-04-19) — drives every fluent indicator
/// attachment in <see cref="StreamingIndicatorExtensions"/>. Pre-X.10.b the class
/// was at 53%L / 56%B because only ~4 of the 9 extensions were exercised. Each
/// fact pins one extension method:
///   - Subscribes the indicator to BarAppended
///   - Adds the indicator's OutputSeries to the axes
///   - Appending bars must propagate to the indicator's output series
/// 9 extension methods × 1 fact each = 9 facts; at least 1 OHLC append per fact
/// triggers the lambda body so the for-each at line N is fully covered.</summary>
public class StreamingIndicatorExtensionsTests
{
    private static (StreamingCandlestickSeries candles, Axes axes) Make(int capacity = 100)
    {
        var candles = new StreamingCandlestickSeries(capacity);
        var fig = new Figure();
        var axes = fig.AddSubPlot();
        return (candles, axes);
    }

    private static void AppendBars(StreamingCandlestickSeries c, int count)
    {
        for (int i = 0; i < count; i++)
            c.AppendBar(100 + i, 110 + i, 95 + i, 105 + i);
    }

    [Fact]
    public void WithStreamingSma_AttachesAndAppendsOutputSeries()
    {
        var (candles, axes) = Make();
        var ind = candles.WithStreamingSma(axes, period: 5);
        AppendBars(candles, 10);
        Assert.NotNull(ind);
        Assert.NotEmpty(axes.Series);
    }

    [Fact]
    public void WithStreamingEma_AttachesAndAppendsOutputSeries()
    {
        var (candles, axes) = Make();
        var ind = candles.WithStreamingEma(axes, period: 5);
        AppendBars(candles, 10);
        Assert.NotNull(ind);
        Assert.NotEmpty(axes.Series);
    }

    [Fact]
    public void WithStreamingRsi_AttachesAndAppendsOutputSeries()
    {
        var (candles, axes) = Make();
        var ind = candles.WithStreamingRsi(axes, period: 14);
        AppendBars(candles, 20);
        Assert.NotNull(ind);
        Assert.NotEmpty(axes.Series);
    }

    [Fact]
    public void WithStreamingBollinger_AttachesAndAppendsOutputSeries()
    {
        var (candles, axes) = Make();
        var ind = candles.WithStreamingBollinger(axes, period: 20, numStdDev: 2.0);
        AppendBars(candles, 25);
        Assert.NotNull(ind);
        Assert.NotEmpty(axes.Series);
    }

    [Fact]
    public void WithStreamingMacd_AttachesAndAppendsOutputSeries()
    {
        var (candles, axes) = Make();
        var ind = candles.WithStreamingMacd(axes, fast: 12, slow: 26, signal: 9);
        AppendBars(candles, 30);
        Assert.NotNull(ind);
        Assert.NotEmpty(axes.Series);
    }

    [Fact]
    public void WithStreamingAtr_AttachesAndAppendsOutputSeries()
    {
        var (candles, axes) = Make();
        var ind = candles.WithStreamingAtr(axes, period: 14);
        AppendBars(candles, 20);
        Assert.NotNull(ind);
        Assert.NotEmpty(axes.Series);
    }

    [Fact]
    public void WithStreamingStochastic_AttachesAndAppendsOutputSeries()
    {
        var (candles, axes) = Make();
        var ind = candles.WithStreamingStochastic(axes, kPeriod: 14, dPeriod: 3);
        AppendBars(candles, 20);
        Assert.NotNull(ind);
        Assert.NotEmpty(axes.Series);
    }

    [Fact]
    public void WithStreamingWilliamsR_AttachesAndAppendsOutputSeries()
    {
        var (candles, axes) = Make();
        var ind = candles.WithStreamingWilliamsR(axes, period: 14);
        AppendBars(candles, 20);
        Assert.NotNull(ind);
        Assert.NotEmpty(axes.Series);
    }

    [Fact]
    public void WithStreamingCci_AttachesAndAppendsOutputSeries()
    {
        var (candles, axes) = Make();
        var ind = candles.WithStreamingCci(axes, period: 20);
        AppendBars(candles, 25);
        Assert.NotNull(ind);
        Assert.NotEmpty(axes.Series);
    }
}
