// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;

namespace MatPlotLibNet.Transforms;

/// <summary>Transforms a <see cref="Figure"/> into SVG format. Also implements <see cref="ISvgRenderer"/> for backward compatibility.</summary>
public sealed class SvgTransform : FigureTransform, ISvgRenderer
{
    /// <summary>Initializes a new SVG transform with the specified chart renderer.</summary>
    public SvgTransform(IChartRenderer renderer) : base(renderer) { }

    /// <summary>Initializes a new SVG transform with the default chart renderer.</summary>
    public SvgTransform() { }

    /// <summary>Renders the figure to a complete SVG string.</summary>
    public string Render(Figure figure)
    {
        var w = figure.Width.ToString("G", CultureInfo.InvariantCulture);
        var h = figure.Height.ToString("G", CultureInfo.InvariantCulture);

        // Resolve the effective spacing via the shared PrepareSpacing helper. This runs
        // ConstrainedLayoutEngine.Compute when the figure has TightLayout or
        // ConstrainedLayout enabled — the SAME logic ChartRenderer.Render uses for the
        // PNG/PDF paths. Previously SvgTransform skipped this step entirely, so tight-
        // and constrained-layout figures had broken SVG margins (axis labels overlapping
        // data, colorbars clipped) while their PNGs rendered correctly.
        var bgCtx = new SvgRenderContext();
        var resolvedSpacing = Renderer.PrepareSpacing(figure, bgCtx);

        // Render background + title sequentially into the bgCtx fragment.
        double plotAreaTop = Renderer.RenderBackground(figure, bgCtx, resolvedSpacing);

        if (figure.SubPlots.Count == 0)
            return BuildSvgDocument(w, h, figure, sb => bgCtx.WriteTo(sb));

        // Compute subplot layout with the resolved spacing — guarantees margins and
        // gutters match the PNG pipeline.
        var plotAreas = Renderer.ComputeSubPlotLayout(figure, plotAreaTop, resolvedSpacing);
        var theme = figure.Theme;

        // Propagate interactivity flags to each axes before rendering
        if (figure.HasInteractivity)
            foreach (var axes in figure.SubPlots)
                axes.EnableInteractiveAttributes = true;

        // Propagate 3D rotation flag to 3D axes
        if (figure.Enable3DRotation)
            foreach (var axes in figure.SubPlots)
                if (axes.CoordinateSystem == CoordinateSystem.ThreeD)
                    axes.Emit3DVertexData = true;

        // Render subplots in parallel (each gets its own context). We pass `figure` explicitly
        // so the 3-D square-cube layout is applied — matches PngTransform's full-Render path.
        var subplotContexts = new SvgRenderContext[figure.SubPlots.Count];
        Parallel.For(0, figure.SubPlots.Count, i =>
        {
            var ctx = new SvgRenderContext();
            Renderer.RenderAxes(figure, figure.SubPlots[i], plotAreas[i], ctx, theme);
            subplotContexts[i] = ctx;
        });

        // Render figure-level colorbar (if present) after all subplots — pass the
        // resolved spacing so colorbar positioning matches PNG exactly.
        SvgRenderContext? figCbCtx = null;
        if (figure.FigureColorBar is { Visible: true } cb)
        {
            figCbCtx = new SvgRenderContext();
            Renderer.RenderFigureColorBar(figure, plotAreas, cb, figCbCtx, resolvedSpacing);
        }

        return BuildSvgDocument(w, h, figure, sb =>
        {
            bgCtx.WriteTo(sb);
            foreach (var ctx in subplotContexts)
                ctx.WriteTo(sb);
            figCbCtx?.WriteTo(sb);
            // ServerInteraction routes browser events through SignalR instead of the local
            // client-side IIFEs — the SignalR dispatcher replaces all four local scripts
            // (zoom/pan, legend-toggle, selection/brush, tooltip) with one unified branch
            // that invokes ChartHub methods. Opted-in branches are included; others are
            // omitted for size. Emission order is preserved from v1.2.1 so existing static
            // figures produce byte-identical SVG output.
            if (figure.ServerInteraction)
            {
                sb.AppendLine(SvgSignalRInteractionScript.GetScript(
                    enableBrushSelect: figure.EnableSelection,
                    enableHover: figure.EnableRichTooltips));
            }
            else
            {
                if (figure.EnableZoomPan)
                    sb.AppendLine(SvgInteractivityScript.GetZoomPanScript());
                if (figure.EnableLegendToggle)
                    sb.AppendLine(SvgLegendToggleScript.GetScript());
            }
            // v1.2.0 preserved order: RichTooltips / Highlight / Selection are always
            // emitted after the zoom/pan/legend block for static figures. v1.2.2 skips
            // RichTooltips and Selection for ServerInteraction figures (the dispatcher
            // replaces them) but keeps Highlight emission unchanged.
            if (!figure.ServerInteraction && figure.EnableRichTooltips)
                sb.AppendLine(SvgCustomTooltipScript.GetScript());
            if (figure.EnableHighlight)
                sb.AppendLine(SvgHighlightScript.GetScript());
            if (!figure.ServerInteraction && figure.EnableSelection)
                sb.AppendLine(SvgSelectionScript.GetScript());
            if (figure.Enable3DRotation)
                sb.AppendLine(Svg3DRotationScript.GetScript());
            if (figure.EnableTreemapDrilldown)
                sb.AppendLine(SvgTreemapDrilldownScript.GetScript());
            if (figure.EnableSankeyHover)
                sb.AppendLine(SvgSankeyHoverScript.GetScript());
        });
    }

