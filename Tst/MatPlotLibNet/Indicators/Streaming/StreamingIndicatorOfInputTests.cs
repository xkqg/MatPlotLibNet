// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Data;
using MatPlotLibNet.Indicators.Streaming;
using MatPlotLibNet.Models.Series.Streaming;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Indicators.Streaming;

/// <summary><see cref="StreamingIndicator{TInput}"/> is the one root of a streaming indicator whatever it is fed: a bar, a price
/// or a row of several series. It owns what every one of them shares: one point per input on every output at the same x, no
/// output ahead of another, and the way a bar or a price reaches an input.</summary>
public sealed class StreamingIndicatorOfInputTests
{
    /// <summary>Two outputs from a bar: the range and the volume.</summary>
    private sealed class RangeAndVolume : StreamingIndicator<OhlcvBar>
    {
        public RangeAndVolume() : base([new StreamingLineSeries(16), new StreamingLineSeries(16)]) { }
        public override int WarmupPeriod => 2;
        public bool Throw { get; set; }
        public List<int> Indexes { get; } = [];

        protected override void Step(OhlcvBar input, int index, Span<double> outputs)
        {
            Indexes.Add(index);
            if (Throw) throw new InvalidOperationException("no value for this bar");
            outputs[0] = input.High - input.Low;
            // outputs[1] is left alone on the first bar: a value not set is NaN, not whatever the last bar left there.
            if (index > 0) outputs[1] = input.Volume;
        }
    }

    /// <summary>One output from a price.</summary>
    private sealed class Doubled : StreamingIndicator<double>
    {
        public Doubled() : base(16) { }
        public override int WarmupPeriod => 1;
        protected override void Step(double input, int index, Span<double> outputs) => outputs[0] = input * 2;
    }

    /// <summary>One output from a row of several series.</summary>
    private sealed class RowSum : StreamingIndicator<double[]>
    {
        public RowSum() : base(16) { }
        public override int WarmupPeriod => 1;
        protected override void Step(double[] input, int index, Span<double> outputs) => outputs[0] = input.Sum();
    }

    private static double[] Values(StreamingLineSeries s) => s.CreateSnapshot().YData;
    private static double[] Xs(StreamingLineSeries s) => s.CreateSnapshot().XData;

    [Fact]
    public void EveryInput_AddsOnePointToEveryOutput_AtTheSameX()
    {
        var ind = new RangeAndVolume();

        ind.Append(new OhlcvBar(10, 14, 9, 12, 100));
        ind.Append(new OhlcvBar(12, 15, 11, 14, 200));
        ind.Append(new OhlcvBar(14, 20, 13, 19, 300));

        Assert.Equal([5.0, 4.0, 7.0], Values(ind.OutputSeries[0]));
        Assert.Equal([0.0, 1.0, 2.0], Xs(ind.OutputSeries[0]));
        Assert.Equal(Xs(ind.OutputSeries[0]), Xs(ind.OutputSeries[1]));
        Assert.Equal(3, ind.ProcessedCount);
    }

    [Fact]
    public void AnOutputThatIsNotSet_IsNaN_NotWhatTheLastBarLeft()
    {
        var ind = new RangeAndVolume();

        ind.Append(new OhlcvBar(10, 14, 9, 12, 100));
        ind.Append(new OhlcvBar(12, 15, 11, 14, 200));
        ind.Append(new OhlcvBar(14, 20, 13, 19, double.NaN));

        var volume = Values(ind.OutputSeries[1]);
        Assert.True(double.IsNaN(volume[0]));   // index 0: left alone
        Assert.Equal(200.0, volume[1]);
        Assert.True(double.IsNaN(volume[2]));   // a bar with no volume writes NaN, not the 200 before it
    }

    [Fact]
    public void TheStepIsToldTheIndexOfTheInputItIsGiven()
    {
        var ind = new RangeAndVolume();

        for (int i = 0; i < 4; i++) ind.Append(new OhlcvBar(1, 2, 0, 1, 1));

        Assert.Equal([0, 1, 2, 3], ind.Indexes);
    }

    /// <summary>The Bollinger form wrote its upper and lower band before the middle band could fail, so a bar that threw left
    /// three series of different lengths. All the outputs are worked out first; none is touched when the step throws.</summary>
    [Fact]
    public void AStepThatThrows_LeavesEveryOutputAndTheCountAsTheyWere()
    {
        var ind = new RangeAndVolume();
        ind.Append(new OhlcvBar(10, 14, 9, 12, 100));
        ind.Throw = true;

        Assert.Throws<InvalidOperationException>(() => ind.Append(new OhlcvBar(12, 15, 11, 14, 200)));

        Assert.Equal(1, ind.ProcessedCount);
        Assert.Single(Values(ind.OutputSeries[0]));
        Assert.Single(Values(ind.OutputSeries[1]));
        ind.Throw = false;
        ind.Append(new OhlcvBar(12, 15, 11, 14, 200));
        Assert.Equal([0.0, 1.0], Xs(ind.OutputSeries[1]));   // the bar after the failure takes the next place, no gap
    }

