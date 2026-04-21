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
    private readonly SKBitmap _bitmap;
    private readonly SKCanvas _canvas;
    private readonly SkiaRenderContext _ctx;

    public SkiaRenderContextTests()
    {
        _bitmap = new SKBitmap(100, 100);
        _canvas = new SKCanvas(_bitmap);
        _canvas.Clear(SKColors.Transparent);
        _ctx = new SkiaRenderContext(_canvas);
    }

    public void Dispose()
    {
        _canvas.Dispose();
        _bitmap.Dispose();
    }

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
        _ctx.DrawLine(new Point(10, 50), new Point(90, 50), Colors.Red, 2, LineStyle.Solid);
        _canvas.Flush();

        Assert.True(HasNonTransparentPixels());
    }

    /// <summary>Verifies that DrawLines renders a polyline with non-transparent pixels.</summary>
    [Fact]
    public void DrawLines_RendersPolyline()
    {
        var points = new List<Point> { new(10, 10), new(50, 90), new(90, 10) };
        _ctx.DrawLines(points, Colors.Blue, 2, LineStyle.Solid);
        _canvas.Flush();

        Assert.True(HasNonTransparentPixels());
    }

    /// <summary>Verifies that DrawPolygon with a fill color renders filled pixels.</summary>
    [Fact]
    public void DrawPolygon_WithFill_RendersFilled()
    {
        var points = new List<Point> { new(10, 10), new(90, 10), new(90, 90), new(10, 90) };
        _ctx.DrawPolygon(points, fill: Colors.Green, stroke: null, strokeThickness: 0);
        _canvas.Flush();

        Assert.True(HasNonTransparentPixels());
    }

    /// <summary>Verifies that DrawPolygon with a stroke color renders an outline.</summary>
    [Fact]
    public void DrawPolygon_WithStroke_RendersOutline()
    {
        var points = new List<Point> { new(10, 10), new(90, 10), new(90, 90), new(10, 90) };
        _ctx.DrawPolygon(points, fill: null, stroke: Colors.Red, strokeThickness: 2);
        _canvas.Flush();

        Assert.True(HasNonTransparentPixels());
    }

    /// <summary>Verifies that DrawCircle with a fill color renders non-transparent pixels at the center.</summary>
    [Fact]
    public void DrawCircle_WithFill_RendersCenterArea()
    {
        _ctx.DrawCircle(new Point(50, 50), 30, fill: Colors.Blue, stroke: null, strokeThickness: 0);
        _canvas.Flush();

        // Verify the center area has non-transparent pixels
        var pixel = _bitmap.GetPixel(50, 50);
        Assert.NotEqual(SKColors.Transparent, pixel);
    }

    /// <summary>Verifies that DrawCircle with a stroke color renders an outline.</summary>
    [Fact]
    public void DrawCircle_WithStroke_RendersOutline()
    {
        _ctx.DrawCircle(new Point(50, 50), 30, fill: null, stroke: Colors.Red, strokeThickness: 3);
        _canvas.Flush();

        Assert.True(HasNonTransparentPixels());
    }

    /// <summary>Verifies that DrawRectangle with a fill color renders non-transparent pixels at the center.</summary>
    [Fact]
    public void DrawRectangle_WithFill_RendersFilled()
    {
        _ctx.DrawRectangle(new Rect(10, 10, 80, 80), fill: Colors.Green, stroke: null, strokeThickness: 0);
        _canvas.Flush();

        var pixel = _bitmap.GetPixel(50, 50);
        Assert.NotEqual(SKColors.Transparent, pixel);
    }

    /// <summary>Verifies that DrawRectangle with a stroke color renders an outline.</summary>
    [Fact]
    public void DrawRectangle_WithStroke_RendersOutline()
    {
        _ctx.DrawRectangle(new Rect(10, 10, 80, 80), fill: null, stroke: Colors.Black, strokeThickness: 2);
        _canvas.Flush();

        Assert.True(HasNonTransparentPixels());
    }

    /// <summary>Verifies that DrawEllipse with a fill color renders non-transparent pixels at the center.</summary>
    [Fact]
    public void DrawEllipse_WithFill_RendersFilled()
    {
        _ctx.DrawEllipse(new Rect(10, 10, 80, 80), fill: Colors.Magenta, stroke: null, strokeThickness: 0);
        _canvas.Flush();

        var pixel = _bitmap.GetPixel(50, 50);
        Assert.NotEqual(SKColors.Transparent, pixel);
    }

    /// <summary>Verifies that DrawText renders non-transparent pixels on the canvas.</summary>
    [Fact]
    public void DrawText_RendersPixels()
    {
        _ctx.DrawText("Hello", new Point(10, 50), new Font { Size = 20, Color = Colors.Black },
            TextAlignment.Left);
        _canvas.Flush();

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
        _ctx.DrawPath(segments, fill: null, stroke: Colors.Red, strokeThickness: 2);
        _canvas.Flush();

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
        _ctx.DrawPath(segments, fill: Colors.Cyan, stroke: null, strokeThickness: 0);
        _canvas.Flush();

        Assert.True(HasNonTransparentPixels());
    }

    /// <summary>Verifies that PushClip prevents drawing outside the clipping rectangle.</summary>
    [Fact]
    public void PushClip_ClipsDrawingOutsideRect()
    {
        // Clip to a small region in the top-left
        _ctx.PushClip(new Rect(0, 0, 10, 10));
        // Draw a rectangle in the bottom-right, fully outside the clip
        _ctx.DrawRectangle(new Rect(50, 50, 40, 40), fill: Colors.Red, stroke: null, strokeThickness: 0);
        _canvas.Flush();

        // Verify the bottom-right area remains transparent (alpha == 0)
        var pixel = _bitmap.GetPixel(70, 70);
        Assert.Equal(0, pixel.Alpha);
    }

    /// <summary>Verifies that PopClip restores full-canvas drawing after removing the clip region.</summary>
    [Fact]
    public void PopClip_RestoresDrawingAfterClip()
    {
        _ctx.PushClip(new Rect(0, 0, 10, 10));
        _ctx.PopClip();
        // Drawing should now work across the full canvas
        _ctx.DrawRectangle(new Rect(50, 50, 40, 40), fill: Colors.Blue, stroke: null, strokeThickness: 0);
        _canvas.Flush();

        var pixel = _bitmap.GetPixel(70, 70);
        Assert.NotEqual(SKColors.Transparent, pixel);
    }

    /// <summary>Verifies that MeasureText returns a non-zero width and height for a text string.</summary>
    [Fact]
    public void MeasureText_ReturnsNonZeroSize()
    {
        var size = _ctx.MeasureText("Hello World", new Font { Size = 12 });

        Assert.True(size.Width > 0);
        Assert.True(size.Height > 0);
    }

    /// <summary>Verifies that MeasureText returns larger dimensions for a larger font size.</summary>
    [Fact]
    public void MeasureText_LargerFont_ReturnsLargerSize()
    {
        var smallSize = _ctx.MeasureText("Hello", new Font { Size = 10 });
        var largeSize = _ctx.MeasureText("Hello", new Font { Size = 24 });

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
        opaqueCtx.DrawRectangle(new Rect(10, 10, 80, 80), fill: Colors.Red, stroke: null, strokeThickness: 0);
        opaqueCanvas.Flush();
        var opaquePixel = opaqueBitmap.GetPixel(50, 50);

        // Now draw with 50% opacity
        _ctx.SetOpacity(0.5);
        _ctx.DrawRectangle(new Rect(10, 10, 80, 80), fill: Colors.Red, stroke: null, strokeThickness: 0);
        _canvas.Flush();
        var semiPixel = _bitmap.GetPixel(50, 50);

        Assert.True(semiPixel.Alpha < opaquePixel.Alpha,
            $"Semi-transparent alpha ({semiPixel.Alpha}) should be less than opaque alpha ({opaquePixel.Alpha})");
    }

    private bool HasNonTransparentPixels()
    {
        for (int y = 0; y < _bitmap.Height; y++)
            for (int x = 0; x < _bitmap.Width; x++)
                if (_bitmap.GetPixel(x, y).Alpha > 0)
                    return true;
        return false;
    }
}

