// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Interaction;

/// <summary>The rows a brush covers: the keys (<see cref="XYSeries.RowKeys"/>) of the points inside the rectangle, in the order
/// of the data, each once. It is derived from a <see cref="BrushSelectEvent"/> by the one rule that reads the rectangle against
/// the points (<see cref="RowSelectionExtensions.RowSelectionOf"/>), wherever the brush was made, and given to the
/// <see cref="IChartEventHandler"/> right after the brush itself, so an application can act on rows (filter a table, light them
/// elsewhere) instead of on a rectangle of pixels. An empty selection is a selection: the reader brushed an empty place and
/// cleared it. Non-mutating, and never sent by a client: it is worked out where the figure is.</summary>
/// <param name="ChartId">The chart that was brushed.</param>
/// <param name="AxesIndex">The subplot that was brushed.</param>
/// <param name="Rows">The keys of the rows inside the brush.</param>
public sealed record RowSelectionEvent(string ChartId, int AxesIndex, IReadOnlyList<string> Rows)
    : FigureNotificationEvent(ChartId, AxesIndex);

/// <summary>Reads a brush against the rows of a figure.</summary>
public static class RowSelectionExtensions
{
    extension(Figure figure)
    {
        /// <summary>The rows a brush covers on its subplot: the key of every point of a visible series with keys that lies inside
        /// the rectangle (its edges included), each key once, in the order of the data. A point that cannot be placed, a missing
        /// value or an infinity, is never inside.</summary>
        /// <param name="brush">The brush, with its rectangle in data units and the subplot it was made on.</param>
        /// <returns>The event for the rows; <see langword="null"/> when the subplot does not exist or has no series with keys,
        /// since the brush then says nothing about rows.</returns>
        public RowSelectionEvent? RowSelectionOf(BrushSelectEvent brush)
        {
            if ((uint)brush.AxesIndex >= (uint)figure.SubPlots.Count) return null;

            var withKeys = figure.SubPlots[brush.AxesIndex].Series.OfType<XYSeries>().Where(s => s.RowKeys is not null).ToArray();
            if (withKeys.Length == 0) return null;

            double xLow = Math.Min(brush.X1, brush.X2), xHigh = Math.Max(brush.X1, brush.X2);
            double yLow = Math.Min(brush.Y1, brush.Y2), yHigh = Math.Max(brush.Y1, brush.Y2);
            var rows = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var series in withKeys.Where(s => s.Visible))
            {
                for (int i = 0; i < series.RowKeys!.Length; i++)
                {
                    double x = series.XData[i], y = series.YData[i];
                    if (x >= xLow && x <= xHigh && y >= yLow && y <= yHigh && seen.Add(series.RowKeys[i]))
                        rows.Add(series.RowKeys[i]);
                }
            }

            return new RowSelectionEvent(brush.ChartId, brush.AxesIndex, rows);
        }
    }
}
