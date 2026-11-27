// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Data;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;
using MsDataFrame = Microsoft.Data.Analysis.DataFrame;

namespace MatPlotLibNet;

/// <summary>
/// Extension methods that turn a <see cref="MsDataFrame"/> directly into a <see cref="FigureBuilder"/>,
/// using column names for X, Y, and an optional hue (group-by) column.
/// Materialises the named columns to arrays, then hands them to the library's one split of observations by a hue, which the
/// figures of <see cref="EnumerableFigureExtensions"/> and the faceted figures draw through too.
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
///         palette: [Colors.Blue, Colors.Orange, Colors.Green])
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
    /// <param name="df">The source data frame.</param>
    extension(MsDataFrame df)
    {
        /// <summary>Plots two numeric columns as a line series, with optional hue grouping.</summary>
        /// <param name="x">Name of the numeric X column.</param>
        /// <param name="y">Name of the numeric Y column.</param>
        /// <param name="hue">Optional name of a string column to group by. One series per unique value.</param>
        /// <param name="palette">Optional color palette. Defaults to Tab10 when <see langword="null"/>.</param>
        /// <returns>A <see cref="FigureBuilder"/> ready for further configuration or rendering.</returns>
        public FigureBuilder Line(
            string           x,
            string           y,
            string?          hue     = null,
            Color[]?         palette = null) =>
            new HueSplit(df.Values(x), df.Values(y), df.HueCol(hue), palette).Lines();

        /// <summary>Plots two numeric columns as a scatter series, with optional hue grouping.</summary>
        /// <param name="x">Name of the numeric X column.</param>
        /// <param name="y">Name of the numeric Y column.</param>
        /// <param name="hue">Optional name of a string column to group by. One series per unique value.</param>
        /// <param name="palette">Optional color palette. Defaults to Tab10 when <see langword="null"/>.</param>
        /// <returns>A <see cref="FigureBuilder"/> ready for further configuration or rendering.</returns>
        public FigureBuilder Scatter(
            string           x,
            string           y,
            string?          hue     = null,
            Color[]?         palette = null) =>
            new HueSplit(df.Values(x), df.Values(y), df.HueCol(hue), palette).Scatters();

        /// <summary>Builds a <see cref="PairGridSeries"/> from the named numeric columns of
        /// <paramref name="df"/>. When <paramref name="hue"/> is provided, the named string column
        /// is converted into <see cref="PairGridSeries.HueGroups"/> integer IDs and
        /// <see cref="PairGridSeries.HueLabels"/> human-readable category names so the figure-level
        /// legend renders the original strings instead of opaque integers.</summary>
        /// <remarks>
        /// <para><b>Hue groups on the series.</b> This extension carries <c>HueGroups</c> on the
        /// <see cref="PairGridSeries"/> model itself, because a pair grid resolves a colour per group in each cell when it is drawn;
        /// <see cref="Line"/>/<see cref="Scatter"/>/<see cref="Hist"/> draw one series per group instead. Both number the groups
        /// in the order their labels are first seen.</para>
        /// <para><b>Empty-column validation.</b> Throws on an empty <paramref name="columns"/> array
        /// rather than propagating a cryptic downstream <see cref="IndexOutOfRangeException"/> from
        /// the row-count probe. The sibling extensions (Line/Scatter/Hist) propagate the underlying
        /// <see cref="System.Collections.Generic.KeyNotFoundException"/> from column lookup; the
        /// hardened upfront validation here is intentional.</para>
        /// </remarks>
        /// <param name="columns">Names of the numeric columns to include, in order. Each column
        /// becomes one variable in the N×N pair grid.</param>
        /// <param name="hue">Optional name of a categorical (string) column for hue grouping. The groups are numbered in the order
        /// their labels are first seen (<see cref="HueGrouper"/>), as the library's pair plot and seaborn's number them.</param>
        /// <param name="palette">Optional palette to assign per-group colours. When <see langword="null"/>
        /// the renderer falls back to <c>QualitativeColorMaps.Tab10</c>.</param>
        /// <param name="configure">Optional callback to override defaults after the series is built.</param>
        /// <returns>A <see cref="FigureBuilder"/> ready for further configuration or rendering.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="columns"/> is empty or
        /// any named column is missing from <paramref name="df"/>.</exception>
        public FigureBuilder PairGrid(
            string[]                    columns,
            string?                     hue       = null,
            Color[]?                    palette   = null,
            Action<PairGridSeries>?     configure = null)
        {
            columns.ThrowIfEmpty("At least one column must be specified.");

            var variables = new double[columns.Length][];
            for (int c = 0; c < columns.Length; c++)
                variables[c] = df.Values(columns[c]);

            return Plt.Create().AddSubPlot(1, 1, 1, ax => ax.PairGrid(variables, s =>
            {
                s.Labels = columns;

                if (hue is not null)
                {
                    var codes = HueGrouper.Codes(df.StringCol(hue));
                    s.HueGroups = codes.Groups;
                    s.HueLabels = codes.Labels;
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
        /// <param name="edgeFromCol">Name of the string column carrying the source-node ID per row.</param>
        /// <param name="edgeToCol">Name of the string column carrying the target-node ID per row.</param>
        /// <param name="weightCol">Optional name of a numeric column with per-edge weights. When null all weights default to 1.0.</param>
        /// <param name="directedCol">Optional name of a boolean column setting per-edge direction. When null all edges default to undirected.</param>
        /// <param name="configure">Optional callback to override defaults after the series is built.</param>
        /// <returns>A <see cref="FigureBuilder"/> ready for further configuration or rendering.</returns>
        /// <exception cref="ArgumentException">Thrown when any named column is missing from <paramref name="df"/>.</exception>
        public FigureBuilder NetworkGraph(
            string                          edgeFromCol,
            string                          edgeToCol,
            string?                         weightCol   = null,
            string?                         directedCol = null,
            Action<NetworkGraphSeries>?     configure   = null)
        {
            var fromValues = df.StringCol(edgeFromCol);
            var toValues   = df.StringCol(edgeToCol);
            int rows = Math.Min(fromValues.Length, toValues.Length);

            double[]? weights  = weightCol   is not null ? df.Values(weightCol) : null;
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
        /// <param name="columns">Names of the numeric columns to include, in order.</param>
        /// <param name="configure">Optional callback to set <see cref="ClustermapSeries"/> properties
        /// such as <see cref="ClustermapSeries.RowTree"/>, <see cref="ClustermapSeries.ColorMap"/>,
        /// or <see cref="ClustermapSeries.ShowLabels"/>.</param>
        /// <returns>A <see cref="FigureBuilder"/> ready for further configuration or rendering.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="columns"/> is empty or
        /// contains a column name that does not exist in <paramref name="df"/>.</exception>
        public FigureBuilder Clustermap(
            string[]                    columns,
            Action<ClustermapSeries>?   configure = null)
        {
            columns.ThrowIfEmpty("At least one column must be specified.");

            var cols = columns.Select(name => df.Values(name)).ToArray();
            int rows = cols[0].Length;
            var data = new double[rows, columns.Length];
            for (int c = 0; c < columns.Length; c++)
                for (int r = 0; r < rows; r++)
                    data[r, c] = cols[c][r];

            return Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Clustermap(data, configure));
        }

        /// <summary>Plots a numeric column as a histogram, with optional hue grouping.</summary>
        /// <param name="column">Name of the numeric column.</param>
        /// <param name="bins">Number of histogram bins (default <see cref="HistogramSeries.RestingBins"/>).</param>
        /// <param name="hue">Optional name of a string column to group by. One series per unique value.</param>
        /// <param name="palette">Optional color palette. Defaults to Tab10 when <see langword="null"/>.</param>
        /// <returns>A <see cref="FigureBuilder"/> ready for further configuration or rendering.</returns>
        public FigureBuilder Hist(
            string           column,
            int              bins    = HistogramSeries.RestingBins,
            string?          hue     = null,
            Color[]?         palette = null)
        {
            double[] values = df.Values(column);
            return new HueSplit(values, values, df.HueCol(hue), palette).Histograms(bins);
        }
    }
}
