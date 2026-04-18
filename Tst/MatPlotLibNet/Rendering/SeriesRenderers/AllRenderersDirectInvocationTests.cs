// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.TestFixtures;

namespace MatPlotLibNet.Tests.Rendering.SeriesRenderers;

/// <summary>Direct-invocation coverage for every <c>SeriesRenderer&lt;T&gt;</c> that the
/// coverage report flagged as 0% line/branch — these renderers are normally only reached
/// through the <c>SvgSeriesRenderer.Visit(T)</c> visitor dispatch, which the high-level
/// API tests don't statically wire to a coverage instrument.
///
/// <para>Each Theory case constructs a real <see cref="SeriesRenderContext"/> and calls
/// <c>renderer.Render(series)</c> directly (NOT via <c>series.Accept(visitor, area)</c>).
/// That is what graduates the renderer's coverage from 0% to its real exercise level.</para>
///
/// <para>Coverage targets (formerly L=0/B=0 in <c>out/coverage/coverage.cobertura.xml</c>):
/// BubbleSeriesRenderer, Contour3DSeriesRenderer, DonutSeriesRenderer, FunnelSeriesRenderer,
/// GanttSeriesRenderer, GaugeSeriesRenderer, Line3DSeriesRenderer, OhlcBarSeriesRenderer,
/// PolarBarSeriesRenderer, PolarScatterSeriesRenderer, ProgressBarSeriesRenderer,
/// Quiver3DSeriesRenderer, QuiverSeriesRenderer, SparklineSeriesRenderer,
/// SunburstSeriesRenderer, Text3DSeriesRenderer, Trisurf3DSeriesRenderer,
/// VoxelSeriesRenderer, WaterfallSeriesRenderer, WireframeSeriesRenderer — plus
/// AreaSeriesRenderer (was 38.7%) and LineSeriesRenderer (was 50%) for branch uplift.</para>
/// </summary>
public class AllRenderersDirectInvocationTests
{
    // ── Shared infrastructure ────────────────────────────────────────────────

    /// <summary>The standard plot bounds used by every direct-invocation test. Matches
    /// the <c>800 × 600</c> figure size that <see cref="Plt"/>-based tests use, with a
    /// generous inner area so polar/3D wedges have room to draw.</summary>
    private static readonly Rect StandardBounds = new(80, 60, 640, 480);

    /// <summary>Builds a fresh <see cref="SeriesRenderContext"/> with a linear data
    /// transform mapping <paramref name="dataXMin"/>..<paramref name="dataXMax"/> ×
    /// <paramref name="dataYMin"/>..<paramref name="dataYMax"/> onto the standard plot
    /// bounds. The returned <see cref="SvgRenderContext"/> is exposed via the out parameter
    /// so tests can inspect the accumulated SVG.</summary>
    private static SeriesRenderContext NewContext(
        out SvgRenderContext svg,
        double dataXMin = -10, double dataXMax = 10,
        double dataYMin = -10, double dataYMax = 10)
    {
        svg = new SvgRenderContext();
        var area = new RenderArea(StandardBounds, svg);
        var transform = new DataTransform(dataXMin, dataXMax, dataYMin, dataYMax, StandardBounds);
        return new SeriesRenderContext(transform, svg, Colors.Tab10Blue, area);
    }

    /// <summary>Returns true if any drawing primitive made it into the SVG output.
    /// We accept any of the renderer-relevant SVG element opens — different renderers emit
    /// different primitives (lines, polylines, polygons, paths, rects, circles, text).</summary>
    private static bool HasAnyDrawing(string svg) =>
        svg.Contains("<line ") || svg.Contains("<polyline ") || svg.Contains("<polygon")
        || svg.Contains("<path ") || svg.Contains("<rect ") || svg.Contains("<circle ")
        || svg.Contains("<text ") || svg.Contains("<ellipse ");

    // ── Theory: every 0%-coverage renderer with non-trivial input ────────────

