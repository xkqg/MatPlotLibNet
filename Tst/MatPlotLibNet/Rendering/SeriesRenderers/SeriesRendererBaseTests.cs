// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Models.Series.XY;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Downsampling;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.SeriesRenderers;

/// <summary>
/// Phase Z.1 — direct branch-arm coverage for the abstract <c>SeriesRenderer</c> base class
/// (<c>?? base</c> fall-throughs in Resolve*, tooltip branch matrix, downsampling guards).
/// Existing renderer tests exercise the happy paths through concrete subclasses; this file
/// targets the base-class branches that no subclass alone could reach.
/// </summary>
public class SeriesRendererBaseTests
{
    private static readonly Rect Bounds = new(80, 60, 640, 480);

    private static SeriesRenderContext NewContext(IRenderContext? ctx = null, bool tooltips = false, CycledProperties? cycled = null)
    {
        ctx ??= new SvgRenderContext();
        var area = new RenderArea(Bounds, ctx);
        var transform = new DataTransform(0, 100, 0, 50, Bounds);
        return new SeriesRenderContext(transform, ctx, Colors.Tab10Blue, area)
        {
            TooltipsEnabled = tooltips,
            CycledProps = cycled,
        };
    }

    // ── Resolve* — `?? cycled ?? default` arms ──────────────────────────────

    [Fact]
    public void ResolveColor_ReturnsSeriesColor_WhenSet()
    {
        var r = new TestRenderer(NewContext());
        Assert.Equal(Colors.Red, r.CallResolveColor(Colors.Red));
    }

    [Fact]
    public void ResolveColor_FallsBackToContext_WhenNull()
    {
        var r = new TestRenderer(NewContext());
        Assert.Equal(Colors.Tab10Blue, r.CallResolveColor(null));
    }

    [Fact]
    public void ResolveLineStyle_ReturnsCycled_WhenSeriesNullAndCyclerPresent()
    {
        var cycled = new CycledProperties(Colors.Red, LineStyle.Dashed, MarkerStyle.None, 2.0);
        var r = new TestRenderer(NewContext(cycled: cycled));
        Assert.Equal(LineStyle.Dashed, r.CallResolveLineStyle(null));
    }

    [Fact]
    public void ResolveLineStyle_FallsBackToSolid_WhenCyclerAbsent()
    {
        var r = new TestRenderer(NewContext(cycled: null));
        Assert.Equal(LineStyle.Solid, r.CallResolveLineStyle(null));
    }

    [Fact]
    public void ResolveLineStyle_PrefersSeriesValue_WhenSet()
    {
        var cycled = new CycledProperties(Colors.Red, LineStyle.Dashed, MarkerStyle.None, 2.0);
        var r = new TestRenderer(NewContext(cycled: cycled));
        Assert.Equal(LineStyle.Dotted, r.CallResolveLineStyle(LineStyle.Dotted));
    }

    [Fact]
    public void ResolveMarkerStyle_FallsBackToNone_WhenAllNull()
    {
        var r = new TestRenderer(NewContext(cycled: null));
        Assert.Equal(MarkerStyle.None, r.CallResolveMarkerStyle(null));
    }

    [Fact]
    public void ResolveMarkerStyle_ReturnsCycled_WhenCyclerPresent()
    {
        var cycled = new CycledProperties(Colors.Red, LineStyle.Solid, MarkerStyle.Square, 1.0);
        var r = new TestRenderer(NewContext(cycled: cycled));
        Assert.Equal(MarkerStyle.Square, r.CallResolveMarkerStyle(null));
    }

    [Fact]
    public void ResolveLineWidth_FallsBackTo15_WhenAllNull()
    {
        var r = new TestRenderer(NewContext(cycled: null));
        Assert.Equal(1.5, r.CallResolveLineWidth(null));
    }

    [Fact]
    public void ResolveLineWidth_ReturnsCycled_WhenCyclerPresent()
    {
        var cycled = new CycledProperties(Colors.Red, LineStyle.Solid, MarkerStyle.None, 3.5);
        var r = new TestRenderer(NewContext(cycled: cycled));
        Assert.Equal(3.5, r.CallResolveLineWidth(null));
    }

    // ── BeginTooltip / EndTooltip — `TooltipsEnabled && Ctx is SvgRenderContext` ────

    [Fact]
    public void BeginTooltip_EmitsGroup_WhenEnabledAndSvgContext()
    {
        var svg = new SvgRenderContext();
        var r = new TestRenderer(NewContext(ctx: svg, tooltips: true));
        r.CallBeginTooltip("hello");
        r.CallEndTooltip();
        var output = svg.GetOutput();
        Assert.Contains("<title>hello</title>", output);
    }

    [Fact]
    public void BeginTooltip_NoOp_WhenTooltipsDisabled()
    {
        var svg = new SvgRenderContext();
        var r = new TestRenderer(NewContext(ctx: svg, tooltips: false));
        r.CallBeginTooltip("ignored");
        r.CallEndTooltip();
        Assert.DoesNotContain("<title>", svg.GetOutput());
    }

