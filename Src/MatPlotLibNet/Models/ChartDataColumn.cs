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

    /// <summary>OLE Automation dates (what a date axis carries), spelled as <c>yyyy-MM-dd</c>, with <c>HH:mm</c>
    /// when the value has a time part.</summary>
    Date = 1,

    /// <summary>Texts: categories, labels, names.</summary>
    Text = 2,
}

/// <summary>A column of a <see cref="ChartDataTable"/>: its header and the kind of its cells.</summary>
/// <param name="Header">The header text, as the reader sees it.</param>
/// <param name="Kind">How the column's cells are read.</param>
public readonly record struct ChartDataColumn(string Header, DataColumnKind Kind = DataColumnKind.Number)
{
    /// <summary>The header text, as the reader sees it.</summary>
    public string Header { get; } = Header ?? throw new ArgumentNullException(nameof(Header));
}
