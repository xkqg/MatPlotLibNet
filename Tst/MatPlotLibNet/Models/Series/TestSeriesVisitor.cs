// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Models.Series.Streaming;
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
    public void Visit(RugplotSeries series, RenderArea area) => LastVisited = nameof(RugplotSeries);
    public void Visit(StripplotSeries series, RenderArea area) => LastVisited = nameof(StripplotSeries);
    public void Visit(EventplotSeries series, RenderArea area) => LastVisited = nameof(EventplotSeries);
    public void Visit(BrokenBarSeries series, RenderArea area) => LastVisited = nameof(BrokenBarSeries);
    public void Visit(CountSeries series, RenderArea area) => LastVisited = nameof(CountSeries);
    public void Visit(PcolormeshSeries series, RenderArea area) => LastVisited = nameof(PcolormeshSeries);
    public void Visit(ResidualSeries series, RenderArea area) => LastVisited = nameof(ResidualSeries);
    public void Visit(PointplotSeries series, RenderArea area) => LastVisited = nameof(PointplotSeries);
    public void Visit(SwarmplotSeries series, RenderArea area) => LastVisited = nameof(SwarmplotSeries);
    public void Visit(SpectrogramSeries series, RenderArea area) => LastVisited = nameof(SpectrogramSeries);
    public void Visit(TableSeries series, RenderArea area) => LastVisited = nameof(TableSeries);
    public void Visit(TricontourSeries series, RenderArea area) => LastVisited = nameof(TricontourSeries);
    public void Visit(TripcolorSeries series, RenderArea area) => LastVisited = nameof(TripcolorSeries);
    public void Visit(QuiverKeySeries series, RenderArea area) => LastVisited = nameof(QuiverKeySeries);
    public void Visit(BarbsSeries series, RenderArea area) => LastVisited = nameof(BarbsSeries);
    public void Visit(Stem3DSeries series, RenderArea area) => LastVisited = nameof(Stem3DSeries);
    public void Visit(Bar3DSeries series, RenderArea area) => LastVisited = nameof(Bar3DSeries);
    public void Visit(PlanarBar3DSeries series, RenderArea area) => LastVisited = nameof(PlanarBar3DSeries);
    public void Visit(Text3DSeries series, RenderArea area) => LastVisited = nameof(Text3DSeries);
    public void Visit(Line3DSeries series, RenderArea area) => LastVisited = nameof(Line3DSeries);
    public void Visit(Trisurf3DSeries series, RenderArea area) => LastVisited = nameof(Trisurf3DSeries);
    public void Visit(Contour3DSeries series, RenderArea area) => LastVisited = nameof(Contour3DSeries);
    public void Visit(Quiver3DSeries series, RenderArea area) => LastVisited = nameof(Quiver3DSeries);
    public void Visit(VoxelSeries series, RenderArea area) => LastVisited = nameof(VoxelSeries);

    // ── Batch C extension: 7 additional Visit overloads (2026-04-18) ─────────
    public void Visit(PolarHeatmapSeries series, RenderArea area) => LastVisited = nameof(PolarHeatmapSeries);
    public void Visit(SignalSeries series, RenderArea area) => LastVisited = nameof(SignalSeries);
    public void Visit(SignalXYSeries series, RenderArea area) => LastVisited = nameof(SignalXYSeries);
    public void Visit(StreamingLineSeries series, RenderArea area) => LastVisited = nameof(StreamingLineSeries);
    public void Visit(StreamingScatterSeries series, RenderArea area) => LastVisited = nameof(StreamingScatterSeries);
    public void Visit(StreamingSignalSeries series, RenderArea area) => LastVisited = nameof(StreamingSignalSeries);
    public void Visit(StreamingCandlestickSeries series, RenderArea area) => LastVisited = nameof(StreamingCandlestickSeries);

    // ── v1.10 Pair-Selection Visualisation Pack ──
    public void Visit(DendrogramSeries series, RenderArea area) => LastVisited = nameof(DendrogramSeries);
    public void Visit(ClustermapSeries series, RenderArea area) => LastVisited = nameof(ClustermapSeries);
    public void Visit(PairGridSeries series, RenderArea area) => LastVisited = nameof(PairGridSeries);
    public void Visit(NetworkGraphSeries series, RenderArea area) => LastVisited = nameof(NetworkGraphSeries);
}
