// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.Downsampling;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Abstract base for all series renderers. Provides virtual access to the rendering context
/// and shared helper methods that subclasses can override to customize behavior.</summary>
internal abstract class SeriesRenderer
{
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

    /// <summary>Initializes a new renderer with the supplied rendering context.</summary>
    /// <param name="context">The context containing transform, draw target, and theme data for this render pass.</param>
    protected SeriesRenderer(SeriesRenderContext context) => Context = context;

    /// <summary>Returns the series-specific color if set, otherwise the theme cycle color. Override to change fallback logic.</summary>
    protected virtual Color ResolveColor(Color? seriesColor) => seriesColor ?? SeriesColor;

    /// <summary>Returns the series-specific line style if set; falls back to the cycled style, then <see cref="LineStyle.Solid"/>.</summary>
    protected virtual LineStyle ResolveLineStyle(LineStyle? seriesLineStyle) =>
        seriesLineStyle ?? Context.CycledProps?.LineStyle ?? LineStyle.Solid;

    /// <summary>Returns the series-specific marker style if set; falls back to the cycled style, then <see cref="MarkerStyle.None"/>.</summary>
    protected virtual MarkerStyle ResolveMarkerStyle(MarkerStyle? seriesMarkerStyle) =>
        seriesMarkerStyle ?? Context.CycledProps?.MarkerStyle ?? MarkerStyle.None;

    /// <summary>Returns the series-specific line width if set; falls back to the cycled width, then 1.5.</summary>
    protected virtual double ResolveLineWidth(double? seriesLineWidth) =>
        seriesLineWidth ?? Context.CycledProps?.LineWidth ?? 1.5;

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

    /// <summary>Converts a normalized alpha (0.0–1.0) to a byte and applies it to the color.</summary>
    protected Color ApplyAlpha(Color color, double alpha) =>
        color.WithAlpha((byte)Math.Round(Math.Clamp(alpha, 0.0, 1.0) * 255));

    /// <summary>Applies viewport culling and LTTB downsampling to XY data when maxPoints is set.</summary>
    protected XYData ApplyDownsampling(double[] x, double[] y, int? maxPoints)
    {
        if (maxPoints is null || x.Length <= maxPoints.Value) return new(x, y);
        var culled = ViewportCuller.Cull(x, y, Transform.DataXMin, Transform.DataXMax);
        if (culled.X.Length <= maxPoints.Value) return culled;
        return new LttbDownsampler().Downsample(culled.X, culled.Y, maxPoints.Value);
    }
}

/// <summary>Generic typed series renderer. Each concrete renderer handles exactly one <typeparamref name="T"/> series type.</summary>
/// <typeparam name="T">The series type this renderer handles.</typeparam>
internal abstract class SeriesRenderer<T> : SeriesRenderer where T : ISeries
{
    /// <inheritdoc />
    protected SeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <summary>Renders the given series using the context provided at construction time.</summary>
    public abstract void Render(T series);
}
