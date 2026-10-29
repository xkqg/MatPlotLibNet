// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace MatPlotLibNet.Models.Series;

/// <summary>A span of days laid out as a calendar, and the one owner of where a day stands in it: seven rows, one for each
/// day of the week, the first row the day the caller's week starts on (<see cref="FirstDayOfWeek"/>), by one column for
/// each week, from the week that holds the first date to the week that holds the last.
/// <para>A day's row is its place in the week counted from the week start, <c>((int)day.DayOfWeek - (int)FirstDayOfWeek +
/// 7) % 7</c>, and its column is the number of weeks since the week that holds the first date. The arithmetic is exact
/// integer math on the week start the caller names, never on the culture's, which differs between machines (Sunday under
/// en-US, Monday under most of Europe) and between Windows and Linux; the names of the days and months are the invariant
/// culture's for the same reason.</para>
/// <para>A day's cell holds the sum of the values given for it, whatever their time of day. A missing value (NaN) adds
/// nothing, and a day without a value, or with only missing values, is NaN: blank, never zero. A year is 53 columns wide,
/// never 52 (365 days are one more than 52 weeks), and 54 when it has 366 days and starts on the last day of a week.</para>
/// <para>Read by <see cref="GridAxesExtensions.CalendarHeatmap"/>, which draws <see cref="Cells"/> as one heatmap, row 0 at
/// the top.</para></summary>
internal sealed class CalendarGrid
{
    /// <summary>The number of rows: the days of a week.</summary>
    internal const int DaysPerWeek = 7;

    /// <summary>The names of the days and months, the same on every machine.</summary>
    private static readonly DateTimeFormatInfo Invariant = CultureInfo.InvariantCulture.DateTimeFormat;

    private CalendarGrid(DateTime first, DateTime last, DayOfWeek firstDayOfWeek)
    {
        First = first;
        Last = last;
        FirstDayOfWeek = firstDayOfWeek;
        Cells = new double[DaysPerWeek, ColumnOf(last) + 1];
        MonthColumns = MonthStarts();
    }

    /// <summary>The first day of the span: the earliest date, without its time of day.</summary>
    internal DateTime First { get; }

    /// <summary>The last day of the span: the latest date, without its time of day.</summary>
    internal DateTime Last { get; }

    /// <summary>The day a week starts on: the day of the top row.</summary>
    internal DayOfWeek FirstDayOfWeek { get; }

    /// <summary>The date of the cell in row 0, column 0: the first day of the week that holds <see cref="First"/>. It lies up to
    /// six days before the first date, and is <see langword="null"/> only when that is before the first day a
    /// <see cref="DateTime"/> holds. The table of the heatmap gives every cell its date from this one.</summary>
    internal DateTime? FirstCellDate =>
        RowOf(First) is var back && First.Ticks >= back * TimeSpan.TicksPerDay ? First.AddDays(-back) : null;

    /// <summary>The cells, <c>[row, column]</c>: the sum of the values of the day that stands there, NaN where no value
    /// was given (the days before the first date and after the last included).</summary>
    internal double[,] Cells { get; }

    /// <summary>The number of week columns.</summary>
    internal int Columns => Cells.GetLength(1);

    /// <summary>The name of each row, from the top: the invariant culture's abbreviation of the day that stands there
    /// (<c>Mon</c>, <c>Tue</c>, ...).</summary>
    internal string[] WeekdayNames =>
        [.. Enumerable.Range(0, DaysPerWeek).Select(row => Invariant.AbbreviatedDayNames[((int)FirstDayOfWeek + row) % DaysPerWeek])];

    /// <summary>The column that holds the 1st of each month of the span, with the month's name (the invariant culture's
    /// abbreviation, <c>Jan</c>, <c>Feb</c>, ...), from left to right. A month whose 1st lies before the first date is not
    /// on the calendar and has no entry.</summary>
    internal IReadOnlyDictionary<int, string> MonthColumns { get; }

    /// <summary>Lays out <paramref name="values"/> on the days of <paramref name="dates"/>: one grid over the span from the
    /// earliest date to the latest, each value added to its day's cell.</summary>
    /// <param name="dates">The day of each value; in any order, a day may come more than once, and the time of day is
    /// ignored.</param>
    /// <param name="values">The value of each date, one per date; NaN marks a missing value.</param>
    /// <param name="firstDayOfWeek">The day a week starts on: the day of the top row.</param>
    /// <returns>The grid.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="dates"/> or <paramref name="values"/> is
    /// <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="dates"/> is empty, or <paramref name="values"/> does not carry
    /// one value per date.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="firstDayOfWeek"/> is not a day of the week.</exception>
    internal static CalendarGrid Of(DateTime[] dates, double[] values, DayOfWeek firstDayOfWeek)
    {
        ArgumentNullException.ThrowIfNull(dates);
        if (dates.Length == 0)
        {
            throw new ArgumentException("No dates were given; a calendar draws one cell per day.", nameof(dates));
        }

        values.ThrowIfMisaligned(dates.Length, nameof(values), "dates", "date");
        if (firstDayOfWeek is < DayOfWeek.Sunday or > DayOfWeek.Saturday)
        {
            throw new ArgumentOutOfRangeException(nameof(firstDayOfWeek), firstDayOfWeek, "A week starts on one of the seven days of the week.");
        }

        var grid = new CalendarGrid(dates.Min().Date, dates.Max().Date, firstDayOfWeek);
        var given = new bool[DaysPerWeek, grid.Columns];
        for (int i = 0; i < dates.Length; i++)
        {
            // A missing value adds nothing; a day that only has missing values stays blank.
            if (values[i].IsMissing()) continue;

            int row = grid.RowOf(dates[i]), column = grid.ColumnOf(dates[i]);
            grid.Cells[row, column] += values[i];
            given[row, column] = true;
        }

        for (int row = 0; row < DaysPerWeek; row++)
        {
            for (int column = 0; column < grid.Columns; column++)
            {
                if (!given[row, column]) grid.Cells[row, column] = double.NaN;
            }
        }

        return grid;
    }

    /// <summary>The row <paramref name="date"/> stands in: its place in the week, counted from
    /// <see cref="FirstDayOfWeek"/>.</summary>
    /// <param name="date">Any date; only its day of the week counts.</param>
    /// <returns>The row, from 0 (the top) to 6.</returns>
    internal int RowOf(DateTime date) => ((int)date.DayOfWeek - (int)FirstDayOfWeek + DaysPerWeek) % DaysPerWeek;

    /// <summary>The column <paramref name="date"/> stands in: the number of weeks since the week that holds
    /// <see cref="First"/>. The days of the first column before <see cref="First"/> are counted in, so a week start is
    /// always the top of a column.</summary>
    /// <param name="date">A date of the span; its time of day is ignored.</param>
    /// <returns>The column, from 0.</returns>
    internal int ColumnOf(DateTime date) => (RowOf(First) + (date.Date - First).Days) / DaysPerWeek;

    /// <summary>The column of the 1st of every month from the first date to the last, counted in months so the end of the
    /// calendar a <see cref="DateTime"/> can hold is never stepped past.</summary>
    private Dictionary<int, string> MonthStarts()
    {
        var columns = new Dictionary<int, string>();
        int firstMonth = First.Year * 12 + First.Month - 1 + (First.Day == 1 ? 0 : 1);
        int lastMonth = Last.Year * 12 + Last.Month - 1;
        for (int month = firstMonth; month <= lastMonth; month++)
        {
            columns.Add(ColumnOf(new DateTime(month / 12, month % 12 + 1, 1)), Invariant.AbbreviatedMonthNames[month % 12]);
        }

        return columns;
    }
}
