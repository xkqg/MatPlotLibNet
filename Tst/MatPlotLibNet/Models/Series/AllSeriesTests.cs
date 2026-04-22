// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Models.Series.Streaming;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies common <see cref="ISeries"/> behavior across all series types.</summary>
public class AllSeriesTests
{
    /// <summary>Minimal <see cref="IAxesContext"/> for series whose <c>ComputeDataRange</c>
    /// reads context fields (e.g. CandlestickSeries, WaterfallSeries, GanttSeries).</summary>
    private sealed class NullAxesContext : IAxesContext
    {
        public double? XAxisMin => null;
        public double? XAxisMax => null;
        public double? YAxisMin => null;
        public double? YAxisMax => null;
        public BarMode BarMode => BarMode.Grouped;
        public IReadOnlyList<ISeries> AllSeries => [];
    }

    /// <summary>Phase Q (2026-04-19): companion context with all four axis bounds set so the
    /// non-null branch of every <c>context.XAxisMin ?? default</c> in <c>ComputeDataRange</c>
    /// actually executes. Pre-Q the only available context was <see cref="NullAxesContext"/>,
    /// which left those branches unhit and pinned multiple series at 100% line / 50% branch
    /// (CandlestickSeries, GanttSeries, OhlcBarSeries, Contour3D / Line3D / Surface /
    /// Trisurf3D / ResidualSeries — every series whose ComputeDataRange consults the
    /// axes context for its primary axis bounds).</summary>
    private sealed class BoundedAxesContext : IAxesContext
    {
        public double? XAxisMin => -100.0;
        public double? XAxisMax => 100.0;
        public double? YAxisMin => -100.0;
        public double? YAxisMax => 100.0;
        public BarMode BarMode => BarMode.Grouped;
        public IReadOnlyList<ISeries> AllSeries => [];
    }

    /// <summary>Helper: factory for a populated streaming line series (so its data range is non-null).</summary>
    private static StreamingLineSeries MakeStreamingLine()
    {
        var s = new StreamingLineSeries(capacity: 16);
        s.AppendPoint(0.0, 1.0);
        s.AppendPoint(1.0, 2.0);
        s.AppendPoint(2.0, 3.0);
        return s;
    }

    /// <summary>Helper: factory for a populated streaming scatter series.</summary>
    private static StreamingScatterSeries MakeStreamingScatter()
    {
        var s = new StreamingScatterSeries(capacity: 16);
        s.AppendPoint(0.0, 1.0);
        s.AppendPoint(1.0, 2.0);
        return s;
    }

    /// <summary>Helper: factory for a populated streaming signal series.</summary>
    private static StreamingSignalSeries MakeStreamingSignal()
    {
        var s = new StreamingSignalSeries(capacity: 16);
        s.AppendSample(1.0);
        s.AppendSample(2.0);
        return s;
    }

    /// <summary>Helper: factory for a populated streaming candlestick series.</summary>
    private static StreamingCandlestickSeries MakeStreamingCandlestick()
    {
        var s = new StreamingCandlestickSeries(capacity: 16);
        s.AppendBar(10, 15, 8, 13);
        s.AppendBar(13, 18, 11, 16);
        return s;
    }

