// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>A box that stands on a foot, as it stands on the plot in pixels: the two opposite corners it runs between.</summary>
/// <param name="From">The corner at the data's upper-left of an upright box that rises, as <see cref="Rendering.SeriesRenderers.BarSeriesRenderer.PixelBar"/> keeps it.</param>
/// <param name="To">The opposite corner.</param>
internal readonly record struct PixelBox(Point From, Point To)
{
    /// <summary>The rectangle the box fills: it keeps a positive size on an axis that runs backwards.</summary>
    internal Rect Rect => Rect.FromCorners(From, To);

    /// <summary>The one rule that turns a box in data units into pixels: the box that stands on <paramref name="foot"/> and reaches
    /// <paramref name="end"/>, across <paramref name="slot"/> of the category axis, as matplotlib draws a bar: each corner placed by the log scale's clip (a
    /// value a log axis cannot place a thousand decades beyond it), then cut by the plot. A side that runs to such a value is
    /// cut on the edge where that axis begins (a foot or an end of 0 or below, a slot reaching 0 or below), and a box whose
    /// two sides along one axis both lie beyond it is not drawn (its corners are not numbers, so it is skipped). A missing
    /// value leaves a corner that is not a number too. Bars, waterfall steps, counts, histogram bins and the box of a box
    /// plot all stand this way.</summary>
    /// <param name="transform">The mapping of the plot the box is drawn on.</param>
    /// <param name="slot">The stretch of the category axis the box covers.</param>
    /// <param name="foot">Where the box starts along the value axis, in data units.</param>
    /// <param name="end">Where it ends along the value axis, in data units.</param>
    /// <param name="rising"><see langword="true"/> when the end lies at or beyond the foot in the direction the data
    /// grows; a falling box keeps its corners the other way round, so <see cref="PixelBox.From"/> stays the corner at the
    /// data's upper-left.</param>
    /// <param name="orientation">The axis the value runs along: Y for <see cref="BarOrientation.Vertical"/>, X for horizontal.</param>
    /// <returns>The box in pixels.</returns>
    internal static PixelBox OnFoot(DataTransform transform, CategorySlot slot, double foot, double end, bool rising, BarOrientation orientation)
    {
        // From is the corner at the screen's start along both axes (the data's upper-left): along the value axis the value that
        // stands there of the box's low and high value, across it the slot's end that stands there.
        bool alongX = orientation.ValuesAlongX;
        double low = rising ? foot : end, high = rising ? end : foot;
        var from = Point.Along(alongX, alongX.AtScreenStart(low, high), (!alongX).AtScreenStart(slot.Left, slot.Right));
        var to = Point.Along(alongX, alongX.AtScreenEnd(low, high), (!alongX).AtScreenEnd(slot.Left, slot.Right));
        var box = new PixelBox(transform.DataToPixelUnclipped(from.X, from.Y), transform.DataToPixelUnclipped(to.X, to.Y));

        // Both corners beyond one axis: the box lies wholly where that axis' log scale puts what it cannot place.
        if ((transform.BeyondOf(box.From) & transform.BeyondOf(box.To)).Any)
        {
            return new PixelBox(new Point(double.NaN, double.NaN), box.To);
        }

        return new PixelBox(transform.AtTheStart(box.From), transform.AtTheStart(box.To));
    }
}
