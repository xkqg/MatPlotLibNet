// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Styling;
using SkiaSharp;

namespace MatPlotLibNet.Skia.Tests;

/// <summary>Verifies <see cref="SkiaRenderContext"/> behavior.</summary>
public class SkiaRenderContextTests : IDisposable
{
    private readonly SkiaCanvasFixture _fixture = new(200);

    public void Dispose() => _fixture.Dispose();

    /// <summary>Verifies that the constructor accepts an SKCanvas without throwing.</summary>
    [Fact]
    public void Constructor_AcceptsSKCanvas_WithoutThrowing()
    {
        using var bitmap = new SKBitmap(10, 10);
        using var canvas = new SKCanvas(bitmap);
        var ctx = new SkiaRenderContext(canvas);
        Assert.NotNull(ctx);
    }

    /// <summary>Verifies that DrawLine renders non-transparent pixels on the canvas.</summary>
    [Fact]
    public void DrawLine_RendersNonTransparentPixels()
    {
        _fixture.Context.DrawLine(new Point(10, 50), new Point(90, 50), new StrokeStyle(Colors.Red, 2, LineStyle.Solid));
        _fixture.Canvas.Flush();

        Assert.True(HasNonTransparentPixels());
    }

    /// <summary>Verifies that DrawLines renders a polyline with non-transparent pixels.</summary>
    [Fact]
    public void DrawLines_RendersPolyline()
    {
        var points = new List<Point> { new(10, 10), new(50, 90), new(90, 10) };
        _fixture.Context.DrawLines(points, new StrokeStyle(Colors.Blue, 2, LineStyle.Solid));
        _fixture.Canvas.Flush();

        Assert.True(HasNonTransparentPixels());
    }

    /// <summary>Verifies that DrawPolygon with a fill color renders filled pixels.</summary>
    [Fact]
    public void DrawPolygon_WithFill_RendersFilled()
    {
        var points = new List<Point> { new(10, 10), new(90, 10), new(90, 90), new(10, 90) };
        _fixture.Context.DrawPolygon(points, new ShapeStyle(Colors.Green, null, 0));
        _fixture.Canvas.Flush();

        Assert.True(HasNonTransparentPixels());
    }

    /// <summary>Verifies that DrawPolygon with a stroke color renders an outline.</summary>
    [Fact]
    public void DrawPolygon_WithStroke_RendersOutline()
    {
        var points = new List<Point> { new(10, 10), new(90, 10), new(90, 90), new(10, 90) };
        _fixture.Context.DrawPolygon(points, new ShapeStyle(null, Colors.Red, 2));
        _fixture.Canvas.Flush();

        Assert.True(HasNonTransparentPixels());
    }

    /// <summary>Verifies that DrawCircle with a fill color renders non-transparent pixels at the center.</summary>
    [Fact]
    public void DrawCircle_WithFill_RendersCenterArea()
    {
        _fixture.Context.DrawCircle(new Point(50, 50), 30, new ShapeStyle(Colors.Blue, null, 0));
        _fixture.Canvas.Flush();

        // Verify the center area has non-transparent pixels
        var pixel = _fixture.Bitmap.GetPixel(50, 50);
        Assert.NotEqual(SKColors.Transparent, pixel);
    }

    /// <summary>Verifies that DrawCircle with a stroke color renders an outline.</summary>
    [Fact]
    public void DrawCircle_WithStroke_RendersOutline()
    {
        _fixture.Context.DrawCircle(new Point(50, 50), 30, new ShapeStyle(null, Colors.Red, 3));
        _fixture.Canvas.Flush();

        Assert.True(HasNonTransparentPixels());
    }

    /// <summary>Verifies that DrawRectangle and DrawEllipse with a fill color render non-transparent pixels at the center.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DrawRectangleAndEllipse_WithFill_RenderFilled(bool rectangle)
    {
        if (rectangle) _fixture.Context.DrawRectangle(new Rect(10, 10, 80, 80), new ShapeStyle(Colors.Green, null, 0));
        else _fixture.Context.DrawEllipse(new Rect(10, 10, 80, 80), new ShapeStyle(Colors.Magenta, null, 0));
        _fixture.Canvas.Flush();

        var pixel = _fixture.Bitmap.GetPixel(50, 50);
        Assert.NotEqual(SKColors.Transparent, pixel);
    }

