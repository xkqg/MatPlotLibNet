// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Rendering;

/// <summary>A spine as it stands on the plot: the two ends of its line, in pixels.</summary>
/// <param name="From">The end at the left of a horizontal spine, at the bottom of a vertical one.</param>
/// <param name="To">The other end.</param>
internal readonly record struct SpineLine(Point From, Point To);

/// <summary>The one owner of where a spine is: its position across the plot (the edge, a data value or a fraction of the
/// axes) and the stretch of the plot it spans (all of it, or the bounds a range frame sets). The renderer draws the four
/// spines from it and the tick marks hang from the position it gives, so a moved spine takes its ticks with it.</summary>
internal static class SpineLineExtensions
{
    extension(SpineConfig spine)
    {
        /// <summary>The Y of a horizontal spine: <paramref name="edgeY"/> at the edge, the pixel row of a data value or of a
        /// fraction of the axes' height (0 at the bottom) when the spine has been moved.</summary>
        /// <param name="plot">The plot area.</param>
        /// <param name="edgeY">The row of the plot's own edge on this side.</param>
        /// <param name="transform">The transform that places a data value.</param>
        /// <returns>The row, in pixels.</returns>
        internal double HorizontalY(Rect plot, double edgeY, DataTransform transform) => spine.Position switch
        {
            SpinePosition.Data => transform.DataToPixel(0, spine.PositionValue).Y,
            SpinePosition.Axes => plot.Y + (1 - spine.PositionValue) * plot.Height,
            _ => edgeY,
        };

        /// <summary>The X of a vertical spine: <paramref name="edgeX"/> at the edge, the pixel column of a data value or of a
        /// fraction of the axes' width (0 at the left) when the spine has been moved.</summary>
        /// <param name="plot">The plot area.</param>
        /// <param name="edgeX">The column of the plot's own edge on this side.</param>
        /// <param name="transform">The transform that places a data value.</param>
        /// <returns>The column, in pixels.</returns>
        internal double VerticalX(Rect plot, double edgeX, DataTransform transform) => spine.Position switch
        {
            SpinePosition.Data => transform.DataToPixel(spine.PositionValue, 0).X,
            SpinePosition.Axes => plot.X + spine.PositionValue * plot.Width,
            _ => edgeX,
        };

        /// <summary>The line of a horizontal spine: across its <see cref="HorizontalY"/>, from its
        /// <see cref="SpineConfig.BoundsFrom"/> to its <see cref="SpineConfig.BoundsTo"/> (data values of X), the plot's left
        /// and right edge where it has none.</summary>
        /// <param name="plot">The plot area.</param>
        /// <param name="edgeY">The row of the plot's own edge on this side.</param>
        /// <param name="transform">The transform that places a data value.</param>
        /// <returns>The line.</returns>
        internal SpineLine HorizontalLine(Rect plot, double edgeY, DataTransform transform)
        {
            double y = spine.HorizontalY(plot, edgeY, transform);
            double from = spine.BoundsFrom is { } begin ? transform.DataToPixel(begin, 0).X : plot.X;
            double to = spine.BoundsTo is { } end ? transform.DataToPixel(end, 0).X : plot.X + plot.Width;
            return new SpineLine(new Point(from, y), new Point(to, y));
        }

        /// <summary>The line of a vertical spine: along its <see cref="VerticalX"/>, from its
        /// <see cref="SpineConfig.BoundsFrom"/> to its <see cref="SpineConfig.BoundsTo"/> (data values of Y), the plot's top and
        /// bottom edge where it has none. It runs from the top down, as it always has.</summary>
        /// <param name="plot">The plot area.</param>
        /// <param name="edgeX">The column of the plot's own edge on this side.</param>
        /// <param name="transform">The transform that places a data value.</param>
        /// <returns>The line.</returns>
        internal SpineLine VerticalLine(Rect plot, double edgeX, DataTransform transform)
        {
            double x = spine.VerticalX(plot, edgeX, transform);
            double top = spine.BoundsTo is { } end ? transform.DataToPixel(0, end).Y : plot.Y;
            double bottom = spine.BoundsFrom is { } begin ? transform.DataToPixel(0, begin).Y : plot.Y + plot.Height;
            return new SpineLine(new Point(x, top), new Point(x, bottom));
        }
    }
}
