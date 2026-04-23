// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Indicators;

/// <summary>Colin Twiggs' Money Flow indicator — the true-range-based refinement of Chaikin
/// Money Flow. Output bounded in <c>[-1, 1]</c>: positive = accumulation, negative =
/// distribution. Uses Wilder-style smoothing (<c>α = 1/period</c>) on both the accumulation-
/// distribution numerator and the volume denominator. Reference: Twiggs, C. (2002–2004),
/// <i>Twiggs Money Flow</i>, Incredible Charts documentation.</summary>
public sealed class TwiggsMoneyFlow : CandleIndicator<SignalResult>
{
    private readonly int _period;

    /// <summary>Creates a new Twiggs Money Flow indicator.</summary>
    /// <param name="high">High prices.</param>
    /// <param name="low">Low prices.</param>
    /// <param name="close">Close prices.</param>
    /// <param name="volume">Volume per bar.</param>
    /// <param name="period">Wilder smoothing period. Default 21. Must be ≥ 2.</param>
    public TwiggsMoneyFlow(double[] high, double[] low, double[] close, double[] volume, int period = 21)
        : base([], high, low, close, volume)
    {
        if (high.Length != low.Length || high.Length != close.Length || high.Length != volume.Length)
            throw new ArgumentException(
                $"high ({high.Length}), low ({low.Length}), close ({close.Length}), and volume ({volume.Length}) must have equal length.");
        if (period < 2)
            throw new ArgumentException($"period must be >= 2 (got {period}).", nameof(period));
        _period = period;
        Label = $"TMF({period})";
    }

    /// <inheritdoc />
    public override SignalResult Compute()
    {
        int n = BarCount;
        if (n < 2) return Array.Empty<double>();

        int outLen = n - 1;
        var ad = new double[outLen];
        var volTail = new double[outLen];

        for (int i = 0; i < outLen; i++)
        {
            int b = i + 1;
            double pc = Close[b - 1];
            double th = Math.Max(High[b], pc);
            double tl = Math.Min(Low[b], pc);
            double tr = th - tl;
            ad[i] = tr > 0 ? (2.0 * Close[b] - th - tl) / tr * Volume[b] : 0.0;
            volTail[i] = Volume[b];
        }

        double alpha = 1.0 / _period;
        var emaAd = WilderEma(ad, alpha);
        var emaVol = WilderEma(volTail, alpha);

        var tmf = new double[outLen];
        for (int i = 0; i < outLen; i++)
            tmf[i] = emaVol[i] > 0 ? emaAd[i] / emaVol[i] : 0.0;
        return tmf;
    }

    /// <summary>Wilder-style EMA (<c>adjust=False</c>): <c>y_0 = x_0</c>, then
    /// <c>y_t = α·x_t + (1−α)·y_{t−1}</c>.</summary>
    private static double[] WilderEma(double[] values, double alpha)
    {
        int n = values.Length;
        var y = new double[n];
        if (n == 0) return y;
        double oneMinusAlpha = 1.0 - alpha;
        y[0] = values[0];
        for (int i = 1; i < n; i++) y[i] = alpha * values[i] + oneMinusAlpha * y[i - 1];
        return y;
    }

    /// <inheritdoc />
    public override void Apply(Axes axes)
    {
        PlotSignal(axes, Compute(), warmup: 1);
        axes.YAxis.Min = -1;
        axes.YAxis.Max = 1;
    }
}