    /// <summary>Verifies that DrawRectangle with a stroke color renders an outline.</summary>
    [Fact]
    public void DrawRectangle_WithStroke_RendersOutline()
    {
        _fixture.Context.DrawRectangle(new Rect(10, 10, 80, 80), new ShapeStyle(null, Colors.Black, 2));
        _fixture.Canvas.Flush();

        Assert.True(HasNonTransparentPixels());
    }

    /// <summary>Verifies that DrawText renders non-transparent pixels on the canvas.</summary>
    [Fact]
    public void DrawText_RendersPixels()
    {
        _fixture.Context.DrawText("Hello", new Point(10, 50), new Font { Size = 20, Color = Colors.Black },
            TextAlignment.Left);
        _fixture.Canvas.Flush();

        Assert.True(HasNonTransparentPixels());
    }

    /// <summary>Verifies that DrawPath with MoveTo and LineTo segments renders non-transparent pixels.</summary>
    [Fact]
    public void DrawPath_MoveToLineTo_RendersSegments()
    {
        var segments = new List<PathSegment>
        {
            new MoveToSegment(new Point(10, 10)),
            new LineToSegment(new Point(90, 50)),
            new LineToSegment(new Point(50, 90))
        };
        _fixture.Context.DrawPath(segments, new ShapeStyle(null, Colors.Red, 2));
        _fixture.Canvas.Flush();

        Assert.True(HasNonTransparentPixels());
    }

    /// <summary>Verifies that DrawPath with a Close segment renders a filled closed shape.</summary>
    [Fact]
    public void DrawPath_WithClose_RendersClosedShape()
    {
        var segments = new List<PathSegment>
        {
            new MoveToSegment(new Point(10, 10)),
            new LineToSegment(new Point(90, 10)),
            new LineToSegment(new Point(50, 90)),
            new CloseSegment()
        };
        _fixture.Context.DrawPath(segments, new ShapeStyle(Colors.Cyan, null, 0));
        _fixture.Canvas.Flush();

        Assert.True(HasNonTransparentPixels());
    }

    /// <summary>Verifies that PushClip prevents drawing outside the clipping rectangle.</summary>
    [Fact]
    public void PushClip_ClipsDrawingOutsideRect()
    {
        // Clip to a small region in the top-left
        _fixture.Context.PushClip(new Rect(0, 0, 10, 10));
        // Draw a rectangle in the bottom-right, fully outside the clip
        _fixture.Context.DrawRectangle(new Rect(50, 50, 40, 40), new ShapeStyle(Colors.Red, null, 0));
        _fixture.Canvas.Flush();

        // Verify the bottom-right area remains transparent (alpha == 0)
        var pixel = _fixture.Bitmap.GetPixel(70, 70);
        Assert.Equal(0, pixel.Alpha);
    }

    /// <summary>A clip to an outline (the disc of a polar axes, the triangle of a ternary one) keeps what is drawn inside the outline
    /// and cuts what lies in the box round it but outside the outline.</summary>
    [Fact]
    public void AnOutlineClip_KeepsTheInside_AndCutsTheRestOfItsBox()
    {
        PathSegment[] triangle =
            [new MoveToSegment(new Point(50, 0)), new LineToSegment(new Point(0, 100)), new LineToSegment(new Point(100, 100)), new CloseSegment()];

        using (_fixture.Context.ClippedTo(new ClipRegion(new Rect(0, 0, 100, 100), triangle)))
        {
            _fixture.Context.DrawRectangle(new Rect(0, 0, 100, 100), new ShapeStyle(Colors.Red, null, 0));
        }

        _fixture.Canvas.Flush();

        Assert.Equal(0, _fixture.Bitmap.GetPixel(5, 10).Alpha);
        Assert.Equal(SKColors.Red, _fixture.Bitmap.GetPixel(50, 80));
    }