    public static TheoryData<ISeries, string> AllSeriesInstances => new()
    {
        { new LineSeries([1.0], [2.0]), nameof(LineSeries) },
        { new ScatterSeries([1.0], [2.0]), nameof(ScatterSeries) },
        { new BarSeries(["A"], [1.0]), nameof(BarSeries) },
        { new HistogramSeries([1.0, 2.0, 3.0]), nameof(HistogramSeries) },
        { new PieSeries([30.0, 70.0]), nameof(PieSeries) },
        { new HeatmapSeries(new double[,] { { 1, 2 }, { 3, 4 } }), nameof(HeatmapSeries) },
        { new Histogram2DSeries([1.0, 2.0, 3.0], [4.0, 5.0, 6.0]), nameof(Histogram2DSeries) },
        { new BoxSeries([[1.0, 2.0, 3.0]]), nameof(BoxSeries) },
        { new ViolinSeries([[1.0, 2.0, 3.0]]), nameof(ViolinSeries) },
        { new KdeSeries([1.0, 2.0, 3.0]), nameof(KdeSeries) },
        { new RegressionSeries([1.0, 2.0, 3.0], [1.0, 2.0, 3.0]), nameof(RegressionSeries) },
        { new HexbinSeries([1.0, 2.0, 3.0], [1.0, 2.0, 3.0]), nameof(HexbinSeries) },
        { new ContourSeries([1.0, 2.0], [1.0, 2.0], new double[,] { { 1, 2 }, { 3, 4 } }), nameof(ContourSeries) },
        { new ContourfSeries([1.0, 2.0], [1.0, 2.0], new double[,] { { 1, 2 }, { 3, 4 } }), nameof(ContourfSeries) },
        { new StemSeries([1.0, 2.0], [3.0, 4.0]), nameof(StemSeries) },
        { new AreaSeries([1.0, 2.0], [3.0, 4.0]), nameof(AreaSeries) },
        { new StepSeries([1.0, 2.0], [3.0, 4.0]), nameof(StepSeries) },
        { new EcdfSeries([3.0, 1.0, 2.0]), nameof(EcdfSeries) },
        { new ErrorBarSeries([1.0, 2.0], [3.0, 4.0], [0.1, 0.1], [0.2, 0.2]), nameof(ErrorBarSeries) },
        { new CandlestickSeries([10.0], [15.0], [8.0], [13.0]), nameof(CandlestickSeries) },
        { new QuiverSeries([1.0], [2.0], [0.5], [0.5]), nameof(QuiverSeries) },
        { new StreamplotSeries([0.0, 1.0], [0.0, 1.0], new double[,] { { 1, 0 }, { 0, 1 } }, new double[,] { { 0, 1 }, { 1, 0 } }), nameof(StreamplotSeries) },
        { new RadarSeries(["A", "B", "C"], [1.0, 2.0, 3.0]), nameof(RadarSeries) },
        { new DonutSeries([30.0, 70.0]), nameof(DonutSeries) },
        { new BubbleSeries([1.0], [2.0], [10.0]), nameof(BubbleSeries) },
        { new OhlcBarSeries([10.0], [15.0], [8.0], [13.0]), nameof(OhlcBarSeries) },
        { new WaterfallSeries(["A"], [10.0]), nameof(WaterfallSeries) },
        { new FunnelSeries(["A"], [10.0]), nameof(FunnelSeries) },
        { new GanttSeries(["A"], [0.0], [1.0]), nameof(GanttSeries) },
        { new GaugeSeries(50), nameof(GaugeSeries) },
        { new ProgressBarSeries(0.5), nameof(ProgressBarSeries) },
        { new SparklineSeries([1.0, 2.0, 3.0]), nameof(SparklineSeries) },
        { new TreemapSeries(new TreeNode { Label = "Root", Children = [new TreeNode { Label = "A", Value = 10 }] }), nameof(TreemapSeries) },
        { new SunburstSeries(new TreeNode { Label = "Root", Children = [new TreeNode { Label = "A", Value = 10 }] }), nameof(SunburstSeries) },
        { new SankeySeries([new SankeyNode("A"), new SankeyNode("B")], [new SankeyLink(0, 1, 10)]), nameof(SankeySeries) },
        { new PolarLineSeries([1.0, 2.0], [0.0, 1.0]), nameof(PolarLineSeries) },
        { new PolarScatterSeries([1.0], [0.5]), nameof(PolarScatterSeries) },
        { new PolarBarSeries([5.0, 10.0], [0.0, 1.57]), nameof(PolarBarSeries) },
        { new SurfaceSeries([1.0, 2.0], [1.0, 2.0], new double[,] { { 1, 2 }, { 3, 4 } }), nameof(SurfaceSeries) },
        { new WireframeSeries([1.0, 2.0], [1.0, 2.0], new double[,] { { 1, 2 }, { 3, 4 } }), nameof(WireframeSeries) },
        { new Scatter3DSeries(new double[] { 1.0 }, new double[] { 2.0 }, new double[] { 3.0 }), nameof(Scatter3DSeries) },
        { new RugplotSeries(new double[] { 1.0, 2.0, 3.0 }), nameof(RugplotSeries) },
        { new StripplotSeries([[1.0, 2.0], [3.0, 4.0]]), nameof(StripplotSeries) },
        { new EventplotSeries([[1.0, 2.0], [3.0]]), nameof(EventplotSeries) },
        { new BrokenBarSeries([[new(1.0, 2.0), new(4.0, 1.0)]]), nameof(BrokenBarSeries) },
        { new CountSeries(["a", "b", "a"]), nameof(CountSeries) },
        { new PcolormeshSeries(new double[] { 0.0, 1.0, 2.0 }, new double[] { 0.0, 1.0, 2.0 }, new double[,] { { 1, 2 }, { 3, 4 } }), nameof(PcolormeshSeries) },
        { new ResidualSeries(new double[] { 1.0, 2.0, 3.0 }, new double[] { 2.0, 4.0, 6.0 }), nameof(ResidualSeries) },
        { new PointplotSeries([[1.0, 2.0, 3.0], [4.0, 5.0]]), nameof(PointplotSeries) },
        { new SwarmplotSeries([[1.0, 2.0], [3.0, 4.0]]), nameof(SwarmplotSeries) },
        { new SpectrogramSeries(new double[] { 1.0, 0.5, -0.5, -1.0, 0.0, 0.5, 1.0, 0.5 }), nameof(SpectrogramSeries) },
        { new TableSeries([["a", "b"], ["c", "d"]]), nameof(TableSeries) },
        { new TricontourSeries(new double[] { 0.0, 1.0, 0.5 }, new double[] { 0.0, 0.0, 1.0 }, new double[] { 1.0, 2.0, 3.0 }), nameof(TricontourSeries) },
        { new TripcolorSeries(new double[] { 0.0, 1.0, 0.5 }, new double[] { 0.0, 0.0, 1.0 }, new double[] { 1.0, 2.0, 3.0 }), nameof(TripcolorSeries) },
        { new QuiverKeySeries(0.5, 0.9, 1.0, "1 m/s"), nameof(QuiverKeySeries) },
        { new BarbsSeries(new double[] { 1.0, 2.0 }, new double[] { 1.0, 2.0 }, new double[] { 10.0, 20.0 }, new double[] { 45.0, 90.0 }), nameof(BarbsSeries) },
        { new Stem3DSeries(new double[] { 1.0, 2.0 }, new double[] { 1.0, 2.0 }, new double[] { 3.0, 4.0 }), nameof(Stem3DSeries) },
        { new Bar3DSeries(new double[] { 1.0, 2.0 }, new double[] { 1.0, 2.0 }, new double[] { 3.0, 4.0 }), nameof(Bar3DSeries) },
        // ── Batch C extension: 16 missing series types (2026-04-18) ───────────
        { new ImageSeries(new double[,] { { 1, 2 }, { 3, 4 } }), nameof(ImageSeries) },
        { new StackedAreaSeries([1.0, 2.0, 3.0], [[1.0, 2.0, 3.0], [2.0, 3.0, 1.0]]), nameof(StackedAreaSeries) },
        { new PolarHeatmapSeries(new double[,] { { 1, 2 }, { 3, 4 } }, thetaBins: 2, rBins: 2), nameof(PolarHeatmapSeries) },
        { new SignalSeries([1.0, 2.0, 3.0], sampleRate: 100.0), nameof(SignalSeries) },
        { new SignalXYSeries([1.0, 2.0, 3.0], [4.0, 5.0, 6.0]), nameof(SignalXYSeries) },
        { new PlanarBar3DSeries(new double[] { 1.0, 2.0 }, new double[] { 1.0, 2.0 }, new double[] { 3.0, 4.0 }), nameof(PlanarBar3DSeries) },
        { new Text3DSeries([new Text3DAnnotation(1, 2, 3, "A"), new Text3DAnnotation(4, 5, 6, "B")]), nameof(Text3DSeries) },
        { new Line3DSeries(new double[] { 1.0, 2.0 }, new double[] { 1.0, 2.0 }, new double[] { 3.0, 4.0 }), nameof(Line3DSeries) },
        { new Trisurf3DSeries(new double[] { 0.0, 1.0, 0.5 }, new double[] { 0.0, 0.0, 1.0 }, new double[] { 1.0, 2.0, 3.0 }), nameof(Trisurf3DSeries) },
        { new Contour3DSeries(new double[] { 0.0, 1.0 }, new double[] { 0.0, 1.0 }, new double[,] { { 1, 2 }, { 3, 4 } }), nameof(Contour3DSeries) },
        { new Quiver3DSeries(new double[] { 1.0 }, new double[] { 2.0 }, new double[] { 3.0 }, new double[] { 0.5 }, new double[] { 0.5 }, new double[] { 0.5 }), nameof(Quiver3DSeries) },
        { new VoxelSeries(new bool[2, 2, 2] { { { true, false }, { false, true } }, { { false, true }, { true, false } } }), nameof(VoxelSeries) },
        { MakeStreamingLine(), nameof(StreamingLineSeries) },
        { MakeStreamingScatter(), nameof(StreamingScatterSeries) },
        { MakeStreamingSignal(), nameof(StreamingSignalSeries) },
        { MakeStreamingCandlestick(), nameof(StreamingCandlestickSeries) },
    };

