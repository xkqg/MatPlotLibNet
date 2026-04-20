// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.CartesianParts;

/// <summary>
/// Abstract base for a single responsibility within <c>CartesianAxesRenderer.Render</c>.
/// The Composite pattern — the orchestrator (<see cref="CartesianAxesRenderer"/>) walks
/// an ordered list of parts and calls <see cref="Render"/> on each, rather than
/// inlining 23 distinct steps in one method.
/// </summary>
/// <remarks>
/// <b>Phase B.4 of the strict-90 floor plan (2026-04-20).</b>
/// Each concrete subclass owns ONE responsibility: Annotations, Spans,
/// ReferenceLines, SecondaryYAxis, SecondaryXAxis, Signals. The orchestrator
/// keeps the overall sequencing + delegates to the parts.
/// </remarks>
public abstract class CartesianAxesPart
{
    /// <summary>The axes model (series, annotations, spans, etc. read from here).</summary>
    protected Axes Axes { get; }

    /// <summary>Pixel-space plot-area rectangle.</summary>
    protected Rect PlotArea { get; }

    /// <summary>Render context (Ctx.DrawText / DrawLine / DrawRectangle etc.).</summary>
    protected IRenderContext Ctx { get; }

    /// <summary>Active theme (for colors, default font).</summary>
    protected Theme Theme { get; }

    /// <summary>Data→pixel coordinate transform for the primary axes.</summary>
    protected DataTransform Transform { get; }

    /// <summary>Base constructor with shared per-axes state.</summary>
    protected CartesianAxesPart(Axes axes, Rect plotArea, IRenderContext ctx, Theme theme, DataTransform transform)
    {
        Axes = axes;
        PlotArea = plotArea;
        Ctx = ctx;
        Theme = theme;
        Transform = transform;
    }

    /// <summary>Returns the tick font from the theme (shared convenience).</summary>
    protected Font TickFont() => ThemedFontProvider.TickFont(Theme);

    /// <summary>Returns the axis-label font from the theme (shared convenience).</summary>
    protected Font LabelFont() => ThemedFontProvider.LabelFont(Theme);

    /// <summary>Renders this part's responsibility onto <see cref="Ctx"/>.</summary>
    public abstract void Render();
}