    /// <summary>Verifies that PopClip restores full-canvas drawing after removing the clip region.</summary>
    [Fact]
    public void PopClip_RestoresDrawingAfterClip()
    {
        _fixture.Context.PushClip(new Rect(0, 0, 10, 10));
        _fixture.Context.PopClip();
        // Drawing should now work across the full canvas
        _fixture.Context.DrawRectangle(new Rect(50, 50, 40, 40), new ShapeStyle(Colors.Blue, null, 0));
        _fixture.Canvas.Flush();

        var pixel = _fixture.Bitmap.GetPixel(70, 70);
        Assert.NotEqual(SKColors.Transparent, pixel);
    }

    /// <summary>Verifies that MeasureText returns a non-zero width and height for a text string.</summary>
    [Fact]
    public void MeasureText_ReturnsNonZeroSize()
    {
        var size = _fixture.Context.MeasureText("Hello World", new Font { Size = 12 });

        Assert.True(size.Width > 0);
        Assert.True(size.Height > 0);
    }

    /// <summary>Verifies that MeasureText returns larger dimensions for a larger font size.</summary>
    [Fact]
    public void MeasureText_LargerFont_ReturnsLargerSize()
    {
        var smallSize = _fixture.Context.MeasureText("Hello", new Font { Size = 10 });
        var largeSize = _fixture.Context.MeasureText("Hello", new Font { Size = 24 });

        Assert.True(largeSize.Width > smallSize.Width);
        Assert.True(largeSize.Height > smallSize.Height);
    }

    /// <summary>Verifies that SetOpacity reduces the alpha channel of rendered pixels.</summary>
    [Fact]
    public void SetOpacity_ReducesPixelAlpha()
    {
        // Draw fully opaque rectangle first to compare
        using var opaqueBitmap = new SKBitmap(100, 100);
        using var opaqueCanvas = new SKCanvas(opaqueBitmap);
        opaqueCanvas.Clear(SKColors.Transparent);
        var opaqueCtx = new SkiaRenderContext(opaqueCanvas);
        opaqueCtx.DrawRectangle(new Rect(10, 10, 80, 80), new ShapeStyle(Colors.Red, null, 0));
        opaqueCanvas.Flush();
        var opaquePixel = opaqueBitmap.GetPixel(50, 50);

        // Now draw with 50% opacity
        _fixture.Context.SetOpacity(0.5);
        _fixture.Context.DrawRectangle(new Rect(10, 10, 80, 80), new ShapeStyle(Colors.Red, null, 0));
        _fixture.Canvas.Flush();
        var semiPixel = _fixture.Bitmap.GetPixel(50, 50);

        Assert.True(semiPixel.Alpha < opaquePixel.Alpha,
            $"Semi-transparent alpha ({semiPixel.Alpha}) should be less than opaque alpha ({opaquePixel.Alpha})");
    }

    // ── The line style of a shape's stroke ────────────────────────────────────
    // A shape's stroke is dashed by the rule a line's stroke is dashed by (CreateStrokePaint: the pattern of the style, in
    // multiples of the thickness). At a thickness of 2 the dashed pattern 5,2 is 10 on and 4 off, so a stroke that starts at
    // x = 10 is on from 10 to 20, off from 20 to 24 and on again from 24 to 34: pixel 15 is inked and pixel 22 is not.

    private static ShapeStyle Stroked(LineStyle style) =>
        new(null, Colors.Black, 2) { StrokeLineStyle = style };

    private static readonly PathSegment[] HorizontalStroke =
        [new MoveToSegment(new Point(10, 50)), new LineToSegment(new Point(90, 50))];

    private static readonly Point[] Square = [new(10, 50), new(90, 50), new(90, 90), new(10, 90)];

    /// <summary>The shapes a stroke is drawn on, by name: a path and a polygon along their first edge, a rectangle along its top edge (the
    /// rectangle, the circle and the ellipse stroke through the same fill-then-stroke sequence, so they take the line style too).</summary>
    private void StrokeOn(string shape, ShapeStyle style)
    {
        switch (shape)
        {
            case "path": _fixture.Context.DrawPath(HorizontalStroke, style); break;
            case "polygon": _fixture.Context.DrawPolygon(Square, style); break;
            default: _fixture.Context.DrawRectangle(new Rect(10, 50, 80, 40), style); break;
        }
        _fixture.Canvas.Flush();
    }

