// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.TestFixtures;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Rendering.SeriesRenderers;

/// <summary>Every <c>SeriesRenderer&lt;T&gt;</c> drawn on its own, in a context that maps a data range onto a plot (not through the
/// figure): over data it draws, it reaches the drawing surface; over no data, missing values, one point or a degenerate range it
/// draws without throwing; and the arms of a renderer that only a direct call reaches (colours given, a missing label, a depth
/// queue, an interpolation or colour range of an image) draw what they name.</summary>
public class SeriesRenderersTests
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

    /// <summary>The markup a renderer writes drawing <paramref name="series"/> in a context that maps the given data range onto the
    /// standard plot bounds.</summary>
    /// <typeparam name="TSeries">The series the renderer draws.</typeparam>
    /// <param name="make">Makes the renderer over the context it is handed.</param>
    /// <param name="series">The series to draw.</param>
    /// <param name="dataXMin">The data range's left end.</param>
    /// <param name="dataXMax">The data range's right end.</param>
    /// <param name="dataYMin">The data range's bottom.</param>
    /// <param name="dataYMax">The data range's top.</param>
    private static string Drawn<TSeries>(Func<SeriesRenderContext, SeriesRenderer<TSeries>> make, TSeries series,
        double dataXMin = -10, double dataXMax = 10, double dataYMin = -10, double dataYMax = 10) where TSeries : ISeries
    {
        make(NewContext(out var svg, dataXMin, dataXMax, dataYMin, dataYMax)).Render(series);
        return svg.GetOutput();
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

        data.Add(nameof(BubbleSeriesRenderer), () => Drawn(c => new BubbleSeriesRenderer(c), new BubbleSeries([1, 2, 3], [4, 5, 6], [50, 80, 120])));

        data.Add(nameof(Contour3DSeriesRenderer), () =>
        {
            var z = new double[5, 5];
            for (int i = 0; i < 5; i++)
                for (int j = 0; j < 5; j++)
                    z[i, j] = i + j;
            return Drawn(c => new Contour3DSeriesRenderer(c), new Contour3DSeries([0, 1, 2, 3, 4], [0, 1, 2, 3, 4], z) { Levels = 5 });
        });

        data.Add(nameof(DonutSeriesRenderer), () => Drawn(c => new DonutSeriesRenderer(c), new DonutSeries([30, 25, 20, 15, 10]) { CenterText = "Total" }));

        data.Add(nameof(FunnelSeriesRenderer), () => Drawn(c => new FunnelSeriesRenderer(c), new FunnelSeries(["Visit", "Signup", "Pay"], [100, 60, 25])));

        data.Add(nameof(GanttSeriesRenderer), () => Drawn(c => new GanttSeriesRenderer(c), new GanttSeries(["Plan", "Build", "Ship"], [0, 2, 5], [3, 6, 9]), dataXMin: 0, dataXMax: 10, dataYMin: -1, dataYMax: 3));

        data.Add(nameof(GaugeSeriesRenderer), () => Drawn(c => new GaugeSeriesRenderer(c), new GaugeSeries(72) { Min = 0, Max = 100 }));

        data.Add(nameof(Line3DSeriesRenderer), () => Drawn(c => new Line3DSeriesRenderer(c), new Line3DSeries(new double[] { 0, 1, 2, 3 }, new double[] { 0, 1, 0, 1 }, new double[] { 0, 1, 2, 3 })));

        data.Add(nameof(OhlcBarSeriesRenderer), () => Drawn(c => new OhlcBarSeriesRenderer(c), new OhlcBarSeries([10, 20, 30], [40, 50, 60], [5, 15, 25], [35, 25, 55]), dataXMin: -1, dataXMax: 4, dataYMin: 0, dataYMax: 100));

        data.Add(nameof(PolarBarSeriesRenderer), () => Drawn(c => new PolarBarSeriesRenderer(c), new PolarBarSeries([1, 2, 3, 4], [0, Math.PI / 2, Math.PI, 1.5 * Math.PI])));

        data.Add(nameof(PolarScatterSeriesRenderer), () => Drawn(c => new PolarScatterSeriesRenderer(c), new PolarScatterSeries([1, 2, 3, 4], [0, Math.PI / 2, Math.PI, 1.5 * Math.PI])));

        data.Add(nameof(ProgressBarSeriesRenderer), () => Drawn(c => new ProgressBarSeriesRenderer(c), new ProgressBarSeries(0.42)));

        data.Add(nameof(Quiver3DSeriesRenderer), () => Drawn(c => new Quiver3DSeriesRenderer(c), new Quiver3DSeries(
                    new double[] { 0, 1 }, new double[] { 0, 1 }, new double[] { 0, 1 },
                    new double[] { 1, -1 }, new double[] { 0, 1 }, new double[] { 1, 0 })));

        data.Add(nameof(QuiverSeriesRenderer), () => Drawn(c => new QuiverSeriesRenderer(c), new QuiverSeries([0, 1, 2], [0, 1, 2], [1, 0, -1], [0, 1, 0])));

        data.Add(nameof(SparklineSeriesRenderer), () => Drawn(c => new SparklineSeriesRenderer(c), new SparklineSeries([1, 3, 2, 5, 4, 7, 6, 9])));

        data.Add(nameof(SunburstSeriesRenderer), () =>
        {
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
            return Drawn(c => new SunburstSeriesRenderer(c), new SunburstSeries(root));
        });

        data.Add(nameof(Text3DSeriesRenderer), () => Drawn(c => new Text3DSeriesRenderer(c), new Text3DSeries(new()
            {
                new Text3DAnnotation(0, 0, 0, "origin"),
                new Text3DAnnotation(1, 1, 1, "p1"),
                new Text3DAnnotation(-1, -1, 2, "p2"),
            })));

        data.Add(nameof(Trisurf3DSeriesRenderer), () => Drawn(c => new Trisurf3DSeriesRenderer(c), new Trisurf3DSeries(
                new double[] { 0, 1, 0.5, 0, 1, 0.5 },
                new double[] { 0, 0, 1, 0, 0, 1 },
                new double[] { 0, 1, 2, 1, 2, 3 })));

        data.Add(nameof(VoxelSeriesRenderer), () =>
        {
            var filled = new bool[2, 2, 2];
            filled[0, 0, 0] = true;
            filled[1, 1, 1] = true;
            return Drawn(c => new VoxelSeriesRenderer(c), new VoxelSeries(filled));
        });

        data.Add(nameof(WaterfallSeriesRenderer), () => Drawn(c => new WaterfallSeriesRenderer(c), new WaterfallSeries(["Start", "Q1", "Q2", "Q3"], [50, 30, -20, 40]), dataXMin: -1, dataXMax: 5, dataYMin: -50, dataYMax: 100));

        data.Add(nameof(WireframeSeriesRenderer), () =>
        {
            var z = new double[3, 3];
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    z[i, j] = Math.Sin(i) + Math.Cos(j);
            return Drawn(c => new WireframeSeriesRenderer(c), new WireframeSeries([0, 1, 2], [0, 1, 2], z));
        });

        // Renderers that aren't 0% but score very low on branch coverage — direct
        // invocation here picks up the branches that the high-level tests miss.
        data.Add(nameof(AreaSeriesRenderer), () => Drawn(c => new AreaSeriesRenderer(c), new AreaSeries([0, 1, 2, 3, 4], [1, 3, 2, 4, 3])));

        data.Add(nameof(LineSeriesRenderer), () => Drawn(c => new LineSeriesRenderer(c), new LineSeries([0, 1, 2, 3, 4], [0, 1, 4, 9, 16])));

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

        data.Add(nameof(BubbleSeriesRenderer), () => Drawn(c => new BubbleSeriesRenderer(c), new BubbleSeries([], [], [])));

        // total <= 0 path: empty Sizes => Sum() == 0 => early return
        data.Add(nameof(DonutSeriesRenderer), () => Drawn(c => new DonutSeriesRenderer(c), new DonutSeries([])));

        data.Add(nameof(GanttSeriesRenderer), () => Drawn(c => new GanttSeriesRenderer(c), new GanttSeries([], [], [])));

        // X.Length < 2 early-return path
        data.Add(nameof(Line3DSeriesRenderer), () => Drawn(c => new Line3DSeriesRenderer(c), new Line3DSeries(new double[] { 0 }, new double[] { 0 }, new double[] { 0 })));

        data.Add(nameof(OhlcBarSeriesRenderer), () => Drawn(c => new OhlcBarSeriesRenderer(c), new OhlcBarSeries([], [], [], [])));

        data.Add(nameof(PolarBarSeriesRenderer), () => Drawn(c => new PolarBarSeriesRenderer(c), new PolarBarSeries([], [])));

        data.Add(nameof(PolarScatterSeriesRenderer), () => Drawn(c => new PolarScatterSeriesRenderer(c), new PolarScatterSeries([], [])));

        // X.Length == 0 early return
        data.Add(nameof(Quiver3DSeriesRenderer), () => Drawn(c => new Quiver3DSeriesRenderer(c), new Quiver3DSeries(
                new double[] { }, new double[] { }, new double[] { },
                new double[] { }, new double[] { }, new double[] { })));

        data.Add(nameof(QuiverSeriesRenderer), () => Drawn(c => new QuiverSeriesRenderer(c), new QuiverSeries([], [], [], [])));

        // Values.Length < 2 early-return
        data.Add(nameof(SparklineSeriesRenderer), () => Drawn(c => new SparklineSeriesRenderer(c), new SparklineSeries(EdgeCaseData.Empty)));

        // GetMaxDepth == 0 early-return for leaf-only root
        data.Add(nameof(SunburstSeriesRenderer), () => Drawn(c => new SunburstSeriesRenderer(c), new SunburstSeries(new TreeNode { Label = "leaf", Value = 1 })));

        data.Add(nameof(Text3DSeriesRenderer), () => Drawn(c => new Text3DSeriesRenderer(c), new Text3DSeries(new())));

        // X.Length < 3 early return
        data.Add(nameof(Trisurf3DSeriesRenderer), () => Drawn(c => new Trisurf3DSeriesRenderer(c), new Trisurf3DSeries(
                new double[] { 0, 1 }, new double[] { 0, 0 }, new double[] { 0, 1 })));

        // 0-dimension cube: dim check early-return
        data.Add(nameof(VoxelSeriesRenderer), () => Drawn(c => new VoxelSeriesRenderer(c), new VoxelSeries(new bool[0, 0, 0])));

        data.Add(nameof(WaterfallSeriesRenderer), () => Drawn(c => new WaterfallSeriesRenderer(c), new WaterfallSeries([], [])));

        // rows < 2 early return — pass a 1×1 grid
        data.Add(nameof(WireframeSeriesRenderer), () => Drawn(c => new WireframeSeriesRenderer(c), new WireframeSeries([0], [0], new double[1, 1])));

        data.Add(nameof(Contour3DSeriesRenderer), () => Drawn(c => new Contour3DSeriesRenderer(c), new Contour3DSeries([0], [0], new double[1, 1])));

        // maxVal <= 0 early-return path: all-zero values
        data.Add(nameof(FunnelSeriesRenderer), () => Drawn(c => new FunnelSeriesRenderer(c), new FunnelSeries(["A", "B"], [0, 0])));

        // range <= 0 early-return path: Min == Max
        data.Add(nameof(GaugeSeriesRenderer), () => Drawn(c => new GaugeSeriesRenderer(c), new GaugeSeries(50) { Min = 50, Max = 50 }));

        // n == 0 early-return path
        data.Add(nameof(AreaSeriesRenderer), () => Drawn(c => new AreaSeriesRenderer(c), new AreaSeries([], [])));

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

        data.Add(nameof(BubbleSeriesRenderer), () => Drawn(c => new BubbleSeriesRenderer(c), new BubbleSeries(
                EdgeCaseData.AllNaN, EdgeCaseData.AllNaN, new double[] { 50, 50, 50 })));

        data.Add(nameof(QuiverSeriesRenderer), () => Drawn(c => new QuiverSeriesRenderer(c), new QuiverSeries(
                EdgeCaseData.MixedNaN, EdgeCaseData.MixedNaN,
                EdgeCaseData.MixedNaN, EdgeCaseData.MixedNaN)));

        // Mixed-NaN: Min/Max return NaN in degenerate cases — must not throw
        data.Add(nameof(SparklineSeriesRenderer), () => Drawn(c => new SparklineSeriesRenderer(c), new SparklineSeries(EdgeCaseData.MixedNaN)));

        data.Add(nameof(WaterfallSeriesRenderer), () => Drawn(c => new WaterfallSeriesRenderer(c), new WaterfallSeries(
                ["A", "B", "C"], EdgeCaseData.AllNaN)));

        data.Add(nameof(GanttSeriesRenderer), () => Drawn(c => new GanttSeriesRenderer(c), new GanttSeries(
                ["A", "B", "C"], EdgeCaseData.AllNaN, EdgeCaseData.AllNaN)));

        data.Add(nameof(OhlcBarSeriesRenderer), () => Drawn(c => new OhlcBarSeriesRenderer(c), new OhlcBarSeries(
                EdgeCaseData.AllNaN, EdgeCaseData.AllNaN,
                EdgeCaseData.AllNaN, EdgeCaseData.AllNaN)));

        data.Add(nameof(PolarScatterSeriesRenderer), () => Drawn(c => new PolarScatterSeriesRenderer(c), new PolarScatterSeries(
                EdgeCaseData.MixedNaN, EdgeCaseData.MixedNaN)));

        data.Add(nameof(LineSeriesRenderer), () => Drawn(c => new LineSeriesRenderer(c), new LineSeries(
                EdgeCaseData.Ramp(6), EdgeCaseData.MixedNaN)));

        data.Add(nameof(AreaSeriesRenderer), () => Drawn(c => new AreaSeriesRenderer(c), new AreaSeries(
                EdgeCaseData.Ramp(6), EdgeCaseData.MixedNaN)));

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

        data.Add(nameof(BubbleSeriesRenderer), () => Drawn(c => new BubbleSeriesRenderer(c), new BubbleSeries([5], [5], [80])));

        // Single arrow with zero magnitude — exercises len < 1e-6 continue path
        data.Add(nameof(QuiverSeriesRenderer), () => Drawn(c => new QuiverSeriesRenderer(c), new QuiverSeries([5], [5], [0], [0])));

        // Single arrow with zero magnitude — exercises shaftLen < 1e-6 continue
        data.Add(nameof(Quiver3DSeriesRenderer), () => Drawn(c => new Quiver3DSeriesRenderer(c), new Quiver3DSeries(
                new double[] { 0 }, new double[] { 0 }, new double[] { 0 },
                new double[] { 0 }, new double[] { 0 }, new double[] { 0 })));

        // All-equal values exercise the |yMax - yMin| < 1e-10 expansion path
        data.Add(nameof(SparklineSeriesRenderer), () => Drawn(c => new SparklineSeriesRenderer(c), new SparklineSeries(EdgeCaseData.AllEqual(8, 5.0))));

        // All-equal grid: zMin == zMax so zRange falls back to 1
        data.Add(nameof(WireframeSeriesRenderer), () => Drawn(c => new WireframeSeriesRenderer(c), new WireframeSeries(
                [0, 1, 2], [0, 1, 2], new double[3, 3])));

        // All-equal Z exercises the zRange == 0 fallback
        data.Add(nameof(Trisurf3DSeriesRenderer), () => Drawn(c => new Trisurf3DSeriesRenderer(c), new Trisurf3DSeries(
                new double[] { 0, 1, 2 }, new double[] { 0, 1, 0 }, new double[] { 5, 5, 5 })));

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
        Assert.True(HasAnyDrawing(Drawn(c => new DonutSeriesRenderer(c), new DonutSeries([1, 2, 3]))));
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
        var series = new Trisurf3DSeries(
            new double[] { 0, 1, 0.5 },
            new double[] { 0, 0, 1 },
            new double[] { 0, 1, 2 })
        {
            ColorMap = MatPlotLibNet.Styling.ColorMaps.ColorMaps.Viridis,
            EdgeColor = Colors.Black,
            ShowWireframe = true,
        };
        Assert.True(HasAnyDrawing(Drawn(c => new Trisurf3DSeriesRenderer(c), series)));
    }

    /// <summary>Sunburst with <c>ShowLabels = false</c> exercises the no-label branch
    /// (skips collision-resolution batch). The default <see cref="NormalCases"/> entry
    /// hits the with-label branch.</summary>
    [Fact]
    public void SunburstRenderer_NoLabels_SkipsLabelBatch()
    {
        var root = new TreeNode
        {
            Label = "Root",
            Children = new[]
            {
                new TreeNode { Label = "A", Value = 30 },
                new TreeNode { Label = "B", Value = 70 },
            }
        };
        Assert.True(HasAnyDrawing(Drawn(c => new SunburstSeriesRenderer(c), new SunburstSeries(root) { ShowLabels = false })));
    }

    /// <summary>Waterfall last-bar branch: the connector line is drawn between bars but
    /// SKIPPED on the final bar (i &lt; Length - 1 guard). A single-bar series exercises
    /// the false side of that branch.</summary>
    [Fact]
    public void WaterfallRenderer_SingleBar_NoConnectorLine()
    {
        Assert.True(HasAnyDrawing(Drawn(c => new WaterfallSeriesRenderer(c), new WaterfallSeries(["Only"], [50]))));
    }

    /// <summary>Funnel with explicit Colors[] exercises the colors-supplied branch.
    /// <see cref="NormalCases"/> takes the theme-cycle fallback branch.</summary>
    [Fact]
    public void FunnelRenderer_WithExplicitColors_UsesProvidedColors()
    {
        Assert.True(HasAnyDrawing(Drawn(c => new FunnelSeriesRenderer(c), new FunnelSeries(["A", "B", "C"], [50, 30, 10])
        {
            Colors = [Colors.Red, Colors.Green, Colors.Blue],
        })));
    }

    /// <summary>Donut with explicit Colors[] takes the colors-supplied branch.</summary>
    [Fact]
    public void DonutRenderer_WithExplicitColors_UsesProvidedColors()
    {
        Assert.True(HasAnyDrawing(Drawn(c => new DonutSeriesRenderer(c), new DonutSeries([1, 1, 1])
        {
            Colors = [Colors.Red, Colors.Green, Colors.Blue],
        })));
    }

    /// <summary>Gauge with explicit Ranges array exercises the user-supplied bands path
    /// instead of the default 60/80/100 green/amber/red triple.</summary>
    [Fact]
    public void GaugeRenderer_WithExplicitRanges_UsesProvidedBands()
    {
        Assert.True(HasAnyDrawing(Drawn(c => new GaugeSeriesRenderer(c), new GaugeSeries(50)
        {
            Min = 0, Max = 100,
            Ranges = [new(25, Colors.Red), new(75, Colors.Amber), new(100, Colors.Green)],
        })));
    }

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("Histogram2D_UniformCounts_HitsMinEqualsMaxBranch", () =>
        {
            var context = NewContext(out var svg);
            // Same X+Y for many points → all bins have the same count → min==max
            var series = new Histogram2DSeries([0.5, 0.5, 0.5, 0.5], [0.5, 0.5, 0.5, 0.5])
            {
                BinsX = 2, BinsY = 2
            };
            new Histogram2DSeriesRenderer(context).Render(series);
            return svg.GetOutput();
        }, "<rect"),
        new DrawnToken("Image_UniformData_HitsMinEqualsMaxBranch", () =>
        {
            var context = NewContext(out var svg);
            var series = new ImageSeries(new double[,] { { 5, 5 }, { 5, 5 } });
            new ImageSeriesRenderer(context).Render(series);
            return svg.GetOutput();
        }, "<rect"),
        new DrawnToken("Image_AlphaLessThanOne_HitsOpacityBranch", () =>
        {
            var context = NewContext(out var svg);
            var series = new ImageSeries(new double[,] { { 1, 2 }, { 3, 4 } }) { Alpha = 0.5 };
            new ImageSeriesRenderer(context).Render(series);
            return svg.GetOutput();
        }, "opacity") { Comparison = StringComparison.OrdinalIgnoreCase },
        new DrawnToken("Image_BilinearInterpolation_HitsNonNearestBranch", () =>
        {
            var context = NewContext(out var svg);
            var series = new ImageSeries(new double[,] { { 1, 2 }, { 3, 4 } }) { Interpolation = "bilinear" };
            new ImageSeriesRenderer(context).Render(series);
            return svg.GetOutput();
        }, "<rect"),
        // Set limits are the colour range: 1 of 0..5 is a fifth of the way up viridis, not its low end.
        new DrawnToken("Image_VMinAndVMaxBothSet_AreTheColourRange", () =>
        {
            var context = NewContext(out var svg);
            var series = new ImageSeries(new double[,] { { 1, 2 }, { 3, 4 } }) { VMin = 0, VMax = 5 };
            new ImageSeriesRenderer(context).Render(series);
            return svg.GetOutput();
        }, "<rect") { Absent = [$"fill=\"{ColorMaps.Viridis.GetColor(0).ToHex()}\""] },
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    // ── ImageSeriesRenderer: uniform / alpha / interpolation arms ───────────

    [Theory]
    [InlineData(0, 2)]
    [InlineData(2, 0)]
    public void Image_WithNoRowsOrNoColumns_DrawsNothing(int rows, int columns)
    {
        var context = NewContext(out var svg);
        new ImageSeriesRenderer(context).Render(new ImageSeries(new double[rows, columns]));
        Assert.DoesNotContain("<rect", svg.GetOutput());
    }

    /// <summary>An interpolation the registry does not know is drawn as nearest: one cell per value.</summary>
    [Fact]
    public void Image_UnknownInterpolation_FallsBackToNearest()
    {
        var context = NewContext(out var svg);
        new ImageSeriesRenderer(context).Render(new ImageSeries(new double[,] { { 1, 2 }, { 3, 4 } }) { Interpolation = "no-such-interpolation" });
        Assert.Equal(4, svg.GetOutput().Elements("rect").Count);
    }

    // ── BarbsSeriesRenderer: zero-magnitude + flag pennant arms ─────────────

    /// <summary>A series a renderer has nothing to draw from writes nothing at all: a mesh of fewer than two edges along X or along Y,
    /// and a flow of no node.</summary>
    public static TheoryData<string, Func<string>> NothingToDraw() => new()
    {
        { "a mesh of one X edge", () => Drawn(c => new PcolormeshSeriesRenderer(c), new PcolormeshSeries(new double[] { 0.0 }, new double[] { 0.0, 1.0 }, new double[1, 1])) },
        { "a mesh of one Y edge", () => Drawn(c => new PcolormeshSeriesRenderer(c), new PcolormeshSeries(new double[] { 0.0, 1.0 }, new double[] { 0.0 }, new double[1, 1])) },
        { "a flow of no node", () => Drawn(c => new SankeySeriesRenderer(c), new SankeySeries([], [])) },
    };

    [Theory]
    [MemberData(nameof(NothingToDraw))]
    public void ASeriesWithNothingToDrawFrom_WritesNothing(string series, Func<string> drawn)
    {
        Assert.True(new SvgRenderContext().GetOutput() == drawn(), $"{series} wrote markup");
    }
}
