// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests;

/// <summary>A surface that draws nothing: the twelve members of <see cref="IRenderContext"/> that have no default body, each inert and
/// virtual, so a test double overrides only the ones it observes. A text measures as long as it has characters at 0.6 of the font size
/// and as tall as the font.
/// <para>It declares no member the interface gives a default body (the turned text, the rich text, the groups, the hyperlinks), so
/// those still run as the interface writes them. A double that observes one of those lists <see cref="IRenderContext"/> again beside
/// this base and implements the member.</para></summary>
internal class SilentRenderContext : IRenderContext
{
    /// <inheritdoc />
    public virtual void DrawLine(Point p1, Point p2, StrokeStyle stroke) { }

    /// <inheritdoc />
    public virtual void DrawLines(IReadOnlyList<Point> points, StrokeStyle stroke) { }

    /// <inheritdoc />
    public virtual void DrawPolygon(IReadOnlyList<Point> points, ShapeStyle shape) { }

    /// <inheritdoc />
    public virtual void DrawCircle(Point center, double radius, ShapeStyle shape) { }

    /// <inheritdoc />
    public virtual void DrawRectangle(Rect rect, ShapeStyle shape) { }

    /// <inheritdoc />
    public virtual void DrawEllipse(Rect bounds, ShapeStyle shape) { }

    /// <inheritdoc />
    public virtual void DrawText(string text, Point position, Font font, TextAlignment alignment) { }

    /// <inheritdoc />
    public virtual void DrawPath(IReadOnlyList<PathSegment> segments, ShapeStyle shape) { }

    /// <inheritdoc />
    public virtual void PushClip(Rect clipRect) { }

    /// <inheritdoc />
    public virtual void PopClip() { }

    /// <inheritdoc />
    public virtual Size MeasureText(string text, Font font) => new(text.Length * font.Size * 0.6, font.Size);

    /// <inheritdoc />
    public virtual void SetOpacity(double opacity) { }
}
