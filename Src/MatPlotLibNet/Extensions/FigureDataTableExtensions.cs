// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.TickFormatters;

namespace MatPlotLibNet;

/// <summary>Turns a figure into the tables that say what it shows — the accessible alternative that carries the
/// information the picture carries, which alt text alone cannot (WCAG 1.1.1, and 1.3.1 for the header cells).
/// <para>Each series says its own data (<see cref="ISeries.ToDataTable"/>); this is the composition: series that
/// share an x become columns of ONE table, series that do not get a table each, and the AXES names the first
/// column, because a series only knows it is "x" and the axes knows it is "Quarter".</para></summary>
public static class FigureDataTableExtensions
{
    /// <summary>The tables that say what <paramref name="figure"/> shows, one group of x-sharing series at a
    /// time, subplot by subplot in draw order. Invisible series are left out — the table says what the picture
    /// shows — and a series with no tabular form contributes nothing.</summary>
    public static IReadOnlyList<ChartDataTable> ToDataTables(this Figure figure)
    {
        ArgumentNullException.ThrowIfNull(figure);
        string name = figure.AccessibleName();
        var tables = new List<ChartDataTable>();
        foreach (var axes in figure.SubPlots)
        {
            tables.AddRange(axes.ToDataTables(name));
        }

        return tables;
    }

    /// <summary>The tables for ONE subplot. <paramref name="figureName"/> is the figure's accessible name, which
    /// the caption is built from; pass an empty string for a table that stands alone.</summary>
    public static IReadOnlyList<ChartDataTable> ToDataTables(this Axes axes, string figureName = "")
    {
        ArgumentNullException.ThrowIfNull(axes);
        var fragments = new List<SeriesFragment>();
        foreach (var series in axes.AllSeries)
        {
            if (series.Visible && series.ToDataTable() is { } table)
            {
                fragments.Add(new SeriesFragment(series, table));
            }
        }

        if (fragments.Count == 0)
        {
            return [];
        }

        var groups = GroupBySharedFirstColumn(fragments);
        var tables = new List<ChartDataTable>(groups.Count);
        foreach (var group in groups)
        {
            // A caption names ONE table. When a subplot yields several, each says which series it is; two
            // anonymous grids under one heading are two things a reader cannot tell apart.
            string? suffix = groups.Count > 1 ? group[0].Series.Label : null;
            tables.Add(Merge(group, axes, Caption(figureName, axes.Title, suffix)));
        }

        return tables;
    }

    /// <summary>A fragment: the series and the table it gave.</summary>
    private readonly record struct SeriesFragment(ISeries Series, ChartDataTable Table);

    /// <summary>Runs of fragments whose first columns hold the same cells — those become columns of one table.
    /// The grouping is a RUN, not a bucket: it keeps draw order, which is the order a legend reads in.</summary>
    private static List<List<SeriesFragment>> GroupBySharedFirstColumn(List<SeriesFragment> fragments)
    {
        var groups = new List<List<SeriesFragment>>();
        groups.Add([fragments[0]]);
        for (int i = 1; i < fragments.Count; i++)
        {
            var current = groups[^1];
            if (SharesFirstColumn(current[0].Table, fragments[i].Table))
            {
                current.Add(fragments[i]);
            }
            else
            {
                groups.Add([fragments[i]]);
            }
        }

        return groups;
    }

    /// <summary>Whether two fragments are read along the same first column — by VALUE, so two series built from
    /// separately-allocated but identical x arrays still share one table.</summary>
    private static bool SharesFirstColumn(ChartDataTable left, ChartDataTable right)
    {
        if (left.Columns[0].Kind != right.Columns[0].Kind || left.RowCount != right.RowCount)
        {
            return false;
        }

        for (int r = 0; r < left.RowCount; r++)
        {
            if (!left.Rows[r][0].Equals(right.Rows[r][0]))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>One table from a run of fragments: the shared first column under the axis' own name, then every
    /// fragment's remaining columns.</summary>
    private static ChartDataTable Merge(List<SeriesFragment> group, Axes axes, string? caption)
    {
        var seed = group[0].Table;
        var columns = new List<ChartDataColumn> { FirstColumn(seed, axes) };
        foreach (var fragment in group)
        {
            for (int c = 1; c < fragment.Table.Columns.Count; c++)
            {
                columns.Add(fragment.Table.Columns[c]);
            }
        }

        var rows = new List<IReadOnlyList<DataCell>>(seed.RowCount);
        for (int r = 0; r < seed.RowCount; r++)
        {
            var cells = new List<DataCell>(columns.Count) { seed.Rows[r][0] };
            foreach (var fragment in group)
            {
                for (int c = 1; c < fragment.Table.Columns.Count; c++)
                {
                    cells.Add(fragment.Table.Rows[r][c]);
                }
            }

            rows.Add(cells);
        }

        return new ChartDataTable(caption, columns, rows);
    }

    /// <summary>The first column, named and typed by the AXES: its label where it has one, and a date kind when
    /// the axis reads as dates — either because its scale says so, or because its tick formatter does. The two
    /// are set by different builder calls, and a chart that looks dated must table as dated.</summary>
    private static ChartDataColumn FirstColumn(ChartDataTable seed, Axes axes)
    {
        var column = seed.Columns[0];
        string header = column.Header == "x" && axes.XAxis.Label is { Length: > 0 } label ? label : column.Header;
        var kind = column.Kind == DataColumnKind.Number && ReadsAsDates(axes.XAxis) ? DataColumnKind.Date : column.Kind;
        return new ChartDataColumn(header, kind);
    }

    /// <summary>Whether an axis reads as dates: <see cref="AxisScale.Date"/>, or a date tick formatter set on its
    /// own (<c>SetXTickFormatter</c> sets the formatter and not the scale).</summary>
    private static bool ReadsAsDates(Axis axis) =>
        axis.Scale == AxisScale.Date || axis.TickFormatter is DateTickFormatter or AutoDateFormatter;

    /// <summary>The caption: the figure's name, the subplot's title, both joined, or nothing.</summary>
    private static string? Caption(string figureName, string? axesTitle, string? seriesLabel)
    {
        var parts = new List<string>(3);
        if (figureName.Length > 0)
        {
            parts.Add(figureName);
        }

        if (axesTitle is { Length: > 0 })
        {
            parts.Add(axesTitle);
        }

        if (seriesLabel is { Length: > 0 })
        {
            parts.Add(seriesLabel);
        }

        return parts.Count == 0 ? null : string.Join(" — ", parts);
    }
}
