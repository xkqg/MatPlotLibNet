// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering;

/// <summary>Encapsulates the plot region bounds and render context for series rendering.</summary>
public sealed class RenderArea
{
    public Rect PlotBounds { get; }

    public IRenderContext Context { get; }

    /// <summary>Initializes a new render area with the specified bounds and context.</summary>
    public RenderArea(Rect plotBounds, IRenderContext context)
    {
        PlotBounds = plotBounds;
        Context = context;
    }
}
