// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.CartesianParts;

/// <summary>
/// Abstract base for one thing <c>CartesianAxesRenderer.Render</c> draws on a Cartesian axes. The renderer
/// (<see cref="CartesianAxesRenderer"/>) keeps the order the things are drawn in: at its place in that order it makes the
/// part, with the transform of this draw, and calls <see cref="Render"/> on it.
/// </summary>
/// <remarks>
/// Each concrete part draws one thing: the spans, the patches, the reference lines, the drawing tools, the axis break
/// marks, the annotations and the signal markers; every axis is drawn by the axis part (<c>CartesianAxisPart</c>). Every part is
/// internal to the library and is built from the render's context (<c>CartesianPartContext</c>).
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

    /// <summary>The constructor every part of the library takes: the render's context, which holds the axes, the plot area, the
    /// surface, the theme and the transform the part draws through.</summary>
    /// <param name="context">What the render draws with.</param>
    private protected CartesianAxesPart(in CartesianPartContext context)
        : this(context.Axes, context.PlotArea, context.Ctx, context.Theme, context.Transform) { }

    /// <summary>Returns the tick font from the theme (shared convenience).</summary>
    protected Font TickFont() => Theme.TickFont();

    /// <summary>Returns the axis-label font from the theme (shared convenience).</summary>
    protected Font LabelFont() => Theme.LabelFont();

    /// <summary>Renders this part's responsibility onto <see cref="Ctx"/>.</summary>
    public abstract void Render();
}
