// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>
/// Shared test fake for <see cref="IRenderContext"/> that records every call as its own typed leaf of <see cref="CallRecord"/>. Lets
/// unit tests assert not only "a rectangle was drawn" but "a rectangle at (x, y, w, h) with fill=red, stroke=null", read by name
/// (<c>ctx.Of&lt;DrawRectangleCall&gt;()[0].Rect</c>), so a renamed argument fails to compile.
/// </summary>
internal class RecordingRenderContext : SilentRenderContext, IRenderContext, IOutlineClipRenderContext
{
    public List<CallRecord> Calls { get; } = new();

    /// <summary>The calls of one leaf, in the order they were made.</summary>
    public List<TCall> Of<TCall>() where TCall : CallRecord => [.. Calls.OfType<TCall>()];

    /// <summary>How many calls of one leaf were made.</summary>
    public int Count<TCall>() where TCall : CallRecord => Calls.Count(c => c is TCall);

    /// <summary>The rectangles drawn, in order.</summary>
    public List<Rect> Rects() => [.. Of<DrawRectangleCall>().Select(c => c.Rect)];

    /// <summary>The corners of every polygon drawn, in order.</summary>
    public List<Point[]> Polygons() => [.. Of<DrawPolygonCall>().Select(c => c.Points)];

    /// <summary>The fill of every shape drawn that has one, in order.</summary>
    public List<Color> Fills() => [.. Calls.OfType<ShapeCall>().Select(c => c.Fill).OfType<Color>()];

    /// <summary>The points of every polyline drawn, or of those drawn in <paramref name="colour"/> at <paramref name="width"/> when named.</summary>
    public List<Point[]> Polylines(Color? colour = null, double? width = null) =>
        [.. Of<DrawLinesCall>().Where(c => (colour is null || c.Color == colour) && (width is null || c.Thickness == width)).Select(c => c.Points)];

    public override void DrawLine(Point p1, Point p2, StrokeStyle stroke) => Calls.Add(new DrawLineCall(p1, p2, stroke));

    public override void DrawLines(IReadOnlyList<Point> points, StrokeStyle stroke) => Calls.Add(new DrawLinesCall([.. points], stroke));

    public override void DrawPolygon(IReadOnlyList<Point> points, ShapeStyle shape) => Calls.Add(new DrawPolygonCall([.. points], shape));

    public override void DrawCircle(Point center, double radius, ShapeStyle shape) => Calls.Add(new DrawCircleCall(center, radius, shape));

    public override void DrawRectangle(Rect rect, ShapeStyle shape) => Calls.Add(new DrawRectangleCall(rect, shape));

    public override void DrawEllipse(Rect bounds, ShapeStyle shape) => Calls.Add(new DrawEllipseCall(bounds, shape));

    public override void DrawText(string text, Point position, Font font, TextAlignment alignment)
        => Calls.Add(new DrawTextCall(text, position, font, alignment, 0));

    public void DrawText(string text, Point position, Font font, TextAlignment alignment, double rotation)
        => Calls.Add(new DrawTextCall(text, position, font, alignment, rotation));

    public override void DrawPath(IReadOnlyList<PathSegment> segments, ShapeStyle shape) => Calls.Add(new DrawPathCall([.. segments], shape));

    public override void PushClip(Rect clipRect) => Calls.Add(new PushClipCall(clipRect));

    public void PushClip(IReadOnlyList<PathSegment> outline) => Calls.Add(new PushClipOutlineCall([.. outline]));

    public override void PopClip() => Calls.Add(new PopClipCall());

    public override Size MeasureText(string text, Font font)
        => new(text.Length * font.Size * 0.6, font.Size);

    public override void SetOpacity(double opacity) => Calls.Add(new SetOpacityCall(opacity));

    public void BeginGroup(string cssClass) => Calls.Add(new BeginGroupCall(cssClass));

    public void EndGroup() => Calls.Add(new EndGroupCall());

    public void SetNextElementData(string key, string value) => Calls.Add(new SetNextElementDataCall(key, value));

    public void DrawRichText(RichText richText, Point position, Font font, TextAlignment alignment)
        => Calls.Add(new DrawRichTextCall(richText, position, font, alignment, 0));

    public void DrawRichText(RichText richText, Point position, Font font, TextAlignment alignment, double rotation)
        => Calls.Add(new DrawRichTextCall(richText, position, font, alignment, rotation));

