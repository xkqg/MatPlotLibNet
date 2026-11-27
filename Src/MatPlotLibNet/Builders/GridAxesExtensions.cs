// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Rendering;
using System.Globalization;
using MatPlotLibNet.Models;

namespace MatPlotLibNet;

/// <summary>Charts whose data is the cells of a grid: every verb here lays the caller's data out as cells and draws them
/// with a series the library already has, one series for the whole grid, so the chart travels through JSON as that
/// series. A missing value (NaN) is a blank cell, never a zero. The verbs set no axis limits unless their chart cannot be
/// read without them, because a limit clips every series on the axes and overwrites the caller's own: a heatmap's own
/// range already spans its cells, while the markers of <see cref="Spy"/> need a half cell past the grid, so it says where
/// it sets them.</summary>
public static class GridAxesExtensions
{
    extension(AxesBuilder axes)
    {
        /// <summary>Draws a calendar heatmap: one cell for each day, coloured by its value, in seven weekday rows by one column
        /// for each week, so a pattern over the days of the week and the seasons of the year shows at a glance (a GitHub-style
        /// contribution chart).
        /// <para>The grid spans the dates, from the week that holds the earliest to the week that holds the latest: a whole
        /// year is 53 week columns, or 54 when it has 366 days and starts on the last day of a week. The top row is
        /// <paramref name="firstDayOfWeek"/>, and a day's row is its place in the week counted from it; the week start is the
        /// one given, never the culture's, so the same call draws the same picture on every machine.</para>
        /// <para>A day's cell holds the sum of the values given for it, whatever their time of day. A missing value (NaN) adds
        /// nothing, and a day without a value (the days of the first week before the earliest date and of the last week after
        /// the latest included) is blank, not zero.</para>
        /// <para>The chart is ONE <see cref="HeatmapSeries"/>, with the weekdays named on the Y axis (all seven, the week start
        /// at the top, in the invariant culture's abbreviations), each month named under the column that holds its 1st (a month
        /// whose 1st lies before the earliest date is not named), and a colour bar to read the values against; a colour bar
        /// set up before the call is kept as it was set up. It has no legend entry. The verb sets no axis limits; the plot has an aspect of one
        /// (<see cref="Models.Axes.Aspect"/>), so the cells are square at any figure size and the plot is the largest rectangle of
        /// the calendar's own proportions that fits, centred.</para>
        /// <para>The chart is its parts: written to JSON it is the heatmap with its blank days, the named ticks and the colour
        /// bar, and read back it draws the same picture. It is not laid out again; change the data and call the verb
        /// again.</para></summary>
        /// <param name="dates">The day of each value, in any order; a day may come more than once, and the time of day is
        /// ignored.</param>
        /// <param name="values">The value of each date, one per date; NaN marks a missing value.</param>
        /// <param name="firstDayOfWeek">The day a week starts on, drawn in the top row; Monday unless given.</param>
        /// <param name="configure">Optional styling of the one heatmap, run once after the verb has built it, so what it sets
        /// wins: its colour map, its normaliser, cell labels.</param>
        /// <returns>The same builder, for chaining.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="dates"/> or
        /// <paramref name="values"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">No dates are given, or <paramref name="values"/> does not carry one value per
        /// date.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="firstDayOfWeek"/> is not a day of the week.</exception>
        /// <example>
        /// <code>
        /// using MatPlotLibNet.Styling.ColorMaps;
        ///
        /// var first = new DateTime(2025, 1, 1);
        /// DateTime[] days = [.. Enumerable.Range(0, 365).Select(d => first.AddDays(d))];
        /// double[] commits = [.. days.Select(d => (double)(d.DayOfYear * 37 % 11))];
        ///
        /// string svg = Plt.Create()
        ///     .WithTitle("Commits in 2025")
        ///     .WithSize(1100, 300)
        ///     .AddSubPlot(1, 1, 1, ax => ax
        ///         .CalendarHeatmap(days, commits, configure: s => s.ColorMap = SequentialColorMaps.Greens))
        ///     .ToSvg();
        /// </code>
        /// </example>
        public AxesBuilder CalendarHeatmap(DateTime[] dates, double[] values,
            DayOfWeek firstDayOfWeek = DayOfWeek.Monday, Action<HeatmapSeries>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(axes);

            // Every refusal comes from laying out the grid, before the axes are touched.
            var grid = CalendarGrid.Of(dates, values, firstDayOfWeek);

            // A heatmap draws its row 0 at the top while the Y axis counts upward: the weekdays are named from the top down.
            // The heatmap carries the date of its first cell, so its table can give every cell the date it stands for.
            return axes
                .Heatmap(grid.Cells, cells => { cells.FirstCellDate = grid.FirstCellDate; configure?.Invoke(cells); })
                .SetYCategoryTicks(grid.WeekdayNames, topDown: true)
                .SetXCategoryTicks(grid.MonthColumns)
                .SetAspect(1)
                .WithColorBar();
        }

