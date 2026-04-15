// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text;
using MatPlotLibNet;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Rendering.TextMeasurement;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.Svg;

/// <summary>An <see cref="IRenderContext"/> implementation that emits SVG markup for each drawing operation.</summary>
public sealed class SvgRenderContext : IRenderContext
{
    private readonly StringBuilder _sb = new();
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    private int _clipId;
    private List<(string Key, string Value)>? _pendingData;

    /// <summary>Returns the accumulated SVG markup as a string.</summary>
    public string GetOutput() => _sb.ToString();

    /// <summary>Gets the current length of the accumulated SVG output.</summary>
    public int OutputLength => _sb.Length;

    /// <summary>Appends the accumulated SVG markup to an existing StringBuilder (avoids extra string allocation).</summary>
    public void WriteTo(StringBuilder target) => target.Append(_sb);

    /// <inheritdoc />
    public void SetNextElementData(string key, string value)
    {
        _pendingData ??= [];
        _pendingData.Add((key, value));
    }

    private void FlushPendingData()
    {
        if (_pendingData is null || _pendingData.Count == 0) return;
        foreach (var (key, value) in _pendingData)
            _sb.Append(" data-").Append(key).Append("=\"").Append(value).Append('"');
        _pendingData.Clear();
    }

    /// <inheritdoc />
    public void DrawLine(Point p1, Point p2, Color color, double thickness, LineStyle style)
    {
        _sb.Append("<line x1=\"").Append(F(p1.X)).Append("\" y1=\"").Append(F(p1.Y))
           .Append("\" x2=\"").Append(F(p2.X)).Append("\" y2=\"").Append(F(p2.Y))
           .Append("\" stroke=\"").Append(color.ToHex()).Append("\" stroke-width=\"").Append(F(thickness)).Append('"');
        AppendDashArray(style);
        FlushPendingData();
        _sb.AppendLine(" />");
    }

    /// <inheritdoc />
    public void DrawLines(IReadOnlyList<Point> points, Color color, double thickness, LineStyle style)
    {
        if (points.Count < 2) return;
        _sb.Append("<polyline points=\"");
        for (int i = 0; i < points.Count; i++)
        {
            if (i > 0) _sb.Append(' ');
            _sb.Append(F(points[i].X)).Append(',').Append(F(points[i].Y));
        }
        _sb.Append("\" fill=\"none\" stroke=\"").Append(color.ToHex())
           .Append("\" stroke-width=\"").Append(F(thickness)).Append('"');
        AppendDashArray(style);
        FlushPendingData();
        _sb.AppendLine(" />");
    }

    /// <inheritdoc />
    public void DrawPolygon(IReadOnlyList<Point> points, Color? fill, Color? stroke, double strokeThickness)
    {
        _sb.Append("<polygon points=\"");
        for (int i = 0; i < points.Count; i++)
        {
            if (i > 0) _sb.Append(' ');
            _sb.Append(F(points[i].X)).Append(',').Append(F(points[i].Y));
        }
        _sb.Append('"');
        AppendFillStroke(fill, stroke, strokeThickness);
        FlushPendingData();
        _sb.AppendLine(" />");
    }

    /// <inheritdoc />
    public void DrawCircle(Point center, double radius, Color? fill, Color? stroke, double strokeThickness)
    {
        _sb.Append("<circle cx=\"").Append(F(center.X)).Append("\" cy=\"").Append(F(center.Y))
           .Append("\" r=\"").Append(F(radius)).Append('"');
        AppendFillStroke(fill, stroke, strokeThickness);
        FlushPendingData();
        _sb.AppendLine(" />");
    }

    /// <inheritdoc />
    public void DrawRectangle(Rect rect, Color? fill, Color? stroke, double strokeThickness)
    {
        _sb.Append("<rect x=\"").Append(F(rect.X)).Append("\" y=\"").Append(F(rect.Y))
           .Append("\" width=\"").Append(F(rect.Width)).Append("\" height=\"").Append(F(rect.Height)).Append('"');
        AppendFillStroke(fill, stroke, strokeThickness);
        FlushPendingData();
        _sb.AppendLine(" />");
    }

