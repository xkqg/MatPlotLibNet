// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet;

/// <summary>The verbs of the axes builder that add a table or a tree grid: each adds the series through the axes verb of the
/// same name (<see cref="AxesSpecialVerbs"/>) and hands it to the caller's callback.</summary>
public static class AxesBuilderSpecialVerbs
{
    extension(AxesBuilder axes)
    {
        /// <summary>Adds a table series to the axes.</summary>
        public AxesBuilder Table(string[][] cellData, Action<TableSeries>? configure = null)
            => axes.Configured(axes.Axes.Table(cellData), configure);

        /// <summary>Adds a tree grid — indented rows with right-aligned value columns (htop's process tree, the
        /// ARIA <c>treegrid</c>): what a treemap stops being able to do once the leaves are many and small.</summary>
        public AxesBuilder TreeGrid(IReadOnlyList<TreeGridRow> rows, Action<TreeGridSeries>? configure = null)
            => axes.Configured(axes.Axes.TreeGrid(rows), configure);
    }
}
