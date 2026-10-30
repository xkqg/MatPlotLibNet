// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Data;
using MatPlotLibNet.Indicators;
using MatPlotLibNet.Indicators.Streaming;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series.Streaming;

namespace MatPlotLibNet.Tests.Indicators.Streaming;

/// <summary>A streaming form refuses what its batch indicator refuses, in the same words, so a chart that goes live does not
/// start accepting a period the batch form would have thrown on. Each case here is one argument that no form takes: the form is
/// not built, and the exception names the argument.</summary>
public sealed class StreamingFormsGuardTests
{
    public static TheoryData<string, Func<object>, string> BadArguments() => new()
    {
        { "AdaptiveStochastic smoothing", () => new StreamingAdaptiveStochastic(smoothingPeriod: 0), "smoothingPeriod" },
        { "Bocpd hazard low", () => new StreamingBocpd(hazard: 0), "hazard" },
        { "Bocpd hazard high", () => new StreamingBocpd(hazard: 1), "hazard" },
        { "Bocpd variance", () => new StreamingBocpd(priorVariance: 0), "priorVariance" },
        { "Bocpd run length", () => new StreamingBocpd(maxRunLength: 0), "maxRunLength" },
        { "Cusum threshold", () => new StreamingCusum(threshold: 0), "threshold" },
        { "CyberCycle alpha low", () => new StreamingCyberCycle(alpha: 0), "alpha" },
        { "CyberCycle alpha high", () => new StreamingCyberCycle(alpha: 1), "alpha" },
        { "Decycler period", () => new StreamingDecycler(hpPeriod: 3), "hpPeriod" },
        { "SuperSmoother period", () => new StreamingEhlersSuperSmoother(period: 1), "period" },
        { "ForceIndex period", () => new StreamingForceIndex(period: 0), "period" },
        { "Klinger fast", () => new StreamingKlingerVolumeOscillator(fastPeriod: 1), "fastPeriod" },
        { "Klinger slow", () => new StreamingKlingerVolumeOscillator(fastPeriod: 5, slowPeriod: 5), "slowPeriod" },
        { "Klinger signal", () => new StreamingKlingerVolumeOscillator(signalPeriod: 0), "signalPeriod" },
        { "LaguerreRsi alpha low", () => new StreamingLaguerreRsi(alpha: 0), "alpha" },
        { "LaguerreRsi alpha high", () => new StreamingLaguerreRsi(alpha: 1), "alpha" },
        { "MamaFama fast low", () => new StreamingMamaFama(fastLimit: 0), "fastLimit" },
        { "MamaFama fast high", () => new StreamingMamaFama(fastLimit: 1.5), "fastLimit" },
        { "MamaFama slow low", () => new StreamingMamaFama(slowLimit: 0), "slowLimit" },
        { "MamaFama slow above fast", () => new StreamingMamaFama(fastLimit: 0.3, slowLimit: 0.3), "slowLimit" },
        { "RoofingFilter high-pass", () => new StreamingRoofingFilter(hpPeriod: 3), "hpPeriod" },
        { "RoofingFilter low-pass", () => new StreamingRoofingFilter(lpPeriod: 1), "lpPeriod" },
        { "RoofingFilter low-pass above high-pass", () => new StreamingRoofingFilter(hpPeriod: 10, lpPeriod: 10), "lpPeriod" },
        { "Supertrend period", () => new StreamingSupertrend(period: 0), "period" },
        { "Supertrend multiplier", () => new StreamingSupertrend(multiplier: 0), "multiplier" },
        { "TurbulenceIndex window", () => new StreamingTurbulenceIndex(window: 1), "window" },
        { "TurbulenceIndex regularization", () => new StreamingTurbulenceIndex(regularization: -1), "regularization" },
        { "TwiggsMoneyFlow period", () => new StreamingTwiggsMoneyFlow(period: 1), "period" },
    };

    [Theory]
    [MemberData(nameof(BadArguments))]
    public void AnArgumentNoFormTakes_RefusesTheForm_AndNamesTheArgument(string label, Func<object> build, string argument)
    {
        var refused = Assert.Throws<ArgumentException>(build);

        Assert.True(refused.ParamName == argument, $"{label}: the exception names '{refused.ParamName}', not '{argument}'");
    }

    [Fact]
    public void ABarWhoseHighIsBelowItsLow_IsRefusedByTheAdaptiveStochastic()
    {
        var form = new StreamingAdaptiveStochastic();

        Assert.Throws<ArgumentException>(() => form.AppendBar(new OhlcvBar(10, 9, 11, 10, 100)));
    }

