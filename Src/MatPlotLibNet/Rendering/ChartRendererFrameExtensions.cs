// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering.Frames;

namespace MatPlotLibNet.Rendering;

/// <summary>Any renderer drawn onto a <see cref="FrameTarget"/>. The library's renderer draws once, onto the target
/// (<see cref="IFrameChartRenderer"/>); a renderer of the caller's knows nothing of targets, so it draws with its
/// <see cref="IChartRenderer.Render"/> onto the target's surface, keeping nothing, and lays out with its
/// <see cref="IChartRenderer.ComputeLayout"/>.</summary>
internal static class ChartRendererFrameExtensions
{
    extension(IChartRenderer renderer)
    {
        /// <summary>Draws <paramref name="figure"/> onto <paramref name="target"/>, the way an export draws: no layout is asked
        /// for.</summary>
        /// <param name="figure">The figure to draw.</param>
        /// <param name="target">What the draw goes onto.</param>
        internal void Draw(Figure figure, FrameTarget target)
        {
            if (renderer is IFrameChartRenderer frames) frames.Draw(figure, target);
            else renderer.Render(figure, target.Background);
        }

        /// <summary>Draws <paramref name="figure"/> onto <paramref name="target"/> and returns the layout of the draw, the way a
        /// native control paints.</summary>
        /// <param name="figure">The figure to paint.</param>
        /// <param name="target">What the paint goes onto.</param>
        /// <returns>The plot areas and legend item bounds of the draw.</returns>
        internal LayoutResult Paint(Figure figure, FrameTarget target)
        {
            if (renderer is IFrameChartRenderer frames) return frames.Paint(figure, target);
            renderer.Render(figure, target.Background);
            return renderer.ComputeLayout(figure, target.Background);
        }
    }
}
