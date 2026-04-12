// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering;

/// <summary>Specifies the horizontal alignment of rendered text.</summary>
public enum TextAlignment { Left, Center, Right }

/// <summary>Defines the rendering surface abstraction for drawing chart primitives.</summary>
public interface IRenderContext
{
    /// <summary>Draws a straight line between two points.</summary>
    void DrawLine(Point p1, Point p2, Color color, double thickness, LineStyle style);

    /// <summary>Draws a connected polyline through the specified points.</summary>
    void DrawLines(IReadOnlyList<Point> points, Color color, double thickness, LineStyle style);

    /// <summary>Draws a filled and/or stroked polygon defined by the given vertices.</summary>
    void DrawPolygon(IReadOnlyList<Point> points, Color? fill, Color? stroke, double strokeThickness);

    /// <summary>Draws a circle at the specified center with the given radius.</summary>
    void DrawCircle(Point center, double radius, Color? fill, Color? stroke, double strokeThickness);

    /// <summary>Draws a filled and/or stroked rectangle.</summary>
    void DrawRectangle(Rect rect, Color? fill, Color? stroke, double strokeThickness);

    /// <summary>Draws an ellipse inscribed within the specified bounding rectangle.</summary>
    void DrawEllipse(Rect bounds, Color? fill, Color? stroke, double strokeThickness);

    /// <summary>Draws a text string at the specified position with the given font and alignment.</summary>
    void DrawText(string text, Point position, Font font, TextAlignment alignment);

    /// <summary>
    /// Draws a rotated text string at the specified position.
    /// <paramref name="rotation"/> is in degrees, positive = counter-clockwise in standard math orientation.
    /// Default implementation ignores rotation and delegates to the non-rotated overload.
    /// </summary>
    /// <param name="text">The string to draw.</param>
    /// <param name="position">The anchor point in pixel space.</param>
    /// <param name="font">Font family, size, weight, and color.</param>
    /// <param name="alignment">Horizontal alignment relative to <paramref name="position"/>.</param>
    /// <param name="rotation">Rotation angle in degrees (positive = counter-clockwise).</param>
    void DrawText(string text, Point position, Font font, TextAlignment alignment, double rotation)
        => DrawText(text, position, font, alignment);

    /// <summary>Draws a complex path composed of move, line, bezier, arc, and close segments.</summary>
    void DrawPath(IReadOnlyList<PathSegment> segments, Color? fill, Color? stroke, double strokeThickness);

    /// <summary>Pushes a rectangular clipping region onto the clip stack.</summary>
    void PushClip(Rect clipRect);

    /// <summary>Pops the most recent clipping region from the clip stack.</summary>
    void PopClip();

    /// <summary>Measures the size of the given text when rendered with the specified font.</summary>
    /// <returns>The computed size of the text bounding box.</returns>
    Size MeasureText(string text, Font font);

    /// <summary>Sets the global opacity for subsequent drawing operations.</summary>
    void SetOpacity(double opacity);

    /// <summary>Begins a named group (e.g., emits <c>&lt;g class="..."&gt;</c> in SVG). Default is a no-op.</summary>
    void BeginGroup(string cssClass) { }

    /// <summary>Attaches a data attribute on the next drawn SVG element. No-op for non-SVG contexts.</summary>
    void SetNextElementData(string key, string value) { }

    /// <summary>Ends the current group. Default is a no-op.</summary>
    void EndGroup() { }

    /// <summary>
    /// Draws a <see cref="RichText"/> value that may contain superscript, subscript, and Unicode-substituted
    /// math characters.  The default implementation concatenates all span text and delegates to
    /// <see cref="DrawText(string, Point, Font, TextAlignment)"/> for backends that do not support rich text.
    /// Override in <c>SvgRenderContext</c> to emit <c>&lt;tspan&gt;</c> elements.
    /// </summary>
    void DrawRichText(RichText richText, Point position, Font font, TextAlignment alignment)
    {
        var sb = new StringBuilder();
        foreach (var span in richText.Spans) sb.Append(span.Text);
        DrawText(sb.ToString(), position, font, alignment);
    }

    /// <summary>
    /// Draws a rotated <see cref="RichText"/> value. Default implementation ignores rotation.
    /// Override in <c>SvgRenderContext</c> to emit the rotation transform.
    /// </summary>
    void DrawRichText(RichText richText, Point position, Font font, TextAlignment alignment, double rotation)
        => DrawRichText(richText, position, font, alignment);
}

/// <summary>Base record for path drawing segments used by <see cref="IRenderContext.DrawPath"/>.</summary>
public abstract record PathSegment
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    /// <summary>Returns the SVG path data command string for this segment (e.g., "M 10 20 ").</summary>
    public abstract string ToSvgPathData();

    /// <summary>Formats a double value for SVG path data using invariant culture.</summary>
    protected static string F(double value) => value.ToString("G", Inv);
}

/// <summary>A path segment that moves the current position without drawing.</summary>
public sealed record MoveToSegment(Point Point) : PathSegment
{
    /// <inheritdoc />
    public override string ToSvgPathData() => $"M {F(Point.X)} {F(Point.Y)} ";
}

/// <summary>A path segment that draws a straight line to the specified point.</summary>
public sealed record LineToSegment(Point Point) : PathSegment
{
    /// <inheritdoc />
    public override string ToSvgPathData() => $"L {F(Point.X)} {F(Point.Y)} ";
}

/// <summary>A path segment that draws a cubic Bezier curve through two control points to an endpoint.</summary>
public sealed record BezierSegment(Point Control1, Point Control2, Point End) : PathSegment
{
    /// <inheritdoc />
    public override string ToSvgPathData() =>
        $"C {F(Control1.X)} {F(Control1.Y)} {F(Control2.X)} {F(Control2.Y)} {F(End.X)} {F(End.Y)} ";
}

/// <summary>A path segment that draws an elliptical arc.</summary>
public sealed record ArcSegment(Point Center, double RadiusX, double RadiusY,
    double StartAngle, double EndAngle) : PathSegment
{
    /// <inheritdoc />
    public override string ToSvgPathData() =>
        $"A {F(RadiusX)} {F(RadiusY)} 0 0 1 {F(Center.X)} {F(Center.Y)} ";
}

/// <summary>A path segment that closes the current sub-path by drawing a line back to its start.</summary>
public sealed record CloseSegment() : PathSegment
{
    /// <inheritdoc />
    public override string ToSvgPathData() => "Z ";
}