    /// <summary>Verifies that Label defaults to null for every series type.</summary>
    [Theory]
    [MemberData(nameof(AllSeriesInstances))]
    public void Label_DefaultsToNull(ISeries series, string _)
        => Assert.Null(series.Label);

    /// <summary>Verifies that Visible defaults to true for every series type.</summary>
    [Theory]
    [MemberData(nameof(AllSeriesInstances))]
    public void Visible_DefaultsToTrue(ISeries series, string _)
        => Assert.True(series.Visible);

    /// <summary>Verifies that ZOrder has the correct default for each series type.
    /// AreaSeries defaults to -1 (renders behind all other series); everything else defaults to 0.</summary>
    [Theory]
    [MemberData(nameof(AllSeriesInstances))]
    public void ZOrder_DefaultsToExpectedValue(ISeries series, string _)
        => Assert.Equal(series is AreaSeries ? -1 : 0, series.ZOrder);

    /// <summary>Verifies that Accept dispatches to the correct visitor method for each series type.</summary>
    [Theory]
    [MemberData(nameof(AllSeriesInstances))]
    public void Accept_DispatchesToCorrectVisitorMethod(ISeries series, string expectedName)
    {
        var visitor = new TestSeriesVisitor();
        series.Accept(visitor, null!);
        Assert.Equal(expectedName, visitor.LastVisited);
    }

