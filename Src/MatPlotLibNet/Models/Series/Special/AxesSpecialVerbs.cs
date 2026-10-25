// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet;

/// <summary>The non-chart "special" verbs of <see cref="Axes"/> (<c>Table</c>, <c>TreeGrid</c>), each constructing
/// one series and handing it to <see cref="Axes.AddSeries{T}"/>.</summary>
public static class AxesSpecialVerbs
{
    extension(Axes axes)
    {
        /// <summary>Adds a table series rendered inside the plot area.</summary>
        /// <param name="cellData">2D array of cell text values (rows × columns).</param>
        /// <returns>The newly created <see cref="TableSeries"/> for further configuration.</returns>
        public TableSeries Table(string[][] cellData) => axes.AddSeries(new TableSeries(cellData));

        /// <summary>Adds a tree-grid series — indented rows with right-aligned value columns.</summary>
        /// <param name="rows">The rows, already flattened in the order they are drawn.</param>
        /// <returns>The newly created <see cref="TreeGridSeries"/> for further configuration.</returns>
        public TreeGridSeries TreeGrid(IReadOnlyList<TreeGridRow> rows) => axes.AddSeries(new TreeGridSeries(rows));
    }
}
