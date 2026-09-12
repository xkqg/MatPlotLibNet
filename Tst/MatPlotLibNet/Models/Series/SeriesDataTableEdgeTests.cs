// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Every fallback arm of every table a series builds: the label list that is absent, the one that is
/// shorter than the data, the optional column that is not there. Each of these is a branch a chart hits the
/// first time a caller leaves something out, and each would otherwise report a wrong name or throw.</summary>
public class SeriesDataTableEdgeTests
{
    /// <summary>A series outside the <see cref="ChartSeries"/> hierarchy — what the interface's own default
    /// answers for.</summary>
    private sealed class BareSeries : ISeries
    {
        public string? Label { get; set; }

        public bool Visible { get; set; } = true;

        public int ZOrder { get; set; }

        public void Accept(ISeriesVisitor visitor, RenderArea area)
        {
        }

        public DataRangeContribution ComputeDataRange(IAxesContext context) => new(null, null, null, null);

        public SeriesDto ToSeriesDto() => new() { Type = "bare" };
    }

    [Fact]
    public void ASeriesOutsideTheHierarchy_GetsTheInterfacesOwnDefault()
    {
        Assert.Null(((ISeries)new BareSeries()).ToDataTable());
    }

    // ---- a column needs a header -------------------------------------------------------------------------------

    [Fact]
    public void AColumnWithoutAHeader_IsRefused()
    {
        Assert.Throws<ArgumentNullException>(() => new ChartDataColumn(null!));
    }

    // ---- the label lists that may be absent or short ------------------------------------------------------------

    [Fact]
    public void APieWithFewerLabelsThanSlices_NumbersTheRest()
    {
        var table = new PieSeries([1, 2, 3]) { Labels = ["only"] }.ToDataTable()!;

        Assert.Equal(["only", "slice 2", "slice 3"], table.Rows.Select(r => r[0].Text));
    }

    [Fact]
    public void ADonutWithoutLabels_NumbersItsSlices()
    {
        var table = new DonutSeries([1, 2]).ToDataTable()!;

        Assert.Equal(["slice 1", "slice 2"], table.Rows.Select(r => r[0].Text));
    }

    [Fact]
    public void ADonutWithFewerLabelsThanSlices_NumbersTheRest()
    {
        var table = new DonutSeries([1, 2, 3]) { Labels = ["only"] }.ToDataTable()!;

        Assert.Equal(["only", "slice 2", "slice 3"], table.Rows.Select(r => r[0].Text));
    }

    [Fact]
    public void AFilledContourWhoseAxesAreShorterThanItsGrid_FallsBackToTheIndex()
    {
        var series = new ContourfSeries([10.0], [20.0], new double[,] { { 1, 2 }, { 3, 4 } });

        var table = series.ToDataTable()!;

        Assert.Equal([10.0, 20.0], [table.Rows[0][0].Number, table.Rows[0][1].Number]);
        Assert.Equal([1.0, 1.0], [table.Rows[3][0].Number, table.Rows[3][1].Number]);
    }

    [Fact]
    public void AStackedAreaWithFewerLabelsThanStacks_NumbersTheRest()
    {
        var table = new StackedAreaSeries([1], [[1], [2]]) { Labels = ["first"] }.ToDataTable()!;

        Assert.Equal(["x", "first", "series 2"], table.Columns.Select(c => c.Header));
    }

    [Fact]
    public void ABrokenBarWithoutLabels_NumbersItsRows()
    {
        var table = new BrokenBarSeries([[new BarRange(1.0, 2.0)], [new BarRange(3.0, 1.0)]]).ToDataTable()!;

        Assert.Equal(["1", "2"], table.Rows.Select(r => r[0].Text));
    }

    [Fact]
    public void ABrokenBarWithLabels_UsesThem()
    {
        var table = new BrokenBarSeries([[new BarRange(1.0, 2.0)]]) { Labels = ["machine"] }.ToDataTable()!;

        Assert.Equal("machine", table.Rows[0][0].Text);
    }

