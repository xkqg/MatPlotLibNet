// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Models.Series.Streaming;
using MatPlotLibNet.Rendering.Lighting;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.Svg;

/// <summary>Visitor that dispatches each series type to its typed <see cref="SeriesRenderer{T}"/>.</summary>
/// <remarks>Each series type has a dedicated renderer class in <c>Rendering.SeriesRenderers</c>, named once: in the Visit line of
/// its kind, which makes it over this pass' context and hands it the series. A series pass makes a visitor for each series it draws
/// (<see cref="SeriesPass"/>), so a renderer is made for the one series it draws and nothing is held per kind between two series.</remarks>
internal sealed class SvgSeriesRenderer : ISeriesVisitor, ISeriesRendererHost
{
    private SeriesRenderContext _context;

    /// <summary>The radial scale of the polar axes the series are drawn on, handed to every polar renderer through its context.</summary>
    internal RadialScale? RadialScale { init => _context = _context with { RadialScale = value }; }

    /// <summary>When set, this visitor draws nothing: each series adds the marks it would draw to this collection
    /// (<see cref="SeriesRenderer{T}.AddMarks"/>), so a legend can find the place that covers the fewest of them.</summary>
    internal MarkObstacles.Builder? Marks { get; init; }

    /// <summary>Draws <paramref name="series"/> through its renderer, or, when this visitor collects
    /// (<see cref="Marks"/>), adds its marks instead: the one place the two modes part.</summary>
    private void Dispatch<T>(SeriesRenderer<T> renderer, T series) where T : ISeries
    {
        if (Marks is { } marks) renderer.AddMarks(series, marks);
        else renderer.Render(series);
    }

    /// <summary>Initializes a new <see cref="SvgSeriesRenderer"/> for a single render pass.</summary>
    /// <param name="transform">The coordinate transform mapping data space to pixel space.</param>
    /// <param name="ctx">The drawing target for this render pass.</param>
    /// <param name="seriesColor">The theme cycle color for this series slot.</param>
    /// <param name="theme">The active visual theme, forwarded to per-series renderers for theme-specific defaults.</param>
    /// <param name="cycledProps">Optional additional cycled style properties (line style, marker, width).</param>
    /// <param name="tooltipsEnabled">Whether to emit SVG tooltip group wrappers.</param>
    /// <param name="plotArea">The pixel bounds of the current plot area.</param>
    /// <param name="projection">Unified 3D projection to pass through to 3D series renderers.</param>
    /// <param name="lightSource">Optional light source for per-face lighting on 3D surfaces.</param>
    /// <param name="emit3DData">When true, 3D renderers emit data-v3d attributes for interactive rotation.</param>
    /// <param name="depthQueue">Optional shared depth queue for cross-series back-to-front compositing on 3D axes.</param>
    public SvgSeriesRenderer(DataTransform transform, IRenderContext ctx, Color seriesColor, Styling.Theme theme,
        CycledProperties? cycledProps = null, bool tooltipsEnabled = false, Rect plotArea = default,
        Projection3D? projection = null, ILightSource? lightSource = null, bool emit3DData = false,
        DepthQueue3D? depthQueue = null)
    {
        _context = new SeriesRenderContext(transform, ctx, seriesColor, new RenderArea(plotArea, ctx))
        {
            TooltipsEnabled = tooltipsEnabled,
            CycledProps     = cycledProps,
            Projection3D    = projection,
            LightSource     = lightSource,
            Emit3DData      = emit3DData,
            Theme           = theme,
            DepthQueue      = depthQueue,
        };
    }

    /// <summary>A visitor for one render pass that draws every series with <paramref name="context"/> as it is given: what a
    /// series pass (<see cref="SeriesPass"/>) builds once per series, with nothing copied after it.</summary>
    /// <param name="context">Everything the series' renderers draw with.</param>
    internal SvgSeriesRenderer(SeriesRenderContext context) => _context = context;

    /// <inheritdoc />
    /// <remarks>Made over this pass' context and drawn as every renderer of the library is (<see cref="Dispatch{T}"/>).</remarks>
    void ISeriesRendererHost.Draw<TSeries>(TSeries series, Func<SeriesRenderContext, SeriesRenderer<TSeries>> renderer) =>
        Dispatch(renderer(_context), series);


