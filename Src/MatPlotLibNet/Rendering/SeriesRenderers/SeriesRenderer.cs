// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Abstract base for all series renderers. Provides virtual access to the rendering context
/// and shared helper methods that subclasses can override to customize behavior.</summary>
internal abstract class SeriesRenderer
{
    /// <summary>Gets the full rendering context record.</summary>
    protected SeriesRenderContext Context { get; }

    /// <summary>Gets the coordinate transform. Override to apply custom transformations.</summary>
    protected virtual DataTransform Transform => Context.Transform;

    /// <summary>Gets the drawing target. Override to intercept or wrap drawing calls.</summary>
    protected virtual IRenderContext Ctx => Context.Ctx;

    /// <summary>Gets the theme cycle color for this series. Override to change default coloring logic.</summary>
    protected virtual Color SeriesColor => Context.SeriesColor;

    /// <summary>Gets the plot area bounds. Override for custom layout behavior.</summary>
    protected virtual RenderArea Area => Context.Area;

    /// <summary>Gets whether tooltips are enabled. Override to force enable/disable per renderer.</summary>
    protected virtual bool TooltipsEnabled => Context.TooltipsEnabled;

    protected SeriesRenderer(SeriesRenderContext context) => Context = context;

    /// <summary>Returns the series-specific color if set, otherwise the theme cycle color. Override to change fallback logic.</summary>
    protected virtual Color ResolveColor(Color? seriesColor) => seriesColor ?? SeriesColor;

    /// <summary>Opens a tooltip wrapper around subsequent drawing calls. Override to customize tooltip format.</summary>
    protected virtual void BeginTooltip(string text)
    {
        if (TooltipsEnabled && Ctx is SvgRenderContext svgCtx)
            svgCtx.BeginTooltipGroup(text);
    }

    /// <summary>Closes the tooltip wrapper. Override if BeginTooltip was overridden.</summary>
    protected virtual void EndTooltip()
    {
        if (TooltipsEnabled && Ctx is SvgRenderContext svgCtx)
            svgCtx.EndTooltipGroup();
    }
}

/// <summary>Generic typed series renderer. Each concrete renderer handles exactly one <typeparamref name="T"/> series type.</summary>
/// <typeparam name="T">The series type this renderer handles.</typeparam>
internal abstract class SeriesRenderer<T> : SeriesRenderer where T : ISeries
{
    protected SeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <summary>Renders the given series using the context provided at construction time.</summary>
    public abstract void Render(T series);
}
