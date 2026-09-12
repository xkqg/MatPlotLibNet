// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Every series says its own data as a table, the way every series already says its own DTO — one
/// definition per type, no central switch. This is the cross-cutting conformance run over
/// <see cref="AllSeriesTests.AllSeriesInstances"/>: what a type returns is its business, but the SHAPE it returns
/// is the contract, and a series that quietly stops answering is a table that quietly loses a row.</summary>
public class SeriesDataTableTests
{
    /// <summary>The series types that have no tabular form. Kept here as the one list, and pinned against the
    /// cookbook by <c>ReleaseContractTests</c> so the page and the code cannot drift.</summary>
    public static readonly string[] WithoutATabularForm =
    [
        nameof(QuiverKeySeries),   // a reference arrow: an annotation, not data
        nameof(Text3DSeries),      // text placed in a 3-D scene
    ];

    [Theory]
    [MemberData(nameof(AllSeriesTests.AllSeriesInstances), MemberType = typeof(AllSeriesTests))]
    public void EverySeries_AnswersWithoutThrowing(ISeries series, string label)
    {
        var table = series.ToDataTable();

        if (WithoutATabularForm.Contains(label))
        {
            Assert.Null(table);
            return;
        }

        Assert.NotNull(table);
    }

    [Theory]
    [MemberData(nameof(AllSeriesTests.AllSeriesInstances), MemberType = typeof(AllSeriesTests))]
    public void ASeriesTable_HasColumnsAndNoCaption(ISeries series, string label)
    {
        var table = series.ToDataTable();
        if (table is null)
        {
            return;
        }

        // The series knows its data, never the figure's name: the caption is the figure's to add.
        Assert.Null(table.Caption);
        Assert.NotEmpty(table.Columns);
        Assert.All(table.Columns, column => Assert.False(string.IsNullOrWhiteSpace(column.Header), label));
    }

    [Theory]
    [MemberData(nameof(AllSeriesTests.AllSeriesInstances), MemberType = typeof(AllSeriesTests))]
    public void ASeriesTable_NamesItsValueColumnAfterTheSeriesLabel(ISeries series, string _)
    {
        series.Label = "Revenue";
        var table = series.ToDataTable();
        if (table is null || table.Columns.Count < 2)
        {
            return;
        }

        // A labelled series is named in the table the way it is named in the legend — for the families that have
        // exactly one value column. A multi-column family (OHLC, Gantt, a stack) keeps its own column names.
        if (series is XYSeries or PolarSeries or BarSeries or WaterfallSeries or RadarSeries or FunnelSeries
            or PieSeries or DonutSeries or CountSeries or StatTileSeries or SparklineSeries)
        {
            Assert.Contains(table.Columns, column => column.Header == "Revenue");
        }
    }

    // ---- the shapes that carry the families ---------------------------------------------------------------------

    [Fact]
    public void AnXYSeries_IsTwoColumns_XAndTheLabel()
    {
        var table = new LineSeries([1, 2, 3], [12, 18, 15]) { Label = "2026" }.ToDataTable()!;

        Assert.Equal(["x", "2026"], table.Columns.Select(c => c.Header));
        Assert.Equal(3, table.RowCount);
        Assert.Equal(15.0, table.Rows[2][1].Number);
    }

    [Fact]
    public void AnUnlabelledXYSeries_NamesItsValueColumnY()
    {
        var table = new LineSeries([1], [2]).ToDataTable()!;

        Assert.Equal(["x", "y"], table.Columns.Select(c => c.Header));
    }

    [Fact]
    public void AnXYSeries_EmptyInput_IsAnEmptyTableNotNull()
    {
        var table = new LineSeries([], []).ToDataTable()!;

        Assert.Equal(0, table.RowCount);
        Assert.Equal(2, table.Columns.Count);
    }

    [Fact]
    public void AnXYSeries_SinglePoint_HasOneRow()
    {
        Assert.Equal(1, new LineSeries([1], [2]).ToDataTable()!.RowCount);
    }

