// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.TickLocators;

/// <summary>Generates tick positions for a symmetric logarithmic scale.
/// Places ticks at powers of 10 outside the linear threshold, and at regular intervals
/// within the linear region. Symmetric around zero.</summary>
public sealed class SymlogLocator : ITickLocator
{
    private readonly double _linthresh;

    /// <summary>Creates a symlog tick locator.</summary>
    /// <param name="linthresh">Linear threshold — ticks within this range are evenly spaced.</param>
    public SymlogLocator(double linthresh = 1.0) => _linthresh = linthresh;

    /// <inheritdoc />
    public double[] Locate(double min, double max)
    {
        var ticks = new List<double>();

        // Always include 0
        ticks.Add(0);

        // Linear ticks within [-linthresh, linthresh]
        if (_linthresh >= 1)
        {
            ticks.Add(-_linthresh);
            ticks.Add(_linthresh);
        }

        // Log ticks: powers of 10 on both sides
        for (int exp = 0; exp <= 15; exp++)
        {
            double v = _linthresh * Math.Pow(10, exp);
            if (v > max * 1.1) break;
            if (v >= _linthresh) { ticks.Add(v); ticks.Add(-v); }
        }

        // Filter to range and sort
        ticks.RemoveAll(t => t < min - (max - min) * 0.01 || t > max + (max - min) * 0.01);
        ticks.Sort();

        // Deduplicate
        var result = new List<double> { ticks[0] };
        for (int i = 1; i < ticks.Count; i++)
            if (Math.Abs(ticks[i] - ticks[i - 1]) > 1e-10)
                result.Add(ticks[i]);

        return result.ToArray();
    }
}