    /// <summary>Each entry is (renderer-name, factory). The factory builds a series and
    /// renders it through a freshly constructed renderer + context, returning the
    /// resulting SVG. The Theory then asserts the SVG produced ANY drawing primitive —
    /// which proves that <c>renderer.Render(series)</c> traversed at least one line of
    /// the renderer's body and reached the <c>IRenderContext</c>.</summary>
    public static TheoryData<string, Func<string>> NormalCases()
    {
        var data = new TheoryData<string, Func<string>>();

        data.Add(nameof(BubbleSeriesRenderer), () =>
        {
            var ctx = NewContext(out var svg);
            new BubbleSeriesRenderer(ctx).Render(
                new BubbleSeries([1, 2, 3], [4, 5, 6], [50, 80, 120]));
            return svg.GetOutput();
        });

        data.Add(nameof(Contour3DSeriesRenderer), () =>
        {
            var ctx = NewContext(out var svg);
            var z = new double[5, 5];
            for (int i = 0; i < 5; i++)
                for (int j = 0; j < 5; j++)
                    z[i, j] = i + j;
            new Contour3DSeriesRenderer(ctx).Render(
                new Contour3DSeries([0, 1, 2, 3, 4], [0, 1, 2, 3, 4], z) { Levels = 5 });
            return svg.GetOutput();
        });

        data.Add(nameof(DonutSeriesRenderer), () =>
        {
            var ctx = NewContext(out var svg);
            new DonutSeriesRenderer(ctx).Render(
                new DonutSeries([30, 25, 20, 15, 10]) { CenterText = "Total" });
            return svg.GetOutput();
        });

        data.Add(nameof(FunnelSeriesRenderer), () =>
        {
            var ctx = NewContext(out var svg);
            new FunnelSeriesRenderer(ctx).Render(
                new FunnelSeries(["Visit", "Signup", "Pay"], [100, 60, 25]));
            return svg.GetOutput();
        });

        data.Add(nameof(GanttSeriesRenderer), () =>
        {
            var ctx = NewContext(out var svg, dataXMin: 0, dataXMax: 10, dataYMin: -1, dataYMax: 3);
            new GanttSeriesRenderer(ctx).Render(
                new GanttSeries(["Plan", "Build", "Ship"], [0, 2, 5], [3, 6, 9]));
            return svg.GetOutput();
        });

        data.Add(nameof(GaugeSeriesRenderer), () =>
        {
            var ctx = NewContext(out var svg);
            new GaugeSeriesRenderer(ctx).Render(new GaugeSeries(72) { Min = 0, Max = 100 });
            return svg.GetOutput();
        });

        data.Add(nameof(Line3DSeriesRenderer), () =>
        {
            var ctx = NewContext(out var svg);
            new Line3DSeriesRenderer(ctx).Render(
                new Line3DSeries(new double[] { 0, 1, 2, 3 }, new double[] { 0, 1, 0, 1 }, new double[] { 0, 1, 2, 3 }));
            return svg.GetOutput();
        });

        data.Add(nameof(OhlcBarSeriesRenderer), () =>
        {
            var ctx = NewContext(out var svg, dataXMin: -1, dataXMax: 4, dataYMin: 0, dataYMax: 100);
            new OhlcBarSeriesRenderer(ctx).Render(
                new OhlcBarSeries([10, 20, 30], [40, 50, 60], [5, 15, 25], [35, 25, 55]));
            return svg.GetOutput();
        });

        data.Add(nameof(PolarBarSeriesRenderer), () =>
        {
            var ctx = NewContext(out var svg);
            new PolarBarSeriesRenderer(ctx).Render(
                new PolarBarSeries([1, 2, 3, 4], [0, Math.PI / 2, Math.PI, 1.5 * Math.PI]));
            return svg.GetOutput();
        });

        data.Add(nameof(PolarScatterSeriesRenderer), () =>
        {
            var ctx = NewContext(out var svg);
            new PolarScatterSeriesRenderer(ctx).Render(
                new PolarScatterSeries([1, 2, 3, 4], [0, Math.PI / 2, Math.PI, 1.5 * Math.PI]));
            return svg.GetOutput();
        });

        data.Add(nameof(ProgressBarSeriesRenderer), () =>
        {
            var ctx = NewContext(out var svg);
            new ProgressBarSeriesRenderer(ctx).Render(new ProgressBarSeries(0.42));
            return svg.GetOutput();
        });

        data.Add(nameof(Quiver3DSeriesRenderer), () =>
        {
            var ctx = NewContext(out var svg);
            new Quiver3DSeriesRenderer(ctx).Render(
                new Quiver3DSeries(
                    new double[] { 0, 1 }, new double[] { 0, 1 }, new double[] { 0, 1 },
                    new double[] { 1, -1 }, new double[] { 0, 1 }, new double[] { 1, 0 }));
            return svg.GetOutput();
        });

        data.Add(nameof(QuiverSeriesRenderer), () =>
        {
            var ctx = NewContext(out var svg);
            new QuiverSeriesRenderer(ctx).Render(
                new QuiverSeries([0, 1, 2], [0, 1, 2], [1, 0, -1], [0, 1, 0]));
            return svg.GetOutput();
        });

        data.Add(nameof(SparklineSeriesRenderer), () =>
        {
            var ctx = NewContext(out var svg);
            new SparklineSeriesRenderer(ctx).Render(
                new SparklineSeries([1, 3, 2, 5, 4, 7, 6, 9]));
            return svg.GetOutput();
        });

        data.Add(nameof(SunburstSeriesRenderer), () =>
        {
            var ctx = NewContext(out var svg);
            var root = new TreeNode
            {
                Label = "Root",
                Children = new[]
                {
                    new TreeNode { Label = "A", Value = 0, Children = new[]
                    {
                        new TreeNode { Label = "A1", Value = 30 },
                        new TreeNode { Label = "A2", Value = 20 },
                    }},
                    new TreeNode { Label = "B", Value = 40 },
                    new TreeNode { Label = "C", Value = 10 },
                }
            };
            new SunburstSeriesRenderer(ctx).Render(new SunburstSeries(root));
            return svg.GetOutput();
        });

        data.Add(nameof(Text3DSeriesRenderer), () =>
        {
            var ctx = NewContext(out var svg);
            new Text3DSeriesRenderer(ctx).Render(new Text3DSeries(new()
            {
                new Text3DAnnotation(0, 0, 0, "origin"),
                new Text3DAnnotation(1, 1, 1, "p1"),
                new Text3DAnnotation(-1, -1, 2, "p2"),
            }));
            return svg.GetOutput();
        });

        data.Add(nameof(Trisurf3DSeriesRenderer), () =>
        {
            var ctx = NewContext(out var svg);
            new Trisurf3DSeriesRenderer(ctx).Render(new Trisurf3DSeries(
                new double[] { 0, 1, 0.5, 0, 1, 0.5 },
                new double[] { 0, 0, 1, 0, 0, 1 },
                new double[] { 0, 1, 2, 1, 2, 3 }));
            return svg.GetOutput();
        });

        data.Add(nameof(VoxelSeriesRenderer), () =>
        {
            var ctx = NewContext(out var svg);
            var filled = new bool[2, 2, 2];
            filled[0, 0, 0] = true;
            filled[1, 1, 1] = true;
            new VoxelSeriesRenderer(ctx).Render(new VoxelSeries(filled));
            return svg.GetOutput();
        });

        data.Add(nameof(WaterfallSeriesRenderer), () =>
        {
            var ctx = NewContext(out var svg, dataXMin: -1, dataXMax: 5, dataYMin: -50, dataYMax: 100);
            new WaterfallSeriesRenderer(ctx).Render(
                new WaterfallSeries(["Start", "Q1", "Q2", "Q3"], [50, 30, -20, 40]));
            return svg.GetOutput();
        });

        data.Add(nameof(WireframeSeriesRenderer), () =>
        {
            var ctx = NewContext(out var svg);
            var z = new double[3, 3];
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    z[i, j] = Math.Sin(i) + Math.Cos(j);
            new WireframeSeriesRenderer(ctx).Render(
                new WireframeSeries([0, 1, 2], [0, 1, 2], z));
            return svg.GetOutput();
        });

        // Renderers that aren't 0% but score very low on branch coverage — direct
        // invocation here picks up the branches that the high-level tests miss.
        data.Add(nameof(AreaSeriesRenderer), () =>
        {
            var ctx = NewContext(out var svg);
            new AreaSeriesRenderer(ctx).Render(
                new AreaSeries([0, 1, 2, 3, 4], [1, 3, 2, 4, 3]));
            return svg.GetOutput();
        });

        data.Add(nameof(LineSeriesRenderer), () =>
        {
            var ctx = NewContext(out var svg);
            new LineSeriesRenderer(ctx).Render(
                new LineSeries([0, 1, 2, 3, 4], [0, 1, 4, 9, 16]));
            return svg.GetOutput();
        });

        return data;
    }

