// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.TickLocators;

/// <summary>
/// Places ticks at powers of 10 within [min, max].
/// Equivalent to matplotlib's <c>LogLocator</c> with base=10.
/// Requires min &gt; 0.
/// </summary>
/// <remarks>
/// When the range spans less than a full decade (e.g. min=1, max=5), no exact power of 10 may
/// fall within [min, max]. In that case the lower decade boundary (10^⌊log10(min)⌋) is returned
/// if it lies within the range; otherwise <c>min</c> itself is used as a fallback tick.
/// </remarks>
public sealed class LogLocator : ITickLocator
{
    /// <inheritdoc />
    public double[] Locate(double min, double max)
    {
        if (min <= 0) min = 1e-10;
        if (max <= min) return [min];

        int startExp = (int)Math.Floor(Math.Log10(min));
        int endExp   = (int)Math.Ceiling(Math.Log10(max));

        var ticks = new List<double>();
        for (int exp = startExp; exp <= endExp; exp++)
        {
            double t = Math.Pow(10, exp);
            if (t >= min && t <= max)
                ticks.Add(t);
        }

        // If nothing fell in range (e.g. min=1, max=5 — both within decade 0..1)
        // include the lower decade boundary if valid
        if (ticks.Count == 0)
            ticks.Add(min);

        return ticks.ToArray();
    }
}
