// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering;

/// <summary>Encapsulates the plot region bounds and render context for series rendering.</summary>
public sealed class RenderArea
{
    /// <summary>Gets the pixel-space bounding rectangle of the plot area.</summary>
    public Rect PlotBounds { get; }

    /// <summary>Gets the render context used for drawing operations.</summary>
    public IRenderContext Context { get; }

    /// <summary>Initializes a new render area with the specified bounds and context.</summary>
    public RenderArea(Rect plotBounds, IRenderContext context)
    {
        PlotBounds = plotBounds;
        Context = context;
    }
}