    // XY family
    /// <inheritdoc />
    public void Visit(LineSeries s, RenderArea a) => Dispatch(new LineSeriesRenderer(_context), s);
    /// <inheritdoc />
    public void Visit(RegressionSeries s, RenderArea a) => Dispatch(new RegressionSeriesRenderer(_context), s);
    /// <inheritdoc />
    public void Visit(ScatterSeries s, RenderArea a) => Dispatch(new ScatterSeriesRenderer(_context), s);
    /// <inheritdoc />
    public void Visit(StepSeries s, RenderArea a) => Dispatch(new StepSeriesRenderer(_context), s);
    /// <inheritdoc />
    public void Visit(AreaSeries s, RenderArea a) => Dispatch(new AreaSeriesRenderer(_context), s);
    /// <inheritdoc />
    public void Visit(ErrorBarSeries s, RenderArea a) => Dispatch(new ErrorBarSeriesRenderer(_context), s);
    /// <inheritdoc />
    public void Visit(BubbleSeries s, RenderArea a) => Dispatch(new BubbleSeriesRenderer(_context), s);
    /// <inheritdoc />
    public void Visit(SparklineSeries s, RenderArea a) => Dispatch(new SparklineSeriesRenderer(_context), s);
    /// <inheritdoc />
    public void Visit(EcdfSeries s, RenderArea a) => Dispatch(new EcdfSeriesRenderer(_context), s);
    /// <inheritdoc />
    public void Visit(StackedAreaSeries s, RenderArea a) => Dispatch(new StackedAreaSeriesRenderer(_context), s);

    // Categorical family
    /// <inheritdoc />
    public void Visit(BarSeries s, RenderArea a) => Dispatch(new BarSeriesRenderer(_context), s);
    /// <inheritdoc />
    public void Visit(HistogramSeries s, RenderArea a) => Dispatch(new HistogramSeriesRenderer(_context), s);
    /// <inheritdoc />
    public void Visit(WaterfallSeries s, RenderArea a) => Dispatch(new WaterfallSeriesRenderer(_context), s);
    /// <inheritdoc />
    public void Visit(FunnelSeries s, RenderArea a) => Dispatch(new FunnelSeriesRenderer(_context), s);
    /// <inheritdoc />
    public void Visit(GanttSeries s, RenderArea a) => Dispatch(new GanttSeriesRenderer(_context), s);
    /// <inheritdoc />
    public void Visit(ProgressBarSeries s, RenderArea a) => Dispatch(new ProgressBarSeriesRenderer(_context), s);
    /// <inheritdoc />
    public void Visit(StatTileSeries s, RenderArea a) => Dispatch(new StatTileSeriesRenderer(_context), s);
    /// <inheritdoc />
    public void Visit(StateTimelineSeries s, RenderArea a) => Dispatch(new StateTimelineSeriesRenderer(_context), s);

    /// <inheritdoc />
    public void Visit(BulletGraphSeries s, RenderArea a) => Dispatch(new BulletGraphSeriesRenderer(_context), s);

    // Circular family
    /// <inheritdoc />
    public void Visit(PieSeries s, RenderArea a) => Dispatch(new PieSeriesRenderer(_context), s);
    /// <inheritdoc />
    public void Visit(RadarSeries s, RenderArea a) => Dispatch(new RadarSeriesRenderer(_context), s);
    /// <inheritdoc />
    public void Visit(DonutSeries s, RenderArea a) => Dispatch(new DonutSeriesRenderer(_context), s);
    /// <inheritdoc />
    public void Visit(GaugeSeries s, RenderArea a) => Dispatch(new GaugeSeriesRenderer(_context), s);

    // Grid family
    /// <inheritdoc />
    public void Visit(HeatmapSeries s, RenderArea a) => Dispatch(new HeatmapSeriesRenderer(_context), s);
    /// <inheritdoc />
    public void Visit(HexbinSeries s, RenderArea a) => Dispatch(new HexbinSeriesRenderer(_context), s);
    /// <inheritdoc />
    public void Visit(ImageSeries s, RenderArea a) => Dispatch(new ImageSeriesRenderer(_context), s);
    /// <inheritdoc />
    public void Visit(Histogram2DSeries s, RenderArea a) => Dispatch(new Histogram2DSeriesRenderer(_context), s);
    /// <inheritdoc />
    public void Visit(ContourSeries s, RenderArea a) => Dispatch(new ContourSeriesRenderer(_context), s);
    /// <inheritdoc />
    public void Visit(ContourfSeries s, RenderArea a) => Dispatch(new ContourfSeriesRenderer(_context), s);

