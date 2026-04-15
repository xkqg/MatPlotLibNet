// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;
using MatPlotLibNet;
using SkiaSharp;

namespace MatPlotLibNet.Skia;

/// <summary>Render context implementation using SkiaSharp for raster and PDF output.</summary>
public sealed class SkiaRenderContext : IRenderContext
{
    private readonly SKCanvas _canvas;

    // Current group opacity applied to subsequent Draw* calls. Kept as instance state
    // rather than as a Skia SaveLayer because SetOpacity/SetOpacity(1.0) is called
    // pairwise in series renderers (e.g. SurfaceSeriesRenderer) without an intervening
    // Restore, so layer-based implementation would drop every polygon between the
    // two calls. Each CreateFillPaint/CreateStrokePaint multiplies the alpha of its
    // color by this field.
    private double _opacity = 1.0;

    /// <summary>Creates a new SkiaSharp render context wrapping the given canvas.</summary>
    public SkiaRenderContext(SKCanvas canvas) => _canvas = canvas;

    /// <inheritdoc />
    public void DrawLine(Point p1, Point p2, Color color, double thickness, LineStyle style)
    {
        using var paint = CreateStrokePaint(color, thickness, style);
        _canvas.DrawLine((float)p1.X, (float)p1.Y, (float)p2.X, (float)p2.Y, paint);
    }

    /// <inheritdoc />
    public void DrawLines(IReadOnlyList<Point> points, Color color, double thickness, LineStyle style)
    {
        if (points.Count < 2) return;
        using var paint = CreateStrokePaint(color, thickness, style);
        using var path = new SKPath();
        path.MoveTo((float)points[0].X, (float)points[0].Y);
        for (int i = 1; i < points.Count; i++)
            path.LineTo((float)points[i].X, (float)points[i].Y);
        _canvas.DrawPath(path, paint);
    }

    /// <inheritdoc />
    public void DrawPolygon(IReadOnlyList<Point> points, Color? fill, Color? stroke, double strokeThickness)
    {
        if (points.Count < 3) return;
        using var path = new SKPath();
        path.MoveTo((float)points[0].X, (float)points[0].Y);
        for (int i = 1; i < points.Count; i++)
            path.LineTo((float)points[i].X, (float)points[i].Y);
        path.Close();

        if (fill.HasValue)
        {
            using var paint = CreateFillPaint(fill.Value);
            _canvas.DrawPath(path, paint);
        }
        if (stroke.HasValue && strokeThickness > 0)
        {
            using var paint = CreateStrokePaint(stroke.Value, strokeThickness, LineStyle.Solid);
            _canvas.DrawPath(path, paint);
        }
    }

    /// <inheritdoc />
    public void DrawCircle(Point center, double radius, Color? fill, Color? stroke, double strokeThickness)
    {
        if (fill.HasValue)
        {
            using var paint = CreateFillPaint(fill.Value);
            _canvas.DrawCircle((float)center.X, (float)center.Y, (float)radius, paint);
        }
        if (stroke.HasValue && strokeThickness > 0)
        {
            using var paint = CreateStrokePaint(stroke.Value, strokeThickness, LineStyle.Solid);
            _canvas.DrawCircle((float)center.X, (float)center.Y, (float)radius, paint);
        }
    }

    /// <inheritdoc />
    public void DrawRectangle(Rect rect, Color? fill, Color? stroke, double strokeThickness)
    {
        var skRect = new SKRect((float)rect.X, (float)rect.Y,
            (float)(rect.X + rect.Width), (float)(rect.Y + rect.Height));
        if (fill.HasValue)
        {
            using var paint = CreateFillPaint(fill.Value);
            _canvas.DrawRect(skRect, paint);
        }
        if (stroke.HasValue && strokeThickness > 0)
        {
            using var paint = CreateStrokePaint(stroke.Value, strokeThickness, LineStyle.Solid);
            _canvas.DrawRect(skRect, paint);
        }
    }

