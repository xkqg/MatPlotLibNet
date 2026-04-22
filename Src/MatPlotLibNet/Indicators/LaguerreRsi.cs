// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

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
        Label = $"LagRSI({alpha:0.00})";
    }

    /// <inheritdoc />
    public override SignalResult Compute()
    {
        int n = Prices.Length;
        if (n < 2) return Array.Empty<double>();

        double a = _alpha;
        double oneMinusA = 1 - a;

        // Initial state: all L_k seeded to p[0] per Ehlers.
        double l0 = Prices[0], l1 = Prices[0], l2 = Prices[0], l3 = Prices[0];

        int outLen = n - 1;
        var result = new double[outLen];
        for (int i = 1; i < n; i++)
        {
            double p = Prices[i];
            double prevL0 = l0, prevL1 = l1, prevL2 = l2, prevL3 = l3;
            l0 = a * p + oneMinusA * prevL0;
            l1 = -oneMinusA * l0 + prevL0 + oneMinusA * prevL1;
            l2 = -oneMinusA * l1 + prevL1 + oneMinusA * prevL2;
            l3 = -oneMinusA * l2 + prevL2 + oneMinusA * prevL3;

            double cu = 0, cd = 0;
            if (l0 >= l1) cu += l0 - l1; else cd += l1 - l0;
            if (l1 >= l2) cu += l1 - l2; else cd += l2 - l1;
            if (l2 >= l3) cu += l2 - l3; else cd += l3 - l2;

            double sum = cu + cd;
            result[i - 1] = sum > 0 ? cu / sum : 0;
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
