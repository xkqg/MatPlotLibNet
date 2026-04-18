// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering;

/// <summary>Specifies the horizontal alignment of rendered text.</summary>
/// <remarks><b>Append-only ordinal contract (Phase O of v1.7.2):</b> never reorder,
/// remove, or renumber. See <c>EnumOrdinalContractTests</c>.</remarks>
public enum TextAlignment { Left = 0, Center = 1, Right = 2 }

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

    /// <summary>
    /// Measures a <see cref="RichText"/> value by summing per-span widths at their effective font sizes.
    /// Default implementation measures each span independently and uses the max single-span height.
    /// </summary>
    Size MeasureRichText(RichText richText, Font font)
    {
        double totalWidth = 0;
        double maxHeight = 0;
        foreach (var span in richText.Spans)
        {
            var spanFont = span.FontSizeScale == 1.0 ? font : font with { Size = font.Size * span.FontSizeScale };
            var size = MeasureText(span.Text, spanFont);
            totalWidth += size.Width;
            if (size.Height > maxHeight) maxHeight = size.Height;
        }
        return new Size(totalWidth, maxHeight);
    }

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

/// <summary>
/// A path segment that draws an elliptical arc. Angles are in degrees, screen-Y-down
/// convention (0° = +X / 3 o'clock, positive angles sweep clockwise on screen).
/// </summary>
/// <remarks>
/// The arc's START point is implicit — it must equal the current path point (the end of
/// the previous segment). The caller normally emits a <see cref="MoveToSegment"/> or
/// <see cref="LineToSegment"/> to position at the perimeter at <paramref name="StartAngle"/>
/// before this arc. The end point (passed to the SVG <c>A</c> command) is computed here
/// from <paramref name="Center"/>, <paramref name="RadiusX"/>, and
/// <paramref name="EndAngle"/>.
/// </remarks>
public sealed record ArcSegment(Point Center, double RadiusX, double RadiusY,
    double StartAngle, double EndAngle) : PathSegment
{
    /// <inheritdoc />
    public override string ToSvgPathData()
    {
        // Compute the actual end point of the arc (on the perimeter at EndAngle). The
        // earlier implementation emitted (Center.X, Center.Y) which caused the arc to
        // sweep back to the center, producing petal-shaped wedges in SunburstSeries.
        double endRad = EndAngle * Math.PI / 180.0;
        double endX = Center.X + RadiusX * Math.Cos(endRad);
        double endY = Center.Y + RadiusY * Math.Sin(endRad);

        // large-arc-flag: 1 if the angular sweep is greater than 180°.
        int large = Math.Abs(EndAngle - StartAngle) > 180.0 ? 1 : 0;
        // sweep-flag: 1 = clockwise on screen (positive sweep in screen-Y-down), 0 = CCW.
        // The sunburst renderer uses reverse direction for the inner arc (EndAngle < StartAngle)
        // to close the wedge polygon, so we must respect the sign.
        int sweep = EndAngle >= StartAngle ? 1 : 0;

        return $"A {F(RadiusX)} {F(RadiusY)} 0 {large} {sweep} {F(endX)} {F(endY)} ";
    }
}

/// <summary>A path segment that closes the current sub-path by drawing a line back to its start.</summary>
public sealed record CloseSegment() : PathSegment
{
    /// <inheritdoc />
    public override string ToSvgPathData() => "Z ";
}
