// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Models.Series.Streaming;
using MatPlotLibNet.Rendering.Lighting;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.Svg;

/// <summary>Visitor that dispatches each series type to its typed <see cref="SeriesRenderer{T}"/>.</summary>
/// <remarks>Each series type has a dedicated renderer class in <c>Rendering.SeriesRenderers</c>.
/// Renderers are lazily created on first use and reused for subsequent calls with the same context.
/// THREAD-SAFETY: This class is designed for single-threaded use within one render pass.
/// Lazy fields use ??= which is not atomic. Do not share instances across threads.</remarks>
internal sealed class SvgSeriesRenderer : ISeriesVisitor
{
    private SeriesRenderContext _context;

    // XY family
    private LineSeriesRenderer? _line;
    private RegressionSeriesRenderer? _regression;
    private ScatterSeriesRenderer? _scatter;
    private StepSeriesRenderer? _step;
    private AreaSeriesRenderer? _area;
    private ErrorBarSeriesRenderer? _errorBar;
    private BubbleSeriesRenderer? _bubble;
    private SparklineSeriesRenderer? _sparkline;
    private EcdfSeriesRenderer? _ecdf;
    private StackedAreaSeriesRenderer? _stackedArea;

    // Categorical family
    private BarSeriesRenderer? _bar;
    private HistogramSeriesRenderer? _histogram;
    private WaterfallSeriesRenderer? _waterfall;
    private FunnelSeriesRenderer? _funnel;
    private GanttSeriesRenderer? _gantt;
    private ProgressBarSeriesRenderer? _progressBar;
    private StatTileSeriesRenderer? _statTile;
    private StateTimelineSeriesRenderer? _stateTimeline;
    private BulletGraphSeriesRenderer? _bullet;

    // Circular family
    private PieSeriesRenderer? _pie;
    private RadarSeriesRenderer? _radar;
    private DonutSeriesRenderer? _donut;
    private GaugeSeriesRenderer? _gauge;

    // Grid family
    private HeatmapSeriesRenderer? _heatmap;
    private HexbinSeriesRenderer? _hexbin;
    private ImageSeriesRenderer? _image;
    private Histogram2DSeriesRenderer? _histogram2D;
    private ContourSeriesRenderer? _contour;
    private ContourfSeriesRenderer? _contourf;

    // Distribution family
    private BoxSeriesRenderer? _box;
    private ViolinSeriesRenderer? _violin;
    private KdeSeriesRenderer? _kde;

    // Financial family
    private CandlestickSeriesRenderer? _candlestick;
    private OhlcBarSeriesRenderer? _ohlcBar;

    // Field family
    private QuiverSeriesRenderer? _quiver;
    private StreamplotSeriesRenderer? _streamplot;
    private StemSeriesRenderer? _stem;

    // Hierarchical family
    private TreemapSeriesRenderer? _treemap;
    private SunburstSeriesRenderer? _sunburst;
    private DendrogramSeriesRenderer? _dendrogram;
    private ClustermapSeriesRenderer? _clustermap;
    private PairGridSeriesRenderer? _pairgrid;
    private NetworkGraphSeriesRenderer? _networkgraph;
    private RelativeRotationSeriesRenderer? _relativeRotation;

    // Flow family
    private SankeySeriesRenderer? _sankey;

    // Polar family
    private PolarLineSeriesRenderer? _polarLine;
    private PolarScatterSeriesRenderer? _polarScatter;
    private PolarBarSeriesRenderer? _polarBar;

    // ThreeD family
    private SurfaceSeriesRenderer? _surface;
    private WireframeSeriesRenderer? _wireframe;
    private Scatter3DSeriesRenderer? _scatter3D;

    // Distribution v0.8
    private RugplotSeriesRenderer? _rugplot;
    private StripplotSeriesRenderer? _stripplot;

    // Categorical v0.8
    private EventplotSeriesRenderer? _eventplot;
    private BrokenBarSeriesRenderer? _brokenBar;
    private CountSeriesRenderer? _count;