    /// <inheritdoc />
    public override void Transform(Figure figure, Stream output)
    {
        var svg = Render(figure);
        var bytes = Encoding.UTF8.GetBytes(svg);
        output.Write(bytes, 0, bytes.Length);
    }

    private static string BuildSvgDocument(string w, string h, Figure figure, Action<StringBuilder> writeBody)
    {
        var sb = new StringBuilder(512);

        // Determine alt-text: prefer AltText, fall back to Title, then nothing (empty title still written)
        string altText = figure.AltText ?? figure.Title ?? string.Empty;
        bool hasDescription = figure.Description is not null;

        sb.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 ")
          .Append(w).Append(' ').Append(h)
          .Append("\" width=\"").Append(w)
          .Append("\" height=\"").Append(h);
        if (figure.ResponsiveSvg)
            sb.Append("\" style=\"max-width:100%;height:auto");
        sb.Append("\" role=\"img\"")
          .Append(" aria-labelledby=\"chart-title\"");
        if (hasDescription)
            sb.Append(" aria-describedby=\"chart-desc\"");
        if (figure.ServerInteraction && figure.ChartId is { } chartId)
        {
            sb.Append(" data-chart-id=\"").Append(SvgXmlHelper.EscapeXml(chartId)).Append('"');
            // Phase G.8 of v1.7.2 follow-on plan — emit axis limits on the root SVG so
            // the SignalR interaction script can read its initial view state AND the
            // reset view (Home key target) without extra round-trips. Pre-fix the
            // script read `data-xmin/xmax/ymin/ymax` + `data-reset-*` but nothing ever
            // emitted them — so every wheel/pan/reset invoke() silently bailed on
            // `if (!isFinite(xMin))`. Pull from the first subplot's data ranges.
            if (figure.SubPlots.Count > 0)
            {
                var ax0 = figure.SubPlots[0];
                double xmin = ax0.XAxis.Min ?? 0, xmax = ax0.XAxis.Max ?? 1;
                double ymin = ax0.YAxis.Min ?? 0, ymax = ax0.YAxis.Max ?? 1;
                var invA = System.Globalization.CultureInfo.InvariantCulture;
                sb.Append(" data-xmin=\"").Append(xmin.ToString("G6", invA))
                  .Append("\" data-xmax=\"").Append(xmax.ToString("G6", invA))
                  .Append("\" data-ymin=\"").Append(ymin.ToString("G6", invA))
                  .Append("\" data-ymax=\"").Append(ymax.ToString("G6", invA)).Append('"');
                sb.Append(" data-reset-xmin=\"").Append(xmin.ToString("G6", invA))
                  .Append("\" data-reset-xmax=\"").Append(xmax.ToString("G6", invA))
                  .Append("\" data-reset-ymin=\"").Append(ymin.ToString("G6", invA))
                  .Append("\" data-reset-ymax=\"").Append(ymax.ToString("G6", invA)).Append('"');
            }
        }
        // Phase 7 of v1.7.2 plan — emit non-default interaction-theme tokens as
        // data-mpl-* attributes so the embedded scripts can read them at runtime
        // without recompiling. Only non-default values are emitted (zero-config callers
        // get byte-identical output to v1.7.1).
        var t = figure.InteractionTheme;
        var d = Models.InteractionTheme.Default;
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        if (t.HighlightOpacity != d.HighlightOpacity)
            sb.Append(" data-mpl-highlight-opacity=\"").Append(t.HighlightOpacity.ToString("G6", inv)).Append('"');
        if (t.SankeyDimLinkOpacity != d.SankeyDimLinkOpacity)
            sb.Append(" data-mpl-sankey-link-opacity=\"").Append(t.SankeyDimLinkOpacity.ToString("G6", inv)).Append('"');
        if (t.SankeyDimNodeOpacity != d.SankeyDimNodeOpacity)
            sb.Append(" data-mpl-sankey-node-opacity=\"").Append(t.SankeyDimNodeOpacity.ToString("G6", inv)).Append('"');
        if (t.TreemapTransitionMs != d.TreemapTransitionMs)
            sb.Append(" data-mpl-treemap-transition-ms=\"").Append(t.TreemapTransitionMs).Append('"');
        if (t.TooltipOffsetX != d.TooltipOffsetX)
            sb.Append(" data-mpl-tooltip-offset-x=\"").Append(t.TooltipOffsetX.ToString("G6", inv)).Append('"');
        if (t.TooltipOffsetY != d.TooltipOffsetY)
            sb.Append(" data-mpl-tooltip-offset-y=\"").Append(t.TooltipOffsetY.ToString("G6", inv)).Append('"');
        sb.AppendLine(">");

        // SVG title (always emitted for role="img" accessibility)
        sb.Append("<title id=\"chart-title\">")
          .Append(SvgXmlHelper.EscapeXml(altText))
          .AppendLine("</title>");

        if (hasDescription)
        {
            sb.Append("<desc id=\"chart-desc\">")
              .Append(SvgXmlHelper.EscapeXml(figure.Description!))
              .AppendLine("</desc>");
        }

        writeBody(sb);
        sb.AppendLine("</svg>");
        return sb.ToString();
    }
}
