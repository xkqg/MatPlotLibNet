// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Rendering;

/// <summary>Visitor interface for rendering each series type through the visitor pattern.</summary>
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
}
