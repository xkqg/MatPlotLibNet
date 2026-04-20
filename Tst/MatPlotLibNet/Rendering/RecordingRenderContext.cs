// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>
/// Shared test fake for <see cref="IRenderContext"/> that records every call with
/// full argument payload. Lets unit tests assert not only "a rectangle was drawn"
/// but "a rectangle at (x, y, w, h) with fill=red, stroke=null" — needed for
/// 100/100 coverage of extracted renderers where the only observable behaviour
/// is the sequence of calls on the context.
/// </summary>
internal sealed class RecordingRenderContext : IRenderContext
{
    public List<CallRecord> Calls { get; } = new();

    public List<CallRecord> OfKind(string kind) =>
        Calls.Where(c => c.Kind == kind).ToList();

    public int CountOf(string kind) => Calls.Count(c => c.Kind == kind);

    public void DrawLine(Point p1, Point p2, Color color, double thickness, LineStyle style)
        => Calls.Add(new CallRecord("DrawLine", new { p1, p2, color, thickness, style }));

    public void DrawLines(IReadOnlyList<Point> points, Color color, double thickness, LineStyle style)
        => Calls.Add(new CallRecord("DrawLines", new { points = points.ToArray(), color, thickness, style }));

    public void DrawPolygon(IReadOnlyList<Point> points, Color? fill, Color? stroke, double strokeThickness)
        => Calls.Add(new CallRecord("DrawPolygon", new { points = points.ToArray(), fill, stroke, strokeThickness }));

    public void DrawCircle(Point center, double radius, Color? fill, Color? stroke, double strokeThickness)
        => Calls.Add(new CallRecord("DrawCircle", new { center, radius, fill, stroke, strokeThickness }));

    public void DrawRectangle(Rect rect, Color? fill, Color? stroke, double strokeThickness)
        => Calls.Add(new CallRecord("DrawRectangle", new { rect, fill, stroke, strokeThickness }));

    public void DrawEllipse(Rect bounds, Color? fill, Color? stroke, double strokeThickness)
        => Calls.Add(new CallRecord("DrawEllipse", new { bounds, fill, stroke, strokeThickness }));

    public void DrawText(string text, Point position, Font font, TextAlignment alignment)
        => Calls.Add(new CallRecord("DrawText", new { text, position, font, alignment, rotation = 0.0 }));

    public void DrawText(string text, Point position, Font font, TextAlignment alignment, double rotation)
        => Calls.Add(new CallRecord("DrawText", new { text, position, font, alignment, rotation }));

    public void DrawPath(IReadOnlyList<PathSegment> segments, Color? fill, Color? stroke, double strokeThickness)
        => Calls.Add(new CallRecord("DrawPath", new { segments = segments.ToArray(), fill, stroke, strokeThickness }));

    public void PushClip(Rect clipRect)
        => Calls.Add(new CallRecord("PushClip", new { clipRect }));

    public void PopClip() => Calls.Add(new CallRecord("PopClip", null));

    public Size MeasureText(string text, Font font)
        => new(text.Length * font.Size * 0.6, font.Size);

    public void SetOpacity(double opacity)
        => Calls.Add(new CallRecord("SetOpacity", new { opacity }));

    public void BeginGroup(string cssClass)
        => Calls.Add(new CallRecord("BeginGroup", new { cssClass }));

    public void EndGroup() => Calls.Add(new CallRecord("EndGroup", null));

    public void SetNextElementData(string key, string value)
        => Calls.Add(new CallRecord("SetNextElementData", new { key, value }));

    public void DrawRichText(RichText richText, Point position, Font font, TextAlignment alignment)
        => Calls.Add(new CallRecord("DrawRichText", new { richText, position, font, alignment, rotation = 0.0 }));

    public void DrawRichText(RichText richText, Point position, Font font, TextAlignment alignment, double rotation)
        => Calls.Add(new CallRecord("DrawRichText", new { richText, position, font, alignment, rotation }));
}

internal sealed record CallRecord(string Kind, object? Args);
