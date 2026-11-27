// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text;
using MatPlotLibNet;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Rendering.TextMeasurement;
using MatPlotLibNet.Styling;
using System.Runtime.CompilerServices;

namespace MatPlotLibNet.Rendering.Svg;

/// <summary>An <see cref="IRenderContext"/> implementation that emits SVG markup for each drawing operation.</summary>
/// <remarks>The public entry of the SVG surface: each member hands its work to the type that owns it. The markup, what is asked of the
/// next element, the clips and the paint servers are the element buffer's (the one envelope of every
/// element); the marks are the mark writer's, the texts the text writer's and
/// what is written for the browser the markup writer's. What the draw told the surface is its
/// themed surface. A clip cuts the marks drawn under it, never a text.</remarks>
public sealed partial class SvgRenderContext : IRenderContext, IMarkupRenderContext, IOutlineClipRenderContext, ISurfaceRenderContext,
    ITextClipRenderContext
{
    /// <summary>The markup and the envelope every element is written in.</summary>
    private readonly SvgElementBuffer _buffer;

    /// <summary>The writer of the marks.</summary>
    private readonly SvgMarkWriter _marks;

    /// <summary>The writer of the texts.</summary>
    private readonly SvgTextWriter _texts;

    /// <summary>The writer of what is written for the browser; a field of its own, not read-only: it keeps whether an opacity
    /// wrapper is open.</summary>
    private SvgMarkupWriter _markup;

    /// <summary>What the surface was told: the looks of the theme the marks drawn next are drawn under (their dashes and joins, and how a
    /// text is measured), the math texts of the figure and the opacity of the marks.</summary>
    private ThemedSurface _surface = new();

    /// <inheritdoc />
    ref ThemedSurface ISurfaceRenderContext.Surface => ref _surface;

    /// <summary>Default constructor: a context whose clip, hatch and gradient ids carry no prefix.</summary>
    public SvgRenderContext() : this(default)
    {
    }

    /// <summary>A context whose clip, hatch and gradient ids carry the prefix of <paramref name="ids"/>: the context of a later
    /// subplot of a figure, whose ids must not repeat those of the subplots before it.</summary>
    /// <param name="ids">The scope that names the ids this context writes.</param>
    internal SvgRenderContext(SvgIdScope ids)
    {
        _buffer = new SvgElementBuffer(ids);
        _marks = new SvgMarkWriter(_buffer);
        _texts = new SvgTextWriter(_buffer);
        _markup = new SvgMarkupWriter(_buffer);
    }

    /// <summary>What the figure asks the subplot drawn into this context to write for the browser (<see cref="SvgEmit"/>); none
    /// for a context made outside a figure's draw.</summary>
    internal SvgEmit Emit { get; init; }

    /// <inheritdoc />
    SvgEmit IMarkupRenderContext.Emit => Emit;

    /// <summary>Returns the accumulated SVG markup as a string.</summary>
    public string GetOutput() => _buffer.Markup.ToString();

    /// <summary>Appends the accumulated SVG markup to an existing StringBuilder (avoids extra string allocation).</summary>
    public void WriteTo(StringBuilder target) => target.Append(_buffer.Markup);

    /// <summary>The markup written so far, for the document that is made from it (<see cref="Rendering.Svg.SvgFigureDocument.SvgDocumentParts"/>) to
    /// read without a copy.</summary>
    internal StringBuilder Markup => _buffer.Markup;

    /// <inheritdoc />
    bool ITextClipRenderContext.CutsTexts
    {
        get => _buffer.CutsTexts;
        set => _buffer.CutsTexts = value;
    }

    /// <inheritdoc />
    public void SetNextElementData(string key, string value) => _buffer.SetNextData(key, value);

    /// <inheritdoc />
    public void SetNextTextLength(double width) => _buffer.SetNextTextLength(width);

    /// <summary>Applies a <c>class</c> attribute to the next drawn element: <c>CubeFrame.DrawPanes</c> tags the panes with
    /// <c>class="mpl-pane"</c>, so the script's depth sort skips them.</summary>
    /// <param name="className">The class.</param>
    void IMarkupRenderContext.SetNextElementClass(string className) => _buffer.SetNextClass(className);

    /// <inheritdoc />
    void IMarkupRenderContext.SetNextTextLevel(int? level) => _buffer.SetNextTextLevel(level);

    /// <inheritdoc />
    /// <remarks>The stroke is written by the one writer of a stroke drawn from a colour, so a translucent colour writes its
    /// alpha as <c>stroke-opacity</c>, as the PNG paints it.</remarks>
    public void DrawLine(Point p1, Point p2, StrokeStyle stroke) => _marks.Line(p1, p2, stroke, _surface.Looks.Strokes);

    /// <inheritdoc />
    /// <remarks>The stroke is written as a line's is: a translucent colour writes its alpha as <c>stroke-opacity</c>.</remarks>
    public void DrawLines(IReadOnlyList<Point> points, StrokeStyle stroke) => _marks.Polyline(points, stroke, _surface.Looks.Strokes);

    /// <inheritdoc />
    public void DrawPolygon(IReadOnlyList<Point> points, ShapeStyle shape) =>
        _marks.Shape(new PolygonGeometry(points), shape, _surface.Looks.Strokes);

    /// <inheritdoc />
    public void DrawCircle(Point center, double radius, ShapeStyle shape) =>
        _marks.Shape(new CircleGeometry(center, radius), shape, _surface.Looks.Strokes);

    /// <inheritdoc />
    public void DrawRectangle(Rect rect, ShapeStyle shape) => _marks.Shape(new RectGeometry(rect), shape, _surface.Looks.Strokes);

    /// <inheritdoc />
    public void DrawEllipse(Rect bounds, ShapeStyle shape) => _marks.Shape(new EllipseGeometry(bounds), shape, _surface.Looks.Strokes);

    /// <inheritdoc />
    public void DrawPath(IReadOnlyList<PathSegment> segments, ShapeStyle shape) =>
        _marks.Shape(new PathGeometry(segments), shape, _surface.Looks.Strokes);

    /// <inheritdoc />
    public void DrawText(string text, Point position, Font font, TextAlignment alignment)
        => DrawText(text, position, font, alignment, rotation: 0);

    /// <inheritdoc />
    /// <remarks>Written outside every clip pushed: a clip cuts marks, never a text. With a glyph source
    /// installed (the Skia package installs one when it loads) the text is its glyph outline, else a <c>&lt;text&gt;</c> element.</remarks>
    public void DrawText(string text, Point position, Font font, TextAlignment alignment, double rotation) =>
        _texts.Write(text, position, font, alignment, rotation);

    /// <inheritdoc />
    public void PushClip(Rect clipRect) => _buffer.PushClip(new RectGeometry(clipRect));

    /// <inheritdoc />
    void IOutlineClipRenderContext.PushClip(IReadOnlyList<PathSegment> outline) => _buffer.PushClip(new PathGeometry(outline));

    /// <inheritdoc />
    public void PopClip() => _buffer.PopClip();

    /// <inheritdoc />
    /// <remarks>The set of the looks the surface was told while the installed glyph source outlines text from the package's own fonts
    /// (<see cref="Rendering.Svg.SvgRenderContext.SvgTextWriter.OutlinesInPackageFaces"/>, which carry every face of every set); the library's own while text is written
    /// for the viewer's fonts, which may hold none of them.</remarks>
    MathFontSet IThemedRenderContext.MathFonts => _surface.MathFontsFor(inPackageFonts: SvgTextWriter.OutlinesInPackageFaces);

    /// <inheritdoc />
    public Size MeasureText(string text, Font font) => FontMetricsDefault.Current.Measure(text, font);

    /// <inheritdoc />
    public void DrawRichText(RichText richText, Point position, Font font, TextAlignment alignment)
        => DrawRichText(richText, position, font, alignment, rotation: 0);

    /// <inheritdoc />
    /// <remarks>Laid out once in core and drawn by the one verb of a text: each run as a text (its glyph outline when the Skia package's
    /// glyph source is installed), each rule as a rectangle.</remarks>
    public void DrawRichText(RichText richText, Point position, Font font, TextAlignment alignment, double rotation) =>
        this.Draw(TextLabel.OfRich(richText), font, new TextPlacement(position, alignment) { Rotation = rotation });

    /// <inheritdoc />
    /// <remarks>SVG wraps what is drawn next in a group of the opacity held; full opacity
    /// closes the wrapper and opens none.</remarks>
    public void SetOpacity(double opacity) => _markup.WrapInOpacity(_surface.HoldOpacity(opacity));

    /// <summary>Opens an SVG group element with a CSS class attribute, outside every clip.</summary>
    public void BeginGroup(string cssClass) => _markup.BeginGroup(cssClass);

    /// <summary>Opens an SVG group with a CSS class and <c>aria-label</c> for accessibility.</summary>
    /// <param name="cssClass">The group's class.</param>
    /// <param name="ariaLabel">The group's accessible name.</param>
    void IMarkupRenderContext.BeginAccessibleGroup(string cssClass, string ariaLabel) => _markup.BeginAccessibleGroup(cssClass, ariaLabel);

    /// <summary>Closes the current SVG group element. The one writer of a group's end tag: an opacity wrapper, a 3-D subgroup and a
    /// tooltip group close through it; a group drawn in a clip is closed first.</summary>
    public void EndGroup() => _markup.EndGroup();

    /// <summary>Opens an SVG hyperlink: <c>&lt;a href="…" style="cursor:pointer" aria-label="…"&gt;</c>.
    /// <c>href</c> (SVG 2), not <c>xlink:href</c>: every browser the library targets resolves it, inline in HTML
    /// and as a standalone file alike, and it needs no extra namespace on the root.</summary>
    public void BeginHyperlink(string url, string? ariaLabel, bool? expanded = null) => _markup.BeginHyperlink(url, ariaLabel, expanded);

    /// <summary>Closes the current SVG hyperlink.</summary>
    public void EndHyperlink() => _markup.EndHyperlink();

    /// <summary>Text carrying its <c>aria-level</c> — a tree grid's row depth (ARIA <c>treegrid</c>), which is
    /// what makes an indented list navigable rather than merely visible.</summary>
    public void DrawTextWithLevel(string text, Point position, Font font, int level)
    {
        _buffer.SetNextTextLevel(level);
        DrawText(text, position, font, TextAlignment.Left);
        _buffer.SetNextTextLevel(null);
    }

    /// <inheritdoc />
    /// <remarks>With interactive attributes the group carries the series' slot and, with the data cursor on, its mapping; without them
    /// a labelled series gets an accessible group and an unlabelled one none.</remarks>
    bool IMarkupRenderContext.OpenSeriesGroup(SvgEmit emit, string? label, int slotIndex, DataTransform? drawnWith) =>
        _markup.OpenSeriesGroup(emit, label, slotIndex, drawnWith);

    /// <inheritdoc />
    void IMarkupRenderContext.OpenPatchGroup(int index, string label) => _markup.OpenPatchGroup(index, label);

    /// <inheritdoc />
    void IMarkupRenderContext.WriteAxesView(int axesIndex, Rect plotArea, DataTransform? drawnWith,
        IReadOnlyList<SecondaryAxisTransform>? secondary) => _markup.WriteAxesView(axesIndex, plotArea, drawnWith, secondary);

    /// <inheritdoc />
    /// <returns><see langword="true"/>: the scene is always opened here.</returns>
    bool IMarkupRenderContext.Begin3DSceneGroup(double elevation, double azimuth, double? distance, Rect plotBounds,
        CubeFaceSelection faces)
    {
        _markup.Begin3DSceneGroup(elevation, azimuth, distance, plotBounds, faces);
        return true;
    }

    /// <inheritdoc />
    void IMarkupRenderContext.Begin3DSubgroup(string className) => _markup.Begin3DSubgroup(className);

    /// <inheritdoc />
    void IMarkupRenderContext.End3DSubgroup() => EndGroup();

    /// <inheritdoc />
    /// <returns><see langword="true"/>: the group is always opened here.</returns>
    bool IMarkupRenderContext.BeginLegendItemGroup(int legendIndex, string? ariaLabel)
    {
        _markup.BeginLegendItemGroup(legendIndex, ariaLabel);
        return true;
    }

    /// <inheritdoc />
    /// <remarks>Browsers display <c>&lt;title&gt;</c> content as a tooltip when hovering over any child element.</remarks>
    void IMarkupRenderContext.BeginTooltipGroup(string tooltipText) => _markup.BeginTooltipGroup(tooltipText);

    /// <inheritdoc />
    void IMarkupRenderContext.EndTooltipGroup() => EndGroup();

    /// <summary>The texts of an SVG drawing surface. With a glyph source installed (the Skia package installs one when it loads) a text is
    /// written as its glyph outline, which every viewer draws alike and which matches the PNG, both being set in the same faces; without one
    /// it is a <c>&lt;text&gt;</c> element the browser sets in whichever face it finds for the family. Either is one element of the buffer's
    /// envelope, written as a text (<see cref="SvgElementBuffer.WriteText{TElement}"/>): outside every clip, as a clip cuts marks and not a
    /// text, except the texts a series has the surface cut (a contour's labels). A value over the buffer, held by the surface, so it allocates
    /// nothing.</summary>
    /// <param name="buffer">The buffer the texts are written into.</param>
    internal readonly struct SvgTextWriter(SvgElementBuffer buffer)
    {
        /// <summary>Whether the installed glyph source outlines text in the package's own faces (a shaping one: the Skia package's): the one
        /// test of whether a rich text is outlined, and so set in the faces of the math font set of the looks.</summary>
        internal static bool OutlinesInPackageFaces => GlyphPathProviderDefault.Current is IShapedGlyphPathProvider;

        /// <summary>Writes a text whose <paramref name="alignment"/> edge stands at <paramref name="position"/>, turned
        /// <paramref name="rotation"/> degrees counter-clockwise about it.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Write(string text, Point position, Font font, TextAlignment alignment, double rotation)
        {
            if (GlyphPathProviderDefault.Current?.OutlineOf(text, font, alignment) is { } outline)
            {
                buffer.WriteText(new OutlineElement(outline, position, font, alignment.StartOffset(outline.Advance), rotation));
                return;
            }

            buffer.WriteText(new TextElement(text, position, font, alignment, rotation));
        }

        /// <summary>The fill of a text, when its font names a colour: the hex colour and, when the colour is translucent, its alpha as
        /// <c>fill-opacity</c> (<see cref="SvgWriterExtensions.AppendAlpha"/>), as Skia paints the text. The one writer of a text's fill for
        /// the <c>&lt;text&gt;</c> element and the glyph outline.</summary>
        private static void AppendFill(StringBuilder markup, Font font)
        {
            if (font.Color is { } color)
            {
                markup.Append(" fill=\"").AppendHex(color).Append('"').AppendAlpha("fill-opacity", color);
            }
        }

        /// <summary>The <c>text-anchor</c> keyword for an alignment. The alignment is geometric (Left means the left edge) while SVG's
        /// <c>start</c> and <c>end</c> follow the reading direction, so under <c>direction="rtl"</c> the two swap.</summary>
        private static string AnchorOf(TextAlignment alignment, bool rightToLeft) => alignment switch
        {
            TextAlignment.Center => "middle",
            TextAlignment.Right  => rightToLeft ? "start" : "end",
            _                    => rightToLeft ? "end" : "start",
        };

        /// <summary>A <c>&lt;text&gt;</c> element: position, family and size, the anchor, the direction of a right-to-left text (its first
        /// strong character is Hebrew, Arabic or another right-to-left script, rules P2 and P3 of the bidi algorithm: the browser reorders the
        /// characters on its own, but not the paragraph direction, which decides where the punctuation at the end of a Hebrew title lands),
        /// the turn about the position, italic, bold, the fill, and the width asked of the text (it fits the glyphs to it).</summary>
        private readonly record struct TextElement(string Text, Point Position, Font Font, TextAlignment Alignment, double Rotation) : ISvgElement
        {
            public void WriteOpening(SvgElementBuffer buffer)
            {
                var markup = buffer.Markup;
                bool rightToLeft = MatPlotLibNet.Rendering.Text.BidiAlgorithm.IsRightToLeftParagraph(Text);
                markup.Append("<text x=\"").AppendSvgNumber(Position.X).Append("\" y=\"").AppendSvgNumber(Position.Y)
                      .Append("\" font-family=\"").Append(Font.Family).Append("\" font-size=\"").AppendSvgNumber(Font.Size)
                      .Append("\" text-anchor=\"").Append(AnchorOf(Alignment, rightToLeft)).Append('"');
                if (rightToLeft) markup.Append(" direction=\"rtl\"");
                if (Rotation != 0)
                {
                    // SVG turns clockwise, the library counter-clockwise (the y axis is flipped against the mathematical one).
                    markup.Append(" transform=\"rotate(").AppendSvgNumber(Rotation.ToClockwise()).Append(',')
                          .AppendSvgNumber(Position.X).Append(',').AppendSvgNumber(Position.Y).Append(")\"");
                }
                if (Font.IsItalic) markup.Append(" font-style=\"italic\"");
                if (Font.IsBold) markup.Append(" font-weight=\"bold\"");
                AppendFill(markup, Font);
                if (buffer.TakeTextLength() is { } length)
                {
                    markup.Append(" textLength=\"").AppendSvgNumber(length).Append("\" lengthAdjust=\"spacingAndGlyphs\"");
                }
            }

            public void WriteClosing(StringBuilder markup) => markup.Append('>').Append(Text.EscapeForXml()).AppendLineLf("</text>");
        }

        /// <summary>A text's glyph outline, started where its alignment puts it by the outline's own advance
        /// (<see cref="GlyphPathProviderExtensions.OutlineOf"/>), so the text is not measured again here. The transform is applied right to
        /// left: the glyph origin moves by the alignment offset along the unturned baseline, turns about the anchor, then moves to the anchor,
        /// which is right for a turned text of any alignment (a Y-axis label turned 90 degrees and centred).</summary>
        private readonly record struct OutlineElement(ShapedOutline Outline, Point Position, Font Font, double AlignOffset, double Rotation) : ISvgElement
        {
            public void WriteOpening(SvgElementBuffer buffer)
            {
                // An outline has no font a viewer could substitute, so there is no width to fit it to: the width asked is dropped.
                _ = buffer.TakeTextLength();
                var markup = buffer.Markup;
                markup.Append("<path d=\"").Append(Outline.PathData).Append('"');
                AppendFill(markup, Font);
                markup.Append(" transform=\"translate(").AppendSvgNumber(Position.X).Append(',').AppendSvgNumber(Position.Y).Append(')');
                if (Rotation != 0)
                    markup.Append(" rotate(").AppendSvgNumber(Rotation.ToClockwise()).Append(')');
                if (AlignOffset != 0)
                    markup.Append(" translate(").AppendSvgNumber(AlignOffset).Append(",0)");
                markup.Append('"');
            }

            public void WriteClosing(StringBuilder markup) => markup.AppendLineLf(" />");
        }
    }

    /// <summary>The marks of an SVG drawing surface: a line, a polyline and a shape over its geometry, each one element of the buffer's
    /// envelope (<see cref="SvgElementBuffer.WriteMark{TMark}"/>), stroked by the one writer of a stroke and joined by the theme's looks. A
    /// value over the buffer, held by the surface, so it allocates nothing; its members are inlined into the surface's, so a mark costs the
    /// calls it cost before the surface was split.</summary>
    /// <param name="buffer">The buffer the marks are written into.</param>
    internal readonly struct SvgMarkWriter(SvgElementBuffer buffer)
    {
        /// <summary>Writes a line between two points. Two points have no corner, so it writes no join.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Line(Point p1, Point p2, StrokeStyle stroke, StrokeLook looks) =>
            buffer.WriteMark(new LineElement(p1, p2, StrokeOf(stroke, 2, looks), looks.Dashes));

        /// <summary>Writes a polyline through <paramref name="points"/>, or nothing when they form no line. A point the transform could
        /// not place (a non-positive value on a log axis, a value inside an axis break) is left out of the polyline, never written as the
        /// literal <c>NaN</c> that breaks the whole element: matplotlib's <c>nonpositive='mask'</c>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Polyline(IReadOnlyList<Point> points, StrokeStyle stroke, StrokeLook looks)
        {
            if (!points.FormsALine) return;
            buffer.WriteMark(new PolylineElement(points, StrokeOf(stroke, points.Count, looks), looks.Dashes));
        }

        /// <summary>Writes a shape: one with both a gradient and a hatch in two layers (the gradient under the hatch,
        /// <see cref="ShapeStyleLayers"/>), any other as one element filled by its paint server.</summary>
        /// <typeparam name="TGeometry">The kind of shape: polygon, circle, rectangle, ellipse or path.</typeparam>
        /// <param name="geometry">Where the shape stands.</param>
        /// <param name="shape">The fill, stroke, gradient and hatch it is painted with; read where it stands, not copied.</param>
        /// <param name="looks">The stroke looks of the theme it is drawn under.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Shape<TGeometry>(TGeometry geometry, in ShapeStyle shape, StrokeLook looks) where TGeometry : struct, ISvgGeometry
        {
            if (shape.HasVisibleGradient && shape.HasVisibleHatch)
            {
                InTwoLayers(geometry, shape, looks);
                return;
            }

            buffer.WriteMark(new ShapeElement<TGeometry>(geometry, shape, looks.Dashes));
        }

        /// <summary>Writes a shape with both a gradient and a hatch as two: the gradient under the hatch.</summary>
        private void InTwoLayers<TGeometry>(TGeometry geometry, in ShapeStyle shape, StrokeLook looks) where TGeometry : struct, ISvgGeometry
        {
            Shape(geometry, shape.UnderTheHatch, looks);
            Shape(geometry, shape.HatchLayer, looks);
        }

        /// <summary>The stroke of a line of <paramref name="points"/> points: joined by the look the surface was told
        /// (<see cref="StrokeLook.JoinOf"/>), the one place a line of any kind is joined. A stroke of fewer than three points has no corner
        /// to join, so it writes no join.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static StrokeStyle StrokeOf(StrokeStyle stroke, int points, StrokeLook looks) =>
            stroke with { Join = points < 3 ? JoinStyle.Miter : looks.JoinOf(stroke) };

        /// <summary>A <c>&lt;line&gt;</c>.</summary>
        private readonly record struct LineElement(Point P1, Point P2, StrokeStyle Stroke, DashLook Dashes) : ISvgElement
        {
            public void WriteOpening(SvgElementBuffer buffer) =>
                buffer.Markup.Append("<line x1=\"").AppendSvgNumber(P1.X).Append("\" y1=\"").AppendSvgNumber(P1.Y)
                    .Append("\" x2=\"").AppendSvgNumber(P2.X).Append("\" y2=\"").AppendSvgNumber(P2.Y).Append('"')
                    .AppendStroke(Stroke, Dashes);

            public void WriteClosing(StringBuilder markup) => markup.AppendLineLf(" />");
        }

        /// <summary>A <c>&lt;polyline&gt;</c> through the points that can be placed.</summary>
        private readonly record struct PolylineElement(IReadOnlyList<Point> Points, StrokeStyle Stroke, DashLook Dashes) : ISvgElement
        {
            public void WriteOpening(SvgElementBuffer buffer)
            {
                var markup = buffer.Markup;
                int written = 0;
                markup.Append("<polyline points=\"");
                for (int i = 0; i < Points.Count; i++)
                {
                    if (double.IsNaN(Points[i].X) || double.IsNaN(Points[i].Y)) continue;
                    if (written++ > 0) markup.Append(' ');
                    markup.AppendSvgPoint(Points[i]);
                }
                markup.Append("\" fill=\"none\"").AppendStroke(Stroke, Dashes);
            }

            public void WriteClosing(StringBuilder markup) => markup.AppendLineLf(" />");
        }

        /// <summary>A shape filled and stroked: its paint server is defined before its tag. A circle and an ellipse have no corner for a
        /// join to show on, so they write none, as a line of two points writes none. The style is held in a field and read where it stands
        /// (a style is the largest value a mark carries).</summary>
        private readonly struct ShapeElement<TGeometry>(TGeometry geometry, in ShapeStyle shape, DashLook dashes) : ISvgElement
            where TGeometry : struct, ISvgGeometry
        {
            private readonly TGeometry _geometry = geometry;
            private readonly ShapeStyle _shape = shape;
            private readonly DashLook _dashes = dashes;

            public void WriteOpening(SvgElementBuffer buffer)
            {
                string? paintServer = buffer.PaintServerOf(in _shape);
                _geometry.WriteGeometry(buffer.Markup);
                if (_geometry.HasCorners || _shape.StrokeJoin == JoinStyle.Miter)
                    buffer.Markup.AppendFillStroke(in _shape, _dashes, paintServer);
                else
                    buffer.Markup.AppendFillStroke(_shape with { StrokeJoin = JoinStyle.Miter }, _dashes, paintServer);
            }

            public void WriteClosing(StringBuilder markup) => markup.AppendLineLf(" />");
        }
    }
}
