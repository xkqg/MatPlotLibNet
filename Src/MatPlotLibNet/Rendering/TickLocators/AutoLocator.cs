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

        double rawStep = range / _targetCount;
        double magnitude = Math.Pow(10, Math.Floor(Math.Log10(rawStep)));
        double normalized = rawStep / magnitude;

        double step = normalized switch
        {
            < 1.5 => magnitude,
            < 3.5 => 2 * magnitude,
            < 7.5 => 5 * magnitude,
            _     => 10 * magnitude
        };

        double first = Math.Ceiling(min / step) * step;
        var ticks = new List<double>();
        for (double t = first; t <= max + step * 0.01; t += step)
            ticks.Add(Math.Round(t, 10));

        return ticks.ToArray();
    }
}
