// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using SkiaSharp;

namespace MatPlotLibNet.Skia;

/// <summary>Draws the rubber band of a brush-select drag onto a SkiaSharp canvas, for the native controls that paint
/// with Skia (WPF, Avalonia, Uno).</summary>
public static class BrushSelectOverlayExtensions
{
    extension(SKCanvas canvas)
    {
        /// <summary>Draws the band between where the drag began and where the pointer is: a translucent fill and a firmer
        /// outline, whichever corner the drag began at.</summary>
        /// <param name="state">The drag in progress, in the control's pixels.</param>
        public void DrawBrushSelect(BrushSelectState state)
        {
            var rect = new SKRect(
                (float)Math.Min(state.StartPixelX, state.CurrentPixelX),
                (float)Math.Min(state.StartPixelY, state.CurrentPixelY),
                (float)Math.Max(state.StartPixelX, state.CurrentPixelX),
                (float)Math.Max(state.StartPixelY, state.CurrentPixelY));

            using var paint = new SKPaint { Color = new SKColor(30, 144, 255, 50), Style = SKPaintStyle.Fill };
            canvas.DrawRect(rect, paint);

            paint.Color = new SKColor(30, 144, 255, 180);
            paint.Style = SKPaintStyle.Stroke;
            paint.StrokeWidth = 1.5f;
            canvas.DrawRect(rect, paint);
        }
    }
}