        /// <summary>Draws a calendar of events: one neutral cell for each day of the span, and on the day an event happened a
        /// coloured mark, its kind named in the legend, so when things happened and how they cluster shows at a glance.
        /// <para>The grid is the calendar of <see cref="CalendarHeatmap"/> (seven weekday rows by week columns, the week start
        /// in the top row, the weekdays and the months named, square cells), spanning the earliest event to the latest. Every
        /// day of the span is a cell in the theme's neutral grey, a quarter opaque, so a day with nothing on it is still there;
        /// the days of the first and the last week outside the span are blank.</para>
        /// <para>An event is ONE bar in the series of its kind, standing inside its day's cell, a tenth of the cell clear of
        /// every edge. The events of one day (of any kind) share the cell's inner width in stripes side by side, in the order
        /// they were given: one stands in the middle, two a half of the width apart, three a third; every mark is as wide as the
        /// narrowest stripe, that of the day with the most events. The kinds take the theme's colours in the
        /// order they first appear and are the legend's entries; an event without a kind is drawn and has no entry. The table
        /// of a kind has a row for each of its events, under the day's ISO date.</para>
        /// <para>The chart is its parts: written to JSON it is the heatmap of days, the floating bars, the named ticks and the
        /// aspect, and read back it draws the same picture. It is not laid out again; change the events and call the verb
        /// again. The verb sets no axis limits.</para></summary>
        /// <param name="dates">The day of each event, in any order; a day may come more than once, and the time of day is
        /// ignored.</param>
        /// <param name="kinds">The kind of each event, one per date; <see langword="null"/> or empty for an event with no
        /// kind.</param>
        /// <param name="firstDayOfWeek">The day a week starts on, drawn in the top row; Monday unless given.</param>
        /// <param name="configure">Optional styling of the parts, run once after the verb has built them, so what it sets wins:
        /// the colour of a kind, the colour map or the cell labels of the days.</param>
        /// <returns>The same builder, for chaining.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="dates"/> or <paramref name="kinds"/> is
        /// <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">No dates are given, or <paramref name="kinds"/> does not carry one kind per
        /// date.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="firstDayOfWeek"/> is not a day of the week.</exception>
        /// <example>
        /// <code>
        /// DateTime[] days = [new(2025, 3, 3), new(2025, 3, 3), new(2025, 3, 12), new(2025, 4, 1)];
        /// string[] kinds = ["deploy", "incident", "deploy", "incident"];
        ///
        /// string svg = Plt.Create()
        ///     .WithTitle("Releases and incidents")
        ///     .WithSize(900, 300)
        ///     .AddSubPlot(1, 1, 1, ax => ax.CalendarEvents(days, kinds))
        ///     .ToSvg();
        /// </code>
        /// </example>
        public AxesBuilder CalendarEvents(DateTime[] dates, string[] kinds,
            DayOfWeek firstDayOfWeek = DayOfWeek.Monday, Action<CalendarEventsParts>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(axes);
            ArgumentNullException.ThrowIfNull(dates);
            ArgumentNullException.ThrowIfNull(kinds);
            kinds.Length.ThrowIfMisaligned(dates.Length, nameof(kinds));

            // Every refusal comes from laying out the grid, before the axes are touched.
            var grid = CalendarGrid.Of(dates, new double[dates.Length], firstDayOfWeek);

            // The days: a cell for every day of the span (0), blank outside it, in the theme's neutral grey, drawn behind the marks;
            // the events of a day share its cell, one bar series for each kind.
            var cells = grid.DayCells();
            var bars = grid.EventMarks(dates, kinds);

            var days = new HeatmapSeries(cells) { FirstCellDate = grid.FirstCellDate, WearsNeutral = true, ZOrder = -1 };
            foreach (var bar in bars) axes.AddSeries(bar);
            axes.AddSeries(days)
                .SetYCategoryTicks(grid.WeekdayNames, topDown: true)
                .SetXCategoryTicks(grid.MonthColumns)
                .SetAspect(1);
            configure?.Invoke(new CalendarEventsParts(days, bars));
            return axes;
        }

