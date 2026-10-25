// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models.Series;

/// <summary>The one rule for which axis bars stand their categories along: upright bars along X, bars that lie down
/// along Y, their values along the other axis. A horizontal bar chart is an upright one with its axes swapped, so a bar
/// series and a count plot measure their range upright and put it on the axes here, and the axes renderer asks here which
/// axis to name the categories on.</summary>
internal static class BarOrientationExtensions
{
    extension(BarOrientation orientation)
    {
        /// <summary>Whether bars of this orientation lie down: their categories along the Y axis, their values
        /// along X.</summary>
        /// <returns><see langword="true"/> for <see cref="BarOrientation.Horizontal"/>.</returns>
        internal bool CategoriesAlongY() => orientation == BarOrientation.Horizontal;

        /// <summary>The scale of the axis bars of this orientation measure their values along: the Y axis' for
        /// upright bars, the X axis' for bars that lie down. Their range takes only the values that axis can place.</summary>
        /// <param name="context">The axes the bars are drawn on.</param>
        /// <returns>The scale of the value axis.</returns>
        internal AxisScale ValueScale(IAxesContext context) =>
            orientation.CategoriesAlongY() ? context.XScale : context.YScale;
    }

    extension(DataRangeContribution upright)
    {
        /// <summary>A range measured as upright bars measure it (the categories' extent as X, the values as Y), put on the axes
        /// the bars stand on: as it is for upright bars, with X and Y swapped (their sticky edges with them) for bars that lie
        /// down.</summary>
        /// <param name="orientation">The bars' orientation.</param>
        /// <returns>The range on the axes.</returns>
        internal DataRangeContribution OnAxes(BarOrientation orientation) =>
            orientation.CategoriesAlongY() ? upright.Transposed() : upright;
    }

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
        /// <summary>Whether this series names its categories along the Y axis: bars that lie down do; every
        /// other category-labelled series (upright bars, a candlestick's dates) names them along X.</summary>
        /// <returns><see langword="true"/> when its categories stand along Y.</returns>
        internal bool NamesCategoriesAlongY() =>
            series is IHasBarOrientation bars && bars.Orientation.CategoriesAlongY();
    }
}
