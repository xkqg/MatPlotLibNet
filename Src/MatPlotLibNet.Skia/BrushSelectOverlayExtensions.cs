// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Rendering.Frames;
using SkiaSharp;

namespace MatPlotLibNet.Skia;

/// <summary>Draws the rubber band of a brush-select drag onto a SkiaSharp canvas, for the native controls that paint
/// with Skia (WPF, Avalonia, Uno).</summary>
public static class BrushSelectOverlayExtensions
{
    extension(SKCanvas canvas)
    {
        /// <summary>Draws the band between where the drag began and where the pointer is: a translucent fill and a firmer
        /// outline, whichever corner the drag began at, by the one rule every native head draws it with (the native controls
        /// draw it through their painter).</summary>
        /// <param name="state">The drag in progress, in the control's pixels.</param>
        public void DrawBrushSelect(BrushSelectState state) =>
            ((MatPlotLibNet.Rendering.IRenderContext)new SkiaRenderContext(canvas)).DrawBrushSelect(state);
    }
}
