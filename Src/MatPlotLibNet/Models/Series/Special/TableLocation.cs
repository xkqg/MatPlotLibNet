// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models.Series;

/// <summary>Where a <see cref="TableSeries"/> stands relative to the axes it belongs to.</summary>
public enum TableLocation
{
    /// <summary>In the plot area, from its top: every displayed column, the row headers included, shares the width of the plot, and
    /// the axes around it draws no frame, ticks or grid when the table is all it holds. The library's own themes place a table here.</summary>
    InPlot = 0,

    /// <summary>Below the axes, as matplotlib's <c>ax.table</c> places one by default (<c>loc='bottom'</c>): the data columns share the
    /// width of the plot, the row headers stand in a column to the left of it, as wide as their text needs unless
    /// <see cref="TableSeries.ColumnWidths"/> gives that column a width, and the axes above is drawn as any axes is. The matplotlib
    /// themes place a table here, and the layout keeps room for it below the axes.</summary>
    BelowAxes = 1,
}