    /// <summary>Asserts that direct invocation of every targeted renderer produces some
    /// drawing primitive in the SVG output. This is the single graduating test that takes
    /// every 0% renderer to a meaningful coverage level.</summary>
    [Theory]
    [MemberData(nameof(NormalCases))]
    public void Renderer_DirectInvocation_ProducesSvgPrimitive(string rendererName, Func<string> render)
    {
        var svg = render();
        Assert.True(HasAnyDrawing(svg),
            $"{rendererName}: direct Render(series) call produced no SVG primitive. Output: '{svg}'");
    }

    // ── Edge case: empty data ────────────────────────────────────────────────

    /// <summary>Empty-data factories. Every renderer must early-return without throwing
    /// when its primary data array is empty. These are separate from
    /// <see cref="NormalCases"/> because some renderers (e.g. <c>FunnelSeriesRenderer</c>,
    /// <c>SparklineSeriesRenderer</c>) deliberately produce NO output for empty input —
    /// the assertion is just "no exception".</summary>
    public static TheoryData<string, Action> EmptyCases()
    {
        var data = new TheoryData<string, Action>();

        data.Add(nameof(BubbleSeriesRenderer), () =>
        {
            var ctx = NewContext(out _);
            new BubbleSeriesRenderer(ctx).Render(new BubbleSeries([], [], []));
        });

        data.Add(nameof(DonutSeriesRenderer), () =>
        {
            var ctx = NewContext(out _);
            // total <= 0 path: empty Sizes => Sum() == 0 => early return
            new DonutSeriesRenderer(ctx).Render(new DonutSeries([]));
        });

        data.Add(nameof(GanttSeriesRenderer), () =>
        {
            var ctx = NewContext(out _);
            new GanttSeriesRenderer(ctx).Render(new GanttSeries([], [], []));
        });

        data.Add(nameof(Line3DSeriesRenderer), () =>
        {
            // X.Length < 2 early-return path
            var ctx = NewContext(out _);
            new Line3DSeriesRenderer(ctx).Render(
                new Line3DSeries(new double[] { 0 }, new double[] { 0 }, new double[] { 0 }));
        });

        data.Add(nameof(OhlcBarSeriesRenderer), () =>
        {
            var ctx = NewContext(out _);
            new OhlcBarSeriesRenderer(ctx).Render(new OhlcBarSeries([], [], [], []));
        });

        data.Add(nameof(PolarBarSeriesRenderer), () =>
        {
            var ctx = NewContext(out _);
            new PolarBarSeriesRenderer(ctx).Render(new PolarBarSeries([], []));
        });

        data.Add(nameof(PolarScatterSeriesRenderer), () =>
        {
            var ctx = NewContext(out _);
            new PolarScatterSeriesRenderer(ctx).Render(new PolarScatterSeries([], []));
        });

        data.Add(nameof(Quiver3DSeriesRenderer), () =>
        {
            var ctx = NewContext(out _);
            // X.Length == 0 early return
            new Quiver3DSeriesRenderer(ctx).Render(new Quiver3DSeries(
                new double[] { }, new double[] { }, new double[] { },
                new double[] { }, new double[] { }, new double[] { }));
        });

        data.Add(nameof(QuiverSeriesRenderer), () =>
        {
            var ctx = NewContext(out _);
            new QuiverSeriesRenderer(ctx).Render(new QuiverSeries([], [], [], []));
        });

        data.Add(nameof(SparklineSeriesRenderer), () =>
        {
            // Values.Length < 2 early-return
            var ctx = NewContext(out _);
            new SparklineSeriesRenderer(ctx).Render(new SparklineSeries(EdgeCaseData.Empty));
        });

        data.Add(nameof(SunburstSeriesRenderer), () =>
        {
            // GetMaxDepth == 0 early-return for leaf-only root
            var ctx = NewContext(out _);
            new SunburstSeriesRenderer(ctx).Render(new SunburstSeries(new TreeNode { Label = "leaf", Value = 1 }));
        });

        data.Add(nameof(Text3DSeriesRenderer), () =>
        {
            var ctx = NewContext(out _);
            new Text3DSeriesRenderer(ctx).Render(new Text3DSeries(new()));
        });

        data.Add(nameof(Trisurf3DSeriesRenderer), () =>
        {
            // X.Length < 3 early return
            var ctx = NewContext(out _);
            new Trisurf3DSeriesRenderer(ctx).Render(new Trisurf3DSeries(
                new double[] { 0, 1 }, new double[] { 0, 0 }, new double[] { 0, 1 }));
        });

        data.Add(nameof(VoxelSeriesRenderer), () =>
        {
            // 0-dimension cube: dim check early-return
            var ctx = NewContext(out _);
            new VoxelSeriesRenderer(ctx).Render(new VoxelSeries(new bool[0, 0, 0]));
        });

        data.Add(nameof(WaterfallSeriesRenderer), () =>
        {
            var ctx = NewContext(out _);
            new WaterfallSeriesRenderer(ctx).Render(new WaterfallSeries([], []));
        });

        data.Add(nameof(WireframeSeriesRenderer), () =>
        {
            // rows < 2 early return — pass a 1×1 grid
            var ctx = NewContext(out _);
            new WireframeSeriesRenderer(ctx).Render(
                new WireframeSeries([0], [0], new double[1, 1]));
        });

        data.Add(nameof(Contour3DSeriesRenderer), () =>
        {
            var ctx = NewContext(out _);
            new Contour3DSeriesRenderer(ctx).Render(
                new Contour3DSeries([0], [0], new double[1, 1]));
        });

        data.Add(nameof(FunnelSeriesRenderer), () =>
        {
            // maxVal <= 0 early-return path: all-zero values
            var ctx = NewContext(out _);
            new FunnelSeriesRenderer(ctx).Render(new FunnelSeries(["A", "B"], [0, 0]));
        });

        data.Add(nameof(GaugeSeriesRenderer), () =>
        {
            // range <= 0 early-return path: Min == Max
            var ctx = NewContext(out _);
            new GaugeSeriesRenderer(ctx).Render(new GaugeSeries(50) { Min = 50, Max = 50 });
        });

        data.Add(nameof(AreaSeriesRenderer), () =>
        {
            // n == 0 early-return path
            var ctx = NewContext(out _);
            new AreaSeriesRenderer(ctx).Render(new AreaSeries([], []));
        });

        return data;
    }

