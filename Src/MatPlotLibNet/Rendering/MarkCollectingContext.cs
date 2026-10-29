// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering;

/// <summary>A drawing target that draws nothing: it turns what a renderer draws into the marks a legend keeps clear of
/// (<see cref="MarkObstacles"/>) and measures text through the context a real draw would use. A renderer whose marks are few
/// (a pie, a network, a dendrogram) hands its own drawing to it, so its marks are the ones it draws by construction; a renderer
/// that draws thousands of marks works its pixels out once instead (<see cref="SeriesRenderers.SeriesRenderer{T}.AddMarks"/>).</summary>
internal sealed class MarkCollectingContext : IRenderContext
{
    /// <summary>The angle between the points an arc is sampled at, in degrees.</summary>
    private const double ArcStepDegrees = 6;

    private readonly IRenderContext _measure;
    private readonly MarkObstacles.Builder _marks;

    /// <summary>Constructs a collecting context.</summary>
    /// <param name="measure">The context text is measured with: the one the figure is drawn into.</param>
    /// <param name="marks">The collection the marks go into.</param>
    internal MarkCollectingContext(IRenderContext measure, MarkObstacles.Builder marks)
    {
        _measure = measure;
        _marks = marks;
    }

    /// <inheritdoc />
    public void DrawLine(Point p1, Point p2, StrokeStyle stroke)
    {
        if (p1.CanDrawLineTo(p2)) _marks.AddPath([p1, p2]);
    }

    /// <inheritdoc />
    public void DrawLines(IReadOnlyList<Point> points, StrokeStyle stroke)
    {
        foreach (var run in points.DrawableRuns())
            if (run.Count >= 2) _marks.AddPath(run);
    }

    /// <inheritdoc />
    public void DrawPolygon(IReadOnlyList<Point> points, ShapeStyle shape) => _marks.AddPath(points);

    /// <inheritdoc />
    public void DrawCircle(Point center, double radius, ShapeStyle shape)
    {
        if (center.IsDrawable()) _marks.AddPoint(center);
    }

    /// <inheritdoc />
    public void DrawRectangle(Rect rect, ShapeStyle shape)
    {
        if (rect.IsDrawable()) _marks.AddBox(rect);
    }

    /// <inheritdoc />
    public void DrawEllipse(Rect bounds, ShapeStyle shape)
    {
        if (bounds.IsDrawable()) _marks.AddBox(bounds);
    }

    /// <inheritdoc />
    public void DrawText(string text, Point position, Font font, TextAlignment alignment) =>
        DrawText(text, position, font, alignment, 0);

    /// <inheritdoc />
    public void DrawText(string text, Point position, Font font, TextAlignment alignment, double rotation)
    {
        if (!position.IsDrawable()) return;
        _marks.AddBox(new TextBox(position, _measure.MeasureText(text, font), font.Size, alignment,
            TextVerticalAlignment.Baseline, rotation).Bounds());
    }

    /// <inheritdoc />
    public void DrawPath(IReadOnlyList<PathSegment> segments, ShapeStyle shape)
    {
        var vertices = new List<Point>();
        foreach (var segment in segments)
        {
            switch (segment)
            {
                case MoveToSegment move:
                    vertices.Add(move.Point);
                    break;
                case LineToSegment line:
                    vertices.Add(line.Point);
                    break;
                case BezierSegment bezier:
                    vertices.Add(bezier.End);
                    break;
                case ArcSegment arc:
                    AddArc(vertices, arc);
                    break;
            }
        }
        if (vertices.Count > 0) _marks.AddPath(vertices);
    }

    /// <summary>Samples an arc into points, from its start angle to its end angle, the way the arc is drawn about its centre.</summary>
    private static void AddArc(List<Point> vertices, ArcSegment arc)
    {
        int steps = Math.Max(1, (int)Math.Ceiling(Math.Abs(arc.EndAngle - arc.StartAngle) / ArcStepDegrees));
        for (int i = 0; i <= steps; i++)
        {
            double radians = (arc.StartAngle + (arc.EndAngle - arc.StartAngle) * i / steps) * Math.PI / 180;
            vertices.Add(new Point(arc.Center.X + arc.RadiusX * Math.Cos(radians), arc.Center.Y + arc.RadiusY * Math.Sin(radians)));
        }
    }

    /// <inheritdoc />
    public void PushClip(Rect clipRect) { }

    /// <inheritdoc />
    public void PopClip() { }

    /// <inheritdoc />
    public Size MeasureText(string text, Font font) => _measure.MeasureText(text, font);

    /// <inheritdoc />
    public Size MeasureRichText(MathText.RichText richText, Font font) => _measure.MeasureRichText(richText, font);

    /// <inheritdoc />
    public void SetOpacity(double opacity) { }
}