    /// <summary>No call carried a coordinate, size, width or path vertex that is not finite, nor a text that says NaN or Infinity.</summary>
    public void AssertNothingUndrawableReached()
    {
        foreach (var call in Calls)
        {
            foreach (var argument in call.GetType().GetProperties())
            {
                object? value = argument.GetValue(call);
                Numbers(value).AssertAllFinite($"{call.Kind}.{argument.Name}");
                if (value is string text) text.AssertNoNonFiniteNumber($"{call.Kind}.{argument.Name}");
            }
        }
    }

    private static IEnumerable<double> Numbers(object? value) => value switch
    {
        double d => [d],
        Point p => [p.X, p.Y],
        Rect r => [r.X, r.Y, r.Width, r.Height],
        Point[] points => points.SelectMany(p => Numbers(p)),
        PathSegment[] segments => segments.SelectMany(s => s.GetType().GetProperties().SelectMany(p => Numbers(p.GetValue(s)))),
        _ => [],
    };

    /// <summary>Every call, in order, with every argument: two drawings with the same signature are the same drawing.</summary>
    /// <param name="digits">The decimals every point is rounded to; <see langword="null"/> (the default) keeps them whole.</param>
    public List<string> Signature(int? digits = null) => SignatureOf(Calls, digits);

    /// <summary>The <see cref="Signature"/> of the calls of one leaf only.</summary>
    public List<string> Signature<TCall>() where TCall : CallRecord => SignatureOf(Of<TCall>(), null);

    private static List<string> SignatureOf(IEnumerable<CallRecord> calls, int? digits) =>
        [.. calls.Select(c => c.Kind + "(" + string.Join("; ", c.GetType().GetProperties().Select(p => Describe(p.GetValue(c), digits))) + ")")];

    private static string Describe(object? value, int? digits) => value switch
    {
        Point p when digits is int d => Math.Round(p.X, d).ToString(CultureInfo.InvariantCulture) + "," + Math.Round(p.Y, d).ToString(CultureInfo.InvariantCulture),
        Point[] points => string.Join(" ", points),
        PathSegment[] segments => string.Join(" ", segments.Select(s => s.ToSvgPathData())),
        _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? "",
    };

    /// <summary>What the first group of class <paramref name="group"/> drew, as one box: its rectangles, polygons and lines, and its
    /// texts as wide as this surface measures them (a text turned upright as tall as it is wide).</summary>
    /// <param name="group">The group's class, as <see cref="BeginGroup"/> recorded it.</param>
    /// <returns>The box.</returns>
    public Rect DrawnBoxOf(string group)
    {
        double left = double.MaxValue, top = double.MaxValue, right = double.MinValue, bottom = double.MinValue;
        void Take(double x0, double y0, double x1, double y1)
        {
            left = Math.Min(left, Math.Min(x0, x1)); right = Math.Max(right, Math.Max(x0, x1));
            top = Math.Min(top, Math.Min(y0, y1)); bottom = Math.Max(bottom, Math.Max(y0, y1));
        }

        int depth = 0;
        bool inside = false;
        foreach (var call in Calls)
        {
            if (call is BeginGroupCall begin)
            {
                if (inside) depth++;
                else if (begin.CssClass == group) { inside = true; depth = 0; }
                continue;
            }
            if (!inside) continue;
            if (call is EndGroupCall)
            {
                if (depth-- == 0) break;
                continue;
            }

            switch (call)
            {
                case DrawRectangleCall { Rect: var r }:
                    Take(r.X, r.Y, r.X + r.Width, r.Y + r.Height);
                    break;
                case DrawPolygonCall { Points: var points }:
                    foreach (var p in points) Take(p.X, p.Y, p.X, p.Y);
                    break;
                case DrawLinesCall { Points: var points }:
                    foreach (var p in points) Take(p.X, p.Y, p.X, p.Y);
                    break;
                case DrawTextCall text:
                    var size = MeasureText(text.Text, text.Font);
                    Point at = text.Position;
                    bool upright = Math.Abs(text.Rotation) < 45;
                    double across = upright ? size.Width : size.Height, high = upright ? size.Height : size.Width;
                    double from = text.Alignment switch
                    {
                        TextAlignment.Left => at.X, TextAlignment.Right => at.X - across, _ => at.X - across / 2,
                    };
                    if (!upright) from = at.X - across / 2;
                    Take(from, at.Y - high, from + across, at.Y);
                    break;
            }
        }

        Assert.True(left < right, $"the {group} drew nothing");
        return new Rect(left, top, right - left, bottom - top);
    }

