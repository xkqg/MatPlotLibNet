// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Rendering.TextMeasurement;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering;

/// <summary>Specifies the horizontal alignment of rendered text.</summary>
/// <remarks><b>Append-only ordinal contract:</b> never reorder,
/// remove, or renumber.</remarks>
public enum TextAlignment
{
    /// <summary>The text starts at the given position and runs to its right.</summary>
    Left = 0,
    /// <summary>The text is centred on the given position.</summary>
    Center = 1,
    /// <summary>The text ends at the given position and runs to its left.</summary>
    Right = 2
}

/// <summary>Defines the rendering surface abstraction for drawing chart primitives.</summary>
public interface IRenderContext
{
    /// <summary>Draws a straight line between two points.</summary>
    void DrawLine(Point p1, Point p2, StrokeStyle stroke);

    /// <summary>Draws a connected polyline through the specified points.</summary>
    void DrawLines(IReadOnlyList<Point> points, StrokeStyle stroke);

    /// <summary>Draws a filled and/or stroked polygon defined by the given vertices.</summary>
    void DrawPolygon(IReadOnlyList<Point> points, ShapeStyle shape);

    /// <summary>Draws a circle at the specified center with the given radius.</summary>
    void DrawCircle(Point center, double radius, ShapeStyle shape);

    /// <summary>Draws a filled and/or stroked rectangle.</summary>
    void DrawRectangle(Rect rect, ShapeStyle shape);

    /// <summary>Draws an ellipse inscribed within the specified bounding rectangle.</summary>
    void DrawEllipse(Rect bounds, ShapeStyle shape);

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
    void DrawPath(IReadOnlyList<PathSegment> segments, ShapeStyle shape);

    /// <summary>Pushes a rectangular clipping region onto the clip stack.</summary>
    /// <remarks>The library's own surfaces (SVG, Skia, MAUI) cut the marks drawn under the clip, never a text: a label, a value or a name
    /// stands whole past it, as matplotlib draws a text.</remarks>
    void PushClip(Rect clipRect);

    /// <summary>Pops the most recent clipping region from the clip stack.</summary>
    void PopClip();

    /// <summary>Measures the size of the given text when rendered with the specified font.</summary>
    /// <returns>The computed size of the text bounding box.</returns>
    Size MeasureText(string text, Font font);

    /// <summary>
    /// Measures a <see cref="RichText"/> value as it is laid out: its fractions stacked, its scripts raised and dropped, each run of
    /// text measured by <see cref="MeasureText"/> in the label's own face. Default implementation lays the text out once, the way
    /// every surface draws it.
    /// </summary>
    /// <param name="richText">The text, in spans.</param>
    /// <param name="font">The font of the whole text, which a span's own size and style change.</param>
    /// <returns>The width of the text, and its height above and below the baseline (at least the line of the font).</returns>
    Size MeasureRichText(RichText richText, Font font) => this.BoxOf(richText, font);

    /// <summary>Sets the global opacity for subsequent drawing operations, from 0 (nothing drawn) to 1 (full): a value outside that range
    /// is held at the nearest end, by every surface of the library.</summary>
    void SetOpacity(double opacity);

    /// <summary>Begins a named group (e.g., emits <c>&lt;g class="..."&gt;</c> in SVG). Default is a no-op.</summary>
    void BeginGroup(string cssClass) { }

    /// <summary>Attaches a data attribute on the next drawn SVG element. No-op for non-SVG contexts.</summary>
    void SetNextElementData(string key, string value) { }

    /// <summary>Asks that the next text drawn be set exactly <paramref name="width"/> wide, whatever font the viewer finds for it:
    /// SVG writes <c>textLength</c> with <c>lengthAdjust="spacingAndGlyphs"</c>, so a line that was measured and packed in one font
    /// does not run into its neighbour in a wider one. It applies to the next text only; a width that is not a positive number is
    /// ignored. No-op for a context that sets no markup text.</summary>
    /// <param name="width">The width the text is to be set to, in pixels.</param>
    void SetNextTextLength(double width) { }

