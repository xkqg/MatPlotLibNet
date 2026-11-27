// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Rendering.TickLocators;

/// <summary>Generates tick positions for a symmetric logarithmic scale.
/// Places ticks at powers of 10 outside the linear threshold, and at regular intervals
/// within the linear region. Symmetric around zero.</summary>
public sealed class SymlogLocator : ITickLocator, IJsonDescribed<TickLocatorDto>, IRoundingLocator, IMinorTickLocator
{
    private readonly double _linthresh;

    /// <summary>Creates a symlog tick locator.</summary>
    /// <param name="linthresh">Linear threshold — ticks within this range are evenly spaced.</param>
    public SymlogLocator(double linthresh = SymlogTransform.DefaultLinThresh) => _linthresh = linthresh;

    /// <inheritdoc />
    /// <remarks>None: matplotlib's symmetric-log scale places its minor ticks with a <c>SymmetricalLogLocator</c> of the scale's
    /// <c>subs</c>, which are <c>None</c> unless given, so it lays the powers its major locator lays, and each is left out for the
    /// major tick standing there.</remarks>
    double[] IMinorTickLocator.MinorsOver(Range1D view, double[] majors, double tickSpace, Styling.Theme theme) => [];

    /// <inheritdoc />
    TickLocatorDto IJsonDescribed<TickLocatorDto>.Describe() => new SymlogTickLocatorDto(_linthresh);

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

        // Filter to the range, give or take the reach of a ladder (ArithmeticTicks.Reach) of its width past either end, and sort
        double slack = (max - min) * ArithmeticTicks.Reach;
        ticks.RemoveAll(t => t < min - slack || t > max + slack);
        ticks.Sort();

        // A range between two decades of the threshold holds no candidate at all.
        if (ticks.Count == 0)
            return [];

        // Deduplicate
        var result = new List<double> { ticks[0] };
        for (int i = 1; i < ticks.Count; i++)
            if (Math.Abs(ticks[i] - ticks[i - 1]) > 1e-10)
                result.Add(ticks[i]);

        return result.ToArray();
    }

    /// <inheritdoc />
    Range1D IRoundingLocator.RoundedOut(Range1D range) => DecadesHolding(range);

    /// <summary>The decades on either side of zero at or beyond the ends of <paramref name="range"/>, as matplotlib's
    /// <c>SymmetricalLogLocator.view_limits</c> rounds under <c>round_numbers</c>: the one rule a symmetric-log axis is rounded out by,
    /// whether it names this locator or its scale brings it. Two ends that round to one decade, which matplotlib widens by one
    /// each way, cannot occur: a range that is wider than nothing has its low end below its high one, and so its decades too.</summary>
    /// <param name="range">The range, its low end below its high end.</param>
    /// <returns>The rounded range.</returns>
    internal static Range1D DecadesHolding(Range1D range) => new(range.Lo.SignedDecadeAtOrBelow(), range.Hi.SignedDecadeAtOrAbove());
}
