// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.ColorBarRendering;
using MatPlotLibNet.Rendering.Layout;
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
        internal Rect WithBoxAspect(double heightOverWidth) => cell.WithBoxAspect(heightOverWidth, 0.5, 0.5);

        /// <summary>The largest rectangle inside <paramref name="cell"/> whose height over its width is
        /// <paramref name="heightOverWidth"/>, the room it leaves shared as <paramref name="anchorX"/> and
        /// <paramref name="anchorY"/> say (matplotlib's <c>Bbox.anchored</c>: 0 against the left or the top, 0.5 centred, 1
        /// against the right or the bottom). A ratio that is not positive and finite changes nothing.</summary>
        /// <param name="heightOverWidth">The box aspect: height divided by width.</param>
        /// <param name="anchorX">Where the box stands across: 0 left, 1 right.</param>
        /// <param name="anchorY">Where the box stands up and down: 0 top, 1 bottom.</param>
        /// <returns>The fitted rectangle.</returns>
        internal Rect WithBoxAspect(double heightOverWidth, double anchorX, double anchorY)
        {
            if (!heightOverWidth.IsPositiveFinite()) return cell;

            // Too tall for the shape: keep the width, take the height from it. Too wide: the other way round.
            double height = cell.Width * heightOverWidth;
            if (height <= cell.Height)
            {
                return new Rect(cell.X, cell.Y + (cell.Height - height) * anchorY, cell.Width, height);
            }

            double width = cell.Height / heightOverWidth;
            return new Rect(cell.X + (cell.Width - width) * anchorX, cell.Y, width, cell.Height);
        }
    }

    extension(Axes axes)
    {
        /// <summary>The rectangle <paramref name="axes"/> is drawn in when it is given <paramref name="cell"/>: the cell itself
        /// when the axes has no aspect, otherwise the largest rectangle inside it in which one unit of Y is that aspect times as long as one
        /// unit of X: the aspect it was given (<see cref="Axes.Aspect"/>), else the one its series ask
        /// (<see cref="AxesRangeExtensions.AspectUnder"/>: a map's 1, as cartopy's <c>GeoAxes</c> sets it). The ranges are measured in the
        /// scale each axis is drawn in (a decade of a log axis is a unit), as matplotlib measures them, from <see cref="AxesRangeExtensions"/>, which
        /// reads no plot area. An aspect that is not positive and finite and a range that cannot be measured (an infinite
        /// end, or none at all) leave the cell as it is. Three have a shape of their own, whatever aspect they were given: a
        /// ternary axes gets the triangle's box, √3/2 high for one wide, a 3-D axes the centred square of its cell (matplotlib's
        /// <c>Axes3D.apply_aspect</c>), where its box, its background, its title and its legend stand, and a polar axes the same
        /// square (matplotlib's <c>PolarAxes.__init__</c>: <c>set_aspect('equal', adjustable='box', anchor='C')</c>), which its
        /// circle fills. A colour bar that takes its room from the axes (<see cref="ColorBarRoomExtensions"/>) takes it from the
        /// cell first, and the shape is then fitted in what is left against the bar's side, as matplotlib's
        /// <c>make_axes_gridspec</c> anchors the axes (<c>panchor</c>: right for a bar beside them, bottom for one below). The
        /// cell of an axes under the figure's own bar has already been narrowed by it (<see cref="FigureGrid.Cells"/>); the shape
        /// is then fitted against that bar when the axes has none of its own, as <c>make_axes</c> anchors each parent.</summary>
        /// <param name="cell">The rectangle the layout gave the axes.</param>
        /// <param name="theme">The theme that pads the range where the axes sets no margin.</param>
        /// <param name="figureBar">The colour bar of the figure when it takes its room from the subplots or stands in the
        /// constrained layout's margin; <see langword="null"/> when it has none in either, and for an inset.</param>
        /// <param name="room">The room the figure's colour bars take; the theme's outside the constrained layout when it is not
        /// given. Under the constrained layout a bar stands in the margin and the axes keep their cell, the shape fitted against the
        /// bar as <c>make_axes</c> anchors each parent (<see cref="ColorBarRoom.AnchorsAxes"/>).</param>
        /// <returns>The rectangle to draw the axes in.</returns>
        internal Rect FittedTo(Rect cell, Theme theme, ColorBar? figureBar = null, ColorBarRoom? room = null)
        {
            double anchorX = 0.5, anchorY = 0.5;
            var where = room ?? ColorBarRoom.Under(theme, constrained: false);
            if (axes.BarTakingRoom(where) is { } own) cell = own.KeptOf(cell);
            var anchoring = where.AnchorsAxes && axes.ColorBar is { Visible: true } bar ? bar : null;
            if ((anchoring ?? figureBar) is { } towards)
            {
                if (towards.Beside) anchorX = 1;
                else anchorY = 1;
            }

            if (axes.CoordinateSystem == CoordinateSystem.Ternary) return cell.WithBoxAspect(TernaryFrame.Height, anchorX, anchorY);
            if (axes.CoordinateSystem is CoordinateSystem.ThreeD or CoordinateSystem.Polar) return cell.WithBoxAspect(1.0, anchorX, anchorY);
            if (axes.AspectUnder(theme) is not { } aspect) return cell;
            if (axes.CoordinateSystem != CoordinateSystem.Cartesian) return cell;

            var range = axes.ComputeDataRanges(theme);
            var (xLo, xHi) = axes.XAxis.ScaleRuleUnder(theme).ForwardRange(range.XMin, range.XMax);
            var (yLo, yHi) = axes.YAxis.ScaleRuleUnder(theme).ForwardRange(range.YMin, range.YMax);
            double xSpan = Math.Abs(xHi - xLo);
            double ySpan = Math.Abs(yHi - yLo);

            // An aspect that is not positive and finite, and a span that is zero, infinite or not a number, make a ratio that is not
            // positive and finite, which WithBoxAspect leaves alone: the one place that decides a box has no shape to fit.
            return cell.WithBoxAspect(aspect * ySpan / xSpan, anchorX, anchorY);
        }

        /// <summary>The first series of the axes that draws a map (<see cref="IMapSeries"/>), which makes the axes a map axes;
        /// <see langword="null"/> for an axes that draws none. Walked by index: a foreach over the list boxes its enumerator.</summary>
        internal IMapSeries? Map
        {
            get
            {
                for (int i = 0; i < axes.Series.Count; i++)
                {
                    if (axes.Series[i] is IMapSeries map) return map;
                }

                return null;
            }
        }
    }
}