    /// <inheritdoc />
    public void DrawEllipse(Rect bounds, Color? fill, Color? stroke, double strokeThickness)
    {
        double cx = bounds.X + bounds.Width / 2;
        double cy = bounds.Y + bounds.Height / 2;
        _sb.Append("<ellipse cx=\"").Append(F(cx)).Append("\" cy=\"").Append(F(cy))
           .Append("\" rx=\"").Append(F(bounds.Width / 2)).Append("\" ry=\"").Append(F(bounds.Height / 2)).Append('"');
        AppendFillStroke(fill, stroke, strokeThickness);
        FlushPendingData();
        _sb.AppendLine(" />");
    }

    /// <inheritdoc />
    public void DrawText(string text, Point position, Font font, TextAlignment alignment)
        => DrawText(text, position, font, alignment, rotation: 0);

    /// <inheritdoc />
    public void DrawText(string text, Point position, Font font, TextAlignment alignment, double rotation)
    {
        // Path mode (preferred): when a glyph path provider is registered (happens
        // automatically when MatPlotLibNet.Skia loads), emit the text as vector glyph
        // outlines rather than <text> elements. That makes the SVG self-contained —
        // it renders identically in any viewer regardless of installed fonts, and the
        // layout matches the PNG pipeline byte-for-byte because both backends use the
        // same DejaVu Sans glyph metrics.
        var provider = ChartServices.GlyphPathProvider;
        if (provider is not null)
        {
            string? d = provider.GetPathData(text, font);
            if (d is not null)
            {
                EmitGlyphPath(d, text, position, font, alignment, rotation);
                return;
            }
        }

        // Legacy <text> fallback: used when no glyph path provider is installed (pure-
        // managed consumers without the Skia package). The browser draws the glyphs
        // using whichever font it can resolve from the font-family stack; layout may
        // not match PNG exactly in that configuration.
        EmitTextElement(text, position, font, alignment, rotation);
    }

    private void EmitTextElement(string text, Point position, Font font, TextAlignment alignment, double rotation)
    {
        string anchor = alignment switch
        {
            TextAlignment.Left   => "start",
            TextAlignment.Center => "middle",
            TextAlignment.Right  => "end",
            _                    => "start",
        };

        _sb.Append("<text x=\"").Append(F(position.X)).Append("\" y=\"").Append(F(position.Y))
           .Append("\" font-family=\"").Append(font.Family).Append("\" font-size=\"").Append(F(font.Size))
           .Append("\" text-anchor=\"").Append(anchor).Append('"');
        if (rotation != 0)
        {
            // SVG rotation: negative because SVG y-axis is flipped vs. mathematical convention
            _sb.Append(" transform=\"rotate(").Append(F(-rotation)).Append(',')
               .Append(F(position.X)).Append(',').Append(F(position.Y)).Append(")\"");
        }
        if (font.Slant == FontSlant.Italic) _sb.Append(" font-style=\"italic\"");
        if (font.Weight == FontWeight.Bold) _sb.Append(" font-weight=\"bold\"");
        if (font.Color.HasValue) _sb.Append(" fill=\"").Append(font.Color.Value.ToHex()).Append('"');
        _sb.Append('>').Append(EscapeXml(text)).AppendLine("</text>");
    }

    private void EmitGlyphPath(string d, string textForMeasure, Point position, Font font, TextAlignment alignment, double rotation)
    {
        // Compute the horizontal offset needed to realise the requested alignment.
        // We measure width via ChartServices.FontMetrics — same source Skia used when
        // it generated the glyph path, so the alignment math matches the glyph layout.
        double width = ChartServices.FontMetrics.Measure(textForMeasure, font).Width;
        double alignOffset = alignment switch
        {
            TextAlignment.Center => -width / 2.0,
            TextAlignment.Right  => -width,
            _                    => 0.0,
        };

        _sb.Append("<path d=\"").Append(d).Append('"');
        if (font.Color.HasValue) _sb.Append(" fill=\"").Append(font.Color.Value.ToHex()).Append('"');
        // Transform composition is applied right-to-left: first move the glyph origin
        // by the alignment offset along the UNROTATED baseline, then rotate around the
        // anchor, then translate to the anchor itself. Gives the correct result for
        // rotated text with any alignment (e.g. Y-axis labels rotated 90° and centred).
        _sb.Append(" transform=\"translate(").Append(F(position.X)).Append(',').Append(F(position.Y)).Append(')');
        if (rotation != 0)
            _sb.Append(" rotate(").Append(F(-rotation)).Append(')');
        if (alignOffset != 0)
            _sb.Append(" translate(").Append(F(alignOffset)).Append(",0)");
        _sb.Append('"').AppendLine(" />");
    }