// ─── SkiaRenderContextCoverageTests.cs ───────────────────────────────────────

/// <summary>Phase Y.6 (v1.7.2, 2026-04-19) — branch coverage for
/// <see cref="SkiaRenderContext"/> methods that the existing harness left at
/// 50–88% branch. Pre-Y.6: 68.5%L / 60.2%B (complexity 98). Each fact pins a
/// specific cobertura `condition-coverage` marker:
///
/// - `DrawLines` line 37: `points.Count &lt; 2` true arm (early return)
/// - `DrawPolygon` line 49: `points.Count &lt; 3` true arm (early return)
/// - `DrawEllipse` line 110: stroke + strokeThickness combo arms
/// - `DrawText(rotation)` line 128: alignment switch's Right arm
/// - `DrawRichText` whole method (was 0% covered) — empty richtext, multi-span,
///   sub/super baseline shift, rotation arm
/// - `DrawPath`: ArcSegment + BezierSegment + CloseSegment switch arms
/// - `PushClip` / `PopClip` — stack discipline
/// - `SetOpacity` — clamping arms (negative, &gt;1)</summary>
public class SkiaRenderContextCoverageTests : IDisposable
{
    private readonly SKBitmap _bitmap;
    private readonly SKCanvas _canvas;
    private readonly SkiaRenderContext _ctx;

