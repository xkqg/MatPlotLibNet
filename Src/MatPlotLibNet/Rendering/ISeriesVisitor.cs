// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Models.Series.Streaming;

namespace MatPlotLibNet.Rendering;


/// <summary>Visitor interface for rendering each series type through the visitor pattern.</summary>
/// <remarks>Each default no-op <c>Visit(...) { }</c> overload is individually
/// <see cref="ExcludeFromCodeCoverageAttribute"/>-tagged: they exist for ISP compatibility
/// (so concrete visitors only need to override the methods they care about), and every
/// production visitor overrides them so the defaults are never entered at runtime.</remarks>
public interface ISeriesVisitor
{
    /// <summary>Renders a line series.</summary>
    void Visit(LineSeries series, RenderArea area);

    /// <summary>Renders a scatter series.</summary>
    void Visit(ScatterSeries series, RenderArea area);

    /// <summary>Renders a bar series.</summary>
    void Visit(BarSeries series, RenderArea area);

    /// <summary>Renders a histogram series.</summary>
    void Visit(HistogramSeries series, RenderArea area);

    /// <summary>Renders a pie series.</summary>
    void Visit(PieSeries series, RenderArea area);

    /// <summary>Renders a heatmap series.</summary>
    void Visit(HeatmapSeries series, RenderArea area);

    /// <summary>Renders an image series.</summary>
    void Visit(ImageSeries series, RenderArea area);

    /// <summary>Renders a 2D histogram (density) series.</summary>
    void Visit(Histogram2DSeries series, RenderArea area);

    /// <summary>Renders a box plot series.</summary>
    void Visit(BoxSeries series, RenderArea area);

    /// <summary>Renders a violin plot series.</summary>
    void Visit(ViolinSeries series, RenderArea area);

    /// <summary>Renders a kernel density estimation (KDE) series.</summary>
    void Visit(KdeSeries series, RenderArea area);

    /// <summary>Renders a polynomial regression series.</summary>
    void Visit(RegressionSeries series, RenderArea area);

    /// <summary>Renders a hexagonal binning series.</summary>
    void Visit(HexbinSeries series, RenderArea area);

    /// <summary>Renders a contour series.</summary>
    void Visit(ContourSeries series, RenderArea area);

    /// <summary>Renders a filled contour series.</summary>
    void Visit(ContourfSeries series, RenderArea area);

    /// <summary>Renders a stem plot series.</summary>
    void Visit(StemSeries series, RenderArea area);

    /// <summary>Renders an area (fill-between) series.</summary>
    void Visit(AreaSeries series, RenderArea area);

    /// <summary>Renders a step-function series.</summary>
    void Visit(StepSeries series, RenderArea area);

    /// <summary>Renders an ECDF series.</summary>
    void Visit(EcdfSeries series, RenderArea area);

    /// <summary>Renders a stacked area (stackplot) series.</summary>
    void Visit(StackedAreaSeries series, RenderArea area);

    /// <summary>Renders an error bar series.</summary>
    void Visit(ErrorBarSeries series, RenderArea area);

    /// <summary>Renders a candlestick (OHLC) series.</summary>
    void Visit(CandlestickSeries series, RenderArea area);

    /// <summary>Renders a quiver (vector field) series.</summary>
    void Visit(QuiverSeries series, RenderArea area);

    /// <summary>Renders a streamplot (vector field streamlines) series.</summary>
    void Visit(StreamplotSeries series, RenderArea area);

    /// <summary>Renders a radar (spider) chart series.</summary>
    void Visit(RadarSeries series, RenderArea area);

    /// <summary>Renders a donut chart series.</summary>
    void Visit(DonutSeries series, RenderArea area);

    /// <summary>Renders a bubble chart series.</summary>
    void Visit(BubbleSeries series, RenderArea area);

    /// <summary>Renders a traditional OHLC bar chart series.</summary>
    void Visit(OhlcBarSeries series, RenderArea area);

    /// <summary>Renders a waterfall chart series.</summary>
    void Visit(WaterfallSeries series, RenderArea area);

    /// <summary>Renders a funnel chart series.</summary>
    void Visit(FunnelSeries series, RenderArea area);

    /// <summary>Renders a Gantt chart series.</summary>
    void Visit(GanttSeries series, RenderArea area);

    /// <summary>Renders a gauge (speedometer) chart series.</summary>
    void Visit(GaugeSeries series, RenderArea area);

    /// <summary>Renders a progress bar series.</summary>
    void Visit(ProgressBarSeries series, RenderArea area);

    /// <summary>Renders a sparkline series.</summary>
    void Visit(SparklineSeries series, RenderArea area);

    /// <summary>Renders a treemap series.</summary>
    void Visit(TreemapSeries series, RenderArea area);

    /// <summary>Renders a sunburst series.</summary>
    void Visit(SunburstSeries series, RenderArea area);

    /// <summary>Renders a Sankey diagram series.</summary>
    void Visit(SankeySeries series, RenderArea area);

    /// <summary>Renders a polar line series.</summary>
    void Visit(PolarLineSeries series, RenderArea area);

    /// <summary>Renders a polar scatter series.</summary>
    void Visit(PolarScatterSeries series, RenderArea area);

    /// <summary>Renders a polar bar series.</summary>
    void Visit(PolarBarSeries series, RenderArea area);

    /// <summary>Renders a 3D surface series.</summary>
    void Visit(SurfaceSeries series, RenderArea area);

    /// <summary>Renders a 3D wireframe series.</summary>
    void Visit(WireframeSeries series, RenderArea area);

