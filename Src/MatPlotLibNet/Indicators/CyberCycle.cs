// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Steps;
using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Indicators;

/// <summary>Ehlers' Cyber Cycle — a second-order IIR that emphasizes the dominant cycle
/// frequency while removing trend. Zero crossings = cycle mid-line, peaks = cycle tops.
/// Reference: Ehlers (2002) <i>Cyber Cycle Indicator</i>, S&amp;C magazine;
/// <i>Cybernetic Analysis for Stocks and Futures</i> (2004) Ch. 6.</summary>
public sealed class CyberCycle : PriceIndicator<SignalResult>
{
    /// <summary>The <c>alpha</c> the indicator is made with when the caller gives none.</summary>
    public const double DefaultAlpha = 0.07;

    private readonly double _alpha;

    /// <summary>Creates a new Cyber Cycle indicator.</summary>
    /// <param name="prices">Price series (typically close).</param>
    /// <param name="alpha">Smoothing constant in (0, 1). Default <see cref="DefaultAlpha"/> (≈15-bar cycles).</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="alpha"/> ∉ (0, 1).</exception>
    public CyberCycle(double[] prices, double alpha = DefaultAlpha) : base(prices, Spec(alpha))
    {
        _alpha = alpha;
    }

    internal static IndicatorSpec Spec(double alpha)
    {
        alpha.ThrowIfNotBetween(0, 1, nameof(alpha));
        return IndicatorSpec.Of(3, $"CC({alpha.InvariantText("0.00")})");
    }

    /// <inheritdoc />
    public override SignalResult Compute() => FoldPrices(new CyberCycleStep(_alpha));

    /// <inheritdoc />
    public override void Apply(Axes axes) => PlotLine(axes, Compute());
}
