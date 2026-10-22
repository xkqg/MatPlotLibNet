// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Extensions;

/// <summary>The data table of a chart built in one call names its rows by the names the chart shows. A verb puts its marks on
/// the centres of the slots whose names its axis shows, so a mark's position IS a name: the table reads it as that name,
/// asking the axes, which owns the names, never each series. A series that stands across the named axis (the dots of a
/// dot plot) is keyed by its names too, and a ridge that stands on a named baseline is named by it.</summary>
public class ComposedChartDataTablesTests
{
    private static readonly string[] Categories = ["Alpha", "Beta", "Gamma"];

    private static IReadOnlyList<ChartDataTable> TablesOf(Action<AxesBuilder> chart) =>
        Plt.Create().AddSubPlot(1, 1, 1, chart).Build().ToDataTables();

    private static string?[] FirstColumn(ChartDataTable table) => [.. table.Rows.Select(row => row[0].Text)];

    private static string[] Headers(ChartDataTable table) => [.. table.Columns.Select(c => c.Header)];

    [Fact]
    public void ALollipop_NamesItsRows_ByItsCategories()
    {
        var table = Assert.Single(TablesOf(ax => ax.Lollipop(Categories, [3, 1, 2])));

        Assert.Equal(["category", "y"], Headers(table));
        Assert.Equal(DataColumnKind.Text, table.Columns[0].Kind);
        Assert.Equal(Categories, FirstColumn(table));
        Assert.Equal([3.0, 1.0, 2.0], table.Rows.Select(row => row[1].Number));
    }

    [Fact]
    public void ANamedColumn_TakesTheAxisTitle_WhenTheAxisHasOne()
    {
        var table = Assert.Single(TablesOf(ax => ax.Lollipop(Categories, [3, 1, 2]).SetXLabel("Product")));

        Assert.Equal(["Product", "y"], Headers(table));
        Assert.Equal(Categories, FirstColumn(table));
    }

    [Fact]
    public void ADumbbell_IsOneTable_NamedByItsCategories()
    {
        var table = Assert.Single(TablesOf(ax => ax.Dumbbell(Categories,
            new BarGroup("Before", [1, 2, 3]), new BarGroup("After", [2, 3, 1]))));

        Assert.Equal(["category", "Before", "After", "y", "y error low", "y error high"], Headers(table));
        Assert.Equal(Categories, FirstColumn(table));
    }

    [Fact]
    public void ASlopeGraph_NamesItsTwoMoments_AndHasAColumnPerCategory()
    {
        var table = Assert.Single(TablesOf(ax => ax.SlopeGraph(Categories,
            new BarGroup("2015", [1, 2, 3]), new BarGroup("2025", [2, 3, 1]))));

        Assert.Equal(["category", "Alpha", "Beta", "Gamma"], Headers(table));
        Assert.Equal(["2015", "2025"], FirstColumn(table));
        Assert.Equal([2.0, 3.0, 1.0], table.Rows[1].Skip(1).Select(cell => cell.Number));
    }

    [Fact]
    public void ADotPlot_IsOneTable_KeyedByItsCategories_WithAColumnPerGroup()
    {
        // The categories run down the Y axis: the dots' y is the name, their x the value. Sorted by the first group,
        // largest first: Beta (3), Gamma (2), Alpha (1).
        var table = Assert.Single(TablesOf(ax => ax.DotPlot(Categories,
            [new BarGroup("2015", [1, 3, 2]), new BarGroup("2025", [2, 3, 1])])));

        Assert.Equal(["category", "2015", "2025"], Headers(table));
        Assert.Equal(DataAxis.Y, table.Columns[0].Axis);
        Assert.Equal(["Beta", "Gamma", "Alpha"], FirstColumn(table));
        Assert.Equal([3.0, 2.0, 1.0], table.Rows.Select(row => row[1].Number));
        Assert.Equal([3.0, 1.0, 2.0], table.Rows.Select(row => row[2].Number));
    }

    [Fact]
    public void ADotPlotGroupWithoutAName_KeepsItsValuesUnderTheirOwnHeader()
    {
        var table = Assert.Single(TablesOf(ax => ax.DotPlot(Categories, [new BarGroup("", [1, 3, 2])])));

        Assert.Equal(["category", "x"], Headers(table));
        Assert.Equal([3.0, 2.0, 1.0], table.Rows.Select(row => row[1].Number));
    }

    [Fact]
    public void AParetoChart_IsOneTable_ItsLineBesideItsBars()
    {
        var table = Assert.Single(TablesOf(ax => ax.Pareto(Categories, [3, 9, 5])));

        Assert.Equal(["category", "Count", "Cumulative %"], Headers(table));
        Assert.Equal(["Beta", "Gamma", "Alpha"], FirstColumn(table));
        Assert.Equal([9.0, 5.0, 3.0], table.Rows.Select(row => row[1].Number));
        Assert.Equal(100.0, table.Rows[2][2].Number);
    }

    [Fact]
    public void ABumpChart_KeepsTheXItWasGiven_AndNamesAColumnPerGroup()
    {
        var table = Assert.Single(TablesOf(ax => ax
            .BumpChart([2021, 2022, 2023], [new BarGroup("Hawks", [1, 2, 3]), new BarGroup("Owls", [2, 1, 1])])
            .SetXLabel("Season")));

        Assert.Equal(["Season", "Hawks", "Owls"], Headers(table));
        Assert.Equal([2021.0, 2022.0, 2023.0], table.Rows.Select(row => row[0].Number));
    }

