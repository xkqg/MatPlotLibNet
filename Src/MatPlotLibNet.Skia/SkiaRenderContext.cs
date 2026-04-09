// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;
using SkiaSharp;

namespace MatPlotLibNet.Skia;

/// <summary>Render context implementation using SkiaSharp for raster and PDF output.</summary>
public sealed class SkiaRenderContext : IRenderContext
{
    private readonly SKCanvas _canvas;

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
    {
        using var paint = new SKPaint
        {
            Color = ToSkColor(font.Color ?? Colors.Black),
            TextSize = (float)font.Size,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName(font.Family,
                font.Weight == FontWeight.Bold ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal,
                SKFontStyleWidth.Normal,
                font.Slant == FontSlant.Italic ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright)
        };

        float x = (float)position.X;
        if (alignment == TextAlignment.Center) x -= paint.MeasureText(text) / 2;
        else if (alignment == TextAlignment.Right) x -= paint.MeasureText(text);

        _canvas.DrawText(text, x, (float)position.Y, paint);
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
    public Size MeasureText(string text, Font font)
    {
        return new Size(text.Length * font.Size * 0.6, font.Size * 1.2);
    }

    /// <inheritdoc />
    public void SetOpacity(double opacity) => _canvas.SaveLayer(new SKPaint { Color = new SKColor(255, 255, 255, (byte)(opacity * 255)) });

    /// <summary>Creates a filled SKPaint from the given color.</summary>
    private static SKPaint CreateFillPaint(Color color) => new()
    {
        Color = ToSkColor(color),
        Style = SKPaintStyle.Fill,
        IsAntialias = true
    };

    /// <summary>Creates a stroke SKPaint with optional dash pattern based on the line style.</summary>
    private static SKPaint CreateStrokePaint(Color color, double thickness, LineStyle style)
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

    /// <summary>Converts a MatPlotLibNet Color to a SkiaSharp SKColor.</summary>
    private static SKColor ToSkColor(Color c) => new(c.R, c.G, c.B, c.A);
}
