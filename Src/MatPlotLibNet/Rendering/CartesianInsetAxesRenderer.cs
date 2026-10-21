// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering;

/// <summary>Renders inset axes (<see cref="Axes.InsetBounds"/>): Cartesian axes drawn inside the parent's plot area.</summary>
/// <remarks>Right of an inset lies the PARENT's data, and no margin is reserved there for a column of end labels. So an
/// inset draws no end labels, and a series that asked for one keeps its legend entry: the name is neither lost nor drawn
/// over the parent plot. The answer flows through the one question every consumer asks
/// (<see cref="AxesRenderer.DrawsDirectLabels"/>): the legend, its measurer and the end-label owner
/// (<see cref="Layout.DirectLabelMetrics"/>) all ask the renderer the factory builds for the axes.</remarks>
internal sealed class CartesianInsetAxesRenderer : CartesianAxesRenderer
{
    /// <summary>Initializes a renderer for inset axes.</summary>
    /// <param name="axes">The inset axes.</param>
    /// <param name="plotArea">The inset's pixel-space rectangle inside the parent's plot area.</param>
    /// <param name="ctx">The drawing surface.</param>
    /// <param name="theme">The active theme.</param>
    public CartesianInsetAxesRenderer(Axes axes, Rect plotArea, IRenderContext ctx, Theme theme)
        : base(axes, plotArea, ctx, theme) { }

    /// <inheritdoc />
    /// <remarks>An inset has no right margin of its own, so it draws no end labels.</remarks>
    internal override bool DrawsDirectLabels => false;
}