    [Fact]
    public void ARidgeline_IsOneTable_WithAColumnPerGroup_OfTheRidgesHeights()
    {
        var table = Assert.Single(TablesOf(ax => ax
            .Ridgeline([new NamedSample("January", [1, 2, 3, 2.5]), new NamedSample("July", [5, 6, 7, 6.5])])
            .SetXLabel("Temperature")));

        Assert.Equal(["Temperature", "January", "July"], Headers(table));
        Assert.Equal(200, table.RowCount);

        // A height is measured from the ridge's own baseline: 0 or more, and the tallest ridge reaches exactly the next
        // baseline, one slot up.
        double[] heights = [.. table.Rows.SelectMany(row => new[] { row[1].Number, row[2].Number })];
        Assert.All(heights, h => Assert.True(h >= 0, $"a height of {h}"));
        Assert.Equal(1.0, heights.Max(), 12);
        Assert.All(table.Columns.Skip(1), column => Assert.Equal(DataAxis.None, column.Axis));
    }

    [Fact]
    public void ARidgeTheCallerNamed_IsTabledUnderThatName()
    {
        int ridges = 0;
        var table = Assert.Single(TablesOf(ax => ax.Ridgeline(
            [new NamedSample("January", [1, 2, 3, 2.5]), new NamedSample("July", [5, 6, 7, 6.5])],
            configure: ridge => ridge.Label = ridges++ == 0 ? "Jan 2025" : null)));

        Assert.Equal(["x", "Jan 2025", "July"], Headers(table));
    }

    [Fact]
    public void ACalendarHeatmap_NamesTheWeekdayOfEveryCell_AndNumbersItsWeek()
    {
        // 1 January 2025 is a Wednesday: row 2 of a week from Monday, week column 0.
        var first = new DateTime(2025, 1, 1);
        DateTime[] days = [.. Enumerable.Range(0, 10).Select(d => first.AddDays(d))];
        var table = Assert.Single(TablesOf(ax => ax.CalendarHeatmap(days, [.. days.Select(d => (double)d.Day)])));

        Assert.Equal(["row", "column", "value"], Headers(table));
        Assert.Equal([DataColumnKind.Text, DataColumnKind.Number, DataColumnKind.Number], table.Columns.Select(c => c.Kind));
        var wednesday = Assert.Single(table.Rows, row => row[2].Number == 1);
        Assert.Equal("Wed", wednesday[0].Text);
        Assert.Equal(0, wednesday[1].Number);
        Assert.Equal(["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"], table.Rows.Select(row => row[0].Text).Distinct());
    }

    [Fact]
    public void AParallelCoordinatesClass_NamesEveryPointByItsDimension()
    {
        var tables = TablesOf(ax => ax.ParallelCoordinates(["Sepal", "Petal"],
            [new BarGroup("setosa", [5.1, 1.4]), new BarGroup("setosa", [4.9, 1.3]), new BarGroup("virginica", [6.3, 6.0])]));

        Assert.Equal(2, tables.Count);
        Assert.Equal(["category", "setosa"], Headers(tables[0]));
        // The gap between two rows of a class is no position: its name cell is empty, its value missing.
        Assert.Equal(["Sepal", "Petal", null, "Sepal", "Petal"], FirstColumn(tables[0]));
        Assert.Equal(DataCellKind.Empty, tables[0].Rows[2][0].Kind);
        Assert.Equal(["Sepal", "Petal"], FirstColumn(tables[1]));
    }

    [Fact]
    public void AMarimekko_KeepsTheEdgesOfItsColumns_BecauseTheirWidthsAreItsData()
    {
        // The names stand under the centres of the columns, where no point of an area stands; the points stand on the
        // edges, which give each column's share of the total. The edges stay numbers, and the segments name the columns.
        var table = Assert.Single(TablesOf(ax => ax.Marimekko(["Retail", "Online"],
            [new BarGroup("Brand A", [30, 15]), new BarGroup("Brand B", [20, 25])])));

        Assert.Equal(["x", "Brand A", "y2", "Brand B", "y2"], Headers(table));
        Assert.Equal(DataColumnKind.Number, table.Columns[0].Kind);
        Assert.Equal(100.0, table.Rows[^1][0].Number);
    }

    [Fact]
    public void ALineBetweenTheNames_KeepsItsNumbers()
    {
        // A tick stands on each slot centre; a point at 0, 1 or 2 stands between the names, which name none of it.
        var tables = TablesOf(ax => ax.Bar(Categories, [3, 1, 2]).Plot([0.0, 1.0, 2.0], [1.0, 2.0, 3.0], s => s.Label = "trend"));

        Assert.Equal(2, tables.Count);
        Assert.Equal(["category", "value"], Headers(tables[0]));
        Assert.Equal(["x", "trend"], Headers(tables[1]));
        Assert.Equal([0.0, 1.0, 2.0], tables[1].Rows.Select(row => row[0].Number));
    }

    [Fact]
    public void ALineOnTheCentresOfBars_JoinsTheBarsTable()
    {
        var table = Assert.Single(TablesOf(ax => ax
            .Bar(Categories, [3, 1, 2])
            .Plot([0.5, 1.5, 2.5], [1.0, 2.0, 3.0], s => s.Label = "trend")));

        Assert.Equal(["category", "value", "trend"], Headers(table));
        Assert.Equal(Categories, FirstColumn(table));
    }

    [Fact]
    public void AnAxisWithoutNames_LeavesThePositionsAsTheyAre()
    {
        var table = Assert.Single(TablesOf(ax => ax.Plot([0.5, 1.5], [1.0, 2.0])));

        Assert.Equal(["x", "y"], Headers(table));
        Assert.Equal(DataColumnKind.Number, table.Columns[0].Kind);
        Assert.Equal(0.5, table.Rows[0][0].Number);
    }
}
