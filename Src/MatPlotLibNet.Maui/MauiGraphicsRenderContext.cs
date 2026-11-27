// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Microsoft.Maui.Graphics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;
using MplFont = MatPlotLibNet.Styling.Font;
using MplPoint = MatPlotLibNet.Rendering.Point;
using MplSize = MatPlotLibNet.Rendering.Size;
using MplRect = MatPlotLibNet.Rendering.Rect;
using MplColor = MatPlotLibNet.Styling.Color;

namespace MatPlotLibNet.Maui;

/// <summary>An <see cref="IRenderContext"/> implementation that draws onto a .NET MAUI <see cref="ICanvas"/>.</summary>
/// <remarks>A clip cuts the marks drawn under it, never a text: the canvas state is restored past the clip
/// before a text and clipped again before the next mark; a text a series has the surface cut (a contour's label) is drawn under the clip.</remarks>
public sealed class MauiGraphicsRenderContext : IRenderContext, IOutlineClipRenderContext, ISurfaceRenderContext, ITextClipRenderContext,
    IClipSurface<MauiGraphicsRenderContext.Clip>
{
    private readonly ICanvas _canvas;

    /// <summary>The clips pushed on this canvas and how many are applied.</summary>
    private MarkClips<Clip> _clips;

    /// <summary>What the surface was told: the looks of the theme the marks drawn next are drawn under, the math texts of the figure and
    /// the opacity set last (<see cref="SetOpacity"/>), which a restored canvas state would otherwise take back.</summary>
    private ThemedSurface _surface = new();

    /// <inheritdoc />
    ref ThemedSurface ISurfaceRenderContext.Surface => ref _surface;

    /// <summary>The alpha the canvas is set to: the opacity the surface holds, as the canvas takes it.</summary>
    private float Alpha => (float)_surface.Opacity;

    /// <summary>A clip as the canvas cuts with it: a rectangle, or the outline of a plot that is not one, kept to cut with again after a text.</summary>
    /// <param name="Rect">The rectangle, when <paramref name="Outline"/> is <see langword="null"/>.</param>
    /// <param name="Outline">The outline, cut by the nonzero rule.</param>
    internal readonly record struct Clip(RectF Rect, PathF? Outline);

    /// <inheritdoc />
    void IClipSurface<Clip>.Apply(Clip clip)
    {
        _canvas.SaveState();
        if (clip.Outline is { } outline) _canvas.ClipPath(outline, WindingMode.NonZero);
        else _canvas.ClipRectangle(clip.Rect.X, clip.Rect.Y, clip.Rect.Width, clip.Rect.Height);
    }

    /// <inheritdoc />
    /// <remarks>The opacity set under the clip outlives it: the canvas' alpha is part of the state restored.</remarks>
    void IClipSurface<Clip>.Lift()
    {
        _canvas.RestoreState();
        _canvas.Alpha = Alpha;
    }

    /// <summary>Initializes a new instance wrapping the specified MAUI canvas.</summary>
    public MauiGraphicsRenderContext(ICanvas canvas)
    {
        _canvas = canvas;
    }

    /// <inheritdoc />
    public void DrawLine(MplPoint p1, MplPoint p2, StrokeStyle stroke)
    {
        _clips.Mark(this);
        ApplyStroke(stroke);
        _canvas.DrawLine((float)p1.X, (float)p1.Y, (float)p2.X, (float)p2.Y);
    }

    /// <inheritdoc />
    public void DrawLines(IReadOnlyList<MplPoint> points, StrokeStyle stroke)
    {
        if (!points.FormsALine) return;
        _clips.Mark(this);
        ApplyStroke(stroke);

        var sink = new MauiPathSink(new PathF());
        points.WalkPolyline(ref sink, closed: false);

        _canvas.DrawPath(sink.Path);
    }

    /// <inheritdoc />
    public void DrawPolygon(IReadOnlyList<MplPoint> points, ShapeStyle shape)
    {
        var sink = new MauiPathSink(new PathF());
        points.WalkPolyline(ref sink, closed: true);

        PaintShape(shape, new PathShape(sink.Path));
    }

    /// <inheritdoc />
    public void DrawCircle(MplPoint center, double radius, ShapeStyle shape) => PaintShape(shape, new CircleShape(center, radius));

    /// <inheritdoc />
    public void DrawRectangle(MplRect rect, ShapeStyle shape) => PaintShape(shape, new RectShape(rect));

    /// <inheritdoc />
    public void DrawEllipse(MplRect bounds, ShapeStyle shape) => PaintShape(shape, new EllipseShape(bounds));

    /// <inheritdoc />
    /// <remarks>Drawn past every clip pushed: a clip cuts marks, never a text, but the texts a series has the surface cut, a contour's
    /// labels. Drawn in the face of the font (bold, italic) and its colour (black when it names none),
    /// started where its alignment puts it by the library's width.</remarks>
    public void DrawText(string text, MplPoint position, MplFont font, MatPlotLibNet.Rendering.TextAlignment alignment)
    {
        _clips.Text(this);
        _canvas.Font = ToMauiFont(font);
        _canvas.FontSize = (float)font.Size;
        // The canvas keeps its font colour between calls: a font that names none is black, as the PNG surface draws it, not the
        // colour of the text before it.
        _canvas.FontColor = ToMauiColor((font.Color ?? MatPlotLibNet.Styling.Colors.Black));

        // The string starts where the alignment puts it by the width the layout measured it at (as the other surfaces start it), not where
        // the canvas would put it by the width of its own fonts, which is not the width the layout left room for.
        float start = (float)(position.X + alignment.StartOffset(MeasureText(text, font).Width));
        _canvas.DrawString(text, start, (float)position.Y, HorizontalAlignment.Left);
    }

    /// <inheritdoc />
    /// <remarks>The canvas is turned about the anchor for this text only and turned back after it. A positive rotation turns the
    /// text counter-clockwise, as in the SVG and Skia output; the MAUI canvas turns clockwise, so it is given the opposite angle.</remarks>
    public void DrawText(string text, MplPoint position, MplFont font, MatPlotLibNet.Rendering.TextAlignment alignment, double rotation)
    {
        _clips.Text(this);
        if (rotation == 0)
        {
            DrawText(text, position, font, alignment);
            return;
        }

        _canvas.SaveState();
        try
        {
            _canvas.Rotate((float)rotation.ToClockwise(), (float)position.X, (float)position.Y);
            DrawText(text, position, font, alignment);
        }
        finally
        {
            _canvas.RestoreState();
        }
    }

    /// <inheritdoc />
    /// <remarks>Laid out once in core and drawn by the one verb of a text: each run through
    /// <see cref="DrawText(string, MplPoint, MplFont, MatPlotLibNet.Rendering.TextAlignment, double)"/>, each rule as a rectangle, so a
    /// fraction is stacked and a script raised on this canvas too.</remarks>
    public void DrawRichText(MatPlotLibNet.Rendering.MathText.RichText richText, MplPoint position, MplFont font,
        MatPlotLibNet.Rendering.TextAlignment alignment, double rotation) =>
        MatPlotLibNet.Rendering.MathText.MathTextRenderContextExtensions.Draw(this, MatPlotLibNet.Rendering.MathText.TextLabel.OfRich(richText), font,
            new MatPlotLibNet.Rendering.TextPlacement(position, alignment) { Rotation = rotation });

    /// <inheritdoc />
    public void DrawPath(IReadOnlyList<PathSegment> segments, ShapeStyle shape) => PaintShape(shape, new PathShape(PathOf(segments)));

    /// <summary>The canvas path of <paramref name="segments"/>: the one conversion a drawn path and a clip outline share.</summary>
    private static PathF PathOf(IReadOnlyList<PathSegment> segments)
    {
        var sink = new MauiPathSink(new PathF());
        segments.WalkInto(ref sink);
        return sink.Path;
    }

    /// <inheritdoc />
    public void PushClip(MplRect clipRect) => _clips.Push(this, new Clip(ToRectF(clipRect), null));

    /// <inheritdoc />
    void IOutlineClipRenderContext.PushClip(IReadOnlyList<PathSegment> outline) => _clips.Push(this, new Clip(default, PathOf(outline)));

    /// <inheritdoc />
    public void PopClip() => _clips.Pop(this);

    /// <inheritdoc />
    bool ITextClipRenderContext.CutsTexts
    {
        get => _clips.CutsTexts;
        set => _clips.CutsTexts = value;
    }

    /// <inheritdoc />
    /// <remarks>The library's own metrics, the ones the layout measured every label with: a canvas that measured with
    /// a width of its own would place text the layout had left room of another width for.</remarks>
    public MplSize MeasureText(string text, MplFont font) => FontMetricsDefault.Current.Measure(text, font);

    /// <inheritdoc />
    public void SetOpacity(double opacity)
    {
        _surface.HoldOpacity(opacity);
        _canvas.Alpha = Alpha;
    }

    /// <summary>Sets the canvas' stroke: its colour, its thickness, the dash pattern of its line style under the theme's look and the join of its corners. The one
    /// place a line, a polyline and the outline of a shape are stroked from. The canvas keeps its stroke state between calls, so the
    /// pattern is set every time: a solid shape drawn after a dashed line gets no dashes.</summary>
    /// <param name="stroke">The colour, width, dash style and join of the stroke.</param>
    private void ApplyStroke(StrokeStyle stroke)
    {
        _canvas.StrokeColor = ToMauiColor(stroke.Color);
        _canvas.StrokeSize = (float)stroke.Thickness;
        _canvas.StrokeDashPattern = ToMauiDashPattern(stroke.Style, stroke.Thickness, _surface.Looks.Strokes.Dashes);
        _canvas.StrokeLineJoin = ToMaui(_surface.Looks.Strokes.JoinOf(stroke));
    }

    /// <inheritdoc />
    /// <remarks>The library's own (math in the label's face): the canvas draws with the fonts the platform holds, which hold none of the
    /// faces of the other sets (Computer Modern, DejaVu Sans Oblique), and a run measured in a face it then draws in another would stand
    /// where another glyph should.</remarks>
    MatPlotLibNet.Rendering.MathText.MathFontSet IThemedRenderContext.MathFonts => _surface.MathFontsFor(inPackageFonts: false);

    /// <summary>Paints <paramref name="geometry"/> in <paramref name="shape"/>, cut by the clips pushed: the one sequence of a shape on a
    /// canvas (<see cref="CanvasShapeExtensions.PaintIn"/>: the fill, the hatch over it, the outline, each when it shows). The geometry is
    /// a value the method is made for (<typeparamref name="TShape"/>), not a delegate, so a shape allocates nothing beyond its paints.</summary>
    /// <typeparam name="TShape">The kind of geometry: a path, a circle, a rectangle or an ellipse.</typeparam>
    /// <param name="shape">The fill, the hatch and the outline of the shape.</param>
    /// <param name="geometry">The geometry, painted once for each layer.</param>
    private void PaintShape<TShape>(in ShapeStyle shape, in TShape geometry) where TShape : struct, IMauiShape
    {
        _clips.Mark(this);
        new Layers<TShape>(this, geometry).PaintIn(shape);
    }

    /// <summary>The layers of one shape on this canvas. The geometry paints its outline with whatever paint is set, so it is painted once
    /// for each layer. The canvas keeps its antialiasing between calls: a layer sets it for its own paint (none for an aliased fill,
    /// <see cref="ShapeStyle.Aliased"/>) and puts it back, so the next mark is antialiased.</summary>
    /// <typeparam name="TShape">The kind of geometry.</typeparam>
    /// <param name="surface">The surface whose canvas the layers are painted on.</param>
    /// <param name="geometry">The shape: its bounding box (a gradient's ends and the origin of a pattern are relative to it) and its outline.</param>
    private readonly struct Layers<TShape>(MauiGraphicsRenderContext surface, TShape geometry) : ICanvasShape where TShape : struct, IMauiShape
    {
        /// <inheritdoc />
        /// <remarks>The flat fill, with the gradient over it as the fill paint when the style has one.</remarks>
        public void Fill(in ShapeStyle style)
        {
            var canvas = surface._canvas;
            var bounds = geometry.Bounds;
            canvas.FillColor = ToMauiColor(style.Fill!.Value);

            if (style.HasVisibleGradient && bounds.Width > 0 && bounds.Height > 0)
            {
                canvas.SetFillPaint(GradientPaintOf(style.Gradient!.Value, bounds), bounds);
            }

            canvas.Antialias = !style.Aliased;
            geometry.Fill(canvas);
            canvas.Antialias = true;
        }

        /// <inheritdoc />
        /// <remarks>The tile of the hatch as the fill paint, from the shape's bounding box.</remarks>
        public void Hatch(in ShapeStyle style)
        {
            var canvas = surface._canvas;
            canvas.Antialias = !style.Aliased;
            canvas.SetFillPaint(new PatternPaint { Pattern = new HatchTilePattern(style.Hatch, ToMauiColor(style.HatchStroke)) }, geometry.Bounds);
            geometry.Fill(canvas);
            canvas.Antialias = true;
        }

        /// <inheritdoc />
        public void Stroke(StrokeStyle outline)
        {
            surface.ApplyStroke(outline);
            geometry.Stroke(surface._canvas);
        }
    }

    /// <summary>The geometry of one shape, painted onto a canvas.</summary>
    private interface IMauiShape
    {
        /// <summary>The shape's bounding box, as the canvas takes a rectangle.</summary>
        RectF Bounds { get; }

        /// <summary>Paints the shape's outline with the canvas' current fill paint.</summary>
        void Fill(ICanvas canvas);

        /// <summary>Draws the shape's outline with the canvas' current stroke.</summary>
        void Stroke(ICanvas canvas);
    }

    /// <summary>A polygon or a path, already built.</summary>
    private readonly record struct PathShape(PathF Path) : IMauiShape
    {
        public RectF Bounds => Path.Bounds;

        public void Fill(ICanvas canvas) => canvas.FillPath(Path);

        public void Stroke(ICanvas canvas) => canvas.DrawPath(Path);
    }

    /// <summary>A circle around its centre.</summary>
    private readonly record struct CircleShape(MplPoint Center, double Radius) : IMauiShape
    {
        public RectF Bounds => ToRectF(new MplRect(Center.X - Radius, Center.Y - Radius, 2 * Radius, 2 * Radius));

        public void Fill(ICanvas canvas) => canvas.FillCircle((float)Center.X, (float)Center.Y, (float)Radius);

        public void Stroke(ICanvas canvas) => canvas.DrawCircle((float)Center.X, (float)Center.Y, (float)Radius);
    }

    /// <summary>A rectangle.</summary>
    private readonly record struct RectShape(MplRect Rect) : IMauiShape
    {
        public RectF Bounds => ToRectF(Rect);

        public void Fill(ICanvas canvas) => canvas.FillRectangle(Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height);

        public void Stroke(ICanvas canvas) => canvas.DrawRectangle(Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height);
    }

    /// <summary>An ellipse inscribed in its bounds.</summary>
    private readonly record struct EllipseShape(MplRect Box) : IMauiShape
    {
        public RectF Bounds => ToRectF(Box);

        public void Fill(ICanvas canvas) => canvas.FillEllipse(Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height);

        public void Stroke(ICanvas canvas) => canvas.DrawEllipse(Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height);
    }

    /// <summary>A linear gradient paint for <paramref name="gradient"/>: the canvas takes its two ends as fractions of the
    /// rectangle it is set for, and the style gives them in the units of the picture.</summary>
    private static LinearGradientPaint GradientPaintOf(LinearGradient gradient, RectF bounds) => new()
    {
        StartColor = ToMauiColor(gradient.StartColor),
        EndColor = ToMauiColor(gradient.EndColor),
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

    private static Microsoft.Maui.Graphics.Color ToMauiColor(MplColor color) =>
        new(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);

    /// <summary>The canvas' face for the font: the platform's default family (the family of the library's font is a list of names for
    /// a browser, which a canvas cannot read), bold when the font is (<see cref="FontStyleExtensions"/>), italic when it is.</summary>
    /// <returns>The canvas' font.</returns>
    /// <param name="font">The <see cref="MplFont"/> it works on.</param>
    private static Microsoft.Maui.Graphics.Font ToMauiFont(MplFont font) => new(
        Microsoft.Maui.Graphics.Font.Default.Name,
        font.IsBold ? FontWeights.Bold : FontWeights.Regular,
        font.IsItalic ? FontStyleType.Italic : FontStyleType.Normal);

    /// <summary>The canvas' join: round and bevel by name, anything else a mitre.</summary>
    /// <returns>The canvas' join.</returns>
    /// <param name="join">The <see cref="JoinStyle"/> it works on.</param>
    internal static LineJoin ToMaui(JoinStyle join) => join switch
    {
        JoinStyle.Round => LineJoin.Round,
        JoinStyle.Bevel => LineJoin.Bevel,
        _ => LineJoin.Miter,
    };

    /// <summary>The dashes of a line of this style as they are set on <c>ICanvas.StrokeDashPattern</c>. A MAUI canvas multiplies every
    /// value by the stroke size, so the pattern is the lengths the theme's look gives the line
    /// (<see cref="DashPatterns.Lengths"/>, the lengths the SVG and the PNG of the same figure have) divided by the width.</summary>
    /// <param name="thickness">The stroke size the canvas is set to, in pixels.</param>
    /// <param name="look">The dash look of the theme the line is drawn under.</param>
    /// <returns>The pattern; empty for a solid line, for no line and for a stroke of no width.</returns>
    /// <param name="style">The <see cref="LineStyle"/> it works on.</param>
    private static float[] ToMauiDashPattern(LineStyle style, double thickness, DashLook look)
    {
        var dashes = DashPatterns.Lengths(style, thickness, look);
        if (dashes.Count == 0 || thickness <= 0) return [];
        var pattern = new float[dashes.Count];
        for (int i = 0; i < pattern.Length; i++) pattern[i] = (float)(dashes[i] / thickness);
        return pattern;
    }

    /// <summary>The rectangle at <c>(X, Y)</c> of size <c>(Width, Height)</c>, in the floats a canvas takes.</summary>
    /// <returns>The canvas' rectangle.</returns>
    /// <param name="rect">The <see cref="MplRect"/> it works on.</param>
    internal static RectF ToRectF(MplRect rect) => new RectF((float)rect.X, (float)rect.Y, (float)rect.Width, (float)rect.Height);
}
