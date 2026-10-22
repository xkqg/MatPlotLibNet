// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models.Series;

/// <summary>A series whose rows stand on the axes in a way the columns of its own table cannot say as positions: a
/// heatmap counts its rows and columns, it does not place them, and a ridge stands on a baseline as a whole. Handed the
/// names the axes shows (<see cref="AxisNames"/>), such a series tables itself with them, so its table names its rows by
/// the names its picture shows. The data table asks the axes for the names; the series only says where it stands.</summary>
internal interface ITableNamedByAxes
{
    /// <summary>The series' table, with its rows named by what <paramref name="names"/> shows where they stand; the plain
    /// table (<see cref="ISeries.ToDataTable"/>) where the axes names nothing there.</summary>
    /// <param name="names">The names the axes shows along the axes the series stands on.</param>
    /// <returns>The table, or <see langword="null"/> for a series with no tabular form.</returns>
    ChartDataTable? ToDataTable(AxisNames names);
}
