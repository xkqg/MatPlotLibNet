// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Steps;
using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Indicators;

/// <summary>Ehlers' Adaptive Stochastic — classic Stochastic %K with a lookback adapted
/// to the dominant cycle period from the Hilbert transform, then optionally
/// SuperSmoothed. Reference: Ehlers (2013) <i>Cycle Analytics for Traders</i>, Ch. 12.</summary>
public sealed class AdaptiveStochastic : CandleIndicator<SignalResult>
{
    /// <summary>The <c>smoothingPeriod</c> the indicator is made with when the caller gives none.</summary>
    public const int DefaultSmoothingPeriod = 3;

    private readonly int _smoothingPeriod;

    /// <summary>Creates a new Adaptive Stochastic indicator.</summary>
    /// <param name="bars">High, Low (must satisfy <c>L ≤ H</c>) and Close; built via <see cref="OhlcvBars.Hlc"/>.</param>
    /// <param name="smoothingPeriod">SuperSmoother period applied to raw %K. Default <see cref="DefaultSmoothingPeriod"/>.
    /// Must be ≥ 1 (use 1 to skip smoothing).</param>
    public AdaptiveStochastic(OhlcvBars bars, int smoothingPeriod = DefaultSmoothingPeriod)
        : base(bars, Spec(smoothingPeriod))
    {
        bars.ThrowIfHighBelowLow(nameof(AdaptiveStochastic));
        _smoothingPeriod = smoothingPeriod;
    }

    internal static IndicatorSpec Spec(int smoothingPeriod) => IndicatorSpec.OfCheckedWindow("AdaptStoch", smoothingPeriod, 1, 6);

    /// <inheritdoc />
    public override SignalResult Compute() => FoldHlc(new AdaptiveStochasticStep(_smoothingPeriod));

    /// <summary>The range the values of the indicator live in: its pane, and the pane of <see cref="Streaming.StreamingAdaptiveStochastic"/>, are pinned to it.</summary>
    internal static PaneRange Pane { get; } = PaneRange.Percent;

    /// <inheritdoc />
    public override void Apply(Axes axes)
    {
        PlotLine(axes, Compute());
        Pane.Pin(axes);
    }
}
