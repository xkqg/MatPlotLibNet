// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.TickLocators;

/// <summary>
/// Places ticks at exact multiples of a fixed <c>baseValue</c> within [min, max],
/// optionally shifted by an <c>offset</c>: ticks are at <c>offset + n * baseValue</c>.
/// Equivalent to matplotlib's <c>MultipleLocator</c>.
/// </summary>
public sealed class MultipleLocator : ITickLocator
{
    private readonly double _base;
    private readonly double _offset;

    /// <summary>Initialises with the tick spacing base value and an optional offset.</summary>
    /// <param name="baseValue">The interval between consecutive ticks.</param>
    /// <param name="offset">Shift applied to all tick positions (default 0). Use 0.5 to center ticks inside bar slots.</param>
    public MultipleLocator(double baseValue, double offset = 0) { _base = baseValue; _offset = offset; }

    /// <inheritdoc />
    public double[] Locate(double min, double max)
    {
        double first = _offset + Math.Ceiling((min - _offset) / _base) * _base;
        var ticks = new List<double>();
        for (double t = first; t <= max + _base * 1e-10; t += _base)
            ticks.Add(Math.Round(t, 10));
        return ticks.ToArray();
    }
}
