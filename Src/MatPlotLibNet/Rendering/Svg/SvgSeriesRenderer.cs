// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
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
    private readonly SeriesRenderContext _context;

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

    // Grid v0.8 C
    private TricontourSeriesRenderer? _tricontour;
    private TripcolorSeriesRenderer? _tripcolor;

    // Field v0.8 C
    private QuiverKeySeriesRenderer? _quiverKey;
    private BarbsSeriesRenderer? _barbs;

    public SvgSeriesRenderer(DataTransform transform, IRenderContext ctx, Color seriesColor, bool tooltipsEnabled = false)
    {
        _context = new SeriesRenderContext(transform, ctx, seriesColor, new RenderArea(default, ctx))
        {
            TooltipsEnabled = tooltipsEnabled
        };
    }

    // XY family
    /// <inheritdoc />
    public void Visit(LineSeries s, RenderArea a) => (_line ??= new(_context)).Render(s);
    /// <inheritdoc />
    public void Visit(RegressionSeries s, RenderArea a) => (_regression ??= new(_context)).Render(s);
    /// <inheritdoc />
    public void Visit(ScatterSeries s, RenderArea a) => (_scatter ??= new(_context)).Render(s);
    /// <inheritdoc />
    public void Visit(StepSeries s, RenderArea a) => (_step ??= new(_context)).Render(s);
    /// <inheritdoc />
    public void Visit(AreaSeries s, RenderArea a) => (_area ??= new(_context)).Render(s);
    /// <inheritdoc />
    public void Visit(ErrorBarSeries s, RenderArea a) => (_errorBar ??= new(_context)).Render(s);
    /// <inheritdoc />
    public void Visit(BubbleSeries s, RenderArea a) => (_bubble ??= new(_context)).Render(s);
    /// <inheritdoc />
    public void Visit(SparklineSeries s, RenderArea a) => (_sparkline ??= new(_context)).Render(s);
    /// <inheritdoc />
    public void Visit(EcdfSeries s, RenderArea a) => (_ecdf ??= new(_context)).Render(s);
    /// <inheritdoc />
    public void Visit(StackedAreaSeries s, RenderArea a) => (_stackedArea ??= new(_context)).Render(s);

    // Categorical family
    /// <inheritdoc />
    public void Visit(BarSeries s, RenderArea a) => (_bar ??= new(_context)).Render(s);
    /// <inheritdoc />
    public void Visit(HistogramSeries s, RenderArea a) => (_histogram ??= new(_context)).Render(s);
    /// <inheritdoc />
    public void Visit(WaterfallSeries s, RenderArea a) => (_waterfall ??= new(_context)).Render(s);
    /// <inheritdoc />
    public void Visit(FunnelSeries s, RenderArea a) => (_funnel ??= new(_context)).Render(s);
    /// <inheritdoc />
    public void Visit(GanttSeries s, RenderArea a) => (_gantt ??= new(_context)).Render(s);
    /// <inheritdoc />
    public void Visit(ProgressBarSeries s, RenderArea a) => (_progressBar ??= new(_context)).Render(s);

    // Circular family
    /// <inheritdoc />
    public void Visit(PieSeries s, RenderArea a) => (_pie ??= new(_context)).Render(s);
    /// <inheritdoc />
    public void Visit(RadarSeries s, RenderArea a) => (_radar ??= new(_context)).Render(s);
    /// <inheritdoc />
    public void Visit(DonutSeries s, RenderArea a) => (_donut ??= new(_context)).Render(s);
    /// <inheritdoc />
    public void Visit(GaugeSeries s, RenderArea a) => (_gauge ??= new(_context)).Render(s);

    // Grid family
    /// <inheritdoc />
    public void Visit(HeatmapSeries s, RenderArea a) => (_heatmap ??= new(_context)).Render(s);
    /// <inheritdoc />
    public void Visit(HexbinSeries s, RenderArea a) => (_hexbin ??= new(_context)).Render(s);
    /// <inheritdoc />
    public void Visit(ImageSeries s, RenderArea a) => (_image ??= new(_context)).Render(s);
    /// <inheritdoc />
    public void Visit(Histogram2DSeries s, RenderArea a) => (_histogram2D ??= new(_context)).Render(s);
    /// <inheritdoc />
    public void Visit(ContourSeries s, RenderArea a) => (_contour ??= new(_context)).Render(s);
    /// <inheritdoc />
    public void Visit(ContourfSeries s, RenderArea a) => (_contourf ??= new(_context)).Render(s);

    // Distribution family
    /// <inheritdoc />
    public void Visit(BoxSeries s, RenderArea a) => (_box ??= new(_context)).Render(s);
    /// <inheritdoc />
    public void Visit(ViolinSeries s, RenderArea a) => (_violin ??= new(_context)).Render(s);
    /// <inheritdoc />
    public void Visit(KdeSeries s, RenderArea a) => (_kde ??= new(_context)).Render(s);

    // Financial family
    /// <inheritdoc />
    public void Visit(CandlestickSeries s, RenderArea a) => (_candlestick ??= new(_context)).Render(s);
    /// <inheritdoc />
    public void Visit(OhlcBarSeries s, RenderArea a) => (_ohlcBar ??= new(_context)).Render(s);

    // Field family
    /// <inheritdoc />
    public void Visit(QuiverSeries s, RenderArea a) => (_quiver ??= new(_context)).Render(s);
    public void Visit(StreamplotSeries s, RenderArea a) => (_streamplot ??= new(_context)).Render(s);
    /// <inheritdoc />
    public void Visit(StemSeries s, RenderArea a) => (_stem ??= new(_context)).Render(s);

    // Hierarchical family
    /// <inheritdoc />
    public void Visit(TreemapSeries s, RenderArea a) => (_treemap ??= new(_context)).Render(s);
    /// <inheritdoc />
    public void Visit(SunburstSeries s, RenderArea a) => (_sunburst ??= new(_context)).Render(s);

    // Flow family
    /// <inheritdoc />
    public void Visit(SankeySeries s, RenderArea a) => (_sankey ??= new(_context)).Render(s);

    // Polar family
    /// <inheritdoc />
    public void Visit(PolarLineSeries s, RenderArea a) => (_polarLine ??= new(_context)).Render(s);
    /// <inheritdoc />
    public void Visit(PolarScatterSeries s, RenderArea a) => (_polarScatter ??= new(_context)).Render(s);
    /// <inheritdoc />
    public void Visit(PolarBarSeries s, RenderArea a) => (_polarBar ??= new(_context)).Render(s);

    // ThreeD family
    /// <inheritdoc />
    public void Visit(SurfaceSeries s, RenderArea a) => (_surface ??= new(_context)).Render(s);
    /// <inheritdoc />
    public void Visit(WireframeSeries s, RenderArea a) => (_wireframe ??= new(_context)).Render(s);
    /// <inheritdoc />
    public void Visit(Scatter3DSeries s, RenderArea a) => (_scatter3D ??= new(_context)).Render(s);

    // Distribution v0.8
    /// <inheritdoc />
    public void Visit(RugplotSeries s, RenderArea a) => (_rugplot ??= new(_context)).Render(s);
    /// <inheritdoc />
    public void Visit(StripplotSeries s, RenderArea a) => (_stripplot ??= new(_context)).Render(s);

    // Categorical v0.8
    /// <inheritdoc />
    public void Visit(EventplotSeries s, RenderArea a) => (_eventplot ??= new(_context)).Render(s);
    /// <inheritdoc />
    public void Visit(BrokenBarSeries s, RenderArea a) => (_brokenBar ??= new(_context)).Render(s);
    /// <inheritdoc />
    public void Visit(CountSeries s, RenderArea a) => (_count ??= new(_context)).Render(s);

    // Grid v0.8
    /// <inheritdoc />
    public void Visit(PcolormeshSeries s, RenderArea a) => (_pcolormesh ??= new(_context)).Render(s);

    // XY v0.8
    /// <inheritdoc />
    public void Visit(ResidualSeries s, RenderArea a) => (_residual ??= new(_context)).Render(s);

    // Phase B additions
    /// <inheritdoc />
    public void Visit(PointplotSeries s, RenderArea a) => (_pointplot ??= new(_context)).Render(s);
    /// <inheritdoc />
    public void Visit(SwarmplotSeries s, RenderArea a) => (_swarmplot ??= new(_context)).Render(s);
    /// <inheritdoc />
    public void Visit(SpectrogramSeries s, RenderArea a) => (_spectrogram ??= new(_context)).Render(s);
    /// <inheritdoc />
    public void Visit(TableSeries s, RenderArea a) => (_table ??= new(_context)).Render(s);

    // Phase C additions
    /// <inheritdoc />
    public void Visit(TricontourSeries s, RenderArea a) => (_tricontour ??= new(_context)).Render(s);
    /// <inheritdoc />
    public void Visit(TripcolorSeries s, RenderArea a) => (_tripcolor ??= new(_context)).Render(s);
    /// <inheritdoc />
    public void Visit(QuiverKeySeries s, RenderArea a) => (_quiverKey ??= new(_context)).Render(s);
    /// <inheritdoc />
    public void Visit(BarbsSeries s, RenderArea a) => (_barbs ??= new(_context)).Render(s);

    // Phase D additions
    private Stem3DSeriesRenderer? _stem3D;
    private Bar3DSeriesRenderer? _bar3D;
    /// <inheritdoc />
    public void Visit(Stem3DSeries s, RenderArea a) => (_stem3D ??= new(_context)).Render(s);
    /// <inheritdoc />
    public void Visit(Bar3DSeries s, RenderArea a) => (_bar3D ??= new(_context)).Render(s);
}