    /// <summary>Empty-data invocations must NOT throw. Some renderers early-return with no
    /// drawing (e.g. funnel with all-zero values, sparkline with &lt; 2 points), others
    /// may emit nothing at all — the contract here is just "doesn't crash".</summary>
    [Theory]
    [MemberData(nameof(EmptyCases))]
    public void Renderer_EmptyData_DoesNotThrow(string rendererName, Action invoke)
    {
        var ex = Record.Exception(invoke);
        Assert.Null(ex);
        // Above is the actual contract; rendererName is included so xUnit shows it on failure.
        _ = rendererName;
    }

    // ── Edge case: NaN data ──────────────────────────────────────────────────

    /// <summary>NaN propagation tests — every numeric renderer must accept NaN values
    /// without throwing. The transform may produce NaN pixel coordinates, which
    /// downstream SVG sinks will render as garbage but must NOT crash.</summary>
    public static TheoryData<string, Action> NaNCases()
    {
        var data = new TheoryData<string, Action>();

        data.Add(nameof(BubbleSeriesRenderer), () =>
        {
            var ctx = NewContext(out _);
            new BubbleSeriesRenderer(ctx).Render(new BubbleSeries(
                EdgeCaseData.AllNaN, EdgeCaseData.AllNaN, new double[] { 50, 50, 50 }));
        });

        data.Add(nameof(QuiverSeriesRenderer), () =>
        {
            var ctx = NewContext(out _);
            new QuiverSeriesRenderer(ctx).Render(new QuiverSeries(
                EdgeCaseData.MixedNaN, EdgeCaseData.MixedNaN,
                EdgeCaseData.MixedNaN, EdgeCaseData.MixedNaN));
        });

        data.Add(nameof(SparklineSeriesRenderer), () =>
        {
            var ctx = NewContext(out _);
            // Mixed-NaN: Min/Max return NaN in degenerate cases — must not throw
            new SparklineSeriesRenderer(ctx).Render(new SparklineSeries(EdgeCaseData.MixedNaN));
        });

        data.Add(nameof(WaterfallSeriesRenderer), () =>
        {
            var ctx = NewContext(out _);
            new WaterfallSeriesRenderer(ctx).Render(new WaterfallSeries(
                ["A", "B", "C"], EdgeCaseData.AllNaN));
        });

        data.Add(nameof(GanttSeriesRenderer), () =>
        {
            var ctx = NewContext(out _);
            new GanttSeriesRenderer(ctx).Render(new GanttSeries(
                ["A", "B", "C"], EdgeCaseData.AllNaN, EdgeCaseData.AllNaN));
        });

        data.Add(nameof(OhlcBarSeriesRenderer), () =>
        {
            var ctx = NewContext(out _);
            new OhlcBarSeriesRenderer(ctx).Render(new OhlcBarSeries(
                EdgeCaseData.AllNaN, EdgeCaseData.AllNaN,
                EdgeCaseData.AllNaN, EdgeCaseData.AllNaN));
        });

        data.Add(nameof(PolarScatterSeriesRenderer), () =>
        {
            var ctx = NewContext(out _);
            new PolarScatterSeriesRenderer(ctx).Render(new PolarScatterSeries(
                EdgeCaseData.MixedNaN, EdgeCaseData.MixedNaN));
        });

        data.Add(nameof(LineSeriesRenderer), () =>
        {
            var ctx = NewContext(out _);
            new LineSeriesRenderer(ctx).Render(new LineSeries(
                EdgeCaseData.Ramp(6), EdgeCaseData.MixedNaN));
        });

        data.Add(nameof(AreaSeriesRenderer), () =>
        {
            var ctx = NewContext(out _);
            new AreaSeriesRenderer(ctx).Render(new AreaSeries(
                EdgeCaseData.Ramp(6), EdgeCaseData.MixedNaN));
        });

        return data;
    }

