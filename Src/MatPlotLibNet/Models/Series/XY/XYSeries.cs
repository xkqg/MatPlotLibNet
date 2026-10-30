// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models.Series;

/// <summary>Base class for series plotted on Cartesian X/Y coordinates.</summary>
public abstract class XYSeries : ChartSeries, IPriceSeries
{
    /// <summary>The x coordinate of each point, in data units.</summary>
    public virtual double[] XData { get; }

    /// <summary>The y coordinate of each point, in data units; one value per <see cref="XData"/> entry.</summary>
    public double[] YData { get; }

    /// <inheritdoc />
    public double[] PriceData => YData;

    /// <summary>The most points drawn for this series. When the series holds more, it is thinned to this many points
    /// before drawing while keeping its overall shape; null (the default) draws every point.</summary>
    public int? MaxDisplayPoints { get; set; }

    private string[]? _rowKeys;

    /// <summary>The author's own identity for each point, one key for each: the key of the row of data the point was drawn from.
    /// The keys name the rows of the table, travel in the JSON, and are written as <c>data-row</c> on every mark that is one row
    /// (the markers of a scatter, the circles of a bubble chart), so a script can light the same row on another chart of the same
    /// data. <see langword="null"/> (the default) names none.</summary>
    /// <exception cref="ArgumentException">There are not as many keys as points, or a key is blank or given twice; the message
    /// says which, and the keys the series had are kept.</exception>
    public string[]? RowKeys
    {
        get => _rowKeys;
        set
        {
            if (value is not null) RefuseKeysThatDoNotNameTheRows(value);
            _rowKeys = value;
        }
    }

    private void RefuseKeysThatDoNotNameTheRows(string[] keys)
    {
        if (keys.Length != XData.Length)
            throw new ArgumentException($"RowKeys has {keys.Length} keys for {XData.Length} points; give one key to each point.", nameof(RowKeys));

        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < keys.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(keys[i]))
                throw new ArgumentException($"RowKeys[{i}] is blank; a key is the name of its row.", nameof(RowKeys));
            if (!seen.Add(keys[i]))
                throw new ArgumentException($"RowKeys has the key \"{keys[i]}\" more than once; a key names one row.", nameof(RowKeys));
        }
    }

    /// <summary>Initializes with X and Y data arrays.</summary>
    protected XYSeries(double[] xData, double[] yData) { XData = xData; YData = yData; }

    /// <inheritdoc />
    /// <remarks>Two columns: the x values, and the y values under the series' own label; the keys of the rows, when it has them, come first.</remarks>
    public override ChartDataTable? ToDataTable() =>
        this.WithRowKeys(ChartDataTable.FromNumberColumns(
            [new("x", Axis: DataAxis.X), new(Label ?? "y", Axis: DataAxis.Y)],
            [XData, YData]));

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
    {
        // Empty series contributes no data range.
        if (XData.Length == 0 || YData.Length == 0)
            return new DataRangeContribution(null, null, null, null);
        // Over the values the axis can place: a missing value (NaN) and an infinity are left out, so one gap cannot
        // collapse the range; on a LOG axis a 0 or a negative is masked out too, so one window that priced 0 cannot
        // drive the floor negative and blank the whole panel. A null context (a series ranged outside any axes) reads
        // as linear.
        var x = XData.DrawableRange(context?.XScale ?? AxisScale.Linear);
        var y = YData.DrawableRange(context?.YScale ?? AxisScale.Linear);
        return new(x?.Lo, x?.Hi, y?.Lo, y?.Hi);
    }
}

/// <summary>Puts the keys of the rows into the table of a series that has them.</summary>
internal static class XYSeriesRowKeyExtensions
{
    extension(XYSeries series)
    {
        /// <summary>The table with the keys of the rows as its first column, "row", a text for each row; the table as it is when the
        /// series has no keys.</summary>
        /// <param name="table">The series' own table.</param>
        internal ChartDataTable WithRowKeys(ChartDataTable table)
        {
            if (series.RowKeys is not { } keys) return table;

            var rows = new IReadOnlyList<DataCell>[table.RowCount];
            for (int row = 0; row < rows.Length; row++)
                rows[row] = [DataCell.FromText(keys[row]), .. table.Rows[row]];

            return new ChartDataTable(table.Caption, [new("row", DataColumnKind.Text), .. table.Columns], rows);
        }
    }
}