    /// <summary>Verifies that Label can be set and read back for every series type.</summary>
    [Theory]
    [MemberData(nameof(AllSeriesInstances))]
    public void Label_CanBeSetAndRead(ISeries series, string _)
    {
        series.Label = "test-label";
        Assert.Equal("test-label", series.Label);
    }

    /// <summary>Verifies that Visible can be set to false for every series type.</summary>
    [Theory]
    [MemberData(nameof(AllSeriesInstances))]
    public void Visible_CanBeSetToFalse(ISeries series, string _)
    {
        series.Visible = false;
        Assert.False(series.Visible);
    }

    /// <summary>Verifies that ZOrder can be set and read back for every series type.</summary>
    [Theory]
    [MemberData(nameof(AllSeriesInstances))]
    public void ZOrder_CanBeSet(ISeries series, string _)
    {
        series.ZOrder = 42;
        Assert.Equal(42, series.ZOrder);
    }

    // ── Consolidated property-default checks (Phase 9 dedup, 2026-04-18) ─────
    // These three tests replace ~50 near-identical per-series test methods that
    // each constructed one series and asserted the same default. The source of
    // truth for "what defaults to null" is now ONE place. Per-series test files
    // keep only TYPE-SPECIFIC behaviour (e.g. AreaSeries.YData2 default).

    /// <summary>Every series implementing <see cref="IHasColor"/> must default to
    /// <c>Color = null</c> so the theme cycler picks the actual colour at render time.</summary>
    [Theory]
    [MemberData(nameof(AllSeriesInstances))]
    public void IHasColor_DefaultsToNull(ISeries series, string _)
    {
        if (series is IHasColor hc) Assert.Null(hc.Color);
    }

    /// <summary>Every series implementing <see cref="IColormappable"/> must default to
    /// <c>ColorMap = null</c> so the axes-level colormap propagates instead.</summary>
    [Theory]
    [MemberData(nameof(AllSeriesInstances))]
    public void IColormappable_DefaultColorMapIsNull(ISeries series, string _)
    {
        if (series is IColormappable cm) Assert.Null(cm.ColorMap);
    }

    /// <summary>Every <see cref="XYSeries"/>-derived series must store the X and Y
    /// data passed to its constructor (i.e. no defensive cloning that breaks
    /// downstream Append-style mutation).</summary>
    [Theory]
    [MemberData(nameof(AllSeriesInstances))]
    public void XYSeries_StoresXAndYData(ISeries series, string _)
    {
        if (series is XYSeries xy)
        {
            Assert.NotNull(xy.XData);
            Assert.NotNull(xy.YData);
            Assert.Equal(xy.XData.Length, xy.YData.Length);
        }
    }

