// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet;

/// <summary>The line and scatter verbs of one Y axis slot: the one place that checks the two data arrays line up, makes the
/// series and puts it on the slot. <c>Axes.Plot</c> and <c>Axes.Scatter</c> (<see cref="AxesXYVerbs"/>) are these verbs over
/// the primary slot, and <see cref="YAxisSlotBuilder.Plot"/> and <see cref="YAxisSlotBuilder.Scatter"/> are them over the slot
/// the builder holds. Internal on purpose: the public way to put a series on a slot is <c>YAxisSlot.AddSeries</c>.</summary>
internal static class YAxisSlotXYVerbs
{
    extension(YAxisSlot slot)
    {
        /// <summary>Adds a line series of the given X and Y data to this Y axis.</summary>
        /// <param name="x">The X-axis data values.</param>
        /// <param name="y">The Y-axis data values, in the units of this axis.</param>
        /// <returns>The newly created <see cref="LineSeries"/>.</returns>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="y"/> differ in length.</exception>
        internal LineSeries Plot(double[] x, double[] y)
        {
            x.Length.ThrowIfMisaligned(y.Length);
            return slot.AddSeries(new LineSeries(x, y));
        }

        /// <summary>Adds a scatter series of the given X and Y data to this Y axis, the twin of <c>Plot</c>.</summary>
        /// <param name="x">The X-axis data values.</param>
        /// <param name="y">The Y-axis data values, in the units of this axis.</param>
        /// <returns>The newly created <see cref="ScatterSeries"/>.</returns>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="y"/> differ in length.</exception>
        internal ScatterSeries Scatter(double[] x, double[] y)
        {
            x.Length.ThrowIfMisaligned(y.Length);
            return slot.AddSeries(new ScatterSeries(x, y));
        }
    }
}