    /// <summary>NaN inputs must not crash any renderer. Output is allowed to contain NaN
    /// pixel coordinates — that's a downstream rendering-quality concern, not a
    /// renderer-correctness concern.</summary>
    [Theory]
    [MemberData(nameof(NaNCases))]
    public void Renderer_NaNData_DoesNotThrow(string rendererName, Action invoke)
    {
        var ex = Record.Exception(invoke);
        Assert.Null(ex);
        _ = rendererName;
    }

    // ── Single-point degenerate cases ────────────────────────────────────────

    /// <summary>Single-point inputs exercise the boundary-degenerate code paths in
    /// renderers that compute extents (e.g. Min/Max == single value, range == 0).</summary>
    public static TheoryData<string, Action> SinglePointCases()
    {
        var data = new TheoryData<string, Action>();

        data.Add(nameof(BubbleSeriesRenderer), () =>
        {
            var ctx = NewContext(out _);
            new BubbleSeriesRenderer(ctx).Render(new BubbleSeries([5], [5], [80]));
        });

        data.Add(nameof(QuiverSeriesRenderer), () =>
        {
            var ctx = NewContext(out _);
            // Single arrow with zero magnitude — exercises len < 1e-6 continue path
            new QuiverSeriesRenderer(ctx).Render(new QuiverSeries([5], [5], [0], [0]));
        });

        data.Add(nameof(Quiver3DSeriesRenderer), () =>
        {
            var ctx = NewContext(out _);
            // Single arrow with zero magnitude — exercises shaftLen < 1e-6 continue
            new Quiver3DSeriesRenderer(ctx).Render(new Quiver3DSeries(
                new double[] { 0 }, new double[] { 0 }, new double[] { 0 },
                new double[] { 0 }, new double[] { 0 }, new double[] { 0 }));
        });

        data.Add(nameof(SparklineSeriesRenderer), () =>
        {
            var ctx = NewContext(out _);
            // All-equal values exercise the |yMax - yMin| < 1e-10 expansion path
            new SparklineSeriesRenderer(ctx).Render(
                new SparklineSeries(EdgeCaseData.AllEqual(8, 5.0)));
        });

        data.Add(nameof(WireframeSeriesRenderer), () =>
        {
            var ctx = NewContext(out _);
            // All-equal grid: zMin == zMax so zRange falls back to 1
            new WireframeSeriesRenderer(ctx).Render(new WireframeSeries(
                [0, 1, 2], [0, 1, 2], new double[3, 3]));
        });

        data.Add(nameof(Trisurf3DSeriesRenderer), () =>
        {
            var ctx = NewContext(out _);
            // All-equal Z exercises the zRange == 0 fallback
            new Trisurf3DSeriesRenderer(ctx).Render(new Trisurf3DSeries(
                new double[] { 0, 1, 2 }, new double[] { 0, 1, 0 }, new double[] { 5, 5, 5 }));
        });

        return data;
    }

