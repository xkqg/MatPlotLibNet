// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.TickLocators;

/// <summary>
/// Places ticks at exact multiples of a fixed <c>baseValue</c> within [min, max].
/// Equivalent to matplotlib's <c>MultipleLocator</c>.
/// </summary>
public sealed class MultipleLocator : ITickLocator
{
    private readonly double _base;

    /// <summary>Initialises with the tick spacing base value.</summary>
    /// <param name="baseValue">The interval between consecutive ticks; ticks are placed at multiples of this value.</param>
    public MultipleLocator(double baseValue) => _base = baseValue;

    /// <inheritdoc />
    public double[] Locate(double min, double max)
    {
        double first = Math.Ceiling(min / _base) * _base;
        var ticks = new List<double>();
        for (double t = first; t <= max + _base * 1e-10; t += _base)
            ticks.Add(Math.Round(t, 10));
        return ticks.ToArray();
    }
}