        /// <summary>Draws a matrix as a grid of cells, one square for each value, as matplotlib's <c>matshow</c> does: the first
        /// row on top as a matrix is written, the columns numbered along the top and the rows down the left, whole numbers only
        /// (every one of a small matrix, round numbers of a larger one: at most nine intervals across, the ticks of
        /// <see cref="Spy"/>), and a colour bar to read the values against.
        /// <para>The cells are square because the plot has an aspect of one (<see cref="Models.Axes.Aspect"/>): the plot is the
        /// largest rectangle of the matrix' own proportions that fits, centred, at any figure size. The column numbers stand
        /// on the top edge only, the bottom edge keeping its tick marks (<see cref="AxesBuilderTickVerbs.WithXTickLabelsOnTop"/>, matplotlib's
        /// <c>tick_top()</c> then <c>set_ticks_position('both')</c>). A missing value (NaN) is a blank cell, never a zero.</para>
        /// <para>The chart is ONE <see cref="HeatmapSeries"/>: written to JSON it is the heatmap, the named ticks, the aspect
        /// and the colour bar, and read back it draws the same picture. It is not laid out again; change the matrix and call
        /// the verb again.</para></summary>
        /// <param name="matrix">The values, rows by columns; at least one of each.</param>
        /// <param name="configure">Optional styling of the one heatmap, run once after the verb has built it, so what it sets
        /// wins: its colour map, its normaliser, cell labels.</param>
        /// <returns>The same builder, for chaining.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="matrix"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="matrix"/> has no row or no column.</exception>
        /// <example>
        /// <code>
        /// var correlation = new double[,] { { 1.0, 0.8, 0.1 }, { 0.8, 1.0, -0.3 }, { 0.1, -0.3, 1.0 } };
        ///
        /// string svg = Plt.Create()
        ///     .WithTitle("Correlation")
        ///     .WithSize(500, 500)
        ///     .AddSubPlot(1, 1, 1, ax => ax.MatShow(correlation))
        ///     .ToSvg();
        /// </code>
        /// </example>
        public AxesBuilder MatShow(double[,] matrix, Action<HeatmapSeries>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(axes);
            ArgumentNullException.ThrowIfNull(matrix);
            matrix.ThrowIfNoCell("a matrix shown");
            int rows = matrix.GetLength(0), columns = matrix.GetLength(1);

            // A heatmap draws row 0 at the top while its Y axis counts upward: the rows are numbered from the top down.
            var rowNames = NamesOf(rows);
            return axes
                .Heatmap(matrix, configure)
                .SetAspect(1)
                .SetXCategoryTicks(NamesOf(columns))
                .SetYCategoryTicks([.. Enumerable.Range(0, rows).Select(row => rowNames.GetValueOrDefault(row, string.Empty))], topDown: true)
                .WithXTickLabelsOnTop()
                .WithColorBar();
        }

