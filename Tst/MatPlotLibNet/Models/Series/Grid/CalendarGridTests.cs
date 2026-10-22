// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>The days of a span laid out as a calendar: seven rows, one per day of the week, the first row the day the
/// caller's week starts on, by one column per week. The arithmetic is exact integer math on the explicit week start (never
/// the culture's, which differs between Windows and Linux), so every assert here is exact. The cells a day lands in are
/// checked against a walk over the days, an oracle that shares no arithmetic with the grid.</summary>
public class CalendarGridTests
{
    /// <summary>Where a day lands: its row (its place in the week) and its column (its week).</summary>
    private readonly record struct Cell(int Row, int Column);

    /// <summary>The first and the last day of a span.</summary>
    private readonly record struct Span(DateTime First, DateTime Last);

    private static readonly Span[] Spans =
    [
        new(new DateTime(2025, 1, 1), new DateTime(2025, 12, 31)),    // a common year, starting on a Wednesday
        new(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31)),    // a leap year, starting on a Monday
        new(new DateTime(2024, 11, 17), new DateTime(2025, 2, 11)),   // across the turn of a year
        new(new DateTime(2025, 6, 15), new DateTime(2025, 6, 15)),    // one day
        new(new DateTime(2023, 2, 26), new DateTime(2023, 3, 12)),    // two weeks and a day across the end of a February
        new(new DateTime(2000, 1, 1), new DateTime(2000, 12, 31)),    // a leap year starting on a Saturday
    ];

    /// <summary>Every week start.</summary>
    public static TheoryData<DayOfWeek> WeekStarts { get; } = new(Enum.GetValues<DayOfWeek>());

    /// <summary>Every week start against every span, the span by its index into <see cref="Spans"/>.</summary>
    public static TheoryData<DayOfWeek, int> WeekStartsBySpan { get; } = Build();

    private static TheoryData<DayOfWeek, int> Build()
    {
        var data = new TheoryData<DayOfWeek, int>();
        foreach (var weekStart in Enum.GetValues<DayOfWeek>())
        {
            for (int s = 0; s < Spans.Length; s++) data.Add(weekStart, s);
        }

        return data;
    }

    private static List<DateTime> Days(DateTime first, DateTime last)
    {
        var days = new List<DateTime>();
        for (var day = first; day <= last; day = day.AddDays(1)) days.Add(day);
        return days;
    }

    /// <summary>The oracle: walk back from the first day to the most recent week start (that many rows lie above it in its
    /// column), then walk forward one day at a time, one row down per day and a new column whenever a week starts.</summary>
    private static Dictionary<DateTime, Cell> Walk(DateTime first, DateTime last, DayOfWeek weekStart)
    {
        int row = 0;
        for (var day = first; day.DayOfWeek != weekStart; day = day.AddDays(-1)) row++;

        int column = 0;
        var cells = new Dictionary<DateTime, Cell>();
        foreach (var day in Days(first, last))
        {
            if (day != first && day.DayOfWeek == weekStart)
            {
                row = 0;
                column++;
            }

            cells[day] = new Cell(row, column);
            row++;
        }

        return cells;
    }

    /// <summary>Every day of <paramref name="year"/>, each holding its day of the year.</summary>
    private static CalendarGrid Year(int year, DayOfWeek weekStart)
    {
        var days = Days(new DateTime(year, 1, 1), new DateTime(year, 12, 31));
        return CalendarGrid.Of([.. days], [.. days.Select(d => (double)d.DayOfYear)], weekStart);
    }

    private static int Filled(CalendarGrid grid) => grid.Cells.Cast<double>().Count(v => !double.IsNaN(v));

    // ---- where a day lands ------------------------------------------------------------------------------------------

    /// <summary>Every day lands in the row and column the walk puts it in, holding its own value, for every week start and
    /// every span; the grid is exactly as wide as the walk and nothing else is filled.</summary>
    [Theory]
    [MemberData(nameof(WeekStartsBySpan))]
    public void EveryDay_LandsInTheCellTheWalkPutsItIn(DayOfWeek weekStart, int spanIndex)
    {
        var span = Spans[spanIndex];
        var days = Days(span.First, span.Last);
        var expected = Walk(span.First, span.Last, weekStart);

        var grid = CalendarGrid.Of([.. days], [.. days.Select((_, i) => i + 1.0)], weekStart);

        Assert.Equal(7, grid.Cells.GetLength(0));
        Assert.Equal(expected.Values.Max(c => c.Column) + 1, grid.Columns);
        Assert.Equal(grid.Columns, grid.Cells.GetLength(1));
        for (int i = 0; i < days.Count; i++)
        {
            var cell = expected[days[i]];
            Assert.Equal(cell.Row, grid.RowOf(days[i]));
            Assert.Equal(cell.Column, grid.ColumnOf(days[i]));
            Assert.Equal(i + 1.0, grid.Cells[cell.Row, cell.Column]);
        }

        Assert.Equal(days.Count, Filled(grid));
    }

    /// <summary>The row is the place in the week counted from the week start the caller names: a Monday is the top row of
    /// a week that starts on Monday, the second of one that starts on Sunday, and the last of one that starts on
    /// Tuesday.</summary>
    [Theory]
    [InlineData(DayOfWeek.Monday, 0)]
    [InlineData(DayOfWeek.Sunday, 1)]
    [InlineData(DayOfWeek.Saturday, 2)]
    [InlineData(DayOfWeek.Tuesday, 6)]
    public void AMonday_StandsInTheRowOfItsPlaceInTheWeek(DayOfWeek weekStart, int row)
    {
        var monday = new DateTime(2025, 3, 3);

        var grid = CalendarGrid.Of([monday], [1], weekStart);

        Assert.Equal(DayOfWeek.Monday, monday.DayOfWeek);
        Assert.Equal(row, grid.RowOf(monday));
        Assert.Equal(1, grid.Cells[row, 0]);
        Assert.Equal(weekStart, grid.FirstDayOfWeek);
    }

    // ---- how wide a year is -----------------------------------------------------------------------------------------

    /// <summary>A year is never 52 week columns: 365 days are one more than 52 weeks. It is 53, and 54 when the year has
    /// 366 days and its first day is the last day of a week (it then starts a column alone and ends one alone). Between
    /// 1901 and 2100 that is seven years for every week start: the 49 leap years there start on each day of the week
    /// seven times.</summary>
    [Theory]
    [MemberData(nameof(WeekStarts))]
    public void AWholeYear_Is53Columns_And54InExactlySevenYears(DayOfWeek weekStart)
    {
        var wide = new List<int>();
        for (int year = 1901; year <= 2100; year++)
        {
            int columns = Year(year, weekStart).Columns;
            Assert.InRange(columns, 53, 54);
            if (columns == 54) wide.Add(year);
        }

        var lastDayOfAWeek = (DayOfWeek)(((int)weekStart + 6) % 7);
        var expected = Enumerable.Range(1901, 200)
            .Where(y => DateTime.IsLeapYear(y) && new DateTime(y, 1, 1).DayOfWeek == lastDayOfAWeek)
            .ToList();
        Assert.Equal(expected, wide);
        Assert.Equal(7, wide.Count);
    }

    /// <summary>Weeks that start on Monday need a 54th column in the leap years that start on a Sunday.</summary>
    [Fact]
    public void WeeksStartingOnMonday_Need54ColumnsInTheLeapYearsThatStartOnASunday()
    {
        int[] wide = [.. Enumerable.Range(1901, 200).Where(y => Year(y, DayOfWeek.Monday).Columns == 54)];

        Assert.Equal([1928, 1956, 1984, 2012, 2040, 2068, 2096], wide);
    }

    /// <summary>Weeks that start on Sunday need a 54th column in the leap years that start on a Saturday, 2000 and 2028
    /// among them; under weeks that start on Monday the same 2000 is 53 columns wide.</summary>
    [Fact]
    public void WeeksStartingOnSunday_Need54ColumnsIn2000And2028()
    {
        int[] wide = [.. Enumerable.Range(1901, 200).Where(y => Year(y, DayOfWeek.Sunday).Columns == 54)];

        Assert.Equal([1916, 1944, 1972, 2000, 2028, 2056, 2084], wide);
        Assert.Equal(53, Year(2000, DayOfWeek.Monday).Columns);
    }

    /// <summary>February 29 has a cell of its own, the day after the 28th and the day before March 1, and a leap year
    /// fills 366 cells.</summary>
    [Fact]
    public void TheLeapDay_HasACellOfItsOwn_BetweenThe28thAndMarch1()
    {
        var grid = Year(2024, DayOfWeek.Monday);
        var leapDay = new DateTime(2024, 2, 29);

        Assert.Equal(DayOfWeek.Thursday, leapDay.DayOfWeek);
        Assert.Equal(3, grid.RowOf(leapDay));
        Assert.Equal(60, grid.Cells[3, grid.ColumnOf(leapDay)]);
        Assert.Equal(59, grid.Cells[2, grid.ColumnOf(leapDay)]);
        Assert.Equal(61, grid.Cells[4, grid.ColumnOf(leapDay)]);
        Assert.Equal(366, Filled(grid));
    }

    /// <summary>The first and the last week are partial: the days of the first column before the first date and the days
    /// of the last column after the last date are blank (NaN), not zero. 2025 starts and ends on a Wednesday.</summary>
    [Fact]
    public void TheDaysBeforeTheFirstDate_AndAfterTheLast_AreBlank()
    {
        var grid = Year(2025, DayOfWeek.Monday);

        Assert.Equal(53, grid.Columns);
        double[] firstColumn = [.. Enumerable.Range(0, 7).Select(r => grid.Cells[r, 0])];
        double[] lastColumn = [.. Enumerable.Range(0, 7).Select(r => grid.Cells[r, 52])];
        Assert.Equal([double.NaN, double.NaN, 1, 2, 3, 4, 5], firstColumn);
        Assert.Equal([363, 364, 365, double.NaN, double.NaN, double.NaN, double.NaN], lastColumn);
    }

    // ---- what a cell holds ------------------------------------------------------------------------------------------

    /// <summary>Two values on one day are summed (a day is a total of what happened on it), and the time of day plays no
    /// part: one minute past midnight and one minute to midnight are the same day.</summary>
    [Fact]
    public void TwoValuesOnOneDay_AreSummed_WhateverTheTimeOfDay()
    {
        DateTime[] dates = [new(2025, 3, 4, 0, 1, 0), new(2025, 3, 5, 12, 0, 0), new(2025, 3, 4, 23, 59, 0)];

        var grid = CalendarGrid.Of(dates, [2, 7, 5], DayOfWeek.Monday);

        Assert.Equal(new DateTime(2025, 3, 4), grid.First);
        Assert.Equal(new DateTime(2025, 3, 5), grid.Last);
        Assert.Equal(1, grid.Columns);
        Assert.Equal(7, grid.Cells[1, 0]);
        Assert.Equal(7, grid.Cells[2, 0]);
        Assert.Equal(2, Filled(grid));
    }

    /// <summary>A missing value (NaN) adds nothing: a day with a value and a missing one holds the value, a day with only
    /// missing values is blank, and a day of 0 holds 0 (a zero is a value, not a blank).</summary>
    [Fact]
    public void AMissingValue_AddsNothing_AndADayOfOnlyMissingValuesIsBlank()
    {
        DateTime[] dates = [new(2025, 3, 3), new(2025, 3, 4), new(2025, 3, 4), new(2025, 3, 5)];

        var grid = CalendarGrid.Of(dates, [double.NaN, 3, double.NaN, 0], DayOfWeek.Monday);

        Assert.True(double.IsNaN(grid.Cells[0, 0]));
        Assert.Equal(3, grid.Cells[1, 0]);
        Assert.Equal(0, grid.Cells[2, 0]);
    }

    /// <summary>An infinity is a value beyond every scale, not a missing one: the day holds it (the heatmap draws it at the
    /// end of its colour scale).</summary>
    [Fact]
    public void AnInfiniteValue_IsHeld_NotLeftOut()
    {
        var grid = CalendarGrid.Of([new DateTime(2025, 3, 3)], [double.PositiveInfinity], DayOfWeek.Monday);

        Assert.Equal(double.PositiveInfinity, grid.Cells[0, 0]);
    }

    /// <summary>The dates may come in any order: the grid spans the earliest to the latest, and each value lands on its
    /// own day.</summary>
    [Fact]
    public void TheDates_MayComeInAnyOrder()
    {
        var days = Days(new DateTime(2025, 5, 1), new DateTime(2025, 7, 31));
        double[] values = [.. days.Select((_, i) => i * 3.0)];
        var inOrder = CalendarGrid.Of([.. days], values, DayOfWeek.Monday);

        var shuffled = CalendarGrid.Of([.. Enumerable.Reverse(days)], [.. Enumerable.Reverse(values)], DayOfWeek.Monday);

        Assert.Equal(inOrder.Cells, shuffled.Cells);
        Assert.Equal(new DateTime(2025, 5, 1), shuffled.First);
        Assert.Equal(new DateTime(2025, 7, 31), shuffled.Last);
    }

    // ---- the names --------------------------------------------------------------------------------------------------

    /// <summary>The rows are named from the week start down, in the invariant culture's abbreviations.</summary>
    [Fact]
    public void TheRows_AreNamedFromTheWeekStartDown()
    {
        var day = new DateTime(2025, 3, 3);

        Assert.Equal(["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"], CalendarGrid.Of([day], [1], DayOfWeek.Monday).WeekdayNames);
        Assert.Equal(["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"], CalendarGrid.Of([day], [1], DayOfWeek.Sunday).WeekdayNames);
    }

    /// <summary>For every week start, row r is named after the day r days past the week start.</summary>
    [Theory]
    [MemberData(nameof(WeekStarts))]
    public void RowR_IsNamedAfterTheDayRDaysPastTheWeekStart(DayOfWeek weekStart)
    {
        var start = new DateTime(2025, 3, 2);
        while (start.DayOfWeek != weekStart) start = start.AddDays(1);

        var names = CalendarGrid.Of([start], [1], weekStart).WeekdayNames;

        Assert.Equal([.. Enumerable.Range(0, 7).Select(r => start.AddDays(r).ToString("ddd", CultureInfo.InvariantCulture))], names);
    }

    /// <summary>Nothing reads the culture: under a culture whose week starts on Sunday and whose day names are not English,
    /// a week that starts on Monday still puts a Monday in the top row, and the names stay the invariant ones.</summary>
    [Theory]
    [InlineData("en-US")]
    [InlineData("nl-NL")]
    [InlineData("ar-SA")]
    public void TheGrid_ReadsNoCulture(string culture)
    {
        var previous = CultureInfo.CurrentCulture;
        var previousUi = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = new CultureInfo(culture);

            var grid = Year(2025, DayOfWeek.Monday);

            Assert.Equal(0, grid.RowOf(new DateTime(2025, 3, 3)));
            Assert.Equal(53, grid.Columns);
            Assert.Equal("Mon", grid.WeekdayNames[0]);
            Assert.Equal("Feb", grid.MonthColumns[4]);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
            CultureInfo.CurrentUICulture = previousUi;
        }
    }

    /// <summary>Every month is named on the column that holds its 1st, in the invariant culture's abbreviations: in 2025
    /// under weeks that start on Monday, February 1 (a Saturday) is in column 4 and December 1 in column 48.</summary>
    [Fact]
    public void EveryMonth_IsNamedOnTheColumnThatHoldsItsFirst()
    {
        var grid = Year(2025, DayOfWeek.Monday);
        var walk = Walk(new DateTime(2025, 1, 1), new DateTime(2025, 12, 31), DayOfWeek.Monday);

        var expected = Enumerable.Range(1, 12).ToDictionary(
            m => walk[new DateTime(2025, m, 1)].Column,
            m => CultureInfo.InvariantCulture.DateTimeFormat.AbbreviatedMonthNames[m - 1]);
        Assert.Equal(expected, grid.MonthColumns);
        Assert.Equal("Feb", grid.MonthColumns[4]);
        Assert.Equal("Dec", grid.MonthColumns[48]);
        Assert.Equal(grid.MonthColumns.Keys.Order(), grid.MonthColumns.Keys);
    }

    /// <summary>A month whose 1st lies before the first date is not named: its 1st is not on the calendar.</summary>
    [Fact]
    public void AMonthWhoseFirstIsBeforeTheFirstDate_IsNotNamed()
    {
        var grid = CalendarGrid.Of([new DateTime(2025, 1, 15), new DateTime(2025, 3, 10)], [1, 1], DayOfWeek.Monday);

        Assert.Equal(["Feb", "Mar"], grid.MonthColumns.Values);
        Assert.Equal(grid.ColumnOf(new DateTime(2025, 2, 1)), grid.MonthColumns.Keys.First());
    }

    /// <summary>A span across the turn of a year names the months of both years in order, the first on column 0 when the
    /// span starts on its 1st.</summary>
    [Fact]
    public void ASpanAcrossAYear_NamesTheMonthsOfBothYears()
    {
        var grid = CalendarGrid.Of([new DateTime(2024, 12, 1), new DateTime(2025, 2, 1)], [1, 1], DayOfWeek.Monday);

        Assert.Equal(["Dec", "Jan", "Feb"], grid.MonthColumns.Values);
        Assert.Equal(0, grid.MonthColumns.Keys.First());
    }

    /// <summary>The ends of the calendar a <see cref="DateTime"/> can hold are laid out without stepping past them: the
    /// first week of year 1 (which starts on a Monday) under weeks that start on Sunday, and December of year 9999.</summary>
    [Fact]
    public void TheEndsOfTheCalendar_AreLaidOutWithoutOverflow()
    {
        var start = CalendarGrid.Of([DateTime.MinValue, DateTime.MinValue.AddDays(6)], [1, 2], DayOfWeek.Sunday);
        var end = CalendarGrid.Of([new DateTime(9999, 12, 1), DateTime.MaxValue], [1, 2], DayOfWeek.Sunday);

        Assert.Equal(1, start.RowOf(DateTime.MinValue));
        Assert.Equal(2, start.Columns);
        Assert.Equal(["Jan"], start.MonthColumns.Values);
        Assert.Equal(["Dec"], end.MonthColumns.Values);
        Assert.Equal(new DateTime(9999, 12, 31), end.Last);
        Assert.Equal(2, end.Cells[(int)DayOfWeek.Friday, end.ColumnOf(DateTime.MaxValue)]);
    }

    // ---- what is refused --------------------------------------------------------------------------------------------

    /// <summary>No dates or no values at all are refused by the parameter's name.</summary>
    [Fact]
    public void NoArrays_AreRefusedByName()
    {
        var noDates = Assert.Throws<ArgumentNullException>(() => CalendarGrid.Of(null!, [1], DayOfWeek.Monday));
        var noValues = Assert.Throws<ArgumentNullException>(() => CalendarGrid.Of([DateTime.MinValue], null!, DayOfWeek.Monday));

        Assert.Equal("dates", noDates.ParamName);
        Assert.Equal("values", noValues.ParamName);
    }

    /// <summary>An empty span is refused: a calendar is a span of days, and no dates have none.</summary>
    [Fact]
    public void NoDates_AreRefused()
    {
        var error = Assert.Throws<ArgumentException>(() => CalendarGrid.Of([], [], DayOfWeek.Monday));

        Assert.Equal("dates", error.ParamName);
        Assert.Equal("No dates were given; a calendar draws one cell per day. (Parameter 'dates')", error.Message);
    }

    /// <summary>Values that do not pair up with the dates are refused in the words every verb uses, counted in dates.</summary>
    [Fact]
    public void ValuesThatDoNotPairUpWithTheDates_AreRefused()
    {
        DateTime[] dates = [new(2025, 1, 1), new(2025, 1, 2), new(2025, 1, 3), new(2025, 1, 4)];

        var error = Assert.Throws<ArgumentException>(() => CalendarGrid.Of(dates, [1, 2, 3], DayOfWeek.Monday));

        Assert.Equal("values", error.ParamName);
        Assert.Equal("3 values were given for 4 dates; there must be one value per date. (Parameter 'values')", error.Message);
    }

    /// <summary>A week start that is not a day of the week is refused by name.</summary>
    [Theory]
    [InlineData(7)]
    [InlineData(-1)]
    public void AWeekStartThatIsNotADay_IsRefused(int weekStart)
    {
        var error = Assert.Throws<ArgumentOutOfRangeException>(() => CalendarGrid.Of([new DateTime(2025, 1, 1)], [1], (DayOfWeek)weekStart));

        Assert.Equal("firstDayOfWeek", error.ParamName);
    }
}