    /// <summary>Renders a 3D scatter series.</summary>
    void Visit(Scatter3DSeries series, RenderArea area);

    /// <summary>Renders a rug plot series.</summary>
    void Visit(RugplotSeries series, RenderArea area);

    /// <summary>Renders a strip plot series.</summary>
    void Visit(StripplotSeries series, RenderArea area);

    /// <summary>Renders an event plot series.</summary>
    void Visit(EventplotSeries series, RenderArea area);

    /// <summary>Renders a broken bar series.</summary>
    void Visit(BrokenBarSeries series, RenderArea area);

    /// <summary>Renders a count plot series.</summary>
    void Visit(CountSeries series, RenderArea area);

    /// <summary>Renders a pseudocolor mesh series.</summary>
    void Visit(PcolormeshSeries series, RenderArea area);

    /// <summary>Renders a residual plot series.</summary>
    void Visit(ResidualSeries series, RenderArea area);

    /// <summary>Renders a point plot series.</summary>
    void Visit(PointplotSeries series, RenderArea area);

    /// <summary>Renders a swarm plot series.</summary>
    void Visit(SwarmplotSeries series, RenderArea area);

    /// <summary>Renders a spectrogram series.</summary>
    void Visit(SpectrogramSeries series, RenderArea area);

    /// <summary>Renders a table series.</summary>
    void Visit(TableSeries series, RenderArea area);

    /// <summary>Renders a contour series on a triangular mesh.</summary>
    void Visit(TricontourSeries series, RenderArea area);

    /// <summary>Renders a pseudocolor series on a triangular mesh.</summary>
    void Visit(TripcolorSeries series, RenderArea area);

    /// <summary>Renders a quiver key (reference arrow) series.</summary>
    void Visit(QuiverKeySeries series, RenderArea area);

    /// <summary>Renders a wind barb series.</summary>
    void Visit(BarbsSeries series, RenderArea area);

    /// <summary>Renders a 3D stem series.</summary>
    void Visit(Stem3DSeries series, RenderArea area);

    /// <summary>Renders a 3D bar series.</summary>
    void Visit(Bar3DSeries series, RenderArea area);

    /// <summary>Renders a planar 3D bar series — flat translucent rectangles in Y-planes.
    /// Default no-op for ISP compatibility with existing visitor implementations.</summary>
    [ExcludeFromCodeCoverage] void Visit(PlanarBar3DSeries series, RenderArea area) { }

    // ── v1.0 Signal series (default no-ops for ISP compatibility) ──

    /// <summary>Renders a monotonic-XY signal series. Default is a no-op for ISP compatibility.</summary>
    [ExcludeFromCodeCoverage] void Visit(SignalXYSeries series, RenderArea area) { }

    /// <summary>Renders a uniform-sample-rate signal series. Default is a no-op for ISP compatibility.</summary>
    [ExcludeFromCodeCoverage] void Visit(SignalSeries series, RenderArea area) { }

    // ── v1.1.1 Polar heatmap (default no-op for ISP compatibility) ──

    /// <summary>Renders a polar heatmap series (wedge/sector cells). Default is a no-op for ISP compatibility.</summary>
    [ExcludeFromCodeCoverage] void Visit(PolarHeatmapSeries series, RenderArea area) { }

    // ── v1.3.0 3D series enhancements (default no-ops for ISP compatibility) ──

    /// <summary>Renders a 3D polyline series. Default is a no-op for ISP compatibility.</summary>
    [ExcludeFromCodeCoverage] void Visit(Line3DSeries series, RenderArea area) { }

    /// <summary>Renders a triangulated surface series. Default is a no-op for ISP compatibility.</summary>
    [ExcludeFromCodeCoverage] void Visit(Trisurf3DSeries series, RenderArea area) { }

    /// <summary>Renders a 3D contour series. Default is a no-op for ISP compatibility.</summary>
    [ExcludeFromCodeCoverage] void Visit(Contour3DSeries series, RenderArea area) { }

    /// <summary>Renders a 3D quiver (vector field) series. Default is a no-op for ISP compatibility.</summary>
    [ExcludeFromCodeCoverage] void Visit(Quiver3DSeries series, RenderArea area) { }

    /// <summary>Renders a voxel series. Default is a no-op for ISP compatibility.</summary>
    [ExcludeFromCodeCoverage] void Visit(VoxelSeries series, RenderArea area) { }

    /// <summary>Renders a 3D text annotation series. Default is a no-op for ISP compatibility.</summary>
    [ExcludeFromCodeCoverage] void Visit(Text3DSeries series, RenderArea area) { }

    // ── v1.4.0 Streaming series (default no-ops for ISP compatibility) ──

    /// <summary>Renders a streaming line series. Default is a no-op for ISP compatibility.</summary>
    [ExcludeFromCodeCoverage] void Visit(StreamingLineSeries series, RenderArea area) { }

    /// <summary>Renders a streaming scatter series. Default is a no-op for ISP compatibility.</summary>
    [ExcludeFromCodeCoverage] void Visit(StreamingScatterSeries series, RenderArea area) { }

    /// <summary>Renders a streaming signal series. Default is a no-op for ISP compatibility.</summary>
    [ExcludeFromCodeCoverage] void Visit(StreamingSignalSeries series, RenderArea area) { }

    /// <summary>Renders a streaming candlestick series. Default is a no-op for ISP compatibility.</summary>
    [ExcludeFromCodeCoverage] void Visit(StreamingCandlestickSeries series, RenderArea area) { }
}
