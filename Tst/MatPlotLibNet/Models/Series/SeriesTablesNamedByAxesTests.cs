// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.TickFormatters;
using MatPlotLibNet.Rendering.TickLocators;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>A series whose rows stand on the axes in a way its table's columns cannot say as positions (a heatmap counts
/// its cells, a ridge stands on a baseline) tables itself with the names the axes shows where it stands. The names are
/// the axes' (<see cref="AxisNames"/>); the series only says where it stands.</summary>
public class SeriesTablesNamedByAxesTests
{
    /// <summary>An axes whose Y axis names three slots from the top down, the way a heatmap's rows and a ridgeline's groups
    /// are named.</summary>
    private static Axes WithYNames(params string[] names)
    {
        var axes = new Axes();
        axes.YAxis.TickLocator = new FixedLocator([.. Enumerable.Range(0, names.Length).Select(k => k + 0.5)]);
        axes.YAxis.TickFormatter = new CategoryFormatter(names, reversed: true);
        return axes;
    }

    private static ChartDataTable? TableOf(ITableNamedByAxes series, Axes axes) =>
        series.ToDataTable(AxisNames.Of(axes, (ISeries)series));

    private static string[] Headers(ChartDataTable table) => [.. table.Columns.Select(c => c.Header)];

    [Fact]
    public void AxisNames_OfAPrimarySeries_ReadBothAxes()
    {
        var axes = new Axes();
        var line = axes.AddSeries(new LineSeries([0.5], [0.5]));

        Assert.Equal(new AxisNames(axes, true, true), AxisNames.Of(axes, line));
    }

    [Fact]
    public void AxisNames_OfASeriesOnASecondaryAxis_ReadNothingAlongIt()
    {
        var axes = WithYNames("a");
        var right = axes.AddSecondarySeries(new LineSeries([0.5], [0.5]));
        var top = axes.PlotXSecondary([0.5], [0.5]);

        Assert.Null(AxisNames.Of(axes, right).Along(DataAxis.Y, [0.5]));
        Assert.Equal(["a"], AxisNames.Of(axes, top).Along(DataAxis.Y, [0.5])!);
        Assert.False(AxisNames.Of(axes, top).OnPrimaryX);
    }

    /// <summary>ROADMAP.md row 101: a series on the 2nd right axis, or on a left axis, is on neither the primary Y
    /// axis nor right slot 0 — <c>AxisNames.Of</c> must not fall back to "primary" just because it isn't in
    /// <c>SecondarySeries</c> (right slot 0 alone).</summary>
    [Fact]
    public void AxisNames_OfASeriesOnAFurtherOutwardAxis_ReadNothingAlongItEither()
    {
        var axes = WithYNames("a");
        var secondRight = axes.AddOutwardYAxis(AxisSide.Right).AddSeries(new LineSeries([0.5], [0.5]));
        var left = axes.AddOutwardYAxis(AxisSide.Left).AddSeries(new LineSeries([0.5], [0.5]));

        Assert.False(AxisNames.Of(axes, secondRight).OnPrimaryY);
        Assert.False(AxisNames.Of(axes, left).OnPrimaryY);
        Assert.Null(AxisNames.Of(axes, secondRight).Along(DataAxis.Y, [0.5]));
        Assert.Null(AxisNames.Of(axes, left).Along(DataAxis.Y, [0.5]));
    }

    [Fact]
    public void AxisNames_AlongNoAxis_NameNothing()
    {
        var axes = WithYNames("a");

        Assert.Null(AxisNames.Of(axes, axes.AddSeries(new LineSeries([0.5], [0.5]))).Along(DataAxis.None, [0.5]));
    }

    [Fact]
    public void AHeatmapRow_CoversTheSlotItIsDrawnIn_RowZeroOnTop()
    {
        var heatmap = new HeatmapSeries(new double[3, 2]);

        Assert.Equal(CategorySlot.Of(2), heatmap.RowSlot(0));
        Assert.Equal(0.5, heatmap.RowSlot(2).Centre);
        Assert.Equal(new CategorySlot(1, 1.5, 2), HeatmapSeries.ColumnSlot(1));
    }

