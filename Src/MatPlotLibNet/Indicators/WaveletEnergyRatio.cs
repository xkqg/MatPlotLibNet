// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Wavelet;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Indicators;

/// <summary>Rolling-window Haar wavelet energy ratio — fraction of total band energy at a
/// chosen detail level. Reveals which time scale currently dominates: level 0 = highest
/// frequency (noise), higher levels = longer-term structure. Reference: Rosso et al. (2001),
/// <i>Journal of Neuroscience Methods</i> 105(1); Mallat (2009), <i>A Wavelet Tour of Signal
/// Processing</i>.</summary>
public sealed class WaveletEnergyRatio : PriceIndicator<SignalResult>
{
    private readonly int _window;
    private readonly int _level;
    private readonly int _levels;

    /// <summary>Creates a new Wavelet Energy Ratio indicator.</summary>
    /// <param name="prices">Price series.</param>
    /// <param name="window">Rolling window length; must be a power of 2 ≥ 4.</param>
    /// <param name="level">Target detail level in <c>[0, log2(window))</c>.</param>
    public WaveletEnergyRatio(double[] prices, int window = 64, int level = 0) : base(prices)
    {
        if (window < 4 || (window & (window - 1)) != 0)
            throw new ArgumentException(
                $"window must be a power of 2 and >= 4 (got {window}).", nameof(window));
        int levels = (int)Math.Log2(window);
        if (level < 0 || level >= levels)
            throw new ArgumentException(
                $"level must be in [0, {levels}) (got {level}).", nameof(level));
        _window = window;
        _level = level;
        _levels = levels;
        Label = $"WER(W={window},L={level})";
    }

    /// <inheritdoc />
    public override SignalResult Compute()
    {
        int n = Prices.Length;
        if (n < _window) return Array.Empty<double>();

        int outLen = n - _window + 1;
        var result = new double[outLen];
        for (int t = 0; t < outLen; t++)
        {
            var window = new ReadOnlySpan<double>(Prices, t, _window);
            var energy = HaarDwt.EnergyPerLevel(window, _levels);
            double total = 0;
            for (int k = 0; k < energy.Length; k++) total += energy[k];
            result[t] = total > 0 ? energy[_level] / total : 0;
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
