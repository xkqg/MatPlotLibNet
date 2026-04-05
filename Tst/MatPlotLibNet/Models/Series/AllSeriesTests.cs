// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Models.Series;

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
    };

    [Theory]
    [MemberData(nameof(AllSeriesInstances))]
    public void Label_DefaultsToNull(ISeries series, string _)
        => Assert.Null(series.Label);

    [Theory]
    [MemberData(nameof(AllSeriesInstances))]
    public void Visible_DefaultsToTrue(ISeries series, string _)
        => Assert.True(series.Visible);

    [Theory]
    [MemberData(nameof(AllSeriesInstances))]
    public void ZOrder_DefaultsToZero(ISeries series, string _)
        => Assert.Equal(0, series.ZOrder);

    [Theory]
    [MemberData(nameof(AllSeriesInstances))]
    public void Accept_DispatchesToCorrectVisitorMethod(ISeries series, string expectedName)
    {
        var visitor = new TestSeriesVisitor();
        series.Accept(visitor, null!);
        Assert.Equal(expectedName, visitor.LastVisited);
    }

    [Theory]
    [MemberData(nameof(AllSeriesInstances))]
    public void Label_CanBeSetAndRead(ISeries series, string _)
    {
        series.Label = "test-label";
        Assert.Equal("test-label", series.Label);
    }

    [Theory]
    [MemberData(nameof(AllSeriesInstances))]
    public void Visible_CanBeSetToFalse(ISeries series, string _)
    {
        series.Visible = false;
        Assert.False(series.Visible);
    }

    [Theory]
    [MemberData(nameof(AllSeriesInstances))]
    public void ZOrder_CanBeSet(ISeries series, string _)
    {
        series.ZOrder = 42;
        Assert.Equal(42, series.ZOrder);
    }
}
