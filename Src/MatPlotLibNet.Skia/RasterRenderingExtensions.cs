// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using SkiaSharp;
using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Skia;

/// <summary>Draws a figure onto a bitmap: the one place a raster export (a PNG, a frame of a GIF) decides how big the
/// bitmap is and how the figure is scaled onto it.</summary>
internal static class RasterRenderingExtensions
{
    extension(IChartRenderer renderer)
    {
        /// <summary>The figure drawn on white at <paramref name="dpi"/> dots per inch (see <see cref="Skia.RasterRenderingExtensions.RasterSize"/>). The
        /// caller owns the bitmap.</summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="dpi"/> is not a positive, finite number.</exception>
        /// <exception cref="ArgumentException">The figure at that resolution is not a whole number of pixels from 1 up.</exception>
        public SKBitmap RenderToBitmap(Figure figure, double dpi)
        {
            RasterSize size = RasterSizeOf(figure, dpi);
            var bitmap = new SKBitmap(size.Width, size.Height);
            try
            {
                using var canvas = new SKCanvas(bitmap);
                canvas.Clear(SKColors.White);
                if (size.ScaleX != 1f || size.ScaleY != 1f)
                {
                    canvas.Scale(size.ScaleX, size.ScaleY);
                }

                renderer.Render(figure, new SkiaRenderContext(canvas) { SnapsToPixelGrid = true });
                return bitmap;
            }
            catch
            {
                bitmap.Dispose();
                throw;
            }
        }
    }

    /// <summary>The size in pixels a figure is drawn at for a resolution, and the scale that makes the drawing fill it.
    /// A figure is laid out in units of one pixel at 96 dpi, so a unit is <c>dpi / 96</c> pixels; the pixel count is the
    /// scaled size rounded to a whole number, and the drawing is scaled by exactly the ratio of that count to the figure's
    /// size, so the picture meets all four edges of the bitmap: no strip of the background is cut off at the right or the
    /// bottom, and none is left white.</summary>
    /// <param name="Width">Pixels across.</param>
    /// <param name="Height">Pixels down.</param>
    /// <param name="ScaleX">Pixels per unit across; exactly 1 at 96 dpi when the figure has a whole size.</param>
    /// <param name="ScaleY">Pixels per unit down.</param>
    internal readonly record struct RasterSize(int Width, int Height, float ScaleX, float ScaleY);

    /// <summary>The size <paramref name="figure"/> is drawn at on a raster image of <paramref name="dpi"/> dots per inch.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="dpi"/> is not a positive, finite number.</exception>
    /// <exception cref="ArgumentException">The figure at that resolution is not between 1 and <see cref="int.MaxValue"/>
    /// pixels each way.</exception>
    internal static RasterSize RasterSizeOf(Figure figure, double dpi)
    {
        dpi.ThrowIfNotFinite(nameof(dpi)).ThrowIfNotAbove(0, nameof(dpi));

        double unitsToPixels = dpi / Figure.ReferenceDpi;
        double width = Math.Round(figure.Width * unitsToPixels, MidpointRounding.AwayFromZero);
        double height = Math.Round(figure.Height * unitsToPixels, MidpointRounding.AwayFromZero);
        if (!(width >= 1 && width <= int.MaxValue && height >= 1 && height <= int.MaxValue))
        {
            throw new ArgumentException(
                string.Invariant(
                    $"A figure of {figure.Width} by {figure.Height} at {dpi} dpi is {width} by {height} pixels; a raster image needs a whole number of pixels from 1 to {int.MaxValue} each way."),
                nameof(figure));
        }

        return new RasterSize((int)width, (int)height, (float)(width / figure.Width), (float)(height / figure.Height));
    }
}