    [Fact]
    public void AnXYSeries_WithMismatchedLengths_StopsAtTheShorterOne()
    {
        // The models permit it (only the builder validates); the table reports pairs, never an index out of range.
        var series = new LineSeries([1, 2, 3], [10, 20]);

        Assert.Equal(2, series.ToDataTable()!.RowCount);
    }

    [Fact]
    public void AnXYSeries_NaN_AndInfinity_ReachTheTableUnchanged()
    {
        var table = new LineSeries([1, 2], [double.NaN, double.PositiveInfinity]).ToDataTable()!;

        Assert.True(double.IsNaN(table.Rows[0][1].Number));
        Assert.True(double.IsPositiveInfinity(table.Rows[1][1].Number));
    }

    [Fact]
    public void ACategoricalSeries_IsACategoryColumnAndAValueColumn()
    {
        var table = new BarSeries(["A", "B"], [1, 2]) { Label = "Sales" }.ToDataTable()!;

        Assert.Equal(["category", "Sales"], table.Columns.Select(c => c.Header));
        Assert.Equal(DataColumnKind.Text, table.Columns[0].Kind);
        Assert.Equal("B", table.Rows[1][0].Text);
        Assert.Equal(2.0, table.Rows[1][1].Number);
    }

    [Fact]
    public void ABarSeries_StackedOrNot_ReportsItsOwnValues()
    {
        // The table is the data: a stacked bar draws a cumulative top, and its own value is what it contributes.
        var table = new BarSeries(["A"], [3]) { StackBaseline = [10] }.ToDataTable()!;

        Assert.Equal(3.0, table.Rows[0][1].Number);
    }

    [Fact]
    public void AnOhlcSeries_IsFiveColumns()
    {
        var table = new CandlestickSeries([10], [15], [8], [13]).ToDataTable()!;

        Assert.Equal(["x", "open", "high", "low", "close"], table.Columns.Select(c => c.Header));
        Assert.Equal([0.0, 10.0, 15.0, 8.0, 13.0], table.Rows[0].Select(c => c.Number));
    }

    [Fact]
    public void AnOhlcSeries_WithDateLabels_NamesTheFirstColumnDateAndPrintsThem()
    {
        var table = new CandlestickSeries([10], [15], [8], [13]) { DateLabels = ["2026-03-15"] }.ToDataTable()!;

        Assert.Equal("date", table.Columns[0].Header);
        Assert.Equal(DataColumnKind.Text, table.Columns[0].Kind);
        Assert.Equal("2026-03-15", table.Rows[0][0].Text);
    }

    [Fact]
    public void ADatasetSeries_IsLongForm_DatasetThenValue()
    {
        var table = new BoxSeries([[1, 2], [3]]).ToDataTable()!;

        Assert.Equal(["dataset", "value"], table.Columns.Select(c => c.Header));
        Assert.Equal(3, table.RowCount);
        Assert.Equal("1", table.Rows[0][0].Text);
        Assert.Equal("2", table.Rows[2][0].Text);
        Assert.Equal(3.0, table.Rows[2][1].Number);
    }

    [Fact]
    public void APolarSeries_IsThetaThenR()
    {
        var table = new PolarLineSeries([5, 10], [0, 1.57]) { Label = "signal" }.ToDataTable()!;

        Assert.Equal(["theta", "signal"], table.Columns.Select(c => c.Header));
        Assert.Equal(1.57, table.Rows[1][0].Number);
        Assert.Equal(10.0, table.Rows[1][1].Number);
    }

    [Fact]
    public void AStreamingSeries_ReportsItsSnapshot()
    {
        var series = new MatPlotLibNet.Models.Series.Streaming.StreamingLineSeries(capacity: 4);
        series.AppendPoint(1, 10);
        series.AppendPoint(2, 20);

        var table = series.ToDataTable()!;

        Assert.Equal(2, table.RowCount);
        Assert.Equal(20.0, table.Rows[1][1].Number);
    }

    [Fact]
    public void AStreamingSeries_EmptyInput_IsAnEmptyTable()
    {
        Assert.Equal(0, new MatPlotLibNet.Models.Series.Streaming.StreamingLineSeries(capacity: 4).ToDataTable()!.RowCount);
    }