    /// <summary>A shape stroked dashed leaves the gaps of the dashed line: the library's 5 on and 2 off from x = 10, the lengths the SVG
    /// writes, whatever the width.</summary>
    [Theory]
    [InlineData("path")]
    [InlineData("polygon")]
    [InlineData("rectangle")]
    public void ADashedStroke_LeavesTheGapsOfADashedLine(string shape)
    {
        StrokeOn(shape, Stroked(LineStyle.Dashed));

        Assert.True(_fixture.Bitmap.GetPixel(12, 50).Alpha > 0, "the first dash is not inked");
        Assert.Equal(0, _fixture.Bitmap.GetPixel(16, 50).Alpha);
        Assert.True(_fixture.Bitmap.GetPixel(19, 50).Alpha > 0, "the second dash is not inked");
    }

    /// <summary>A shape stroked solid, the default, has no gap: the figures drawn before a shape had a line style.</summary>
    [Theory]
    [InlineData("path")]
    [InlineData("polygon")]
    public void ASolidStroke_LeavesNoGap(string shape)
    {
        StrokeOn(shape, Stroked(LineStyle.Solid));

        Assert.True(_fixture.Bitmap.GetPixel(22, 50).Alpha > 0);
    }

    /// <summary>A path whose stroke has the line style None draws no stroke, and the fill under it is still drawn.</summary>
    [Fact]
    public void ANonePathStroke_DrawsNoStroke()
    {
        _fixture.Context.DrawPath(HorizontalStroke, new ShapeStyle(null, Colors.Black, 2) { StrokeLineStyle = LineStyle.None });
        _fixture.Canvas.Flush();
        Assert.False(HasNonTransparentPixels());

        _fixture.Context.DrawPath([new MoveToSegment(new Point(10, 10)), new LineToSegment(new Point(90, 10)), new LineToSegment(new Point(50, 90)), new CloseSegment()],
            new ShapeStyle(Colors.Green, Colors.Black, 2) { StrokeLineStyle = LineStyle.None });
        _fixture.Canvas.Flush();
        Assert.True(_fixture.Bitmap.GetPixel(50, 30).Alpha > 0, "the fill is gone");
        Assert.Equal(0, _fixture.Bitmap.GetPixel(50, 9).Alpha);
    }

    /// <summary>A polygon whose stroke has the line style None draws no stroke, and the fill under it is still drawn.</summary>
    [Fact]
    public void ANonePolygonStroke_DrawsNoStroke()
    {
        _fixture.Context.DrawPolygon(Square, new ShapeStyle(null, Colors.Black, 2) { StrokeLineStyle = LineStyle.None });
        _fixture.Canvas.Flush();
        Assert.False(HasNonTransparentPixels());

        _fixture.Context.DrawPolygon(Square, new ShapeStyle(Colors.Green, Colors.Black, 2) { StrokeLineStyle = LineStyle.None });
        _fixture.Canvas.Flush();
        Assert.True(_fixture.Bitmap.GetPixel(50, 70).Alpha > 0, "the fill is gone");
        Assert.Equal(0, _fixture.Bitmap.GetPixel(50, 49).Alpha);
    }

    private bool HasNonTransparentPixels()
    {
        for (int y = 0; y < _fixture.Bitmap.Height; y++)
            for (int x = 0; x < _fixture.Bitmap.Width; x++)
                if (_fixture.Bitmap.GetPixel(x, y).Alpha > 0)
                    return true;
        return false;
    }

    [Fact]
    public void DrawLines_FewerThanTwoPoints_NoOp()
    {
        _fixture.Context.DrawLines(new List<Point> { new(10, 10) }, new StrokeStyle(Colors.Red, 1, LineStyle.Solid));
    }

    [Fact]
    public void DrawLines_TwoOrMorePoints_DrawsPath()
    {
        _fixture.Context.DrawLines(new List<Point> { new(10, 10), new(50, 50), new(90, 30) },
            new StrokeStyle(Colors.Black, 2, LineStyle.Solid));
        Assert.NotEqual(SKColors.Transparent, _fixture.Bitmap.GetPixel(50, 50));
    }

