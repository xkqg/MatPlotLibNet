// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.DataFrame;
using MatPlotLibNet.Models.Series;
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
    /// <returns>A <see cref="FigureBuilder"/> ready for further configuration or rendering.</returns>
    public static FigureBuilder Line(
        this MsDataFrame df,
        string           x,
        string           y,
        string?          hue     = null,
        Color[]?         palette = null)
    {
        var xs = df.DoubleCol(x);
        var ys = df.DoubleCol(y);

        if (hue is null)
            return Enumerable.Range(0, xs.Length)
                .Select(i => new Xy(xs[i], ys[i]))
                .Line(r => r.X, r => r.Y, palette: palette);

        var hs = df.StringCol(hue);
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
    /// <returns>A <see cref="FigureBuilder"/> ready for further configuration or rendering.</returns>
    public static FigureBuilder Scatter(
        this MsDataFrame df,
        string           x,
        string           y,
        string?          hue     = null,
        Color[]?         palette = null)
    {
        var xs = df.DoubleCol(x);
        var ys = df.DoubleCol(y);

        if (hue is null)
            return Enumerable.Range(0, xs.Length)
                .Select(i => new Xy(xs[i], ys[i]))
                .Scatter(r => r.X, r => r.Y, palette: palette);

        var hs = df.StringCol(hue);
        return Enumerable.Range(0, xs.Length)
            .Select(i => new Xyh(xs[i], ys[i], hs[i]))
            .Scatter(r => r.X, r => r.Y, r => r.Hue, palette);
    }

    /// <summary>Builds a <see cref="PairGridSeries"/> from the named numeric columns of
    /// <paramref name="df"/>. When <paramref name="hue"/> is provided, the named string column
    /// is converted into <see cref="PairGridSeries.HueGroups"/> integer IDs and
    /// <see cref="PairGridSeries.HueLabels"/> human-readable category names so the figure-level
    /// legend renders the original strings instead of opaque integers.</summary>
    /// <remarks>
    /// <para><b>Hue convention divergence.</b> This extension carries <c>HueGroups</c> on the
    /// <see cref="PairGridSeries"/> model itself; <see cref="Line"/>/<see cref="Scatter"/>/<see cref="Hist"/>
    /// instead pre-shred to row-carrier records and rely on <c>EnumerableFigureExtensions</c>
    /// grouping. Both shapes are valid for their respective use cases: pair-grid needs the hue
    /// metadata at render time to resolve a per-group colour per cell, whereas the others can
    /// resolve at figure-build time. A future unified <c>IHueable</c> abstraction is tracked
    /// in <c>docs/contrib/v1-10b-pair-selection-series.md</c>.</para>
    /// <para><b>Empty-column validation.</b> Throws on an empty <paramref name="columns"/> array
    /// rather than propagating a cryptic downstream <see cref="IndexOutOfRangeException"/> from
    /// the row-count probe. The sibling extensions (Line/Scatter/Hist) propagate the underlying
    /// <see cref="System.Collections.Generic.KeyNotFoundException"/> from column lookup; the
    /// hardened upfront validation here is intentional.</para>
    /// </remarks>
    /// <param name="df">The source data frame.</param>
    /// <param name="columns">Names of the numeric columns to include, in order. Each column
    /// becomes one variable in the N×N pair grid.</param>
    /// <param name="hue">Optional name of a categorical (string) column for hue grouping.
    /// Distinct values are sorted lexicographically and assigned increasing integer group IDs.</param>
    /// <param name="palette">Optional palette to assign per-group colours. When <see langword="null"/>
    /// the renderer falls back to <c>QualitativeColorMaps.Tab10</c>.</param>
    /// <param name="configure">Optional callback to override defaults after the series is built.</param>
    /// <returns>A <see cref="FigureBuilder"/> ready for further configuration or rendering.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="columns"/> is empty or
    /// any named column is missing from <paramref name="df"/>.</exception>
    public static FigureBuilder PairGrid(
        this MsDataFrame            df,
        string[]                    columns,
        string?                     hue       = null,
        Color[]?                    palette   = null,
        Action<PairGridSeries>?     configure = null)
    {
        if (columns.Length == 0)
            throw new ArgumentException("At least one column must be specified.", nameof(columns));

        var variables = new double[columns.Length][];
        for (int c = 0; c < columns.Length; c++)
            variables[c] = df.DoubleCol(columns[c]);

        return Plt.Create().AddSubPlot(1, 1, 1, ax => ax.PairGrid(variables, s =>
        {
            s.Labels = columns;

            if (hue is not null)
            {
                var hueValues = df.StringCol(hue);
                var unique = hueValues.Distinct().OrderBy(x => x, StringComparer.Ordinal).ToArray();
                var idMap  = new Dictionary<string, int>(StringComparer.Ordinal);
                for (int i = 0; i < unique.Length; i++) idMap[unique[i]] = i;
                var groups = new int[hueValues.Length];
                for (int i = 0; i < hueValues.Length; i++) groups[i] = idMap[hueValues[i]];
                s.HueGroups = groups;
                s.HueLabels = unique;
            }

            if (palette is not null) s.HuePalette = palette;
            configure?.Invoke(s);
        }));
    }

    /// <summary>Builds a <see cref="NetworkGraphSeries"/> from named edge-list columns.
    /// Nodes are derived implicitly from the union of distinct values in the source
    /// (<paramref name="edgeFromCol"/>) and target (<paramref name="edgeToCol"/>) columns;
    /// optional <paramref name="weightCol"/> populates per-edge weight; optional
    /// <paramref name="directedCol"/> (boolean column) populates per-edge direction.</summary>
    /// <param name="df">The source data frame.</param>
    /// <param name="edgeFromCol">Name of the string column carrying the source-node ID per row.</param>
    /// <param name="edgeToCol">Name of the string column carrying the target-node ID per row.</param>
    /// <param name="weightCol">Optional name of a numeric column with per-edge weights. When null all weights default to 1.0.</param>
    /// <param name="directedCol">Optional name of a boolean column setting per-edge direction. When null all edges default to undirected.</param>
    /// <param name="configure">Optional callback to override defaults after the series is built.</param>
    /// <returns>A <see cref="FigureBuilder"/> ready for further configuration or rendering.</returns>
    /// <exception cref="ArgumentException">Thrown when any named column is missing from <paramref name="df"/>.</exception>
    public static FigureBuilder NetworkGraph(
        this MsDataFrame                df,
        string                          edgeFromCol,
        string                          edgeToCol,
        string?                         weightCol   = null,
        string?                         directedCol = null,
        Action<NetworkGraphSeries>?     configure   = null)
    {
        var fromValues = df.StringCol(edgeFromCol);
        var toValues   = df.StringCol(edgeToCol);
        int rows = Math.Min(fromValues.Length, toValues.Length);

        double[]? weights  = weightCol   is not null ? df.DoubleCol(weightCol) : null;
        // Boolean column: read as DataFrameColumn and coerce to bool via Convert.ToBoolean.
        bool[]?   directed = null;
        if (directedCol is not null)
        {
            var col = df.AnyCol(directedCol);
            directed = new bool[col.Length];
            for (long i = 0; i < col.Length; i++)
                directed[i] = Convert.ToBoolean(col[i] ?? false, System.Globalization.CultureInfo.InvariantCulture);
        }

        // Derive distinct node IDs from the union of From and To columns,
        // preserving first-seen order so output is deterministic per dataframe.
        var nodeIds = new List<string>(rows * 2);
        var seen    = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < rows; i++)
        {
            if (seen.Add(fromValues[i])) nodeIds.Add(fromValues[i]);
            if (seen.Add(toValues[i]))   nodeIds.Add(toValues[i]);
        }
        var nodes = new GraphNode[nodeIds.Count];
        for (int i = 0; i < nodeIds.Count; i++) nodes[i] = new GraphNode(nodeIds[i]);

        var edges = new GraphEdge[rows];
        for (int i = 0; i < rows; i++)
        {
            edges[i] = new GraphEdge(
                fromValues[i],
                toValues[i],
                Weight:     weights  is not null ? weights[i]  : 1.0,
                IsDirected: directed is not null && directed[i]);
        }

        return Plt.Create().AddSubPlot(1, 1, 1, ax => ax.NetworkGraph(nodes, edges, configure));
    }

    /// <summary>Builds a <see cref="ClustermapSeries"/> from the named numeric columns of
    /// <paramref name="df"/>. Each DataFrame row becomes a matrix row; each named column
    /// becomes a matrix column.</summary>
    /// <param name="df">The source data frame.</param>
    /// <param name="columns">Names of the numeric columns to include, in order.</param>
    /// <param name="configure">Optional callback to set <see cref="ClustermapSeries"/> properties
    /// such as <see cref="ClustermapSeries.RowTree"/>, <see cref="ClustermapSeries.ColorMap"/>,
    /// or <see cref="ClustermapSeries.ShowLabels"/>.</param>
    /// <returns>A <see cref="FigureBuilder"/> ready for further configuration or rendering.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="columns"/> is empty or
    /// contains a column name that does not exist in <paramref name="df"/>.</exception>
    public static FigureBuilder Clustermap(
        this MsDataFrame            df,
        string[]                    columns,
        Action<ClustermapSeries>?   configure = null)
    {
        if (columns.Length == 0)
            throw new ArgumentException("At least one column must be specified.", nameof(columns));

        var cols = columns.Select(name => df.DoubleCol(name)).ToArray();
        int rows = cols[0].Length;
        var data = new double[rows, columns.Length];
        for (int c = 0; c < columns.Length; c++)
            for (int r = 0; r < rows; r++)
                data[r, c] = cols[c][r];

        return Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Clustermap(data, configure));
    }

    /// <summary>Plots a numeric column as a histogram, with optional hue grouping.</summary>
    /// <param name="df">The source data frame.</param>
    /// <param name="column">Name of the numeric column.</param>
    /// <param name="bins">Number of histogram bins (default 30).</param>
    /// <param name="hue">Optional name of a string column to group by. One series per unique value.</param>
    /// <param name="palette">Optional color palette. Defaults to Tab10 when <see langword="null"/>.</param>
    /// <returns>A <see cref="FigureBuilder"/> ready for further configuration or rendering.</returns>
    public static FigureBuilder Hist(
        this MsDataFrame df,
        string           column,
        int              bins    = 30,
        string?          hue     = null,
        Color[]?         palette = null)
    {
        var vs = df.DoubleCol(column);

        if (hue is null)
            return vs.Hist(v => v, bins, palette: palette);

        var hs = df.StringCol(hue);
        return Enumerable.Range(0, vs.Length)
            .Select(i => new Vh(vs[i], hs[i]))
            .Hist(r => r.Value, bins, r => r.Hue, palette);
    }

    // ── Private row-carrier record structs ────────────────────────────────────

    private readonly record struct Xy(double X, double Y);
    private readonly record struct Xyh(double X, double Y, string Hue);
    private readonly record struct Vh(double Value, string Hue);
}
