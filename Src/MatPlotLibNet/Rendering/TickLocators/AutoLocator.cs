// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Rendering.TickLocators;

/// <summary>
/// Chooses evenly spaced tick positions on the steps 1, 2, 2.5, 5 and 10 times a power of ten, taking the step nearest the range
/// over the target count. An axis without a locator of its own is placed by it under the library's own themes
/// (the matplotlib themes place by matplotlib's own locator), and it can be set on any axis like any
/// other <see cref="ITickLocator"/>.
/// </summary>
public sealed class AutoLocator : ITickLocator, IJsonDescribed<TickLocatorDto>, IRoundingLocator
{
    private readonly int _targetCount;

    /// <summary>The plot length, in pixels, from which an axis asks for the full 8 tick intervals; a shorter plot asks for
    /// fewer (<see cref="ForLength"/>). The layout, which does not know how long a plot turns out to be, measures the ticks
    /// of a stacked Y axis as for a plot of this length, the densest they can be.</summary>
    internal const double FullDensityLength = 240;

    /// <summary>Initialises with the desired number of tick intervals.</summary>
    /// <param name="targetCount">Approximate number of major tick intervals; the algorithm rounds to the nearest nice number. Defaults to 5.</param>
    public AutoLocator(int targetCount = 5) => _targetCount = targetCount;

    /// <summary>The default placement of an axis' ticks along a plot <paramref name="plotPixels"/> long: 8 intervals from
    /// <see cref="FullDensityLength"/> up, so normal-sized plots keep the ticks they have always had; a tick per 30 px, held
    /// between 2 and 6, along a shorter plot (a sparkline, a dashboard row), where the labels would otherwise overlap. Every
    /// axis without a locator or a spacing of its own is placed by it.</summary>
    /// <param name="plotPixels">The plot's length along the axis, in pixels.</param>
    /// <returns>The locator.</returns>
    internal static AutoLocator ForLength(double plotPixels) =>
        new(plotPixels >= FullDensityLength ? 8 : Math.Clamp((int)Math.Round(plotPixels / 30), 2, 6));

    /// <inheritdoc />
    TickLocatorDto IJsonDescribed<TickLocatorDto>.Describe() => new AutoTickLocatorDto(_targetCount);

    /// <inheritdoc />
    public double[] Locate(double min, double max)
    {
        double range = max - min;
        if (range <= 0) return [min];

        // The one ladder of ticks: from the multiple at or above the low end, each worked out from its index, in one array of
        // exactly its size.
        return ArithmeticTicks.Over(min, max, NiceStep(range, _targetCount)).ToArray();
    }

    /// <summary>Expands a raw <c>[lo, hi]</c> range outward to the multiples of the step <see cref="Locate"/> takes over it, so
    /// both endpoints land on tick positions and the axis limits and tick labels agree; a bound within matplotlib's edge tolerance
    /// (a ten-billionth of a step) of a multiple counts as on it. The step is this locator's nearest one, not the one matplotlib's
    /// <c>MaxNLocator.view_limits</c> takes (the smallest at or above the range over the bins).</summary>
    /// <param name="lo">Raw lower bound of the data range.</param>
    /// <param name="hi">Raw upper bound of the data range.</param>
    /// <returns>A <see cref="MinMaxRange"/> widened to the nearest nice-number step. When
    /// <paramref name="hi"/> ≤ <paramref name="lo"/> the input is returned unchanged (degenerate range).</returns>
    public MinMaxRange ExpandToNiceBounds(double lo, double hi) =>
        hi <= lo ? new(lo, hi) : ArithmeticTicks.Holding(lo, hi, NiceStep(hi - lo, _targetCount));

    /// <inheritdoc />
    /// <remarks>The multiples of its step that hold the range (<see cref="ExpandToNiceBounds"/>), as matplotlib's
    /// <c>AutoLocator</c>, a <c>MaxNLocator</c>, ends an axis under <c>round_numbers</c> at the ends of the ticks it lays.</remarks>
    Range1D IRoundingLocator.RoundedOut(Range1D range)
    {
        var (lo, hi) = ExpandToNiceBounds(range.Lo, range.Hi);
        return new Range1D(lo, hi);
    }

    /// <summary>
    /// Picks the step nearest the range over the target count among 1, 2, 2.5, 5 and 10 times a power of ten (the steps of
    /// matplotlib's default <c>AutoLocator</c>, which takes the smallest at or above instead). Kept static so both
    /// <see cref="Locate"/> and <see cref="ExpandToNiceBounds"/> share a single source of truth.
    /// </summary>
    private static double NiceStep(double range, int targetCount)
    {
        double rawStep = range / targetCount;
        double magnitude = rawStep.DecadeAtOrBelow();
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
