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
        var candidates = new List<(ChartDataTable Table, bool Unitable)>();
        foreach (var axes in figure.SubPlots)
        {
            // A subplot that names itself has a name to lose, so its table is never swallowed into a row.
            bool unitable = axes.Title is not { Length: > 0 };
            foreach (var table in axes.ToDataTables(name))
            {
                candidates.Add((table, unitable));
            }
        }

        return UniteSingleRowRuns(candidates, name);
    }

    /// <summary>Runs of neighbouring tables that are the SAME single row said about different things become one
    /// table with a row each. A KPI tile row is a row of subplots — that is layout, not data — and a reader
    /// handed five one-row grids has to do the joining the picture already did for everyone else.
    /// <para>The test is deliberately narrow: identical headers, exactly one row, and a first column of text
    /// that names the row. Anything with an x of its own keeps its own table.</para></summary>
    private static List<ChartDataTable> UniteSingleRowRuns(
        List<(ChartDataTable Table, bool Unitable)> candidates, string figureName)
    {
        var united = new List<ChartDataTable>(candidates.Count);
        for (int i = 0; i < candidates.Count;)
        {
            int j = i;
            while (candidates[i].Unitable && j + 1 < candidates.Count && candidates[j + 1].Unitable
                   && IsSameSingleRowShape(candidates[i].Table, candidates[j + 1].Table))
            {
                j++;
            }

            if (j == i)
            {
                united.Add(candidates[i].Table);
                i++;
                continue;
            }

            var rows = new List<IReadOnlyList<DataCell>>(j - i + 1);
            for (int k = i; k <= j; k++)
            {
                rows.Add(candidates[k].Table.Rows[0]);
            }

            // The row names itself in its first cell, so the united table takes the figure's name and drops the
            // per-table suffix that told the single rows apart.
            united.Add(new ChartDataTable(figureName.Length > 0 ? figureName : null, candidates[i].Table.Columns, rows));
            i = j + 1;
        }

        return united;
    }

    /// <summary>Whether two tables are one row of the same shape: the same headers and kinds, one row each, and
    /// a leading text column — the row's own name.</summary>
    private static bool IsSameSingleRowShape(ChartDataTable left, ChartDataTable right) =>
        left.RowCount == 1 && right.RowCount == 1
        && left.Columns[0].Kind == DataColumnKind.Text
        && left.Columns.Count == right.Columns.Count
        && left.Columns.SequenceEqual(right.Columns);

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
            // A caption names ONE table. Several tables from one subplot MUST each say which series they are —
            // two anonymous grids under one heading are two things a reader cannot tell apart. A lone table says
            // it too, unless a column header already carries the word, which is the common case for an x/y pair.
            string? label = group[0].Series.Label;
            bool alreadySaid = groups.Count == 1 && label is { Length: > 0 }
                && group.Any(f => f.Table.Columns.Any(c => c.Header == label));
            string? suffix = alreadySaid ? null : label;
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
    /// fragment's remaining columns. Every column that is a POSITION is resolved against the axis it is a
    /// position on — that is the whole reason a series marks them.</summary>
    private static ChartDataTable Merge(List<SeriesFragment> group, Axes axes, string? caption)
    {
        var seed = group[0].Table;
        var columns = new List<ChartDataColumn> { Resolve(seed.Columns[0], axes) };
        foreach (var fragment in group)
        {
            for (int c = 1; c < fragment.Table.Columns.Count; c++)
            {
                columns.Add(Resolve(fragment.Table.Columns[c], axes));
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

    /// <summary>A column, named and typed by the AXIS it is a position on. A series knows its <c>start</c> and
    /// <c>end</c> are x coordinates; only the axes knows x reads as dates — so a positional column becomes a
    /// date column here, and an anonymous "x" takes the axis' own label. A column that is not a position
    /// (a count, a size, a category) is returned untouched: it is nobody's coordinate.
    /// <para>Only the bare <c>x</c> is renamed. A y column already carries the SERIES' label, which is the name
    /// the legend gives it and the only thing that tells two series in one table apart.</para></summary>
    private static ChartDataColumn Resolve(ChartDataColumn column, Axes axes)
    {
        var axis = column.Axis switch
        {
            DataAxis.X => axes.XAxis,
            DataAxis.Y => axes.YAxis,
            _ => null,
        };
        if (axis is null)
        {
            return column;
        }

        string header = column.Header == "x" && axis.Label is { Length: > 0 } label ? label : column.Header;
        var kind = column.Kind == DataColumnKind.Number && ReadsAsDates(axis) ? DataColumnKind.Date : column.Kind;
        return new ChartDataColumn(header, kind, column.Axis);
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
