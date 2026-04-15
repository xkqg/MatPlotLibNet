// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies common <see cref="ISeries"/> behavior across all series types.</summary>
public class AllSeriesTests
{
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
        { new BrokenBarSeries([[(1.0, 2.0), (4.0, 1.0)]]), nameof(BrokenBarSeries) },
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
}
