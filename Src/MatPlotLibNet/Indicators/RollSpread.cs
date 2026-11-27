// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Indicators;

/// <summary>Roll's bid-ask-spread estimator from serial autocovariance of price changes.
/// Yields a non-zero spread only when consecutive price differences are negatively correlated
/// (the bid-ask-bounce signature). Reference: Roll (1984), <i>Journal of Finance</i> 39(4).</summary>
/// <remarks>Per window: <c>cov = Σ(Δp_t − Δp̄)(Δp_{t-1} − Δp̄) / N</c> over N lagged pairs.
/// <c>S = 2·√(−cov)</c> when <c>cov &lt; 0</c>, else 0 (spread unidentifiable).</remarks>
public sealed class RollSpread : PriceIndicator<SignalResult>
{
    /// <summary>The <c>period</c> the indicator is made with when the caller gives none.</summary>
    public const int DefaultPeriod = 20;

    private readonly int _period;

    /// <summary>Creates a new Roll spread indicator.</summary>
    /// <param name="prices">Price series (typically close).</param>
    /// <param name="period">Number of lagged pairs in the covariance window (default <see cref="DefaultPeriod"/>, must be ≥ 2).</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="period"/> &lt; 2.</exception>
    public RollSpread(double[] prices, int period = DefaultPeriod) : base(prices, Spec(period))
    {
        _period = period;
    }

    internal static IndicatorSpec Spec(int period) => IndicatorSpec.OfCheckedWindow("Roll", period, 2, period + 1);

    /// <inheritdoc />
    public override SignalResult Compute()
    {
        var dp = new double[PairsIn];
        for (int i = 0; i < dp.Length; i++)
            dp[i] = Prices[i + 1] - Prices[i];

        // N lagged pairs over a window spanning N+1 Δp values.
        int windowLen = _period + 1;
        int outLen = ValueCount;
        var result = new double[outLen];
        for (int w = 0; w < outLen; w++)
        {
            int start = w;
            int end = w + windowLen - 1; // inclusive
            // Mean over all Δp values in the window
            double sum = 0;
            for (int k = start; k <= end; k++) sum += dp[k];
            double mean = sum / windowLen;

            double cov = 0;
            for (int t = start + 1; t <= end; t++)
            {
                cov += (dp[t] - mean) * (dp[t - 1] - mean);
            }
            cov /= _period;

            result[w] = cov < 0 ? 2.0 * Math.Sqrt(-cov) : 0.0;
        }
        return result;
    }

    /// <inheritdoc />
    public override void Apply(Axes axes) => PlotLine(axes, Compute());
}
