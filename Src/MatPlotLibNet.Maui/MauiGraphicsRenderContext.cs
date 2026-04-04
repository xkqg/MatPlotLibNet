// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using Microsoft.Maui.Graphics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;
using MplColor = MatPlotLibNet.Styling.Color;
using MplFont = MatPlotLibNet.Styling.Font;
using MplPoint = MatPlotLibNet.Rendering.Point;
using MplSize = MatPlotLibNet.Rendering.Size;
using MplRect = MatPlotLibNet.Rendering.Rect;

namespace MatPlotLibNet.Maui;

/// <summary>An <see cref="IRenderContext"/> implementation that draws onto a .NET MAUI <see cref="ICanvas"/>.</summary>
public sealed class MauiGraphicsRenderContext : IRenderContext
{
    private readonly ICanvas _canvas;

    /// <summary>Initializes a new instance wrapping the specified MAUI canvas.</summary>
    public MauiGraphicsRenderContext(ICanvas canvas)
    {
        _canvas = canvas;
    }

    /// <inheritdoc />
    public void DrawLine(MplPoint p1, MplPoint p2, MplColor color, double thickness, Styling.LineStyle style)
    {
        _canvas.StrokeColor = color.ToMauiColor();
        _canvas.StrokeSize = (float)thickness;
        _canvas.StrokeDashPattern = style.ToMauiDashPattern();
        _canvas.DrawLine((float)p1.X, (float)p1.Y, (float)p2.X, (float)p2.Y);
    }

    /// <inheritdoc />
    public void DrawLines(IReadOnlyList<MplPoint> points, MplColor color, double thickness, Styling.LineStyle style)
    {
        if (points.Count < 2) return;
        _canvas.StrokeColor = color.ToMauiColor();
        _canvas.StrokeSize = (float)thickness;
        _canvas.StrokeDashPattern = style.ToMauiDashPattern();

        var path = new PathF();
        path.MoveTo((float)points[0].X, (float)points[0].Y);
        for (int i = 1; i < points.Count; i++)
            path.LineTo((float)points[i].X, (float)points[i].Y);

        _canvas.DrawPath(path);
    }

    /// <inheritdoc />
    public void DrawPolygon(IReadOnlyList<MplPoint> points, MplColor? fill, MplColor? stroke, double strokeThickness)
    {
        var path = new PathF();
        if (points.Count > 0)
        {
            path.MoveTo((float)points[0].X, (float)points[0].Y);
            for (int i = 1; i < points.Count; i++)
                path.LineTo((float)points[i].X, (float)points[i].Y);
            path.Close();
        }

        if (fill.HasValue)
        {
            _canvas.FillColor = fill.Value.ToMauiColor();
            _canvas.FillPath(path);
        }
        if (stroke.HasValue)
        {
            _canvas.StrokeColor = stroke.Value.ToMauiColor();
            _canvas.StrokeSize = (float)strokeThickness;
            _canvas.DrawPath(path);
        }
    }

    /// <inheritdoc />
    public void DrawCircle(MplPoint center, double radius, MplColor? fill, MplColor? stroke, double strokeThickness)
    {
        if (fill.HasValue)
        {
            _canvas.FillColor = fill.Value.ToMauiColor();
            _canvas.FillCircle((float)center.X, (float)center.Y, (float)radius);
        }
        if (stroke.HasValue)
        {
            _canvas.StrokeColor = stroke.Value.ToMauiColor();
            _canvas.StrokeSize = (float)strokeThickness;
            _canvas.DrawCircle((float)center.X, (float)center.Y, (float)radius);
        }
    }

    /// <inheritdoc />
    public void DrawRectangle(MplRect rect, MplColor? fill, MplColor? stroke, double strokeThickness)
    {
        if (fill.HasValue)
        {
            _canvas.FillColor = fill.Value.ToMauiColor();
            _canvas.FillRectangle((float)rect.X, (float)rect.Y, (float)rect.Width, (float)rect.Height);
        }
        if (stroke.HasValue)
        {
            _canvas.StrokeColor = stroke.Value.ToMauiColor();
            _canvas.StrokeSize = (float)strokeThickness;
            _canvas.DrawRectangle((float)rect.X, (float)rect.Y, (float)rect.Width, (float)rect.Height);
        }
    }

    /// <inheritdoc />
    public void DrawEllipse(MplRect bounds, MplColor? fill, MplColor? stroke, double strokeThickness)
    {
        if (fill.HasValue)
        {
            _canvas.FillColor = fill.Value.ToMauiColor();
            _canvas.FillEllipse((float)bounds.X, (float)bounds.Y, (float)bounds.Width, (float)bounds.Height);
        }
        if (stroke.HasValue)
        {
            _canvas.StrokeColor = stroke.Value.ToMauiColor();
            _canvas.StrokeSize = (float)strokeThickness;
            _canvas.DrawEllipse((float)bounds.X, (float)bounds.Y, (float)bounds.Width, (float)bounds.Height);
        }
    }

    /// <inheritdoc />
    public void DrawText(string text, MplPoint position, MplFont font, MatPlotLibNet.Rendering.TextAlignment alignment)
    {
        _canvas.FontSize = (float)font.Size;
        if (font.Color.HasValue)
            _canvas.FontColor = font.Color.Value.ToMauiColor();

        var hAlign = alignment switch
        {
            MatPlotLibNet.Rendering.TextAlignment.Center => HorizontalAlignment.Center,
            MatPlotLibNet.Rendering.TextAlignment.Right => HorizontalAlignment.Right,
            _ => HorizontalAlignment.Left
        };

        _canvas.DrawString(text, (float)position.X, (float)position.Y, hAlign);
    }

    /// <inheritdoc />
    public void DrawPath(IReadOnlyList<PathSegment> segments, MplColor? fill, MplColor? stroke, double strokeThickness)
    {
        var path = new PathF();
        foreach (var seg in segments)
        {
            switch (seg)
            {
                case MoveToSegment m:
                    path.MoveTo((float)m.Point.X, (float)m.Point.Y);
                    break;
                case LineToSegment l:
                    path.LineTo((float)l.Point.X, (float)l.Point.Y);
                    break;
                case BezierSegment b:
                    path.CurveTo(
                        (float)b.Control1.X, (float)b.Control1.Y,
                        (float)b.Control2.X, (float)b.Control2.Y,
                        (float)b.End.X, (float)b.End.Y);
                    break;
                case CloseSegment:
                    path.Close();
                    break;
            }
        }

        if (fill.HasValue)
        {
            _canvas.FillColor = fill.Value.ToMauiColor();
            _canvas.FillPath(path);
        }
        if (stroke.HasValue)
        {
            _canvas.StrokeColor = stroke.Value.ToMauiColor();
            _canvas.StrokeSize = (float)strokeThickness;
            _canvas.DrawPath(path);
        }
    }

    /// <inheritdoc />
    public void PushClip(MplRect clipRect)
    {
        _canvas.SaveState();
        _canvas.ClipRectangle((float)clipRect.X, (float)clipRect.Y,
            (float)clipRect.Width, (float)clipRect.Height);
    }

    /// <inheritdoc />
    public void PopClip()
    {
        _canvas.RestoreState();
    }

    /// <inheritdoc />
    public MplSize MeasureText(string text, MplFont font)
    {
        double width = text.Length * font.Size * 0.6;
        double height = font.Size * 1.2;
        return new MplSize(width, height);
    }

    /// <inheritdoc />
    public void SetOpacity(double opacity)
    {
        _canvas.Alpha = (float)opacity;
    }
}