    // Grid v0.8
    private PcolormeshSeriesRenderer? _pcolormesh;

    // XY v0.8
    private ResidualSeriesRenderer? _residual;

    // Categorical v0.8 B
    private PointplotSeriesRenderer? _pointplot;

    // Distribution v0.8 B
    private SwarmplotSeriesRenderer? _swarmplot;

    // Grid v0.8 B
    private SpectrogramSeriesRenderer? _spectrogram;

    // Special v0.8 B
    private TableSeriesRenderer? _table;
    private TreeGridSeriesRenderer? _treeGrid;

    // Grid v0.8 C
    private TricontourSeriesRenderer? _tricontour;
    private TripcolorSeriesRenderer? _tripcolor;

    // Field v0.8 C
    private QuiverKeySeriesRenderer? _quiverKey;
    private BarbsSeriesRenderer? _barbs;

    // Signal v1.0
    private SignalXYSeriesRenderer? _signalXY;
    private SignalSeriesRenderer? _signal;

    /// <summary>The radial scale of the polar axes the series are drawn on, handed to every polar renderer through its context.</summary>
    internal RadialScale? RadialScale { init => _context = _context with { RadialScale = value }; }

    /// <summary>The first colour slot the series holds; its parts take the slots from here on
    /// (<see cref="SeriesRenderContext.FirstColorSlot"/>).</summary>
    internal int FirstColorSlot { init => _context = _context with { FirstColorSlot = value }; }

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

    /// <summary>The theme this pass draws under. A series that draws itself through <c>Accept</c> (a map from
    /// <c>MatPlotLibNet.Geo</c>) reads its theme-dependent colours here, since a <see cref="RenderArea"/> carries none.</summary>
    internal Styling.Theme Theme => _context.Theme;

    // XY family
    /// <inheritdoc />
    public void Visit(LineSeries s, RenderArea a) => Dispatch(_line ??= new(_context), s);
    /// <inheritdoc />
    public void Visit(RegressionSeries s, RenderArea a) => Dispatch(_regression ??= new(_context), s);
    /// <inheritdoc />
    public void Visit(ScatterSeries s, RenderArea a) => Dispatch(_scatter ??= new(_context), s);
    /// <inheritdoc />
    public void Visit(StepSeries s, RenderArea a) => Dispatch(_step ??= new(_context), s);
    /// <inheritdoc />
    public void Visit(AreaSeries s, RenderArea a) => Dispatch(_area ??= new(_context), s);
    /// <inheritdoc />
    public void Visit(ErrorBarSeries s, RenderArea a) => Dispatch(_errorBar ??= new(_context), s);
    /// <inheritdoc />
    public void Visit(BubbleSeries s, RenderArea a) => Dispatch(_bubble ??= new(_context), s);
    /// <inheritdoc />
    public void Visit(SparklineSeries s, RenderArea a) => Dispatch(_sparkline ??= new(_context), s);
    /// <inheritdoc />
    public void Visit(EcdfSeries s, RenderArea a) => Dispatch(_ecdf ??= new(_context), s);
    /// <inheritdoc />
    public void Visit(StackedAreaSeries s, RenderArea a) => Dispatch(_stackedArea ??= new(_context), s);

    // Categorical family
    /// <inheritdoc />
    public void Visit(BarSeries s, RenderArea a) => Dispatch(_bar ??= new(_context), s);
    /// <inheritdoc />
    public void Visit(HistogramSeries s, RenderArea a) => Dispatch(_histogram ??= new(_context), s);
    /// <inheritdoc />
    public void Visit(WaterfallSeries s, RenderArea a) => Dispatch(_waterfall ??= new(_context), s);
    /// <inheritdoc />
    public void Visit(FunnelSeries s, RenderArea a) => Dispatch(_funnel ??= new(_context), s);
    /// <inheritdoc />
    public void Visit(GanttSeries s, RenderArea a) => Dispatch(_gantt ??= new(_context), s);
    /// <inheritdoc />
    public void Visit(ProgressBarSeries s, RenderArea a) => Dispatch(_progressBar ??= new(_context), s);
    /// <inheritdoc />
    public void Visit(StatTileSeries s, RenderArea a) => Dispatch(_statTile ??= new(_context), s);
    /// <inheritdoc />
    public void Visit(StateTimelineSeries s, RenderArea a) => Dispatch(_stateTimeline ??= new(_context), s);

