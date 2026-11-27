// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Steps;
using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Indicators;

/// <summary>Ehlers' Laguerre RSI — RSI computed over a 4-stage Laguerre IIR filter cascade.
/// Zero lookback, minimal lag, output bounded to [0, 1]. Reference: Ehlers (2004),
/// <i>Cybernetic Analysis for Stocks and Futures</i>, Ch. 10.</summary>
public sealed class LaguerreRsi : PriceIndicator<SignalResult>
{
    /// <summary>The <c>alpha</c> the indicator is made with when the caller gives none.</summary>
    public const double DefaultAlpha = 0.2;

    private readonly double _alpha;

    /// <summary>Creates a new Laguerre RSI indicator.</summary>
    /// <param name="prices">Price series (typically close).</param>
    /// <param name="alpha">Smoothing parameter in (0, 1). Default <see cref="DefaultAlpha"/>.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="alpha"/> ∉ (0, 1).</exception>
    public LaguerreRsi(double[] prices, double alpha = DefaultAlpha) : base(prices, Spec(alpha))
    {
        _alpha = alpha;
    }

    internal static IndicatorSpec Spec(double alpha)
    {
        alpha.ThrowIfNotBetween(0, 1, nameof(alpha));
        return IndicatorSpec.Of(1, $"LagRSI({alpha.InvariantText("0.00")})");
    }

    /// <inheritdoc />
    public override SignalResult Compute() => FoldPrices(new LaguerreRsiStep(_alpha));

    /// <summary>The range the values of the indicator live in: its pane, and the pane of <see cref="Streaming.StreamingLaguerreRsi"/>, are pinned to it.</summary>
    internal static PaneRange Pane { get; } = PaneRange.Unit;

    /// <inheritdoc />
    public override void Apply(Axes axes)
    {
        PlotLine(axes, Compute());
        Pane.Pin(axes);
    }
}
