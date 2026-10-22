// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models.Series;

/// <summary>The one rule for which axis bars stand their categories along: upright bars along X, bars that lie down
/// along Y, their values along the other axis. A horizontal bar chart is an upright one with its axes swapped, so a bar
/// series and a count plot measure their range upright and put it on the axes here, and the axes renderer asks here which
/// axis to name the categories on.</summary>
internal static class BarOrientationExtensions
{
    /// <summary>Whether bars of <paramref name="orientation"/> lie down: their categories along the Y axis, their values
    /// along X.</summary>
    /// <param name="orientation">The bars' orientation.</param>
    /// <returns><see langword="true"/> for <see cref="BarOrientation.Horizontal"/>.</returns>
    internal static bool CategoriesAlongY(this BarOrientation orientation) => orientation == BarOrientation.Horizontal;

    /// <summary>The scale of the axis bars of <paramref name="orientation"/> measure their values along: the Y axis' for
    /// upright bars, the X axis' for bars that lie down. Their range takes only the values that axis can place.</summary>
    /// <param name="orientation">The bars' orientation.</param>
    /// <param name="context">The axes the bars are drawn on.</param>
    /// <returns>The scale of the value axis.</returns>
    internal static AxisScale ValueScale(this BarOrientation orientation, IAxesContext context) =>
        orientation.CategoriesAlongY() ? context.XScale : context.YScale;

    /// <summary>A range measured as upright bars measure it (the categories' extent as X, the values as Y), put on the axes
    /// the bars stand on: as it is for upright bars, with X and Y swapped (their sticky edges with them) for bars that lie
    /// down.</summary>
    /// <param name="upright">The range in the upright frame.</param>
    /// <param name="orientation">The bars' orientation.</param>
    /// <returns>The range on the axes.</returns>
    internal static DataRangeContribution OnAxes(this DataRangeContribution upright, BarOrientation orientation) =>
        orientation.CategoriesAlongY() ? upright.Transposed() : upright;

    /// <summary>A range with its X and Y swapped, sticky edges with them: the range of a picture drawn on its side.</summary>
    /// <param name="range">The range to turn.</param>
    /// <returns>The range with X as Y and Y as X.</returns>
    internal static DataRangeContribution Transposed(this DataRangeContribution range) =>
        range with
        {
            XMin = range.YMin, XMax = range.YMax, YMin = range.XMin, YMax = range.XMax,
            StickyXMin = range.StickyYMin, StickyXMax = range.StickyYMax,
            StickyYMin = range.StickyXMin, StickyYMax = range.StickyXMax,
        };

    /// <summary>Whether <paramref name="series"/> names its categories along the Y axis: bars that lie down do; every
    /// other category-labelled series (upright bars, a candlestick's dates) names them along X.</summary>
    /// <param name="series">The category-labelled series.</param>
    /// <returns><see langword="true"/> when its categories stand along Y.</returns>
    internal static bool NamesCategoriesAlongY(this ICategoryLabeled series) =>
        series is IHasBarOrientation bars && bars.Orientation.CategoriesAlongY();
}
