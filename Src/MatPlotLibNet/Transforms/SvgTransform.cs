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
            if (figure.EnableZoomPan)
                sb.AppendLine(SvgInteractivityScript.GetZoomPanScript());
            if (figure.EnableLegendToggle)
                sb.AppendLine(SvgLegendToggleScript.GetScript());
            if (figure.EnableRichTooltips)
                sb.AppendLine(SvgCustomTooltipScript.GetScript());
            if (figure.EnableHighlight)
                sb.AppendLine(SvgHighlightScript.GetScript());
            if (figure.EnableSelection)
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
          .Append("\" height=\"").Append(h)
          .Append("\" role=\"img\"")
          .Append(" aria-labelledby=\"chart-title\"");
        if (hasDescription)
            sb.Append(" aria-describedby=\"chart-desc\"");
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