        /// <summary>Draws a spy plot: a square marker on every cell of the matrix whose magnitude is above the threshold, so the
        /// pattern of the non-zero cells (a band, a block, a scatter of fill-in) shows at a glance.
        /// <para>The chart is ONE <see cref="ScatterSeries"/>, so it wears one colour, and its points are the cells row by row:
        /// x is the column and y the row. The axes run exactly over the matrix, half a cell past the first and the last column
        /// and row, so a marker on an edge cell is drawn whole, and the Y axis runs backwards so row 0 is on top, as a matrix
        /// is written. The ticks are whole numbers, the cells' own numbers, at every cell of a small matrix and at round numbers
        /// (every 2, 5, 10 ... cells) of a larger one: at most nine intervals across. A limit of your own set after the call
        /// replaces the limits.</para>
        /// <para>A cell is a marker when <c>Math.Abs(value) &gt; threshold</c>: a cell exactly at the threshold is not, a
        /// negative value counts by its size, and an infinity is a value like any other that is not zero. A missing value (NaN)
        /// is no marker, and no other marker moves for it. A matrix with no cell above the threshold draws no marker and still
        /// shows its extent.</para>
        /// <para>The markers are squares of the scatter's own size, a size in points and not in cells: a larger matrix has the
        /// same markers on a finer grid, so lower <see cref="ScatterSeries.MarkerSize"/> in <paramref name="configure"/> once the
        /// markers touch. The plot has an aspect of one (<see cref="Models.Axes.Aspect"/>), so the spacing of the grid is
        /// the same across and down: the plot is the largest rectangle of the matrix' own proportions that fits, centred, at any
        /// figure size.</para>
        /// <para>The chart is its parts: written to JSON it is the scatter and the limits, and read back it draws the same
        /// picture. It is not laid out again; change the matrix and call the verb again.</para></summary>
        /// <param name="matrix">The values, rows by columns; at least one of each.</param>
        /// <param name="threshold">A cell is a marker only when its magnitude is above this; 0 (the default) marks every cell
        /// that is not zero. A finite number of 0 or more.</param>
        /// <param name="configure">Optional styling of the one scatter, run once after the verb has built it, so what it sets
        /// wins: its colour, its marker and the marker's size, a label.</param>
        /// <returns>The same builder, for chaining.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="matrix"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="matrix"/> has no row or no column.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="threshold"/> is negative, NaN or infinite.</exception>
        /// <example>
        /// <code>
        /// // A banded matrix: the diagonal and the two diagonals beside it.
        /// var band = new double[20, 20];
        /// for (int r = 0; r &lt; 20; r++)
        /// {
        ///     for (int c = Math.Max(0, r - 1); c &lt;= Math.Min(19, r + 1); c++)
        ///     {
        ///         band[r, c] = 1;
        ///     }
        /// }
        ///
        /// string svg = Plt.Create()
        ///     .WithTitle("A tridiagonal matrix")
        ///     .WithSize(500, 500)
        ///     .AddSubPlot(1, 1, 1, ax => ax
        ///         .Spy(band, configure: s => s.MarkerSize = 12))
        ///     .ToSvg();
        /// </code>
        /// </example>
        public AxesBuilder Spy(double[,] matrix, double threshold = 0,
            Action<ScatterSeries>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(axes);
            ArgumentNullException.ThrowIfNull(matrix);
            matrix.ThrowIfNoCell("a spy plot");
            int rows = matrix.GetLength(0), columns = matrix.GetLength(1);

            threshold.ThrowIfNotFinite(nameof(threshold)).ThrowIfBelow(0, nameof(threshold));
            var marks = matrix.CellsAbove(threshold);
            // matplotlib's spy draws its markers as a Line2D it adds itself (add_line), in lines.color, asking no cycle.
            var cells = new ScatterSeries(marks.Columns, marks.Rows) { Marker = MarkerStyle.Square, AsksAs = CycleCall.Neither };

            // The half cell at each end is the margin a marker needs: a limit clips at the frame, so a marker standing on the
            // first or last column or row would be cut in half. The Y limits run downward: row 0 on top. The same limits are why
            // the automatic ticks cannot stay: they would be halves of a cell, and one on the margin before the first cell.
            return axes.AddSeries(cells, configure)
                .SetXLim(-0.5, columns - 0.5).SetYLim(rows - 0.5, -0.5)
                .SetAspect(1)
                .SetXTickLocator(new MultipleLocator(StepOf(columns)))
                .SetYTickLocator(new MultipleLocator(StepOf(rows)));
        }
    }

    /// <summary>The steps a whole-number tick step is a power of ten times.</summary>
    private static readonly int[] Ladder = [1, 2, 5];

    /// <summary>The step between the ticks of an axis that numbers <paramref name="cells"/> cells: the smallest of 1, 2, 5, 10,
    /// 20, 50 ... that leaves at most nine intervals across them (<see cref="TickBudget.Standard"/>; matplotlib's whole-number locator
    /// of a spy plot: nine bins and the steps 1, 2, 5 and 10).</summary>
    /// <param name="cells">The number of cells along the axis; 1 or more.</param>
    /// <returns>The distance between two ticks, in cells.</returns>
    internal static int StepOf(int cells)
    {
        for (long magnitude = 1; ; magnitude *= 10)
        {
            foreach (int rung in Ladder)
            {
                if (cells <= TickBudget.Standard.Most * rung * magnitude)
                {
                    return (int)(rung * magnitude);
                }
            }
        }
    }

    /// <summary>The numbers of the cells a tick stands on, by the cell's index: every <see cref="GridAxesExtensions.StepOf"/> cells from the first,
    /// each naming its own index.</summary>
    /// <param name="cells">The number of cells along the axis; 1 or more.</param>
    /// <returns>The name of each ticked cell; a cell between two ticks has no entry.</returns>
    internal static Dictionary<int, string> NamesOf(int cells)
    {
        int step = StepOf(cells);
        var names = new Dictionary<int, string>();
        for (int cell = 0; cell < cells; cell += step)
        {
            names[cell] = cell.InvariantText();
        }

        return names;
    }