    [Fact]
    public void BeginTooltip_NoOp_WhenContextIsNotSvg()
    {
        var fake = new RecordingRenderContext();
        var r = new TestRenderer(NewContext(ctx: fake, tooltips: true));
        r.CallBeginTooltip("ignored");
        r.CallEndTooltip();
        // Recording context should not have received any draw calls
        Assert.Empty(fake.Calls);
    }

    // ── ApplyDownsampling — null guard + slow path + culled-fits-budget arm ─

    [Fact]
    public void ApplyDownsampling_PassesThrough_WhenMaxPointsNull()
    {
        var r = new TestRenderer(NewContext());
        var x = new double[] { 1, 2, 3, 4, 5 };
        var y = new double[] { 10, 20, 30, 40, 50 };
        var result = r.CallApplyDownsampling(x, y, null);
        Assert.Same(x, result.X);
        Assert.Same(y, result.Y);
    }

    [Fact]
    public void ApplyDownsampling_PassesThrough_WhenLengthBelowBudget()
    {
        var r = new TestRenderer(NewContext());
        var x = new double[] { 1, 2, 3 };
        var y = new double[] { 10, 20, 30 };
        var result = r.CallApplyDownsampling(x, y, 100);
        Assert.Same(x, result.X);
    }

    [Fact]
    public void ApplyDownsampling_ReturnsCulled_WhenViewportCullsBelowBudget()
    {
        // Transform DataXMin=0, DataXMax=100. Place 6 points but make 4 of them OOB on the left.
        var r = new TestRenderer(NewContext());
        var x = new double[] { -50, -40, -30, -20, 50, 60 };
        var y = new double[] {   1,   1,   1,   1,  1,  1 };
        var result = r.CallApplyDownsampling(x, y, 4);
        // After culling to [0,100] only the last two survive — under the budget of 4
        Assert.True(result.X.Length <= 4);
    }

    [Fact]
    public void ApplyDownsampling_RunsLttb_WhenCulledExceedsBudget()
    {
        var r = new TestRenderer(NewContext());
        var x = new double[100];
        var y = new double[100];
        for (int i = 0; i < 100; i++) { x[i] = i; y[i] = Math.Sin(i * 0.1); }
        var result = r.CallApplyDownsampling(x, y, 20);
        Assert.True(result.X.Length <= 20);
    }

    // ── Test helpers ────────────────────────────────────────────────────────

    private sealed class TestRenderer : SeriesRenderer<LineSeries>
    {
        public TestRenderer(SeriesRenderContext context) : base(context) { }
        public override void Render(LineSeries series) { }
        public Color CallResolveColor(Color? c) => ResolveColor(c);
        public LineStyle CallResolveLineStyle(LineStyle? s) => ResolveLineStyle(s);
        public MarkerStyle CallResolveMarkerStyle(MarkerStyle? s) => ResolveMarkerStyle(s);
        public double CallResolveLineWidth(double? w) => ResolveLineWidth(w);
        public void CallBeginTooltip(string text) => BeginTooltip(text);
        public void CallEndTooltip() => EndTooltip();
        public XYData CallApplyDownsampling(double[] x, double[] y, int? maxPoints) => ApplyDownsampling(x, y, maxPoints);
    }

    private sealed class RecordingRenderContext : IRenderContext
    {
        public List<string> Calls { get; } = new();
        public void DrawLine(Point p1, Point p2, Color color, double thickness, LineStyle style) => Calls.Add(nameof(DrawLine));
        public void DrawLines(IReadOnlyList<Point> points, Color color, double thickness, LineStyle style) => Calls.Add(nameof(DrawLines));
        public void DrawPolygon(IReadOnlyList<Point> points, Color? fill, Color? stroke, double strokeThickness) => Calls.Add(nameof(DrawPolygon));
        public void DrawCircle(Point center, double radius, Color? fill, Color? stroke, double strokeThickness) => Calls.Add(nameof(DrawCircle));
        public void DrawRectangle(Rect rect, Color? fill, Color? stroke, double strokeThickness) => Calls.Add(nameof(DrawRectangle));
        public void DrawEllipse(Rect bounds, Color? fill, Color? stroke, double strokeThickness) => Calls.Add(nameof(DrawEllipse));
        public void DrawText(string text, Point position, Font font, TextAlignment alignment) => Calls.Add(nameof(DrawText));
        public void DrawPath(IReadOnlyList<PathSegment> segments, Color? fill, Color? stroke, double strokeThickness) => Calls.Add(nameof(DrawPath));
        public void PushClip(Rect clipRect) => Calls.Add(nameof(PushClip));
        public void PopClip() => Calls.Add(nameof(PopClip));
        public Size MeasureText(string text, Font font) => new(text.Length * font.Size * 0.6, font.Size);
        public void SetOpacity(double opacity) => Calls.Add(nameof(SetOpacity));
    }
}