    /// <inheritdoc />
    public void Visit(BulletGraphSeries s, RenderArea a) => Dispatch(_bullet ??= new(_context), s);

    // Circular family
    /// <inheritdoc />
    public void Visit(PieSeries s, RenderArea a) => Dispatch(_pie ??= new(_context), s);
    /// <inheritdoc />
    public void Visit(RadarSeries s, RenderArea a) => Dispatch(_radar ??= new(_context), s);
    /// <inheritdoc />
    public void Visit(DonutSeries s, RenderArea a) => Dispatch(_donut ??= new(_context), s);
    /// <inheritdoc />
    public void Visit(GaugeSeries s, RenderArea a) => Dispatch(_gauge ??= new(_context), s);

    // Grid family
    /// <inheritdoc />
    public void Visit(HeatmapSeries s, RenderArea a) => Dispatch(_heatmap ??= new(_context), s);
    /// <inheritdoc />
    public void Visit(HexbinSeries s, RenderArea a) => Dispatch(_hexbin ??= new(_context), s);
    /// <inheritdoc />
    public void Visit(ImageSeries s, RenderArea a) => Dispatch(_image ??= new(_context), s);
    /// <inheritdoc />
    public void Visit(Histogram2DSeries s, RenderArea a) => Dispatch(_histogram2D ??= new(_context), s);
    /// <inheritdoc />
    public void Visit(ContourSeries s, RenderArea a) => Dispatch(_contour ??= new(_context), s);
    /// <inheritdoc />
    public void Visit(ContourfSeries s, RenderArea a) => Dispatch(_contourf ??= new(_context), s);

    // Distribution family
    /// <inheritdoc />
    public void Visit(BoxSeries s, RenderArea a) => Dispatch(_box ??= new(_context), s);
    /// <inheritdoc />
    public void Visit(ViolinSeries s, RenderArea a) => Dispatch(_violin ??= new(_context), s);
    /// <inheritdoc />
    public void Visit(KdeSeries s, RenderArea a) => Dispatch(_kde ??= new(_context), s);

    // Financial family
    /// <inheritdoc />
    public void Visit(CandlestickSeries s, RenderArea a) => Dispatch(_candlestick ??= new(_context), s);
    /// <inheritdoc />
    public void Visit(OhlcBarSeries s, RenderArea a) => Dispatch(_ohlcBar ??= new(_context), s);

    // Field family
    /// <inheritdoc />
    public void Visit(QuiverSeries s, RenderArea a) => Dispatch(_quiver ??= new(_context), s);
    /// <inheritdoc />
    public void Visit(StreamplotSeries s, RenderArea a) => Dispatch(_streamplot ??= new(_context), s);
    /// <inheritdoc />
    public void Visit(StemSeries s, RenderArea a) => Dispatch(_stem ??= new(_context), s);

    // Hierarchical family
    /// <inheritdoc />
    public void Visit(TreemapSeries s, RenderArea a) => Dispatch(_treemap ??= new(_context), s);
    /// <inheritdoc />
    public void Visit(SunburstSeries s, RenderArea a) => Dispatch(_sunburst ??= new(_context), s);
    /// <inheritdoc />
    public void Visit(DendrogramSeries s, RenderArea a) => Dispatch(_dendrogram ??= new(_context), s);
    /// <inheritdoc />
    public void Visit(ClustermapSeries s, RenderArea a) => Dispatch(_clustermap ??= new(_context), s);
    /// <inheritdoc />
    public void Visit(PairGridSeries s, RenderArea a) => Dispatch(_pairgrid ??= new(_context), s);
    /// <inheritdoc />
    public void Visit(NetworkGraphSeries s, RenderArea a) => Dispatch(_networkgraph ??= new(_context), s);
    public void Visit(RelativeRotationSeries s, RenderArea a) => Dispatch(_relativeRotation ??= new(_context), s);