    /// <summary>The share of a cell's side left clear on each edge, so a mark never touches the cell's border.</summary>
    internal const double CellInset = 0.1;

    /// <summary>The share of its stripe a mark fills; the rest is the gap between two events of a day.</summary>
    internal const double StripeFill = 0.9;

    extension(CalendarGrid grid)
    {
        /// <summary>The date of the cell at <paramref name="row"/> and <paramref name="column"/>, or <see langword="null"/> when
        /// the cell lies before the first date or after the last (the days of the first and the last week the span does not
        /// hold).</summary>
        /// <param name="row">The row, from 0 (the top).</param>
        /// <param name="column">The week column, from 0.</param>
        /// <returns>The day the cell stands for, if it is in the span.</returns>
        internal DateTime? DateAt(int row, int column)
        {
            int days = column * CalendarGrid.DaysPerWeek + row - grid.RowOf(grid.First);
            return days >= 0 && days <= (grid.Last - grid.First).Days ? grid.First.AddDays(days) : null;
        }

        /// <summary>The days of the calendar as the cells of a heatmap: 0 for every day of the span, missing for the cells of the first
        /// and the last week outside it, so the days draw as a neutral grid behind the marks.</summary>
        /// <returns>One row for each weekday and one column for each week.</returns>
        internal double[,] DayCells()
        {
            var cells = new double[CalendarGrid.DaysPerWeek, grid.Columns];
            for (int row = 0; row < CalendarGrid.DaysPerWeek; row++)
            {
                for (int column = 0; column < grid.Columns; column++)
                {
                    cells[row, column] = grid.DateAt(row, column) is null ? double.NaN : 0;
                }
            }

            return cells;
        }

        /// <summary>The events as bar series, one for each kind in the order the kinds first appear: each event is a bar named by
        /// its ISO date, standing in the stripe the events of its day share (<see cref="GridAxesExtensions.StripeOf"/>), in the order they were
        /// given. Every mark is as wide as the narrowest stripe of the calendar, so one width serves all of a kind. A kind with no
        /// name has no legend name.</summary>
        /// <param name="dates">The day of each event.</param>
        /// <param name="kinds">The kind of each event, one per date; <see langword="null"/> counts as no kind.</param>
        /// <returns>The series, in the order of the kinds.</returns>
        internal List<BarSeries> EventMarks(DateTime[] dates, string[] kinds)
        {
            var eventsOnDay = dates.GroupBy(date => date.Date).ToDictionary(group => group.Key, group => group.Count());
            int mostOnADay = eventsOnDay.Values.Max();
            var placed = new Dictionary<DateTime, int>();
            var marksOfKind = new Dictionary<string, FloatingBars>();
            var kindOrder = new List<string>();
            for (int i = 0; i < dates.Length; i++)
            {
                var day = dates[i].Date;
                int index = placed.GetValueOrDefault(day);
                placed[day] = index + 1;
                string kind = kinds[i] ?? string.Empty;
                var stripe = grid.StripeOf(day, index, eventsOnDay[day], mostOnADay);
                if (!marksOfKind.TryGetValue(kind, out var marks))
                {
                    marksOfKind[kind] = marks = new FloatingBars(stripe.Width, null);
                    kindOrder.Add(kind);
                }

                marks.Add(day.InvariantText("yyyy-MM-dd"), stripe.Centre, stripe.Foot, stripe.Height);
            }

            return [.. kindOrder.Select(kind => marksOfKind[kind].ToSeries(kind.Length > 0 ? kind : null))];
        }

        /// <summary>Where the event number <paramref name="index"/> of <paramref name="count"/> that fall on <paramref name="day"/>
        /// stands in the cell of that day, in the units of the grid drawn as a heatmap (row 0 at the top).</summary>
        /// <param name="day">A day of the span.</param>
        /// <param name="index">The event's place among the events of that day, from 0.</param>
        /// <param name="count">How many events the day has; at least 1.</param>
        /// <param name="mostOnADay">The most events any day of the calendar has, which sets the width of every mark; at least
        /// <paramref name="count"/>.</param>
        /// <returns>The stripe the event's mark fills.</returns>
        internal EventStripe StripeOf(DateTime day, int index, int count, int mostOnADay)
        {
            // A heatmap draws row 0 at the top while the Y axis counts upward: the row's slot along Y is counted from the bottom.
            var row = CategorySlot.FromTop(grid.RowOf(day), CalendarGrid.DaysPerWeek);
            double inner = 1 - 2 * CellInset;
            double stripe = inner / count;
            return new EventStripe(
                grid.ColumnOf(day) + CellInset + (index + 0.5) * stripe,
                inner / mostOnADay * StripeFill,
                row.Left + CellInset,
                inner);
        }
    }

