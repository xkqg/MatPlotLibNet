// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models;

/// <summary>How the cells of a <see cref="ChartDataColumn"/> are read. The kind lives on the column, not the
/// cell: a column is homogeneous, and it is the axis — not the value — that knows whether <c>46082</c> is a
/// count or the 15th of March.</summary>
public enum DataColumnKind
{
    /// <summary>Numbers, spelled in full and culture-invariant.</summary>
    Number = 0,

    /// <summary>OLE Automation dates (what a date axis carries), spelled as <c>yyyy-MM-dd</c>, then as far
    /// into <c>HH:mm:ss.fff</c> as the value's own precision goes.</summary>
    Date = 1,

    /// <summary>Texts: categories, labels, names.</summary>
    Text = 2,
}

/// <summary>Which axis of the subplot a column's values are positions on, if any. A series only knows that its
/// <c>start</c> and <c>end</c> are x coordinates; the AXES knows whether x reads as dates — so the series marks
/// the column, and the composition that has the axes decides how it prints.</summary>
public enum DataAxis
{
    /// <summary>Not a position: a value, a count, a label, a size.</summary>
    None = 0,

    /// <summary>A position along the x axis.</summary>
    X = 1,

    /// <summary>A position along the y axis.</summary>
    Y = 2,
}

/// <summary>A column of a <see cref="ChartDataTable"/>: its header, the kind of its cells, and the axis its
/// values are positions on.</summary>
/// <param name="Header">The header text, as the reader sees it.</param>
/// <param name="Kind">How the column's cells are read.</param>
/// <param name="Axis">The axis the column's values are positions on — <see cref="DataAxis.None"/> for a value
/// that is not a coordinate.</param>
public readonly record struct ChartDataColumn(
    string Header,
    DataColumnKind Kind = DataColumnKind.Number,
    DataAxis Axis = DataAxis.None)
{
    /// <summary>The header text, as the reader sees it.</summary>
    public string Header { get; } = Header ?? throw new ArgumentNullException(nameof(Header));

    /// <summary>A column of positions along one axis (a point's <c>x</c> or <c>y</c>, a bar's start, a price): the one spelling of a
    /// column on an axis in every table, and the owner of the bare header a position the series gives no name of its own is read under.</summary>
    /// <param name="axis">The axis the values are positions on: <see cref="DataAxis.X"/> or <see cref="DataAxis.Y"/>.</param>
    /// <param name="header">What the series calls it; <see langword="null"/> for the bare header, <c>x</c> or <c>y</c>.</param>
    /// <returns>The column.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="axis"/> is <see cref="DataAxis.None"/>, which has no default header.</exception>
    internal static ChartDataColumn Position(DataAxis axis, string? header = null) => new(header ?? axis switch
    {
        DataAxis.X => "x",
        DataAxis.Y => "y",
        _ => throw axis.NotAMember(nameof(axis), "A point is a position along the X or the Y axis."),
    }, Axis: axis);
}
