// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering;

/// <summary>The one owner of the aspect of a plot: the rule that fits the rectangle a plot is given to the shape its aspect asks
/// for. The layout applies it to the cells it computes (and to an inset's rectangle), never a renderer, because the
/// interaction layer hit-tests the rectangles the layout hands it.</summary>
internal static class PlotAspectExtensions
{
    extension(Rect cell)
    {
        /// <summary>The largest rectangle inside <paramref name="cell"/> whose height over its width is
        /// <paramref name="heightOverWidth"/>, centred in it. A ratio that is not positive and finite changes nothing.</summary>
        /// <param name="heightOverWidth">The box aspect: height divided by width.</param>
        /// <returns>The fitted rectangle.</returns>
        internal Rect WithBoxAspect(double heightOverWidth)
        {
            if (!(heightOverWidth > 0) || !double.IsFinite(heightOverWidth)) return cell;

            // Too tall for the shape: keep the width, take the height from it. Too wide: the other way round.
            double height = cell.Width * heightOverWidth;
            if (height <= cell.Height)
            {
                return new Rect(cell.X, cell.Y + (cell.Height - height) / 2, cell.Width, height);
            }

            double width = cell.Height / heightOverWidth;
            return new Rect(cell.X + (cell.Width - width) / 2, cell.Y, width, cell.Height);
        }
    }

    extension(Axes axes)
    {
        /// <summary>The rectangle <paramref name="axes"/> is drawn in when it is given <paramref name="cell"/>: the cell itself
        /// when the axes asks for no aspect, otherwise the largest rectangle inside it in which one unit of Y is
        /// <see cref="Axes.Aspect"/> times as long as one unit of X. The ranges are measured in the scale each axis is drawn in
        /// (a decade of a log axis is a unit), as matplotlib measures them, from <see cref="AxesRangeExtensions"/>, which
        /// reads no plot area. A polar or 3-D axes, an aspect that is not positive and finite, and a range that cannot be
        /// measured (an infinite end, or none at all) all leave the cell as it is. A ternary axes is the exception: its shape
        /// is the triangle's own, √3/2 high for one wide, whatever aspect it was given, so it always gets that box.</summary>
        /// <param name="cell">The rectangle the layout gave the axes.</param>
        /// <param name="theme">The theme that pads the range where the axes sets no margin.</param>
        /// <returns>The rectangle to draw the axes in.</returns>
        internal Rect FittedTo(Rect cell, Theme theme)
        {
            if (axes.CoordinateSystem == CoordinateSystem.Ternary) return cell.WithBoxAspect(TernaryFrame.Height);
            if (axes.Aspect is not { } aspect) return cell;
            if (axes.CoordinateSystem != CoordinateSystem.Cartesian) return cell;

            var range = axes.ComputeDataRanges(theme);
            double xSpan = Math.Abs(axes.XAxis.Scale.Forward(range.XMax, axes.XAxis.SymLogLinThresh) - axes.XAxis.Scale.Forward(range.XMin, axes.XAxis.SymLogLinThresh));
            double ySpan = Math.Abs(axes.YAxis.Scale.Forward(range.YMax, axes.YAxis.SymLogLinThresh) - axes.YAxis.Scale.Forward(range.YMin, axes.YAxis.SymLogLinThresh));

            // An aspect that is not positive and finite, and a span that is zero, infinite or not a number, make a ratio that is not
            // positive and finite, which WithBoxAspect leaves alone: the one place that decides a box has no shape to fit.
            return cell.WithBoxAspect(aspect * ySpan / xSpan);
        }
    }
}
