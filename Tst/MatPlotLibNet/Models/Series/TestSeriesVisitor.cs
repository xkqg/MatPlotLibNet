// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Test helper that records the last visited series type name.</summary>
internal sealed class TestSeriesVisitor : ISeriesVisitor
{
    public string? LastVisited { get; private set; }

    public void Visit(LineSeries series, RenderArea area) => LastVisited = nameof(LineSeries);
    public void Visit(ScatterSeries series, RenderArea area) => LastVisited = nameof(ScatterSeries);
    public void Visit(BarSeries series, RenderArea area) => LastVisited = nameof(BarSeries);
    public void Visit(HistogramSeries series, RenderArea area) => LastVisited = nameof(HistogramSeries);
    public void Visit(PieSeries series, RenderArea area) => LastVisited = nameof(PieSeries);
    public void Visit(HeatmapSeries series, RenderArea area) => LastVisited = nameof(HeatmapSeries);
    public void Visit(BoxSeries series, RenderArea area) => LastVisited = nameof(BoxSeries);
    public void Visit(ViolinSeries series, RenderArea area) => LastVisited = nameof(ViolinSeries);
    public void Visit(KdeSeries series, RenderArea area) => LastVisited = nameof(KdeSeries);
    public void Visit(RegressionSeries series, RenderArea area) => LastVisited = nameof(RegressionSeries);
    public void Visit(HexbinSeries series, RenderArea area) => LastVisited = nameof(HexbinSeries);
    public void Visit(ContourSeries series, RenderArea area) => LastVisited = nameof(ContourSeries);
    public void Visit(ContourfSeries series, RenderArea area) => LastVisited = nameof(ContourfSeries);
    public void Visit(StemSeries series, RenderArea area) => LastVisited = nameof(StemSeries);
    public void Visit(AreaSeries series, RenderArea area) => LastVisited = nameof(AreaSeries);
    public void Visit(StepSeries series, RenderArea area) => LastVisited = nameof(StepSeries);
    public void Visit(EcdfSeries series, RenderArea area) => LastVisited = nameof(EcdfSeries);
    public void Visit(ImageSeries series, RenderArea area) => LastVisited = nameof(ImageSeries);
    public void Visit(Histogram2DSeries series, RenderArea area) => LastVisited = nameof(Histogram2DSeries);
    public void Visit(StackedAreaSeries series, RenderArea area) => LastVisited = nameof(StackedAreaSeries);
    public void Visit(ErrorBarSeries series, RenderArea area) => LastVisited = nameof(ErrorBarSeries);
    public void Visit(CandlestickSeries series, RenderArea area) => LastVisited = nameof(CandlestickSeries);
    public void Visit(QuiverSeries series, RenderArea area) => LastVisited = nameof(QuiverSeries);
    public void Visit(StreamplotSeries series, RenderArea area) => LastVisited = nameof(StreamplotSeries);
    public void Visit(RadarSeries series, RenderArea area) => LastVisited = nameof(RadarSeries);
    public void Visit(DonutSeries series, RenderArea area) => LastVisited = nameof(DonutSeries);
    public void Visit(BubbleSeries series, RenderArea area) => LastVisited = nameof(BubbleSeries);
    public void Visit(OhlcBarSeries series, RenderArea area) => LastVisited = nameof(OhlcBarSeries);
    public void Visit(WaterfallSeries series, RenderArea area) => LastVisited = nameof(WaterfallSeries);
    public void Visit(FunnelSeries series, RenderArea area) => LastVisited = nameof(FunnelSeries);
    public void Visit(GanttSeries series, RenderArea area) => LastVisited = nameof(GanttSeries);
    public void Visit(GaugeSeries series, RenderArea area) => LastVisited = nameof(GaugeSeries);
    public void Visit(ProgressBarSeries series, RenderArea area) => LastVisited = nameof(ProgressBarSeries);
    public void Visit(SparklineSeries series, RenderArea area) => LastVisited = nameof(SparklineSeries);
    public void Visit(TreemapSeries series, RenderArea area) => LastVisited = nameof(TreemapSeries);
    public void Visit(SunburstSeries series, RenderArea area) => LastVisited = nameof(SunburstSeries);
    public void Visit(SankeySeries series, RenderArea area) => LastVisited = nameof(SankeySeries);
    public void Visit(PolarLineSeries series, RenderArea area) => LastVisited = nameof(PolarLineSeries);
    public void Visit(PolarScatterSeries series, RenderArea area) => LastVisited = nameof(PolarScatterSeries);
    public void Visit(PolarBarSeries series, RenderArea area) => LastVisited = nameof(PolarBarSeries);
    public void Visit(SurfaceSeries series, RenderArea area) => LastVisited = nameof(SurfaceSeries);
    public void Visit(WireframeSeries series, RenderArea area) => LastVisited = nameof(WireframeSeries);
    public void Visit(Scatter3DSeries series, RenderArea area) => LastVisited = nameof(Scatter3DSeries);
}
