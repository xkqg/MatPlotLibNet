// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Styling;

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
            if (kinds.Length != dates.Length)
            {
                throw new ArgumentException($"{kinds.Length} kinds were given for {dates.Length} dates; there must be one kind per date.", nameof(kinds));
            }

            // Every refusal comes from laying out the grid, before the axes are touched.
            var grid = CalendarGrid.Of(dates, new double[dates.Length], firstDayOfWeek);

            // The days: a cell for every day of the span (0), blank outside it, in the theme's neutral grey, drawn behind the marks.
            var cells = new double[CalendarGrid.DaysPerWeek, grid.Columns];
            for (int row = 0; row < CalendarGrid.DaysPerWeek; row++)
            {
                for (int column = 0; column < grid.Columns; column++)
                {
                    cells[row, column] = grid.DateAt(row, column) is null ? double.NaN : 0;
                }
            }

            // The events of a day, in the order given, share its cell: their place among the day's events is their stripe.
            var eventsOnDay = dates.GroupBy(date => date.Date).ToDictionary(group => group.Key, group => group.Count());
            int mostOnADay = eventsOnDay.Values.Max();
            var placed = new Dictionary<DateTime, int>();
            var kindMarks = new Dictionary<string, KindMarks>();
            var kindOrder = new List<string>();
            for (int i = 0; i < dates.Length; i++)
            {
                var day = dates[i].Date;
                int index = placed.GetValueOrDefault(day);
                placed[day] = index + 1;
                string kind = kinds[i] ?? string.Empty;
                if (!kindMarks.TryGetValue(kind, out var marks))
                {
                    kindMarks[kind] = marks = new KindMarks();
                    kindOrder.Add(kind);
                }

                marks.Add(day, grid.StripeOf(day, index, eventsOnDay[day], mostOnADay));
            }

            var bars = new List<BarSeries>();
            foreach (string kind in kindOrder)
            {
                var marks = kindMarks[kind];
                bars.Add(new BarSeries([.. marks.Days], [.. marks.Heights])
                {
                    XCoordinate = [.. marks.Centres],
                    StackBaseline = [.. marks.Feet],
                    BarWidth = marks.Width,
                    Label = kind.Length > 0 ? kind : null,
                });
            }

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
        /// on the top edge and on the bottom edge too, since this library draws a mirrored tick on both. A missing value (NaN)
        /// is a blank cell, never a zero.</para>
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
            int rows = matrix.GetLength(0), columns = matrix.GetLength(1);
            if (rows == 0 || columns == 0)
            {
                throw new ArgumentException($"The matrix has {rows} rows and {columns} columns; a matrix shown needs at least one cell.", nameof(matrix));
            }

            // A heatmap draws row 0 at the top while its Y axis counts upward: the rows are numbered from the top down.
            return axes
                .Heatmap(matrix, configure)
                .SetAspect(1)
                .SetXCategoryTicks(WholeTickNames(columns))
                .SetYCategoryTicks([.. Enumerable.Range(0, rows).Select(row => WholeTickNames(rows).GetValueOrDefault(row, string.Empty))], topDown: true)
                .WithXTicksMirrored()
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
            int rows = matrix.GetLength(0), columns = matrix.GetLength(1);
            if (rows == 0 || columns == 0)
            {
                throw new ArgumentException($"The matrix has {rows} rows and {columns} columns; a spy plot needs at least one cell.", nameof(matrix));
            }

            if (!double.IsFinite(threshold) || threshold < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(threshold), threshold,
                    "The threshold must be a finite number of 0 or more; a cell is a marker when its magnitude is above it.");
            }

            var x = new List<double>();
            var y = new List<double>();
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < columns; c++)
                {
                    // NaN is above nothing, so a missing value falls out here without a branch of its own.
                    if (Math.Abs(matrix[r, c]) > threshold)
                    {
                        x.Add(c);
                        y.Add(r);
                    }
                }
            }

            var cells = new ScatterSeries([.. x], [.. y]) { Marker = MarkerStyle.Square };

            // The half cell at each end is the margin a marker needs: a limit clips at the frame, so a marker standing on the
            // first or last column or row would be cut in half. The Y limits run downward: row 0 on top. The same limits are why
            // the automatic ticks cannot stay: they would be halves of a cell, and one on the margin before the first cell.
            return axes.AddSeries(cells, configure)
                .SetXLim(-0.5, columns - 0.5).SetYLim(rows - 0.5, -0.5)
                .SetAspect(1)
                .SetXTickLocator(new MultipleLocator(WholeTickStep(columns)))
                .SetYTickLocator(new MultipleLocator(WholeTickStep(rows)));
        }
    }

    /// <summary>The step between the ticks of an axis that numbers <paramref name="cells"/> cells: the smallest of 1, 2, 5, 10,
    /// 20, 50 ... that leaves at most nine intervals across them (matplotlib's whole-number locator of a spy plot, nine bins
    /// and the steps 1, 2, 5 and 10), so a small matrix is ticked at every cell and a large one at round numbers.</summary>
    /// <param name="cells">The number of cells along the axis; 1 or more.</param>
    private static double WholeTickStep(int cells)
    {
        for (double magnitude = 1; ; magnitude *= 10)
        {
            foreach (int ladder in WholeTickLadder)
            {
                if (cells <= MaxTickIntervals * ladder * magnitude)
                {
                    return ladder * magnitude;
                }
            }
        }
    }

    /// <summary>The marks of one kind of event, as the arrays a bar series is made of: the event's day (its ISO date names the
    /// bar), the middle of its stripe along X, where the bar begins and how tall it is along Y. Every stripe of a calendar is
    /// as wide as the others of the calendar, so the width is the first one's.</summary>
    private sealed class KindMarks
    {
        internal List<string> Days { get; } = [];

        internal List<double> Centres { get; } = [];

        internal List<double> Feet { get; } = [];

        internal List<double> Heights { get; } = [];

        internal double Width { get; private set; }

        internal void Add(DateTime day, EventStripe stripe)
        {
            Days.Add(day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            Centres.Add(stripe.Centre);
            Feet.Add(stripe.Foot);
            Heights.Add(stripe.Height);
            if (Width == 0) Width = stripe.Width;
        }
    }

    /// <summary>The numbers of the cells a whole-number tick stands on, by the cell's index: every <see cref="WholeTickStep"/>
    /// cells from the first, each naming its own index.</summary>
    /// <param name="cells">The number of cells along the axis; 1 or more.</param>
    private static Dictionary<int, string> WholeTickNames(int cells)
    {
        int step = (int)WholeTickStep(cells);
        var names = new Dictionary<int, string>();
        for (int cell = 0; cell < cells; cell += step)
        {
            names[cell] = cell.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        return names;
    }

    /// <summary>The steps a whole-number tick step is a power of ten times.</summary>
    private static readonly int[] WholeTickLadder = [1, 2, 5];

    /// <summary>The most intervals the ticks of a spy plot divide an axis into.</summary>
    private const int MaxTickIntervals = 9;
}
