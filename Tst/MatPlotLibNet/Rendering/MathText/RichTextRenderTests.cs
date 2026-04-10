// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.MathText;

/// <summary>Verifies that <see cref="SvgRenderContext.DrawRichText"/> emits correct SVG tspan markup,
/// and that chart titles containing math mode produce valid SVG output.</summary>
public class RichTextRenderTests
{
    private static SvgRenderContext MakeCtx() => new();

    // --- SVG tspan emission ---

    [Fact]
    public void DrawRichText_PlainText_EmitsTextElementNoTspan()
    {
        var ctx = MakeCtx();
        var richText = MathTextParser.Parse("hello");
        ctx.DrawRichText(richText, new Point(50, 50), new Font { Size = 12 }, TextAlignment.Left);
        var svg = ctx.GetOutput();
        Assert.Contains("<text", svg);
        Assert.Contains("hello", svg);
    }

    [Fact]
    public void DrawRichText_WithSuperscript_EmitsTspanBaselineShift()
    {
        var ctx = MakeCtx();
        var richText = MathTextParser.Parse("$R^{2}$");
        ctx.DrawRichText(richText, new Point(50, 50), new Font { Size = 12 }, TextAlignment.Left);
        var svg = ctx.GetOutput();
        Assert.Contains("baseline-shift", svg);
        Assert.Contains("super", svg);
    }

    [Fact]
    public void DrawRichText_WithSubscript_EmitsTspanBaselineShiftSub()
    {
        var ctx = MakeCtx();
        var richText = MathTextParser.Parse("$x_{i}$");
        ctx.DrawRichText(richText, new Point(50, 50), new Font { Size = 12 }, TextAlignment.Left);
        var svg = ctx.GetOutput();
        Assert.Contains("baseline-shift", svg);
        Assert.Contains("sub", svg);
    }

    [Fact]
    public void DrawRichText_GreekAlpha_EmitsAlphaCharacter()
    {
        var ctx = MakeCtx();
        var richText = MathTextParser.Parse("$\\alpha$");
        ctx.DrawRichText(richText, new Point(50, 50), new Font { Size = 12 }, TextAlignment.Left);
        var svg = ctx.GetOutput();
        Assert.Contains("\u03B1", svg); // α
    }

    // --- IRenderContext default fallback (plain-text backend) ---

    [Fact]
    public void DrawRichText_DefaultFallback_ConcatenatesText()
    {
        // Use a minimal implementation that only has the default DrawRichText
        IRenderContext ctx = new FallbackRenderContext();
        var richText = MathTextParser.Parse("$\\alpha^{2}$");
        ctx.DrawRichText(richText, new Point(0, 0), new Font { Size = 12 }, TextAlignment.Left);
        var fallback = (FallbackRenderContext)ctx;
        Assert.NotNull(fallback.LastDrawnText);
        Assert.Contains("\u03B1", fallback.LastDrawnText!); // α in concatenated text
    }

    // --- Integration: chart title with math mode ---

    [Fact]
    public void ChartTitle_WithGreek_ProducesUnicodeInSvg()
    {
        string svg = Plt.Create()
            .WithTitle("$\\alpha$ vs $\\beta$")
            .Plot([1.0, 2.0], [1.0, 2.0])
            .ToSvg();

        Assert.Contains("\u03B1", svg); // α
        Assert.Contains("\u03B2", svg); // β
    }

    [Fact]
    public void AxesTitle_WithSuperscript_ProducesTspanInSvg()
    {
        string svg = new FigureBuilder()
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.WithTitle("R$^{2}$");
                ax.Plot([1.0, 2.0], [1.0, 2.0]);
            })
            .Build()
            .ToSvg();

        Assert.Contains("baseline-shift", svg);
    }

    [Fact]
    public void AxisLabel_WithGreek_ProducesUnicodeInSvg()
    {
        string svg = new FigureBuilder()
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.SetYLabel("$\\sigma$ (Pa)");
                ax.Plot([1.0, 2.0], [1.0, 2.0]);
            })
            .Build()
            .ToSvg();

        Assert.Contains("\u03C3", svg); // σ
    }

    // --- Helper: minimal IRenderContext that tracks last DrawText call ---

    private sealed class FallbackRenderContext : IRenderContext
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
        public Size MeasureText(string text, Font font) => new(text.Length * font.Size * 0.6, font.Size * 1.2);
        public void SetOpacity(double opacity) { }
    }
}