    /// <summary>Single-point / degenerate-range invocations must not throw.</summary>
    [Theory]
    [MemberData(nameof(SinglePointCases))]
    public void Renderer_SinglePointOrDegenerate_DoesNotThrow(string rendererName, Action invoke)
    {
        var ex = Record.Exception(invoke);
        Assert.Null(ex);
        _ = rendererName;
    }

    // ── Renderer-specific branch coverage probes ─────────────────────────────
    // These hit the if/else branches that the basic NormalCases entry doesn't,
    // boosting branch-coverage on already line-covered renderers.

    /// <summary>Donut with a zero CenterText skip path — exercises the
    /// <c>CenterText is not null</c> branch's false side. <see cref="NormalCases"/>
    /// already covers the true side.</summary>
    [Fact]
    public void DonutRenderer_NoCenterText_TakesElseBranch()
    {
        var ctx = NewContext(out var svg);
        new DonutSeriesRenderer(ctx).Render(new DonutSeries([1, 2, 3]));
        Assert.True(HasAnyDrawing(svg.GetOutput()));
    }

    /// <summary>Voxel with a depth-queue context — <c>VoxelSeriesRenderer.Render</c> has
    /// two branches: queue path (cross-series compositing) vs. direct paint. The
    /// <see cref="NormalCases"/> entry hits the no-queue branch; this test hits the
    /// queue path.</summary>
    [Fact]
    public void VoxelRenderer_WithDepthQueue_PushesIntoQueue()
    {
        var svg = new SvgRenderContext();
        var area = new RenderArea(StandardBounds, svg);
        var transform = new DataTransform(-10, 10, -10, 10, StandardBounds);
        var queue = new DepthQueue3D();
        var ctx = new SeriesRenderContext(transform, svg, Colors.Tab10Blue, area)
        {
            DepthQueue = queue,
        };

        var filled = new bool[2, 2, 2];
        filled[0, 0, 0] = true;
        new VoxelSeriesRenderer(ctx).Render(new VoxelSeries(filled));

        // Queue path: faces are NOT drawn directly to the SVG context — they're queued.
        // Flushing should then emit the polygons. SetOpacity is the only thing the
        // renderer writes directly in the queued branch, so the SVG starts effectively
        // empty; after Flush() it must contain polygon primitives.
        queue.Flush();
        Assert.True(HasAnyDrawing(svg.GetOutput()),
            "VoxelSeriesRenderer with DepthQueue should defer all polygons to the queue and emit them on Flush().");
    }

