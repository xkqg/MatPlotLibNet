// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Indicators;

/// <summary>Ehlers' Center-of-Gravity Oscillator (2002) — linearly weighted price average
/// centred on zero. Recent prices carry higher weight than older ones; shifts in the
/// gravity centre flag momentum turns and tend to lead unweighted oscillators slightly.
/// Reference: Ehlers, J. F. (2002), <i>The CG Oscillator</i>, Stocks &amp; Commodities 20(3);
/// Ch. 7 of <i>Cybernetic Analysis for Stocks and Futures</i> (2004).</summary>
public sealed class CgOscillator : PriceIndicator<SignalResult>
{
    /// <summary>The <c>period</c> the indicator is made with when the caller gives none.</summary>
    public const int DefaultPeriod = 10;

    private readonly int _period;

    /// <summary>Creates a new CG Oscillator.</summary>
    /// <param name="prices">Price series.</param>
    /// <param name="period">Weighting window length. Default <see cref="DefaultPeriod"/>. Must be ≥ 2.</param>
    public CgOscillator(double[] prices, int period = DefaultPeriod) : base(prices, Spec(period))
    {
        _period = period;
    }

    internal static IndicatorSpec Spec(int period) => IndicatorSpec.OfCheckedWindow("CG", period, 2, period - 1);

    /// <inheritdoc />
    public override SignalResult Compute()
    {
        int outLen = ValueCount;
        var result = new double[outLen];
        double offset = (_period + 1) / 2.0;

        for (int w = 0; w < outLen; w++)
        {
            // Window spans indices [w .. w+_period-1]; price_{t-i} for i=0..period-1
            // corresponds to Prices[end - i] where end = w + _period - 1.
            int end = w + _period - 1;
            double num = 0;
            double den = 0;
            for (int i = 0; i < _period; i++)
            {
                double p = Prices[end - i];
                num += (i + 1) * p;
                den += p;
            }
            result[w] = den != 0 ? -num / den + offset : 0.0;
        }
        return result;
    }

    /// <inheritdoc />
    public override void Apply(Axes axes) => PlotLine(axes, Compute());
}