    [Fact]
    public void APointplotWithoutCategories_NumbersThem()
    {
        var table = new PointplotSeries([[1.0, 2.0], [3.0]]).ToDataTable()!;

        Assert.Equal(["1", "1", "2"], table.Rows.Select(r => r[0].Text));
    }

    [Fact]
    public void APointplotWithCategories_UsesThem()
    {
        var table = new PointplotSeries([[1.0], [2.0]]) { Categories = ["a", "b"] }.ToDataTable()!;

        Assert.Equal(["a", "b"], table.Rows.Select(r => r[0].Text));
    }

    [Fact]
    public void APairGridWithoutLabels_NumbersItsVariables()
    {
        var table = new PairGridSeries([[1.0], [2.0]]).ToDataTable()!;

        Assert.Equal(["variable 1", "variable 2"], table.Rows.Select(r => r[0].Text));
    }

    [Fact]
    public void APairGridWithLabels_UsesThem()
    {
        var table = new PairGridSeries([[1.0]]) { Labels = ["height"] }.ToDataTable()!;

        Assert.Equal("height", table.Rows[0][0].Text);
    }

    [Fact]
    public void ARelativeRotationWithFewerLabelsThanAssets_NumbersTheRest()
    {
        double[][] closes = [[1, 2, 3, 4, 5, 6, 7, 8], [2, 3, 4, 5, 6, 7, 8, 9]];
        double[] benchmark = [1, 1, 1, 1, 1, 1, 1, 1];
        var series = new RelativeRotationSeries(closes, benchmark, ["one", "two"]);

        var table = series.ToDataTable()!;

        Assert.Equal(["asset", "rs-ratio", "rs-momentum"], table.Columns.Select(c => c.Header));
        Assert.Contains(table.Rows, r => r[0].Text == "one");
        Assert.Contains(table.Rows, r => r[0].Text == "two");
    }

    // ---- optional columns ---------------------------------------------------------------------------------------

    [Fact]
    public void AnErrorBarWithoutErrors_IsJustAPointTable()
    {
        var series = new ErrorBarSeries([1], [2], [], []);

        Assert.Equal(["x", "y"], series.ToDataTable()!.Columns.Select(c => c.Header));
    }

    [Fact]
    public void AnErrorBarWithEveryArm_CarriesFourErrorColumns()
    {
        var series = new ErrorBarSeries([1], [2], [0.1], [0.2])
        {
            XErrorLow = [0.3],
            XErrorHigh = [0.4],
        };

        var table = series.ToDataTable()!;

        Assert.Equal(
            ["x", "y", "y error low", "y error high", "x error low", "x error high"],
            table.Columns.Select(c => c.Header));
        Assert.Equal(0.4, table.Rows[0][5].Number);
    }

    [Fact]
    public void AnAreaWithoutASecondLine_IsTwoColumns()
    {
        Assert.Equal(2, new AreaSeries([1], [2]).ToDataTable()!.Columns.Count);
    }

    [Fact]
    public void AnAreaBetweenTwoLines_CarriesBoth()
    {
        var table = new AreaSeries([1], [2]) { YData2 = [3] }.ToDataTable()!;

        Assert.Equal(["x", "y", "y2"], table.Columns.Select(c => c.Header));
        Assert.Equal(3.0, table.Rows[0][2].Number);
    }

    [Fact]
    public void ABulletGraphWithoutATarget_IsTwoColumns()
    {
        Assert.Equal(2, new BulletGraphSeries(5).ToDataTable()!.Columns.Count);
    }

    [Fact]
    public void ABulletGraphWithATarget_CarriesIt()
    {
        var table = new BulletGraphSeries(5) { Target = 8 }.ToDataTable()!;

        Assert.Equal(["label", "value", "target"], table.Columns.Select(c => c.Header));
        Assert.Equal(8.0, table.Rows[0][2].Number);
    }

    [Fact]
    public void AnOhlcWithFewerDateLabelsThanBars_LeavesTheRestBlank()
    {
        var table = new CandlestickSeries([1, 2], [2, 3], [0, 1], [1, 2]) { DateLabels = ["day one"] }.ToDataTable()!;

        Assert.Equal("day one", table.Rows[0][0].Text);
        Assert.Equal("", table.Rows[1][0].Text);
    }