    // Distribution family
    /// <inheritdoc />
    public void Visit(BoxSeries s, RenderArea a) => Dispatch(new BoxSeriesRenderer(_context), s);
    /// <inheritdoc />
    public void Visit(ViolinSeries s, RenderArea a) => Dispatch(new ViolinSeriesRenderer(_context), s);
    /// <inheritdoc />
    public void Visit(KdeSeries s, RenderArea a) => Dispatch(new KdeSeriesRenderer(_context), s);

    // Financial family
    /// <inheritdoc />
    public void Visit(CandlestickSeries s, RenderArea a) => Dispatch(new CandlestickSeriesRenderer(_context), s);
    /// <inheritdoc />
    public void Visit(OhlcBarSeries s, RenderArea a) => Dispatch(new OhlcBarSeriesRenderer(_context), s);

    // Field family
    /// <inheritdoc />
    public void Visit(QuiverSeries s, RenderArea a) => Dispatch(new QuiverSeriesRenderer(_context), s);
    /// <inheritdoc />
    public void Visit(StreamplotSeries s, RenderArea a) => Dispatch(new StreamplotSeriesRenderer(_context), s);
    /// <inheritdoc />
    public void Visit(StemSeries s, RenderArea a) => Dispatch(new StemSeriesRenderer(_context), s);

    // Hierarchical family
    /// <inheritdoc />
    public void Visit(TreemapSeries s, RenderArea a) => Dispatch(new TreemapSeriesRenderer(_context), s);
    /// <inheritdoc />
    public void Visit(SunburstSeries s, RenderArea a) => Dispatch(new SunburstSeriesRenderer(_context), s);
    /// <inheritdoc />
    public void Visit(DendrogramSeries s, RenderArea a) => Dispatch(new DendrogramSeriesRenderer(_context), s);
    /// <inheritdoc />
    public void Visit(ClustermapSeries s, RenderArea a) => Dispatch(new ClustermapSeriesRenderer(_context), s);
    /// <inheritdoc />
    public void Visit(PairGridSeries s, RenderArea a) => Dispatch(new PairGridSeriesRenderer(_context), s);
    /// <inheritdoc />
    public void Visit(NetworkGraphSeries s, RenderArea a) => Dispatch(new NetworkGraphSeriesRenderer(_context), s);
    public void Visit(RelativeRotationSeries s, RenderArea a) => Dispatch(new RelativeRotationSeriesRenderer(_context), s);

    // Flow family
    /// <inheritdoc />
    public void Visit(SankeySeries s, RenderArea a) => Dispatch(new SankeySeriesRenderer(_context), s);
    /// <inheritdoc />
    public void Visit(ChordSeries s, RenderArea a) => Dispatch(new ChordSeriesRenderer(_context), s);
    /// <inheritdoc />
    public void Visit(WordCloudSeries s, RenderArea a) => Dispatch(new WordCloudSeriesRenderer(_context), s);

    // Polar family
    /// <inheritdoc />
    public void Visit(PolarLineSeries s, RenderArea a) => Dispatch(new PolarLineSeriesRenderer(_context), s);
    /// <inheritdoc />
    public void Visit(PolarScatterSeries s, RenderArea a) => Dispatch(new PolarScatterSeriesRenderer(_context), s);
    /// <inheritdoc />
    public void Visit(PolarBarSeries s, RenderArea a) => Dispatch(new PolarBarSeriesRenderer(_context), s);

    // Ternary family
    /// <inheritdoc />
    public void Visit(TernaryScatterSeries s, RenderArea a) => Dispatch(new TernaryScatterSeriesRenderer(_context), s);
    /// <inheritdoc />
    public void Visit(TernaryLineSeries s, RenderArea a) => Dispatch(new TernaryLineSeriesRenderer(_context), s);
    /// <inheritdoc />
    public void Visit(PiperSeries s, RenderArea a) => Dispatch(new PiperSeriesRenderer(_context), s);

