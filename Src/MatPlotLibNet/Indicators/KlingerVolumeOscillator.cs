// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Steps;
using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Indicators;

/// <summary>Stephen Klinger's volume oscillator (1977) — combines volume direction with
/// cumulative money flow. Produces a KVO line (fast EMA − slow EMA of volume force) plus a
/// Signal line (EMA of KVO); crossovers are the standard buy/sell trigger. Reference:
/// Klinger, S. J. (1977). <i>Volume Oscillator</i>; popularised in <i>Technical Analysis of
/// Stocks &amp; Commodities</i>.</summary>
public sealed class KlingerVolumeOscillator : CandleIndicator<KlingerResult>
{
    private readonly int _fastPeriod;
    private readonly int _slowPeriod;
    private readonly int _signalPeriod;

    /// <summary>Creates a new Klinger Volume Oscillator.</summary>
    /// <param name="bars">High, Low, Close and Volume; built via <see cref="OhlcvBars.Hlcv"/>.</param>
    /// <param name="fastPeriod">Fast EMA span for volume-force smoothing. Default 34. Must be ≥ 2.</param>
    /// <param name="slowPeriod">Slow EMA span. Default 55. Must be &gt; <paramref name="fastPeriod"/>.</param>
    /// <param name="signalPeriod">EMA span for the signal line. Default 13. Must be ≥ 1.</param>
    public KlingerVolumeOscillator(OhlcvBars bars, int fastPeriod = 34, int slowPeriod = 55, int signalPeriod = 13)
        : base(bars)
    {
        if (fastPeriod < 2)
            throw new ArgumentException($"fastPeriod must be >= 2 (got {fastPeriod}).", nameof(fastPeriod));
        if (slowPeriod <= fastPeriod)
            throw new ArgumentException(
                $"slowPeriod must be > fastPeriod (got slow={slowPeriod}, fast={fastPeriod}).", nameof(slowPeriod));
        if (signalPeriod < 1)
            throw new ArgumentException($"signalPeriod must be >= 1 (got {signalPeriod}).", nameof(signalPeriod));
        _fastPeriod = fastPeriod;
        _slowPeriod = slowPeriod;
        _signalPeriod = signalPeriod;
        Label = $"KVO({fastPeriod}/{slowPeriod}/{signalPeriod})";
    }

    /// <inheritdoc />
    public override KlingerResult Compute()
    {
        int n = BarCount;
        if (n < 2) return new KlingerResult(Array.Empty<double>(), Array.Empty<double>());

        int outLen = n - 1;
        var kvo = new double[outLen];
        var signal = new double[outLen];
        var step = new KlingerStep(_fastPeriod, _slowPeriod, _signalPeriod);
        for (int b = 0; b < n; b++)
        {
            var point = step.Next(High[b], Low[b], Close[b], Volume[b]);
            if (b == 0) continue;
            kvo[b - 1] = point.Kvo;
            signal[b - 1] = point.Signal;
        }

        return new KlingerResult(kvo, signal);
    }

    /// <summary>Exponential moving average, <c>adjust=False</c> (pandas convention): seeds with
    /// the first input sample, then applies <c>α·x_t + (1−α)·y_{t−1}</c> where <c>α = 2/(span+1)</c>.
    /// Matches the Python reference <c>pd.Series(x).ewm(span=span, adjust=False).mean()</c>.</summary>

    /// <inheritdoc />
    public override void Apply(Axes axes)
    {
        var r = Compute();
        PlotSignal(axes, r.Kvo, warmup: 1, label: "KVO");
        PlotSignal(axes, r.Signal, warmup: 1, label: "Signal", color: Colors.Tab10Orange);
    }
}
