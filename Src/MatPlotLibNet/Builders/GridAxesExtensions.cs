// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

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
        /// set up before the call is kept as it was set up. It has no legend entry. The verb sets no axis limits and no aspect:
        /// the cells fill the plot area, so they are square only at a figure size that makes them so.</para>
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
        /// markers touch. The verb sets no aspect, so the spacing of the grid is the plot's width over the number of columns
        /// across and its height over the number of rows down; the two are equal only at a figure size that gives the plot the
        /// matrix' own proportions.</para>
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

    /// <summary>The steps a whole-number tick step is a power of ten times.</summary>
    private static readonly int[] WholeTickLadder = [1, 2, 5];

    /// <summary>The most intervals the ticks of a spy plot divide an axis into.</summary>
    private const int MaxTickIntervals = 9;
}