    [Fact]
    public void AnOhlcWithAnEmptyDateLabelArray_FallsBackToTheIndex()
    {
        var table = new CandlestickSeries([1], [2], [0], [1]) { DateLabels = [] }.ToDataTable()!;

        Assert.Equal("x", table.Columns[0].Header);
        Assert.Equal(0.0, table.Rows[0][0].Number);
    }

    // ---- grids whose axis arrays are shorter than the grid -------------------------------------------------------

    [Fact]
    public void AContourWhoseAxesAreShorterThanItsGrid_FallsBackToTheIndex()
    {
        var series = new ContourSeries([10.0], [20.0], new double[,] { { 1, 2 }, { 3, 4 } });

        var table = series.ToDataTable()!;

        Assert.Equal([10.0, 20.0], [table.Rows[0][0].Number, table.Rows[0][1].Number]);
        Assert.Equal([1.0, 1.0], [table.Rows[3][0].Number, table.Rows[3][1].Number]);
    }

    [Fact]
    public void AFilledContourReportsTheSameGrid()
    {
        var table = new ContourfSeries([1.0, 2.0], [3.0, 4.0], new double[,] { { 5, 6 }, { 7, 8 } }).ToDataTable()!;

        Assert.Equal(4, table.RowCount);
        Assert.Equal(8.0, table.Rows[3][2].Number);
    }

    [Fact]
    public void ASurfaceWhoseAxesAreShorterThanItsGrid_FallsBackToTheIndex()
    {
        var table = new SurfaceSeries([10.0], [20.0], new double[,] { { 1, 2 }, { 3, 4 } }).ToDataTable()!;

        Assert.Equal([1.0, 1.0, 4.0], [table.Rows[3][0].Number, table.Rows[3][1].Number, table.Rows[3][2].Number]);
    }

    [Fact]
    public void AContour3DWhoseAxesCoverTheGrid_UsesThem()
    {
        var table = new Contour3DSeries([1.0, 2.0], [3.0, 4.0], new double[,] { { 5, 6 }, { 7, 8 } }).ToDataTable()!;

        Assert.Equal([2.0, 4.0, 8.0], [table.Rows[3][0].Number, table.Rows[3][1].Number, table.Rows[3][2].Number]);
    }

    [Fact]
    public void AStreamplotWhoseAxesAreShorterThanItsField_FallsBackToTheIndex()
    {
        var series = new StreamplotSeries([10.0], [20.0],
            new double[,] { { 1, 0 }, { 0, 1 } }, new double[,] { { 0, 1 }, { 1, 0 } });

        var table = series.ToDataTable()!;

        Assert.Equal(["x", "y", "u", "v"], table.Columns.Select(c => c.Header));
        Assert.Equal([1.0, 1.0], [table.Rows[3][0].Number, table.Rows[3][1].Number]);
    }

    [Fact]
    public void AStreamplotWhoseAxesCoverTheField_UsesThem()
    {
        var series = new StreamplotSeries([10.0, 11.0], [20.0, 21.0],
            new double[,] { { 1, 0 }, { 0, 1 } }, new double[,] { { 0, 1 }, { 1, 0 } });

        Assert.Equal([11.0, 21.0], [series.ToDataTable()!.Rows[3][0].Number, series.ToDataTable()!.Rows[3][1].Number]);
    }

    // ---- the tables that carry their own headers -----------------------------------------------------------------

    [Fact]
    public void ATableSeriesWithoutHeaders_NumbersItsColumns()
    {
        var table = new TableSeries([["a", "b"]]).ToDataTable()!;

        Assert.Equal(["column 1", "column 2"], table.Columns.Select(c => c.Header));
    }

    [Fact]
    public void ATableSeriesWithRaggedRows_PadsTheShortOnes()
    {
        var table = new TableSeries([["a", "b"], ["c"]]).ToDataTable()!;

        Assert.Equal("", table.Rows[1][1].Text);
    }

    [Fact]
    public void AnEmptyTableSeries_StillHasAColumn()
    {
        var table = new TableSeries([]).ToDataTable()!;

        Assert.Single(table.Columns);
        Assert.Equal(0, table.RowCount);
    }

