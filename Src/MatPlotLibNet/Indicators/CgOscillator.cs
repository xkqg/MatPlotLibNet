// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Indicators;

/// <summary>Ehlers' Center-of-Gravity Oscillator (2002) — linearly weighted price average
/// centred on zero. Recent prices carry higher weight than older ones; shifts in the
/// gravity centre flag momentum turns and tend to lead unweighted oscillators slightly.
/// Reference: Ehlers, J. F. (2002), <i>The CG Oscillator</i>, Stocks &amp; Commodities 20(3);
/// Ch. 7 of <i>Cybernetic Analysis for Stocks and Futures</i> (2004).</summary>
public sealed class CgOscillator : PriceIndicator<SignalResult>
{
    private readonly int _period;

    /// <summary>Creates a new CG Oscillator.</summary>
    /// <param name="prices">Price series.</param>
    /// <param name="period">Weighting window length. Default 10. Must be ≥ 2.</param>
    public CgOscillator(double[] prices, int period = 10) : base(prices)
    {
        if (period < 2)
            throw new ArgumentException($"period must be >= 2 (got {period}).", nameof(period));
        _period = period;
        Label = $"CG({period})";
    }

    /// <inheritdoc />
    public override SignalResult Compute()
    {
        int n = Prices.Length;
        if (n < _period) return Array.Empty<double>();

        int outLen = n - _period + 1;
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
    public override void Apply(Axes axes) => PlotSignal(axes, Compute(), warmup: _period - 1);
}