    // Flow family
    /// <inheritdoc />
    public void Visit(SankeySeries s, RenderArea a) => Dispatch(_sankey ??= new(_context), s);

    // Polar family
    /// <inheritdoc />
    public void Visit(PolarLineSeries s, RenderArea a) => Dispatch(_polarLine ??= new(_context), s);
    /// <inheritdoc />
    public void Visit(PolarScatterSeries s, RenderArea a) => Dispatch(_polarScatter ??= new(_context), s);
    /// <inheritdoc />
    public void Visit(PolarBarSeries s, RenderArea a) => Dispatch(_polarBar ??= new(_context), s);

    // ThreeD family
    /// <inheritdoc />
    public void Visit(SurfaceSeries s, RenderArea a) => Dispatch(_surface ??= new(_context), s);
    /// <inheritdoc />
    public void Visit(WireframeSeries s, RenderArea a) => Dispatch(_wireframe ??= new(_context), s);
    /// <inheritdoc />
    public void Visit(Scatter3DSeries s, RenderArea a) => Dispatch(_scatter3D ??= new(_context), s);

    // Distribution v0.8
    /// <inheritdoc />
    public void Visit(RugplotSeries s, RenderArea a) => Dispatch(_rugplot ??= new(_context), s);
    /// <inheritdoc />
    public void Visit(StripplotSeries s, RenderArea a) => Dispatch(_stripplot ??= new(_context), s);

    // Categorical v0.8
    /// <inheritdoc />
    public void Visit(EventplotSeries s, RenderArea a) => Dispatch(_eventplot ??= new(_context), s);
    /// <inheritdoc />
    public void Visit(BrokenBarSeries s, RenderArea a) => Dispatch(_brokenBar ??= new(_context), s);
    /// <inheritdoc />
    public void Visit(CountSeries s, RenderArea a) => Dispatch(_count ??= new(_context), s);

    // Grid v0.8
    /// <inheritdoc />
    public void Visit(PcolormeshSeries s, RenderArea a) => Dispatch(_pcolormesh ??= new(_context), s);

    // XY v0.8
    /// <inheritdoc />
    public void Visit(ResidualSeries s, RenderArea a) => Dispatch(_residual ??= new(_context), s);

    // Phase B additions
    /// <inheritdoc />
    public void Visit(PointplotSeries s, RenderArea a) => Dispatch(_pointplot ??= new(_context), s);
    /// <inheritdoc />
    public void Visit(SwarmplotSeries s, RenderArea a) => Dispatch(_swarmplot ??= new(_context), s);
    /// <inheritdoc />
    public void Visit(SpectrogramSeries s, RenderArea a) => Dispatch(_spectrogram ??= new(_context), s);
    /// <inheritdoc />
    public void Visit(TableSeries s, RenderArea a) => Dispatch(_table ??= new(_context), s);
    /// <inheritdoc />
    public void Visit(TreeGridSeries s, RenderArea a) => Dispatch(_treeGrid ??= new(_context), s);

    // Phase C additions
    /// <inheritdoc />
    public void Visit(TricontourSeries s, RenderArea a) => Dispatch(_tricontour ??= new(_context), s);
    /// <inheritdoc />
    public void Visit(TripcolorSeries s, RenderArea a) => Dispatch(_tripcolor ??= new(_context), s);
    /// <inheritdoc />
    public void Visit(QuiverKeySeries s, RenderArea a) => Dispatch(_quiverKey ??= new(_context), s);
    /// <inheritdoc />
    public void Visit(BarbsSeries s, RenderArea a) => Dispatch(_barbs ??= new(_context), s);