    /// <inheritdoc />
    public void DrawEllipse(Rect bounds, Color? fill, Color? stroke, double strokeThickness)
    {
        var skRect = new SKRect((float)bounds.X, (float)bounds.Y,
            (float)(bounds.X + bounds.Width), (float)(bounds.Y + bounds.Height));
        if (fill.HasValue)
        {
            using var paint = CreateFillPaint(fill.Value);
            _canvas.DrawOval(skRect, paint);
        }
        if (stroke.HasValue && strokeThickness > 0)
        {
            using var paint = CreateStrokePaint(stroke.Value, strokeThickness, LineStyle.Solid);
            _canvas.DrawOval(skRect, paint);
        }
    }

    /// <inheritdoc />
    public void DrawText(string text, Point position, Font font, TextAlignment alignment)
        => DrawText(text, position, font, alignment, rotation: 0);

    /// <inheritdoc />
    public void DrawText(string text, Point position, Font font, TextAlignment alignment, double rotation)
    {
        var typeface = FigureSkiaExtensions.ResolveTypeface(font.Family,
            font.Weight == FontWeight.Bold ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal,
            font.Slant == FontSlant.Italic ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright);
        using var skFont = new SKFont(typeface, (float)font.Size);
        using var paint  = new SKPaint { Color = ToSkColor(font.Color ?? Colors.Black), IsAntialias = true };

        float textWidth = skFont.MeasureText(text);
        float dx = alignment switch
        {
            TextAlignment.Center => -textWidth / 2,
            TextAlignment.Right  => -textWidth,
            _                    => 0,
        };

        if (rotation == 0)
        {
            _canvas.DrawText(text, (float)position.X + dx, (float)position.Y, skFont, paint);
        }
        else
        {
            _canvas.Save();
            // Rotate around the anchor point (matches SVG transform="rotate(angle, x, y)").
            // Note: matplotlib/SVG positive rotation is counter-clockwise; Skia's RotateDegrees is clockwise.
            _canvas.RotateDegrees(-(float)rotation, (float)position.X, (float)position.Y);
            _canvas.DrawText(text, (float)position.X + dx, (float)position.Y, skFont, paint);
            _canvas.Restore();
        }
    }

    /// <inheritdoc />
    public void DrawRichText(MatPlotLibNet.Rendering.MathText.RichText richText, Point position, Font font, TextAlignment alignment)
        => DrawRichText(richText, position, font, alignment, rotation: 0);

    /// <inheritdoc />
    public void DrawRichText(MatPlotLibNet.Rendering.MathText.RichText richText, Point position, Font font, TextAlignment alignment, double rotation)
    {
        // Concatenate spans for total width measurement (sub/super render at 0.7 scale).
        var typeface = FigureSkiaExtensions.ResolveTypeface(font.Family,
            font.Weight == FontWeight.Bold ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal,
            font.Slant == FontSlant.Italic ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright);
        using var paint = new SKPaint { Color = ToSkColor(font.Color ?? Colors.Black), IsAntialias = true };

        // Measure total width with per-span scaling.
        float totalWidth = 0;
        foreach (var span in richText.Spans)
        {
            using var f = new SKFont(typeface, (float)(font.Size * span.FontSizeScale));
            totalWidth += f.MeasureText(span.Text);
        }

        float dx = alignment switch
        {
            TextAlignment.Center => -totalWidth / 2,
            TextAlignment.Right  => -totalWidth,
            _                    => 0,
        };

        if (rotation != 0)
        {
            _canvas.Save();
            _canvas.RotateDegrees(-(float)rotation, (float)position.X, (float)position.Y);
        }

        float cursorX = (float)position.X + dx;
        float baseY   = (float)position.Y;
        foreach (var span in richText.Spans)
        {
            using var f = new SKFont(typeface, (float)(font.Size * span.FontSizeScale));
            float spanY = span.Kind switch
            {
                MatPlotLibNet.Rendering.MathText.TextSpanKind.Superscript => baseY - (float)(font.Size * 0.40),
                MatPlotLibNet.Rendering.MathText.TextSpanKind.Subscript   => baseY + (float)(font.Size * 0.20),
                _                                                          => baseY,
            };
            _canvas.DrawText(span.Text, cursorX, spanY, f, paint);
            cursorX += f.MeasureText(span.Text);
        }

        if (rotation != 0)
            _canvas.Restore();
    }

