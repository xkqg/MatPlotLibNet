// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

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
    private int _clipId;
    private List<(string Key, string Value)>? _pendingData;
    private string? _pendingClass;

    /// <summary>Default constructor: wires the gradient registry to the internal buffer
    /// so gradient defs land inline at the point of <see cref="DefineLinearGradient"/>.</summary>
    public SvgRenderContext()
    {
        _gradients = new SvgGradientRegistry(_sb);
    }

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
        if (_pendingClass is not null)
        {
            _sb.Append(" class=\"").Append(_pendingClass).Append('"');
            _pendingClass = null;
        }
        if (_pendingData is null || _pendingData.Count == 0) return;
        foreach (var (key, value) in _pendingData)
            _sb.Append(" data-").Append(key).Append("=\"").Append(value).Append('"');
        _pendingData.Clear();
    }

    /// <summary>Phase F of v1.7.2 follow-on plan — applies a <c>class</c> attribute
    /// to the next drawn element. Used by <c>ThreeDAxesRenderer.Render3DPanes</c> to
    /// tag panes with <c>class="mpl-pane"</c> so the JS depth-sort skips them and the
    /// behavioural test can assert DOM order.</summary>
    internal void SetNextElementClass(string className) => _pendingClass = className;

    /// <inheritdoc />
    public void DrawLine(Point p1, Point p2, Color color, double thickness, LineStyle style)
    {
        _sb.Append("<line x1=\"").Append(p1.X.ToSvgNumber()).Append("\" y1=\"").Append(p1.Y.ToSvgNumber())
           .Append("\" x2=\"").Append(p2.X.ToSvgNumber()).Append("\" y2=\"").Append(p2.Y.ToSvgNumber())
           .Append("\" stroke=\"").Append(color.ToHex()).Append("\" stroke-width=\"").Append(thickness.ToSvgNumber()).Append('"');
        _sb.AppendDashArray(style);
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
            _sb.Append(points[i].X.ToSvgNumber()).Append(',').Append(points[i].Y.ToSvgNumber());
        }
        _sb.Append("\" fill=\"none\" stroke=\"").Append(color.ToHex())
           .Append("\" stroke-width=\"").Append(thickness.ToSvgNumber()).Append('"');
        _sb.AppendDashArray(style);
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
            _sb.Append(points[i].X.ToSvgNumber()).Append(',').Append(points[i].Y.ToSvgNumber());
        }
        _sb.Append('"');
        _sb.AppendFillStroke(fill, stroke, strokeThickness);
        FlushPendingData();
        _sb.AppendLine(" />");
    }

    /// <inheritdoc />
    public void DrawCircle(Point center, double radius, Color? fill, Color? stroke, double strokeThickness)
    {
        _sb.Append("<circle cx=\"").Append(center.X.ToSvgNumber()).Append("\" cy=\"").Append(center.Y.ToSvgNumber())
           .Append("\" r=\"").Append(radius.ToSvgNumber()).Append('"');
        _sb.AppendFillStroke(fill, stroke, strokeThickness);
        FlushPendingData();
        _sb.AppendLine(" />");
    }

    /// <inheritdoc />
    public void DrawRectangle(Rect rect, Color? fill, Color? stroke, double strokeThickness)
    {
        _sb.Append("<rect x=\"").Append(rect.X.ToSvgNumber()).Append("\" y=\"").Append(rect.Y.ToSvgNumber())
           .Append("\" width=\"").Append(rect.Width.ToSvgNumber()).Append("\" height=\"").Append(rect.Height.ToSvgNumber()).Append('"');
        _sb.AppendFillStroke(fill, stroke, strokeThickness);
        FlushPendingData();
        _sb.AppendLine(" />");
    }

    /// <inheritdoc />
    public void DrawEllipse(Rect bounds, Color? fill, Color? stroke, double strokeThickness)
    {
        double cx = bounds.X + bounds.Width / 2;
        double cy = bounds.Y + bounds.Height / 2;
        _sb.Append("<ellipse cx=\"").Append(cx.ToSvgNumber()).Append("\" cy=\"").Append(cy.ToSvgNumber())
           .Append("\" rx=\"").Append((bounds.Width / 2).ToSvgNumber()).Append("\" ry=\"").Append((bounds.Height / 2).ToSvgNumber()).Append('"');
        _sb.AppendFillStroke(fill, stroke, strokeThickness);
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

        _sb.Append("<text x=\"").Append(position.X.ToSvgNumber()).Append("\" y=\"").Append(position.Y.ToSvgNumber())
           .Append("\" font-family=\"").Append(font.Family).Append("\" font-size=\"").Append(font.Size.ToSvgNumber())
           .Append("\" text-anchor=\"").Append(anchor).Append('"');
        if (rotation != 0)
        {
            // SVG rotation: negative because SVG y-axis is flipped vs. mathematical convention
            _sb.Append(" transform=\"rotate(").Append((-rotation).ToSvgNumber()).Append(',')
               .Append(position.X.ToSvgNumber()).Append(',').Append(position.Y.ToSvgNumber()).Append(")\"");
        }
        if (font.Slant == FontSlant.Italic) _sb.Append(" font-style=\"italic\"");
        if (font.Weight == FontWeight.Bold) _sb.Append(" font-weight=\"bold\"");
        if (font.Color.HasValue) _sb.Append(" fill=\"").Append(font.Color.Value.ToHex()).Append('"');
        FlushPendingData();
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
        _sb.Append(" transform=\"translate(").Append(position.X.ToSvgNumber()).Append(',').Append(position.Y.ToSvgNumber()).Append(')');
        if (rotation != 0)
            _sb.Append(" rotate(").Append((-rotation).ToSvgNumber()).Append(')');
        if (alignOffset != 0)
            _sb.Append(" translate(").Append(alignOffset.ToSvgNumber()).Append(",0)");
        _sb.Append('"');
        FlushPendingData();
        _sb.AppendLine(" />");
    }

    /// <inheritdoc />
    public void DrawPath(IReadOnlyList<PathSegment> segments, Color? fill, Color? stroke, double strokeThickness)
    {
        _sb.Append("<path d=\"");
        foreach (var seg in segments)
            _sb.Append(seg.ToSvgPathData());
        _sb.Append('"');
        _sb.AppendFillStroke(fill, stroke, strokeThickness);
        // Phase G.7 of v1.7.2 follow-on plan — missing flush here caused
        // SetNextElementData pushes to leak across iterations (e.g. Sankey's
        // per-link data-* attributes stacked onto every subsequent node).
        FlushPendingData();
        _sb.AppendLine(" />");
    }

    /// <inheritdoc />
    public void PushClip(Rect clipRect)
    {
        int id = _clipId++;
        _sb.Append("<defs><clipPath id=\"clip-").Append(id)
           .Append("\"><rect x=\"").Append(clipRect.X.ToSvgNumber()).Append("\" y=\"").Append(clipRect.Y.ToSvgNumber())
           .Append("\" width=\"").Append(clipRect.Width.ToSvgNumber()).Append("\" height=\"").Append(clipRect.Height.ToSvgNumber())
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

        _sb.Append("<text x=\"").Append(position.X.ToSvgNumber()).Append("\" y=\"").Append(position.Y.ToSvgNumber())
           .Append("\" font-family=\"").Append(font.Family)
           .Append("\" font-size=\"").Append(font.Size.ToSvgNumber())
           .Append("\" text-anchor=\"").Append(anchor).Append('"');
        if (rotation != 0)
            _sb.Append(" transform=\"rotate(").Append((-rotation).ToSvgNumber()).Append(',')
               .Append(position.X.ToSvgNumber()).Append(',').Append(position.Y.ToSvgNumber()).Append(")\"");
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
                   .Append("\" font-size=\"").Append((span.FontSizeScale * 100).ToSvgNumber()).Append("%\">")
                   .Append(EscapeXml(span.Text))
                   .Append("</tspan>");
            }
        }

        _sb.AppendLine("</text>");
    }

    // Tracks whether SetOpacity has currently opened an <g opacity="..."> wrapper.
    // SetOpacity is called PAIRWISE by renderers: once with the series alpha, then
    // again with 1.0 to "reset". The Skia backend treats this as a state variable
    // and does not need a wrapper. The SVG backend used to OPEN a fresh <g> on each
    // call -- producing two unclosed wrappers and a malformed SVG that broke the
    // browser XML parser (and silently disabled embedded interactivity scripts).
    private bool _opacityGroupOpen;

    /// <inheritdoc />
    public void SetOpacity(double opacity)
    {
        // Close any previously open opacity wrapper so the SVG stays balanced.
        if (_opacityGroupOpen)
        {
            _sb.AppendLine("</g>");
            _opacityGroupOpen = false;
        }
        // Only open a new wrapper when opacity actually differs from full opacity.
        // Calling SetOpacity(1.0) acts as a clean RESET with no extra DOM noise.
        if (opacity < 1.0)
        {
            _sb.Append("<g opacity=\"").Append(opacity.ToSvgNumber()).AppendLine("\">");
            _opacityGroupOpen = true;
        }
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


    // ──────────────────────────────────────────────────────────────────────────
    // Linear gradient defs — used by SankeySeriesRenderer for source→target link
    // colour blends. Gradients are SVG-specific so there's no IRenderContext surface;
    // the Sankey renderer checks `if (Ctx is SvgRenderContext svg)` before calling these.
    // ──────────────────────────────────────────────────────────────────────────

    // Gradient-defs emission delegated to SvgGradientRegistry (Phase F.2.d, 2026-04-20).
    // Composition over inheritance: the registry writes directly to _sb so output byte-
    // order is preserved; the render context keeps only the DefineLinearGradient public
    // surface as a thin forwarder.
    private readonly SvgGradientRegistry _gradients;

    /// <summary>Emits a <c>&lt;linearGradient&gt;</c> into the SVG <c>&lt;defs&gt;</c> stream
    /// with two stops (<paramref name="from"/> at 0 %, <paramref name="to"/> at 100 %) and
    /// returns a unique gradient ID suitable for <c>fill="url(#id)"</c> references. The
    /// gradient direction is controlled by <paramref name="x1"/>/<paramref name="y1"/>/
    /// <paramref name="x2"/>/<paramref name="y2"/> in user space units (SVG
    /// <c>gradientUnits="userSpaceOnUse"</c>), so Sankey links can anchor the gradient to
    /// the link's bounding box without the browser re-applying a percentage-based transform.</summary>
    public string DefineLinearGradient(Color from, Color to,
        double x1, double y1, double x2, double y2)
        => _gradients.Register(from, to, x1, y1, x2, y2);

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
               .Append("\" stroke-width=\"").Append(strokeThickness.ToSvgNumber()).Append('"');
        // Phase G.7 of v1.7.2 follow-on plan — flush pending data (e.g. Sankey's
        // data-sankey-link-source/target) onto the gradient-filled path too.
        FlushPendingData();
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

    /// <summary>Opens an SVG group for a 3D scene with camera parameters as data attributes for JS rotation.
    /// Optional <paramref name="light"/> emits data-light-dir/ambient/diffuse so the JS can recompute
    /// face shading under rotation (Phase 6 of the v1.7.2 interaction-hardening plan).</summary>
    internal void Begin3DSceneGroup(double elevation, double azimuth, double? distance, Rect plotBounds,
        Lighting.DirectionalLight? light = null)
    {
        _sb.Append("<g class=\"mpl-3d-scene\"")
           .Append(" data-elevation=\"").Append(elevation.ToSvgNumber()).Append('"')
           .Append(" data-azimuth=\"").Append(azimuth.ToSvgNumber()).Append('"');
        // Phase F.3 of v1.7.2 follow-on plan — always emit data-distance so JS and server
        // stay in lockstep. Projection3D always runs perspective with `dist = distance ?? 10`;
        // without the attribute JS bailed on wheel zoom whenever the caller omitted
        // `distance:` in WithCamera, producing the user-reported "zoom does not work in 3D"
        // symptom. 10 matches Projection3D.DefaultDist.
        _sb.Append(" data-distance=\"").Append((distance ?? 10.0).ToSvgNumber()).Append('"');
        _sb.Append(" data-plot-x=\"").Append(plotBounds.X.ToSvgNumber()).Append('"')
           .Append(" data-plot-y=\"").Append(plotBounds.Y.ToSvgNumber()).Append('"')
           .Append(" data-plot-w=\"").Append(plotBounds.Width.ToSvgNumber()).Append('"')
           .Append(" data-plot-h=\"").Append(plotBounds.Height.ToSvgNumber()).Append('"');
        if (light is not null)
        {
            _sb.Append(" data-light-dir=\"")
               .Append(light.Dx.ToSvgNumber()).Append(',')
               .Append(light.Dy.ToSvgNumber()).Append(',')
               .Append(light.Dz.ToSvgNumber()).Append('"')
               .Append(" data-light-ambient=\"").Append(light.Ambient.ToSvgNumber()).Append('"')
               .Append(" data-light-diffuse=\"").Append(light.Diffuse.ToSvgNumber()).Append('"');
        }
        _sb.AppendLine(">");
    }

    /// <summary>Phase F of v1.7.2 follow-on plan — subgroup wrapper inside the 3D scene
    /// group. Three tiers mirror matplotlib's draw order (axes3d.py:458-470):
    /// <c>mpl-3d-back</c> (panes, edges, grid, axis labels), <c>mpl-3d-data</c> (series
    /// quads; depth-sorted by JS in place), <c>mpl-3d-front</c> (tick marks + labels).
    /// Keeps axis-infrastructure OUT of the depth-sort pool so panes never paint over
    /// back-corner surface quads on interactive rotation.</summary>
    internal void Begin3DSubgroup(string className)
    {
        _sb.Append("<g class=\"").Append(className).AppendLine("\">");
    }

    /// <summary>Closes a subgroup opened by <see cref="Begin3DSubgroup"/>.</summary>
    internal void End3DSubgroup()
    {
        _sb.AppendLine("</g>");
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


    private static string EscapeXml(string text) => SvgXmlHelper.EscapeXml(text);
}
