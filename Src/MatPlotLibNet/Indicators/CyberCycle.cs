// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Indicators;

/// <summary>Ehlers' Cyber Cycle — a second-order IIR that emphasizes the dominant cycle
/// frequency while removing trend. Zero crossings = cycle mid-line, peaks = cycle tops.
/// Reference: Ehlers (2002) <i>Cyber Cycle Indicator</i>, S&amp;C magazine;
/// <i>Cybernetic Analysis for Stocks and Futures</i> (2004) Ch. 6.</summary>
public sealed class CyberCycle : PriceIndicator<SignalResult>
{
    private readonly double _alpha;

    /// <summary>Creates a new Cyber Cycle indicator.</summary>
    /// <param name="prices">Price series (typically close).</param>
    /// <param name="alpha">Smoothing constant in (0, 1). Default 0.07 (≈15-bar cycles).</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="alpha"/> ∉ (0, 1).</exception>
    public CyberCycle(double[] prices, double alpha = 0.07) : base(prices)
    {
        if (alpha <= 0 || alpha >= 1)
            throw new ArgumentException($"alpha must be in (0, 1) (got {alpha}).", nameof(alpha));
        _alpha = alpha;
        Label = $"CC({alpha:0.00})";
    }

    /// <inheritdoc />
    public override SignalResult Compute()
    {
        int n = Prices.Length;
        if (n < 4) return Array.Empty<double>();

        var smooth = new double[n];
        var cc = new double[n];
        for (int i = 0; i < n; i++)
        {
            smooth[i] = i >= 3
                ? (Prices[i] + 2 * Prices[i - 1] + 2 * Prices[i - 2] + Prices[i - 3]) / 6.0
                : Prices[i];
        }

        double a = _alpha;
        double oneMinusHalfA = 1 - 0.5 * a;
        double sqHalf = oneMinusHalfA * oneMinusHalfA;
        double twoOneMinusA = 2 * (1 - a);
        double sqOneMinusA = (1 - a) * (1 - a);

        for (int i = 3; i < n; i++)
        {
            cc[i] = sqHalf * (smooth[i] - 2 * smooth[i - 1] + smooth[i - 2])
                  + twoOneMinusA * cc[i - 1]
                  - sqOneMinusA * cc[i - 2];
        }

        int outLen = n - 3;
        var result = new double[outLen];
        Array.Copy(cc, 3, result, 0, outLen);
        return result;
    }

    /// <inheritdoc />
    public override void Apply(Axes axes) => PlotSignal(axes, Compute(), warmup: 3);
}
