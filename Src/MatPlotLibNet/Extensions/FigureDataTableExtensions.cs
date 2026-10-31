// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.TickFormatters;

namespace MatPlotLibNet;

/// <summary>Turns a figure into the tables that say what it shows — the accessible alternative that carries the
/// information the picture carries, which alt text alone cannot (WCAG 1.1.1, and 1.3.1 for the header cells).
/// <para>Each series says its own data (<see cref="ISeries.ToDataTable"/>); this is the composition: series that
/// share an x become columns of ONE table, series that do not get a table each, and the AXES names the first
/// column, because a series only knows it is "x" and the axes knows it is "Quarter". The axes also names the
/// positions it shows names at: a mark on the centre of a named slot reads as that name (<see cref="KeyedByName"/>),
/// by the rule the picture writes the names with, so a chart built in one call tables its categories, not the
/// positions of their slots.</para>
/// <para>What the figure carries itself, outside every subplot, is part of what the picture shows and is tabled after the
/// subplots: the texts placed on it, one row for each, and its images, one row for each, which say what they are (a picture
/// of so many pixels standing at such a place) because their pixels are colours and not a series of values. What the picture
/// does not show is left out, as an invisible series is.</para></summary>
public static class FigureDataTableExtensions
{
    extension(Figure figure)
    {
        /// <summary>The tables that say what this figure shows, one group of x-sharing series at a
        /// time, subplot by subplot in draw order. Invisible series are left out — the table says what the picture
        /// shows — and a series with no tabular form contributes nothing.</summary>
        public IReadOnlyList<ChartDataTable> ToDataTables()
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

