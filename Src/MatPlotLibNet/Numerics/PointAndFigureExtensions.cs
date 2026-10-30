// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Numerics;

/// <summary>One column of a Point and Figure chart: a run of Xs that rises, or of Os that falls, as the indices of the boxes it
/// covers. A box has the index <c>level / boxSize</c> (a price is in the box whose level it reaches), so a column is exact
/// whatever the box size, and the chart divides no price by hand.</summary>
/// <param name="Rising"><see langword="true"/> for a column of Xs (the price rose), <see langword="false"/> for Os (it fell).</param>
/// <param name="From">The index of the box the column begins at: its lowest box when it rises, its highest when it falls.</param>
/// <param name="To">The index of the box the column ends at: its highest box when it rises, its lowest when it falls.</param>
internal readonly record struct PointAndFigureColumn(bool Rising, int From, int To);

/// <summary>The one owner of how a series of closing prices becomes the columns of a Point and Figure chart (close only).
/// A box is a step of <c>boxSize</c> counted from zero (<see cref="PriceStepExtensions"/>, tolerant of a decimal step): an X
/// at a level means the price reached that level, so a price is rounded down to its box in a rising column, and an O means it
/// reached it going down, so it is rounded up in a falling one. A column extends when the price reaches the next box in its
/// direction; otherwise it reverses when the price comes back <c>reversal</c> boxes from the last box PLOTTED, not from the
/// last price, the new column starting one box inside the old column's end; a price that does neither is ignored (a price that
/// extends can never also reverse). The first column is the one the first close that leaves the box of the first price
/// decides: up starts a rising column at the box below that first price, down a falling one at the box above it.</summary>
internal static class PointAndFigureExtensions
{
    extension(double[] prices)
    {
        /// <summary>The columns the prices draw.</summary>
        /// <param name="boxSize">The price one box covers; a finite number above zero (the series refuses any other).</param>
        /// <param name="reversal">How many boxes back from the last plotted box the price has to come to start a new column; 1
        /// or more (the series refuses any other).</param>
        /// <returns>The columns left to right; none when the prices never leave the box of the first one. A price that is not a
        /// finite number is skipped.</returns>
        internal IReadOnlyList<PointAndFigureColumn> PointAndFigureColumns(double boxSize, int reversal)
        {
            var columns = new List<PointAndFigureColumn>();
            PointAndFigureColumn? open = null;
            bool anchored = false;
            int anchorFloor = 0, anchorCeiling = 0;
            foreach (double close in prices)
            {
                if (!double.IsFinite(close))
                {
                    continue;
                }

                int floor = close.FloorStep(0, boxSize), ceiling = close.CeilingStep(0, boxSize);
                if (!anchored)
                {
                    anchorFloor = floor;
                    anchorCeiling = ceiling;
                    anchored = true;
                }
                else if (open is not { } column)
                {
                    if (floor > anchorFloor)
                    {
                        open = new PointAndFigureColumn(true, anchorFloor, floor);
                    }
                    else if (ceiling < anchorCeiling)
                    {
                        open = new PointAndFigureColumn(false, anchorCeiling, ceiling);
                    }
                }
                else if (column.Rising)
                {
                    if (floor > column.To)
                    {
                        open = column with { To = floor };
                    }
                    else if (ceiling <= column.To - reversal)
                    {
                        columns.Add(column);
                        open = new PointAndFigureColumn(false, column.To - 1, ceiling);
                    }
                }
                else if (ceiling < column.To)
                {
                    open = column with { To = ceiling };
                }
                else if (floor >= column.To + reversal)
                {
                    columns.Add(column);
                    open = new PointAndFigureColumn(true, column.To + 1, floor);
                }
            }

            if (open is { } last)
            {
                columns.Add(last);
            }

            return columns;
        }
    }
}