    // Phase D additions
    private Stem3DSeriesRenderer? _stem3D;
    private Bar3DSeriesRenderer? _bar3D;
    private WaffleSeriesRenderer? _waffle;
    private PointAndFigureSeriesRenderer? _pointAndFigure;
    private IcicleSeriesRenderer? _icicle;
    private PlanarBar3DSeriesRenderer? _planarBar3D;
    /// <inheritdoc />
    public void Visit(Stem3DSeries s, RenderArea a) => Dispatch(_stem3D ??= new(_context), s);
    /// <inheritdoc />
    public void Visit(Bar3DSeries s, RenderArea a) => Dispatch(_bar3D ??= new(_context), s);
    /// <inheritdoc />
    public void Visit(PlanarBar3DSeries s, RenderArea a) => Dispatch(_planarBar3D ??= new(_context), s);

    /// <inheritdoc />
    public void Visit(WaffleSeries s, RenderArea a) => Dispatch(_waffle ??= new(_context), s);

    /// <inheritdoc />
    public void Visit(PointAndFigureSeries s, RenderArea a) => Dispatch(_pointAndFigure ??= new(_context), s);

    /// <inheritdoc />
    public void Visit(IcicleSeries s, RenderArea a) => Dispatch(_icicle ??= new(_context), s);

    // Signal v1.0
    /// <inheritdoc />
    public void Visit(SignalXYSeries s, RenderArea a) => Dispatch(_signalXY ??= new(_context), s);
    /// <inheritdoc />
    public void Visit(SignalSeries s, RenderArea a) => Dispatch(_signal ??= new(_context), s);

    // Polar heatmap v1.1.1
    private PolarHeatmapSeriesRenderer? _polarHeatmap;
    /// <inheritdoc />
    public void Visit(PolarHeatmapSeries s, RenderArea a) => Dispatch(_polarHeatmap ??= new(_context), s);

    // ThreeD v1.3.0
    private Line3DSeriesRenderer? _line3D;
    private Trisurf3DSeriesRenderer? _trisurf3D;
    private Contour3DSeriesRenderer? _contour3D;
    private Quiver3DSeriesRenderer? _quiver3D;
    private VoxelSeriesRenderer? _voxel;
    private Text3DSeriesRenderer? _text3D;
    /// <inheritdoc />
    public void Visit(Line3DSeries s, RenderArea a) => Dispatch(_line3D ??= new(_context), s);
    /// <inheritdoc />
    public void Visit(Trisurf3DSeries s, RenderArea a) => Dispatch(_trisurf3D ??= new(_context), s);
    /// <inheritdoc />
    public void Visit(Contour3DSeries s, RenderArea a) => Dispatch(_contour3D ??= new(_context), s);
    /// <inheritdoc />
    public void Visit(Quiver3DSeries s, RenderArea a) => Dispatch(_quiver3D ??= new(_context), s);
    /// <inheritdoc />
    public void Visit(VoxelSeries s, RenderArea a) => Dispatch(_voxel ??= new(_context), s);
    /// <inheritdoc />
    public void Visit(Text3DSeries s, RenderArea a) => Dispatch(_text3D ??= new(_context), s);

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
        Visit(new LineSeries(snapshot.XData, snapshot.YData)
        {
            Label = s.Label,
            Visible = s.Visible,
            Color = s.Color,
            LineStyle = s.LineStyle,
            LineWidth = s.LineWidth,
        }, a);
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
            // ScatterSeries sizes markers by AREA (default 36 = a 6 px marker), the streaming type by
            // DIAMETER — square it, or every streamed marker renders a sixth of its declared size.
            MarkerSize = s.MarkerSize * s.MarkerSize,
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
            LineWidth = s.LineWidth,
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
            UpColor = s.Color ?? Colors.Green,
            DownColor = s.DownColor ?? Colors.Red,
        }, a);
    }
}
