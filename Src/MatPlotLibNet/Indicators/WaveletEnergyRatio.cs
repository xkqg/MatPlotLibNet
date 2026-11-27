// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Indicators;

/// <summary>Rolling-window Haar wavelet energy ratio — fraction of total band energy at a
/// chosen detail level. Reveals which time scale currently dominates: level 0 = highest
/// frequency (noise), higher levels = longer-term structure. Reference: Rosso et al. (2001),
/// <i>Journal of Neuroscience Methods</i> 105(1); Mallat (2009), <i>A Wavelet Tour of Signal
/// Processing</i>.</summary>
public sealed class WaveletEnergyRatio : PriceIndicator<SignalResult>
{
    /// <summary>The <c>window</c> the indicator is made with when the caller gives none.</summary>
    public const int DefaultWindow = 64;

    /// <summary>The <c>level</c> the indicator is made with when the caller gives none.</summary>
    public const int DefaultLevel = 0;

    private readonly int _window;
    private readonly int _level;
    private readonly int _levels;

    /// <summary>Creates a new Wavelet Energy Ratio indicator.</summary>
    /// <param name="prices">Price series.</param>
    /// <param name="window">Rolling window length; must be a power of 2 ≥ 4.</param>
    /// <param name="level">Target detail level in <c>[0, log2(window))</c>.</param>
    public WaveletEnergyRatio(double[] prices, int window = DefaultWindow, int level = DefaultLevel) : base(prices, Spec(window, level))
    {
        int levels = int.Log2(window);
        _window = window;
        _level = level;
        _levels = levels;
    }

    internal static IndicatorSpec Spec(int window, int level)
    {
        window.ThrowIfNotPowerOfTwo(4, nameof(window));
        level.ThrowIfNotIn(0, int.Log2(window) - 1, nameof(level));
        return IndicatorSpec.Of(window - 1, string.Invariant($"WER(W={window},L={level})"));
    }

    /// <inheritdoc />
    public override SignalResult Compute()
    {
        int outLen = ValueCount;
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

    /// <summary>The range the values of the indicator live in: its pane, and the pane of <see cref="Streaming.StreamingWaveletEnergyRatio"/>, are pinned to it.</summary>
    internal static PaneRange Pane { get; } = PaneRange.Unit;

    /// <inheritdoc />
    public override void Apply(Axes axes)
    {
        PlotLine(axes, Compute());
        Pane.Pin(axes);
    }
}