    /// <summary>Ends the current group. Default is a no-op.</summary>
    void EndGroup() { }

    /// <summary>Begins a hyperlink around the elements drawn until <see cref="EndHyperlink"/> — in SVG an
    /// <c>&lt;a href="…"&gt;</c> with the pointer cursor and an optional <c>aria-label</c>. Default is a no-op:
    /// a raster backend has nothing to link.</summary>
    /// <param name="url">The link target.</param>
    /// <param name="ariaLabel">The accessible name, or null for none.</param>
    /// <param name="expanded">For a link that discloses something on the same page: whether it is currently
    /// shown (<c>aria-expanded</c>). Null for an ordinary link.</param>
    void BeginHyperlink(string url, string? ariaLabel, bool? expanded = null) { }

    /// <summary>Ends the current hyperlink. Default is a no-op.</summary>
    void EndHyperlink() { }

    /// <summary>Draws text that reports its depth in a tree (<c>aria-level</c>, 1-based) — what makes a tree
    /// grid navigable rather than merely visible. Backends without markup draw it as ordinary text.</summary>
    void DrawTextWithLevel(string text, Point position, Font font, int level)
        => DrawText(text, position, font, TextAlignment.Left);

    /// <summary>
    /// Draws a <see cref="RichText"/> value: its scripts, fractions, radicals and matrices laid out once, as every surface lays them
    /// out, each run of glyphs drawn through <see cref="DrawText(string, Point, Font, TextAlignment)"/> and each rule (a fraction's
    /// line, a radical's bar) through <see cref="DrawRectangle"/>, so any surface draws math.
    /// </summary>
    void DrawRichText(RichText richText, Point position, Font font, TextAlignment alignment)
        => this.Draw(TextLabel.OfRich(richText), font, new TextPlacement(position, alignment));

    /// <summary>
    /// Draws a <see cref="RichText"/> value turned by <paramref name="rotation"/> degrees counter-clockwise about
    /// <paramref name="position"/>: laid out as the unturned overload lays it out, each run drawn through the turned
    /// <see cref="DrawText(string, Point, Font, TextAlignment, double)"/>.
    /// </summary>
    void DrawRichText(RichText richText, Point position, Font font, TextAlignment alignment, double rotation)
        => this.Draw(TextLabel.OfRich(richText), font, new TextPlacement(position, alignment) { Rotation = rotation });
}

/// <summary>Base record for path drawing segments used by <see cref="IRenderContext.DrawPath"/>.</summary>
public abstract record PathSegment
{
    /// <summary>Returns the SVG path data command string for this segment (e.g., "M 10 20 "). The library's own segments
    /// write their numbers to two decimals, a hundredth of a pixel, as every SVG coordinate is written.</summary>
    public abstract string ToSvgPathData();

    /// <summary>Formats a double value for SVG path data using invariant culture.</summary>
    protected static string F(double value) => value.InvariantText();

    /// <summary>Writes this segment's path data straight into the document's builder, so a path of thousands of segments
    /// makes no string per segment. A segment a user derives is written through its <see cref="ToSvgPathData"/>; the
    /// library's own segments override this with the one number format (<c>AppendSvgNumber</c>)
    /// and build their <see cref="ToSvgPathData"/> from it, so a segment has one formatting rule.</summary>
    /// <param name="sb">The builder the path data is appended to.</param>
    internal virtual void AppendSvgPathData(StringBuilder sb) => sb.Append(ToSvgPathData());

    /// <summary>This segment's path data as a string, made by <see cref="AppendSvgPathData"/>: what a library segment's
    /// <see cref="ToSvgPathData"/> returns.</summary>
    internal string FormatSvgPathData()
    {
        var sb = new StringBuilder();
        AppendSvgPathData(sb);
        return sb.ToString();
    }
}

/// <summary>A path segment that moves the current position without drawing.</summary>
public sealed record MoveToSegment(Point Point) : PathSegment
{
    /// <inheritdoc />
    public override string ToSvgPathData() => FormatSvgPathData();

