// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Indicators.Steps;

namespace MatPlotLibNet.Indicators;

/// <summary>Public wrapper for Ehlers' SuperSmoother — a two-pole Butterworth low-pass that
/// outperforms EMA for noise reduction with minimal lag and no ringing. Applicable to any
/// numerical series (price, indicator output, residuals, volume). Delegates to the internal
/// <see cref="Indicators.EhlersSuperSmoother"/> helper from Tier 2c. Reference: Ehlers, J. F. (2013),
/// <i>Cycle Analytics for Traders</i>, Ch. 3.</summary>
public sealed class EhlersSuperSmoother : Indicator<SignalResult>
{
    /// <summary>The <c>period</c> the indicator is made with when the caller gives none.</summary>
    public const int DefaultPeriod = 10;

    private readonly double[] _input;
    private readonly int _period;

    /// <inheritdoc />
    private protected override int BarsIn => _input.Length;

    /// <summary>Creates a new SuperSmoother indicator over any numerical series.</summary>
    /// <param name="input">Input series. Must be non-null (may be empty).</param>
    /// <param name="period">Butterworth cutoff period. Default <see cref="DefaultPeriod"/>. Must be ≥ 2.</param>
    public EhlersSuperSmoother(double[] input, int period = DefaultPeriod) : base(Spec(period))
    {
        if (input is null)
            throw new ArgumentException("input is required.", nameof(input));
        _input = input;
        _period = period;
    }

    internal static IndicatorSpec Spec(int period) => IndicatorSpec.OfCheckedWindow("SS", period, 2, 0);

    /// <inheritdoc />
    public override SignalResult Compute() =>
        SuperSmooth(((ReadOnlySpan<double>)_input), _period);

    /// <inheritdoc />
    public override void Apply(Axes axes) => PlotLine(axes, Compute());

    /// <summary>Applies the SuperSmoother filter with cutoff <paramref name="period"/>. The first 2 output bars equal the
    /// corresponding input bars.</summary>
    /// <exception cref="ArgumentException">Thrown when <paramref name="period"/> &lt; 2.</exception>
    internal static double[] SuperSmooth(ReadOnlySpan<double> signal, int period)
    {
        period.ThrowIfBelow(2, nameof(period));
        int n = signal.Length;
        if (n == 0) return Array.Empty<double>();

        var result = new double[n];
        var step = new SuperSmootherStep(period);
        for (int i = 0; i < n; i++) result[i] = step.Next(signal[i]);
        return result;
    }
}
