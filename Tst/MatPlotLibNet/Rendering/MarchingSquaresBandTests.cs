// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.Algorithms;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies <see cref="MarchingSquares.ExtractBands"/> filled-contour band extraction.</summary>
public class MarchingSquaresBandTests
{
    // 4x4 monotone grid: z[row,col] = x[col] + y[row], range [-2, 4]
    private static readonly double[] XGrid = [-1, 0, 1, 2];
    private static readonly double[] YGrid = [-1, 0, 1, 2];
    private static readonly double[,] ZGrid = BuildGrid(XGrid, YGrid);

    private static double[,] BuildGrid(double[] x, double[] y)
    {
        var z = new double[y.Length, x.Length];
        for (int row = 0; row < y.Length; row++)
            for (int col = 0; col < x.Length; col++)
                z[row, col] = x[col] + y[row];
        return z;
    }

    /// <summary>ExtractBands returns a non-empty array for a valid grid.</summary>
    [Fact]
    public void ExtractBands_ReturnsBands_ForValidGrid()
    {
        var bands = MarchingSquares.ExtractBands(XGrid, YGrid, ZGrid, levels: 5);
        Assert.NotEmpty(bands);
    }

    /// <summary>Number of bands equals levels - 1 (one band between each consecutive pair of iso-levels).</summary>
    [Fact]
    public void ExtractBands_BandCount_IsLevelsMinusOne()
    {
        const int levels = 5;
        var bands = MarchingSquares.ExtractBands(XGrid, YGrid, ZGrid, levels);
        Assert.Equal(levels - 1, bands.Length);
    }

    /// <summary>Each band carries the correct low and high level values.</summary>
    [Fact]
    public void ExtractBands_EachBand_HasCorrectLevelRange()
    {
        const int levels = 4;
        var bands = MarchingSquares.ExtractBands(XGrid, YGrid, ZGrid, levels);
        for (int i = 0; i < bands.Length - 1; i++)
            Assert.Equal(bands[i].LevelHigh, bands[i + 1].LevelLow, precision: 6);
    }

    /// <summary>Level ranges are strictly ascending.</summary>
    [Fact]
    public void ExtractBands_LevelRanges_AreAscending()
    {
        const int levels = 6;
        var bands = MarchingSquares.ExtractBands(XGrid, YGrid, ZGrid, levels);
        foreach (var band in bands)
            Assert.True(band.LevelHigh > band.LevelLow,
                $"Band [{band.LevelLow:F3}, {band.LevelHigh:F3}] is not ascending");
    }

    /// <summary>Returns empty for a grid with fewer than 2 rows or 2 cols.</summary>
    [Fact]
    public void ExtractBands_ReturnsEmpty_ForTooSmallGrid()
    {
        var bands = MarchingSquares.ExtractBands([1.0], [1.0], new double[,] { { 1.0 } }, levels: 3);
        Assert.Empty(bands);
    }

    /// <summary>Handles the minimal 2x2 grid without throwing.</summary>
    [Fact]
    public void ExtractBands_HandlesMinimalGrid_2x2()
    {
        double[] x = [0.0, 1.0];
        double[] y = [0.0, 1.0];
        double[,] z = { { 0.0, 1.0 }, { 1.0, 2.0 } };

        var bands = MarchingSquares.ExtractBands(x, y, z, levels: 3);
        Assert.Equal(2, bands.Length);
    }

    /// <summary>First level equals z-min and last level equals z-max (full coverage).</summary>
    [Fact]
    public void ExtractBands_FirstAndLastLevels_CoverFullZRange()
    {
        const int levels = 5;
        var bands = MarchingSquares.ExtractBands(XGrid, YGrid, ZGrid, levels);
        double zMin = ZGrid.Cast<double>().Min();
        double zMax = ZGrid.Cast<double>().Max();
        Assert.Equal(zMin, bands[0].LevelLow, precision: 6);
        Assert.Equal(zMax, bands[^1].LevelHigh, precision: 6);
    }

    /// <summary>A completely flat grid (all same z) returns empty bands (no variation to fill).</summary>
    [Fact]
    public void ExtractBands_ReturnsEmpty_ForFlatGrid()
    {
        double[,] flat = { { 5.0, 5.0 }, { 5.0, 5.0 } };
        var bands = MarchingSquares.ExtractBands([0.0, 1.0], [0.0, 1.0], flat, levels: 4);
        Assert.Empty(bands);
    }

    /// <summary>Each band's Polygons collection is not null.</summary>
    [Fact]
    public void ExtractBands_EachBand_HasNonNullPolygons()
    {
        var bands = MarchingSquares.ExtractBands(XGrid, YGrid, ZGrid, levels: 4);
        foreach (var band in bands)
            Assert.NotNull(band.Polygons);
    }

