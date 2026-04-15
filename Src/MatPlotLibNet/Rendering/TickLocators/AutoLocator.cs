// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.TickLocators;

/// <summary>
/// Chooses aesthetically-spaced tick positions using the same nice-number algorithm as
/// <see cref="AxesRenderer.ComputeTickValues(double, double, int)"/>, making it available as an
/// <see cref="ITickLocator"/> strategy that can be swapped on any axis.
/// </summary>
public sealed class AutoLocator : ITickLocator
{
    private readonly int _targetCount;

    /// <summary>Initialises with the desired number of tick intervals.</summary>
    /// <param name="targetCount">Approximate number of major tick intervals; the algorithm rounds to the nearest nice number. Defaults to 5.</param>
    public AutoLocator(int targetCount = 5) => _targetCount = targetCount;

    /// <inheritdoc />
    public double[] Locate(double min, double max)
    {
        double range = max - min;
        if (range <= 0) return [min];

        double step = NiceStep(range, _targetCount);
        double first = Math.Ceiling(min / step) * step;
        var ticks = new List<double>();
        for (double t = first; t <= max + step * 0.01; t += step)
            ticks.Add(Math.Round(t, 10));

        return ticks.ToArray();
    }

    /// <summary>
    /// Expands a raw <c>[lo, hi]</c> range outward to the nearest nice-number tick boundary
    /// (matplotlib <c>MaxNLocator.view_limits</c> equivalent). Guarantees both endpoints land
    /// exactly on tick positions so axis limits and tick labels agree.
    /// </summary>
    public (double Lo, double Hi) ExpandToNiceBounds(double lo, double hi)
    {
        if (hi <= lo) return (lo, hi);
        double step = NiceStep(hi - lo, _targetCount);
        double newLo = Math.Floor(lo / step + 1e-9) * step;
        double newHi = Math.Ceiling(hi / step - 1e-9) * step;
        return (Math.Round(newLo, 10), Math.Round(newHi, 10));
    }

    /// <summary>
    /// Picks the nearest "nice" step using matplotlib's <c>MaxNLocator</c> ladder
    /// <c>{1, 2, 2.5, 5, 10}</c>. Kept static so both <see cref="Locate"/> and
    /// <see cref="ExpandToNiceBounds"/> share a single source of truth.
    /// </summary>
    private static double NiceStep(double range, int targetCount)
    {
        double rawStep = range / targetCount;
        double magnitude = Math.Pow(10, Math.Floor(Math.Log10(rawStep)));
        double normalized = rawStep / magnitude;
        return normalized switch
        {
            < 1.5  => magnitude,
            < 2.25 => 2 * magnitude,
            < 3.75 => 2.5 * magnitude,
            < 7.5  => 5 * magnitude,
            _      => 10 * magnitude
        };
    }
}
