// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using Microsoft.Data.Analysis;

namespace MatPlotLibNet.DataFrame;

/// <summary>Converts a <see cref="DataFrameColumn"/> to a plain managed array.</summary>
public static class DataFrameColumnReader
{
    /// <summary>
    /// Reads every row from <paramref name="column"/> and converts it to <see langword="double"/>.
    /// Null entries become <see cref="double.NaN"/>.
    /// <see cref="DateTime"/> entries are mapped via <see cref="DateTime.ToOADate"/>.
    /// All other numeric types are forwarded through <see cref="Convert.ToDouble(object)"/>.
    /// </summary>
    public static double[] ToDoubleArray(DataFrameColumn column)
    {
        var arr = new double[column.Length];
        for (long i = 0; i < column.Length; i++)
        {
            var v = column[i];
            arr[i] = v switch
            {
                null       => double.NaN,
                DateTime dt => dt.ToOADate(),
                _          => Convert.ToDouble(v, System.Globalization.CultureInfo.InvariantCulture)
            };
        }
        return arr;
    }

    /// <summary>
    /// Reads every row from <paramref name="column"/> and converts it to <see langword="string"/>.
    /// Null entries become <see cref="string.Empty"/>.
    /// </summary>
    public static string[] ToStringArray(DataFrameColumn column)
    {
        var arr = new string[column.Length];
        for (long i = 0; i < column.Length; i++)
            arr[i] = column[i]?.ToString() ?? string.Empty;
        return arr;
    }
}
