// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Streaming;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series.Streaming;

namespace MatPlotLibNet.Tests.Indicators.Streaming;

/// <summary>Each fluent attachment of <see cref="StreamingIndicatorExtensions"/> subscribes its indicator to the candles, adds the
/// indicator's output to the axes, and keeps it fed as bars arrive.</summary>
public class StreamingIndicatorExtensionsTests
{
    public static TheoryData<string, Func<StreamingCandlestickSeries, Axes, object>, int> Attachments => new()
    {
        { "sma", (candles, axes) => candles.WithStreamingSma(axes, period: 5), 10 },
        { "ema", (candles, axes) => candles.WithStreamingEma(axes, period: 5), 10 },
        { "rsi", (candles, axes) => candles.WithStreamingRsi(axes, period: 14), 20 },
        { "bollinger", (candles, axes) => candles.WithStreamingBollinger(axes, period: 20, numStdDev: 2.0), 25 },
        { "macd", (candles, axes) => candles.WithStreamingMacd(axes, fast: 12, slow: 26, signal: 9), 30 },
        { "atr", (candles, axes) => candles.WithStreamingAtr(axes, period: 14), 20 },
        { "stochastic", (candles, axes) => candles.WithStreamingStochastic(axes, kPeriod: 14, dPeriod: 3), 20 },
        { "williams %r", (candles, axes) => candles.WithStreamingWilliamsR(axes, period: 14), 20 },
        { "cci", (candles, axes) => candles.WithStreamingCci(axes, period: 20), 25 },
    };

    [Theory]
    [MemberData(nameof(Attachments))]
    public void AnAttachedIndicator_AddsItsOutputToTheAxes_AndIsFedAsBarsArrive(string indicator,
        Func<StreamingCandlestickSeries, Axes, object> attach, int bars)
    {
        var candles = new StreamingCandlestickSeries(100);
        var axes = new Figure().AddSubPlot();

        Assert.NotNull(attach(candles, axes));
        for (int i = 0; i < bars; i++)
            candles.AppendBar(100 + i, 110 + i, 95 + i, 105 + i);

        Assert.True(axes.Series.Count > 0, $"the {indicator} added no series to the axes");
    }
}