    [Fact]
    public void ATableSeriesWithFewerRowHeadersThanRows_LeavesTheRestBlank()
    {
        var series = new TableSeries([["a"], ["b"]]) { RowHeaders = ["one"] };

        var table = series.ToDataTable()!;

        Assert.Equal("one", table.Rows[0][0].Text);
        Assert.Equal("", table.Rows[1][0].Text);
    }

    [Fact]
    public void ATreeGridWithoutHeaders_NumbersItsColumns()
    {
        var series = new TreeGridSeries([new TreeGridRow("root", ["1", "2"])]);

        var table = series.ToDataTable()!;

        Assert.Equal(["label", "depth", "column 1", "column 2"], table.Columns.Select(c => c.Header));
        Assert.Equal("root", table.Rows[0][0].Text);
        Assert.Equal(0.0, table.Rows[0][1].Number);
    }

    [Fact]
    public void ATreeGridWithHeadersAndDepth_KeepsBoth()
    {
        var series = new TreeGridSeries(
            [new TreeGridRow("child", ["7"]) { Depth = 2 }]) { ColumnHeaders = ["count"] };

        var table = series.ToDataTable()!;

        Assert.Equal(["label", "depth", "count"], table.Columns.Select(c => c.Header));
        Assert.Equal(2.0, table.Rows[0][1].Number);
        Assert.Equal("7", table.Rows[0][2].Text);
    }

    [Fact]
    public void ATreeGridWithARaggedRow_PadsIt()
    {
        var series = new TreeGridSeries([new TreeGridRow("a", ["1", "2"]), new TreeGridRow("b", ["3"])]);

        Assert.Equal("", series.ToDataTable()!.Rows[1][3].Text);
    }

    [Fact]
    public void AnEmptyTreeGrid_IsAnEmptyTable()
    {
        var table = new TreeGridSeries([]).ToDataTable()!;

        Assert.Equal(0, table.RowCount);
        Assert.Equal(["label", "depth"], table.Columns.Select(c => c.Header));
    }

    // ---- the rest of the fallbacks --------------------------------------------------------------------------------

    [Fact]
    public void ASankeyLinkPointingOutsideItsNodeList_NamesTheIndex()
    {
        var series = new SankeySeries([new SankeyNode("A")], [new SankeyLink(0, 7, 3)]);

        var table = series.ToDataTable()!;

        Assert.Equal("A", table.Rows[0][0].Text);
        Assert.Equal("7", table.Rows[0][1].Text);
    }

    [Fact]
    public void ASankeyLinkWithANegativeIndex_NamesTheIndex()
    {
        var series = new SankeySeries([new SankeyNode("A")], [new SankeyLink(-1, 0, 3)]);

        Assert.Equal("-1", series.ToDataTable()!.Rows[0][0].Text);
    }

    [Fact]
    public void ASpectrogramWithoutASampleRate_StepsByOne()
    {
        var series = new SpectrogramSeries(new double[] { 1, 2, 3 }) { SampleRate = 0 };

        var table = series.ToDataTable()!;

        Assert.Equal([0.0, 1.0, 2.0], table.Rows.Select(r => r[0].Number));
    }

    [Fact]
    public void ASpectrogramWithASampleRate_StepsByItsPeriod()
    {
        var series = new SpectrogramSeries(new double[] { 1, 2 }) { SampleRate = 4 };

        Assert.Equal([0.0, 0.25], series.ToDataTable()!.Rows.Select(r => r[0].Number));
    }

    [Fact]
    public void AGaugeReportsItsDial()
    {
        var table = new GaugeSeries(50) { Min = 0, Max = 100 }.ToDataTable()!;

        Assert.Equal(["label", "value", "min", "max"], table.Columns.Select(c => c.Header));
        Assert.Equal(100.0, table.Rows[0][3].Number);
    }

    [Fact]
    public void AProgressBarReportsItsFraction()
    {
        Assert.Equal(0.25, new ProgressBarSeries(0.25).ToDataTable()!.Rows[0][1].Number);
    }

