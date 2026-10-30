// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Steps;
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
        Label = $"CC({alpha.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)})";
    }

    /// <inheritdoc />
    public override SignalResult Compute()
    {
        int n = Prices.Length;
        if (n < 4) return Array.Empty<double>();
        var result = new double[n - 3];
        var step = new CyberCycleStep(_alpha);
        for (int i = 0; i < n; i++)
        {
            double cycle = step.Next(Prices[i]);
            if (i >= 3) result[i - 3] = cycle;
        }
        return result;
    }

    /// <inheritdoc />
    public override void Apply(Axes axes) => PlotSignal(axes, Compute(), warmup: 3);
}
