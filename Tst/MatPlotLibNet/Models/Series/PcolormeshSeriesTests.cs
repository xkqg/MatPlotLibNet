// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="PcolormeshSeries"/> default properties, construction, and serialization.</summary>
public class PcolormeshSeriesTests
{
    private static readonly double[] X = [0.0, 1.0, 2.0]; // M+1=3, M=2 cols
    private static readonly double[] Y = [0.0, 1.0, 2.0]; // N+1=3, N=2 rows
    private static readonly double[,] C = { { 1.0, 2.0 }, { 3.0, 4.0 } };

    [Fact]
    public void Constructor_StoresXYC()
    {
        var series = new PcolormeshSeries(X, Y, C);
        Assert.Equal((double[])series.X, X);
        Assert.Equal((double[])series.Y, Y);
        Assert.Equal(C, series.C);
    }

    [Fact]
    public void Normalizer_DefaultsToNull()
    {
        var series = new PcolormeshSeries(X, Y, C);
        Assert.Null(series.Normalizer);
    }

    [Fact]
    public void GetColorBarRange_ReturnsMinMax()
    {
        var series = new PcolormeshSeries(X, Y, C);
        var (min, max) = series.GetColorBarRange();
        Assert.Equal(1.0, min);
        Assert.Equal(4.0, max);
    }

    [Fact]
    public void ToSeriesDto_ReturnsTypePcolormesh()
    {
        var series = new PcolormeshSeries(X, Y, C);
        Assert.Equal("pcolormesh", series.ToSeriesDto().Type);
    }

    // ── the table a mesh hands back ────────────────────────────────────────────

    [Fact]
    public void TheTable_NamesEachCellByTheCoordinatesTheCallerGave()
    {
        var series = new PcolormeshSeries(new[] { 10.0, 20.0, 30.0 }, new[] { 100.0, 200.0 }, new double[,] { { 1.0, 2.0 } });

        var table = series.ToDataTable()!;

        Assert.Equal(["x", "y", "value"], table.Columns.Select(c => c.Header));
        Assert.Equal([DataAxis.X, DataAxis.Y, DataAxis.None], table.Columns.Select(c => c.Axis));
        Assert.Equal(10.0, table.Rows[0][0].Number);
        Assert.Equal(100.0, table.Rows[0][1].Number);
        Assert.Equal(1.0, table.Rows[0][2].Number);
        Assert.Equal(20.0, table.Rows[1][0].Number);
    }

    [Fact]
    public void TheTable_TakesTheSeriesLabelForItsValueColumn()
    {
        var series = new PcolormeshSeries(new[] { 0.0, 1.0 }, new[] { 0.0, 1.0 }, new double[,] { { 7.0 } }) { Label = "requests" };

        Assert.Equal("requests", series.ToDataTable()!.Columns[2].Header);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void AMeshWithNoEdgesOnEitherSide_HasNoTable(bool emptyX, bool emptyY)
    {
        // Without edges there are no coordinates to name a cell by, and the renderer draws nothing either.
        var series = new PcolormeshSeries(
            emptyX ? Array.Empty<double>() : new[] { 0.0, 1.0 },
            emptyY ? Array.Empty<double>() : new[] { 0.0, 1.0 },
            new double[,] { { 7.0 } });

        Assert.Null(series.ToDataTable());
    }
}