    [Fact]
    public void AVoxelGrid_ReportsOnlyItsFilledCells()
    {
        var filled = new bool[2, 1, 1];
        filled[1, 0, 0] = true;

        var table = new VoxelSeries(filled).ToDataTable()!;

        Assert.Equal(1, table.RowCount);
        Assert.Equal([1.0, 0.0, 0.0], table.Rows[0].Select(c => c.Number));
    }

    [Fact]
    public void ACountSeries_CountsItsGroups()
    {
        var table = new CountSeries(["a", "b", "a"]).ToDataTable()!;

        Assert.Equal(2, table.RowCount);
        Assert.Equal(2.0, table.Rows.Single(r => r[0].Text == "a")[1].Number);
    }

    [Fact]
    public void AnEventplot_IsOneRowPerEvent()
    {
        var table = new EventplotSeries([[1.0, 2.0], [3.0]]).ToDataTable()!;

        Assert.Equal(3, table.RowCount);
        Assert.Equal(["1", "1", "2"], table.Rows.Select(r => r[0].Text));
    }

    [Fact]
    public void APolarHeatmap_IsLongFormOverItsWedges()
    {
        var table = new PolarHeatmapSeries(new double[,] { { 1, 2 }, { 3, 4 } }, thetaBins: 2, rBins: 2).ToDataTable()!;

        Assert.Equal(["r bin", "theta bin", "value"], table.Columns.Select(c => c.Header));
        Assert.Equal(4, table.RowCount);
    }

    [Fact]
    public void AClustermap_TablesInTheOrderItDraws()
    {
        var table = new ClustermapSeries(new double[,] { { 1, 2 }, { 3, 4 } }).ToDataTable()!;

        Assert.Equal(["row", "column", "value"], table.Columns.Select(c => c.Header));
        Assert.Equal(4, table.RowCount);
    }

    [Fact]
    public void AQuiver3D_CarriesItsVectors()
    {
        var series = new Quiver3DSeries(
            new double[] { 1 }, new double[] { 2 }, new double[] { 3 },
            new double[] { 0.1 }, new double[] { 0.2 }, new double[] { 0.3 });

        var table = series.ToDataTable()!;

        Assert.Equal(["x", "y", "z", "u", "v", "w"], table.Columns.Select(c => c.Header));
        Assert.Equal(0.3, table.Rows[0][5].Number);
    }

    [Fact]
    public void APlanarBar3D_IsXYZ()
    {
        var series = new PlanarBar3DSeries(new double[] { 1 }, new double[] { 2 }, new double[] { 3 });

        Assert.Equal(["x", "y", "z"], series.ToDataTable()!.Columns.Select(c => c.Header));
    }

    [Fact]
    public void ATrisurf3D_IsItsVertices()
    {
        var series = new Trisurf3DSeries(
            new double[] { 0, 1, 0.5 }, new double[] { 0, 0, 1 }, new double[] { 1, 2, 3 });

        Assert.Equal(3, series.ToDataTable()!.RowCount);
    }

    [Fact]
    public void ABarbsSeries_CarriesSpeedAndDirection()
    {
        var series = new BarbsSeries(
            new double[] { 1 }, new double[] { 2 }, new double[] { 10 }, new double[] { 45 });

        Assert.Equal(["x", "y", "speed", "direction"], series.ToDataTable()!.Columns.Select(c => c.Header));
    }

    [Fact]
    public void ATricontour_IsItsUnstructuredPoints()
    {
        var series = new TricontourSeries(
            new double[] { 0, 1, 0.5 }, new double[] { 0, 0, 1 }, new double[] { 1, 2, 3 });

        Assert.Equal(3, series.ToDataTable()!.RowCount);
    }

    [Fact]
    public void ATripcolor_IsItsUnstructuredPoints()
    {
        var series = new TripcolorSeries(
            new double[] { 0, 1, 0.5 }, new double[] { 0, 0, 1 }, new double[] { 1, 2, 3 });

        Assert.Equal(3, series.ToDataTable()!.RowCount);
    }