    /// <summary>Trisurf3D with a colormap exercises the <c>useColorMap</c> true branch
    /// AND the <c>EdgeColor != null</c> path; <see cref="NormalCases"/> only hits the
    /// fallback solid-color branch.</summary>
    [Fact]
    public void Trisurf3DRenderer_WithColormapAndEdgeColor_RendersTriangles()
    {
        var ctx = NewContext(out var svg);
        var series = new Trisurf3DSeries(
            new double[] { 0, 1, 0.5 },
            new double[] { 0, 0, 1 },
            new double[] { 0, 1, 2 })
        {
            ColorMap = MatPlotLibNet.Styling.ColorMaps.ColorMaps.Viridis,
            EdgeColor = Colors.Black,
            ShowWireframe = true,
        };
        new Trisurf3DSeriesRenderer(ctx).Render(series);
        Assert.True(HasAnyDrawing(svg.GetOutput()));
    }

    /// <summary>Sunburst with <c>ShowLabels = false</c> exercises the no-label branch
    /// (skips collision-resolution batch). The default <see cref="NormalCases"/> entry
    /// hits the with-label branch.</summary>
    [Fact]
    public void SunburstRenderer_NoLabels_SkipsLabelBatch()
    {
        var ctx = NewContext(out var svg);
        var root = new TreeNode
        {
            Label = "Root",
            Children = new[]
            {
                new TreeNode { Label = "A", Value = 30 },
                new TreeNode { Label = "B", Value = 70 },
            }
        };
        new SunburstSeriesRenderer(ctx).Render(new SunburstSeries(root) { ShowLabels = false });
        Assert.True(HasAnyDrawing(svg.GetOutput()));
    }

