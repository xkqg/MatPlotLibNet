// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Steps;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Indicators;

/// <summary>Ehlers' Laguerre RSI — RSI computed over a 4-stage Laguerre IIR filter cascade.
/// Zero lookback, minimal lag, output bounded to [0, 1]. Reference: Ehlers (2004),
/// <i>Cybernetic Analysis for Stocks and Futures</i>, Ch. 10.</summary>
public sealed class LaguerreRsi : PriceIndicator<SignalResult>
{
    private readonly double _alpha;

    /// <summary>Creates a new Laguerre RSI indicator.</summary>
    /// <param name="prices">Price series (typically close).</param>
    /// <param name="alpha">Smoothing parameter in (0, 1). Default 0.2.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="alpha"/> ∉ (0, 1).</exception>
    public LaguerreRsi(double[] prices, double alpha = 0.2) : base(prices)
    {
        if (alpha <= 0 || alpha >= 1)
            throw new ArgumentException($"alpha must be in (0, 1) (got {alpha}).", nameof(alpha));
        _alpha = alpha;
        Label = $"LagRSI({alpha.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)})";
    }

    /// <inheritdoc />
    public override SignalResult Compute()
    {
        int n = Prices.Length;
        if (n < 2) return Array.Empty<double>();
        var result = new double[n - 1];
        var step = new LaguerreRsiStep(_alpha);
        for (int i = 0; i < n; i++)
        {
            double value = step.Next(Prices[i]);
            if (i >= 1) result[i - 1] = value;
        }
        return result;
    }

    /// <inheritdoc />
    public override void Apply(Axes axes)
    {
        PlotSignal(axes, Compute(), warmup: 1);
        axes.YAxis.Min = 0;
        axes.YAxis.Max = 1;
    }
}