    // ── Cross-cutting Theory tests added in Batch C (2026-04-18) ─────────────
    // Replace dozens of per-series ToSeriesDto / DataRange / interface-default
    // checks with a single Theory each. Keeps the source of truth in ONE place.

    /// <summary>Every series' <see cref="ISeries.ToSeriesDto"/> must round-trip to a
    /// non-null DTO with a non-empty <see cref="Serialization.SeriesDto.Type"/> string,
    /// otherwise persistence and renderer dispatch break.</summary>
    [Theory]
    [MemberData(nameof(AllSeriesInstances))]
    public void ToSeriesDto_RoundTrips(ISeries series, string _)
    {
        var dto = series.ToSeriesDto();
        Assert.NotNull(dto);
        Assert.False(string.IsNullOrEmpty(dto.Type));
    }

    /// <summary>Every series with non-empty data must produce a finite (no NaN, no
    /// infinity) <see cref="DataRangeContribution"/>. Members may be null when the
    /// series legitimately doesn't contribute to that axis (polar series, voxel
    /// series with empty mask, etc.) — those nulls are skipped, not failed on.</summary>
    [Theory]
    [MemberData(nameof(AllSeriesInstances))]
    public void ComputeDataRange_NonEmpty_ProducesFiniteRange(ISeries series, string _)
    {
        var range = series.ComputeDataRange(new NullAxesContext());
        AssertFinite(range.XMin, nameof(range.XMin));
        AssertFinite(range.XMax, nameof(range.XMax));
        AssertFinite(range.YMin, nameof(range.YMin));
        AssertFinite(range.YMax, nameof(range.YMax));
        AssertFinite(range.ZMin, nameof(range.ZMin));
        AssertFinite(range.ZMax, nameof(range.ZMax));
        AssertFinite(range.StickyXMin, nameof(range.StickyXMin));
        AssertFinite(range.StickyXMax, nameof(range.StickyXMax));
        AssertFinite(range.StickyYMin, nameof(range.StickyYMin));
        AssertFinite(range.StickyYMax, nameof(range.StickyYMax));
        AssertFinite(range.StickyZMin, nameof(range.StickyZMin));
        AssertFinite(range.StickyZMax, nameof(range.StickyZMax));

        static void AssertFinite(double? v, string name)
        {
            if (v is null) return;          // null = no contribution → legitimate
            Assert.False(double.IsNaN(v.Value), $"{name} is NaN");
            Assert.False(double.IsInfinity(v.Value), $"{name} is infinite");
        }
    }

    /// <summary>Every series implementing <see cref="IHasMarkerStyle"/> must default to
    /// <c>MarkerStyle.Circle</c> — matplotlib's <c>scatter</c> default.</summary>
    [Theory]
    [MemberData(nameof(AllSeriesInstances))]
    public void IHasMarkerStyle_DefaultsToCircle(ISeries series, string _)
    {
        if (series is IHasMarkerStyle ms) Assert.Equal(MarkerStyle.Circle, ms.MarkerStyle);
    }

    /// <summary>Every series implementing <see cref="IHasAlpha"/> must have <c>Alpha</c>
    /// in the valid <c>[0, 1]</c> range — defaults vary by series type (1.0 for opaque
    /// scatter/bar, 0.3 for translucent KDE/area/violin, etc.) so the assertion is a
    /// range check, not equality. The lower bound is exclusive of NaN; the upper bound
    /// is inclusive of 1.0.</summary>
    [Theory]
    [MemberData(nameof(AllSeriesInstances))]
    public void IHasAlpha_DefaultsToValidRange(ISeries series, string _)
    {
        if (series is IHasAlpha ha)
        {
            Assert.False(double.IsNaN(ha.Alpha), "Alpha defaulted to NaN");
            Assert.InRange(ha.Alpha, 0.0, 1.0);
        }
    }

    /// <summary>Every series implementing <see cref="IHasEdgeColor"/> must default to
    /// <c>EdgeColor = null</c> so the renderer's per-series default stroke is used.</summary>
    [Theory]
    [MemberData(nameof(AllSeriesInstances))]
    public void IHasEdgeColor_DefaultsToNull(ISeries series, string _)
    {
        if (series is IHasEdgeColor ec) Assert.Null(ec.EdgeColor);
    }