    public SkiaRenderContextCoverageTests()
    {
        _bitmap = new SKBitmap(200, 200);
        _canvas = new SKCanvas(_bitmap);
        _canvas.Clear(SKColors.Transparent);
        _ctx = new SkiaRenderContext(_canvas);
    }

    public void Dispose()
    {
        _canvas.Dispose();
        _bitmap.Dispose();
    }

    [Fact]
    public void DrawLines_FewerThanTwoPoints_NoOp()
    {
        _ctx.DrawLines(new List<Point> { new(10, 10) }, Colors.Red, 1, LineStyle.Solid);
    }

    [Fact]
    public void DrawLines_TwoOrMorePoints_DrawsPath()
    {
        _ctx.DrawLines(new List<Point> { new(10, 10), new(50, 50), new(90, 30) },
            Colors.Black, 2, LineStyle.Solid);
        Assert.NotEqual(SKColors.Transparent, _bitmap.GetPixel(50, 50));
    }

    [Fact]
    public void DrawPolygon_FewerThanThreePoints_NoOp()
    {
        _ctx.DrawPolygon(new List<Point> { new(10, 10), new(20, 20) },
            Colors.Red, Colors.Black, 1);
    }

    [Fact]
    public void DrawPolygon_StrokeOnly_NoFill_DrawsOutline()
    {
        _ctx.DrawPolygon(new List<Point> { new(10, 10), new(50, 10), new(30, 50) },
            fill: null, stroke: Colors.Black, strokeThickness: 2);
    }

    [Theory]
    [InlineData(true,  2.0)]
    [InlineData(true,  0.0)]
    [InlineData(false, 2.0)]
    public void DrawEllipse_StrokeStrokeThickness_AllArms(bool hasStroke, double thickness)
    {
        Color? stroke = hasStroke ? Colors.Black : (Color?)null;
        _ctx.DrawEllipse(new Rect(10, 10, 50, 30),
            fill: Colors.Red, stroke: stroke, strokeThickness: thickness);
    }