    [Fact]
    public void AHeatmap_NamesTheRowsAndColumnsItsAxesName()
    {
        var axes = WithYNames("top", "middle", "bottom");
        axes.XAxis.TickLocator = new FixedLocator([0.5, 1.5]);
        axes.XAxis.TickFormatter = new CategoryFormatter(["left", "right"]);
        var heatmap = axes.Heatmap(new double[,] { { 1, 2 }, { 3, 4 }, { 5, 6 } });

        var table = TableOf(heatmap, axes)!;

        Assert.Equal([DataColumnKind.Text, DataColumnKind.Text, DataColumnKind.Number], table.Columns.Select(c => c.Kind));
        Assert.Equal(["top", "right", "2"], table.Rows[1].Select(cell => cell.Text ?? cell.Number.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        Assert.Equal(["bottom", "left"], table.Rows[4].Take(2).Select(cell => cell.Text));
    }

    [Fact]
    public void AHeatmapUnderAxesThatNameNothing_TablesAsItAlwaysDid()
    {
        var axes = new Axes();
        var heatmap = axes.Heatmap(new double[,] { { 1, 2 }, { 3, 4 } });

        var named = TableOf(heatmap, axes)!;
        var plain = heatmap.ToDataTable()!;

        Assert.Equal(plain.ToCsv(), named.ToCsv());
        Assert.Equal(plain.Columns, named.Columns);
    }

    [Fact]
    public void ARidge_IsTabledAsItsHeightAboveTheBaselineItsAxisNames()
    {
        var axes = WithYNames("first", "second");
        var ridge = axes.AddSeries(new AreaSeries([1, 2, 3], [1.5, 1.75, double.NaN]) { YData2 = [1.5, 1.5, 1.5] });

        var table = TableOf(ridge, axes)!;

        Assert.Equal(["x", "first"], Headers(table));
        Assert.Equal(DataAxis.None, table.Columns[1].Axis);
        Assert.Equal([0, 0.25, double.NaN], table.Rows.Select(row => row[1].Number));
    }

    [Fact]
    public void ARidgeWithALabel_IsTabledUnderItsLabel_AndAGapInItsBaseIsNoOtherLevel()
    {
        var axes = WithYNames("first", "second");
        var ridge = axes.AddSeries(new AreaSeries([1, 2], [0.75, 1]) { YData2 = [0.5, double.NaN], Label = "mine" });

        var table = TableOf(ridge, axes)!;

        Assert.Equal(["x", "mine"], Headers(table));
        Assert.Equal(0.25, table.Rows[0][1].Number);
        Assert.True(double.IsNaN(table.Rows[1][1].Number));
    }

    [Theory]
    [InlineData(false, 1.5, 1.5)]   // no second curve
    [InlineData(true, 1.5, 0.5)]    // a second curve of two levels
    [InlineData(true, 1.0, 1.0)]    // one level no name stands on
    public void AnyOtherArea_TablesAsItAlwaysDid(bool second, double bottom0, double bottom1)
    {
        var axes = WithYNames("first", "second");
        var area = axes.AddSeries(new AreaSeries([1, 2], [2, 2]) { YData2 = second ? [bottom0, bottom1] : null });

        Assert.Equal(area.ToDataTable()!.ToCsv(), TableOf(area, axes)!.ToCsv());
    }

    [Fact]
    public void AnAreaWhoseSecondCurveCannotBeDrawn_TablesAsItAlwaysDid()
    {
        var axes = WithYNames("first");
        var area = axes.AddSeries(new AreaSeries([1], [2]) { YData2 = [double.NaN] });

        Assert.Equal(area.ToDataTable()!.ToCsv(), TableOf(area, axes)!.ToCsv());
    }
}