    // ---- bands between thresholds that are given -----------------------------------------------------------------------

    /// <summary>The bands lie between the thresholds as given, whatever range the grid spans: the grid runs from -2 to 4,
    /// the thresholds from 0.5 to 3.</summary>
    [Fact]
    public void ExtractBands_WithThresholds_LieBetweenConsecutiveThresholds()
    {
        var bands = MarchingSquares.ExtractBands(XGrid, YGrid, ZGrid, [0.5, 1.0, 3.0]);

        Assert.Equal([0.5, 1.0], bands.Select(b => b.LevelLow));
        Assert.Equal([1.0, 3.0], bands.Select(b => b.LevelHigh));
    }

    /// <summary>The count of levels is the even spacing over the grid's range, so naming those thresholds draws what the
    /// count draws.</summary>
    [Fact]
    public void ExtractBands_WithThresholds_DrawsTheBandsTheCountDraws_WhenGivenTheEvenSpacing()
    {
        var counted = MarchingSquares.ExtractBands(XGrid, YGrid, ZGrid, levels: 5);
        var given = MarchingSquares.ExtractBands(XGrid, YGrid, ZGrid, ContourLevels.Spanning(-2, 4, 5));

        Assert.Equal(counted.Length, given.Length);
        for (int b = 0; b < counted.Length; b++)
        {
            Assert.Equal(counted[b].LevelLow, given[b].LevelLow);
            Assert.Equal(counted[b].LevelHigh, given[b].LevelHigh);
            Assert.Equal(counted[b].Polygons.Select(p => p.ToList()), given[b].Polygons.Select(p => p.ToList()));
        }
    }

    [Fact]
    public void ExtractBands_WithThresholds_ARingOfEachThresholdBoundsItsBands()
    {
        // z = x + y: the iso-line of 0.5 and the iso-line of 1.0 are the two lines x + y = 0.5 and x + y = 1.0
        var bands = MarchingSquares.ExtractBands(XGrid, YGrid, ZGrid, [0.5, 1.0]);

        var band = Assert.Single(bands);
        Assert.All(band.Polygons.SelectMany(p => p), p => Assert.True(
            Math.Abs(p.X + p.Y - 0.5) < 1e-4 || Math.Abs(p.X + p.Y - 1.0) < 1e-4, $"({p.X}, {p.Y}) lies on neither iso-line"));
    }

    [Theory]
    [InlineData(new double[0])]
    [InlineData(new[] { 1.0 })]
    public void ExtractBands_WithFewerThanTwoThresholds_ReturnsEmpty(double[] thresholds) =>
        Assert.Empty(MarchingSquares.ExtractBands(XGrid, YGrid, ZGrid, thresholds));

    /// <summary>A grid needs two rows and two columns: one short side is enough to leave nothing to fill, for either way of
    /// naming the levels.</summary>
    [Fact]
    public void ExtractBands_ReturnsEmpty_WhenEitherSideOfTheGridHasASingleLine()
    {
        double[,] oneRow = { { 0.0, 1.0, 2.0 } };
        double[,] oneColumn = { { 0.0 }, { 1.0 }, { 2.0 } };

        Assert.Empty(MarchingSquares.ExtractBands([0.0, 1.0, 2.0], [0.0], oneRow, [0.5, 1.5]));
        Assert.Empty(MarchingSquares.ExtractBands([0.0], [0.0, 1.0, 2.0], oneColumn, [0.5, 1.5]));
        Assert.Empty(MarchingSquares.ExtractBands([0.0, 1.0, 2.0], [0.0], oneRow, levels: 4));
        Assert.Empty(MarchingSquares.ExtractBands([0.0], [0.0, 1.0, 2.0], oneColumn, levels: 4));
    }

    [Fact]
    public void ExtractBands_WithThresholds_ReturnsEmpty_ForTooSmallAFlatOrAnUndrawableGrid()
    {
        double[] x = [0.0, 1.0];
        Assert.Empty(MarchingSquares.ExtractBands([1.0], [1.0], new double[,] { { 1.0 } }, [0.0, 1.0]));
        Assert.Empty(MarchingSquares.ExtractBands(x, x, new double[,] { { 5.0, 5.0 }, { 5.0, 5.0 } }, [4.0, 6.0]));
        Assert.Empty(MarchingSquares.ExtractBands(x, x, new double[,] { { double.NaN, double.NaN }, { double.NaN, double.NaN } }, [0.0, 1.0]));
    }
}
