// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.TickLocators;

/// <summary>How the renderers ask an <see cref="ITickLocator"/> for the ticks of a drawn range.</summary>
internal static class TickLocatorExtensions
{
    /// <summary>The ticks of the interval between <paramref name="from"/> and <paramref name="to"/>, whichever way the axis
    /// runs. A locator counts from its minimum up to its maximum, so a reversed limit handed over as drawn
    /// (<c>SetYLim(5.5, 0.5)</c>) gave the single tick 5.5; the ticks of a reversed axis are the ticks of the same
    /// interval (<see cref="Range1D.Interval"/>), and the transform draws each where its value is.</summary>
    /// <param name="locator">The locator that places the ticks.</param>
    /// <param name="from">One end of the drawn range (the axis minimum as set, possibly the larger value).</param>
    /// <param name="to">The other end of the drawn range.</param>
    /// <returns>The locator's ticks for the interval, ascending.</returns>
    internal static double[] LocateInterval(this ITickLocator locator, double from, double to)
    {
        var interval = new Range1D(from, to).Interval;
        return locator.Locate(interval.Lo, interval.Hi);
    }
}
