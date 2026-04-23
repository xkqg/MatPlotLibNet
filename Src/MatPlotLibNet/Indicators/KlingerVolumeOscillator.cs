// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

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
    /// <param name="high">High prices.</param>
    /// <param name="low">Low prices.</param>
    /// <param name="close">Close prices.</param>
    /// <param name="volume">Volume per bar.</param>
    /// <param name="fastPeriod">Fast EMA span for volume-force smoothing. Default 34. Must be ≥ 2.</param>
    /// <param name="slowPeriod">Slow EMA span. Default 55. Must be &gt; <paramref name="fastPeriod"/>.</param>
    /// <param name="signalPeriod">EMA span for the signal line. Default 13. Must be ≥ 1.</param>
    public KlingerVolumeOscillator(double[] high, double[] low, double[] close, double[] volume,
        int fastPeriod = 34, int slowPeriod = 55, int signalPeriod = 13)
        : base([], high, low, close, volume)
    {
        if (high.Length != low.Length || high.Length != close.Length || high.Length != volume.Length)
            throw new ArgumentException(
                $"high ({high.Length}), low ({low.Length}), close ({close.Length}), and volume ({volume.Length}) must have equal length.");
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
        var vf = new double[outLen];
        int prevTrend = 0;
        double cm = 0;
        double prevHlc = (High[0] + Low[0] + Close[0]) / 3.0;

        for (int i = 0; i < outLen; i++)
        {
            int b = i + 1;
            double hlc = (High[b] + Low[b] + Close[b]) / 3.0;
            int trend = hlc > prevHlc ? 1 : (hlc < prevHlc ? -1 : 0);
            double range = High[b] - Low[b];
            double prevRange = High[b - 1] - Low[b - 1];

            cm = (i == 0 || trend != prevTrend) ? prevRange + range : cm + range;

            vf[i] = cm > 0
                ? Volume[b] * Math.Abs(2.0 * range / cm - 1.0) * trend * 100.0
                : 0.0;

            prevHlc = hlc;
            prevTrend = trend;
        }

        var fastEma = EmaAdjustFalse(vf, _fastPeriod);
        var slowEma = EmaAdjustFalse(vf, _slowPeriod);
        var kvo = new double[outLen];
        for (int i = 0; i < outLen; i++) kvo[i] = fastEma[i] - slowEma[i];
        var signal = EmaAdjustFalse(kvo, _signalPeriod);

        return new KlingerResult(kvo, signal);
    }

    /// <summary>Exponential moving average, <c>adjust=False</c> (pandas convention): seeds with
    /// the first input sample, then applies <c>α·x_t + (1−α)·y_{t−1}</c> where <c>α = 2/(span+1)</c>.
    /// Matches the Python reference <c>pd.Series(x).ewm(span=span, adjust=False).mean()</c>.</summary>
    private static double[] EmaAdjustFalse(double[] values, int span)
    {
        int n = values.Length;
        var y = new double[n];
        if (n == 0) return y;
        double alpha = 2.0 / (span + 1);
        double oneMinusAlpha = 1.0 - alpha;
        y[0] = values[0];
        for (int i = 1; i < n; i++) y[i] = alpha * values[i] + oneMinusAlpha * y[i - 1];
        return y;
    }

    /// <inheritdoc />
    public override void Apply(Axes axes)
    {
        var r = Compute();
        PlotSignal(axes, r.Kvo, warmup: 1, label: "KVO");
        PlotSignal(axes, r.Signal, warmup: 1, label: "Signal", color: Colors.Tab10Orange);
    }
}
