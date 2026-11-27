// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Styling;
using MatPlotLibNet;
using SkiaSharp;

namespace MatPlotLibNet.Skia;

/// <summary>Render context implementation using SkiaSharp for raster and PDF output.</summary>
/// <remarks>A clip cuts the marks drawn under it, never a text: the canvas is restored past the clip
/// before a text and clipped again before the next mark.</remarks>
public sealed partial class SkiaRenderContext : IRenderContext, IPixelGridRenderContext, IOutlineClipRenderContext, ISurfaceRenderContext,
    ITextClipRenderContext, IClipSurface<SkiaRenderContext.Clip>
{
    private readonly SKCanvas _canvas;

    /// <summary>The clips pushed on this canvas and how many are applied.</summary>
    private MarkClips<Clip> _clips;

    /// <summary>A clip as the canvas cuts with it: a rectangle, or the outline of a plot that is not one (kept until the clip is popped,
    /// to cut with it again after a text).</summary>
    /// <param name="Rect">The rectangle, when <paramref name="Outline"/> is <see langword="null"/>.</param>
    /// <param name="Outline">The outline, cut with antialiasing.</param>
    internal readonly record struct Clip(SKRect Rect, SKPath? Outline);

    /// <inheritdoc />
    /// <remarks>Saves the canvas and cuts it: an outline with antialiasing, as Agg cuts a clip path, so the edge of the circle of a polar
    /// axes is not stepped.</remarks>
    void IClipSurface<Clip>.Apply(Clip clip)
    {
        _canvas.Save();
        if (clip.Outline is { } outline) _canvas.ClipPath(outline, SKClipOperation.Intersect, antialias: true);
        else _canvas.ClipRect(clip.Rect);
    }

    /// <inheritdoc />
    void IClipSurface<Clip>.Lift() => _canvas.Restore();

    // The group opacity applied to subsequent Draw* calls is the surface's (_surface.Opacity). It is kept as state
    // rather than as a Skia SaveLayer because SetOpacity/SetOpacity(1.0) is called
    // pairwise in series renderers (e.g. SurfaceSeriesRenderer) without an intervening
    // Restore, so layer-based implementation would drop every polygon between the
    // two calls. Each CreateFillPaint/CreateStrokePaint multiplies the alpha of its
    // color by it.

    /// <summary>What the surface was told: the looks of the theme the marks drawn next are drawn under, the math texts of the figure
    /// and the opacity of the marks.</summary>
    private ThemedSurface _surface = new();

    /// <inheritdoc />
    ref ThemedSurface ISurfaceRenderContext.Surface => ref _surface;

    /// <summary>Creates a new SkiaSharp render context wrapping the given canvas. Its text is drawn from the shapings the
    /// package keeps, the ones the layout measured, so a label drawn on every frame is shaped once.</summary>
    public SkiaRenderContext(SKCanvas canvas) : this(canvas, ShapedTextStore.Shared) { }

    /// <summary>Creates a render context over <paramref name="canvas"/> that draws text from the shapings kept in
    /// <paramref name="shaped"/>.</summary>
    internal SkiaRenderContext(SKCanvas canvas, ShapedTextStore shaped)
    {
        _canvas = canvas;
        Shaped = shaped;
    }

    /// <summary>The shapings the text is drawn from.</summary>
    internal ShapedTextStore Shaped { get; }

    /// <summary>Whether the canvas is a grid of pixels: a raster export is, a PDF page, a recording and a control that paints on a canvas
    /// of its own are not. Only a raster export that is told a theme that asks for it (<see cref="RasterLook.StrokesOnPixelGrid"/>, the
    /// matplotlib themes) puts the marks that ask (<see cref="IPixelGridRenderContext"/>: a spine, a tick mark, a grid line, a bar, a cell
    /// of a matrix) on its pixel grid and covers its lines as matplotlib's Agg backend does; every other canvas leaves them where they
    /// stand and strokes its lines.</summary>
    internal bool SnapsToPixelGrid { get; init; }

    /// <inheritdoc />
    /// <remarks>The canvas shapes every face of every set from the package's own fonts: the set of the looks it was told.</remarks>
    MathFontSet IThemedRenderContext.MathFonts => _surface.MathFontsFor(inPackageFonts: true);

    /// <summary>The grid this context puts the marks that ask for it on: the Agg arithmetic over its canvas, or <see langword="null"/>
    /// when the canvas is no pixel grid or the theme it draws under does not ask for one, whose marks then stand where they were put and
    /// whose lines are stroked. The one test of whether this context snaps.</summary>
    private AggPixelGrid? Grid => SnapsToPixelGrid && _surface.Looks.Raster.StrokesOnPixelGrid ? _grid ??= new AggPixelGrid(_canvas) : null;

    private AggPixelGrid? _grid;

    /// <inheritdoc />
    PixelSegment IPixelGridRenderContext.OnPixelGrid(PixelSegment line, double thickness, bool projectingEnds) =>
        Grid is { } grid ? grid.OnPixelGrid(line, thickness, projectingEnds) : line;

    /// <inheritdoc />
    PixelSegment? IPixelGridRenderContext.MarkerOnPixelGrid(Point anchor, PixelSegment mark, double thickness) =>
        Grid?.MarkerOnPixelGrid(anchor, mark, thickness);

    /// <inheritdoc />
    Rect IPixelGridRenderContext.OnPixelGrid(Rect area, double edgeThickness) =>
        Grid is { } grid ? grid.OnPixelGrid(area, edgeThickness) : area;

    /// <inheritdoc />
    IReadOnlyList<Point> IPixelGridRenderContext.OnPixelGrid(IReadOnlyList<Point> line, double thickness) =>
        Grid is { } grid ? grid.OnPixelGrid(line, thickness) : line;

    /// <inheritdoc />
    bool IPixelGridRenderContext.FillsCellGrids => Grid is not null;

    /// <inheritdoc />
    /// <remarks>One image of the cells, drawn into the area put on the pixel grid with nearest sampling and no antialiasing: each pixel
    /// takes the cell its centre falls in. The colours are copied into the image, so a recording that keeps the draw keeps them.</remarks>
    void IPixelGridRenderContext.FillCellGrid(ReadOnlySpan<Color> colours, int columns, Rect area)
    {
        _clips.Mark(this);
        int rows = colours.Length / columns;
        var onGrid = Grid is { } grid ? grid.OnPixelGrid(area, 0) : area;
        var info = new SKImageInfo(columns, rows, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        using var image = SKImage.FromPixelCopy(info, MemoryMarshal.AsBytes(colours));
        using var paint = new SKPaint { IsAntialias = false, Color = SKColors.Black.WithAlpha(byte.MaxValue.Scaled(_surface.Opacity)) };
        _canvas.DrawImage(image, ToSkRect(onGrid),
            new SKSamplingOptions(SKFilterMode.Nearest, SKMipmapMode.None), paint);
    }

    /// <inheritdoc />
    void IPixelGridRenderContext.DrawRectangleOnPixelGrid(Rect area, ShapeStyle style)
    {
        _clips.Mark(this);
        if (Grid is not { } grid)
        {
            DrawRectangle(area, style);
            return;
        }

        var onGrid = grid.OnPixelGrid(area, style.HasVisibleStroke ? style.StrokeThickness : 0);
        // The frame is four bars with square corners: an edge that is dashed, or whose corners are not mitred, is stroked instead.
        if (!style.HasVisibleStroke || !style.OutlineStroke().IsSolid || style.StrokeJoin != JoinStyle.Miter)
        {
            DrawRectangle(onGrid, style);
            return;
        }

        DrawRectangle(onGrid, style with { Stroke = null });
        grid.FillFrame(onGrid, ToSkColor(style.Stroke!.Value), style.StrokeThickness);
    }

    /// <inheritdoc />
    /// <remarks>In a raster export a horizontal or vertical line is filled as the rectangle it covers,
    /// so each pixel takes the share of the line that falls in it, as matplotlib's Agg rasterizer covers it; a dashed one is painted
    /// along its rectangle. Skia covers a stroked path in quarters of a pixel: the rows beside a
    /// horizontal line of 1.39 px were a quarter covered where Agg covers 0.19 of them. The ends are butt ends either way.</remarks>
    public void DrawLine(Point p1, Point p2, StrokeStyle stroke)
    {
        _clips.Mark(this);
        if (Grid is { } grid && !new PixelSegment(p1, p2).Slanted
            && grid.TryFillLine(p1, p2, ToSkColor(stroke.Color), stroke.Thickness, DashPatterns.Lengths(stroke.Style, stroke.Thickness, _surface.Looks.Strokes.Dashes))) return;

        using var paint = CreateStrokePaint(stroke);
        _canvas.DrawLine((float)p1.X, (float)p1.Y, (float)p2.X, (float)p2.Y, paint);
    }

    /// <inheritdoc />
    public void DrawLines(IReadOnlyList<Point> points, StrokeStyle stroke)
    {
        if (!points.FormsALine) return;
        _clips.Mark(this);
        using var paint = CreateStrokePaint(stroke);
        var sink = new SkiaPathSink(new SKPath());
        using var path = sink.Path;
        points.WalkPolyline(ref sink, closed: false);
        _canvas.DrawPath(path, paint);
    }

    /// <inheritdoc />
    public void DrawPolygon(IReadOnlyList<Point> points, ShapeStyle shape)
    {
        _clips.Mark(this);
        var sink = new SkiaPathSink(new SKPath());
        using var path = sink.Path;
        points.WalkPolyline(ref sink, closed: true);

        PaintShape(shape, new PathShape(path));
    }

    /// <inheritdoc />
    public void DrawCircle(Point center, double radius, ShapeStyle shape)
        => PaintShape(shape, new CircleShape((float)center.X, (float)center.Y, (float)radius));

    /// <inheritdoc />
    public void DrawRectangle(Rect rect, ShapeStyle shape)
    {
        PaintShape(shape, new RectShape(ToSkRect(rect)));
    }

    /// <inheritdoc />
    public void DrawEllipse(Rect bounds, ShapeStyle shape)
    {
        PaintShape(shape, new OvalShape(ToSkRect(bounds)));
    }

    /// <inheritdoc />
    public void DrawText(string text, Point position, Font font, TextAlignment alignment)
        => DrawText(text, position, font, alignment, rotation: 0);

    /// <inheritdoc />
    /// <remarks>Drawn past every clip pushed: a clip cuts marks, never a text, but the texts a series has the surface cut, a contour's
    /// labels.</remarks>
    public void DrawText(string text, Point position, Font font, TextAlignment alignment, double rotation)
    {
        _clips.Text(this);
        // One kept shaping gives the width the alignment needs AND the glyphs the canvas paints, so the two cannot
        // disagree — and it is the shaping the layout measured, so a label drawn on every frame is shaped once.
        var shaped = Shaped.Of(text, font);
        if (shaped.Blob is not { } blob)
        {
            return;
        }

        float dx = (float)alignment.StartOffset(shaped.Width);
        using var paint = new SKPaint { Color = ToSkColor(font.Color ?? Colors.Black), IsAntialias = true };
        if (rotation == 0)
        {
            _canvas.DrawText(blob, (float)position.X + dx, (float)position.Y, paint);
        }
        else
        {
            _canvas.Save();
            // Rotate around the anchor point (matches SVG transform="rotate(angle, x, y)").
            // The library turns counter-clockwise; Skia's RotateDegrees turns clockwise.
            _canvas.RotateDegrees((float)rotation.ToClockwise(), (float)position.X, (float)position.Y);
            _canvas.DrawText(blob, (float)position.X + dx, (float)position.Y, paint);
            _canvas.Restore();
        }
    }

    /// <inheritdoc />
    public void DrawRichText(RichText richText, Point position, Font font, TextAlignment alignment)
        => DrawRichText(richText, position, font, alignment, rotation: 0);

    /// <inheritdoc />
    /// <remarks>Laid out once in core and drawn by the one verb of a text: each run painted as a text in its face at its size, each rule
    /// filled as a rectangle.</remarks>
    public void DrawRichText(RichText richText, Point position, Font font, TextAlignment alignment, double rotation) =>
        this.Draw(TextLabel.OfRich(richText), font, new TextPlacement(position, alignment) { Rotation = rotation });

    /// <inheritdoc />
    public void DrawPath(IReadOnlyList<PathSegment> segments, ShapeStyle shape)
    {
        using var path = PathOf(segments);
        PaintShape(shape, new PathShape(path));
    }

    /// <summary>The Skia path of <paramref name="segments"/>: the one conversion a drawn path and a clip outline share.</summary>
    private static SKPath PathOf(IReadOnlyList<PathSegment> segments)
    {
        var sink = new SkiaPathSink(new SKPath());
        segments.WalkInto(ref sink);
        return sink.Path;
    }

    /// <inheritdoc />
    public void PushClip(Rect clipRect) =>
        _clips.Push(this, new Clip(ToSkRect(clipRect), null));

    /// <inheritdoc />
    /// <remarks>Cut with antialiasing, as Agg cuts a clip path, so the edge of the circle of a polar axes is not stepped.</remarks>
    void IOutlineClipRenderContext.PushClip(IReadOnlyList<PathSegment> outline) => _clips.Push(this, new Clip(default, PathOf(outline)));

    /// <inheritdoc />
    public void PopClip() => _clips.Pop(this).Outline?.Dispose();

    /// <inheritdoc />
    public Size MeasureText(string text, Font font) => FontMetricsDefault.Current.Measure(text, font);

    /// <inheritdoc />
    bool ITextClipRenderContext.CutsTexts
    {
        get => _clips.CutsTexts;
        set => _clips.CutsTexts = value;
    }

    /// <inheritdoc />
    public void SetOpacity(double opacity) => _surface.HoldOpacity(opacity);

    /// <summary>Paints <paramref name="geometry"/> in <paramref name="shape"/>, cut by the clips pushed: the one sequence of a shape on a
    /// canvas (<see cref="CanvasShapeExtensions.PaintIn"/>: the fill, the hatch over it, the outline, each when it shows), each layer with
    /// a paint of its own. The geometry is a value the method is made for (<typeparamref name="TShape"/>), not a delegate, so a shape
    /// allocates nothing beyond its paints.</summary>
    /// <typeparam name="TShape">The kind of geometry: a path, a circle, a rectangle or an oval.</typeparam>
    /// <param name="shape">The fill, the hatch and the outline; the outline is dashed by <see cref="ShapeStyle.StrokeLineStyle"/>.</param>
    /// <param name="geometry">The geometry, painted once for each layer.</param>
    private void PaintShape<TShape>(in ShapeStyle shape, in TShape geometry) where TShape : struct, ISkiaShape
    {
        _clips.Mark(this);
        new Layers<TShape>(this, geometry).PaintIn(shape);
    }

    /// <summary>The layers of one shape on this canvas, each painted with its own paint.</summary>
    /// <typeparam name="TShape">The kind of geometry.</typeparam>
    /// <param name="surface">The surface whose canvas and paints the layers are painted with.</param>
    /// <param name="geometry">The geometry.</param>
    private readonly struct Layers<TShape>(SkiaRenderContext surface, TShape geometry) : ICanvasShape where TShape : struct, ISkiaShape
    {
        /// <inheritdoc />
        /// <remarks>An aliased fill (<see cref="ShapeStyle.Aliased"/>) is painted with no antialiasing: a pixel its edge crosses is the fill
        /// or the ground.</remarks>
        public void Fill(in ShapeStyle style)
        {
            using var paint = style.HasVisibleGradient ? surface.CreateGradientPaint(style.Gradient!.Value) : surface.CreateFillPaint(style.Fill!.Value);
            paint.IsAntialias = !style.Aliased;
            geometry.Draw(surface._canvas, paint);
        }

        /// <inheritdoc />
        /// <remarks>Hatch parity with the SVG backend: a hatch that shows on screen and vanishes from an exported PNG is the worst
        /// divergence there is for an operator who screenshots a wall, so the raster backend paints the same pattern, as a tiled shader
        /// over the fill that was just laid down.</remarks>
        public void Hatch(in ShapeStyle style)
        {
            using var tile = surface.CreateHatchPaint(style);
            geometry.Draw(surface._canvas, tile);
        }

        /// <inheritdoc />
        /// <remarks>Through the paint a line of the same stroke is drawn with.</remarks>
        public void Stroke(StrokeStyle outline)
        {
            using var paint = surface.CreateStrokePaint(outline);
            geometry.Draw(surface._canvas, paint);
        }
    }

    /// <summary>The geometry of one shape, painted onto a canvas with a paint.</summary>
    private interface ISkiaShape
    {
        /// <summary>Paints the geometry onto <paramref name="canvas"/> with <paramref name="paint"/>.</summary>
        void Draw(SKCanvas canvas, SKPaint paint);
    }

    /// <summary>A polygon or a path, already built.</summary>
    private readonly record struct PathShape(SKPath Path) : ISkiaShape
    {
        public void Draw(SKCanvas canvas, SKPaint paint) => canvas.DrawPath(Path, paint);
    }

    /// <summary>A circle around its centre.</summary>
    private readonly record struct CircleShape(float X, float Y, float Radius) : ISkiaShape
    {
        public void Draw(SKCanvas canvas, SKPaint paint) => canvas.DrawCircle(X, Y, Radius, paint);
    }

    /// <summary>A rectangle.</summary>
    private readonly record struct RectShape(SKRect Rect) : ISkiaShape
    {
        public void Draw(SKCanvas canvas, SKPaint paint) => canvas.DrawRect(Rect, paint);
    }

    /// <summary>An ellipse inscribed in its bounds.</summary>
    private readonly record struct OvalShape(SKRect Bounds) : ISkiaShape
    {
        public void Draw(SKCanvas canvas, SKPaint paint) => canvas.DrawOval(Bounds, paint);
    }

    /// <summary>Tile size in layout units — the same 6 the SVG hatch registry uses, so the two backends
    /// produce the same hatch density at the same scale.</summary>
    private const float HatchTileSize = (float)MatPlotLibNet.Styling.HatchTile.Size;

    /// <summary>The most a hatch tile is enlarged for a canvas that is scaled up (a PNG at a high resolution): beyond
    /// this the tile is stretched, since a tile is built for every hatched shape drawn.</summary>
    private const float MostHatchTileScale = 16f;

    /// <summary>Builds a paint whose shader tiles the hatch geometry, so any shape the caller draws with it
    /// is covered by the pattern. The base fill is already on the canvas; this paints only the strokes.
    /// <para>The tile is drawn with the pixels the canvas has: on a canvas scaled up for a higher resolution it is a
    /// bigger bitmap with the same 6 units of geometry, and the shader scales it back to 6 units, which the canvas
    /// scales up to the device again. Drawn small and stretched instead, a stroke of one unit would be two doubled
    /// half-tone pixels rather than two solid ones.</para></summary>
    private SKPaint CreateHatchPaint(ShapeStyle shape)
    {
        Color strokes = shape.HatchStroke;

        int tilePixels = (int)MathF.Round(HatchTileSize * DeviceScale());
        float pixelsPerUnit = tilePixels / HatchTileSize;

        using var tile = new SKBitmap(tilePixels, tilePixels);
        using (var canvas = new SKCanvas(tile))
        using (var paint = new SKPaint
               {
                   Color = ToSkColor(strokes),
                   Style = SKPaintStyle.Stroke,
                   StrokeWidth = 1f,
                   IsAntialias = true
               })
        {
            canvas.Clear(SKColors.Transparent);
            canvas.Scale(pixelsPerUnit);
            DrawHatchTile(canvas, shape.Hatch, paint, ToSkColor(strokes));
        }

        return new SKPaint
        {
            Shader = SKShader.CreateBitmap(
                tile.Copy(), SKShaderTileMode.Repeat, SKShaderTileMode.Repeat,
                SKMatrix.CreateScale(1f / pixelsPerUnit, 1f / pixelsPerUnit)),
            IsAntialias = true
        };
    }

    /// <summary>How many device pixels a layout unit is on the canvas being drawn on: 1 for a canvas at its natural
    /// scale (and for one scaled down), more for a raster export at a higher resolution.</summary>
    private float DeviceScale()
    {
        SKMatrix m = _canvas.TotalMatrix;
        float across = new SKPoint(m.ScaleX, m.SkewY).Length;
        float down = new SKPoint(m.SkewX, m.ScaleY).Length;
        return Math.Clamp(MathF.Max(across, down), 1f, MostHatchTileScale);
    }

    /// <summary>Draws one tile of the hatch: the marks <see cref="HatchTile"/> says, the same the SVG pattern and the MAUI
    /// pattern paint draw, so the PNG and the SVG of one figure carry the same marks.</summary>
    private static void DrawHatchTile(SKCanvas canvas, HatchPattern hatch, SKPaint stroke, SKColor color)
    {
        foreach (var mark in HatchTile.MarksOf(hatch))
        {
            if (mark.Kind == HatchMarkKind.Dot)
            {
                DrawHatchDot(canvas, color);
            }
            else
            {
                canvas.DrawLine((float)mark.X1, (float)mark.Y1, (float)mark.X2, (float)mark.Y2, stroke);
            }
        }
    }

    private static void DrawHatchDot(SKCanvas canvas, SKColor color)
    {
        using var dot = new SKPaint { Color = color, Style = SKPaintStyle.Fill, IsAntialias = true };
        canvas.DrawCircle(HatchTileSize / 2, HatchTileSize / 2, 1f, dot);
    }

    /// <summary>Creates a filled SKPaint from the given color, modulated by the current group opacity.</summary>
    private SKPaint CreateFillPaint(Color color) => new()
    {
        Color = ToSkColor(color),
        Style = SKPaintStyle.Fill,
        IsAntialias = true
    };

    /// <summary>Creates a fill paint whose shader blends the two ends of <paramref name="gradient"/> along its line, with the
    /// nearest end's colour beyond either end, modulated by the current group opacity like a flat fill. On a PDF canvas Skia
    /// writes it as an axial shading, a vector, not a bitmap.</summary>
    private SKPaint CreateGradientPaint(LinearGradient gradient) => new()
    {
        Style = SKPaintStyle.Fill,
        IsAntialias = true,
        Shader = SKShader.CreateLinearGradient(
            new SKPoint((float)gradient.Start.X, (float)gradient.Start.Y),
            new SKPoint((float)gradient.End.X, (float)gradient.End.Y),
            [ToSkColor(gradient.StartColor), ToSkColor(gradient.EndColor)],
            SKShaderTileMode.Clamp),
    };

    /// <summary>Creates a stroke SKPaint with its dash pattern under the theme's look and the join of its corners, modulated by the current
    /// group opacity. The one place a line, a polyline and the outline of a shape are stroked from.</summary>
    private SKPaint CreateStrokePaint(StrokeStyle stroke)
    {
        var paint = new SKPaint
        {
            Color = ToSkColor(stroke.Color),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = (float)stroke.Thickness,
            StrokeJoin = ToSkia(_surface.Looks.Strokes.JoinOf(stroke)),
            IsAntialias = true
        };
        var dashes = DashPatterns.Lengths(stroke.Style, stroke.Thickness, _surface.Looks.Strokes.Dashes);
        if (dashes.Count > 0)
            paint.PathEffect = SKPathEffect.CreateDash(dashes.ToSingles(), 0);
        return paint;
    }

    /// <summary>Converts a MatPlotLibNet Color to a SkiaSharp SKColor, applying the current opacity of the surface.</summary>
    private SKColor ToSkColor(Color c)
    {
        return new SKColor(c.R, c.G, c.B, c.A.Scaled(_surface.Opacity));
    }

    /// <summary>The canvas' join: round and bevel by name, anything else a mitre.</summary>
    /// <returns>The canvas' join.</returns>
    /// <param name="join">The <see cref="JoinStyle"/> it works on.</param>
    internal static SKStrokeJoin ToSkia(JoinStyle join) => join switch
    {
        JoinStyle.Round => SKStrokeJoin.Round,
        JoinStyle.Bevel => SKStrokeJoin.Bevel,
        _ => SKStrokeJoin.Miter,
    };

    /// <summary>The rectangle between the corners <c>(X, Y)</c> and <c>(X + Width, Y + Height)</c>, in the floats a canvas takes.
    /// The corners are kept as they are given: a rectangle of negative size is the canvas' to draw or refuse.</summary>
    /// <returns>The canvas' rectangle.</returns>
    /// <param name="rect">The <see cref="Rect"/> it works on.</param>
    internal static SKRect ToSkRect(Rect rect) => new SKRect((float)rect.X, (float)rect.Y, (float)(rect.X + rect.Width), (float)(rect.Y + rect.Height));

    /// <summary>One direction of a <see cref="Skia.SkiaRenderContext.AggPixelGrid.DeviceGrid"/>: the device pixels of one unit along it and the device pixel of the unit 0.</summary>
    /// <param name="Scale">The device pixels of one unit.</param>
    /// <param name="Trans">The device pixel of the unit 0.</param>
    internal readonly record struct DeviceAxis(double Scale, double Trans)
    {
        /// <summary>The device pixel a distance in the figure's units lands on.</summary>
        /// <param name="unit">A distance, in the figure's units.</param>
        /// <returns>The device pixel.</returns>
        internal double Device(double unit) => unit * Scale + Trans;

        /// <summary>The distance in the figure's units that lands on a device pixel.</summary>
        /// <param name="device">A device pixel.</param>
        /// <returns>The distance, in the figure's units.</returns>
        internal double Unit(double device) => (device - Trans) / Scale;

        /// <summary>The whole number nearest to <paramref name="value"/>, a half rounded up: matplotlib's <c>mpl_round</c>, which Agg puts a
        /// vertex on a whole pixel by and reads a stroke's width in whole pixels by.</summary>
        /// <param name="value">A value, in device pixels.</param>
        /// <returns>The whole number.</returns>
        internal static double Rounded(double value) => Math.Floor(value + 0.5);

        /// <summary>The whole device pixel a distance lands nearest to, moved <paramref name="onGrid"/> on.</summary>
        /// <param name="unit">A distance, in the figure's units.</param>
        /// <param name="onGrid">How far past the whole pixel, in device pixels.</param>
        /// <returns>The device pixel.</returns>
        internal double OnGrid(double unit, double onGrid) => Rounded(Device(unit)) + onGrid;

        /// <summary>A distance put on the grid (<see cref="Skia.SkiaRenderContext.DeviceAxis.OnGrid"/>) and read back into the figure's units.</summary>
        /// <param name="unit">A distance, in the figure's units.</param>
        /// <param name="onGrid">How far past the whole pixel, in device pixels.</param>
        /// <returns>The distance on the grid, in the figure's units.</returns>
        internal double Snapped(double unit, double onGrid) => Unit(OnGrid(unit, onGrid));
    }
}
