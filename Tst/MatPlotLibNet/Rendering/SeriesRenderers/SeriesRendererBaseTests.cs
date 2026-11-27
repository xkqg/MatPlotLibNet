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

    private static SeriesRenderContext NewContext(IRenderContext? ctx = null, bool tooltips = false)
    {
        ctx ??= new SvgRenderContext();
        var area = new RenderArea(Bounds, ctx);
        var transform = new DataTransform(0, 100, 0, 50, Bounds);
        return new SeriesRenderContext(transform, ctx, Colors.Tab10Blue, area)
        {
            TooltipsEnabled = tooltips,
        };
    }

    // The colour of a series (named, else its slot's): SeriesRendererOwnerContractTests. Dash, marker and width: SeriesSlotTests.

    // ── BeginTooltip / EndTooltip — `TooltipsEnabled && Ctx is SvgRenderContext` ────

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("BeginTooltip_EmitsGroup_WhenEnabledAndSvgContext", () =>
        {
            var svg = new SvgRenderContext();
            var r = new TestRenderer(NewContext(ctx: svg, tooltips: true));
            r.CallBeginTooltip("hello");
            r.CallEndTooltip();
            return svg.GetOutput();
        }, "<title>hello</title>"),
        new DrawnToken("BeginTooltip_NoOp_WhenTooltipsDisabled", () =>
        {
            var svg = new SvgRenderContext();
            var r = new TestRenderer(NewContext(ctx: svg, tooltips: false));
            r.CallBeginTooltip("ignored");
            r.CallEndTooltip();
            return svg.GetOutput();
        }) { Absent = ["<title>"] },
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

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
        public void CallBeginTooltip(string text) => BeginTooltip(text);
        public void CallEndTooltip() => EndTooltip();
        public XYData CallApplyDownsampling(double[] x, double[] y, int? maxPoints) => ApplyDownsampling(x, y, maxPoints);
    }
}
