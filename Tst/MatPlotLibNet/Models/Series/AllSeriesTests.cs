// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies common <see cref="ISeries"/> behavior across all 28 series types.</summary>
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
        { new BoxSeries([[1.0, 2.0, 3.0]]), nameof(BoxSeries) },
        { new ViolinSeries([[1.0, 2.0, 3.0]]), nameof(ViolinSeries) },
        { new ContourSeries([1.0, 2.0], [1.0, 2.0], new double[,] { { 1, 2 }, { 3, 4 } }), nameof(ContourSeries) },
        { new StemSeries([1.0, 2.0], [3.0, 4.0]), nameof(StemSeries) },
        { new AreaSeries([1.0, 2.0], [3.0, 4.0]), nameof(AreaSeries) },
        { new StepSeries([1.0, 2.0], [3.0, 4.0]), nameof(StepSeries) },
        { new ErrorBarSeries([1.0, 2.0], [3.0, 4.0], [0.1, 0.1], [0.2, 0.2]), nameof(ErrorBarSeries) },
        { new CandlestickSeries([10.0], [15.0], [8.0], [13.0]), nameof(CandlestickSeries) },
        { new QuiverSeries([1.0], [2.0], [0.5], [0.5]), nameof(QuiverSeries) },
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

    /// <summary>Verifies that ZOrder defaults to zero for every series type.</summary>
    [Theory]
    [MemberData(nameof(AllSeriesInstances))]
    public void ZOrder_DefaultsToZero(ISeries series, string _)
        => Assert.Equal(0, series.ZOrder);

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
