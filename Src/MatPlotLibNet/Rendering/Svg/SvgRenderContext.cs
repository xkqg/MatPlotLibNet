// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.Svg;

/// <summary>An <see cref="IRenderContext"/> implementation that emits SVG markup for each drawing operation.</summary>
public sealed class SvgRenderContext : IRenderContext
{
    private readonly StringBuilder _sb = new();
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    private int _clipId;

    /// <summary>Returns the accumulated SVG markup as a string.</summary>
    public string GetOutput() => _sb.ToString();

    /// <summary>Gets the current length of the accumulated SVG output.</summary>
    public int OutputLength => _sb.Length;

    /// <summary>Appends the accumulated SVG markup to an existing StringBuilder (avoids extra string allocation).</summary>
    public void WriteTo(StringBuilder target) => target.Append(_sb);

    /// <inheritdoc />
    public void DrawLine(Point p1, Point p2, Color color, double thickness, LineStyle style)
    {
        _sb.Append("<line x1=\"").Append(F(p1.X)).Append("\" y1=\"").Append(F(p1.Y))
           .Append("\" x2=\"").Append(F(p2.X)).Append("\" y2=\"").Append(F(p2.Y))
           .Append("\" stroke=\"").Append(color.ToHex()).Append("\" stroke-width=\"").Append(F(thickness)).Append('"');
        AppendDashArray(style);
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
        _sb.AppendLine(" />");
    }

    /// <inheritdoc />
    public void DrawCircle(Point center, double radius, Color? fill, Color? stroke, double strokeThickness)
    {
        _sb.Append("<circle cx=\"").Append(F(center.X)).Append("\" cy=\"").Append(F(center.Y))
           .Append("\" r=\"").Append(F(radius)).Append('"');
        AppendFillStroke(fill, stroke, strokeThickness);
        _sb.AppendLine(" />");
    }

    /// <inheritdoc />
    public void DrawRectangle(Rect rect, Color? fill, Color? stroke, double strokeThickness)
    {
        _sb.Append("<rect x=\"").Append(F(rect.X)).Append("\" y=\"").Append(F(rect.Y))
           .Append("\" width=\"").Append(F(rect.Width)).Append("\" height=\"").Append(F(rect.Height)).Append('"');
        AppendFillStroke(fill, stroke, strokeThickness);
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
        _sb.AppendLine(" />");
    }

    /// <inheritdoc />
    public void DrawText(string text, Point position, Font font, TextAlignment alignment)
    {
        string anchor = alignment switch
        {
            TextAlignment.Left => "start",
            TextAlignment.Center => "middle",
            TextAlignment.Right => "end",
            _ => "start"
        };

        _sb.Append("<text x=\"").Append(F(position.X)).Append("\" y=\"").Append(F(position.Y))
           .Append("\" font-family=\"").Append(font.Family).Append("\" font-size=\"").Append(F(font.Size))
           .Append("\" text-anchor=\"").Append(anchor).Append('"');
        if (font.Slant == FontSlant.Italic) _sb.Append(" font-style=\"italic\"");
        if (font.Weight == FontWeight.Bold) _sb.Append(" font-weight=\"bold\"");
        if (font.Color.HasValue) _sb.Append(" fill=\"").Append(font.Color.Value.ToHex()).Append('"');
        _sb.Append('>').Append(EscapeXml(text)).AppendLine("</text>");
    }

    /// <inheritdoc />
    public void DrawText(string text, Point position, Font font, TextAlignment alignment, double rotation)
    {
        if (rotation == 0) { DrawText(text, position, font, alignment); return; }

        string anchor = alignment switch
        {
            TextAlignment.Left => "start",
            TextAlignment.Center => "middle",
            TextAlignment.Right => "end",
            _ => "start"
        };

        // SVG rotation: negative because SVG y-axis is flipped vs. mathematical convention
        _sb.Append("<text x=\"").Append(F(position.X)).Append("\" y=\"").Append(F(position.Y))
           .Append("\" font-family=\"").Append(font.Family).Append("\" font-size=\"").Append(F(font.Size))
           .Append("\" text-anchor=\"").Append(anchor).Append('"')
           .Append(" transform=\"rotate(").Append(F(-rotation)).Append(',')
           .Append(F(position.X)).Append(',').Append(F(position.Y)).Append(")\"");
        if (font.Slant == FontSlant.Italic) _sb.Append(" font-style=\"italic\"");
        if (font.Weight == FontWeight.Bold) _sb.Append(" font-weight=\"bold\"");
        if (font.Color.HasValue) _sb.Append(" fill=\"").Append(font.Color.Value.ToHex()).Append('"');
        _sb.Append('>').Append(EscapeXml(text)).AppendLine("</text>");
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
    public Size MeasureText(string text, Font font)
    {
        double width = text.Length * font.Size * 0.6;
        double height = font.Size * 1.2;
        return new Size(width, height);
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

    /// <summary>Closes the current SVG group element.</summary>
    public void EndGroup()
    {
        _sb.AppendLine("</g>");
    }

    private void AppendFillStroke(Color? fill, Color? stroke, double strokeThickness)
    {
        if (fill.HasValue)
            _sb.Append(" fill=\"").Append(fill.Value.ToHex()).Append('"');
        else
            _sb.Append(" fill=\"none\"");

        if (stroke.HasValue)
            _sb.Append(" stroke=\"").Append(stroke.Value.ToHex()).Append("\" stroke-width=\"").Append(F(strokeThickness)).Append('"');
    }

    /// <summary>Opens an SVG group with a CSS class and <c>data-series-index</c> attribute for JS interactivity.</summary>
    internal void BeginDataGroup(string cssClass, int seriesIndex)
    {
        _sb.Append("<g class=\"").Append(cssClass)
           .Append("\" data-series-index=\"").Append(seriesIndex).AppendLine("\">");
    }

    /// <summary>Opens an SVG group for a legend entry with a <c>data-legend-index</c> attribute.</summary>
    internal void BeginLegendItemGroup(int legendIndex)
    {
        _sb.Append("<g data-legend-index=\"").Append(legendIndex).AppendLine("\" style=\"cursor:pointer\">");
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

    private static string EscapeXml(string text)
    {
        // Fast path: no escaping needed for most labels
        if (text.AsSpan().IndexOfAny('&', '<', '>') < 0) return text;

        var sb = new StringBuilder(text.Length + 8);
        foreach (var ch in text)
        {
            switch (ch)
            {
                case '&': sb.Append("&amp;"); break;
                case '<': sb.Append("&lt;"); break;
                case '>': sb.Append("&gt;"); break;
                default: sb.Append(ch); break;
            }
        }
        return sb.ToString();
    }
}
