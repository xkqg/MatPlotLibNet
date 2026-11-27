// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Microsoft.Data.Analysis;
using MsDataFrame = Microsoft.Data.Analysis.DataFrame;

namespace MatPlotLibNet;

/// <summary>Reads a named column of a <see cref="MsDataFrame"/>: the one place the package finds a column by its name, refuses one
/// the frame does not have, and turns its rows into an array every other call of the package (a figure, a fit, the bars of an
/// indicator) takes.</summary>
/// <example>
/// A moving average of the close column:
/// <code>
/// double[] sma20 = new MatPlotLibNet.Indicators.Sma(df.Values("close"), 20).Compute();
/// </code>
/// </example>
public static class DataFrameColumnExtensions
{
    /// <param name="df">The data frame.</param>
    extension(MsDataFrame df)
    {
        /// <summary>The rows of a numeric column as numbers: a missing value is NaN, a date its OLE Automation date
        /// (<see cref="DataFrame.DataFrameColumnReader.ToDoubleArray"/>).</summary>
        /// <param name="column">The name of the column.</param>
        /// <returns>One number per row.</returns>
        /// <exception cref="ArgumentException">The frame has no column of that name.</exception>
        public double[] Values(string column) => DataFrame.DataFrameColumnReader.ToDoubleArray(df.AnyCol(column));

        /// <summary>The rows of a column as text: a missing value is the empty string.</summary>
        /// <param name="column">The name of the column.</param>
        /// <returns>One string per row.</returns>
        /// <exception cref="ArgumentException">The frame has no column of that name.</exception>
        internal string[] StringCol(string column) => DataFrame.DataFrameColumnReader.ToStringArray(df.AnyCol(column));

        /// <summary>The hue label of each row: the rows of the hue column as text, or none when the caller names no hue column.</summary>
        /// <param name="column">The name of the hue column; <see langword="null"/> when there is none.</param>
        /// <returns>One label per row, or <see langword="null"/>.</returns>
        /// <exception cref="ArgumentException">The frame has no column of that name.</exception>
        internal string[]? HueCol(string? column) => column is null ? null : df.StringCol(column);

        /// <summary>The column of that name.</summary>
        /// <param name="column">The name of the column.</param>
        /// <returns>The column.</returns>
        /// <exception cref="ArgumentException">The frame has no column of that name.</exception>
        internal DataFrameColumn AnyCol(string column) =>
            df.Columns.Any(c => c.Name == column)
                ? df[column]
                : throw new ArgumentException($"DataFrame has no column '{column}'.", nameof(column));
    }
}