    /// <summary>The share of the box the first group of class <paramref name="group"/> drew (<see cref="DrawnBoxOf"/>) that stands on a
    /// figure of <paramref name="width"/> by <paramref name="height"/>, by area: 1 when all of it does, 0 when it stands off the figure.</summary>
    public double DrawnShareOnFigure(string group, double width, double height)
    {
        var box = DrawnBoxOf(group);
        double across = Math.Max(0, Math.Min(width, box.X + box.Width) - Math.Max(0, box.X));
        double down = Math.Max(0, Math.Min(height, box.Y + box.Height) - Math.Max(0, box.Y));
        return across * down / (box.Width * box.Height);
    }
}

/// <summary>What a run of recorded calls drew, read as one thing.</summary>
internal static class CallRecordExtensions
{
    extension(IEnumerable<CallRecord> calls)
    {
        /// <summary>The last rectangle drawn with no fill: the frame a colour bar draws round itself.</summary>
        public DrawRectangleCall Frame() => calls.OfType<DrawRectangleCall>().Last(c => c.Fill is null);
    }

    extension(CallRecord call)
    {
        /// <summary>Whether the call fills, outlines or strokes in <paramref name="colour"/>.</summary>
        public bool Paints(Color colour) => call switch
        {
            ShapeCall shape => shape.Fill == colour || shape.Stroke == colour,
            StrokeCall line => line.Color == colour,
            _ => false,
        };
    }
}

/// <summary>One call a <see cref="RecordingRenderContext"/> kept: a sealed leaf per call of the surface, its arguments typed and named
/// after the parameters. <see cref="Kind"/> is the name of the call (the leaf's, without <c>Call</c>), for a test that holds the order of
/// the calls.</summary>
internal abstract record CallRecord
{
    public string Kind => GetType().Name[..^4];
}

/// <summary>A call that draws a line, read off the stroke it was handed.</summary>
internal abstract record StrokeCall(StrokeStyle Line) : CallRecord
{
    public Color Color => Line.Color;

    public double Thickness => Line.Thickness;

    public LineStyle Style => Line.Style;

    public JoinStyle? Join => Line.Join;
}

/// <summary>A call that fills or outlines a shape, read off the style it was handed.</summary>
internal abstract record ShapeCall(ShapeStyle Shape) : CallRecord
{
    public Color? Fill => Shape.Fill;

    public Color? Stroke => Shape.Stroke;

    public double StrokeThickness => Shape.StrokeThickness;
}

internal sealed record DrawLineCall(Point P1, Point P2, StrokeStyle Line) : StrokeCall(Line);

internal sealed record DrawLinesCall(Point[] Points, StrokeStyle Line) : StrokeCall(Line);

internal sealed record DrawPolygonCall(Point[] Points, ShapeStyle Shape) : ShapeCall(Shape);

internal sealed record DrawCircleCall(Point Center, double Radius, ShapeStyle Shape) : ShapeCall(Shape);

internal sealed record DrawRectangleCall(Rect Rect, ShapeStyle Shape) : ShapeCall(Shape);

internal sealed record DrawEllipseCall(Rect Bounds, ShapeStyle Shape) : ShapeCall(Shape);

internal sealed record DrawPathCall(PathSegment[] Segments, ShapeStyle Shape) : ShapeCall(Shape);

/// <summary>A call that draws a text, plain or rich, at a point.</summary>
internal abstract record TextCall(Point Position, Font Font, TextAlignment Alignment, double Rotation) : CallRecord;

internal sealed record DrawTextCall(string Text, Point Position, Font Font, TextAlignment Alignment, double Rotation)
    : TextCall(Position, Font, Alignment, Rotation);

internal sealed record DrawRichTextCall(RichText RichText, Point Position, Font Font, TextAlignment Alignment, double Rotation)
    : TextCall(Position, Font, Alignment, Rotation);

internal sealed record PushClipCall(Rect ClipRect) : CallRecord;

internal sealed record PushClipOutlineCall(PathSegment[] Outline) : CallRecord;

internal sealed record PopClipCall : CallRecord;

internal sealed record SetOpacityCall(double Opacity) : CallRecord;

internal sealed record BeginGroupCall(string CssClass) : CallRecord;

internal sealed record EndGroupCall : CallRecord;

internal sealed record SetNextElementDataCall(string Key, string Value) : CallRecord;
