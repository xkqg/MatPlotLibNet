// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Skia;

/// <summary>The size in pixels a figure is drawn at for a resolution, and the scale that makes the drawing fill it.
/// A figure is laid out in units of one pixel at 96 dpi, so a unit is <c>dpi / 96</c> pixels; the pixel count is the
/// scaled size rounded to a whole number, and the drawing is scaled by exactly the ratio of that count to the figure's
/// size, so the picture meets all four edges of the bitmap: no strip of the background is cut off at the right or the
/// bottom, and none is left white.</summary>
/// <param name="Width">Pixels across.</param>
/// <param name="Height">Pixels down.</param>
/// <param name="ScaleX">Pixels per unit across; exactly 1 at 96 dpi when the figure has a whole size.</param>
/// <param name="ScaleY">Pixels per unit down.</param>
internal readonly record struct RasterSize(int Width, int Height, float ScaleX, float ScaleY)
{
    /// <summary>The size <paramref name="figure"/> is drawn at on a raster image of <paramref name="dpi"/> dots per inch.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="dpi"/> is not a positive, finite number.</exception>
    /// <exception cref="ArgumentException">The figure at that resolution is not between 1 and <see cref="int.MaxValue"/>
    /// pixels each way.</exception>
    public static RasterSize For(Figure figure, double dpi)
    {
        if (!(dpi > 0) || double.IsInfinity(dpi))
        {
            throw new ArgumentOutOfRangeException(
                nameof(dpi), dpi, "The resolution of a raster image must be a positive, finite number of dots per inch.");
        }

        double unitsToPixels = dpi / Figure.ReferenceDpi;
        double width = Math.Round(figure.Width * unitsToPixels, MidpointRounding.AwayFromZero);
        double height = Math.Round(figure.Height * unitsToPixels, MidpointRounding.AwayFromZero);
        if (!(width >= 1 && width <= int.MaxValue && height >= 1 && height <= int.MaxValue))
        {
            throw new ArgumentException(
                string.Create(CultureInfo.InvariantCulture,
                    $"A figure of {figure.Width} by {figure.Height} at {dpi} dpi is {width} by {height} pixels; a raster image needs a whole number of pixels from 1 to {int.MaxValue} each way."),
                nameof(figure));
        }

        return new RasterSize((int)width, (int)height, (float)(width / figure.Width), (float)(height / figure.Height));
    }
}
