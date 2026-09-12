// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Mcp;

/// <summary>Spec in, the chart's data out — as markdown, because that is the form a model reads without a parser
/// and the form it can quote back. The picture answers "what does this look like"; this answers "what are the
/// values", which is the question a model cannot put to an image.
/// <para>It is capped for the same reason SVG never travels inline: a ten-thousand-row table is a context window
/// spent on one chart. Past the ceiling the server refuses and names <c>save_chart</c>, so the model has
/// somewhere to go instead of a truncated answer it cannot tell from a complete one.</para></summary>
internal sealed class ChartTabulation
{
    private readonly ChartSpecReader _reader;
    private readonly RenderLimits _limits;

    /// <summary>Creates the tabulator over the reader that validates a spec and the limits it answers within.</summary>
    public ChartTabulation(ChartSpecReader reader, RenderLimits limits)
    {
        _reader = reader;
        _limits = limits;
    }

    /// <summary>The chart's data as markdown tables, one per group of series that share an x.</summary>
    /// <exception cref="ToolRefusalException">The spec is not one, or the table is past the row ceiling.</exception>
    public string Describe(ChartSpec spec)
    {
        Figure figure = _reader.Read(spec);
        var tables = figure.ToDataTables();
        if (tables.Count == 0)
        {
            return "This chart has no tabular form: nothing it draws is a series of values. "
                + "render_chart returns the picture, and describe_chart_schema says which chart types carry data.";
        }

        int rows = tables.Sum(table => table.RowCount);
        if (rows > _limits.MaxTableRows)
        {
            throw new ToolRefusalException(
                Invariant($"This chart has {rows} rows and the limit is {_limits.MaxTableRows}: a table that long ")
                + "costs more context than the chart is worth. Plot fewer points, or use save_chart and read the file.");
        }

        var text = new StringBuilder();
        foreach (var table in tables)
        {
            if (text.Length > 0)
            {
                text.AppendLine().AppendLine();
            }

            text.Append(table.ToMarkdown(_limits.MaxTableRows));
        }

        return text.ToString();
    }

    private static string Invariant(FormattableString text) => text.ToString(CultureInfo.InvariantCulture);
}
