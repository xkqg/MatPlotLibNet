// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.Svg;

/// <summary>Visitor that dispatches each series type to its typed <see cref="SeriesRenderer{T}"/>.</summary>
/// <remarks>Each series type has a dedicated renderer class in <c>Rendering.SeriesRenderers</c>.
/// Renderers are lazily created on first use and reused for subsequent calls with the same context.</remarks>
internal sealed class SvgSeriesRenderer : ISeriesVisitor
{
    private readonly SeriesRenderContext _context;

    // XY family
    private LineSeriesRenderer? _line;
    private ScatterSeriesRenderer? _scatter;
    private StepSeriesRenderer? _step;
    private AreaSeriesRenderer? _area;
    private ErrorBarSeriesRenderer? _errorBar;
    private BubbleSeriesRenderer? _bubble;
    private SparklineSeriesRenderer? _sparkline;

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
    private ContourSeriesRenderer? _contour;

    // Distribution family
    private BoxSeriesRenderer? _box;
    private ViolinSeriesRenderer? _violin;

    // Financial family
    private CandlestickSeriesRenderer? _candlestick;
    private OhlcBarSeriesRenderer? _ohlcBar;

    // Field family
    private QuiverSeriesRenderer? _quiver;
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
    public void Visit(ContourSeries s, RenderArea a) => (_contour ??= new(_context)).Render(s);

    // Distribution family
    /// <inheritdoc />
    public void Visit(BoxSeries s, RenderArea a) => (_box ??= new(_context)).Render(s);
    /// <inheritdoc />
    public void Visit(ViolinSeries s, RenderArea a) => (_violin ??= new(_context)).Render(s);

    // Financial family
    /// <inheritdoc />
    public void Visit(CandlestickSeries s, RenderArea a) => (_candlestick ??= new(_context)).Render(s);
    /// <inheritdoc />
    public void Visit(OhlcBarSeries s, RenderArea a) => (_ohlcBar ??= new(_context)).Render(s);

    // Field family
    /// <inheritdoc />
    public void Visit(QuiverSeries s, RenderArea a) => (_quiver ??= new(_context)).Render(s);
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
}
