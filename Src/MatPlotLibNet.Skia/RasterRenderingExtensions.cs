// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using SkiaSharp;

namespace MatPlotLibNet.Skia;

/// <summary>Draws a figure onto a bitmap: the one place a raster export (a PNG, a frame of a GIF) decides how big the
/// bitmap is and how the figure is scaled onto it.</summary>
internal static class RasterRenderingExtensions
{
    /// <summary>The figure drawn on white at <paramref name="dpi"/> dots per inch (see <see cref="RasterSize"/>). The
    /// caller owns the bitmap.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="dpi"/> is not a positive, finite number.</exception>
    /// <exception cref="ArgumentException">The figure at that resolution is not a whole number of pixels from 1 up.</exception>
    public static SKBitmap RenderToBitmap(this IChartRenderer renderer, Figure figure, double dpi)
    {
        RasterSize size = RasterSize.For(figure, dpi);
        var bitmap = new SKBitmap(size.Width, size.Height);
        try
        {
            using var canvas = new SKCanvas(bitmap);
            canvas.Clear(SKColors.White);
            if (size.ScaleX != 1f || size.ScaleY != 1f)
            {
                canvas.Scale(size.ScaleX, size.ScaleY);
            }

            renderer.Render(figure, new SkiaRenderContext(canvas));
            return bitmap;
        }
        catch
        {
            bitmap.Dispose();
            throw;
        }
    }
}
