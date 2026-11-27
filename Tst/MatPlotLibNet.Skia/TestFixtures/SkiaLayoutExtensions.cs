// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using SkiaSharp;

namespace MatPlotLibNet.Skia.Tests;

/// <summary>How a Skia test finds where the layout puts a figure's plot: laid out on a Skia surface, the one the figure is later drawn on.</summary>
public static class SkiaLayoutExtensions
{
    extension(Figure figure)
    {
        /// <summary>The plot area the layout gives the first axes of the figure, measured on a Skia surface.</summary>
        public Rect PlotAreaOnSkia()
        {
            using var bitmap = new SKBitmap(1, 1);
            using var canvas = new SKCanvas(bitmap);
            return new ChartRenderer().ComputeLayout(figure, new SkiaRenderContext(canvas)).PlotAreas[0];
        }
    }
}