            var tables = UniteSingleRowRuns(candidates, name);
            tables.AddRange(TablesOfWhatTheFigureCarries(figure, name));
            return tables;
        }
    }

    private static readonly ChartDataColumn[] TextColumns = [new("text", DataColumnKind.Text)];

    private static readonly ChartDataColumn[] ImageColumns =
    [
        new("image", DataColumnKind.Text),
        new("width (pixels)"),
        new("height (pixels)"),
        new("from left (pixels)"),
        new("from bottom (pixels)"),
    ];

    /// <summary>The tables of what the figure itself carries, in the order the picture stacks it: the texts, then the images. A
    /// kind the figure carries none of (that shows) gives no table. A text with no place on the figure or with nothing but blanks
    /// in it, and an image with no place or no pixel, are not drawn, so they are not tabled; the images keep the numbers they
    /// were added under, so "image 3" is the third one added.</summary>
    private static IEnumerable<ChartDataTable> TablesOfWhatTheFigureCarries(Figure figure, string figureName)
    {
        var texts = figure.Texts
            .Where(text => text.IsShownOn(figure.Width, figure.Height))
            .Select(text => (IReadOnlyList<DataCell>)[DataCell.FromText(text.Text)])
            .ToList();
        if (texts.Count > 0)
        {
            yield return new ChartDataTable(Caption(figureName, null, "figure text"), TextColumns, texts);
        }

        var images = new List<IReadOnlyList<DataCell>>();
        for (int i = 0; i < figure.Images.Count; i++)
        {
            var image = figure.Images[i];
            if (!image.IsShownOn(figure.Height))
            {
                continue;
            }

            int rows = image.Data.GetLength(0), columns = image.Data.GetLength(1);

            images.Add([
                DataCell.FromText(string.Create(CultureInfo.InvariantCulture, $"image {i + 1}")),
                DataCell.FromNumber(columns),
                DataCell.FromNumber(rows),
                DataCell.FromNumber(image.XOffset),
                DataCell.FromNumber(image.YOffset),
            ]);
        }

        if (images.Count > 0)
        {
            yield return new ChartDataTable(Caption(figureName, null, "figure images"), ImageColumns, images);
        }
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

    extension(Axes axes)
    {
        /// <summary>The tables for ONE subplot. <paramref name="figureName"/> is the figure's accessible name, which
        /// the caption is built from; pass an empty string for a table that stands alone.</summary>
        public IReadOnlyList<ChartDataTable> ToDataTables(string figureName = "")
        {
            ArgumentNullException.ThrowIfNull(axes);
            var fragments = new List<SeriesFragment>();
            foreach (var series in axes.AllSeries)
            {
                if (series.Visible && axes.TableOf(series) is { } table)
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

        /// <summary>The table of one series as a reader reads it: the series says its own data, placed as it stands on the
        /// axes (<see cref="ITableNamedByAxes"/>, for a series whose columns cannot say that as positions), and the column of
        /// positions the axes names is its key, read as those names (<see cref="KeyedByName"/>).</summary>
        /// <param name="series">The series.</param>
        /// <returns>The table, or <see langword="null"/> for a series with no tabular form.</returns>
        private ChartDataTable? TableOf(ISeries series)
        {
            var names = AxisNames.Of(axes, series);
            var table = series is ITableNamedByAxes placed ? placed.ToDataTable(names) : series.ToDataTable();
            return table?.KeyedByName(axes, names, series.Label);
        }
    }

    /// <summary>A fragment: the series and the table it gave.</summary>
    private readonly record struct SeriesFragment(ISeries Series, ChartDataTable Table);

    extension(ChartDataTable table)
    {
        /// <summary>The table keyed by the names its positions stand on. A mark on the centre of a named slot (a lollipop's
        /// stem, a slope graph's end, a Pareto chart's point on its bar) has no position a reader needs beside its name: the
        /// position IS the name, so its column reads as the names and becomes the table's key, under the axis' title, or
        /// "category", the word a bar chart's table uses. The x column is the key when the X axis names it; otherwise a y
        /// column the Y axis names becomes the key and moves in front (the dots of a dot plot, whose categories run down the
        /// Y axis), and the x values it leaves take the series' label, which names the series' values whichever axis they run
        /// along. A column the axis does not name in full keeps its numbers, and so does every other column.</summary>
        /// <param name="axes">The axes whose axis titles head a column of names.</param>
        /// <param name="names">The names along the axes the series stands on.</param>
        /// <param name="label">The series' label.</param>
        /// <returns>The table keyed by names, or this table when no position of its key stands on a name.</returns>
        private ChartDataTable KeyedByName(Axes axes, AxisNames names, string? label)
        {
            var columns = table.Columns;
            if (NamesIn(table, 0, names) is { } alongFirst)
            {
                return Keyed(table, 0, alongFirst, NamedHeader(axes, columns[0].Axis), columns[0].Header);
            }

            if (columns.Count > 1 && columns[0].Axis == DataAxis.X && columns[1].Axis == DataAxis.Y
                && NamesIn(table, 1, names) is { } across)
            {
                return Keyed(table, 1, across, NamedHeader(axes, DataAxis.Y), label is { Length: > 0 } ? label : columns[0].Header);
            }

            return table;
        }
    }

    /// <summary>The names the axis of column <paramref name="column"/> shows at its positions, or <see langword="null"/>
    /// when the column is no position or its axis does not name every position of it that can be placed.</summary>
    private static string?[]? NamesIn(ChartDataTable table, int column, AxisNames names) =>
        names.Along(table.Columns[column].Axis, [.. table.Rows.Select(row => row[column].Number)]);

    /// <summary>A column of names takes the title of the axis it runs along, or "category" when that axis has none.</summary>
    private static string NamedHeader(Axes axes, DataAxis axis) =>
        (axis == DataAxis.X ? axes.XAxis : axes.YAxis).Label is { Length: > 0 } title ? title : "category";

    /// <summary>The table with column <paramref name="key"/> read as <paramref name="names"/> under
    /// <paramref name="header"/> and moved in front, the other columns after it in their order; the first column, when it
    /// is not the key, under <paramref name="firstHeader"/>. A position with no name (the gap between two runs of a line)
    /// is an empty cell.</summary>
    private static ChartDataTable Keyed(ChartDataTable table, int key, string?[] names, string header, string firstHeader)
    {
        int[] order = [key, .. Enumerable.Range(0, table.Columns.Count).Where(c => c != key)];
        var columns = order.Select(c =>
            c == key ? new ChartDataColumn(header, DataColumnKind.Text, table.Columns[c].Axis)
            : c == 0 ? new ChartDataColumn(firstHeader, table.Columns[0].Kind, table.Columns[0].Axis)
            : table.Columns[c]).ToList();
        var rows = new IReadOnlyList<DataCell>[table.RowCount];
        for (int r = 0; r < rows.Length; r++)
        {
            var row = table.Rows[r];
            rows[r] = [.. order.Select(c => c != key ? row[c] : names[r] is { } name ? DataCell.FromText(name) : DataCell.Empty)];
        }

        return new ChartDataTable(table.Caption, columns, rows);
    }

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

    /// <summary>One column of a fragment, on its way into a merged table: the table it stands in and its place there.</summary>
    private readonly record struct MergedColumn(ChartDataTable Table, int Index);

    /// <summary>One table from a run of fragments: the shared first column under the axis' own name, then every
    /// fragment's remaining columns. Every column that is a POSITION is resolved against the axis it is a
    /// position on — that is the whole reason a series marks them. A column that is the same as one already in the table, in
    /// its header and in every cell (the edges the cells of a Marimekko chart's segments all stand between), is one column of
    /// it and not repeated: a table says each thing once.</summary>
    private static ChartDataTable Merge(List<SeriesFragment> group, Axes axes, string? caption)
    {
        var seed = group[0].Table;
        var columns = new List<ChartDataColumn> { Resolve(seed.Columns[0], axes) };
        var sources = new List<MergedColumn>();
        foreach (var fragment in group)
        {
            for (int c = 1; c < fragment.Table.Columns.Count; c++)
            {
                var source = new MergedColumn(fragment.Table, c);
                if (sources.Any(kept => SameColumn(kept, source))) continue;
                sources.Add(source);
                columns.Add(Resolve(fragment.Table.Columns[c], axes));
            }
        }

        var rows = new List<IReadOnlyList<DataCell>>(seed.RowCount);
        for (int r = 0; r < seed.RowCount; r++)
        {
            var cells = new List<DataCell>(columns.Count) { seed.Rows[r][0] };
            cells.AddRange(sources.Select(source => source.Table.Rows[r][source.Index]));
            rows.Add(cells);
        }

        return new ChartDataTable(caption, columns, rows);
    }

    /// <summary>Whether two columns are the same: the same column (header, kind and axis) with the same cell in every row.</summary>
    private static bool SameColumn(MergedColumn left, MergedColumn right)
    {
        if (left.Table.Columns[left.Index] != right.Table.Columns[right.Index]) return false;
        for (int r = 0; r < left.Table.RowCount; r++)
        {
            if (!left.Table.Rows[r][left.Index].Equals(right.Table.Rows[r][right.Index])) return false;
        }

        return true;
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
