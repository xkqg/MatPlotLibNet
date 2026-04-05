// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

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

        // Render background + title sequentially
        var bgCtx = new SvgRenderContext();
        double plotAreaTop = Renderer.RenderBackground(figure, bgCtx);

        if (figure.SubPlots.Count == 0)
            return BuildSvgDocument(w, h, sb => bgCtx.WriteTo(sb));

        // Compute subplot layout
        var plotAreas = Renderer.ComputeSubPlotLayout(figure, plotAreaTop);
        var theme = figure.Theme;

        // Render subplots in parallel (each gets its own context)
        var subplotContexts = new SvgRenderContext[figure.SubPlots.Count];
        Parallel.For(0, figure.SubPlots.Count, i =>
        {
            var ctx = new SvgRenderContext();
            Renderer.RenderAxes(figure.SubPlots[i], plotAreas[i], ctx, theme);
            subplotContexts[i] = ctx;
        });

        return BuildSvgDocument(w, h, sb =>
        {
            bgCtx.WriteTo(sb);
            foreach (var ctx in subplotContexts)
                ctx.WriteTo(sb);
            if (figure.EnableZoomPan)
                sb.AppendLine(SvgInteractivityScript.GetZoomPanScript());
        });
    }

    /// <inheritdoc />
    public override void Transform(Figure figure, Stream output)
    {
        var svg = Render(figure);
        var bytes = Encoding.UTF8.GetBytes(svg);
        output.Write(bytes, 0, bytes.Length);
    }

    private static string BuildSvgDocument(string w, string h, Action<StringBuilder> writeBody)
    {
        var sb = new StringBuilder(512);
        sb.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 ")
          .Append(w).Append(' ').Append(h)
          .Append("\" width=\"").Append(w)
          .Append("\" height=\"").Append(h).AppendLine("\">");
        writeBody(sb);
        sb.AppendLine("</svg>");
        return sb.ToString();
    }
}
