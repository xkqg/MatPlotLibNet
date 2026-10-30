// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Microsoft.Maui.Graphics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;
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
    public void DrawLine(MplPoint p1, MplPoint p2, StrokeStyle stroke)
    {
        _canvas.StrokeColor = stroke.Color.ToMauiColor();
        _canvas.StrokeSize = (float)stroke.Thickness;
        _canvas.StrokeDashPattern = stroke.Style.ToMauiDashPattern();
        _canvas.DrawLine((float)p1.X, (float)p1.Y, (float)p2.X, (float)p2.Y);
    }

    /// <inheritdoc />
    public void DrawLines(IReadOnlyList<MplPoint> points, StrokeStyle stroke)
    {
        if (points.Count < 2) return;
        _canvas.StrokeColor = stroke.Color.ToMauiColor();
        _canvas.StrokeSize = (float)stroke.Thickness;
        _canvas.StrokeDashPattern = stroke.Style.ToMauiDashPattern();

        var path = new PathF();
        path.MoveTo((float)points[0].X, (float)points[0].Y);
        for (int i = 1; i < points.Count; i++)
            path.LineTo((float)points[i].X, (float)points[i].Y);

        _canvas.DrawPath(path);
    }

    /// <inheritdoc />
    public void DrawPolygon(IReadOnlyList<MplPoint> points, ShapeStyle shape)
    {
        var path = new PathF();
        if (points.Count > 0)
        {
            path.MoveTo((float)points[0].X, (float)points[0].Y);
            for (int i = 1; i < points.Count; i++)
                path.LineTo((float)points[i].X, (float)points[i].Y);
            path.Close();
        }

        if (shape.HasVisibleFill)
        {
            Fill(shape, path.Bounds, () => _canvas.FillPath(path));
        }
        if (shape.HasVisibleStroke)
        {
            _canvas.StrokeColor = shape.Stroke!.Value.ToMauiColor();
            _canvas.StrokeSize = (float)shape.StrokeThickness;
            _canvas.DrawPath(path);
        }
    }

    /// <inheritdoc />
    public void DrawCircle(MplPoint center, double radius, ShapeStyle shape)
    {
        if (shape.HasVisibleFill)
        {
            var bounds = new RectF((float)(center.X - radius), (float)(center.Y - radius), (float)(2 * radius), (float)(2 * radius));
            Fill(shape, bounds, () => _canvas.FillCircle((float)center.X, (float)center.Y, (float)radius));
        }
        if (shape.HasVisibleStroke)
        {
            _canvas.StrokeColor = shape.Stroke!.Value.ToMauiColor();
            _canvas.StrokeSize = (float)shape.StrokeThickness;
            _canvas.DrawCircle((float)center.X, (float)center.Y, (float)radius);
        }
    }

    /// <inheritdoc />
    public void DrawRectangle(MplRect rect, ShapeStyle shape)
    {
        if (shape.HasVisibleFill)
        {
            var bounds = new RectF((float)rect.X, (float)rect.Y, (float)rect.Width, (float)rect.Height);
            Fill(shape, bounds, () => _canvas.FillRectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height));
        }
        if (shape.HasVisibleStroke)
        {
            _canvas.StrokeColor = shape.Stroke!.Value.ToMauiColor();
            _canvas.StrokeSize = (float)shape.StrokeThickness;
            _canvas.DrawRectangle((float)rect.X, (float)rect.Y, (float)rect.Width, (float)rect.Height);
        }
    }

    /// <inheritdoc />
    public void DrawEllipse(MplRect bounds, ShapeStyle shape)
    {
        if (shape.HasVisibleFill)
        {
            var box = new RectF((float)bounds.X, (float)bounds.Y, (float)bounds.Width, (float)bounds.Height);
            Fill(shape, box, () => _canvas.FillEllipse(box.X, box.Y, box.Width, box.Height));
        }
        if (shape.HasVisibleStroke)
        {
            _canvas.StrokeColor = shape.Stroke!.Value.ToMauiColor();
            _canvas.StrokeSize = (float)shape.StrokeThickness;
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
    public void DrawPath(IReadOnlyList<PathSegment> segments, ShapeStyle shape)
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
                case ArcSegment arc:
                    // The path builder has lines and curves and no arc: the arc is followed with the curves that
                    // BezierSegment-based backends share (ArcSegmentExtensions), where it used to be dropped.
                    foreach (var curve in arc.ToBeziers())
                    {
                        path.CurveTo(
                            (float)curve.Control1.X, (float)curve.Control1.Y,
                            (float)curve.Control2.X, (float)curve.Control2.Y,
                            (float)curve.End.X, (float)curve.End.Y);
                    }
                    break;
                case CloseSegment:
                    path.Close();
                    break;
            }
        }

        if (shape.HasVisibleFill)
        {
            Fill(shape, path.Bounds, () => _canvas.FillPath(path));
        }
        if (shape.HasVisibleStroke)
        {
            _canvas.StrokeColor = shape.Stroke!.Value.ToMauiColor();
            _canvas.StrokeSize = (float)shape.StrokeThickness;
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
    /// <remarks>The library's own metrics, the ones the layout measured every label with: a canvas that measured with
    /// a width of its own would place text the layout had left room of another width for.</remarks>
    public MplSize MeasureText(string text, MplFont font) => FontMetricsDefault.Current.Measure(text, font);

    /// <inheritdoc />
    public void SetOpacity(double opacity)
    {
        _canvas.Alpha = (float)opacity;
    }

    /// <summary>Fills a shape the way the other backends do: the flat fill first, a gradient over it when the style has one,
    /// and the hatch over that. <paramref name="fill"/> paints the shape's outline with whatever paint is set, so it is
    /// called once for each layer.</summary>
    /// <param name="shape">The style.</param>
    /// <param name="bounds">The shape's bounding box: a gradient's ends and the origin of a pattern are relative to it.</param>
    /// <param name="fill">Paints the shape's outline with the canvas' current fill paint.</param>
    private void Fill(ShapeStyle shape, RectF bounds, Action fill)
    {
        _canvas.FillColor = shape.Fill!.Value.ToMauiColor();

        if (shape.HasVisibleGradient && bounds.Width > 0 && bounds.Height > 0)
        {
            _canvas.SetFillPaint(GradientPaintOf(shape.Gradient!.Value, bounds), bounds);
        }

        fill();

        if (shape.HasVisibleHatch)
        {
            Microsoft.Maui.Graphics.Color strokes = (shape.HatchColor ?? shape.Fill!.Value.ContrastingTextColor()).ToMauiColor();
            _canvas.SetFillPaint(new PatternPaint { Pattern = new HatchTilePattern(shape.Hatch, strokes) }, bounds);
            fill();
        }
    }

    /// <summary>A linear gradient paint for <paramref name="gradient"/>: the canvas takes its two ends as fractions of the
    /// rectangle it is set for, and the style gives them in the units of the picture.</summary>
    private static LinearGradientPaint GradientPaintOf(LinearGradient gradient, RectF bounds) => new()
    {
        StartColor = gradient.StartColor.ToMauiColor(),
        EndColor = gradient.EndColor.ToMauiColor(),
        StartPoint = new Microsoft.Maui.Graphics.Point((gradient.Start.X - bounds.X) / bounds.Width, (gradient.Start.Y - bounds.Y) / bounds.Height),
        EndPoint = new Microsoft.Maui.Graphics.Point((gradient.End.X - bounds.X) / bounds.Width, (gradient.End.Y - bounds.Y) / bounds.Height),
    };

    /// <summary>The tile of a hatch, as a pattern a canvas repeats over a fill: the marks <see cref="HatchTile"/> says, the
    /// same the SVG pattern and the raster shader draw.</summary>
    private sealed class HatchTilePattern(HatchPattern hatch, Microsoft.Maui.Graphics.Color strokes)
        : AbstractPattern((float)HatchTile.Size, (float)HatchTile.Size)
    {
        /// <inheritdoc />
        public override void Draw(ICanvas canvas)
        {
            canvas.StrokeColor = strokes;
            canvas.StrokeSize = 1f;
            canvas.FillColor = strokes;

            foreach (var mark in HatchTile.MarksOf(hatch))
            {
                if (mark.Kind == HatchMarkKind.Dot)
                {
                    canvas.FillCircle((float)mark.X1, (float)mark.Y1, 1f);
                }
                else
                {
                    canvas.DrawLine((float)mark.X1, (float)mark.Y1, (float)mark.X2, (float)mark.Y2);
                }
            }
        }
    }
}