    /// <inheritdoc />
    public void DrawPath(IReadOnlyList<PathSegment> segments, Color? fill, Color? stroke, double strokeThickness)
    {
        _sb.Append("<path d=\"");
        foreach (var seg in segments)
            _sb.Append(seg.ToSvgPathData());
        _sb.Append('"');
        AppendFillStroke(fill, stroke, strokeThickness);
        _sb.AppendLine(" />");
    }

    /// <inheritdoc />
    public void PushClip(Rect clipRect)
    {
        int id = _clipId++;
        _sb.Append("<defs><clipPath id=\"clip-").Append(id)
           .Append("\"><rect x=\"").Append(F(clipRect.X)).Append("\" y=\"").Append(F(clipRect.Y))
           .Append("\" width=\"").Append(F(clipRect.Width)).Append("\" height=\"").Append(F(clipRect.Height))
           .AppendLine("\" /></clipPath></defs>");
        _sb.Append("<g clip-path=\"url(#clip-").Append(id).AppendLine(")\">");
    }

    /// <inheritdoc />
    public void PopClip()
    {
        _sb.AppendLine("</g>");
    }

    /// <inheritdoc />
    public Size MeasureText(string text, Font font) => ChartServices.FontMetrics.Measure(text, font);

    /// <inheritdoc />
    public void DrawRichText(RichText richText, Point position, Font font, TextAlignment alignment)
        => DrawRichText(richText, position, font, alignment, rotation: 0);

    /// <inheritdoc />
    public void DrawRichText(RichText richText, Point position, Font font, TextAlignment alignment, double rotation)
    {
        // Shortcut: no super/subscript spans → emit plain text element
        bool hasSpecial = richText.Spans.Any(s => s.Kind != TextSpanKind.Normal);
        if (!hasSpecial)
        {
            var plain = string.Concat(richText.Spans.Select(s => s.Text));
            DrawText(plain, position, font, alignment, rotation);
            return;
        }

        string anchor = alignment switch
        {
            TextAlignment.Left   => "start",
            TextAlignment.Center => "middle",
            TextAlignment.Right  => "end",
            _                    => "start",
        };

        _sb.Append("<text x=\"").Append(F(position.X)).Append("\" y=\"").Append(F(position.Y))
           .Append("\" font-family=\"").Append(font.Family)
           .Append("\" font-size=\"").Append(F(font.Size))
           .Append("\" text-anchor=\"").Append(anchor).Append('"');
        if (rotation != 0)
            _sb.Append(" transform=\"rotate(").Append(F(-rotation)).Append(',')
               .Append(F(position.X)).Append(',').Append(F(position.Y)).Append(")\"");
        if (font.Slant == FontSlant.Italic) _sb.Append(" font-style=\"italic\"");
        if (font.Weight == FontWeight.Bold)  _sb.Append(" font-weight=\"bold\"");
        if (font.Color.HasValue) _sb.Append(" fill=\"").Append(font.Color.Value.ToHex()).Append('"');
        _sb.Append('>');

        foreach (var span in richText.Spans)
        {
            if (span.Kind == TextSpanKind.Normal)
            {
                _sb.Append(EscapeXml(span.Text));
            }
            else
            {
                string shift = span.Kind == TextSpanKind.Superscript ? "super" : "sub";
                _sb.Append("<tspan baseline-shift=\"").Append(shift)
                   .Append("\" font-size=\"").Append(F(span.FontSizeScale * 100)).Append("%\">")
                   .Append(EscapeXml(span.Text))
                   .Append("</tspan>");
            }
        }

        _sb.AppendLine("</text>");
    }