    [Fact]
    public void AGridSeries_IsLongForm_RowColumnValue()
    {
        var table = new HeatmapSeries(new double[,] { { 1, 2 }, { 3, 4 } }).ToDataTable()!;

        Assert.Equal(["row", "column", "value"], table.Columns.Select(c => c.Header));
        Assert.Equal(4, table.RowCount);
        Assert.Equal([1.0, 1.0, 4.0], [table.Rows[3][0].Number, table.Rows[3][1].Number, table.Rows[3][2].Number]);
    }

    [Fact]
    public void AStackedArea_IsOneColumnPerStack()
    {
        var series = new StackedAreaSeries([1, 2], [[1, 2], [3, 4]]) { Labels = ["a", "b"] };

        var table = series.ToDataTable()!;

        Assert.Equal(["x", "a", "b"], table.Columns.Select(c => c.Header));
        Assert.Equal(4.0, table.Rows[1][2].Number);
    }

    [Fact]
    public void AStackedArea_WithoutLabels_NumbersItsStacks()
    {
        var table = new StackedAreaSeries([1], [[1], [2]]).ToDataTable()!;

        Assert.Equal(["x", "series 1", "series 2"], table.Columns.Select(c => c.Header));
    }

    [Fact]
    public void APieSeries_IsLabelThenSize()
    {
        var table = new PieSeries([30, 70]) { Labels = ["A", "B"] }.ToDataTable()!;

        Assert.Equal(["label", "size"], table.Columns.Select(c => c.Header));
        Assert.Equal("B", table.Rows[1][0].Text);
        Assert.Equal(70.0, table.Rows[1][1].Number);
    }

    [Fact]
    public void APieSeries_WithoutLabels_NumbersItsSlices()
    {
        var table = new PieSeries([30, 70]).ToDataTable()!;

        Assert.Equal("slice 1", table.Rows[0][0].Text);
    }

    [Fact]
    public void AGanttSeries_IsTaskStartEnd()
    {
        var table = new GanttSeries(["design"], [0], [5]).ToDataTable()!;

        Assert.Equal(["task", "start", "end"], table.Columns.Select(c => c.Header));
        Assert.Equal("design", table.Rows[0][0].Text);
        Assert.Equal(5.0, table.Rows[0][2].Number);
    }

    [Fact]
    public void AStateTimeline_IsStateStartEnd()
    {
        var table = new StateTimelineSeries([new StateSegment(0, 1, "Up", MatPlotLibNet.Styling.Colors.Green)]).ToDataTable()!;

        Assert.Equal(["state", "start", "end"], table.Columns.Select(c => c.Header));
        Assert.Equal("Up", table.Rows[0][0].Text);
    }

    [Fact]
    public void AStatTile_IsOneRow()
    {
        var table = new StatTileSeries(42) { Label = "Processes" }.ToDataTable()!;

        Assert.Equal(["label", "Processes"], table.Columns.Select(c => c.Header));
        Assert.Equal(1, table.RowCount);
        Assert.Equal(42.0, table.Rows[0][1].Number);
    }

    [Fact]
    public void ATableSeries_ReportsItsOwnCellsAndHeaders()
    {
        var series = new TableSeries([["a", "b"], ["c", "d"]]) { ColumnHeaders = ["one", "two"] };

        var table = series.ToDataTable()!;

        Assert.Equal(["one", "two"], table.Columns.Select(c => c.Header));
        Assert.All(table.Columns, c => Assert.Equal(DataColumnKind.Text, c.Kind));
        Assert.Equal("d", table.Rows[1][1].Text);
    }

    [Fact]
    public void ATableSeries_WithRowHeaders_KeepsThemAsTheFirstColumn()
    {
        var series = new TableSeries([["a"]]) { ColumnHeaders = ["one"], RowHeaders = ["r1"] };

        var table = series.ToDataTable()!;

        Assert.Equal(["", "one"], table.Columns.Select(c => c.Header));
        Assert.Equal("r1", table.Rows[0][0].Text);
        Assert.Equal("a", table.Rows[0][1].Text);
    }

