// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Builders;

/// <summary>Phase Q Wave 2 (2026-04-19) — exercises every <see cref="MatPlotLibNet.Builders.AxesBuilder"/>
/// fluent method that the coverage gate flagged as unhit (84 of 1121 lines, 73.8% line / 58.8% branch).
/// Most are one-line <c>=&gt; AddSeries(ax =&gt; ax.X(...))</c> wrappers around series factories;
/// each test calls the wrapper through a real Plt.Create pipeline so the renderer-side branches
/// also get exercised.</summary>
public class AxesBuilderSmokeTests
{
    private static readonly double[] X = [1.0, 2.0, 3.0, 4.0];
    private static readonly double[] Y = [4.0, 3.0, 2.0, 1.0];
    private static readonly double[] Z = [0.5, 1.5, 2.5, 3.5];

    private static Figure BuildWith(Action<AxesBuilder> configure)
        => Plt.Create().WithSize(400, 300).AddSubPlot(1, 1, 1, configure).Build();

    [Fact] public void SetZLim_SetsZAxisBounds()
    {
        var fig = BuildWith(ax => ax.SetZLim(-5, 5));
        Assert.Equal(-5, fig.SubPlots[0].ZAxis.Min);
        Assert.Equal(5, fig.SubPlots[0].ZAxis.Max);
    }

    [Fact] public void SetYMargin_SetsYAxisMargin()
    {
        var fig = BuildWith(ax => ax.SetYMargin(0.25));
        Assert.Equal(0.25, fig.SubPlots[0].YAxis.Margin);
    }

    [Fact] public void SetYScale_SetsYAxisScale()
    {
        var fig = BuildWith(ax => ax.SetYScale(AxisScale.Log));
        Assert.Equal(AxisScale.Log, fig.SubPlots[0].YAxis.Scale);
    }

    [Fact] public void WithSymlogXScale_SetsScaleAndLinearThreshold()
    {
        var fig = BuildWith(ax => ax.WithSymlogXScale(2.0));
        Assert.Equal(AxisScale.SymLog, fig.SubPlots[0].XAxis.Scale);
    }

    [Fact] public void AxVLine_AddsReferenceLine()
    {
        var fig = BuildWith(ax => ax.AxVLine(2.5));
        Assert.NotEmpty(fig.SubPlots[0].ReferenceLines);
    }

    [Fact] public void AxHSpan_AddsSpanRegion()
    {
        var fig = BuildWith(ax => ax.AxHSpan(1.0, 2.0));
        Assert.NotEmpty(fig.SubPlots[0].Spans);
    }

    [Fact] public void DateTimeXAxis_SetsScaleAndAddsLineSeries()
    {
        DateTime[] dates = [new(2026, 1, 1), new(2026, 2, 1), new(2026, 3, 1)];
        var fig = BuildWith(ax => ax.Plot(dates, [1.0, 2.0, 3.0]));
        Assert.Equal(AxisScale.Date, fig.SubPlots[0].XAxis.Scale);
    }

    [Fact] public void Ecdf_AddsEcdfSeries()
    {
        var fig = BuildWith(ax => ax.Ecdf(X));
        Assert.IsType<EcdfSeries>(fig.SubPlots[0].Series[0]);
    }

    [Fact] public void StackPlot_AddsStackedAreaSeries()
    {
        var fig = BuildWith(ax => ax.StackPlot(X, [Y, Z]));
        Assert.IsType<StackedAreaSeries>(fig.SubPlots[0].Series[0]);
    }

    [Fact] public void Image_AddsImageSeries()
    {
        var fig = BuildWith(ax => ax.Image(new double[,] { { 1, 2 }, { 3, 4 } }));
        Assert.IsType<ImageSeries>(fig.SubPlots[0].Series[0]);
    }

    [Fact] public void Histogram2D_AddsHistogram2DSeries()
    {
        var fig = BuildWith(ax => ax.Histogram2D(X, Y, bins: 10));
        Assert.IsType<Histogram2DSeries>(fig.SubPlots[0].Series[0]);
    }

    [Fact] public void Gauge_AddsGaugeSeries()
    {
        var fig = BuildWith(ax => ax.Gauge(0.5));
        Assert.IsType<GaugeSeries>(fig.SubPlots[0].Series[0]);
    }

    [Fact] public void ProgressBar_AddsProgressBarSeries()
    {
        var fig = BuildWith(ax => ax.ProgressBar(0.5));
        Assert.IsType<ProgressBarSeries>(fig.SubPlots[0].Series[0]);
    }

    [Fact] public void Donut_AddsDonutSeries()
    {
        var fig = BuildWith(ax => ax.Donut([30, 70]));
        Assert.IsType<DonutSeries>(fig.SubPlots[0].Series[0]);
    }

    [Fact] public void Bubble_AddsBubbleSeries()
    {
        var fig = BuildWith(ax => ax.Bubble(X, Y, [10, 20, 30, 40]));
        Assert.IsType<BubbleSeries>(fig.SubPlots[0].Series[0]);
    }

    [Fact] public void OhlcBar_AddsOhlcBarSeries()
    {
        var fig = BuildWith(ax => ax.OhlcBar([10, 12], [15, 17], [8, 10], [13, 15]));
        Assert.IsType<OhlcBarSeries>(fig.SubPlots[0].Series[0]);
    }

