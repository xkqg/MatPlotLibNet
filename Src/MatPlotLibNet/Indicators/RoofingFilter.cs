// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Ehlers;
using MatPlotLibNet.Indicators.Steps;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Indicators;

/// <summary>Ehlers' Roofing Filter — a band-pass implemented as a high-pass followed by a
/// SuperSmoother low-pass. Isolates the cyclic mid-frequency component after trend removal.
/// Reference: Ehlers (2014), <i>Predictive and Successful Indicators</i>, S&amp;C magazine.</summary>
public sealed class RoofingFilter : PriceIndicator<SignalResult>
{
    private readonly int _hpPeriod;
    private readonly int _lpPeriod;

    /// <summary>Creates a new Roofing Filter.</summary>
    /// <param name="prices">Price series.</param>
    /// <param name="hpPeriod">High-pass cutoff (default 48). Must be ≥ 4.</param>
    /// <param name="lpPeriod">Low-pass cutoff (default 10). Must be ≥ 2 and strictly &lt; <paramref name="hpPeriod"/>.</param>
    public RoofingFilter(double[] prices, int hpPeriod = 48, int lpPeriod = 10) : base(prices)
    {
        if (hpPeriod < 4)
            throw new ArgumentException($"hpPeriod must be >= 4 (got {hpPeriod}).", nameof(hpPeriod));
        if (lpPeriod < 2)
            throw new ArgumentException($"lpPeriod must be >= 2 (got {lpPeriod}).", nameof(lpPeriod));
        if (lpPeriod >= hpPeriod)
            throw new ArgumentException(
                $"lpPeriod must be < hpPeriod (got {lpPeriod} vs {hpPeriod}).", nameof(lpPeriod));
        _hpPeriod = hpPeriod;
        _lpPeriod = lpPeriod;
        Label = $"Roof({hpPeriod}/{lpPeriod})";
    }

    /// <inheritdoc />
    public override SignalResult Compute()
    {
        int n = Prices.Length;
        if (n < 3) return Array.Empty<double>();
        var result = new double[n];
        var highPass = new HighPassStep(_hpPeriod);
        var smoother = new SuperSmootherStep(_lpPeriod);
        for (int i = 0; i < n; i++) result[i] = smoother.Next(highPass.Next(Prices[i]));
        return result;
    }

    /// <inheritdoc />
    public override void Apply(Axes axes) => PlotSignal(axes, Compute(), warmup: 2);
}
