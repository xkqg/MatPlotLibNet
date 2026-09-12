// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models;

/// <summary>What a <see cref="DataCell"/> holds.</summary>
public enum DataCellKind
{
    /// <summary>Nothing — the slot exists because the table is rectangular, not because there was a value.</summary>
    Empty = 0,

    /// <summary>A number; the column's <see cref="DataColumnKind"/> decides how it is spelled.</summary>
    Number = 1,

    /// <summary>A text: a category, a label, a task name.</summary>
    Text = 2,
}

/// <summary>One cell of a <see cref="ChartDataTable"/>: a number, a text, or nothing. Built only through
/// <see cref="FromNumber(double)"/>, <see cref="FromText(string)"/> and <see cref="Empty"/>, so a cell that is both a
/// number and a text — or neither while claiming to be one — cannot be written. <c>default(DataCell)</c> is
/// <see cref="Empty"/>.</summary>
public readonly record struct DataCell
{
    private readonly double _value;

    private DataCell(DataCellKind kind, double value, string? text)
    {
        Kind = kind;
        _value = value;
        Text = text;
    }

    /// <summary>The empty cell.</summary>
    public static DataCell Empty => default;

    /// <summary>A cell holding <paramref name="value"/>; NaN and the infinities are values too — a series can
    /// hold them, so the table records them and the emitter spells them.</summary>
    public static DataCell FromNumber(double value) => new(DataCellKind.Number, value, null);

    /// <summary>A cell holding <paramref name="text"/>.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is null.</exception>
    public static DataCell FromText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return new DataCell(DataCellKind.Text, 0, text);
    }

    /// <summary>What this cell holds.</summary>
    public DataCellKind Kind { get; }

    /// <summary>The number, or NaN when the cell is not a <see cref="DataCellKind.Number"/>.</summary>
    public double Number => Kind == DataCellKind.Number ? _value : double.NaN;

    /// <summary>The text, or null when the cell is not a <see cref="DataCellKind.Text"/>.</summary>
    public string? Text { get; }
}
