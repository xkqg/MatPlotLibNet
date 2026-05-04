// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Microsoft.Data.Analysis;
using MsDataFrame = Microsoft.Data.Analysis.DataFrame;

namespace MatPlotLibNet.DataFrame;

/// <summary>Internal column-access extensions shared by all DataFrame bridge classes.</summary>
internal static class DataFrameColumnAccess
{
    internal static double[] DoubleCol(this MsDataFrame df, string name)
    {
        GuardColumn(df, name);
        return DataFrameColumnReader.ToDoubleArray(df[name]);
    }

    internal static string[] StringCol(this MsDataFrame df, string name)
    {
        GuardColumn(df, name);
        return DataFrameColumnReader.ToStringArray(df[name]);
    }

    internal static DataFrameColumn AnyCol(this MsDataFrame df, string name)
    {
        GuardColumn(df, name);
        return df[name];
    }

    private static void GuardColumn(MsDataFrame df, string name)
    {
        if (!df.Columns.Any(c => c.Name == name))
            throw new ArgumentException($"DataFrame has no column '{name}'.", nameof(name));
    }
}