    [Fact]
    public void TheWarmUp_IsTheCountOfInputsBeforeItIsWarm()
    {
        var ind = new RangeAndVolume();
        Assert.False(ind.IsWarmedUp);

        ind.Append(new OhlcvBar(1, 2, 0, 1, 1));
        Assert.False(ind.IsWarmedUp);

        ind.Append(new OhlcvBar(1, 2, 0, 1, 1));
        Assert.True(ind.IsWarmedUp);
    }

    [Fact]
    public void GetLatest_IsNaNBeforeTheFirstInput_ThenTheFirstOutputOfTheLastOne()
    {
        var ind = new RangeAndVolume();
        Assert.True(double.IsNaN(ind.GetLatest()));

        ind.Append(new OhlcvBar(10, 14, 9, 12, 100));

        Assert.Equal(5.0, ind.GetLatest());
    }

    [Fact]
    public void TheStyleOfTheIndicator_IsTheStyleOfItsFirstOutput()
    {
        var ind = new RangeAndVolume { Label = "range", Color = Colors.Red, LineWidth = 3 };

        Assert.Equal("range", ind.OutputSeries[0].Label);
        Assert.Equal(Colors.Red, ind.OutputSeries[0].Color);
        Assert.Equal(3, ind.OutputSeries[0].LineWidth);
        Assert.Equal("range", ind.Label);
        Assert.Equal(Colors.Red, ind.Color);
        Assert.Equal(3, ind.LineWidth);
    }

    [Fact]
    public void ACapacity_GivesOneOutputOfThatCapacity()
    {
        var ind = new Doubled();

        Assert.Single(ind.OutputSeries);
        Assert.Equal(16, ind.OutputSeries[0].Capacity);
    }

    [Fact]
    public void ABarReachesABarIndicatorWhole_VolumeIncluded()
    {
        var ind = new RangeAndVolume();
        ind.AppendBar(new OhlcvBar(1, 2, 0, 1, 7));
        ind.AppendBar(new OhlcvBar(1, 2, 0, 1, 8));

        Assert.Equal(8.0, Values(ind.OutputSeries[1])[1]);
    }

    [Fact]
    public void ACandleReachesABarIndicatorWithoutAVolume()
    {
        var ind = new RangeAndVolume();
        ind.AppendCandle(new OhlcBar(1, 2, 0, 1));
        ind.AppendCandle(new OhlcBar(1, 2, 0, 1));

        Assert.True(double.IsNaN(Values(ind.OutputSeries[1])[1]));
    }

    [Fact]
    public void APriceReachesABarIndicatorAsABarOfThatPriceAndNoVolume()
    {
        var ind = new RangeAndVolume();
        ((IStreamingIndicator)ind).Append(42.0);
        ((IStreamingIndicator)ind).Append(43.0);

        Assert.Equal([0.0, 0.0], Values(ind.OutputSeries[0]));   // open = high = low = close = the price
        Assert.True(double.IsNaN(Values(ind.OutputSeries[1])[1]));
    }

    [Fact]
    public void ABarReachesAPriceIndicatorAsItsClose_ByEveryRoute()
    {
        var ind = new Doubled();
        ind.AppendBar(new OhlcvBar(1, 9, 0, 4, 100));
        ind.AppendCandle(new OhlcBar(1, 9, 0, 5));
        ((IStreamingIndicator)ind).Append(6.0);
        ind.Append(7.0);

        Assert.Equal([8.0, 10.0, 12.0, 14.0], Values(ind.OutputSeries[0]));
    }

    [Fact]
    public void ABarCannotBeAppendedToAnIndicatorOfRows_AndTheMessageSaysWhy()
    {
        var ind = new RowSum();

        var ex = Assert.Throws<NotSupportedException>(() => ind.AppendBar(new OhlcvBar(1, 2, 0, 1, 1)));

        Assert.Contains(nameof(RowSum), ex.Message);
        Assert.Contains("Double[]", ex.Message);
        Assert.Equal(0, ind.ProcessedCount);
    }

    [Fact]
    public void ARowReachesAnIndicatorOfRows()
    {
        var ind = new RowSum();
        ind.Append([1.0, 2.0, 3.0]);
        ind.Append([4.0, 5.0]);

        Assert.Equal([6.0, 9.0], Values(ind.OutputSeries[0]));
    }
}
