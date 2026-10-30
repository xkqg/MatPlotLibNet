// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models.Series;

/// <summary>Where one event stands inside the cell of its day, in the units of the calendar grid (a cell is one unit across
/// and one unit down): the middle of its stripe along the week axis, the stripe's width, and the foot and the height of the
/// mark.</summary>
/// <param name="Centre">The middle of the stripe along X.</param>
/// <param name="Width">How wide the stripe's mark is; narrower than the stripe, so neighbours do not touch.</param>
/// <param name="Foot">Where the mark begins along Y.</param>
/// <param name="Height">How tall the mark is.</param>
internal readonly record struct EventStripe(double Centre, double Width, double Foot, double Height);

/// <summary>The one owner of where events stand inside the cells of a calendar. A day's cell is inset a tenth of a side on
/// every edge, and the events of a day divide the inner width into stripes side by side, in the order they were given, whatever
/// kind they are: one event stands in the middle, two a half of the width apart, three a third. Every mark is as wide as the
/// narrowest stripe of the calendar (the day with the most events), so the marks look alike from day to day and one bar series
/// can hold them all.</summary>
internal static class CalendarEventLayoutExtensions
{
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
            var row = CategorySlot.Of(CalendarGrid.DaysPerWeek - 1 - grid.RowOf(day));
            double inner = 1 - 2 * CellInset;
            double stripe = inner / count;
            return new EventStripe(
                grid.ColumnOf(day) + CellInset + (index + 0.5) * stripe,
                inner / mostOnADay * StripeFill,
                row.Left + CellInset,
                inner);
        }
    }
}
