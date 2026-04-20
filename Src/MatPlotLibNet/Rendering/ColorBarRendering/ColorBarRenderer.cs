// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Rendering.ColorBarRendering;

/// <summary>
/// Abstract base for orientation-specific colorbar rendering. Replaces the
/// 150-line god-method <c>AxesRenderer.RenderColorBar</c> (which had 5 nested
/// switches mixing Horizontal/Vertical with Extend modes, edges, and label
/// positioning).
/// </summary>
/// <remarks>
/// <b>Phase B.1 of the strict-90 floor plan (2026-04-20).</b>
/// Two concrete subclasses — <see cref="HorizontalColorBarRenderer"/> and
/// <see cref="VerticalColorBarRenderer"/> — own the orientation-specific
/// layout logic via polymorphism, eliminating the inline switch in the
/// AxesRenderer.RenderColorBar god-method.
/// </remarks>
public abstract class ColorBarRenderer
{
    /// <summary>The colorbar configuration model (Orientation, Extend, Label, etc.).</summary>
    protected ColorBar Cb { get; }

    /// <summary>The resolved color map (from ColorBar.ColorMap, then data provider, then Viridis default).</summary>
    protected IColorMap ColorMap { get; }

    /// <summary>The lower bound of the colorbar value range.</summary>
    protected double Min { get; }

    /// <summary>The upper bound of the colorbar value range.</summary>
    protected double Max { get; }

    /// <summary>The plot area within which the colorbar is positioned (typically alongside the axes).</summary>
    protected Rect PlotArea { get; }

    /// <summary>The render context to draw into (SVG / Skia / etc.).</summary>
    protected IRenderContext Ctx { get; }

    /// <summary>The active visual theme (used for foreground text, frame border, etc.).</summary>
    protected Theme Theme { get; }

    /// <summary>The number of gradient steps along the colorbar (default 50).</summary>
    protected int Steps { get; }

    /// <summary>Number of tick labels along the colorbar (default 6 = 0%, 20%, 40%, 60%, 80%, 100%).</summary>
    protected const int TickCount = 6;

    /// <summary>Fraction of the colorbar length used for each extend wedge (default 0.10 = 10%).</summary>
    protected const double ExtendFrac = 0.10;

    /// <summary>True when <see cref="Cb"/>'s Extend includes Min (Min or Both).</summary>
    protected bool ExtendMin { get; }

    /// <summary>True when <see cref="Cb"/>'s Extend includes Max (Max or Both).</summary>
    protected bool ExtendMax { get; }

    /// <summary>Constructs a renderer with all the state needed to draw the colorbar.</summary>
    protected ColorBarRenderer(ColorBar cb, IColorMap colorMap, double min, double max,
                                Rect plotArea, IRenderContext ctx, Theme theme, int steps = 50)
    {
        Cb = cb;
        ColorMap = colorMap;
        Min = min;
        Max = max;
        PlotArea = plotArea;
        Ctx = ctx;
        Theme = theme;
        Steps = steps;
        ExtendMin = cb.Extend is ColorBarExtend.Min or ColorBarExtend.Both;
        ExtendMax = cb.Extend is ColorBarExtend.Max or ColorBarExtend.Both;
    }

    /// <summary>Renders the colorbar onto the context. Concrete subclasses implement
    /// orientation-specific layout (horizontal below the plot, vertical to the right).</summary>
    public abstract void Render();
}
