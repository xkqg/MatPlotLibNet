// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.Frames;

/// <summary>The one paint of a native control: every head (WPF, Avalonia, Uno, MAUI) paints through it, and delivers the layout
/// it returns to its interaction host the way its own threading asks.</summary>
internal static class FrameTargetPaintExtensions
{
    /// <summary>The fill of the rubber band of a brush-select drag: dodger blue, translucent, so the marks under it show.</summary>
    private static readonly Color BandFill = Css4Colors.DodgerBlue.WithAlpha(50);

    /// <summary>The outline of the band, firmer than its fill.</summary>
    private static readonly Color BandOutline = Css4Colors.DodgerBlue.WithAlpha(180);

    /// <summary>How thick the band's outline is.</summary>
    private const double BandOutlineThickness = 1.5;

    extension(FrameTarget target)
    {
        /// <summary>Paints <paramref name="figure"/> onto this target with the process' chart renderer
        /// (<see cref="ChartRendererDefault.Current"/>) and draws the band of a brush-select drag in progress over it.</summary>
        /// <param name="figure">The figure to paint.</param>
        /// <param name="band">The drag in progress, or <see langword="null"/> for none.</param>
        /// <returns>The layout of the draw, for the control's interaction host.</returns>
        internal LayoutResult Paint(Figure figure, BrushSelectState? band)
        {
            var layout = ChartRendererDefault.Current.Paint(figure, target);
            if (band is { } drag) target.Overlay.DrawBrushSelect(drag);
            return layout;
        }
    }

    extension(IRenderContext ctx)
    {
        /// <summary>Draws the band between where a brush-select drag began and where the pointer is: a translucent fill under a
        /// firmer outline, whichever corner the drag began at.</summary>
        /// <param name="state">The drag in progress, in the control's pixels.</param>
        internal void DrawBrushSelect(BrushSelectState state)
        {
            double left = Math.Min(state.StartPixelX, state.CurrentPixelX);
            double top = Math.Min(state.StartPixelY, state.CurrentPixelY);
            var rect = new Rect(left, top, Math.Abs(state.CurrentPixelX - state.StartPixelX), Math.Abs(state.CurrentPixelY - state.StartPixelY));
            ctx.DrawRectangle(rect, ShapeStyle.Edged(BandFill, BandOutline, BandOutlineThickness));
        }
    }
}
