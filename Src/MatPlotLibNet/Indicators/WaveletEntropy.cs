// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Wavelet;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Indicators;

/// <summary>Rolling-window Shannon entropy of the Haar wavelet energy distribution. Low =
/// one band dominates (structured signal); high = energy spread across bands (complex / noisy).
/// Reference: Rosso et al. (2001), <i>Journal of Neuroscience Methods</i> 105(1).</summary>
public sealed class WaveletEntropy : PriceIndicator<SignalResult>
{
    private readonly int _window;
    private readonly int _levels;

    /// <summary>Creates a new Wavelet Entropy indicator.</summary>
    /// <param name="prices">Price series.</param>
    /// <param name="window">Rolling window length; must be a power of 2 ≥ 4.</param>
    public WaveletEntropy(double[] prices, int window = 64) : base(prices)
    {
        if (window < 4 || (window & (window - 1)) != 0)
            throw new ArgumentException(
                $"window must be a power of 2 and >= 4 (got {window}).", nameof(window));
        _window = window;
        _levels = (int)Math.Log2(window);
        Label = $"WEnt(W={window})";
    }

    /// <inheritdoc />
    public override SignalResult Compute()
    {
        int n = Prices.Length;
        if (n < _window) return Array.Empty<double>();

        int outLen = n - _window + 1;
        var result = new double[outLen];
        double normalizer = Math.Log(_levels + 1);

        for (int t = 0; t < outLen; t++)
        {
            var window = new ReadOnlySpan<double>(Prices, t, _window);
            var energy = HaarDwt.EnergyPerLevel(window, _levels);

            double total = 0;
            for (int k = 0; k < energy.Length; k++) total += energy[k];
            if (total == 0)
            {
                result[t] = 0;
                continue;
            }

            double h = 0;
            for (int k = 0; k < energy.Length; k++)
            {
                double p = energy[k] / total;
                if (p > 0) h -= p * Math.Log(p);
            }
            result[t] = h / normalizer;
        }
        return result;
    }

    /// <inheritdoc />
    public override void Apply(Axes axes)
    {
        PlotSignal(axes, Compute(), warmup: _window - 1);
        axes.YAxis.Min = 0;
        axes.YAxis.Max = 1;
    }
}