    [Fact]
    public void DrawPolygon_FewerThanThreePoints_DrawsTheOutlineItIs()
    {
        _fixture.Context.DrawPolygon(new List<Point> { new(10, 10), new(20, 20) },
            new ShapeStyle(Colors.Red, Colors.Black, 1));
        Assert.NotEqual(SKColors.Transparent, _fixture.Bitmap.GetPixel(15, 15));
    }

    [Fact]
    public void DrawPolygon_StrokeOnly_NoFill_DrawsOutline()
    {
        _fixture.Context.DrawPolygon(new List<Point> { new(10, 10), new(50, 10), new(30, 50) }, new ShapeStyle(null, Colors.Black, 2));
    }

    [Theory]
    [InlineData(true,  2.0)]
    [InlineData(true,  0.0)]
    [InlineData(false, 2.0)]
    public void DrawEllipse_StrokeStrokeThickness_AllArms(bool hasStroke, double thickness)
    {
        Color? stroke = hasStroke ? Colors.Black : (Color?)null;
        _fixture.Context.DrawEllipse(new Rect(10, 10, 50, 30), new ShapeStyle(Colors.Red, stroke, thickness));
    }

    [Theory]
    [InlineData(TextAlignment.Left,   0.0)]
    [InlineData(TextAlignment.Center, 0.0)]
    [InlineData(TextAlignment.Right,  0.0)]
    [InlineData(TextAlignment.Center, 90.0)]
    public void DrawText_AlignmentAndRotation_AllArms(TextAlignment alignment, double rotation)
    {
        _fixture.Context.DrawText("Hello", new Point(50, 50),
            new Font { Family = "sans-serif", Size = 12 }, alignment, rotation);
    }

    [Fact]
    public void DrawText_BoldItalic_ResolvesStyledTypeface()
    {
        var font = new Font
        {
            Family = "sans-serif", Size = 14,
            Weight = FontWeight.Bold, Slant = FontSlant.Italic,
        };
        _fixture.Context.DrawText("Bold-Italic", new Point(20, 100), font, TextAlignment.Left);
    }

    [Theory]
    [InlineData("alpha", 10, 50, 12, TextAlignment.Left)]    // a single normal span
    [InlineData("x_i^2", 20, 100, 14, TextAlignment.Center)] // a subscript and a superscript
    public void DrawRichText_DoesNotThrow(string text, double x, double y, double size, TextAlignment alignment)
    {
        var rt = MathTextParser.Parse(text);
        _fixture.Context.DrawRichText(rt, new Point(x, y),
            new Font { Family = "sans-serif", Size = size }, alignment);
    }

    [Fact]
    public void DrawRichText_Rotated_DoesNotThrow()
    {
        var rt = MathTextParser.Parse("rotated");
        _fixture.Context.DrawRichText(rt, new Point(50, 50),
            new Font { Family = "sans-serif", Size = 12 }, TextAlignment.Right, rotation: 45);
    }

    [Fact]
    public void DrawPath_AllSegmentTypes_NoThrow()
    {
        var segments = new List<PathSegment>
        {
            new MoveToSegment(new Point(10, 10)),
            new LineToSegment(new Point(50, 50)),
            new BezierSegment(new Point(60, 50), new Point(70, 60), new Point(80, 80)),
            new ArcSegment(new Point(100, 100), 20, 20, 0, 180),
            new CloseSegment(),
        };
        _fixture.Context.DrawPath(segments, new ShapeStyle(Colors.Blue, Colors.Black, 1));
    }

    [Fact]
    public void PushClip_PopClip_StackDisciplineWorks()
    {
        _fixture.Context.PushClip(new Rect(10, 10, 30, 30));
        _fixture.Context.DrawRectangle(new Rect(0, 0, 50, 50), new ShapeStyle(Colors.Red, null, 0));
        _fixture.Context.PopClip();
        _fixture.Context.DrawRectangle(new Rect(60, 60, 30, 30), new ShapeStyle(Colors.Green, null, 0));
    }

    [Theory]
    [InlineData(-0.5)]
    [InlineData(0.0)]
    [InlineData(0.5)]
    [InlineData(1.0)]
    [InlineData(1.5)]
    public void SetOpacity_ClampedToValidRange(double opacity)
    {
        _fixture.Context.SetOpacity(opacity);
        _fixture.Context.DrawRectangle(new Rect(10, 10, 30, 30), new ShapeStyle(Colors.Red, null, 0));
    }

