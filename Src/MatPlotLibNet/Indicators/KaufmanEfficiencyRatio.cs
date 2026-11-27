// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Indicators;

/// <summary>Kaufman Efficiency Ratio — a 0–1 trend-vs-noise gauge. Near 1 = strong trend,
/// near 0 = choppy range. Dirt-simple math, huge downstream utility (basis of KAMA).</summary>
/// <remarks>For bar index <c>t</c> and window length <c>N</c>:
/// <c>ER(t) = |C_t − C_{t-N}| / Σ|C_i − C_{i-1}|</c> where the sum runs <c>i ∈ (t-N+1..t)</c>.
/// When the denominator is 0 (flat window), output is 0 (explicit guard). Reference:
/// Kaufman, P. J. (1995). <i>Smarter Trading</i>.</remarks>
public sealed class KaufmanEfficiencyRatio : PriceIndicator<SignalResult>
{
    /// <summary>The <c>period</c> the indicator is made with when the caller gives none.</summary>
    public const int DefaultPeriod = 10;

    private readonly int _period;

    /// <summary>Creates a new Kaufman Efficiency Ratio indicator.</summary>
    /// <param name="prices">Price data (typically close prices).</param>
    /// <param name="period">Lookback window length (default <see cref="DefaultPeriod"/>). Must be ≥ 1.</param>
    /// <exception cref="ArgumentException">Thrown when period &lt; 1.</exception>
    public KaufmanEfficiencyRatio(double[] prices, int period = DefaultPeriod) : base(prices, Spec(period))
    {
        _period = period;
    }

    internal static IndicatorSpec Spec(int period) => IndicatorSpec.OfCheckedWindow("ER", period, 1, period);

    /// <inheritdoc />
    public override SignalResult Compute()
    {
        int outLen = ValueCount;
        var result = new double[outLen];

        // Precompute absolute first-differences once (one per pair of bars).
        var absDiff = new double[PairsIn];
        for (int i = 0; i < absDiff.Length; i++)
            absDiff[i] = Math.Abs(Prices[i + 1] - Prices[i]);

        double volatility = 0;
        for (int w = 0; w < outLen; w++)
        {
            // At output index w, bar t = w + _period:
            //   change     = |C_t − C_{t-N}|
            //   volatility = Σ absDiff[i] for i ∈ [t-N .. t-1]  (= [w .. w+_period-1]): summed over the first window (bars
            //   1.._period), then rolled forward a bar at a time.
            int t = w + _period;
            if (w == 0)
                for (int i = 0; i < _period; i++) volatility += absDiff[i];
            else
                volatility += absDiff[t - 1] - absDiff[w - 1];

            double change = Math.Abs(Prices[t] - Prices[t - _period]);
            result[w] = volatility == 0 ? 0.0 : change / volatility;
        }

        return result;
    }

    /// <summary>The range the values of the indicator live in: its pane, and the pane of <see cref="Streaming.StreamingKaufmanEfficiencyRatio"/>, are pinned to it.</summary>
    internal static PaneRange Pane { get; } = PaneRange.Unit;

    /// <inheritdoc />
    public override void Apply(Axes axes)
    {
        PlotLine(axes, Compute());
        Pane.Pin(axes);
    }
}
