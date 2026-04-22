// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Indicators;

/// <summary>LazyBear's Squeeze Momentum indicator: fires when Bollinger Bands contract inside
/// Keltner Channels (low-volatility coiling), with momentum direction from linear regression.
/// Reference: LazyBear (TradingView, 2014), based on Carter (2005) <i>Mastering the Trade</i>.</summary>
public sealed class SqueezeMomentum : CandleIndicator<SqueezeResult>
{
    private readonly int _period;
    private readonly double _bbMult;
    private readonly double _kcMult;

    /// <summary>Creates a new Squeeze Momentum indicator.</summary>
    /// <param name="high">High prices.</param>
    /// <param name="low">Low prices.</param>
    /// <param name="close">Close prices.</param>
    /// <param name="period">Rolling window length (default 20). Must be ≥ 2.</param>
    /// <param name="bbMult">Bollinger Band stddev multiplier (default 2.0). Must be &gt; 0.</param>
    /// <param name="kcMult">Keltner Channel ATR multiplier (default 1.5). Must be &gt; 0.</param>
    public SqueezeMomentum(double[] high, double[] low, double[] close,
                           int period = 20, double bbMult = 2.0, double kcMult = 1.5)
        : base([], high, low, close, [])
    {
        if (high.Length != low.Length || low.Length != close.Length)
            throw new ArgumentException("HLC arrays must have equal length.");
        if (period < 2)
            throw new ArgumentException($"period must be >= 2 (got {period}).", nameof(period));
        if (bbMult <= 0)
            throw new ArgumentException($"bbMult must be > 0 (got {bbMult}).", nameof(bbMult));
        if (kcMult <= 0)
            throw new ArgumentException($"kcMult must be > 0 (got {kcMult}).", nameof(kcMult));
        for (int i = 0; i < high.Length; i++)
        {
            if (high[i] < low[i])
                throw new ArgumentException(
                    $"SqueezeMomentum requires H >= L; bar {i} has H={high[i]} < L={low[i]}.");
        }
        _period = period;
        _bbMult = bbMult;
        _kcMult = kcMult;
        Label = $"Squeeze({period})";
    }

    /// <inheritdoc />
    public override SqueezeResult Compute()
    {
        int n = BarCount;
        if (n <= _period + 1)
            return new SqueezeResult(Array.Empty<bool>(), Array.Empty<double>());

        // Per-bar true range; TR[0] uses H-L fallback, TR[t>=1] uses prev close.
        var tr = new double[n];
        tr[0] = High[0] - Low[0];
        for (int t = 1; t < n; t++)
        {
            double hl = High[t] - Low[t];
            double hc = Math.Abs(High[t] - Close[t - 1]);
            double lc = Math.Abs(Low[t] - Close[t - 1]);
            tr[t] = Math.Max(hl, Math.Max(hc, lc));
        }

        int outLen = n - _period - 1;
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
            double sumC = 0;
            for (int i = startBar; i <= endBar; i++) sumC += Close[i];
            double basis = sumC / N;
            double varSum = 0;
            for (int i = startBar; i <= endBar; i++)
            {
                double d = Close[i] - basis;
                varSum += d * d;
            }
            double stddev = Math.Sqrt(varSum / N);
            double bbUpper = basis + _bbMult * stddev;
            double bbLower = basis - _bbMult * stddev;

            // SMA(TR, N)
            double sumTr = 0;
            for (int i = startBar; i <= endBar; i++) sumTr += tr[i];
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
        PlotSignal(axes, result.Momentum, warmup: _period + 1);
    }
}