    /// <summary>Phase X.2.a (v1.7.2, 2026-04-19) — exercises every renderer end-to-end.
    /// Pre-X the AllSeries Theories covered model-side properties (Label, Visible, Alpha,
    /// ZOrder, ToSeriesDto, ComputeDataRange) but NEVER touched the renderers — so every
    /// `Render` method's empty-data guards, null-color fallbacks, single-point arms etc.
    /// stayed unexercised by the cross-cutting harness. Each series here builds a real
    /// Figure and renders to SVG, hitting the renderer's `Render(series)` method through
    /// the full SvgSeriesRenderer dispatch path. Asserts the SVG is non-empty + valid;
    /// catches render-time exceptions on degenerate-input series. Lifts many renderers
    /// from "ComputeDataRange covered, Render not" → "both covered". 3D / polar / hierarchical
    /// series use their dedicated AddSubPlot variants (none — AddSeries works for all).</summary>
    [Theory]
    [MemberData(nameof(AllSeriesInstances))]
    public void RendersToSvg_WithoutCrash(ISeries series, string _)
    {
        var fig = Plt.Create()
            .WithSize(400, 300)
            .AddSubPlot(1, 1, 1, ax => ax.AddSeries((ChartSeries)series))
            .Build();
        var svg = fig.ToSvg();
        Assert.NotNull(svg);
        Assert.StartsWith("<svg", svg);
        Assert.EndsWith("</svg>", svg.TrimEnd());      // trailing whitespace OK
    }

    /// <summary>Phase X.2.a (v1.7.2, 2026-04-19) — exercises every IHasColor renderer's
    /// `series.Color != null` true branch. The existing IHasColor_DefaultsToNull Theory
    /// covers the null arm; this covers the non-null arm by setting an explicit color
    /// before render. Many renderers fall back to ResolveColor()'s cycler path when Color
    /// is null; the explicit-Color path is otherwise unhit by AllSeries scaffolding.</summary>
    [Theory]
    [MemberData(nameof(AllSeriesInstances))]
    public void RendersToSvg_WithExplicitColor(ISeries series, string _)
    {
        if (series is not IHasColor hc) return;     // non-coloured series → no branch to hit
        hc.Color = Colors.Red;
        var fig = Plt.Create()
            .WithSize(400, 300)
            .AddSubPlot(1, 1, 1, ax => ax.AddSeries((ChartSeries)series))
            .Build();
        var svg = fig.ToSvg();
        Assert.StartsWith("<svg", svg);
    }

    /// <summary>Phase Q Wave 1 (2026-04-19): exercises the non-null branch of every
    /// <c>context.XAxisMin ?? default</c> / <c>context.XAxisMax ?? default</c> in
    /// <c>ComputeDataRange</c>. Pre-Q only <see cref="NullAxesContext"/> was used, so the
    /// non-null branch was unhit on every series whose range computation reads the context —
    /// CandlestickSeries / OhlcBarSeries / GanttSeries / 3D series — pinning each at
    /// 100% line / 50% branch. This Theory adds one assertion per series that the same
    /// finite-range invariant holds when the context DOES provide bounds; together with
    /// the existing null-context Theory, both branches are now covered for every series.</summary>
    [Theory]
    [MemberData(nameof(AllSeriesInstances))]
    public void ComputeDataRange_WithBoundedAxisContext_StillProducesFiniteRange(ISeries series, string _)
    {
        var range = series.ComputeDataRange(new BoundedAxesContext());
        AssertFinite(range.XMin, nameof(range.XMin));
        AssertFinite(range.XMax, nameof(range.XMax));
        AssertFinite(range.YMin, nameof(range.YMin));
        AssertFinite(range.YMax, nameof(range.YMax));
        AssertFinite(range.ZMin, nameof(range.ZMin));
        AssertFinite(range.ZMax, nameof(range.ZMax));
        AssertFinite(range.StickyXMin, nameof(range.StickyXMin));
        AssertFinite(range.StickyXMax, nameof(range.StickyXMax));
        AssertFinite(range.StickyYMin, nameof(range.StickyYMin));
        AssertFinite(range.StickyYMax, nameof(range.StickyYMax));
        AssertFinite(range.StickyZMin, nameof(range.StickyZMin));
        AssertFinite(range.StickyZMax, nameof(range.StickyZMax));

        static void AssertFinite(double? v, string name)
        {
            if (v is null) return;
            Assert.False(double.IsNaN(v.Value), $"{name} is NaN");
            Assert.False(double.IsInfinity(v.Value), $"{name} is infinite");
        }
    }
}