    [Fact]
    public void AHistogram2D_IsItsObservations()
    {
        var table = new Histogram2DSeries([1.0, 2.0], [3.0, 4.0]).ToDataTable()!;

        Assert.Equal(["x", "y"], table.Columns.Select(c => c.Header));
        Assert.Equal(2, table.RowCount);
    }

    [Fact]
    public void AHexbin_IsItsObservations()
    {
        Assert.Equal(2, new HexbinSeries([1.0, 2.0], [3.0, 4.0]).ToDataTable()!.RowCount);
    }

    [Fact]
    public void APcolormesh_IsLongFormOverItsCells()
    {
        var series = new PcolormeshSeries(
            new double[] { 0, 1, 2 }, new double[] { 0, 1, 2 }, new double[,] { { 1, 2 }, { 3, 4 } });

        Assert.Equal(4, series.ToDataTable()!.RowCount);
    }

    [Fact]
    public void AnImageSeries_IsLongFormOverItsPixels()
    {
        Assert.Equal(4, new ImageSeries(new double[,] { { 1, 2 }, { 3, 4 } }).ToDataTable()!.RowCount);
    }

    [Fact]
    public void AKdeAndARugplot_AreTheirSamples()
    {
        Assert.Equal(3, new KdeSeries([1.0, 2.0, 3.0]).ToDataTable()!.RowCount);
        Assert.Equal(3, new RugplotSeries(new Vec([1.0, 2.0, 3.0])).ToDataTable()!.RowCount);
    }

    [Fact]
    public void AnEcdfAndASparkline_AreIndexedByPosition()
    {
        Assert.Equal(["index", "value"], new EcdfSeries([3.0, 1.0]).ToDataTable()!.Columns.Select(c => c.Header));
        Assert.Equal(["index", "value"], new SparklineSeries([1.0, 2.0]).ToDataTable()!.Columns.Select(c => c.Header));
    }

    [Fact]
    public void ARegressionAndAResidual_AreTheirObservations()
    {
        Assert.Equal(3, new RegressionSeries([1.0, 2.0, 3.0], [1.0, 2.0, 3.0]).ToDataTable()!.RowCount);
        Assert.Equal(3, new ResidualSeries(new Vec([1.0, 2.0, 3.0]), new Vec([2.0, 4.0, 6.0])).ToDataTable()!.RowCount);
    }

    [Fact]
    public void AStemAndAWaterfallAndAFunnel_CarryTheirOwnShapes()
    {
        Assert.Equal(["x", "y"], new StemSeries([1.0], [2.0]).ToDataTable()!.Columns.Select(c => c.Header));
        Assert.Equal("A", new WaterfallSeries(["A"], [1.0]).ToDataTable()!.Rows[0][0].Text);
        Assert.Equal("A", new FunnelSeries(["A"], [1.0]).ToDataTable()!.Rows[0][0].Text);
    }

    [Fact]
    public void ARadarAndAStreamingSignal_CarryTheirOwnShapes()
    {
        Assert.Equal("A", new RadarSeries(["A"], [1.0]).ToDataTable()!.Rows[0][0].Text);

        var signal = new MatPlotLibNet.Models.Series.Streaming.StreamingSignalSeries(capacity: 4, sampleRate: 2);
        signal.AppendSample(1);
        signal.AppendSample(2);
        Assert.Equal(2, signal.ToDataTable()!.RowCount);
    }

    [Fact]
    public void AStreamingCandlestick_IsFiveColumns()
    {
        var series = new MatPlotLibNet.Models.Series.Streaming.StreamingCandlestickSeries(capacity: 4);
        series.AppendBar(1, 2, 0, 1);

        var table = series.ToDataTable()!;

        Assert.Equal(["x", "open", "high", "low", "close"], table.Columns.Select(c => c.Header));
        Assert.Equal(1, table.RowCount);
    }

    [Fact]
    public void ASunburstAndADendrogram_WalkTheirTrees()
    {
        var root = new TreeNode { Label = "Root", Children = [new TreeNode { Label = "A", Value = 1 }] };

        Assert.Equal(2, new SunburstSeries(root).ToDataTable()!.RowCount);
        Assert.Equal(2, new DendrogramSeries(root).ToDataTable()!.RowCount);
    }
}