    /// <inheritdoc />
    public void SetOpacity(double opacity)
    {
        _sb.Append("<g opacity=\"").Append(F(opacity)).AppendLine("\">");
    }

    /// <summary>Opens an SVG group element with a CSS class attribute.</summary>
    public void BeginGroup(string cssClass)
    {
        _sb.Append("<g class=\"").Append(cssClass).AppendLine("\">");
    }

    /// <summary>Opens an SVG group with a CSS class and <c>aria-label</c> for accessibility.</summary>
    internal void BeginAccessibleGroup(string cssClass, string ariaLabel)
    {
        _sb.Append("<g class=\"").Append(cssClass)
           .Append("\" aria-label=\"").Append(EscapeXml(ariaLabel)).AppendLine("\">");
    }

    /// <summary>Closes the current SVG group element.</summary>
    public void EndGroup()
    {
        _sb.AppendLine("</g>");
    }

    private void AppendFillStroke(Color? fill, Color? stroke, double strokeThickness)
    {
        if (fill.HasValue)
        {
            _sb.Append(" fill=\"").Append(fill.Value.ToHex()).Append('"');
            if (fill.Value.A < 255)
                _sb.Append(" fill-opacity=\"").Append(F(fill.Value.A / 255.0)).Append('"');
        }
        else
            _sb.Append(" fill=\"none\"");

        if (stroke.HasValue)
            _sb.Append(" stroke=\"").Append(stroke.Value.ToHex()).Append("\" stroke-width=\"").Append(F(strokeThickness)).Append('"');
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Linear gradient defs — used by SankeySeriesRenderer for source→target link
    // colour blends. Gradients are SVG-specific so there's no IRenderContext surface;
    // the Sankey renderer checks `if (Ctx is SvgRenderContext svg)` before calling these.
    // ──────────────────────────────────────────────────────────────────────────

    private int _gradientId;

    /// <summary>Emits a <c>&lt;linearGradient&gt;</c> into the SVG <c>&lt;defs&gt;</c> stream
    /// with two stops (<paramref name="from"/> at 0 %, <paramref name="to"/> at 100 %) and
    /// returns a unique gradient ID suitable for <c>fill="url(#id)"</c> references. The
    /// gradient direction is controlled by <paramref name="x1"/>/<paramref name="y1"/>/
    /// <paramref name="x2"/>/<paramref name="y2"/> in user space units (SVG
    /// <c>gradientUnits="userSpaceOnUse"</c>), so Sankey links can anchor the gradient to
    /// the link's bounding box without the browser re-applying a percentage-based transform.</summary>
    public string DefineLinearGradient(Color from, Color to,
        double x1, double y1, double x2, double y2)
    {
        int id = _gradientId++;
        string refId = $"grad-{id}";
        _sb.Append("<defs><linearGradient id=\"").Append(refId)
           .Append("\" gradientUnits=\"userSpaceOnUse\" x1=\"").Append(F(x1))
           .Append("\" y1=\"").Append(F(y1)).Append("\" x2=\"").Append(F(x2))
           .Append("\" y2=\"").Append(F(y2)).Append("\">");
        AppendGradientStop(0, from);
        AppendGradientStop(1, to);
        _sb.AppendLine("</linearGradient></defs>");
        return refId;
    }

    private void AppendGradientStop(double offset, Color color)
    {
        _sb.Append("<stop offset=\"").Append(F(offset))
           .Append("\" stop-color=\"").Append(color.ToHex()).Append('"');
        if (color.A < 255)
            _sb.Append(" stop-opacity=\"").Append(F(color.A / 255.0)).Append('"');
        _sb.Append(" />");
    }

    /// <summary>Draws a filled path whose fill references a previously-defined gradient
    /// (see <see cref="DefineLinearGradient"/>). Used by the Sankey renderer to emit
    /// source→target colour blends for every link.</summary>
    public void DrawPathWithGradientFill(IReadOnlyList<PathSegment> segments, string gradientId,
        Color? stroke, double strokeThickness)
    {
        _sb.Append("<path d=\"");
        foreach (var seg in segments) _sb.Append(seg.ToSvgPathData());
        _sb.Append("\" fill=\"url(#").Append(gradientId).Append(")\"");
        if (stroke.HasValue)
            _sb.Append(" stroke=\"").Append(stroke.Value.ToHex())
               .Append("\" stroke-width=\"").Append(F(strokeThickness)).Append('"');
        _sb.AppendLine(" />");
    }

    /// <summary>Opens an SVG group with a CSS class and <c>data-series-index</c> attribute for JS interactivity, with optional <c>aria-label</c>.</summary>
    internal void BeginDataGroup(string cssClass, int seriesIndex, string? ariaLabel = null)
    {
        _sb.Append("<g class=\"").Append(cssClass)
           .Append("\" data-series-index=\"").Append(seriesIndex).Append('"');
        if (!string.IsNullOrEmpty(ariaLabel))
            _sb.Append(" aria-label=\"").Append(EscapeXml(ariaLabel)).Append('"');
        _sb.AppendLine(">");
    }

    /// <summary>Opens an SVG group for a 3D scene with camera parameters as data attributes for JS rotation.</summary>
    internal void Begin3DSceneGroup(double elevation, double azimuth, double? distance, Rect plotBounds)
    {
        _sb.Append("<g class=\"mpl-3d-scene\"")
           .Append(" data-elevation=\"").Append(F(elevation)).Append('"')
           .Append(" data-azimuth=\"").Append(F(azimuth)).Append('"');
        if (distance.HasValue)
            _sb.Append(" data-distance=\"").Append(F(distance.Value)).Append('"');
        _sb.Append(" data-plot-x=\"").Append(F(plotBounds.X)).Append('"')
           .Append(" data-plot-y=\"").Append(F(plotBounds.Y)).Append('"')
           .Append(" data-plot-w=\"").Append(F(plotBounds.Width)).Append('"')
           .Append(" data-plot-h=\"").Append(F(plotBounds.Height)).Append('"')
           .AppendLine(">");
    }

    /// <summary>Opens an SVG group for a legend entry with a <c>data-legend-index</c> attribute and optional <c>aria-label</c>.</summary>
    internal void BeginLegendItemGroup(int legendIndex, string? ariaLabel = null)
    {
        _sb.Append("<g data-legend-index=\"").Append(legendIndex).Append("\" style=\"cursor:pointer\"");
        if (!string.IsNullOrEmpty(ariaLabel))
            _sb.Append(" aria-label=\"").Append(EscapeXml(ariaLabel)).Append('"');
        _sb.AppendLine(">");
    }

    /// <summary>Opens an SVG group containing a <c>&lt;title&gt;</c> element for native browser hover tooltips.</summary>
    /// <remarks>Browsers display <c>&lt;title&gt;</c> content as a tooltip when hovering over any child element.
    /// Must be paired with a matching <see cref="EndTooltipGroup"/> call.</remarks>
    internal void BeginTooltipGroup(string tooltipText)
    {
        _sb.Append("<g><title>").Append(EscapeXml(tooltipText)).AppendLine("</title>");
    }

    /// <summary>Closes the SVG group opened by <see cref="BeginTooltipGroup"/>.</summary>
    internal void EndTooltipGroup()
    {
        _sb.AppendLine("</g>");
    }

    private void AppendDashArray(LineStyle style)
    {
        var pattern = DashPatterns.GetPattern(style);
        if (pattern.Length == 0) return;
        _sb.Append(" stroke-dasharray=\"");
        for (int i = 0; i < pattern.Length; i++)
        {
            if (i > 0) _sb.Append(',');
            _sb.Append(F(pattern[i]));
        }
        _sb.Append('"');
    }

    private static string F(double value) => value.ToString("G", Inv);

    private static string EscapeXml(string text) => SvgXmlHelper.EscapeXml(text);
}
