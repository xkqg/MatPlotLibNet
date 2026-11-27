// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Models.Series;

/// <summary>Which axis a series' categories stand along, as its range and its names reach the axes: a horizontal bar chart is an
/// upright one with its axes swapped, so a bar series, a count plot, a box that lies down and a fill that lies down measure their
/// range upright and put it on the axes (<see cref="AlongExtensions"/>' <c>OnAxes</c>, which swaps it by <see cref="Transposed"/> for bars
/// that lie down), and the axes renderer asks here which axis names the categories. Which way the bars run is their orientation's own
/// member (<see cref="AlongExtensions"/>' <c>ValuesAlongX</c>).</summary>
internal static class CategoryPlacementExtensions
{
    extension(DataRangeContribution range)
    {
        /// <summary>A range with its X and Y swapped, sticky edges with them: the range of a picture drawn on its side.</summary>
        /// <returns>The range with X as Y and Y as X.</returns>
        internal DataRangeContribution Transposed() =>
            range with
            {
                XMin = range.YMin, XMax = range.YMax, YMin = range.XMin, YMax = range.XMax,
                StickyXMin = range.StickyYMin, StickyXMax = range.StickyYMax,
                StickyYMin = range.StickyXMin, StickyYMax = range.StickyXMax,
            };
    }

    extension(ICategoryLabeled series)
    {
        /// <summary>Whether this series names its categories along the Y axis, as it says itself (<see cref="ICategoryPlacement"/>: bars
        /// that lie down, a Gantt chart's tasks); every other category-labelled series (upright bars, a candlestick's dates, one of your
        /// own) names them along X.</summary>
        /// <returns><see langword="true"/> when its categories stand along Y.</returns>
        internal bool NamesCategoriesAlongY() => series is ICategoryPlacement { CategoriesAlongY: true };
    }
}
