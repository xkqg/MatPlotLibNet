// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering.Frames;

namespace MatPlotLibNet.Rendering;

/// <summary>A renderer that draws onto a <see cref="FrameTarget"/> in one pass, which hands an unchanged subplot out as it was
/// drawn: the library's <see cref="ChartRenderer"/>. Any <see cref="IChartRenderer"/> is drawn onto a target through the same
/// names (<see cref="ChartRendererFrameExtensions"/>). They live here and not on <see cref="IChartRenderer"/> so that an
/// application can still implement, proxy or mock the public interface.</summary>
internal interface IFrameChartRenderer : IChartRenderer
{
    /// <summary>Draws <paramref name="figure"/> onto <paramref name="target"/>, the way an export draws: no layout is asked for.</summary>
    /// <param name="figure">The figure to draw.</param>
    /// <param name="target">What the draw goes onto.</param>
    internal void Draw(Figure figure, FrameTarget target);

    /// <summary>Draws <paramref name="figure"/> onto <paramref name="target"/> and returns the layout its one draw was made
    /// with, the way a native control paints.</summary>
    /// <param name="figure">The figure to paint.</param>
    /// <param name="target">What the paint goes onto.</param>
    /// <returns>The plot areas and legend item bounds of the draw.</returns>
    internal LayoutResult Paint(Figure figure, FrameTarget target);
}