    // ThreeD family
    /// <inheritdoc />
    public void Visit(SurfaceSeries s, RenderArea a) => Dispatch(new SurfaceSeriesRenderer(_context), s);
    /// <inheritdoc />
    public void Visit(WireframeSeries s, RenderArea a) => Dispatch(new WireframeSeriesRenderer(_context), s);
    /// <inheritdoc />
    public void Visit(Scatter3DSeries s, RenderArea a) => Dispatch(new Scatter3DSeriesRenderer(_context), s);

    // Distribution v0.8
    /// <inheritdoc />
    public void Visit(RugplotSeries s, RenderArea a) => Dispatch(new RugplotSeriesRenderer(_context), s);
    /// <inheritdoc />
    public void Visit(StripplotSeries s, RenderArea a) => Dispatch(new StripplotSeriesRenderer(_context), s);

    // Categorical v0.8
    /// <inheritdoc />
    public void Visit(EventplotSeries s, RenderArea a) => Dispatch(new EventplotSeriesRenderer(_context), s);
    /// <inheritdoc />
    public void Visit(BrokenBarSeries s, RenderArea a) => Dispatch(new BrokenBarSeriesRenderer(_context), s);
    /// <inheritdoc />
    public void Visit(CountSeries s, RenderArea a) => Dispatch(new CountSeriesRenderer(_context), s);

    // Grid v0.8
    /// <inheritdoc />
    public void Visit(PcolormeshSeries s, RenderArea a) => Dispatch(new PcolormeshSeriesRenderer(_context), s);

    // XY v0.8
    /// <inheritdoc />
    public void Visit(ResidualSeries s, RenderArea a) => Dispatch(new ResidualSeriesRenderer(_context), s);

    // Phase B additions
    /// <inheritdoc />
    public void Visit(PointplotSeries s, RenderArea a) => Dispatch(new PointplotSeriesRenderer(_context), s);
    /// <inheritdoc />
    public void Visit(SwarmplotSeries s, RenderArea a) => Dispatch(new SwarmplotSeriesRenderer(_context), s);
    /// <inheritdoc />
    public void Visit(SpectrogramSeries s, RenderArea a) => Dispatch(new SpectrogramSeriesRenderer(_context), s);
    /// <inheritdoc />
    public void Visit(TableSeries s, RenderArea a) => Dispatch(new TableSeriesRenderer(_context), s);
    /// <inheritdoc />
    public void Visit(TreeGridSeries s, RenderArea a) => Dispatch(new TreeGridSeriesRenderer(_context), s);

    // Phase C additions
    /// <inheritdoc />
    public void Visit(TricontourSeries s, RenderArea a) => Dispatch(new TricontourSeriesRenderer(_context), s);
    /// <inheritdoc />
    public void Visit(TripcolorSeries s, RenderArea a) => Dispatch(new TripcolorSeriesRenderer(_context), s);
    /// <inheritdoc />
    public void Visit(QuiverKeySeries s, RenderArea a) => Dispatch(new QuiverKeySeriesRenderer(_context), s);
    /// <inheritdoc />
    public void Visit(BarbsSeries s, RenderArea a) => Dispatch(new BarbsSeriesRenderer(_context), s);

    // Phase D additions
    /// <inheritdoc />
    public void Visit(Stem3DSeries s, RenderArea a) => Dispatch(new Stem3DSeriesRenderer(_context), s);
    /// <inheritdoc />
    public void Visit(Bar3DSeries s, RenderArea a) => Dispatch(new Bar3DSeriesRenderer(_context), s);
    /// <inheritdoc />
    public void Visit(PlanarBar3DSeries s, RenderArea a) => Dispatch(new PlanarBar3DSeriesRenderer(_context), s);

    /// <inheritdoc />
    public void Visit(WaffleSeries s, RenderArea a) => Dispatch(new WaffleSeriesRenderer(_context), s);

    /// <inheritdoc />
    public void Visit(PointAndFigureSeries s, RenderArea a) => Dispatch(new PointAndFigureSeriesRenderer(_context), s);

    /// <inheritdoc />
    public void Visit(IcicleSeries s, RenderArea a) => Dispatch(new IcicleSeriesRenderer(_context), s);

    // Signal v1.0
    /// <inheritdoc />
    public void Visit(SignalXYSeries s, RenderArea a) => Dispatch(new MonotonicLineRenderer<SignalXYSeries>(_context), s);
    /// <inheritdoc />
    public void Visit(SignalSeries s, RenderArea a) => Dispatch(new MonotonicLineRenderer<SignalSeries>(_context), s);