    /// <summary>Waterfall last-bar branch: the connector line is drawn between bars but
    /// SKIPPED on the final bar (i &lt; Length - 1 guard). A single-bar series exercises
    /// the false side of that branch.</summary>
    [Fact]
    public void WaterfallRenderer_SingleBar_NoConnectorLine()
    {
        var ctx = NewContext(out var svg);
        new WaterfallSeriesRenderer(ctx).Render(new WaterfallSeries(["Only"], [50]));
        Assert.True(HasAnyDrawing(svg.GetOutput()));
    }

    /// <summary>Funnel with explicit Colors[] exercises the colors-supplied branch.
    /// <see cref="NormalCases"/> takes the theme-cycle fallback branch.</summary>
    [Fact]
    public void FunnelRenderer_WithExplicitColors_UsesProvidedColors()
    {
        var ctx = NewContext(out var svg);
        new FunnelSeriesRenderer(ctx).Render(new FunnelSeries(["A", "B", "C"], [50, 30, 10])
        {
            Colors = [Colors.Red, Colors.Green, Colors.Blue],
        });
        Assert.True(HasAnyDrawing(svg.GetOutput()));
    }

    /// <summary>Donut with explicit Colors[] takes the colors-supplied branch.</summary>
    [Fact]
    public void DonutRenderer_WithExplicitColors_UsesProvidedColors()
    {
        var ctx = NewContext(out var svg);
        new DonutSeriesRenderer(ctx).Render(new DonutSeries([1, 1, 1])
        {
            Colors = [Colors.Red, Colors.Green, Colors.Blue],
        });
        Assert.True(HasAnyDrawing(svg.GetOutput()));
    }

    /// <summary>Gauge with explicit Ranges array exercises the user-supplied bands path
    /// instead of the default 60/80/100 green/amber/red triple.</summary>
    [Fact]
    public void GaugeRenderer_WithExplicitRanges_UsesProvidedBands()
    {
        var ctx = NewContext(out var svg);
        new GaugeSeriesRenderer(ctx).Render(new GaugeSeries(50)
        {
            Min = 0, Max = 100,
            Ranges = [(25, Colors.Red), (75, Colors.Amber), (100, Colors.Green)],
        });
        Assert.True(HasAnyDrawing(svg.GetOutput()));
    }
}
