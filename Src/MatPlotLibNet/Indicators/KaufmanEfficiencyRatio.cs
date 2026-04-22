// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Indicators;

/// <summary>Kaufman Efficiency Ratio — a 0–1 trend-vs-noise gauge. Near 1 = strong trend,
/// near 0 = choppy range. Dirt-simple math, huge downstream utility (basis of KAMA).</summary>
/// <remarks>For bar index <c>t</c> and window length <c>N</c>:
/// <c>ER(t) = |C_t − C_{t-N}| / Σ|C_i − C_{i-1}|</c> where the sum runs <c>i ∈ (t-N+1..t)</c>.
/// When the denominator is 0 (flat window), output is 0 (explicit guard). Reference:
/// Kaufman, P. J. (1995). <i>Smarter Trading</i>.</remarks>
public sealed class KaufmanEfficiencyRatio : PriceIndicator<SignalResult>
{
    private readonly int _period;

    /// <summary>Creates a new Kaufman Efficiency Ratio indicator.</summary>
    /// <param name="prices">Price data (typically close prices).</param>
    /// <param name="period">Lookback window length (default 10). Must be ≥ 1.</param>
    /// <exception cref="ArgumentException">Thrown when period &lt; 1.</exception>
    public KaufmanEfficiencyRatio(double[] prices, int period = 10) : base(prices)
    {
        if (period < 1)
            throw new ArgumentException($"KaufmanEfficiencyRatio period must be >= 1 (got {period}).", nameof(period));
        _period = period;
        Label = $"ER({period})";
    }

    /// <inheritdoc />
    public override SignalResult Compute()
    {
        int n = Prices.Length;
        if (n <= _period) return Array.Empty<double>();

        int outLen = n - _period;
        var result = new double[outLen];

        // Precompute absolute first-differences once (length n-1).
        var absDiff = new double[n - 1];
        for (int i = 0; i < n - 1; i++)
            absDiff[i] = Math.Abs(Prices[i + 1] - Prices[i]);

        // Initial volatility window = sum of absDiff[0.._period-1] (covers bars 1.._period).
        double volatility = 0;
        for (int i = 0; i < _period; i++) volatility += absDiff[i];

        for (int w = 0; w < outLen; w++)
        {
            // At output index w, bar t = w + _period:
            //   change     = |C_t − C_{t-N}|
            //   volatility = Σ absDiff[i] for i ∈ [t-N .. t-1]  (= [w .. w+_period-1])
            int t = w + _period;
            double change = Math.Abs(Prices[t] - Prices[t - _period]);
            result[w] = volatility == 0 ? 0.0 : change / volatility;

            // Roll the window forward for the next iteration (skip on last).
            if (w + 1 < outLen)
                volatility += absDiff[t] - absDiff[w];
        }

        return result;
    }

    /// <inheritdoc />
    public override void Apply(Axes axes)
    {
        PlotSignal(axes, Compute(), _period);
        axes.YAxis.Min = 0;
        axes.YAxis.Max = 1;
    }
}