    /// <inheritdoc />
    public void DrawPath(IReadOnlyList<PathSegment> segments, Color? fill, Color? stroke, double strokeThickness)
    {
        using var path = new SKPath();
        foreach (var seg in segments)
        {
            switch (seg)
            {
                case MoveToSegment m: path.MoveTo((float)m.Point.X, (float)m.Point.Y); break;
                case LineToSegment l: path.LineTo((float)l.Point.X, (float)l.Point.Y); break;
                case BezierSegment b: path.CubicTo(
                    (float)b.Control1.X, (float)b.Control1.Y,
                    (float)b.Control2.X, (float)b.Control2.Y,
                    (float)b.End.X, (float)b.End.Y); break;
                case ArcSegment a: path.ArcTo(
                    new SKRect((float)(a.Center.X - a.RadiusX), (float)(a.Center.Y - a.RadiusY),
                               (float)(a.Center.X + a.RadiusX), (float)(a.Center.Y + a.RadiusY)),
                    (float)a.StartAngle, (float)(a.EndAngle - a.StartAngle), false); break;
                case CloseSegment: path.Close(); break;
            }
        }
        if (fill.HasValue) { using var p = CreateFillPaint(fill.Value); _canvas.DrawPath(path, p); }
        if (stroke.HasValue && strokeThickness > 0) { using var p = CreateStrokePaint(stroke.Value, strokeThickness, LineStyle.Solid); _canvas.DrawPath(path, p); }
    }

    /// <inheritdoc />
    public void PushClip(Rect clipRect)
    {
        _canvas.Save();
        _canvas.ClipRect(new SKRect((float)clipRect.X, (float)clipRect.Y,
            (float)(clipRect.X + clipRect.Width), (float)(clipRect.Y + clipRect.Height)));
    }

    /// <inheritdoc />
    public void PopClip() => _canvas.Restore();

    /// <inheritdoc />
    public Size MeasureText(string text, Font font) => ChartServices.FontMetrics.Measure(text, font);

    /// <inheritdoc />
    public void SetOpacity(double opacity) => _opacity = Math.Clamp(opacity, 0.0, 1.0);

    /// <summary>Creates a filled SKPaint from the given color, modulated by the current group opacity.</summary>
    private SKPaint CreateFillPaint(Color color) => new()
    {
        Color = ToSkColor(color),
        Style = SKPaintStyle.Fill,
        IsAntialias = true
    };

    /// <summary>Creates a stroke SKPaint with optional dash pattern, modulated by the current group opacity.</summary>
    private SKPaint CreateStrokePaint(Color color, double thickness, LineStyle style)
    {
        var paint = new SKPaint
        {
            Color = ToSkColor(color),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = (float)thickness,
            IsAntialias = true
        };
        var dashPattern = DashPatterns.GetPattern(style);
        if (dashPattern.Length > 0)
        {
            var floatDashes = new float[dashPattern.Length];
            for (int i = 0; i < dashPattern.Length; i++)
                floatDashes[i] = (float)(dashPattern[i] * thickness);
            paint.PathEffect = SKPathEffect.CreateDash(floatDashes, 0);
        }
        return paint;
    }

    /// <summary>Converts a MatPlotLibNet Color to a SkiaSharp SKColor, applying the current <see cref="_opacity"/>.</summary>
    private SKColor ToSkColor(Color c)
    {
        byte a = (byte)Math.Round(c.A * _opacity);
        return new SKColor(c.R, c.G, c.B, a);
    }
}
