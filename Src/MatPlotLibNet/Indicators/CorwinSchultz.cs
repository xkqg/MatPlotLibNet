// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Indicators;

/// <summary>Corwin-Schultz bid-ask-spread estimator. Recovers the effective spread from daily
/// H/L only — no tick data required. Output is the rolling-window mean of per-bar spread
/// estimates. Reference: Corwin &amp; Schultz (2012), <i>Journal of Finance</i> 67(2).</summary>
/// <remarks>Per-bar <c>S_t</c> is computed from bars <c>(t-1, t)</c>; negative estimates
/// are clamped to 0 per Corwin-Schultz §II.D. Output length is <c>n − period</c>.</remarks>
public sealed class CorwinSchultz : CandleIndicator<SignalResult>
{
    /// <summary>The <c>period</c> the indicator is made with when the caller gives none.</summary>
    public const int DefaultPeriod = 20;

    private static readonly double Denom = 3.0 - 2.0 * Math.Sqrt(2.0); // 3 − 2√2

    private readonly int _period;

    /// <summary>Creates a new Corwin-Schultz indicator.</summary>
    /// <param name="bars">High and Low; built via <see cref="OhlcvBars.Hl"/>.</param>
    /// <param name="period">Rolling window length in bars (default <see cref="DefaultPeriod"/>).</param>
    /// <exception cref="ArgumentException">Thrown on length mismatch, period &lt; 2,
    /// non-positive H or L, or H &lt; L on any bar.</exception>
    public CorwinSchultz(OhlcvBars bars, int period = DefaultPeriod)
        : base(bars, Spec(period))
    {
        bars.High.ThrowIfNotPositivePrices(nameof(CorwinSchultz), "high");
        bars.Low.ThrowIfNotPositivePrices(nameof(CorwinSchultz), "low");
        bars.ThrowIfHighBelowLow(nameof(CorwinSchultz));
        _period = period;
    }

    internal static IndicatorSpec Spec(int period) => IndicatorSpec.OfCheckedWindow("CS", period, 2, period);

    /// <inheritdoc />
    public override SignalResult Compute()
    {
        // Per-bar S_t for t = 1..n-1 (length n-1).
        return RollingMeanOfPairs(_period, t =>
        {
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
            return s < 0 ? 0 : s;
        });
    }

    /// <inheritdoc />
    public override void Apply(Axes axes) => PlotLine(axes, Compute());
}
