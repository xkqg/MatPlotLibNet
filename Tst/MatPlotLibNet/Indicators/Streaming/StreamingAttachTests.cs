// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Data;
using MatPlotLibNet.Indicators.Streaming;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series.Streaming;

namespace MatPlotLibNet.Tests.Indicators.Streaming;

/// <summary><c>WithStreaming</c> is the one way an indicator is attached to a candle series: the indicator is fed every bar the
/// series receives, with the volume the bar came with, and its outputs are added to the axes. The named verbs
/// (<c>WithStreamingSma</c>, …) are that call with the indicator made for the reader.</summary>
public sealed class StreamingAttachTests
{
    /// <summary>An indicator that writes down what it is given.</summary>
    private sealed class Recorder : StreamingIndicator<OhlcvBar>
    {
        public Recorder() : base([new StreamingLineSeries(16), new StreamingLineSeries(16)]) { }
        public override int WarmupPeriod => 1;
        public List<OhlcvBar> Seen { get; } = [];

        protected override void Step(OhlcvBar input, int index, Span<double> outputs)
        {
            Seen.Add(input);
            outputs[0] = input.Close;
            outputs[1] = input.Volume;
        }
    }

    private static (StreamingCandlestickSeries candles, Axes axes) Make()
    {
        var candles = new StreamingCandlestickSeries(100);
        return (candles, new Figure().AddSubPlot());
    }

    private static double[] Values(IStreamingIndicator ind, int output = 0) => ind.OutputSeries[output].CreateSnapshot().YData;

    [Fact]
    public void WithStreaming_FeedsTheIndicatorEveryBar_WithItsVolume()
    {
        var (candles, axes) = Make();
        var recorder = candles.WithStreaming(axes, new Recorder());

        candles.AppendBar(new OhlcvBar(10, 12, 9, 11, 500));
        candles.AppendBar(new OhlcvBar(11, 13, 10, 12, 700));

        Assert.Equal([new OhlcvBar(10, 12, 9, 11, 500), new OhlcvBar(11, 13, 10, 12, 700)], recorder.Seen);
    }

    [Fact]
    public void WithStreaming_ABarThatComesWithoutAVolume_ReachesTheIndicatorWithNaN()
    {
        var (candles, axes) = Make();
        var recorder = candles.WithStreaming(axes, new Recorder());

        candles.AppendBar(10, 12, 9, 11);

        var seen = Assert.Single(recorder.Seen);
        Assert.Equal(11.0, seen.Close);
        Assert.True(double.IsNaN(seen.Volume));
    }

    [Fact]
    public void WithStreaming_AddsEveryOutputOfTheIndicatorToTheAxes_AndReturnsTheIndicator()
    {
        var (candles, axes) = Make();
        var given = new Recorder();

        Recorder returned = candles.WithStreaming(axes, given);

        Assert.Same(given, returned);
        Assert.Equal(2, axes.Series.Count);
        Assert.Contains(given.OutputSeries[0], axes.Series);
        Assert.Contains(given.OutputSeries[1], axes.Series);
    }

    [Fact]
    public void WithStreaming_AnIndicatorOfPrices_IsFedTheClose()
    {
        var (candles, axes) = Make();
        var sma = candles.WithStreaming(axes, new StreamingSma(2));

        candles.AppendBar(new OhlcvBar(1, 9, 0, 4, 100));
        candles.AppendBar(new OhlcvBar(1, 9, 0, 6, 100));

        Assert.Equal(5.0, sma.GetLatest());
    }

    [Fact]
    public void WithStreaming_AnIndicatorOfCandles_IsFedTheWholeCandle()
    {
        var (candles, axes) = Make();
        var williams = candles.WithStreaming(axes, new StreamingWilliamsR(2));

        candles.AppendBar(new OhlcvBar(5, 10, 0, 5, 1));
        candles.AppendBar(new OhlcvBar(5, 10, 0, 2.5, 1));

        // Williams %R over the two bars: highest high 10, lowest low 0, close 2.5 -> (10 - 2.5) / 10 * -100.
        Assert.Equal(-75.0, williams.GetLatest(), 9);
    }

    [Fact]
    public void WithStreamingObv_AddsAndSubtractsTheVolumeOfEachBar()
    {
        var (candles, axes) = Make();
        var obv = candles.WithStreamingObv(axes);

        foreach (var (close, volume) in new (double, double)[] { (100, 50), (102, 30), (101, 20), (101, 40), (105, 25) })
            candles.AppendBar(new OhlcvBar(close, close, close, close, volume));

        // The first bar opens the sum with its own volume; then up adds, down subtracts, equal keeps.
        Assert.Equal([50.0, 80.0, 60.0, 60.0, 85.0], Values(obv));
    }

    /// <summary>A bar that comes without a volume cannot move the sum: the batch OBV over a volume that is not a number gives
    /// NaN from that bar on, and so does this, because it is that arithmetic.</summary>
    [Fact]
    public void WithStreamingObv_ABarWithoutAVolume_GivesNaN_NotAGuess()
    {
        var (candles, axes) = Make();
        var obv = candles.WithStreamingObv(axes);

        candles.AppendBar(new OhlcvBar(100, 100, 100, 100, 50));
        candles.AppendBar(100, 103, 99, 102);                      // rising, no volume
        candles.AppendBar(new OhlcvBar(102, 105, 101, 104, 10));   // rising, with volume: the sum stays lost

        var values = Values(obv);
        Assert.Equal(50.0, values[0]);
        Assert.True(double.IsNaN(values[1]));
        Assert.True(double.IsNaN(values[2]));
    }

    [Fact]
    public void WithStreamingVwap_WeighsTheTypicalPriceOfEachBarByItsVolume()
    {
        var (candles, axes) = Make();
        var vwap = candles.WithStreamingVwap(axes);

        candles.AppendBar(new OhlcvBar(10, 12, 8, 10, 10));    // typical price (12 + 8 + 10) / 3 = 10
        candles.AppendBar(new OhlcvBar(20, 24, 16, 20, 30));   // typical price (24 + 16 + 20) / 3 = 20

        Assert.Equal([10.0, 17.5], Values(vwap));              // (10 * 10 + 20 * 30) / 40
    }

    [Fact]
    public void WithStreamingVwap_ABarWithoutAVolume_IsTheTypicalPriceItself()
    {
        var (candles, axes) = Make();
        var vwap = candles.WithStreamingVwap(axes);

        candles.AppendBar(10, 12, 8, 10);

        Assert.Equal([10.0], Values(vwap));
    }

    [Fact]
    public void TheNamedVerbs_AreTheGenericAttach_WithTheIndicatorMadeForTheReader()
    {
        var (candles, axes) = Make();
        var named = candles.WithStreamingRsi(axes, period: 3);
        var generic = candles.WithStreaming(axes, new StreamingRsi(3, candles.Capacity));

        for (int i = 0; i < 10; i++) candles.AppendBar(new OhlcvBar(i, i + 2, i - 1, i % 3 + 5 * i, 100));

        Assert.Equal(Values(named), Values(generic));
        Assert.Equal(2, axes.Series.Count);   // one RSI line each
    }
}
