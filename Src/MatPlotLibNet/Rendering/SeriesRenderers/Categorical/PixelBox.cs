// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>A box that stands on a foot, as it stands on the plot in pixels: the two opposite corners it runs between.</summary>
/// <param name="From">The corner at the data's upper-left of an upright box that rises, as <see cref="PixelBar"/> keeps it.</param>
/// <param name="To">The opposite corner.</param>
internal readonly record struct PixelBox(Point From, Point To)
{
    /// <summary>The rectangle the box fills: it keeps a positive size on an axis that runs backwards.</summary>
    internal Rect Rect => Rect.FromCorners(From, To);
}

/// <summary>The one rule that turns a box in data units into pixels: it stands on a foot and reaches an end.</summary>
internal static class PixelBoxExtensions
{
    extension(DataTransform transform)
    {
        /// <summary>The box that stands on <paramref name="foot"/> and reaches <paramref name="end"/>, across
        /// <paramref name="slot"/> of the category axis. A foot the axis cannot place (0 on a log axis) is clipped to the edge
        /// where the axis begins, as matplotlib clips it; an end it cannot place leaves a corner that is not a number, so the
        /// box is not drawable and is skipped. Bars, waterfall steps, counts, histogram bins and the box of a box plot all
        /// stand this way, and a mark sized in data units (a waffle cell, a Point and Figure box) will too.</summary>
        /// <param name="slot">The stretch of the category axis the box covers.</param>
        /// <param name="foot">Where the box starts along the value axis, in data units.</param>
        /// <param name="end">Where it ends along the value axis, in data units.</param>
        /// <param name="rising"><see langword="true"/> when the end lies at or beyond the foot in the direction the data
        /// grows; a falling box keeps its corners the other way round so the clipped foot stays the clipped one.</param>
        /// <param name="orientation">The axis the value runs along: Y for <see cref="BarOrientation.Vertical"/>, X for horizontal.</param>
        /// <returns>The box in pixels.</returns>
        internal PixelBox BoxOnFoot(CategorySlot slot, double foot, double end, bool rising, BarOrientation orientation)
        {
            if (orientation == BarOrientation.Vertical)
            {
                return rising
                    ? new(transform.DataToPixel(slot.Left, end), transform.DataToPixelClippedY(slot.Right, foot))
                    : new(transform.DataToPixelClippedY(slot.Left, foot), transform.DataToPixel(slot.Right, end));
            }

            return rising
                ? new(transform.DataToPixelClippedX(foot, slot.Right), transform.DataToPixel(end, slot.Left))
                : new(transform.DataToPixel(end, slot.Right), transform.DataToPixelClippedX(foot, slot.Left));
        }
    }
}