    internal override void AppendSvgPathData(StringBuilder sb) =>
        sb.Append("M ").AppendSvgNumber(Point.X).Append(' ').AppendSvgNumber(Point.Y).Append(' ');
}

/// <summary>A path segment that draws a straight line to the specified point.</summary>
public sealed record LineToSegment(Point Point) : PathSegment
{
    /// <inheritdoc />
    public override string ToSvgPathData() => FormatSvgPathData();

    internal override void AppendSvgPathData(StringBuilder sb) =>
        sb.Append("L ").AppendSvgNumber(Point.X).Append(' ').AppendSvgNumber(Point.Y).Append(' ');
}

/// <summary>A path segment that draws a cubic Bezier curve through two control points to an endpoint.</summary>
public sealed record BezierSegment(Point Control1, Point Control2, Point End) : PathSegment
{
    /// <inheritdoc />
    public override string ToSvgPathData() => FormatSvgPathData();

    internal override void AppendSvgPathData(StringBuilder sb) =>
        sb.Append("C ").AppendSvgNumber(Control1.X).Append(' ').AppendSvgNumber(Control1.Y).Append(' ')
          .AppendSvgNumber(Control2.X).Append(' ').AppendSvgNumber(Control2.Y).Append(' ')
          .AppendSvgNumber(End.X).Append(' ').AppendSvgNumber(End.Y).Append(' ');
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
    public override string ToSvgPathData() => FormatSvgPathData();

    internal override void AppendSvgPathData(StringBuilder sb)
    {
        // The end point is the actual point of the arc's rim at EndAngle. An earlier version wrote the centre here, which made
        // the arc sweep back to the centre and drew petal-shaped wedges in SunburstSeries.
        var end = EndPoint;

        // large-arc-flag: 1 if the angular sweep is greater than 180°.
        char large = Math.Abs(EndAngle - StartAngle) > 180.0 ? '1' : '0';
        // sweep-flag: 1 = clockwise on screen (positive sweep in screen-Y-down), 0 = CCW.
        // The sunburst renderer uses reverse direction for the inner arc (EndAngle < StartAngle)
        // to close the wedge polygon, so we must respect the sign.
        char sweep = EndAngle >= StartAngle ? '1' : '0';

        sb.Append("A ").AppendSvgNumber(RadiusX).Append(' ').AppendSvgNumber(RadiusY).Append(" 0 ").Append(large).Append(' ')
          .Append(sweep).Append(' ').AppendSvgNumber(end.X).Append(' ').AppendSvgNumber(end.Y).Append(' ');
    }

    /// <summary>The point of the arc's rim at its start angle: where the arc begins.</summary>
    internal Point StartPoint => PointAt(StartRadians);

    /// <summary>The point of the arc's rim at its end angle: the one place the end of an arc is computed, for the end point of
    /// its path data, for the last vertex of its sampled outline and for the end of its last curve, so the three never differ
    /// in the last bit.</summary>
    internal Point EndPoint => PointAt(EndRadians);

    /// <summary>The start angle in radians: the one conversion every reader of the arc's start uses.</summary>
    internal double StartRadians => StartAngle.ToRadians();

    /// <summary>The end angle in radians: the one conversion every reader of the arc's end uses.</summary>
    internal double EndRadians => EndAngle.ToRadians();

    /// <summary>The point of the arc's ellipse at an angle, in radians from +X and clockwise on a screen whose Y points down:
    /// the one place the rim of an arc is computed, for its path data, its curves and its sampled outline.</summary>
    /// <param name="radians">The angle, in radians.</param>
    /// <returns>The point of the rim.</returns>
    internal Point PointAt(double radians) =>
        Center.OnRim(RadiusX, RadiusY, radians);
}

/// <summary>A path segment that closes the current sub-path by drawing a line back to its start.</summary>
public sealed record CloseSegment() : PathSegment
{
    /// <inheritdoc />
    public override string ToSvgPathData() => FormatSvgPathData();

    internal override void AppendSvgPathData(StringBuilder sb) => sb.Append("Z ");
}
