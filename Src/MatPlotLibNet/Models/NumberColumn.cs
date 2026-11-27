// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models;

/// <summary>A column of numbers a table takes after its point (<see cref="ChartDataTable.XY"/>): its header, the numbers read by index
/// and the axis they are positions on, if any.</summary>
/// <param name="Header">The header text, as the reader sees it.</param>
/// <param name="Values">The numbers, one for each row.</param>
/// <param name="Axis">The axis the numbers are positions on; <see cref="DataAxis.None"/> for a value that is not a coordinate.</param>
internal readonly record struct NumberColumn(string Header, IReadOnlyList<double> Values, DataAxis Axis = DataAxis.None)
{
    /// <summary>The column of <paramref name="values"/>, when the series was given some: an arm it was not given (none, or an empty
    /// array) is no column.</summary>
    /// <param name="header">The header text.</param>
    /// <param name="axis">The axis the numbers are positions on.</param>
    /// <param name="values">The numbers, or none.</param>
    /// <returns>The column, or <see langword="null"/> when there is no number.</returns>
    internal static NumberColumn? IfAny(string header, DataAxis axis, IReadOnlyList<double>? values) =>
        values is { Count: > 0 } ? new NumberColumn(header, values, axis) : null;

    /// <summary>The column of the table it heads: a position (<see cref="ChartDataColumn.Position"/>) when the numbers are on an axis.</summary>
    internal ChartDataColumn Column => Axis is DataAxis.None ? new(Header) : ChartDataColumn.Position(Axis, Header);
}
