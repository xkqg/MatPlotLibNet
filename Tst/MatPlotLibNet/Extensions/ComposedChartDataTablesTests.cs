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

        Assert.Equal(["row", "column", "date", "value"], Headers(table));
        Assert.Equal([DataColumnKind.Text, DataColumnKind.Number, DataColumnKind.Date, DataColumnKind.Number], table.Columns.Select(c => c.Kind));
        var wednesday = Assert.Single(table.Rows, row => row[3].Number == 1);
        Assert.Equal("Wed", wednesday[0].Text);
        Assert.Equal(0, wednesday[1].Number);
        Assert.Equal(["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"], table.Rows.Select(row => row[0].Text).Distinct());
    }

    private static DateTime DateOf(DataCell cell) => DateTime.FromOADate(cell.Number);

    /// <summary>Measured before the change: the table of a calendar heatmap named each cell's weekday and numbered its week but not
    /// its date, because the chart travels as a heatmap of its cells. A reader had to work a date out from the week number, the
    /// weekday and the first day shown, which the table did not say. The heatmap now carries the date of its first cell, and the
    /// table gives every cell the date it stands for: the first cell plus 7 days for each column plus one for each row.</summary>
    [Fact]
    public void ACalendarHeatmap_NamesTheDateOfEveryCell()
    {
        var first = new DateTime(2025, 1, 1);          // a Wednesday: row 2 of week 0 from Monday
        DateTime[] days = [.. Enumerable.Range(0, 10).Select(d => first.AddDays(d))];
        var table = Assert.Single(TablesOf(ax => ax.CalendarHeatmap(days, [.. days.Select(d => (double)d.Day)])));

        var wednesday = Assert.Single(table.Rows, row => row[3].Number == 1);
        Assert.Equal(first, DateOf(wednesday[2]));
        foreach (var row in table.Rows)
        {
            // The date of a cell is where it stands: Monday 30 December 2024 is cell (0, 0), a day later one row down.
            var cell = new DateTime(2024, 12, 30).AddDays(7 * row[1].Number + Array.IndexOf(new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" }, row[0].Text));
            Assert.Equal(cell, DateOf(row[2]));
        }
    }

    /// <summary>The days of the first week before the earliest date, and of the last week after the latest, are cells of the grid
    /// with a value that is missing: they keep the date they stand for.</summary>
    [Fact]
    public void ThePaddingOfACalendar_KeepsItsDate_WithAMissingValue()
    {
        DateTime[] days = [new(2025, 1, 1), new(2025, 1, 2)];
        var table = Assert.Single(TablesOf(ax => ax.CalendarHeatmap(days, [5, 6])));

        // One week is one column, and the axis names it (the month that begins in it), so the column cell reads as that name.
        var monday = table.Rows.Single(row => row[0].Text == "Mon");
        Assert.Equal(new DateTime(2024, 12, 30), DateOf(monday[2]));
        Assert.True(double.IsNaN(monday[3].Number));
        Assert.Equal(new DateTime(2025, 1, 5), DateOf(table.Rows.Single(row => row[0].Text == "Sun")[2]));
        Assert.Equal(7, table.RowCount);
    }

    [Fact]
    public void ADayInTheMiddleWithNoValue_KeepsItsDate()
    {
        DateTime[] days = [new(2025, 3, 3), new(2025, 3, 5)];
        var table = Assert.Single(TablesOf(ax => ax.CalendarHeatmap(days, [1, 3])));

        var tuesday = table.Rows.Single(row => row[0].Text == "Tue");
        Assert.Equal(new DateTime(2025, 3, 4), DateOf(tuesday[2]));
        Assert.True(double.IsNaN(tuesday[3].Number));
    }

    [Theory]
    [InlineData(DayOfWeek.Monday)]
    [InlineData(DayOfWeek.Sunday)]
    [InlineData(DayOfWeek.Saturday)]
    public void TheDateOfACell_FollowsTheDayTheWeekStartsOn(DayOfWeek firstDay)
    {
        DateTime[] days = [.. Enumerable.Range(0, 20).Select(d => new DateTime(2025, 1, 1).AddDays(d))];
        var table = Assert.Single(TablesOf(ax => ax.CalendarHeatmap(days, [.. days.Select(d => (double)d.Day)], firstDay)));

        // The cell that holds the 1st of January holds the value 1, whatever day the week starts on.
        var cell = table.Rows.Single(row => row[3].Number == 1);
        Assert.Equal(new DateTime(2025, 1, 1), DateOf(cell[2]));
        // Every cell's weekday name is the weekday of its date.
        foreach (var row in table.Rows)
        {
            Assert.StartsWith(row[0].Text!, DateOf(row[2]).DayOfWeek.ToString());
        }
    }

    /// <summary>A whole year is 53 week columns: the last cell is the 31st of December, in its week, and the date of every cell
    /// is one more than the cell before it, down a column and on to the next.</summary>
    [Fact]
    public void AYearOfDays_HasEveryDateOnce_InOrder()
    {
        DateTime[] days = [.. Enumerable.Range(0, 365).Select(d => new DateTime(2025, 1, 1).AddDays(d))];
        var table = Assert.Single(TablesOf(ax => ax.CalendarHeatmap(days, [.. days.Select(d => 1.0)])));

        var dates = table.Rows.OrderBy(row => row[1].Number).ThenBy(row => Array.IndexOf(new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" }, row[0].Text))
            .Select(row => DateOf(row[2])).ToList();
        Assert.Equal(53 * 7, dates.Count);
        for (int i = 1; i < dates.Count; i++) Assert.Equal(dates[i - 1].AddDays(1), dates[i]);
        Assert.Equal(new DateTime(2025, 12, 31), table.Rows.Where(row => !double.IsNaN(row[3].Number)).Max(row => DateOf(row[2])));
    }

    /// <summary>A heatmap that is no calendar has no first cell date and keeps its three columns.</summary>
    [Fact]
    public void APlainHeatmap_HasNoDateColumn()
    {
        var table = Assert.Single(TablesOf(ax => ax.Heatmap(new double[,] { { 1, 2 }, { 3, 4 } })));

        Assert.Equal(["row", "column", "value"], Headers(table));
    }

    /// <summary>The first cell date travels with the heatmap: read back from JSON, the table gives the same dates.</summary>
    [Fact]
    public void ACalendarHeatmapReadBackFromJson_StillNamesTheDates()
    {
        DateTime[] days = [.. Enumerable.Range(0, 30).Select(d => new DateTime(2025, 2, 10).AddDays(d))];
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.CalendarHeatmap(days, [.. days.Select(d => (double)d.Day)])).Build();

        var written = Assert.Single(figure.ToDataTables());
        var read = Assert.Single(new MatPlotLibNet.Serialization.ChartSerializer().FromJson(figure.ToJson()).ToDataTables());

        Assert.Equal(written.Rows.Select(row => row[2].Number), read.Rows.Select(row => row[2].Number));
        Assert.Contains("\"firstCellDate\"", figure.ToJson());
    }

    [Fact]
    public void APlainHeatmap_WritesNoFirstCellDate()
    {
        var json = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Heatmap(new double[,] { { 1, 2 }, { 3, 4 } })).Build().ToJson();

        Assert.DoesNotContain("firstCellDate", json);
    }

    /// <summary>The ends of the calendar a <see cref="DateTime"/> can hold: a cell past the last day it holds, or a first cell
    /// before the first, has no date to name, and the table still reads.</summary>
    [Fact]
    public void TheEndsOfTheCalendar_NameTheDatesTheyHaveAndLeaveTheRestEmpty()
    {
        var table = Assert.Single(TablesOf(ax => ax.CalendarHeatmap([DateTime.MaxValue.Date], [1])));

        Assert.Equal(7, table.RowCount);
        Assert.Contains(table.Rows, row => row[2].Kind == DataCellKind.Empty);
        Assert.Contains(table.Rows, row => row[2].Kind == DataCellKind.Number);
    }

    [Fact]
    public void TheFirstDayOfTheCalendar_StillTables()
    {
        var table = Assert.Single(TablesOf(ax => ax.CalendarHeatmap([DateTime.MinValue], [1])));

        Assert.Equal(7, table.RowCount);
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

    private static readonly string[] Channels = ["Retail", "Online"];

    private static BarGroup[] Brands => [new("Brand A", [30, 15]), new("Brand B", [20, 25])];

    /// <summary>Measured before the change: the table of a Marimekko chart was the points the chart draws, eight rows of x, Brand A,
    /// y2, Brand B, y2 for three channels, with the edges of the columns as its x. The names of the columns stand under their
    /// centres, where no point stands, so no row said which edges bound which channel. A Marimekko chart now tables as it is
    /// read: a row for each column, named, with the edges it stands between and the height of each segment's cell in it.</summary>
    [Fact]
    public void AMarimekko_NamesItsColumns_OneRowEach()
    {
        var table = Assert.Single(TablesOf(ax => ax.Marimekko(Channels, Brands)));

        Assert.Equal(["category", "left", "right", "Brand A", "Brand B"], Headers(table));
        Assert.Equal(DataColumnKind.Text, table.Columns[0].Kind);
        Assert.Equal(Channels, FirstColumn(table));
        // Retail is 50 of 90 and Online 40 of 90: the edges are 0, 55.6 and 100, in percent of the whole.
        Assert.Equal(50.0 / 90 * 100, table.Rows[0][2].Number, 9);
        Assert.Equal(0.0, table.Rows[0][1].Number);
        Assert.Equal(table.Rows[0][2].Number, table.Rows[1][1].Number);
        Assert.Equal(100.0, table.Rows[1][2].Number);
        // A cell's height is its share of its column, in percent: Brand A is 30 of Retail's 50 and 15 of Online's 40.
        Assert.Equal([60.0, 37.5], table.Rows.Select(row => row[3].Number));
        Assert.Equal([40.0, 62.5], table.Rows.Select(row => row[4].Number));
    }

    /// <summary>A column with no share has no width and no name; it is no row, and the columns after it are not named by their
    /// position in the list.</summary>
    [Fact]
    public void AMarimekkoColumnWithNoShare_IsNoRow_AndTheOthersKeepTheirNames()
    {
        var table = Assert.Single(TablesOf(ax => ax.Marimekko(["Retail", "Dead", "Online"],
            [new BarGroup("Brand A", [30, 0, 15]), new BarGroup("Brand B", [20, 0, 25])])));

        Assert.Equal(["Retail", "Online"], FirstColumn(table));
        Assert.Equal(2, table.RowCount);
    }

    [Fact]
    public void AMarimekkoOfOneColumn_IsOneRow()
    {
        var table = Assert.Single(TablesOf(ax => ax.Marimekko(["Only"], [new BarGroup("Brand A", [5]), new BarGroup("Brand B", [15])])));

        Assert.Equal(["Only"], FirstColumn(table));
        Assert.Equal([0.0], table.Rows.Select(row => row[1].Number));
        Assert.Equal([100.0], table.Rows.Select(row => row[2].Number));
        Assert.Equal([25.0], table.Rows.Select(row => row[3].Number));
        Assert.Equal([75.0], table.Rows.Select(row => row[4].Number));
    }

    /// <summary>An area with a gap on an axis that names nothing keeps the table of its points: only a run the axis names is a
    /// column.</summary>
    [Fact]
    public void AGappedAreaOnANumericAxis_KeepsTheTableOfItsPoints()
    {
        double[] x = [0, 1, double.NaN, 2, 3];
        double[] y = [1, 2, double.NaN, 3, 4];

        var table = Assert.Single(TablesOf(ax => ax.AddSeries(new AreaSeries(x, y) { Label = "gappy" })));

        Assert.Equal(["x", "gappy"], Headers(table));
        Assert.Equal(5, table.RowCount);
    }

    /// <summary>A column that is the same in two fragments, header and every cell, is one column of the merged table.</summary>
    [Fact]
    public void TwoSeriesThatShareAColumn_ShareItInTheMergedTable()
    {
        double[] x = [1, 2, 3];
        var table = Assert.Single(TablesOf(ax => ax
            .AddSeries(new LineSeries(x, [1, 2, 3]) { Label = "a" })
            .AddSeries(new LineSeries(x, [1, 2, 3]) { Label = "a" })));

        Assert.Equal(["x", "a"], Headers(table));
    }

    [Fact]
    public void TwoSeriesWithDifferentColumns_KeepBoth_EvenUnderTheSameHeader()
    {
        double[] x = [1, 2, 3];
        var table = Assert.Single(TablesOf(ax => ax
            .AddSeries(new LineSeries(x, [1, 2, 3]) { Label = "a" })
            .AddSeries(new LineSeries(x, [3, 2, 1]) { Label = "a" })));

        Assert.Equal(["x", "a", "a"], Headers(table));
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
