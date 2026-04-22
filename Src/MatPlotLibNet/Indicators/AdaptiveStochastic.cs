// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Ehlers;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Indicators;

/// <summary>Ehlers' Adaptive Stochastic — classic Stochastic %K with a lookback adapted
/// to the dominant cycle period from the <see cref="HilbertDiscriminator"/>, then optionally
/// SuperSmoothed. Reference: Ehlers (2013) <i>Cycle Analytics for Traders</i>, Ch. 12.</summary>
public sealed class AdaptiveStochastic : CandleIndicator<SignalResult>
{
    private const int MinCycleBars = 6;

    private readonly int _smoothingPeriod;

    /// <summary>Creates a new Adaptive Stochastic indicator.</summary>
    /// <param name="high">High prices.</param>
    /// <param name="low">Low prices (must satisfy <c>L ≤ H</c>).</param>
    /// <param name="close">Close prices.</param>
    /// <param name="smoothingPeriod">SuperSmoother period applied to raw %K. Default 3.
    /// Must be ≥ 1 (use 1 to skip smoothing).</param>
    public AdaptiveStochastic(double[] high, double[] low, double[] close, int smoothingPeriod = 3)
        : base([], high, low, close, [])
    {
        if (high.Length != low.Length || low.Length != close.Length)
            throw new ArgumentException("HLC arrays must have equal length.");
        if (smoothingPeriod < 1)
            throw new ArgumentException(
                $"smoothingPeriod must be >= 1 (got {smoothingPeriod}).", nameof(smoothingPeriod));
        for (int i = 0; i < high.Length; i++)
        {
            if (high[i] < low[i])
                throw new ArgumentException(
                    $"AdaptiveStochastic requires H >= L; bar {i} has H={high[i]} < L={low[i]}.");
        }
        _smoothingPeriod = smoothingPeriod;
        Label = $"AdaptStoch({smoothingPeriod})";
    }

    /// <inheritdoc />
    public override SignalResult Compute()
    {
        int n = BarCount;
        if (n < 7) return Array.Empty<double>();

        // Typical price for Hilbert input — Ehlers' recommendation for stochastics.
        var typical = new double[n];
        for (int t = 0; t < n; t++) typical[t] = (High[t] + Low[t] + Close[t]) / 3.0;

        var hilbert = ((ReadOnlySpan<double>)typical).HilbertDiscriminate();

        int outLen = n - 6;
        var rawK = new double[outLen];
        for (int w = 0; w < outLen; w++)
        {
            int t = 6 + w;
            // HilbertDiscriminator caps Period at 50, so cycleBars = Period/2 ≤ 25.
            // Only the lower-clamp branch can fire.
            int cycleBars = (int)Math.Round(hilbert.Period[t] / 2.0);
            if (cycleBars < MinCycleBars) cycleBars = MinCycleBars;

            int start = Math.Max(0, t - cycleBars + 1);
            double hh = High[start], ll = Low[start];
            for (int k = start + 1; k <= t; k++)
            {
                if (High[k] > hh) hh = High[k];
                if (Low[k] < ll) ll = Low[k];
            }
            double range = hh - ll;
            rawK[w] = range == 0 ? 50.0 : 100.0 * (Close[t] - ll) / range;
        }

        if (_smoothingPeriod < 2) return rawK;
        return ((ReadOnlySpan<double>)rawK).SuperSmooth(_smoothingPeriod);
    }

    /// <inheritdoc />
    public override void Apply(Axes axes)
    {
        PlotSignal(axes, Compute(), warmup: 6);
        axes.YAxis.Min = 0;
        axes.YAxis.Max = 100;
    }
}
