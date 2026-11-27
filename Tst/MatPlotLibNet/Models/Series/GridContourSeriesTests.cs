// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling.ColorMaps;
using MatPlotLibNet.Styling;
using MatPlotLibNet;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>What a contour of the values of a grid holds and answers whether it draws lines (<see cref="ContourSeries"/>) or fills bands
/// (<see cref="ContourfSeries"/>): the grid, the levels, the range, the colour bar's span, the long-form table and the fields of the wire,
/// written once in <see cref="GridContourSeries"/> and held here for both kinds.</summary>
public class GridContourSeriesTests
{
    private static readonly double[] X = [1.0, 2.0];
    private static readonly double[] Y = [3.0, 4.0];
    private static readonly double[,] Z = { { 1, 2 }, { 3, 4 } };

    /// <summary>The two kinds of a contour of a grid, by the name each is written under.</summary>
    public static TheoryData<string> Kinds => ["contour", "contourf"];

    private static GridContourSeries Make(string kind, double[] x, double[] y, double[,] z) =>
        kind == "contour" ? new ContourSeries(x, y, z) : new ContourfSeries(x, y, z);

    /// <summary>A contour aims for ten levels and is given no level values until told; the grid it keeps is held for every kind on a data
    /// base by <c>AllSeriesTests.EveryKindOnADataBase_KeepsWhatItIsMadeFrom_InTheBasesMembers</c>.</summary>
    [Theory]
    [MemberData(nameof(Kinds))]
    public void TheLevels_AimForTen_WithNoValuesGiven(string kind)
    {
        var series = Make(kind, X, Y, Z);

        Assert.Equal(10, series.Levels);
        Assert.Null(series.LevelValues);
    }

    /// <summary>The grid fills its rectangle: its range over the drawable coordinates, sticky at every edge.</summary>
    [Theory]
    [MemberData(nameof(Kinds))]
    public void TheRange_IsTheGridPinnedAtEveryEdge(string kind)
    {
        var range = Make(kind, [1.0, double.NaN, 2.0], Y, new double[,] { { 1, 2, 3 }, { 3, 4, 5 } }).ComputeDataRange(null!);

        Assert.Equal(new DataRangeContribution(1, 2, 3, 4, StickyXMin: 1, StickyXMax: 2, StickyYMin: 3, StickyYMax: 4), range);
    }

    /// <summary>A grid with no column or no row adds nothing to the range.</summary>
    [Theory]
    [InlineData("contour", 0, 2)]
    [InlineData("contour", 2, 0)]
    [InlineData("contourf", 0, 2)]
    [InlineData("contourf", 2, 0)]
    public void AGridWithNoColumnOrNoRow_AddsNothing(string kind, int columns, int rows)
    {
        var series = Make(kind, new double[columns], new double[rows], new double[rows, columns]);

        Assert.Equal(default, series.ComputeDataRange(null!));
    }

    /// <summary>The bar spans the levels matplotlib takes for the count, not the values: <c>contourf(..., levels=10)</c> of values from
    /// 1 to 9 takes 0.8 to 9.6 in steps of 0.8 (matplotlib 3.11.2 prints <c>cs.levels</c> so), lines or bands alike.</summary>
    [Theory]
    [MemberData(nameof(Kinds))]
    public void TheColourBar_SpansTheLevels(string kind)
    {
        var (min, max) = Make(kind, [1.0, 2, 3], Y, new double[,] { { 1.0, 5.0, 3.0 }, { 7.0, 2.0, 9.0 } }).GetColorBarRange();

        Assert.Equal(0.8, min, 12);
        Assert.Equal(9.600000000000001, max, 12);
    }

    /// <summary>The table is the long form of the grid: a row per cell with its x, its y and its value, an index where a coordinate
    /// is missing.</summary>
    [Theory]
    [MemberData(nameof(Kinds))]
    public void TheTable_IsTheLongFormOfTheGrid(string kind)
    {
        var table = Make(kind, [5.0], Y, Z).ToDataTable()!;

        Assert.Equal(["x", "y", "z"], table.Columns.Select(c => c.Header));
        Assert.Equal(4, table.Rows.Count);
        Assert.Equal([5.0, 3.0, 1.0], table.Rows[0].Select(cell => cell.Number));
        Assert.Equal([1.0, 3.0, 2.0], table.Rows[1].Select(cell => cell.Number));
        Assert.Equal([1.0, 4.0, 4.0], table.Rows[3].Select(cell => cell.Number));
    }

    /// <summary>The wire carries the grid and the levels the caller set, and leaves the resting count out.</summary>
    [Theory]
    [MemberData(nameof(Kinds))]
    public void TheWire_CarriesTheGridAndTheGivenLevels(string kind)
    {
        var series = Make(kind, X, Y, Z);
        var resting = series.ToSeriesDto();
        series.Levels = 7;
        series.LevelValues = [1.5, 2.5];
        var given = series.ToSeriesDto();

        Assert.Same(X, resting.XData);
        Assert.Same(Y, resting.YData);
        Assert.Equal([[1.0, 2.0], [3.0, 4.0]], resting.HeatmapData);
        Assert.Null(resting.Levels);
        Assert.Equal(7, given.Levels);
        Assert.Equal([1.5, 2.5], given.LevelValues!);
    }

    // ContourSeries — empty data branch (both X and Y empty, hits the early-return).
    [Fact] public void ContourSeries_EmptyXData_HitsEarlyReturn()
    {
        var s = new ContourSeries(Array.Empty<double>(), Array.Empty<double>(), new double[0, 0]);
        var range = s.ComputeDataRange(new TestAxesContext());
        Assert.Null(range.XMin);
    }
}
