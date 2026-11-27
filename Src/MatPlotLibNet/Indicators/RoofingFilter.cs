// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Steps;
using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Indicators;

/// <summary>Ehlers' Roofing Filter — a band-pass implemented as a high-pass followed by a
/// SuperSmoother low-pass. Isolates the cyclic mid-frequency component after trend removal.
/// Reference: Ehlers (2014), <i>Predictive and Successful Indicators</i>, S&amp;C magazine.</summary>
public sealed class RoofingFilter : PriceIndicator<SignalResult>
{
    /// <summary>The <c>hpPeriod</c> the indicator is made with when the caller gives none.</summary>
    public const int DefaultHpPeriod = 48;

    /// <summary>The <c>lpPeriod</c> the indicator is made with when the caller gives none.</summary>
    public const int DefaultLpPeriod = 10;

    private readonly int _hpPeriod;
    private readonly int _lpPeriod;

    /// <summary>Creates a new Roofing Filter.</summary>
    /// <param name="prices">Price series.</param>
    /// <param name="hpPeriod">High-pass cutoff (default <see cref="DefaultHpPeriod"/>). Must be ≥ 4.</param>
    /// <param name="lpPeriod">Low-pass cutoff (default <see cref="DefaultLpPeriod"/>). Must be ≥ 2 and strictly &lt; <paramref name="hpPeriod"/>.</param>
    public RoofingFilter(double[] prices, int hpPeriod = DefaultHpPeriod, int lpPeriod = DefaultLpPeriod) : base(prices, Spec(hpPeriod, lpPeriod))
    {
        _hpPeriod = hpPeriod;
        _lpPeriod = lpPeriod;
    }

    internal static IndicatorSpec Spec(int hpPeriod, int lpPeriod)
    {
        hpPeriod.ThrowIfBelow(4, nameof(hpPeriod));
        lpPeriod.ThrowIfBelow(2, nameof(lpPeriod));
        if (lpPeriod >= hpPeriod)
            throw new ArgumentException(string.Invariant($"lpPeriod must be < hpPeriod (got {lpPeriod} vs {hpPeriod})."), nameof(lpPeriod));
        return IndicatorSpec.Of(2, string.Invariant($"Roof({hpPeriod}/{lpPeriod})"));
    }

    /// <inheritdoc />
    public override SignalResult Compute()
    {
        int n = ValuedBars;
        var result = new double[n];
        var highPass = new HighPassStep(_hpPeriod);
        var smoother = new SuperSmootherStep(_lpPeriod);
        for (int i = 0; i < n; i++) result[i] = smoother.Next(highPass.Next(Prices[i]));
        return result;
    }

    /// <inheritdoc />
    public override void Apply(Axes axes) => PlotLine(axes, Compute());
}