    [Theory]
    [InlineData(TextAlignment.Left,   0.0)]
    [InlineData(TextAlignment.Center, 0.0)]
    [InlineData(TextAlignment.Right,  0.0)]
    [InlineData(TextAlignment.Center, 90.0)]
    public void DrawText_AlignmentAndRotation_AllArms(TextAlignment alignment, double rotation)
    {
        _ctx.DrawText("Hello", new Point(50, 50),
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
        _ctx.DrawText("Bold-Italic", new Point(20, 100), font, TextAlignment.Left);
    }

    [Fact]
    public void DrawRichText_SingleNormalSpan_DoesNotThrow()
    {
        var rt = MathTextParser.Parse("alpha");
        _ctx.DrawRichText(rt, new Point(10, 50),
            new Font { Family = "sans-serif", Size = 12 }, TextAlignment.Left);
    }

    [Fact]
    public void DrawRichText_WithSubscriptSuperscript_DoesNotThrow()
    {
        var rt = MathTextParser.Parse("x_i^2");
        _ctx.DrawRichText(rt, new Point(20, 100),
            new Font { Family = "sans-serif", Size = 14 }, TextAlignment.Center);
    }

    [Fact]
    public void DrawRichText_Rotated_DoesNotThrow()
    {
        var rt = MathTextParser.Parse("rotated");
        _ctx.DrawRichText(rt, new Point(50, 50),
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
        _ctx.DrawPath(segments, fill: Colors.Blue, stroke: Colors.Black, strokeThickness: 1);
    }

    [Fact]
    public void PushClip_PopClip_StackDisciplineWorks()
    {
        _ctx.PushClip(new Rect(10, 10, 30, 30));
        _ctx.DrawRectangle(new Rect(0, 0, 50, 50), Colors.Red, null, 0);
        _ctx.PopClip();
        _ctx.DrawRectangle(new Rect(60, 60, 30, 30), Colors.Green, null, 0);
    }

    [Theory]
    [InlineData(-0.5)]
    [InlineData(0.0)]
    [InlineData(0.5)]
    [InlineData(1.0)]
    [InlineData(1.5)]
    public void SetOpacity_ClampedToValidRange(double opacity)
    {
        _ctx.SetOpacity(opacity);
        _ctx.DrawRectangle(new Rect(10, 10, 30, 30), Colors.Red, null, 0);
    }

    [Fact]
    public void MeasureText_ReturnsNonZeroWidth()
    {
        var size = _ctx.MeasureText("Hello", new Font { Family = "sans-serif", Size = 12 });
        Assert.True(size.Width > 0);
    }

    [Fact]
    public void DrawText_BoldWeight_RendersWithoutError()
    {
        _ctx.DrawText("bold", new Point(10, 30),
            new Font { Family = "sans-serif", Size = 12, Weight = FontWeight.Bold }, TextAlignment.Left);
    }

    [Fact]
    public void DrawText_ItalicSlant_RendersWithoutError()
    {
        _ctx.DrawText("italic", new Point(10, 30),
            new Font { Family = "sans-serif", Size = 12, Slant = FontSlant.Italic }, TextAlignment.Left);
    }

    [Fact]
    public void DrawText_BoldItalicCombo_RendersWithoutError()
    {
        _ctx.DrawText("bi", new Point(10, 30),
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
        _ctx.DrawRichText(rt, new Point(10, 30),
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
        _ctx.DrawRichText(rt, new Point(10, 30),
            new Font { Family = "sans-serif", Size = 12 }, TextAlignment.Right);
    }

    [Fact]
    public void DrawRichText_WithRotation_RendersWithoutError()
    {
        var rt = new RichText([new TextSpan("rot", TextSpanKind.Normal, FontSizeScale: 1.0)]);
        _ctx.DrawRichText(rt, new Point(50, 50),
            new Font { Family = "sans-serif", Size = 12 }, TextAlignment.Left, rotation: 90);
    }

    [Fact]
    public void DrawRichText_EmptySpans_NoOp()
    {
        var rt = new RichText([]);
        _ctx.DrawRichText(rt, new Point(10, 30),
            new Font { Family = "sans-serif", Size = 12 }, TextAlignment.Left);
    }

    [Fact]
    public void SetOpacity_BelowOne_ReducesPixelAlpha()
    {
        _ctx.SetOpacity(0.5);
        _ctx.DrawRectangle(new Rect(60, 60, 30, 30),
            new Color(255, 0, 0, 255), stroke: null, strokeThickness: 0);
        var px = _bitmap.GetPixel(75, 75);
        Assert.InRange(px.Alpha, 100, 160);
    }

    [Theory]
    [InlineData(LineStyle.Dashed)]
    [InlineData(LineStyle.Dotted)]
    [InlineData(LineStyle.DashDot)]
    public void DrawLine_DashedStyles_PathEffectApplied(LineStyle style)
    {
        _ctx.DrawLine(new Point(10, 100), new Point(190, 100),
            Colors.Black, thickness: 2, style);
        Assert.NotEqual(SKColors.Transparent, _bitmap.GetPixel(100, 100));
    }

    [Fact]
    public void DrawText_CssStyleFamilyStack_RendersWithoutError()
    {
        _ctx.DrawText("css", new Point(10, 30),
            new Font { Family = "DejaVu Sans, sans-serif", Size = 12 },
            TextAlignment.Left);
    }

    [Fact]
    public void DrawText_FamilyStackWithEmptyCandidate_SkipsAndResolves()
    {
        _ctx.DrawText("css2", new Point(10, 30),
            new Font { Family = ", , DejaVu Sans", Size = 12 },
            TextAlignment.Left);
    }

    [Fact]
    public void DrawText_NullFamily_FallsThroughToOsLookup()
    {
        _ctx.DrawText("os", new Point(10, 30),
            new Font { Family = null, Size = 12 },
            TextAlignment.Left);
    }

    [Fact]
    public void SkiaFontMetrics_BoldOnlyNotItalic_FlipsBoldArmAlone()
    {
        var fm = new SkiaFontMetrics();
        var font = new Font { Family = "DejaVu Sans", Size = 12, Weight = FontWeight.Bold, Slant = FontSlant.Normal };
        var size = fm.Measure("test", font);
        Assert.True(size.Width > 0);
    }

    [Fact]
    public void SkiaFontMetrics_ItalicOnlyNotBold_FlipsItalicArmAlone()
    {
        var fm = new SkiaFontMetrics();
        var font = new Font { Family = "DejaVu Sans", Size = 12, Weight = FontWeight.Normal, Slant = FontSlant.Italic };
        var size = fm.Measure("test", font);
        Assert.True(size.Width > 0);
    }

    [Fact]
    public void SkiaFontMetrics_BoldAndItalic_FlipsBothArmsTrue()
    {
        var fm = new SkiaFontMetrics();
        var font = new Font { Family = "DejaVu Sans", Size = 12, Weight = FontWeight.Bold, Slant = FontSlant.Italic };
        var size = fm.Measure("bi", font);
        Assert.True(size.Width > 0);
    }

    [Fact]
    public void SkiaFontMetrics_NormalNeitherBoldNorItalic_FlipsBothArmsFalse()
    {
        var fm = new SkiaFontMetrics();
        var font = new Font { Family = "DejaVu Sans", Size = 12, Weight = FontWeight.Normal, Slant = FontSlant.Normal };
        var size = fm.Measure("plain", font);
        Assert.True(size.Width > 0);
    }

    [Fact]
    public void BuildKey_BoldAndItalic_ReturnsBoldItalicSuffix()
    {
        var style = new SKFontStyle((int)SKFontStyleWeight.Bold, (int)SKFontStyleWidth.Normal, SKFontStyleSlant.Italic);
        var key = FigureSkiaExtensions.BuildKey("DejaVu Sans", style);
        Assert.Equal("DejaVu Sans|BoldItalic", key);
    }

    [Fact]
    public void BuildKey_BoldNotItalic_ReturnsBoldSuffix()
    {
        var style = new SKFontStyle((int)SKFontStyleWeight.Bold, (int)SKFontStyleWidth.Normal, SKFontStyleSlant.Upright);
        var key = FigureSkiaExtensions.BuildKey("DejaVu Sans", style);
        Assert.Equal("DejaVu Sans|Bold", key);
    }

    [Fact]
    public void BuildKey_ItalicNotBold_ReturnsItalicSuffix()
    {
        var style = new SKFontStyle((int)SKFontStyleWeight.Normal, (int)SKFontStyleWidth.Normal, SKFontStyleSlant.Italic);
        var key = FigureSkiaExtensions.BuildKey("DejaVu Sans", style);
        Assert.Equal("DejaVu Sans|Italic", key);
    }

    [Fact]
    public void BuildKey_NormalWeight_ReturnsBareFamily()
    {
        var style = new SKFontStyle((int)SKFontStyleWeight.Normal, (int)SKFontStyleWidth.Normal, SKFontStyleSlant.Upright);
        var key = FigureSkiaExtensions.BuildKey("DejaVu Sans", style);
        Assert.Equal("DejaVu Sans", key);
    }

    [Fact]
    public void ResolveTypeface_NullFamily_FallsBackToOsLookup()
    {
        var tf = FigureSkiaExtensions.ResolveTypeface(null, SKFontStyleWeight.Normal, SKFontStyleSlant.Upright);
        Assert.NotNull(tf);
    }

    [Fact]
    public void ResolveTypeface_UnknownFamily_FallsBackToOsLookup()
    {
        var tf = FigureSkiaExtensions.ResolveTypeface("NotABundledFont", SKFontStyleWeight.Normal, SKFontStyleSlant.Upright);
        Assert.NotNull(tf);
    }
}