    [Fact]
    public void AHistogram_IsBinStartAndCount()
    {
        var table = new HistogramSeries([1, 2, 3, 4]) { Bins = 2 }.ToDataTable()!;

        Assert.Equal(["bin start", "count"], table.Columns.Select(c => c.Header));
        Assert.Equal(2, table.RowCount);
        Assert.Equal(4.0, table.Rows.Sum(r => r[1].Number));
    }

    [Fact]
    public void AHierarchicalSeries_WalksItsTree()
    {
        var root = new TreeNode
        {
            Label = "Root",
            Children = [new TreeNode { Label = "A", Value = 10 }, new TreeNode { Label = "B", Value = 5 }],
        };

        var table = new TreemapSeries(root).ToDataTable()!;

        // The depth is a column, not an indent: a flattened tree that keeps its depth is still a tree.
        Assert.Equal(["label", "depth", "value"], table.Columns.Select(c => c.Header));
        Assert.Equal(3, table.RowCount);
        Assert.Contains(table.Rows, r => r[0].Text == "A" && r[1].Number == 1 && r[2].Number == 10);
    }

    [Fact]
    public void ASankeySeries_IsSourceTargetValue()
    {
        var series = new SankeySeries([new SankeyNode("A"), new SankeyNode("B")], [new SankeyLink(0, 1, 10)]);

        var table = series.ToDataTable()!;

        Assert.Equal(["source", "target", "value"], table.Columns.Select(c => c.Header));
        Assert.Equal("A", table.Rows[0][0].Text);
        Assert.Equal("B", table.Rows[0][1].Text);
        Assert.Equal(10.0, table.Rows[0][2].Number);
    }

    [Fact]
    public void ANetworkGraph_IsItsEdges()
    {
        var series = new NetworkGraphSeries([new GraphNode("a"), new GraphNode("b")], [new GraphEdge("a", "b", 2)]);

        var table = series.ToDataTable()!;

        Assert.Equal(["from", "to", "weight"], table.Columns.Select(c => c.Header));
        Assert.Equal(2.0, table.Rows[0][2].Number);
    }

    [Fact]
    public void AThreeDPointSeries_IsXYZ()
    {
        var table = new Scatter3DSeries(new double[] { 1 }, new double[] { 2 }, new double[] { 3 }).ToDataTable()!;

        Assert.Equal(["x", "y", "z"], table.Columns.Select(c => c.Header));
        Assert.Equal([1.0, 2.0, 3.0], table.Rows[0].Select(c => c.Number));
    }

    [Fact]
    public void AThreeDGridSeries_IsLongFormXYZ()
    {
        var table = new SurfaceSeries([1, 2], [3, 4], new double[,] { { 5, 6 }, { 7, 8 } }).ToDataTable()!;

        Assert.Equal(["x", "y", "z"], table.Columns.Select(c => c.Header));
        Assert.Equal(4, table.RowCount);
    }

    [Fact]
    public void AnErrorBarSeries_CarriesItsErrorColumns()
    {
        var table = new ErrorBarSeries([1], [2], [0.1], [0.2]).ToDataTable()!;

        Assert.Equal(["x", "y", "y error low", "y error high"], table.Columns.Select(c => c.Header));
        Assert.Equal(0.2, table.Rows[0][3].Number);
    }

    [Fact]
    public void AQuiverSeries_IsXYUV()
    {
        var table = new QuiverSeries([1], [2], [0.5], [0.6]).ToDataTable()!;

        Assert.Equal(["x", "y", "u", "v"], table.Columns.Select(c => c.Header));
        Assert.Equal(0.6, table.Rows[0][3].Number);
    }

    [Fact]
    public void ABubbleSeries_CarriesItsSizes()
    {
        var table = new BubbleSeries([1], [2], [10]).ToDataTable()!;

        Assert.Equal(["x", "y", "size"], table.Columns.Select(c => c.Header));
        Assert.Equal(10.0, table.Rows[0][2].Number);
    }
}
