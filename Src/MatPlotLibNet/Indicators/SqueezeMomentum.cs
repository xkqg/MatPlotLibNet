// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Indicators;

/// <summary>LazyBear's Squeeze Momentum indicator: fires when Bollinger Bands contract inside
/// Keltner Channels (low-volatility coiling), with momentum direction from linear regression.
/// Reference: LazyBear (TradingView, 2014), based on Carter (2005) <i>Mastering the Trade</i>.</summary>
public sealed class SqueezeMomentum : CandleIndicator<SqueezeResult>
{
    /// <summary>The <c>period</c> the indicator is made with when the caller gives none.</summary>
    public const int DefaultPeriod = 20;

    /// <summary>The <c>bbMult</c> the indicator is made with when the caller gives none.</summary>
    public const double DefaultBbMult = 2.0;

    /// <summary>The <c>kcMult</c> the indicator is made with when the caller gives none.</summary>
    public const double DefaultKcMult = 1.5;

    private readonly int _period;
    private readonly double _bbMult;
    private readonly double _kcMult;

    /// <summary>Creates a new Squeeze Momentum indicator.</summary>
    /// <param name="bars">High, Low (must satisfy <c>L ≤ H</c>) and Close; built via <see cref="OhlcvBars.Hlc"/>.</param>
    /// <param name="period">Rolling window length (default <see cref="DefaultPeriod"/>). Must be ≥ 2.</param>
    /// <param name="bbMult">Bollinger Band stddev multiplier (default <see cref="DefaultBbMult"/>). Must be &gt; 0.</param>
    /// <param name="kcMult">Keltner Channel ATR multiplier (default <see cref="DefaultKcMult"/>). Must be &gt; 0.</param>
    public SqueezeMomentum(OhlcvBars bars, int period = DefaultPeriod, double bbMult = DefaultBbMult, double kcMult = DefaultKcMult)
        : base(bars, Spec(period, bbMult, kcMult))
    {
        bars.ThrowIfHighBelowLow(nameof(SqueezeMomentum));
        _period = period;
        _bbMult = bbMult;
        _kcMult = kcMult;
    }

    internal static IndicatorSpec Spec(int period, double bbMult, double kcMult)
    {
        period.ThrowIfBelow(2, nameof(period));
        bbMult.ThrowIfNotAbove(0, nameof(bbMult));
        kcMult.ThrowIfNotAbove(0, nameof(kcMult));
        string label = IndicatorSpec.Labelled("Squeeze", period);
        return IndicatorSpec.Of(period + 1, label, label, $"{label} On");
    }

    /// <inheritdoc />
    public override SqueezeResult Compute()
    {
        // The true range of every bar after the first: tr[i] is that of bar i + 1. A window starts at bar 2, so each bar it reads has
        // a close before it.
        var tr = ComputeTrueRange();

        int outLen = ValueCount;
        var squeezeOn = new bool[outLen];
        var momentum = new double[outLen];

        // Precompute x-stats for linreg against window x = [0..period-1].
        double N = _period;
        double sumX = N * (N - 1) / 2.0;
        double sumX2 = (N - 1) * N * (2 * N - 1) / 6.0;
        double denom = N * sumX2 - sumX * sumX;

        for (int w = 0; w < outLen; w++)
        {
            int endBar = _period + 1 + w; // inclusive bar index of current output
            int startBar = endBar - _period + 1; // inclusive

            // SMA(close, N) and stddev(close, N) over [startBar..endBar]
            var closes = Close.AsSpan(startBar, _period);
            double basis = closes.Mean();
            double stddev = closes.StdDev(basis);
            double bbUpper = basis + _bbMult * stddev;
            double bbLower = basis - _bbMult * stddev;

            // SMA(TR, N)
            double sumTr = 0;
            for (int i = startBar; i <= endBar; i++) sumTr += tr[i - 1];
            double rangeMa = sumTr / N;
            double kcUpper = basis + _kcMult * rangeMa;
            double kcLower = basis - _kcMult * rangeMa;

            squeezeOn[w] = bbLower > kcLower && bbUpper < kcUpper;

            // midHL / midRef / linreg slope of (close - midRef) against x=[0..N-1]
            double hi = High[startBar], lo = Low[startBar];
            for (int i = startBar + 1; i <= endBar; i++)
            {
                if (High[i] > hi) hi = High[i];
                if (Low[i] < lo) lo = Low[i];
            }
            double midHL = (hi + lo) / 2.0;
            double midRef = (midHL + basis) / 2.0;

            // Linear regression slope: subtracting constant midRef from each y does
            // not change the slope — but we follow the spec literally for clarity.
            double sumY = 0, sumXY = 0;
            for (int i = 0; i < _period; i++)
            {
                double y = Close[startBar + i] - midRef;
                sumY += y;
                sumXY += i * y;
            }
            // denom is strictly positive: the constructor guards period >= 2, which
            // guarantees N·sumX² > (sumX)² for integer x ∈ [0, N-1].
            momentum[w] = (N * sumXY - sumX * sumY) / denom;
        }

        return new SqueezeResult(squeezeOn, momentum);
    }

    /// <inheritdoc />
    public override void Apply(Axes axes)
    {
        var result = Compute();
        PlotLine(axes, result.Momentum);
    }
}
