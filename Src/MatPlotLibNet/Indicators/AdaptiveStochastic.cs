// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Ehlers;
using MatPlotLibNet.Indicators.Steps;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Indicators;

/// <summary>Ehlers' Adaptive Stochastic — classic Stochastic %K with a lookback adapted
/// to the dominant cycle period from the <see cref="HilbertDiscriminator"/>, then optionally
/// SuperSmoothed. Reference: Ehlers (2013) <i>Cycle Analytics for Traders</i>, Ch. 12.</summary>
public sealed class AdaptiveStochastic : CandleIndicator<SignalResult>
{
    private readonly int _smoothingPeriod;

    /// <summary>Creates a new Adaptive Stochastic indicator.</summary>
    /// <param name="bars">High, Low (must satisfy <c>L ≤ H</c>) and Close; built via <see cref="OhlcvBars.Hlc"/>.</param>
    /// <param name="smoothingPeriod">SuperSmoother period applied to raw %K. Default 3.
    /// Must be ≥ 1 (use 1 to skip smoothing).</param>
    public AdaptiveStochastic(OhlcvBars bars, int smoothingPeriod = 3)
        : base(bars)
    {
        if (smoothingPeriod < 1)
            throw new ArgumentException(
                $"smoothingPeriod must be >= 1 (got {smoothingPeriod}).", nameof(smoothingPeriod));
        for (int i = 0; i < bars.High.Length; i++)
        {
            if (bars.High[i] < bars.Low[i])
                throw new ArgumentException(
                    $"AdaptiveStochastic requires H >= L; bar {i} has H={bars.High[i]} < L={bars.Low[i]}.");
        }
        _smoothingPeriod = smoothingPeriod;
        Label = $"AdaptStoch({smoothingPeriod})";
    }

    /// <inheritdoc />
    public override SignalResult Compute()
    {
        int n = BarCount;
        if (n < 7) return Array.Empty<double>();

        var result = new double[n - 6];
        var step = new AdaptiveStochasticStep(_smoothingPeriod);
        for (int t = 0; t < n; t++)
        {
            double value = step.Next(High[t], Low[t], Close[t]);
            if (t >= 6) result[t - 6] = value;
        }

        return result;
    }

    /// <inheritdoc />
    public override void Apply(Axes axes)
    {
        PlotSignal(axes, Compute(), warmup: 6);
        axes.YAxis.Min = 0;
        axes.YAxis.Max = 100;
    }
}
