// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Models.Series;

/// <summary>The extent of bars that stand in rows, the one rule of a broken-bar chart and a Gantt chart.</summary>
internal static class BarHeightExtensions
{
    extension(double barHeight)
    {
        /// <summary>The extent along the row axis of bars <paramref name="barHeight"/> tall that stand centred on rows 0 to
        /// <paramref name="rows"/> - 1: from half a bar below the first row to half a bar above the last, the data limits
        /// matplotlib's <c>barh</c> and <c>broken_barh</c> give an axis. A height that is not a number or an infinity (nothing is
        /// drawn at it) leaves the rows' own places.</summary>
        /// <param name="rows">The number of rows; none is taken as one, so the extent never runs backwards.</param>
        /// <returns>The extent.</returns>
        internal Range1D AcrossRows(int rows)
        {
            double half = double.IsFinite(barHeight) ? Math.Abs(barHeight) / 2 : 0;
            return new Range1D(-half, Math.Max(rows, 1) - 1 + half);
        }

        /// <summary>The extent along the row axis of bars <paramref name="barHeight"/> tall that stand centred in category slots 0 to
        /// <paramref name="rows"/> - 1 (<see cref="CategorySlot"/>, the rows of a Gantt chart): <see cref="AcrossRows"/> half a slot up.</summary>
        /// <param name="rows">The number of rows; none is taken as one.</param>
        /// <returns>The extent.</returns>
        internal Range1D AcrossSlots(int rows)
        {
            var across = barHeight.AcrossRows(rows);
            return new Range1D(across.Lo + CategorySlot.CentreOffset, across.Hi + CategorySlot.CentreOffset);
        }
    }
}
