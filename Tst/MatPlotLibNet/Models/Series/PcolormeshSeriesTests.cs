// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Indicators;
using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models.Streaming;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Rendering.TickFormatters;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;
using MatPlotLibNet;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="PcolormeshSeries"/> behaviour; what it starts from and the settings it stores are held by <see cref="Serialization.RestingValueCorpusTests"/> and the drawn-property round trips.</summary>
public class PcolormeshSeriesTests
{
    private static readonly double[] X = [0.0, 1.0, 2.0]; // M+1=3, M=2 cols
    private static readonly double[] Y = [0.0, 1.0, 2.0]; // N+1=3, N=2 rows
    private static readonly double[,] C = { { 1.0, 2.0 }, { 3.0, 4.0 } };

    [Fact]
    public void GetColorBarRange_ReturnsMinMax()
    {
        var series = new PcolormeshSeries(X, Y, C);
        var (min, max) = series.GetColorBarRange();
        Assert.Equal(1.0, min);
        Assert.Equal(4.0, max);
    }

    /// <summary>An infinity is no value a colour scale can place: the cells leave it out of the range they are coloured over, and
    /// so does the bar. matplotlib 3.11.2 (python): <c>pcolormesh([[1, inf], [3, 4]])</c> with a colour bar autoscales its norm to
    /// (1.0, 4.0), and the bar reads (1.0, 4.0).</summary>
    [Fact]
    public void TheColourBar_LeavesAnInfinityOut_AsTheCellsDo()
    {
        var series = new PcolormeshSeries(X, Y, new double[,] { { 1.0, double.PositiveInfinity }, { 3.0, 4.0 } });

        Assert.Equal(new MatPlotLibNet.Numerics.MinMaxRange(1.0, 4.0), series.GetColorBarRange());
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

    /// <summary>PcolormeshSeries.GetColorBarRange when all C values are equal: a range of no width, opened one wide from the value
    /// under the library's own themes.</summary>
    [Fact]
    public void PcolormeshSeries_AllEqualC_IsOpenedOneWide()
    {
        var s = new PcolormeshSeries(new double[] { 0.0, 1.0, 2.0 }, new double[] { 0.0, 1.0, 2.0 },
            new double[,] { { 5, 5 }, { 5, 5 } });
        var (min, max) = s.GetColorBarRange();
        Assert.Equal(5.0, min);
        Assert.Equal(6.0, max);
    }

    /// <summary>PcolormeshSeries.ComputeDataRange: `X.Length == 0 || Y.Length == 0`
    /// short-circuit, second arm — empty Y with populated X.</summary>
    [Fact]
    public void PcolormeshSeries_EmptyY_ReturnsSentinelRange()
    {
        var s = new PcolormeshSeries(new double[] { 1.0, 2.0 }, Array.Empty<double>(), new double[2, 0]);
        var range = s.ComputeDataRange(new TestAxesContext());
        Assert.Equal(0, range.XMin);
        Assert.Equal(1, range.XMax);
    }
}
