// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Ehlers;
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

        var (period, _, _, _) = ((ReadOnlySpan<double>)Prices).HilbertDiscriminate();

        // Seed the first 6 bars with the raw price so trend_full[i-1] is defined when the
        // adaptive-window loop kicks in at i=6. Pattern mirrors MAMA/FAMA.
        var trendFull = new double[n];
        for (int i = 0; i < 6; i++) trendFull[i] = Prices[i];

        for (int i = 6; i < n; i++)
        {
            int windowLen = (int)Math.Round(period[i]);
            // Fallback: period still settling (< 2) or window outruns available history.
            if (windowLen < 2 || windowLen > i + 1)
            {
                trendFull[i] = trendFull[i - 1];
                continue;
            }
            // Ehlers' linearly-decaying weights: price_t weighted by windowLen, price_{t-(windowLen-1)} by 1.
            double num = 0;
            double den = 0;
            for (int k = 0; k < windowLen; k++)
            {
                double w = windowLen - k;
                num += w * Prices[i - k];
                den += w;
            }
            trendFull[i] = den > 0 ? num / den : trendFull[i - 1];
        }

        int outLen = n - 6;
        var result = new double[outLen];
        Array.Copy(trendFull, 6, result, 0, outLen);
        return result;
    }

    /// <inheritdoc />
    public override void Apply(Axes axes) => PlotSignal(axes, Compute(), warmup: 6);
}
