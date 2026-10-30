// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Ehlers;
using MatPlotLibNet.Indicators.Steps;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Indicators;

/// <summary>Ehlers' Instantaneous Trendline (iTrend) — an adaptive linearly-weighted moving
/// average whose window length IS the Hilbert-derived dominant cycle. Follows trends with
/// minimal lag in trending regimes and smooths noise in ranging regimes. Reuses
/// <see cref="Ehlers.HilbertDiscriminator"/> (Tier 2c). Reference: Ehlers, J. F. (2001),
/// <i>Rocket Science for Traders</i>, Ch. 16.</summary>
public sealed class EhlersITrend : PriceIndicator<SignalResult>
{
    /// <summary>Creates a new iTrend indicator.</summary>
    /// <param name="prices">Price series (typically close).</param>
    public EhlersITrend(double[] prices) : base(prices)
    {
        Label = "iTrend";
    }

    /// <inheritdoc />
    public override SignalResult Compute()
    {
        int n = Prices.Length;
        if (n < 7) return Array.Empty<double>();

        var result = new double[n - 6];
        var step = new EhlersITrendStep();
        for (int i = 0; i < n; i++)
        {
            double trend = step.Next(Prices[i]);
            if (i >= 6) result[i - 6] = trend;
        }

        return result;
    }

    /// <inheritdoc />
    public override void Apply(Axes axes) => PlotSignal(axes, Compute(), warmup: 6);
}
