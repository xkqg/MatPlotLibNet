// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using Microsoft.Maui.Graphics;

namespace MatPlotLibNet.Maui;

internal static class BrushSelectOverlayExtensions
{
    extension(ICanvas canvas)
    {
        /// <summary>Draws the band between where the drag began and where the finger is: a translucent fill and a firmer
        /// outline, whichever corner the drag began at, in the colours the other native heads use.</summary>
        /// <param name="state">The drag in progress, in the view's pixels.</param>
        internal void DrawBrushSelect(BrushSelectState state)
        {
            float x = (float)Math.Min(state.StartPixelX, state.CurrentPixelX);
            float y = (float)Math.Min(state.StartPixelY, state.CurrentPixelY);
            float width = (float)Math.Abs(state.CurrentPixelX - state.StartPixelX);
            float height = (float)Math.Abs(state.CurrentPixelY - state.StartPixelY);

            canvas.FillColor = Color.FromRgba(30, 144, 255, 50);
            canvas.FillRectangle(x, y, width, height);

            canvas.StrokeColor = Color.FromRgba(30, 144, 255, 180);
            canvas.StrokeSize = 1.5f;
            canvas.DrawRectangle(x, y, width, height);
        }
    }
}
