// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Skia;
using SkiaSharp;

namespace MatPlotLibNet.Benchmarks.Drag;

/// <summary>The Skia side of the drag run: one raster surface and one painter, and the two ways a frame is drawn onto them. It
/// is a class of its own so that an SVG run, which must not load the Skia package unless asked, never touches it.</summary>
public sealed class SkiaDragPaints : IDisposable
{
    private readonly SKSurface _surface = SKSurface.Create(new SKImageInfo(1200, 900));
    private readonly SkiaChartPainter _painter = new();

    /// <summary>Loads the Skia package, which installs its glyph-path provider as it loads, so SVG text is written as outlines.</summary>
    public static void Load() =>
        System.Runtime.CompilerServices.RuntimeHelpers.RunModuleConstructor(typeof(SkiaRenderContext).Module.ModuleHandle);

    /// <summary>The figure drawn onto the canvas through the library's renderer, as a PNG export does.</summary>
    public void Render(Figure figure)
    {
        _surface.Canvas.Clear();
        ChartRendererDefault.Current.Render(figure, new SkiaRenderContext(_surface.Canvas));
    }

    /// <summary>The figure painted as a native control paints it: through the painter, which keeps the subplots that did not change.</summary>
    public void Control(Figure figure)
    {
        _surface.Canvas.Clear();
        _painter.Paint(_surface.Canvas, figure, null);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _painter.Dispose();
        _surface.Dispose();
    }
}
