// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Rendering.Frames;

/// <summary>What one draw of a figure goes onto, as the one drawing sequence (<see cref="ChartRenderer"/>) sees it: a surface for
/// what lies under the subplots (the background, the title, the figure's images), one draw of each subplot, and a surface for what
/// lies above them. The sequence, the layout and the order are the renderer's; a target decides only where each stage lands and
/// whether a subplot is drawn or handed out as it was kept. A target is made for one draw.</summary>
internal abstract class FrameTarget
{
    /// <summary>The surface the background, the title and the underlays are drawn on; the frame's text is measured with it.</summary>
    internal abstract IRenderContext Background { get; }

    /// <summary>The surface what lies above the subplots is drawn on: the figure's colour bar, its texts and its legend.</summary>
    internal abstract IRenderContext Overlay { get; }

    /// <summary>Draws the subplot <paramref name="request"/> names with <paramref name="draw"/>, or hands out what was kept of it.</summary>
    /// <param name="request">The subplot and its plot area.</param>
    /// <param name="draw">Draws the subplot onto the context it is given and returns the bounds of its legend items.</param>
    /// <returns>The bounds of the subplot's legend items in this draw, for the layout a control hit-tests with.</returns>
    internal abstract IReadOnlyList<LegendItemBounds> DrawAxes(in AxesDrawRequest request,
        Func<IRenderContext, IReadOnlyList<LegendItemBounds>> draw);
}

/// <summary>A subplot the drawing sequence asks a target to draw.</summary>
/// <param name="Figure">The figure being drawn.</param>
/// <param name="Index">The subplot's index in <see cref="Figure.SubPlots"/>.</param>
/// <param name="PlotArea">Its plot area in this draw's frame.</param>
internal readonly record struct AxesDrawRequest(Figure Figure, int Index, Rect PlotArea)
{
    /// <summary>The subplot.</summary>
    internal Axes Axes => Figure.SubPlots[Index];
}

/// <summary>The target that draws everything onto one surface, every time: a PNG or a PDF export, a single
/// <see cref="ChartRenderer.Render"/>, a surface that keeps nothing between draws.</summary>
/// <param name="ctx">The surface.</param>
internal sealed class DirectFrameTarget(IRenderContext ctx) : FrameTarget
{
    /// <inheritdoc />
    internal override IRenderContext Background => ctx;

    /// <inheritdoc />
    internal override IRenderContext Overlay => ctx;

    /// <inheritdoc />
    internal override IReadOnlyList<LegendItemBounds> DrawAxes(in AxesDrawRequest request,
        Func<IRenderContext, IReadOnlyList<LegendItemBounds>> draw) => draw(ctx);
}