    [Fact]
    public void MeasureText_ReturnsNonZeroWidth()
    {
        var size = _fixture.Context.MeasureText("Hello", new Font { Family = "sans-serif", Size = 12 });
        Assert.True(size.Width > 0);
    }

    [Theory]
    [InlineData("bold", FontWeight.Bold, FontSlant.Normal)]
    [InlineData("italic", FontWeight.Normal, FontSlant.Italic)]
    public void DrawText_BoldWeightOrItalicSlant_RendersWithoutError(string text, FontWeight weight, FontSlant slant)
    {
        _fixture.Context.DrawText(text, new Point(10, 30),
            new Font { Family = "sans-serif", Size = 12, Weight = weight, Slant = slant }, TextAlignment.Left);
    }

    [Fact]
    public void DrawText_BoldItalicCombo_RendersWithoutError()
    {
        _fixture.Context.DrawText("bi", new Point(10, 30),
            new Font { Family = "sans-serif", Size = 12, Weight = FontWeight.Bold, Slant = FontSlant.Italic },
            TextAlignment.Center);
    }

    [Fact]
    public void DrawRichText_WithSuperscriptSpan_RendersWithoutError()
    {
        var rt = new RichText([
            new TextSpan("x", TextSpanKind.Normal, FontSizeScale: 1.0),
            new TextSpan("2", TextSpanKind.Superscript, FontSizeScale: 0.7),
        ]);
        _fixture.Context.DrawRichText(rt, new Point(10, 30),
            new Font { Family = "sans-serif", Size = 12 }, TextAlignment.Left);
    }

    [Fact]
    public void DrawRichText_WithSubscriptSpan_RendersWithoutError()
    {
        var rt = new RichText([
            new TextSpan("H", TextSpanKind.Normal, FontSizeScale: 1.0),
            new TextSpan("2", TextSpanKind.Subscript, FontSizeScale: 0.7),
            new TextSpan("O", TextSpanKind.Normal, FontSizeScale: 1.0),
        ]);
        _fixture.Context.DrawRichText(rt, new Point(10, 30),
            new Font { Family = "sans-serif", Size = 12 }, TextAlignment.Right);
    }

    [Fact]
    public void DrawRichText_WithRotation_RendersWithoutError()
    {
        var rt = new RichText([new TextSpan("rot", TextSpanKind.Normal, FontSizeScale: 1.0)]);
        _fixture.Context.DrawRichText(rt, new Point(50, 50),
            new Font { Family = "sans-serif", Size = 12 }, TextAlignment.Left, rotation: 90);
    }

    [Fact]
    public void DrawRichText_EmptySpans_NoOp()
    {
        var rt = new RichText([]);
        _fixture.Context.DrawRichText(rt, new Point(10, 30),
            new Font { Family = "sans-serif", Size = 12 }, TextAlignment.Left);
    }

    [Fact]
    public void SetOpacity_BelowOne_ReducesPixelAlpha()
    {
        _fixture.Context.SetOpacity(0.5);
        _fixture.Context.DrawRectangle(new Rect(60, 60, 30, 30), new ShapeStyle(new Color(255, 0, 0, 255), null, 0));
        var px = _fixture.Bitmap.GetPixel(75, 75);
        Assert.InRange(px.Alpha, 100, 160);
    }

    [Theory]
    [InlineData(LineStyle.Dashed)]
    [InlineData(LineStyle.Dotted)]
    [InlineData(LineStyle.DashDot)]
    public void DrawLine_DashedStyles_PathEffectApplied(LineStyle style)
    {
        _fixture.Context.DrawLine(new Point(10, 100), new Point(190, 100), new StrokeStyle(Colors.Black, 2, style));
        Assert.NotEqual(SKColors.Transparent, _fixture.Bitmap.GetPixel(100, 100));
    }

    [Fact]
    public void DrawText_CssStyleFamilyStack_RendersWithoutError()
    {
        _fixture.Context.DrawText("css", new Point(10, 30),
            new Font { Family = "DejaVu Sans, sans-serif", Size = 12 },
            TextAlignment.Left);
    }

