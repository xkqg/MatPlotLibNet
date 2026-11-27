// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Indicators;

/// <summary>Rolling-window Shannon entropy of the Haar wavelet energy distribution. Low =
/// one band dominates (structured signal); high = energy spread across bands (complex / noisy).
/// Reference: Rosso et al. (2001), <i>Journal of Neuroscience Methods</i> 105(1).</summary>
public sealed class WaveletEntropy : PriceIndicator<SignalResult>
{
    /// <summary>The <c>window</c> the indicator is made with when the caller gives none.</summary>
    public const int DefaultWindow = 64;

    private readonly int _window;
    private readonly int _levels;

    /// <summary>Creates a new Wavelet Entropy indicator.</summary>
    /// <param name="prices">Price series.</param>
    /// <param name="window">Rolling window length; must be a power of 2 ≥ 4.</param>
    public WaveletEntropy(double[] prices, int window = DefaultWindow) : base(prices, Spec(window))
    {
        _window = window;
        _levels = int.Log2(window);
    }

    internal static IndicatorSpec Spec(int window)
    {
        window.ThrowIfNotPowerOfTwo(4, nameof(window));
        return IndicatorSpec.Of(window - 1, string.Invariant($"WEnt(W={window})"));
    }

    /// <inheritdoc />
    public override SignalResult Compute()
    {
        int outLen = ValueCount;
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

    /// <summary>The range the values of the indicator live in: its pane, and the pane of <see cref="Streaming.StreamingWaveletEntropy"/>, are pinned to it.</summary>
    internal static PaneRange Pane { get; } = PaneRange.Unit;

    /// <inheritdoc />
    public override void Apply(Axes axes)
    {
        PlotLine(axes, Compute());
        Pane.Pin(axes);
    }
}