    // Polar heatmap v1.1.1
    /// <inheritdoc />
    public void Visit(PolarHeatmapSeries s, RenderArea a) => Dispatch(new PolarHeatmapSeriesRenderer(_context), s);

    // ThreeD v1.3.0
    /// <inheritdoc />
    public void Visit(Line3DSeries s, RenderArea a) => Dispatch(new Line3DSeriesRenderer(_context), s);
    /// <inheritdoc />
    public void Visit(Trisurf3DSeries s, RenderArea a) => Dispatch(new Trisurf3DSeriesRenderer(_context), s);
    /// <inheritdoc />
    public void Visit(Contour3DSeries s, RenderArea a) => Dispatch(new Contour3DSeriesRenderer(_context), s);
    /// <inheritdoc />
    public void Visit(Quiver3DSeries s, RenderArea a) => Dispatch(new Quiver3DSeriesRenderer(_context), s);
    /// <inheritdoc />
    public void Visit(VoxelSeries s, RenderArea a) => Dispatch(new VoxelSeriesRenderer(_context), s);
    /// <inheritdoc />
    public void Visit(Text3DSeries s, RenderArea a) => Dispatch(new Text3DSeriesRenderer(_context), s);

    // Streaming v1.4.0 — the ring buffers, drawn.
    //
    // The visitor declares these as EMPTY default bodies for ISP compatibility, and SVG never overrode them:
    // a streaming series therefore rendered nothing at all on either axis, while its data range was folded in
    // correctly, so the axes scaled to data no reader could see. Measured 2026-08-22 on a live ops wall.
    //
    // A snapshot of the ring is exactly the XY pair the static renderers already draw, so this DELEGATES
    // rather than repeating the line/marker geometry: one place keeps deciding what a line looks like.

    /// <inheritdoc />
    public void Visit(StreamingLineSeries s, RenderArea a)
    {
        var snapshot = s.CreateSnapshot();
        if (snapshot.XData.Length == 0)
        {
            return; // nothing appended yet — an empty ring is not a flat line at zero
        }
        // what the stream was not given stays not given, so the line takes its slot's and its theme's as a line does
        Visit(new LineSeries(snapshot.XData, snapshot.YData) { Label = s.Label, Visible = s.Visible, Color = s.Color, Stroke = s.Stroke }, a);
    }

    /// <inheritdoc />
    public void Visit(StreamingScatterSeries s, RenderArea a)
    {
        var snapshot = s.CreateSnapshot();
        if (snapshot.XData.Length == 0)
        {
            return;
        }
        Visit(new ScatterSeries(snapshot.XData, snapshot.YData)
        {
            Label = s.Label,
            Visible = s.Visible,
            Color = s.Color,
            Alpha = s.Alpha,
            // ScatterSeries sizes markers by AREA in points squared, the streaming type by DIAMETER in pixels: the area
            // of a marker that wide is the square of its diameter in points.
            MarkerSize = Math.Pow(s.MarkerSize.PixelsInPoints, 2),
        }, a);
    }

    /// <inheritdoc />
    public void Visit(StreamingSignalSeries s, RenderArea a)
    {
        var snapshot = s.CreateSnapshot();
        if (snapshot.XData.Length == 0)
        {
            return;
        }
        Visit(new LineSeries(snapshot.XData, snapshot.YData)
        {
            Label = s.Label,
            Visible = s.Visible,
            Color = s.Color,
            // the stream's rule worked out by its slot (given, else the cycler's, else the theme's, else 1 px), handed to the line as drawn
            LineWidth = _context.Slot.LineWidthOf(s),
        }, a);
    }

    /// <inheritdoc />
    public void Visit(StreamingCandlestickSeries s, RenderArea a)
    {
        var snapshot = s.CreateOhlcSnapshot();
        if (snapshot.Open.Length == 0)
        {
            return;
        }
        Visit(new CandlestickSeries(snapshot.Open, snapshot.High, snapshot.Low, snapshot.Close)
        {
            Label = s.Label,
            Visible = s.Visible,
            UpColor = s.Color ?? UpDownColors.Standard.Up,
            DownColor = s.DownColor ?? UpDownColors.Standard.Down,
        }, a);
    }
}
