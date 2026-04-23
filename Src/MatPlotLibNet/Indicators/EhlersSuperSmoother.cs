// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Ehlers;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Indicators;

/// <summary>Public wrapper for Ehlers' SuperSmoother — a two-pole Butterworth low-pass that
/// outperforms EMA for noise reduction with minimal lag and no ringing. Applicable to any
/// numerical series (price, indicator output, residuals, volume). Delegates to the internal
/// <see cref="Ehlers.SuperSmoother"/> helper from Tier 2c. Reference: Ehlers, J. F. (2013),
/// <i>Cycle Analytics for Traders</i>, Ch. 3.</summary>
public sealed class EhlersSuperSmoother : Indicator<SignalResult>
{
    private readonly double[] _input;
    private readonly int _period;

    /// <summary>Creates a new SuperSmoother indicator over any numerical series.</summary>
    /// <param name="input">Input series. Must be non-null (may be empty).</param>
    /// <param name="period">Butterworth cutoff period. Default 10. Must be ≥ 2.</param>
    public EhlersSuperSmoother(double[] input, int period = 10)
    {
        if (input is null)
            throw new ArgumentException("input is required.", nameof(input));
        if (period < 2)
            throw new ArgumentException($"period must be >= 2 (got {period}).", nameof(period));
        _input = input;
        _period = period;
        Label = $"SS({period})";
    }

    /// <inheritdoc />
    public override SignalResult Compute() =>
        ((ReadOnlySpan<double>)_input).SuperSmooth(_period);

    /// <inheritdoc />
    public override void Apply(Axes axes) => PlotSignal(axes, Compute(), warmup: 2);
}
