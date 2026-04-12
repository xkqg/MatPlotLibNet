// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.DataFrame;
using MatPlotLibNet.Styling;
using MsDataFrame = Microsoft.Data.Analysis.DataFrame;

namespace MatPlotLibNet;

/// <summary>
/// Extension methods that turn a <see cref="MsDataFrame"/> directly into a <see cref="FigureBuilder"/>,
/// using column names for X, Y, and an optional hue (group-by) column.
/// Materialises the named columns to arrays, then delegates all grouping and series-creation logic
/// to <see cref="EnumerableFigureExtensions"/>.
/// </summary>
/// <example>
/// Line chart from two numeric columns:
/// <code>
/// // df has columns: "year" (int), "revenue" (double)
/// string svg = df.Line("year", "revenue")
///     .WithTitle("Annual Revenue")
///     .ToSvg();
/// </code>
/// Scatter with hue grouping and a custom palette:
/// <code>
/// // df has columns: "age", "income", "segment" (string)
/// string svg = df.Scatter("age", "income", hue: "segment",
///         palette: [Color.Blue, Color.Orange, Color.Green])
///     .WithTitle("Income by Segment")
///     .ToSvg();
/// </code>
/// Overlapping histograms per group:
/// <code>
/// // df has columns: "score" (double), "cohort" (string)
/// string svg = df.Hist("score", bins: 20, hue: "cohort")
///     .WithTitle("Score Distribution")
///     .ToSvg();
/// </code>
/// </example>
public static class DataFrameFigureExtensions
{
    /// <summary>Plots two numeric columns as a line series, with optional hue grouping.</summary>
    /// <param name="df">The source data frame.</param>
    /// <param name="x">Name of the numeric X column.</param>
    /// <param name="y">Name of the numeric Y column.</param>
    /// <param name="hue">Optional name of a string column to group by. One series per unique value.</param>
    /// <param name="palette">Optional color palette. Defaults to Tab10 when <see langword="null"/>.</param>
    public static FigureBuilder Line(
        this MsDataFrame df,
        string           x,
        string           y,
        string?          hue     = null,
        Color[]?         palette = null)
    {
        var xs = DataFrameColumnReader.ToDoubleArray(ResolveColumn(df, x));
        var ys = DataFrameColumnReader.ToDoubleArray(ResolveColumn(df, y));

        if (hue is null)
            return Enumerable.Range(0, xs.Length)
                .Select(i => new Xy(xs[i], ys[i]))
                .Line(r => r.X, r => r.Y, palette: palette);

        var hs = DataFrameColumnReader.ToStringArray(ResolveColumn(df, hue));
        return Enumerable.Range(0, xs.Length)
            .Select(i => new Xyh(xs[i], ys[i], hs[i]))
            .Line(r => r.X, r => r.Y, r => r.Hue, palette);
    }

    /// <summary>Plots two numeric columns as a scatter series, with optional hue grouping.</summary>
    /// <param name="df">The source data frame.</param>
    /// <param name="x">Name of the numeric X column.</param>
    /// <param name="y">Name of the numeric Y column.</param>
    /// <param name="hue">Optional name of a string column to group by. One series per unique value.</param>
    /// <param name="palette">Optional color palette. Defaults to Tab10 when <see langword="null"/>.</param>
    public static FigureBuilder Scatter(
        this MsDataFrame df,
        string           x,
        string           y,
        string?          hue     = null,
        Color[]?         palette = null)
    {
        var xs = DataFrameColumnReader.ToDoubleArray(ResolveColumn(df, x));
        var ys = DataFrameColumnReader.ToDoubleArray(ResolveColumn(df, y));

        if (hue is null)
            return Enumerable.Range(0, xs.Length)
                .Select(i => new Xy(xs[i], ys[i]))
                .Scatter(r => r.X, r => r.Y, palette: palette);

        var hs = DataFrameColumnReader.ToStringArray(ResolveColumn(df, hue));
        return Enumerable.Range(0, xs.Length)
            .Select(i => new Xyh(xs[i], ys[i], hs[i]))
            .Scatter(r => r.X, r => r.Y, r => r.Hue, palette);
    }

    /// <summary>Plots a numeric column as a histogram, with optional hue grouping.</summary>
    /// <param name="df">The source data frame.</param>
    /// <param name="column">Name of the numeric column.</param>
    /// <param name="bins">Number of histogram bins (default 30).</param>
    /// <param name="hue">Optional name of a string column to group by. One series per unique value.</param>
    /// <param name="palette">Optional color palette. Defaults to Tab10 when <see langword="null"/>.</param>
    public static FigureBuilder Hist(
        this MsDataFrame df,
        string           column,
        int              bins    = 30,
        string?          hue     = null,
        Color[]?         palette = null)
    {
        var vs = DataFrameColumnReader.ToDoubleArray(ResolveColumn(df, column));

        if (hue is null)
            return vs.Hist(v => v, bins, palette: palette);

        var hs = DataFrameColumnReader.ToStringArray(ResolveColumn(df, hue));
        return Enumerable.Range(0, vs.Length)
            .Select(i => new Vh(vs[i], hs[i]))
            .Hist(r => r.Value, bins, r => r.Hue, palette);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static Microsoft.Data.Analysis.DataFrameColumn ResolveColumn(MsDataFrame df, string name)
    {
        // DataFrame[string] throws KeyNotFoundException; detect up front for a friendlier message
        if (!df.Columns.Any(c => c.Name == name))
            throw new ArgumentException($"DataFrame has no column '{name}'.", nameof(name));
        return df[name];
    }

    // ── Private row-carrier record structs ────────────────────────────────────

    private readonly record struct Xy(double X, double Y);
    private readonly record struct Xyh(double X, double Y, string Hue);
    private readonly record struct Vh(double Value, string Hue);
}
