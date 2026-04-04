// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Rendering.Svg;

/// <summary>Produces a complete SVG document string from a <see cref="Figure"/>.</summary>
public sealed class SvgRenderer : ISvgRenderer
{
    private readonly ChartRenderer _renderer;

    /// <summary>Initializes a new SVG renderer with the specified chart renderer.</summary>
    public SvgRenderer(IChartRenderer renderer)
    {
        _renderer = (ChartRenderer)renderer;
    }

    /// <summary>Renders the figure to a standalone SVG string including the XML header element.</summary>
    /// <param name="figure">The figure to render.</param>
    /// <returns>A complete SVG document as a string.</returns>
    public string Render(Figure figure)
    {
        var w = figure.Width.ToString("G", CultureInfo.InvariantCulture);
        var h = figure.Height.ToString("G", CultureInfo.InvariantCulture);

        // Render background + title sequentially
        var bgCtx = new SvgRenderContext();
        double plotAreaTop = _renderer.RenderBackground(figure, bgCtx);

        if (figure.SubPlots.Count == 0)
            return BuildSvg(w, h, bgCtx);

        // Compute subplot layout
        var plotAreas = _renderer.ComputeSubPlotLayout(figure, plotAreaTop);
        var theme = figure.Theme;

        // Render subplots in parallel (each gets its own context)
        var subplotContexts = new SvgRenderContext[figure.SubPlots.Count];
        Parallel.For(0, figure.SubPlots.Count, i =>
        {
            var ctx = new SvgRenderContext();
            _renderer.RenderAxes(figure.SubPlots[i], plotAreas[i], ctx, theme);
            subplotContexts[i] = ctx;
        });

        // Merge: background first, then subplots in order
        int totalLength = bgCtx.OutputLength + 128;
        foreach (var ctx in subplotContexts)
            totalLength += ctx.OutputLength;

        var sb = new StringBuilder(totalLength);
        sb.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 ")
          .Append(w).Append(' ').Append(h)
          .Append("\" width=\"").Append(w)
          .Append("\" height=\"").Append(h).AppendLine("\">");
        bgCtx.WriteTo(sb);
        foreach (var ctx in subplotContexts)
            ctx.WriteTo(sb);
        sb.AppendLine("</svg>");
        return sb.ToString();
    }

    private static string BuildSvg(string w, string h, SvgRenderContext bgCtx)
    {
        var sb = new StringBuilder(bgCtx.OutputLength + 128);
        sb.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 ")
          .Append(w).Append(' ').Append(h)
          .Append("\" width=\"").Append(w)
          .Append("\" height=\"").Append(h).AppendLine("\">");
        bgCtx.WriteTo(sb);
        sb.AppendLine("</svg>");
        return sb.ToString();
    }
}
