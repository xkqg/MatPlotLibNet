// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Steps;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Indicators;

/// <summary>Ehlers' Instantaneous Trendline (iTrend) — an adaptive linearly-weighted moving
/// average whose window length IS the Hilbert-derived dominant cycle. Follows trends with
/// minimal lag in trending regimes and smooths noise in ranging regimes. Reuses
/// the Hilbert transform of the cycle indicators. Reference: Ehlers, J. F. (2001),
/// <i>Rocket Science for Traders</i>, Ch. 16.</summary>
public sealed class EhlersITrend : PriceIndicator<SignalResult>
{
    /// <summary>Creates a new iTrend indicator.</summary>
    /// <param name="prices">Price series (typically close).</param>
    public EhlersITrend(double[] prices) : base(prices, Spec())
    {
    }

    internal static IndicatorSpec Spec() => IndicatorSpec.Of(6, "iTrend");

    /// <inheritdoc />
    public override SignalResult Compute() => FoldPrices(new EhlersITrendStep());

    /// <inheritdoc />
    public override void Apply(Axes axes) => PlotLine(axes, Compute());
}
