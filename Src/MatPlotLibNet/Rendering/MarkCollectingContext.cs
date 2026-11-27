// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Rendering.TextMeasurement;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering;

/// <summary>A drawing target that draws nothing: it turns what a renderer draws into the marks a legend keeps clear of
/// (<see cref="MarkObstacles"/>) and measures text through the context a real draw would use. A renderer whose marks are few
/// (a pie, a network, a dendrogram) hands its own drawing to it, so its marks are the ones it draws by construction; a renderer
/// that draws thousands of marks works its pixels out once instead (<see cref="SeriesRenderers.SeriesRenderer{T}.AddMarks"/>).</summary>
internal sealed class MarkCollectingContext : IRenderContext, IThemedRenderContext
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
        foreach (var run in points.LineRuns())
            _marks.AddPath(run);
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
        _marks.AddBox(new TextBox(new TextPlacement(position, alignment) { Rotation = rotation },
            _measure.ExtentOf(TextLabel.Plain(text), font)).Bounds());
    }

    /// <inheritdoc />
    public void DrawPath(IReadOnlyList<PathSegment> segments, ShapeStyle shape)
    {
        var vertices = new Point[VertexCountOf(segments)];
        var sink = new VertexSink(vertices);
        segments.WalkInto(ref sink);
        if (vertices.Length > 0) _marks.AddPath(vertices);
    }

    /// <summary>How many vertices a path becomes (<see cref="VertexSink"/>): a close, and a segment of a kind the library does not
    /// draw, make none. Counted first so the vertices are made once, at that size.</summary>
    private static int VertexCountOf(IReadOnlyList<PathSegment> segments)
    {
        int count = 0;
        for (int i = 0; i < segments.Count; i++)
            count += segments[i] switch { MoveToSegment or LineToSegment or BezierSegment => 1, ArcSegment arc => StepsOf(arc) + 1, _ => 0 };
        return count;
    }

    /// <summary>The steps an arc is sampled in: one for each <see cref="ArcStepDegrees"/> of its sweep, at least one.</summary>
    private static int StepsOf(ArcSegment arc) =>
        Math.Max(1, (int)Math.Ceiling(Math.Abs(arc.EndAngle - arc.StartAngle) / ArcStepDegrees));

    /// <summary>What a path becomes for the marks: its vertices. A move and a line are a vertex, a curve is its end point, a close
    /// is nothing (the first vertex is already there) and an arc is sampled, from its start angle to its end angle, the way the
    /// arc is drawn about its centre: the first vertex is the arc's <see cref="ArcSegment.StartPoint"/> and the last its
    /// <see cref="ArcSegment.EndPoint"/>, the points the drawn arc begins and ends on, and only the points between are worked
    /// out from the step.</summary>
    private struct VertexSink(Point[] vertices) : IPathSink
    {
        private int _at;

        public void MoveTo(Point point) => vertices[_at++] = point;

        public void LineTo(Point point) => vertices[_at++] = point;

        public void CubicTo(Point control1, Point control2, Point end) => vertices[_at++] = end;

        public void ArcTo(ArcSegment arc)
        {
            int steps = StepsOf(arc);
            vertices[_at++] = arc.StartPoint;
            for (int i = 1; i < steps; i++)
            {
                double radians = (arc.StartAngle + (arc.EndAngle - arc.StartAngle) * i / steps).ToRadians();
                vertices[_at++] = arc.PointAt(radians);
            }
            vertices[_at++] = arc.EndPoint;
        }

        public void Close() { }
    }

    /// <inheritdoc />
    public void PushClip(Rect clipRect) { }

    /// <inheritdoc />
    public void PopClip() { }

    /// <inheritdoc />
    public Size MeasureText(string text, Font font) => _measure.MeasureText(text, font);

    /// <inheritdoc />
    public void SetOpacity(double opacity) { }

    /// <summary>Tells the surface text is measured with: the collector itself draws nothing by a look.</summary>
    /// <param name="looks">The looks of the theme of the figure being drawn.</param>
    void IThemedRenderContext.UseLooks(ThemeLooks looks) => _measure.TellLooks(looks);

    /// <summary>The looks of the surface text is measured with, so a text is measured here as it is there.</summary>
    ThemeLooks IThemedRenderContext.Looks => _measure.LooksTold;

    /// <summary>Tells the surface text is measured with the math texts its figure keeps, which a label measured here reads there.</summary>
    /// <param name="texts">The math texts the figure being drawn keeps.</param>
    void IThemedRenderContext.UseMathTexts(KeptMathTexts texts) => _measure.TellMathTexts(texts);

    /// <summary>The math texts of the surface text is measured with.</summary>
    KeptMathTexts? IThemedRenderContext.MathTexts => _measure.MathTextsTold;

    /// <summary>The metrics of the surface text is measured with, which the collector's own measure forwards to.</summary>
    IFontMetrics? IThemedRenderContext.BoxMetrics => _measure.BoxMetricsTold;

    /// <summary>The math font set of the surface text is measured with.</summary>
    MathText.MathFontSet IThemedRenderContext.MathFonts => _measure.MathFontsTold;
}
