// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models;

/// <summary>The one rule between an axis' link and its series: a linked axis (<see cref="Axis.Link"/>) shows the primary's values
/// in other units, so it holds no series of its own. Both builders of an axis that can be linked (<c>YAxisSlotBuilder</c> and
/// <c>SecondaryXAxisBuilder</c>) call it, so the two refusals read alike whichever comes first, and none keeps a copy of
/// them.</summary>
internal static class AxisLinkExtensions
{
    extension(Axis axis)
    {
        /// <summary>Links this axis through <paramref name="transform"/>, replacing a link it had; refused, with nothing changed,
        /// when the axis holds a series.</summary>
        /// <param name="transform">The link.</param>
        /// <param name="seriesHeld">How many series the axis holds.</param>
        /// <exception cref="ArgumentNullException"><paramref name="transform"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">The axis holds a series.</exception>
        internal void LinkThrough(AxisTransform transform, int seriesHeld)
        {
            ArgumentNullException.ThrowIfNull(transform);
            if (seriesHeld > 0)
            {
                throw new ArgumentException(
                    "This axis holds a series; a linked axis shows the primary axis' values in other units and holds no series of its own.",
                    nameof(transform));
            }

            axis.Link = transform;
        }

        /// <summary>Refuses a series on this axis when it is linked, naming <paramref name="parameter"/>: the values the series
        /// would have measured against it.</summary>
        /// <param name="parameter">The name of the parameter the refusal names.</param>
        /// <exception cref="ArgumentException">The axis is linked.</exception>
        internal void RefuseSeriesWhenLinked(string parameter)
        {
            if (axis.Link is not null)
            {
                throw new ArgumentException(
                    "This axis is linked to the primary axis; a linked axis shows the primary axis' values in other units and holds no series of its own.",
                    parameter);
            }
        }
    }
}
