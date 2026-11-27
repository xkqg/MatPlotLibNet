// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.TickLocators;

/// <summary>How the renderers ask an <see cref="ITickLocator"/> for the ticks of a drawn range, and the range of an axis for where
/// its locator ends it.</summary>
internal static class TickLocatorExtensions
{
    extension(ITickLocator locator)
    {
        /// <summary>The ticks of the interval between <paramref name="from"/> and <paramref name="to"/>, whichever way the axis
        /// runs. A locator counts from its minimum up to its maximum, so a reversed limit handed over as drawn
        /// (<c>SetYLim(5.5, 0.5)</c>) gave the single tick 5.5; the ticks of a reversed axis are the ticks of the same
        /// interval (<see cref="Range1D.Interval"/>), and the transform draws each where its value is.</summary>
        /// <param name="from">One end of the drawn range (the axis minimum as set, possibly the larger value).</param>
        /// <param name="to">The other end of the drawn range.</param>
        /// <returns>The locator's ticks for the interval, ascending.</returns>
        internal double[] LocateInterval(double from, double to)
        {
            var interval = new Range1D(from, to).Interval;
            return locator.Locate(interval.Lo, interval.Hi);
        }

        /// <summary>The ticks of the interval between <paramref name="from"/> and <paramref name="to"/> along an axis with room for
        /// <paramref name="tickSpace"/> labels, drawn under <paramref name="theme"/>: those of <see cref="LocateInterval"/>, or, for a
        /// locator whose ticks follow how many labels fit and the theme (<see cref="ITickSpaceLocator"/>: a log axis' powers of ten, a
        /// logit axis' ticks), the ones it lays for that count, as matplotlib's locators read their axis' <c>get_tick_space</c>. The one
        /// way an axis and a colour bar ask the locator that places their ticks.</summary>
        /// <param name="from">One end of the drawn range.</param>
        /// <param name="to">The other end of the drawn range.</param>
        /// <param name="tickSpace">How many labels fit along the axis (<see cref="MaxNLocator.LabelsAlong"/>).</param>
        /// <param name="theme">The theme the axis is drawn under.</param>
        /// <returns>The locator's ticks for the interval, ascending.</returns>
        internal double[] LocateAlong(double from, double to, double tickSpace, Styling.Theme theme)
        {
            if (locator is not ITickSpaceLocator spaced) return locator.LocateInterval(from, to);
            var interval = new Range1D(from, to).Interval;
            return spaced.LocateWithSpace(interval.Lo, interval.Hi, tickSpace, theme);
        }

        /// <summary>Where the locator ends an axis that shows <paramref name="range"/> when the theme rounds an axis out to its
        /// ticks: its own rule (<see cref="IRoundingLocator"/>), else the range as it is, as matplotlib's base
        /// <c>Locator.view_limits</c> keeps it.</summary>
        /// <param name="range">The range, its low end below its high end.</param>
        /// <returns>The rounded range, or <paramref name="range"/>.</returns>
        internal Range1D RangeRoundedOut(Range1D range) => locator is IRoundingLocator rounding ? rounding.RoundedOut(range) : range;
    }
}
