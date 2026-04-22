// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Indicators;

/// <summary>Corwin-Schultz bid-ask-spread estimator. Recovers the effective spread from daily
/// H/L only — no tick data required. Output is the rolling-window mean of per-bar spread
/// estimates. Reference: Corwin &amp; Schultz (2012), <i>Journal of Finance</i> 67(2).</summary>
/// <remarks>Per-bar <c>S_t</c> is computed from bars <c>(t-1, t)</c>; negative estimates
/// are clamped to 0 per Corwin-Schultz §II.D. Output length is <c>n − period</c>.</remarks>
public sealed class CorwinSchultz : CandleIndicator<SignalResult>
{
    private static readonly double Denom = 3.0 - 2.0 * Math.Sqrt(2.0); // 3 − 2√2

    private readonly int _period;

    /// <summary>Creates a new Corwin-Schultz indicator.</summary>
    /// <exception cref="ArgumentException">Thrown on length mismatch, period &lt; 2,
    /// non-positive H or L, or H &lt; L on any bar.</exception>
    public CorwinSchultz(double[] high, double[] low, int period = 20)
        : base([], high, low, high, [])
    {
        if (high.Length != low.Length)
            throw new ArgumentException(
                $"high ({high.Length}) and low ({low.Length}) must have equal length.", nameof(low));
        if (period < 2)
            throw new ArgumentException($"period must be >= 2 (got {period}).", nameof(period));
        for (int i = 0; i < high.Length; i++)
        {
            if (high[i] <= 0 || low[i] <= 0)
                throw new ArgumentException(
                    $"CorwinSchultz requires strictly positive H/L; bar {i} has H={high[i]}, L={low[i]}.");
            if (high[i] < low[i])
                throw new ArgumentException(
                    $"CorwinSchultz requires H >= L; bar {i} has H={high[i]} < L={low[i]}.");
        }
        _period = period;
        Label = $"CS({period})";
    }

    /// <inheritdoc />
    public override SignalResult Compute()
    {
        int n = BarCount;
        if (n <= _period) return Array.Empty<double>();

        // Per-bar S_t for t = 1..n-1 (length n-1).
        var perBar = new double[n - 1];
        for (int i = 0; i < n - 1; i++)
        {
            int t = i + 1;
            double h0 = High[t - 1], l0 = Low[t - 1];
            double h1 = High[t],     l1 = Low[t];

            double lnHL0 = Math.Log(h0 / l0);
            double lnHL1 = Math.Log(h1 / l1);
            double beta = lnHL0 * lnHL0 + lnHL1 * lnHL1;

            double hMax = Math.Max(h0, h1);
            double lMin = Math.Min(l0, l1);
            double lnRange = Math.Log(hMax / lMin);
            double gamma = lnRange * lnRange;

            double alpha = (Math.Sqrt(2 * beta) - Math.Sqrt(beta)) / Denom
                         - Math.Sqrt(gamma / Denom);

            double eAlpha = Math.Exp(alpha);
            double s = 2 * (eAlpha - 1) / (1 + eAlpha);
            perBar[i] = s < 0 ? 0 : s;
        }

        int outLen = n - _period;
        var result = new double[outLen];
        double sum = 0;
        for (int i = 0; i < _period; i++) sum += perBar[i];
        result[0] = sum / _period;
        for (int i = 1; i < outLen; i++)
        {
            sum += perBar[i + _period - 1] - perBar[i - 1];
            result[i] = sum / _period;
        }
        return result;
    }

    /// <inheritdoc />
    public override void Apply(Axes axes) => PlotSignal(axes, Compute(), _period);
}