    [Fact]
    public void DrawText_FamilyStackWithEmptyCandidate_SkipsAndResolves()
    {
        _fixture.Context.DrawText("css2", new Point(10, 30),
            new Font { Family = ", , DejaVu Sans", Size = 12 },
            TextAlignment.Left);
    }

    [Fact]
    public void DrawText_NullFamily_FallsThroughToOsLookup()
    {
        _fixture.Context.DrawText("os", new Point(10, 30),
            new Font { Family = null!, Size = 12 },
            TextAlignment.Left);
    }

    [Theory]
    [InlineData(FontWeight.Bold, FontSlant.Normal, "test")]
    [InlineData(FontWeight.Normal, FontSlant.Italic, "test")]
    [InlineData(FontWeight.Bold, FontSlant.Italic, "bi")]
    [InlineData(FontWeight.Normal, FontSlant.Normal, "plain")]
    public void SkiaFontMetrics_MeasuresATextInEveryWeightAndSlantArm(FontWeight weight, FontSlant slant, string text)
    {
        var font = new Font { Family = "DejaVu Sans", Size = 12, Weight = weight, Slant = slant };

        Assert.True(new SkiaFontMetrics().Measure(text, font).Width > 0);
    }

    /// <summary>A shape is painted fill then stroke without a delegate made for its geometry: a shape with nothing visible
    /// creates no paint, so drawing it allocates nothing at all.</summary>
    [Fact]
    public void DrawingAShape_AllocatesNoDelegateForItsGeometry()
    {
        var invisible = new ShapeStyle(null, null, 0);
        var rect = new Rect(10, 10, 20, 20);
        for (int i = 0; i < 100; i++) DrawInvisibleShapes(rect, invisible); // warm up the JIT

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 1000; i++) DrawInvisibleShapes(rect, invisible);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(0, allocated);
    }

    private void DrawInvisibleShapes(Rect rect, ShapeStyle invisible)
    {
        _fixture.Context.DrawCircle(new Point(50, 50), 10, invisible);
        _fixture.Context.DrawRectangle(rect, invisible);
        _fixture.Context.DrawEllipse(rect, invisible);
    }

    /// <summary>Each shape is painted through the geometry it was given: a filled and stroked circle, rectangle, ellipse,
    /// polygon and path each cover the pixel at their centre in the fill and the pixel on their outline in the stroke.</summary>
    [Fact]
    public void EveryShape_IsPaintedThroughItsOwnGeometry()
    {
        var style = new ShapeStyle(Colors.Red, Colors.Blue, 2);

        AssertPainted(c => c.DrawCircle(new Point(50, 50), 20, style), centre: new(50, 50), outline: new(70, 50));
        AssertPainted(c => c.DrawRectangle(new Rect(30, 30, 40, 40), style), centre: new(50, 50), outline: new(30, 50));
        AssertPainted(c => c.DrawEllipse(new Rect(20, 35, 60, 30), style), centre: new(50, 50), outline: new(20, 50));
        AssertPainted(c => c.DrawPolygon([new Point(30, 30), new Point(70, 30), new Point(70, 70), new Point(30, 70)], style),
            centre: new(50, 50), outline: new(30, 50));
        AssertPainted(c => c.DrawPath([new MoveToSegment(new Point(30, 30)), new LineToSegment(new Point(70, 30)),
                new LineToSegment(new Point(70, 70)), new LineToSegment(new Point(30, 70)), new CloseSegment()], style),
            centre: new(50, 50), outline: new(30, 50));
    }

    private static void AssertPainted(Action<SkiaRenderContext> draw, SKPointI centre, SKPointI outline)
    {
        using var bitmap = new SKBitmap(100, 100);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);
        draw(new SkiaRenderContext(canvas));
        canvas.Flush();

        Assert.Equal(new SKColor(255, 0, 0, 255), bitmap.GetPixel(centre.X, centre.Y));
        var edge = bitmap.GetPixel(outline.X, outline.Y);
        Assert.True(edge.Blue > 128 && edge.Red < 128, $"outline pixel {edge}");
    }
}