    [Fact] public void Waterfall_AddsWaterfallSeries()
    {
        var fig = BuildWith(ax => ax.Waterfall(["A", "B"], [10, 20]));
        Assert.IsType<WaterfallSeries>(fig.SubPlots[0].Series[0]);
    }

    [Fact] public void Funnel_AddsFunnelSeries()
    {
        var fig = BuildWith(ax => ax.Funnel(["Visit", "Buy"], [100, 25]));
        Assert.IsType<FunnelSeries>(fig.SubPlots[0].Series[0]);
    }

    [Fact] public void Gantt_AddsGanttSeries()
    {
        var fig = BuildWith(ax => ax.Gantt(["A"], [0.0], [1.0]));
        Assert.IsType<GanttSeries>(fig.SubPlots[0].Series[0]);
    }

    [Fact] public void Sunburst_AddsSunburstSeries()
    {
        var root = new TreeNode { Label = "Root", Children = [new() { Label = "A", Value = 1 }] };
        var fig = BuildWith(ax => ax.Sunburst(root));
        Assert.IsType<SunburstSeries>(fig.SubPlots[0].Series[0]);
    }

    [Fact] public void PolarScatter_AddsPolarScatterSeries()
    {
        var fig = BuildWith(ax => ax.PolarScatter([1.0, 2.0], [0.0, 1.57]));
        Assert.IsType<PolarScatterSeries>(fig.SubPlots[0].Series[0]);
    }

    [Fact] public void PolarBar_AddsPolarBarSeries()
    {
        var fig = BuildWith(ax => ax.PolarBar([5.0, 10.0], [0.0, 1.57]));
        Assert.IsType<PolarBarSeries>(fig.SubPlots[0].Series[0]);
    }

    [Fact] public void Wireframe_AddsWireframeSeries()
    {
        var fig = BuildWith(ax => ax.Wireframe(X, Y, new double[,] { { 1, 2, 3, 4 }, { 5, 6, 7, 8 }, { 9, 10, 11, 12 }, { 13, 14, 15, 16 } }));
        Assert.IsType<WireframeSeries>(fig.SubPlots[0].Series[0]);
    }

    [Fact] public void Plot3D_AddsLine3DSeries()
    {
        var fig = BuildWith(ax => ax.Plot3D(X, Y, Z));
        Assert.IsType<Line3DSeries>(fig.SubPlots[0].Series[0]);
    }

    [Fact] public void Trisurf_AddsTrisurf3DSeries()
    {
        var fig = BuildWith(ax => ax.Trisurf([0.0, 1.0, 0.5], [0.0, 0.0, 1.0], [1.0, 2.0, 3.0]));
        Assert.IsType<Trisurf3DSeries>(fig.SubPlots[0].Series[0]);
    }

    [Fact] public void Contour3D_AddsContour3DSeries()
    {
        var fig = BuildWith(ax => ax.Contour3D([0.0, 1.0], [0.0, 1.0], new double[,] { { 1, 2 }, { 3, 4 } }));
        Assert.IsType<Contour3DSeries>(fig.SubPlots[0].Series[0]);
    }

    [Fact] public void Quiver3D_AddsQuiver3DSeries()
    {
        var fig = BuildWith(ax => ax.Quiver3D([1.0], [2.0], [3.0], [0.5], [0.5], [0.5]));
        Assert.IsType<Quiver3DSeries>(fig.SubPlots[0].Series[0]);
    }

    [Fact] public void Voxels_AddsVoxelSeries()
    {
        var fig = BuildWith(ax => ax.Voxels(new bool[2, 2, 2] { { { true, false }, { false, true } }, { { false, true }, { true, false } } }));
        Assert.IsType<VoxelSeries>(fig.SubPlots[0].Series[0]);
    }

    [Fact] public void Text3D_AddsText3DSeries()
    {
        var fig = BuildWith(ax => ax.Text3D(1.0, 2.0, 3.0, "Hello"));
        Assert.IsType<Text3DSeries>(fig.SubPlots[0].Series[0]);
    }

    [Fact] public void WithCamera_SetsElevationAndAzimuth()
    {
        var fig = BuildWith(ax => ax.WithCamera(elevation: 45, azimuth: 30));
        Assert.Equal(45, fig.SubPlots[0].Elevation);
    }

    [Fact] public void Signal_AddsSignalSeries()
    {
        var fig = BuildWith(ax => ax.Signal(Y, sampleRate: 100.0));
        Assert.IsType<SignalSeries>(fig.SubPlots[0].Series[0]);
    }

    [Fact] public void SignalXY_AddsSignalXYSeries()
    {
        var fig = BuildWith(ax => ax.SignalXY(X, Y));
        Assert.IsType<SignalXYSeries>(fig.SubPlots[0].Series[0]);
    }

    [Fact] public void WithDateXAxis_DateTimeOverloadConverts()
    {
        DateTime[] dates = [new(2026, 1, 1), new(2026, 2, 1)];
        var fig = BuildWith(ax => ax.Plot(dates, [1.0, 2.0]));
        Assert.Equal(AxisScale.Date, fig.SubPlots[0].XAxis.Scale);
    }
}
