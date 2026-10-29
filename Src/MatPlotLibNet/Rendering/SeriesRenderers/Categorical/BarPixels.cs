// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>One bar as it stands on the plot in pixels.</summary>
/// <param name="Index">The bar's position in the series: which category's value it is.</param>
/// <param name="From">The corner at the data's lower-left of the bar (its left edge and the end of a rising bar; its left
/// edge and the foot of a falling one, for a vertical bar), as the renderer computes it: the rectangle runs from here to
/// <paramref name="To"/>.</param>
/// <param name="To">The opposite corner.</param>
/// <param name="Rising"><see langword="true"/> when the bar's value is zero or more.</param>
internal readonly record struct PixelBar(int Index, Point From, Point To, bool Rising)
{
    /// <summary>The rectangle the bar fills: it keeps a positive size on an axis that runs backwards.</summary>
    internal Rect Bar => Rect.FromCorners(From, To);

    /// <summary>Where a vertical bar's end stands, as a pixel row: the corner its value reaches, whichever way it points.</summary>
    internal double VerticalEnd => Rising ? From.Y : To.Y;

    /// <summary>Where a horizontal bar's end stands, as a pixel column: the corner its value reaches, whichever way it points.</summary>
    internal double HorizontalEnd => Rising ? To.X : From.X;
}

/// <summary>Where a bar series' bars stand: the one geometry the bar renderer draws them at and hands to a legend.</summary>
internal static class BarPixelExtensions
{
    extension(DataTransform transform)
    {
        /// <summary>Whether the axis a bar's value runs along (the Y axis of a vertical bar, the X axis of a horizontal one) runs
        /// backwards: its limits were set high to low, so a larger value stands lower (or further left) in the picture.</summary>
        /// <param name="orientation">The bar's orientation.</param>
        /// <returns><see langword="true"/> when the axis runs backwards.</returns>
        internal bool ValueRunsBackwards(BarOrientation orientation)
        {
            var drawn = transform.DrawnRange;
            return orientation == BarOrientation.Vertical ? drawn.YMax < drawn.YMin : drawn.XMax < drawn.XMin;
        }
    }

    extension(BarSeries series)
    {
        /// <summary>The bars of the series that can be drawn, in order, as they stand under <paramref name="transform"/>.
        /// A bar stands on its foot and reaches its end. A foot the axis cannot place (0 on a log axis) is clipped to the
        /// edge where the axis begins, as matplotlib clips it; a bar whose end, position or missing foot the axes cannot
        /// place is left out (and its label with it). A rising bar's end is its top, a falling bar's its bottom.</summary>
        /// <param name="transform">The transform the bars are drawn with.</param>
        /// <returns>The bars that stand on the plot.</returns>
        internal IEnumerable<PixelBar> PixelBars(DataTransform transform)
        {
            for (int i = 0; i < series.Categories.Length; i++)
            {
                // Where the bar stands along its category axis: in its category's slot, or around its numeric x
                // (CategorySlot, the one rule the data range and the category labels read too).
                var slot = series.SlotOf(i);
                double barLeft = slot.Left, barRight = slot.Right;

                double baseline = series.Bottoms is { } bottoms ? bottoms[i] : 0;
                double value = series.Values[i];
                double end = baseline + value;
                bool rising = value >= 0;
                Point from, to;
                if (series.Orientation == BarOrientation.Vertical)
                {
                    from = rising ? transform.DataToPixel(barLeft, end) : transform.DataToPixelClippedY(barLeft, baseline);
                    to = rising ? transform.DataToPixelClippedY(barRight, baseline) : transform.DataToPixel(barRight, end);
                }
                else
                {
                    from = rising ? transform.DataToPixelClippedX(baseline, barRight) : transform.DataToPixel(end, barRight);
                    to = rising ? transform.DataToPixel(end, barLeft) : transform.DataToPixelClippedX(baseline, barLeft);
                }
                var pixel = new PixelBar(i, from, to, rising);
                if (pixel.Bar.IsDrawable()) yield return pixel;
            }
        }
    }
}
