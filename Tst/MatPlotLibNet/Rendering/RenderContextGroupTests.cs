// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies that BeginGroup/EndGroup are available on IRenderContext
/// and that type casts to SvgRenderContext are no longer needed.</summary>
public class RenderContextGroupTests
{
    /// <summary>Verifies that IRenderContext has BeginGroup with default no-op.</summary>
    [Fact]
    public void IRenderContext_BeginGroup_DefaultNoOp()
    {
        IRenderContext ctx = new NullRenderContext();
        ctx.BeginGroup("test"); // should not throw
        ctx.EndGroup();
    }

    /// <summary>Verifies that SvgRenderContext.BeginGroup emits a g element.</summary>
    [Fact]
    public void SvgRenderContext_BeginGroup_EmitsGElement()
    {
        var ctx = new SvgRenderContext();
        ctx.BeginGroup("legend");
        ctx.EndGroup();
        string output = ctx.GetOutput();
        Assert.Contains("<g class=\"legend\">", output);
        Assert.Contains("</g>", output);
    }

    /// <summary>Verifies that spine rendering uses BeginGroup without type casts.</summary>
    [Fact]
    public void DrawSpineLine_NoTypeCast_StillEmitsGroup()
    {
        var fig = new Figure();
        var ax = fig.AddSubPlot();
        ax.Plot([1.0, 2.0], [3.0, 4.0]);

        string svg = fig.ToSvg();
        Assert.Contains("class=\"spine\"", svg);
    }

    /// <summary>Verifies that legend rendering emits a group element.</summary>
    [Fact]
    public void RenderLegend_StillEmitsGroup()
    {
        var fig = new Figure();
        var ax = fig.AddSubPlot();
        ax.Plot([1.0, 2.0], [3.0, 4.0]).Label = "Series 1";

        string svg = fig.ToSvg();
        Assert.Contains("class=\"legend\"", svg);
    }

    // ── Phase J — IRenderContext default-impl branch coverage ────────────────

    private static readonly Font TestFont = new() { Family = "Arial", Size = 12 };
    private static readonly Point Origin = new(0, 0);

    /// <summary>DrawText 5-arg default — rotation is ignored and 4-arg overload called.</summary>
    [Fact]
    public void IRenderContext_DrawText5Arg_DefaultDelegatesToNoRotation()
    {
        IRenderContext ctx = new NullRenderContext();
        ctx.DrawText("hello", Origin, TestFont, TextAlignment.Left, 45.0); // default: ignores rotation
    }

    /// <summary>SetNextElementData default no-op — does not throw.</summary>
    [Fact]
    public void IRenderContext_SetNextElementData_DefaultNoOp()
    {
        IRenderContext ctx = new NullRenderContext();
        ctx.SetNextElementData("data-x", "42");
    }

    /// <summary>MeasureRichText default — empty spans → foreach body never executes.</summary>
    [Fact]
    public void IRenderContext_MeasureRichText_EmptySpans_ReturnsZeroSize()
    {
        IRenderContext ctx = new NullRenderContext();
        var rt = new RichText([]);
        var size = ctx.MeasureRichText(rt, TestFont);
        Assert.Equal(0.0, size.Width);
        Assert.Equal(0.0, size.Height);
    }

    /// <summary>MeasureRichText default — span with FontSizeScale == 1.0 (TRUE arm) and
    /// FontSizeScale != 1.0 (FALSE arm); also exercises if(height &gt; maxHeight) TRUE and FALSE arms.</summary>
    [Fact]
    public void IRenderContext_MeasureRichText_MultipleSpans_ScaleArms()
    {
        IRenderContext ctx = new NullRenderContext(); // MeasureText always returns (50, 12)
        // Two spans: scale=1.0 (TRUE arm), scale=0.7 (FALSE arm).
        // Both return height 12 from NullRenderContext; second span's height == maxHeight → if FALSE arm.
        var rt = new RichText([new TextSpan("A"), new TextSpan("B", FontSizeScale: 0.7)]);
        var size = ctx.MeasureRichText(rt, TestFont);
        Assert.Equal(100.0, size.Width);   // 50 + 50
        Assert.Equal(12.0, size.Height);
    }

    /// <summary>DrawRichText 4-arg default — concatenates spans and calls DrawText.</summary>
    [Fact]
    public void IRenderContext_DrawRichText_DefaultConcatenatesAndDraws()
    {
        var ctx = new RecordingNullContext();
        IRenderContext iCtx = ctx;
        var rt = new RichText([new TextSpan("Hello"), new TextSpan(" World")]);
        iCtx.DrawRichText(rt, Origin, TestFont, TextAlignment.Left);
        Assert.Equal("Hello World", ctx.LastDrawnText);
    }

    /// <summary>DrawRichText 5-arg default — delegates to 4-arg, ignoring rotation.</summary>
    [Fact]
    public void IRenderContext_DrawRichText5Arg_DefaultIgnoresRotation()
    {
        var ctx = new RecordingNullContext();
        IRenderContext iCtx = ctx;
        var rt = new RichText([new TextSpan("Hi")]);
        iCtx.DrawRichText(rt, Origin, TestFont, TextAlignment.Left, 90.0);
        Assert.Equal("Hi", ctx.LastDrawnText);
    }

    /// <summary>A minimal IRenderContext that does nothing (verifies default interface methods work).</summary>
    private sealed class NullRenderContext : IRenderContext
    {
        public void DrawLine(Point p1, Point p2, Color color, double thickness, LineStyle style) { }
        public void DrawLines(IReadOnlyList<Point> points, Color color, double thickness, LineStyle style) { }
        public void DrawPolygon(IReadOnlyList<Point> points, Color? fill, Color? stroke, double strokeThickness) { }
        public void DrawCircle(Point center, double radius, Color? fill, Color? stroke, double strokeThickness) { }
        public void DrawRectangle(Rect rect, Color? fill, Color? stroke, double strokeThickness) { }
        public void DrawEllipse(Rect bounds, Color? fill, Color? stroke, double strokeThickness) { }
        public void DrawText(string text, Point position, Font font, TextAlignment alignment) { }
        public void DrawPath(IReadOnlyList<PathSegment> segments, Color? fill, Color? stroke, double strokeThickness) { }
        public void PushClip(Rect clipRect) { }
        public void PopClip() { }
        public Size MeasureText(string text, Font font) => new(50, 12);
        public void SetOpacity(double opacity) { }
    }

    private sealed class RecordingNullContext : IRenderContext
    {
        public string? LastDrawnText { get; private set; }
        public void DrawLine(Point p1, Point p2, Color color, double thickness, LineStyle style) { }
        public void DrawLines(IReadOnlyList<Point> points, Color color, double thickness, LineStyle style) { }
        public void DrawPolygon(IReadOnlyList<Point> points, Color? fill, Color? stroke, double strokeThickness) { }
        public void DrawCircle(Point center, double radius, Color? fill, Color? stroke, double strokeThickness) { }
        public void DrawRectangle(Rect rect, Color? fill, Color? stroke, double strokeThickness) { }
        public void DrawEllipse(Rect bounds, Color? fill, Color? stroke, double strokeThickness) { }
        public void DrawText(string text, Point position, Font font, TextAlignment alignment)
            => LastDrawnText = text;
        public void DrawPath(IReadOnlyList<PathSegment> segments, Color? fill, Color? stroke, double strokeThickness) { }
        public void PushClip(Rect clipRect) { }
        public void PopClip() { }
        public Size MeasureText(string text, Font font) => new(50, 12);
        public void SetOpacity(double opacity) { }
    }
}
