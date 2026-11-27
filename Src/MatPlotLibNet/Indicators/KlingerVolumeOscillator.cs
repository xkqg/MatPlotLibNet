// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Steps;
using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Indicators;

/// <summary>Stephen Klinger's volume oscillator (1977) — combines volume direction with
/// cumulative money flow. Produces a KVO line (fast EMA − slow EMA of volume force) plus a
/// Signal line (EMA of KVO); crossovers are the standard buy/sell trigger. Reference:
/// Klinger, S. J. (1977). <i>Volume Oscillator</i>; popularised in <i>Technical Analysis of
/// Stocks &amp; Commodities</i>.</summary>
public sealed class KlingerVolumeOscillator : CandleIndicator<KlingerResult>
{
    /// <summary>The <c>fastPeriod</c> the indicator is made with when the caller gives none.</summary>
    public const int DefaultFastPeriod = 34;

    /// <summary>The <c>slowPeriod</c> the indicator is made with when the caller gives none.</summary>
    public const int DefaultSlowPeriod = 55;

    /// <summary>The <c>signalPeriod</c> the indicator is made with when the caller gives none.</summary>
    public const int DefaultSignalPeriod = 13;

    private readonly int _fastPeriod;
    private readonly int _slowPeriod;
    private readonly int _signalPeriod;

    /// <summary>Creates a new Klinger Volume Oscillator.</summary>
    /// <param name="bars">High, Low, Close and Volume; built via <see cref="OhlcvBars.Hlcv"/>.</param>
    /// <param name="fastPeriod">Fast EMA span for volume-force smoothing. Default <see cref="DefaultFastPeriod"/>. Must be ≥ 2.</param>
    /// <param name="slowPeriod">Slow EMA span. Default <see cref="DefaultSlowPeriod"/>. Must be &gt; <paramref name="fastPeriod"/>.</param>
    /// <param name="signalPeriod">EMA span for the signal line. Default <see cref="DefaultSignalPeriod"/>. Must be ≥ 1.</param>
    public KlingerVolumeOscillator(OhlcvBars bars, int fastPeriod = DefaultFastPeriod, int slowPeriod = DefaultSlowPeriod, int signalPeriod = DefaultSignalPeriod)
        : base(bars, Spec(fastPeriod, slowPeriod, signalPeriod))
    {
        _fastPeriod = fastPeriod;
        _slowPeriod = slowPeriod;
        _signalPeriod = signalPeriod;
    }

    internal static IndicatorSpec Spec(int fastPeriod, int slowPeriod, int signalPeriod)
    {
        fastPeriod.ThrowIfBelow(2, nameof(fastPeriod));
        slowPeriod.ThrowIfBelow(fastPeriod + 1, nameof(slowPeriod));
        signalPeriod.ThrowIfBelow(1, nameof(signalPeriod));
        string label = string.Invariant($"KVO({fastPeriod}/{slowPeriod}/{signalPeriod})");
        return IndicatorSpec.Of(1, label, label, "Signal");
    }

    /// <inheritdoc />
    public override KlingerResult Compute() =>
        FoldHlcv<KlingerStep, KlingerPoint, Lines>(new(_fastPeriod, _slowPeriod, _signalPeriod), new(ValueCount)).Result;

    /// <summary>The oscillator and its signal, one entry per bar from the first value on.</summary>
    private readonly struct Lines(int count) : IOutputLines<KlingerPoint>
    {
        private readonly double[] _kvo = new double[count], _signal = new double[count];

        internal KlingerResult Result => new(_kvo, _signal);

        public void Set(int index, KlingerPoint output)
        {
            _kvo[index] = output.Kvo;
            _signal[index] = output.Signal;
        }
    }

    /// <summary>Exponential moving average, <c>adjust=False</c> (pandas convention): seeds with
    /// the first input sample, then applies <c>α·x_t + (1−α)·y_{t−1}</c> where <c>α = 2/(span+1)</c>.
    /// Matches the Python reference <c>pd.Series(x).ewm(span=span, adjust=False).mean()</c>.</summary>

    /// <inheritdoc />
    public override void Apply(Axes axes)
    {
        var r = Compute();
        PlotLine(axes, r.Kvo);
        PlotLine(axes, r.Signal, LineLabel(1), null, IndicatorLook.Library.Lines.Second);
    }
}
