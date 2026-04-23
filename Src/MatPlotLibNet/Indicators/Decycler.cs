// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Ehlers;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Indicators;

/// <summary>Ehlers' Decycler — subtracts the dominant-cycle band from the price series to
/// leave only the trend component. <c>decycler_t = price_t − HP_t</c> where HP is the
/// one-pole high-pass filter output. Cleaner than <see cref="EhlersITrend"/> for
/// pure-trend extraction; less reactive because cycle information is fully removed.
/// Reuses <see cref="Ehlers.HighPassFilter"/> (Tier 2c). Reference: Ehlers, J. F. (2015),
/// <i>Decycler Oscillator</i>, Stocks &amp; Commodities 33(6).</summary>
public sealed class Decycler : PriceIndicator<SignalResult>
{
    private readonly int _hpPeriod;

    /// <summary>Creates a new Decycler indicator.</summary>
    /// <param name="prices">Price series.</param>
    /// <param name="hpPeriod">High-pass cutoff period. Default 60. Must be ≥ 4
    /// (HP filter needs at least a 4-bar recurrence to converge).</param>
    public Decycler(double[] prices, int hpPeriod = 60) : base(prices)
    {
        if (hpPeriod < 4)
            throw new ArgumentException($"hpPeriod must be >= 4 (got {hpPeriod}).", nameof(hpPeriod));
        _hpPeriod = hpPeriod;
        Label = $"Decycler({hpPeriod})";
    }

    /// <inheritdoc />
    public override SignalResult Compute()
    {
        int n = Prices.Length;
        if (n < 3) return Array.Empty<double>();

        var hp = ((ReadOnlySpan<double>)Prices).HighPass(_hpPeriod);
        var result = new double[n];
        for (int i = 0; i < n; i++) result[i] = Prices[i] - hp[i];
        return result;
    }

    /// <inheritdoc />
    public override void Apply(Axes axes) => PlotSignal(axes, Compute(), warmup: 2);
}
