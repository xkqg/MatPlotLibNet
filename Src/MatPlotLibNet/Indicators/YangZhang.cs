// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Indicators;

/// <summary>Yang-Zhang volatility estimator — 14× more efficient than close-to-close,
/// handles overnight gaps. Combines three components: overnight jump, open-to-close drift,
/// and Rogers-Satchell intraday.</summary>
/// <remarks>σ²_YZ = σ²_O + k·σ²_C + (1−k)·σ²_RS, with
/// k = 0.34 / (1.34 + (N+1)/(N−1)). Needs <c>C_{t-1}</c> for the overnight term, so the
/// output length is <c>BarCount − period</c>. Reference: Yang &amp; Zhang (2000),
/// <i>Journal of Business</i> 73(3).</remarks>
public sealed class YangZhang : CandleIndicator<SignalResult>
{
    /// <summary>The <c>period</c> the indicator is made with when the caller gives none.</summary>
    public const int DefaultPeriod = 20;

    private readonly int _period;

    /// <summary>Creates a new Yang-Zhang volatility indicator.</summary>
    /// <param name="bars">Open, High, Low and Close (all strictly positive); built via <see cref="OhlcvBars.Ohlc"/>.</param>
    /// <param name="period">Rolling window length. Must be ≥ 2 (k formula has (N−1) in denominator).</param>
    /// <exception cref="ArgumentException">Thrown when period &lt; 2 or any input bar has a non-positive price.</exception>
    public YangZhang(OhlcvBars bars, int period = DefaultPeriod)
        : base(bars, Spec(period))
    {
        bars.ThrowIfNotPositiveOhlc(nameof(YangZhang));
        _period = period;
    }

    internal static IndicatorSpec Spec(int period) => IndicatorSpec.OfCheckedWindow("YZ", period, 2, period);

    /// <inheritdoc />
    public override SignalResult Compute()
    {
        int n = BarCount;
        int outLen = ValueCount;
        var result = new double[outLen];

        // k constant for this period
        double k = 0.34 / (1.34 + (double)(_period + 1) / (_period - 1));

        // Precompute per-bar series used by the three components.
        // lnOC[t] = ln(O_t / C_{t-1})  — defined for t = 1..n-1
        // lnCO[t] = ln(C_t / O_t)      — defined for all t
        // rs[t]   = ln(H/C)·ln(H/O) + ln(L/C)·ln(L/O)  — defined for all t
        var lnOC = new double[n];
        var lnCO = new double[n];
        var rs = new double[n];
        for (int t = 0; t < n; t++)
        {
            lnOC[t] = t == 0 ? 0.0 : Math.Log(Open[t] / Close[t - 1]);
            lnCO[t] = Math.Log(Close[t] / Open[t]);
            double lhc = Math.Log(High[t] / Close[t]);
            double lho = Math.Log(High[t] / Open[t]);
            double llc = Math.Log(Low[t] / Close[t]);
            double llo = Math.Log(Low[t] / Open[t]);
            rs[t] = lhc * lho + llc * llo;
        }

        for (int w = 0; w < outLen; w++)
        {
            // Window covers bars [w+1 .. w+_period], inclusive — first bar uses C_{w} as prevClose.
            int start = w + 1;

            // The three window means in one pass (three independent sums side by side), the two sample variances about them.
            double meanOC = 0, meanCO = 0, meanRS = 0;
            for (int i = start; i < start + _period; i++)
            {
                meanOC += lnOC[i];
                meanCO += lnCO[i];
                meanRS += rs[i];
            }
            meanOC /= _period;
            meanCO /= _period;
            meanRS /= _period;

            double varOC = lnOC.AsSpan(start, _period).Variance(meanOC, ddof: 1);
            double varCO = lnCO.AsSpan(start, _period).Variance(meanCO, ddof: 1);

            double sigma2 = varOC + k * varCO + (1 - k) * meanRS;
            result[w] = Math.Sqrt(Math.Max(0, sigma2));
        }

        return result;
    }

    /// <inheritdoc />
    public override void Apply(Axes axes) => PlotLine(axes, Compute());
}