    /// <summary>Where one event stands inside the cell of its day, in the units of the calendar grid (a cell is one unit across
    /// and one unit down): the middle of its stripe along the week axis, the stripe's width, and the foot and the height of the
    /// mark.</summary>
    /// <param name="Centre">The middle of the stripe along X.</param>
    /// <param name="Width">How wide the stripe's mark is; narrower than the stripe, so neighbours do not touch.</param>
    /// <param name="Foot">Where the mark begins along Y.</param>
    /// <param name="Height">How tall the mark is.</param>
    internal readonly record struct EventStripe(double Centre, double Width, double Foot, double Height);

    /// <summary>A span of days laid out as a calendar, and the one owner of where a day stands in it: seven rows, one for each
    /// day of the week, the first row the day the caller's week starts on (<see cref="GridAxesExtensions.CalendarGrid.FirstDayOfWeek"/>), by one column for
    /// each week, from the week that holds the first date to the week that holds the last.
    /// <para>A day's row is its place in the week counted from the week start, <c>((int)day.DayOfWeek - (int)FirstDayOfWeek +
    /// 7) % 7</c>, and its column is the number of weeks since the week that holds the first date. The arithmetic is exact
    /// integer math on the week start the caller names, never on the culture's, which differs between machines (Sunday under
    /// en-US, Monday under most of Europe) and between Windows and Linux; the names of the days and months are the invariant
    /// culture's for the same reason.</para>
    /// <para>A day's cell holds the sum of the values given for it, whatever their time of day. A missing value (NaN) adds
    /// nothing, and a day without a value, or with only missing values, is NaN: blank, never zero. A year is 53 columns wide,
    /// never 52 (365 days are one more than 52 weeks), and 54 when it has 366 days and starts on the last day of a week.</para>
    /// <para>Read by <see cref="GridAxesExtensions.CalendarHeatmap"/>, which draws <see cref="GridAxesExtensions.CalendarGrid.Cells"/> as one heatmap, row 0 at
    /// the top.</para></summary>
    internal sealed class CalendarGrid
    {
        /// <summary>The number of rows: the days of a week.</summary>
        internal const int DaysPerWeek = 7;

        /// <summary>A Sunday (the 1st of January 2006): the week from it holds the days in the order of <see cref="DayOfWeek"/>, so the
        /// date <c>(int)day</c> days after it names <c>day</c>.</summary>
        private static readonly DateTime ASunday = new(2006, 1, 1);

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

        /// <summary>The date of the cell in row 0, column 0: the first day of the week that holds <see cref="GridAxesExtensions.CalendarGrid.First"/>. It lies up to
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
            [.. Enumerable.Range(0, DaysPerWeek).Select(row => ASunday.AddDays(((int)FirstDayOfWeek + row) % DaysPerWeek).InvariantText("ddd"))];

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
            dates.ThrowIfEmpty("No dates were given; a calendar draws one cell per day.");

            ArgumentNullException.ThrowIfNull(values);
            values.Length.ThrowIfMisaligned(dates.Length, nameof(values));
            firstDayOfWeek.ThrowIfNotDefined(nameof(firstDayOfWeek), "A week starts on one of the seven days of the week.");

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
        /// <see cref="GridAxesExtensions.CalendarGrid.FirstDayOfWeek"/>.</summary>
        /// <param name="date">Any date; only its day of the week counts.</param>
        /// <returns>The row, from 0 (the top) to 6.</returns>
        internal int RowOf(DateTime date) => ((int)date.DayOfWeek - (int)FirstDayOfWeek + DaysPerWeek) % DaysPerWeek;

        /// <summary>The column <paramref name="date"/> stands in: the number of weeks since the week that holds
        /// <see cref="GridAxesExtensions.CalendarGrid.First"/>. The days of the first column before <see cref="GridAxesExtensions.CalendarGrid.First"/> are counted in, so a week start is
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
                var start = new DateTime(month / 12, month % 12 + 1, 1);
                columns.Add(ColumnOf(start), start.InvariantText("MMM"));
            }

            return columns;
        }
    }
}
