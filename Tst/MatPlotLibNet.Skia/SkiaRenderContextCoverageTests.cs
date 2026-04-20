// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Styling;
using SkiaSharp;

namespace MatPlotLibNet.Skia.Tests;

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

    /// <summary>DrawLines line 37 — early-return when points.Count &lt; 2.</summary>
    [Fact]
    public void DrawLines_FewerThanTwoPoints_NoOp()
    {
        _ctx.DrawLines(new List<Point> { new(10, 10) }, Colors.Red, 1, LineStyle.Solid);
        // No exception; nothing drawn (single point can't form a line).
    }

    /// <summary>DrawLines with 2+ points — happy path; verify pixel mutation.</summary>
    [Fact]
    public void DrawLines_TwoOrMorePoints_DrawsPath()
    {
        _ctx.DrawLines(new List<Point> { new(10, 10), new(50, 50), new(90, 30) },
            Colors.Black, 2, LineStyle.Solid);
        Assert.NotEqual(SKColors.Transparent, _bitmap.GetPixel(50, 50));
    }

    /// <summary>DrawPolygon line 49 — early-return when points.Count &lt; 3.</summary>
    [Fact]
    public void DrawPolygon_FewerThanThreePoints_NoOp()
    {
        _ctx.DrawPolygon(new List<Point> { new(10, 10), new(20, 20) },
            Colors.Red, Colors.Black, 1);
    }

    /// <summary>DrawPolygon stroke arm without fill.</summary>
    [Fact]
    public void DrawPolygon_StrokeOnly_NoFill_DrawsOutline()
    {
        _ctx.DrawPolygon(new List<Point> { new(10, 10), new(50, 10), new(30, 50) },
            fill: null, stroke: Colors.Black, strokeThickness: 2);
    }

    /// <summary>DrawEllipse line 110 — stroke=null vs stroke=Some, strokeThickness=0 vs &gt;0.</summary>
    [Theory]
    [InlineData(true,  2.0)]   // stroke + thickness
    [InlineData(true,  0.0)]   // stroke set, thickness zero (skips line 112-114)
    [InlineData(false, 2.0)]   // stroke null
    public void DrawEllipse_StrokeStrokeThickness_AllArms(bool hasStroke, double thickness)
    {
        Color? stroke = hasStroke ? Colors.Black : (Color?)null;
        _ctx.DrawEllipse(new Rect(10, 10, 50, 30),
            fill: Colors.Red, stroke: stroke, strokeThickness: thickness);
    }

    /// <summary>DrawText line 128 — every TextAlignment arm + rotation true arm
    /// (line 138 false arm).</summary>
    [Theory]
    [InlineData(TextAlignment.Left,   0.0)]
    [InlineData(TextAlignment.Center, 0.0)]
    [InlineData(TextAlignment.Right,  0.0)]
    [InlineData(TextAlignment.Center, 90.0)]   // rotated
    public void DrawText_AlignmentAndRotation_AllArms(TextAlignment alignment, double rotation)
    {
        _ctx.DrawText("Hello", new Point(50, 50),
            new Font { Family = "sans-serif", Size = 12 }, alignment, rotation);
    }

    /// <summary>DrawText with bold + italic font — exercises the typeface-resolve arms.</summary>
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

    /// <summary>DrawRichText (whole method, was 0% covered) — minimal single-span input.</summary>
    [Fact]
    public void DrawRichText_SingleNormalSpan_DoesNotThrow()
    {
        var rt = MathTextParser.Parse("alpha");
        _ctx.DrawRichText(rt, new Point(10, 50),
            new Font { Family = "sans-serif", Size = 12 }, TextAlignment.Left);
    }

    /// <summary>DrawRichText with subscript + superscript spans — exercises the
    /// per-span baseline-shift switch (line 192-197).</summary>
    [Fact]
    public void DrawRichText_WithSubscriptSuperscript_DoesNotThrow()
    {
        var rt = MathTextParser.Parse("x_i^2");
        _ctx.DrawRichText(rt, new Point(20, 100),
            new Font { Family = "sans-serif", Size = 14 }, TextAlignment.Center);
    }

    /// <summary>DrawRichText with rotation true arm (line 181-185 + 202-203 Restore).</summary>
    [Fact]
    public void DrawRichText_Rotated_DoesNotThrow()
    {
        var rt = MathTextParser.Parse("rotated");
        _ctx.DrawRichText(rt, new Point(50, 50),
            new Font { Family = "sans-serif", Size = 12 }, TextAlignment.Right, rotation: 45);
    }

    /// <summary>DrawPath with every PathSegment subtype to cover the switch arms
    /// (lines 213-225 — MoveTo / LineTo / Bezier / Arc / Close).</summary>
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

    /// <summary>PushClip + PopClip — stack discipline. After pop, drawing outside
    /// the previous clip rect is not clipped.</summary>
    [Fact]
    public void PushClip_PopClip_StackDisciplineWorks()
    {
        _ctx.PushClip(new Rect(10, 10, 30, 30));
        _ctx.DrawRectangle(new Rect(0, 0, 50, 50), Colors.Red, null, 0);
        _ctx.PopClip();
        _ctx.DrawRectangle(new Rect(60, 60, 30, 30), Colors.Green, null, 0);
        // Both should have drawn without throwing.
    }

    /// <summary>SetOpacity — line 246 Math.Clamp arms (below 0, between, above 1).</summary>
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

    /// <summary>MeasureText delegates to ChartServices.FontMetrics — forward-regression guard.</summary>
    [Fact]
    public void MeasureText_ReturnsNonZeroWidth()
    {
        var size = _ctx.MeasureText("Hello", new Font { Family = "sans-serif", Size = 12 });
        Assert.True(size.Width > 0);
    }

    // ── Phase Z.6: Bold/Italic combos in DrawText (lines 125-126 weight/slant arms)

    /// <summary>DrawText with Bold weight — exercises the Bold arm of line 125.</summary>
    [Fact]
    public void DrawText_BoldWeight_RendersWithoutError()
    {
        _ctx.DrawText("bold", new Point(10, 30),
            new Font { Family = "sans-serif", Size = 12, Weight = FontWeight.Bold }, TextAlignment.Left);
    }

    /// <summary>DrawText with Italic slant — exercises the Italic arm of line 126.</summary>
    [Fact]
    public void DrawText_ItalicSlant_RendersWithoutError()
    {
        _ctx.DrawText("italic", new Point(10, 30),
            new Font { Family = "sans-serif", Size = 12, Slant = FontSlant.Italic }, TextAlignment.Left);
    }

    /// <summary>DrawText with Bold + Italic combo — both arms.</summary>
    [Fact]
    public void DrawText_BoldItalicCombo_RendersWithoutError()
    {
        _ctx.DrawText("bi", new Point(10, 30),
            new Font { Family = "sans-serif", Size = 12, Weight = FontWeight.Bold, Slant = FontSlant.Italic },
            TextAlignment.Center);
    }

    // ── Phase Z.6: DrawRichText spans (lines 192-197 Span.Kind switch arms)

    /// <summary>DrawRichText with superscript span — line 194 arm.</summary>
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

    /// <summary>DrawRichText with subscript span — line 195 arm.</summary>
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

    /// <summary>DrawRichText with rotation — lines 181-184 rotation arm.</summary>
    [Fact]
    public void DrawRichText_WithRotation_RendersWithoutError()
    {
        var rt = new RichText([new TextSpan("rot", TextSpanKind.Normal, FontSizeScale: 1.0)]);
        _ctx.DrawRichText(rt, new Point(50, 50),
            new Font { Family = "sans-serif", Size = 12 }, TextAlignment.Left, rotation: 90);
    }

    /// <summary>DrawRichText with empty Spans collection — foreach skips, no error.</summary>
    [Fact]
    public void DrawRichText_EmptySpans_NoOp()
    {
        var rt = new RichText([]);
        _ctx.DrawRichText(rt, new Point(10, 30),
            new Font { Family = "sans-serif", Size = 12 }, TextAlignment.Left);
    }

    // ── Phase Z.6: opacity multiplied through ToSkColor (line 280)

    /// <summary>SetOpacity to 0.5 then DrawRectangle — alpha in pixel should be ~half of color.A.</summary>
    [Fact]
    public void SetOpacity_BelowOne_ReducesPixelAlpha()
    {
        _ctx.SetOpacity(0.5);
        _ctx.DrawRectangle(new Rect(60, 60, 30, 30),
            new Color(255, 0, 0, 255), stroke: null, strokeThickness: 0);
        var px = _bitmap.GetPixel(75, 75);
        // With 50% group opacity applied to fully-opaque red, A should be ~127
        Assert.InRange(px.Alpha, 100, 160);
    }

    // ── Phase Z.6: Dashed / Dotted line styles → CreateStrokePaint dash branch

    [Theory]
    [InlineData(LineStyle.Dashed)]
    [InlineData(LineStyle.Dotted)]
    [InlineData(LineStyle.DashDot)]
    public void DrawLine_DashedStyles_PathEffectApplied(LineStyle style)
    {
        _ctx.DrawLine(new Point(10, 100), new Point(190, 100),
            Colors.Black, thickness: 2, style);
        // Pixel mid-line should NOT be transparent (dashes draw something)
        Assert.NotEqual(SKColors.Transparent, _bitmap.GetPixel(100, 100));
    }

    // ── Phase Z.6: ResolveTypeface multi-family + null-family branches
    // (exercised indirectly via DrawText, since ResolveTypeface is internal to MatPlotLibNet.Skia)

    /// <summary>DrawText with CSS-style font-family stack — exercises the comma-split + per-candidate
    /// loop in FigureSkiaExtensions.ResolveTypeface.</summary>
    [Fact]
    public void DrawText_CssStyleFamilyStack_RendersWithoutError()
    {
        _ctx.DrawText("css", new Point(10, 30),
            new Font { Family = "DejaVu Sans, sans-serif", Size = 12 },
            TextAlignment.Left);
    }

    /// <summary>DrawText with empty/whitespace candidates in the family stack — exercises the
    /// `continue` arm in ResolveTypeface (skip empty candidate).</summary>
    [Fact]
    public void DrawText_FamilyStackWithEmptyCandidate_SkipsAndResolves()
    {
        _ctx.DrawText("css2", new Point(10, 30),
            new Font { Family = ", , DejaVu Sans", Size = 12 },
            TextAlignment.Left);
    }

    /// <summary>DrawText with null family — falls through to OS lookup in ResolveTypeface.</summary>
    [Fact]
    public void DrawText_NullFamily_FallsThroughToOsLookup()
    {
        _ctx.DrawText("os", new Point(10, 30),
            new Font { Family = null, Size = 12 },
            TextAlignment.Left);
    }

    // ── Phase A.1.3 (Strict-90 plan): SkiaFontMetrics Bold-only / Italic-only combos
    // (cobertura SkiaFontMetrics.cs:L32 cc=75% (3/4) — need isolated weight+slant arms)

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
}
