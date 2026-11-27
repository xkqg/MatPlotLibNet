// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Rendering.TickLocators;

/// <summary>
/// Places ticks at exact multiples of a fixed <c>baseValue</c> within [min, max],
/// optionally shifted by an <c>offset</c>: ticks are at <c>offset + n * baseValue</c>.
/// Equivalent to matplotlib's <c>MultipleLocator</c>.
/// </summary>
public sealed class MultipleLocator : ITickLocator, IJsonDescribed<TickLocatorDto>, IRoundingLocator
{
    private readonly double _base;
    private readonly double _offset;

    /// <summary>Initialises with the tick spacing base value and an optional offset.</summary>
    /// <param name="baseValue">The interval between consecutive ticks.</param>
    /// <param name="offset">Shift applied to all tick positions (default 0). Use 0.5 to center ticks inside bar slots.</param>
    public MultipleLocator(double baseValue, double offset = 0) { _base = baseValue; _offset = offset; }

    /// <inheritdoc />
    TickLocatorDto IJsonDescribed<TickLocatorDto>.Describe() => new MultipleTickLocatorDto(_base, _offset);

    /// <inheritdoc />
    public double[] Locate(double min, double max)
    {
        return ArithmeticTicks.Multiples(min, max, _base, _offset).ToArray();
    }

    /// <inheritdoc />
    Range1D IRoundingLocator.RoundedOut(Range1D range) => MultiplesHolding(range, _base, _offset);

    /// <summary>The multiples of <paramref name="step"/>, shifted by <paramref name="offset"/>, at or beyond the ends of
    /// <paramref name="range"/>, as matplotlib's <c>MultipleLocator.view_limits</c> rounds under <c>round_numbers</c>:
    /// <c>_edge.le(dmin - offset) * step + offset</c> and <c>_edge.ge(dmax - offset) * step + offset</c>, a remainder within the
    /// tolerance of a whole step counted as one, then one more step each way when the two meet, and <c>_nonsingular</c>. The one rule
    /// a <see cref="MultipleLocator"/> and an axis' own tick spacing (<see cref="Models.TickConfig.Spacing"/>) round an axis by.</summary>
    /// <param name="range">The range, its low end below its high end.</param>
    /// <param name="step">The step, positive.</param>
    /// <param name="offset">The shift of every multiple.</param>
    /// <returns>The rounded range.</returns>
    internal static Range1D MultiplesHolding(Range1D range, double step, double offset)
    {
        var edge = TickEdge.Of(step, 0);
        double lo = edge.MultipleAtOrBelow(range.Lo - offset) + offset;
        double hi = edge.MultipleAtOrAbove(range.Hi - offset) + offset;
        if (lo == hi)
        {
            lo -= 1;
            hi += 1;
        }

        return new Range1D(lo, hi).NonsingularBy(expander: 0.001, tiny: 1e-15);
    }
}