    [Fact]
    public void APriceAtOrBelowZero_IsRefusedByTheCusum()
    {
        var form = new StreamingCusum(threshold: 1);

        Assert.Throws<ArgumentException>(() => form.Append(0.0));
    }

    [Fact]
    public void ARowOfOneSignal_IsRefusedByTheDispersionIndex()
    {
        var form = new StreamingDispersionIndex();

        Assert.Throws<ArgumentException>(() => form.Append([1.0]));
    }

    [Fact]
    public void ARowOfAnotherLength_IsRefusedByTheDispersionIndex()
    {
        var form = new StreamingDispersionIndex();
        form.Append([1.0, 2.0, 3.0]);

        Assert.Throws<ArgumentException>(() => form.Append([1.0, 2.0]));
    }

    [Fact]
    public void ARowOfMoreFeaturesThanTheEstimatorTakes_IsRefusedByTheTurbulenceIndex()
    {
        // the estimator takes at most ten
        var form = new StreamingTurbulenceIndex(window: 5);

        Assert.Throws<ArgumentException>(() => form.Append(new double[11]));
    }

    [Fact]
    public void ARowOfAnotherLength_IsRefusedByTheTurbulenceIndex()
    {
        var form = new StreamingTurbulenceIndex(window: 5);
        form.Append([1.0, 2.0]);

        Assert.Throws<ArgumentException>(() => form.Append([1.0, 2.0, 3.0]));
    }
}

/// <summary>The two older bases of the streaming forms keep the members they had: a line width, a candle taken as its close, a
/// price a form that reads candles has no use for, and the old way of writing a second output.</summary>
public sealed class StreamingIndicatorOlderBaseTests
{
    [Fact]
    public void TheLineWidth_IsTheOneTheOutputLinesAreDrawnWith()
    {
        var form = new StreamingSma(3) { LineWidth = 2.5 };

        Assert.Equal(2.5, form.LineWidth);
        Assert.Equal(2.5, form.OutputSeries[0].LineWidth);
    }

    [Fact]
    public void APriceGivenToAFormThatReadsCandles_IsAnEmptyValueNotAGuess()
    {
        var form = new StreamingStochastic();

        form.Append(100.0);

        Assert.True(double.IsNaN(form.GetLatest()));
    }

    [Fact]
    public void ACandleGivenToTheVwap_IsABarWithoutAVolume()
    {
        var withCandle = new StreamingVwap();
        var withBar = new StreamingVwap();

        withCandle.AppendCandle(new OhlcBar(10, 12, 9, 11));
        withBar.AppendBar(OhlcvBar.From(new OhlcBar(10, 12, 9, 11)));

        Assert.Equal(withBar.GetLatest(), withCandle.GetLatest());
        Assert.Equal(1, withCandle.ProcessedCount);
    }

#pragma warning disable CS0618 // the old way of writing a second output is what is held here
    [Fact]
    public void AnOutputWrittenTheOldWay_IsWrittenToItsLine()
    {
        var form = new TwoOutputForm();

        form.Append(1.0);

        // The old way wrote its point at once, and the row of the input (empty for this output) followed it.
        Assert.Contains(7.0, form.OutputSeries[1].CreateSnapshot().YData);
    }

    private sealed class TwoOutputForm() : StreamingIndicator([new StreamingLineSeries(16), new StreamingLineSeries(16)])
    {
        public override int WarmupPeriod => 1;

        protected override double ComputeNext(double price)
        {
            AppendToOutput(1, ProcessedCount, 7.0);
            return price;
        }
    }
#pragma warning restore CS0618
}

/// <summary>A candle indicator hands its subclasses the true range of the bars after the first.</summary>
public sealed class CandleIndicatorTrueRangeTests
{
    private sealed record Ranges(double[] Values) : IIndicatorResult;

    private sealed class RangeProbe(OhlcvBars bars) : CandleIndicator<Ranges>(bars)
    {
        public override Ranges Compute() => new(ComputeTrueRange());

        public override void Apply(Axes axes) { }
    }

    [Fact]
    public void TheTrueRange_IsTheWidestOfTheBarsRangeAndItsGapFromTheCloseBefore()
    {
        double[] high = [10, 12, 11];
        double[] low = [8, 9, 6];
        double[] close = [9, 11, 7];

        var ranges = new RangeProbe(OhlcvBars.Hlc(high, low, close)).Compute().Values;

        // bar 1: high 12, low 9, previous close 9 -> 3; bar 2: high 11, low 6, previous close 11 -> 5
        Assert.Equal([3.0, 5.0], ranges);
    }
}
