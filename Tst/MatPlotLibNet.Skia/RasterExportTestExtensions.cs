// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;
using SkiaSharp;

namespace MatPlotLibNet.Skia.Tests;

/// <summary>A raster export as a draw makes one: a context over a canvas that is a grid of pixels, told the theme it draws under.</summary>
internal static class RasterExportTestExtensions
{
    extension(SKCanvas canvas)
    {
        /// <summary>A context over this canvas as a raster export, told the looks of <paramref name="theme"/> as the drawing sequence
        /// tells it at the start of a stage.</summary>
        /// <param name="theme">The theme drawn under; one of the matplotlib themes puts the marks on the pixel grid.</param>
        internal SkiaRenderContext RasterExportUnder(Theme theme)
        {
            var context = new SkiaRenderContext(canvas) { SnapsToPixelGrid = true };
            context.UseLooksOf(theme);
            return context;
        }
    }
}
