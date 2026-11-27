// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Drawing;
using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Tests.Numerics;

/// <summary>Verifies <see cref="MarchingSquares.ExtractBands"/> filled-contour band extraction.</summary>
public class MarchingSquaresBandTests
{
    /// <summary><paramref name="count"/> levels spread evenly over the values of <paramref name="z"/> that can be drawn, both
    /// ends included: <paramref name="count"/> - 1 bands that cover the grid.</summary>
    private static double[] Even(double[,] z, int count) =>
        z.DrawableRange() is { } range ? [.. Enumerable.Range(0, count).Select(i => range.Lo + i * (range.Hi - range.Lo) / (count - 1))] : [];

    // 4x4 monotone grid: z[row,col] = x[col] + y[row], range [-2, 4]
    private static readonly double[] XGrid = [-1, 0, 1, 2];
    private static readonly double[] YGrid = [-1, 0, 1, 2];
    private static readonly double[,] ZGrid = SampleSurface.Ramp(XGrid, YGrid).Z;

    /// <summary>ExtractBands returns a non-empty array for a valid grid.</summary>
    [Fact]
    public void ExtractBands_ReturnsBands_ForValidGrid()
    {
        var bands = MarchingSquares.ExtractBands(XGrid, YGrid, ZGrid, Even(ZGrid, 5));
        Assert.NotEmpty(bands);
    }

    /// <summary>Number of bands equals levels - 1 (one band between each consecutive pair of iso-levels).</summary>
    [Fact]
    public void ExtractBands_BandCount_IsLevelsMinusOne()
    {
        const int levels = 5;
        var bands = MarchingSquares.ExtractBands(XGrid, YGrid, ZGrid, Even(ZGrid, levels));
        Assert.Equal(levels - 1, bands.Length);
    }

    /// <summary>Each band carries the correct low and high level values.</summary>
    [Fact]
    public void ExtractBands_EachBand_HasCorrectLevelRange()
    {
        const int levels = 4;
        var bands = MarchingSquares.ExtractBands(XGrid, YGrid, ZGrid, Even(ZGrid, levels));
        for (int i = 0; i < bands.Length - 1; i++)
            Assert.Equal(bands[i].LevelHigh, bands[i + 1].LevelLow, precision: 6);
    }

    /// <summary>Level ranges are strictly ascending.</summary>
    [Fact]
    public void ExtractBands_LevelRanges_AreAscending()
    {
        const int levels = 6;
        var bands = MarchingSquares.ExtractBands(XGrid, YGrid, ZGrid, Even(ZGrid, levels));
        foreach (var band in bands)
            Assert.True(band.LevelHigh > band.LevelLow,
                $"Band [{band.LevelLow:F3}, {band.LevelHigh:F3}] is not ascending");
    }

    /// <summary>Returns empty for a grid with fewer than 2 rows or 2 cols.</summary>
    [Fact]
    public void ExtractBands_ReturnsEmpty_ForTooSmallGrid()
    {
        var bands = MarchingSquares.ExtractBands([1.0], [1.0], new double[,] { { 1.0 } }, Even(new double[,] { { 1.0 } }, 3));
        Assert.Empty(bands);
    }

    /// <summary>Handles the minimal 2x2 grid without throwing.</summary>
    [Fact]
    public void ExtractBands_HandlesMinimalGrid_2x2()
    {
        double[] x = [0.0, 1.0];
        double[] y = [0.0, 1.0];
        double[,] z = { { 0.0, 1.0 }, { 1.0, 2.0 } };

        var bands = MarchingSquares.ExtractBands(x, y, z, Even(z, 3));
        Assert.Equal(2, bands.Length);
    }

    /// <summary>First level equals z-min and last level equals z-max (full coverage).</summary>
    [Fact]
    public void ExtractBands_FirstAndLastLevels_CoverFullZRange()
    {
        const int levels = 5;
        var bands = MarchingSquares.ExtractBands(XGrid, YGrid, ZGrid, Even(ZGrid, levels));
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
        var bands = MarchingSquares.ExtractBands([0.0, 1.0], [0.0, 1.0], flat, Even(flat, 4));
        Assert.Empty(bands);
    }

    /// <summary>Each band's Polygons collection is not null.</summary>
    [Fact]
    public void ExtractBands_EachBand_HasNonNullPolygons()
    {
        var bands = MarchingSquares.ExtractBands(XGrid, YGrid, ZGrid, Even(ZGrid, 4));
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

    /// <summary>z = x + y over -1..2: a band above the lowest holds the region where the grid is above its lower
    /// threshold, x + y >= 1 here, the square less the corner triangle below the line, 9 - 4.5 = 4.5, its pieces anticlockwise
    /// and every vertex on or above the line; the lowest band holds none, as the fill paints it under the whole grid.</summary>
    [Fact]
    public void ExtractBands_WithThresholds_AHigherBandHoldsTheRegionAboveItsLowerThreshold()
    {
        var bands = MarchingSquares.ExtractBands(XGrid, YGrid, ZGrid, [0.5, 1.0, 3.0]);

        Assert.Empty(bands[0].Polygons);
        double area = 0;
        foreach (var piece in bands[1].Polygons)
        {
            double twice = 0;
            for (int i = 0; i < piece.Length; i++)
                twice += piece[i].X * piece[(i + 1) % piece.Length].Y - piece[(i + 1) % piece.Length].X * piece[i].Y;
            Assert.True(twice > 0, "a piece runs anticlockwise and encloses something");
            area += twice / 2;
        }

        Assert.Equal(4.5, area, 5);
        Assert.All(bands[1].Polygons.SelectMany(p => p), p => Assert.True(p.X + p.Y >= 1.0 - 1e-5, $"({p.X}, {p.Y}) lies below x + y = 1"));
    }

    /// <summary>A saddle cell, two opposite corners above the level, gives the two corner triangles, as the iso-line's
    /// saddle rule separates them (the lines of the same cell bound them): on the unit cell with 1 at two opposite corners and
    /// 0 at the others, the level 0.5 cuts each corner's triangle of area 1/8.</summary>
    [Theory]
    [InlineData(1.0, 0.0, 0.0, 1.0)]
    [InlineData(0.0, 1.0, 1.0, 0.0)]
    public void ExtractBands_ASaddle_GivesTheTwoCornerTriangles(double bottomLeft, double bottomRight, double topLeft, double topRight)
    {
        double[,] z = { { bottomLeft, bottomRight }, { topLeft, topRight } };

        var band = MarchingSquares.ExtractBands([0.0, 1.0], [0.0, 1.0], z, [0.0, 0.5, 1.0])[1];

        Assert.Equal(2, band.Polygons.Length);
        Assert.All(band.Polygons, piece => Assert.Equal(3, piece.Length));
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
    }

    [Fact]
    public void ExtractBands_WithThresholds_ReturnsEmpty_ForTooSmallAFlatOrAnUndrawableGrid()
    {
        double[] x = [0.0, 1.0];
        Assert.Empty(MarchingSquares.ExtractBands([1.0], [1.0], new double[,] { { 1.0 } }, [0.0, 1.0]));
        Assert.Empty(MarchingSquares.ExtractBands(x, x, new double[,] { { 5.0, 5.0 }, { 5.0, 5.0 } }, [4.0, 6.0]));
        Assert.Empty(MarchingSquares.ExtractBands(x, x, new double[,] { { double.NaN, double.NaN }, { double.NaN, double.NaN } }, [0.0, 1.0]));
    }

    // ---- a band's edge that equals a value of the grid ------------------------------------------------------------------
    // contourpy 1.4.0 (matplotlib 3.11.2), a 4 x 4 grid of zeros whose middle 2 x 2 is 1:
    // g.filled(1, 2) = nothing (the ones are not above 1: a band holds lower < z <= upper), g.filled(0, 1) = the octagon
    // (0,1) (1,0) (2,0) (3,1) (3,2) (2,3) (1,3) (0,2), area 7, where the zeros of the grid's rim are not above 0.
    private static readonly double[] Four = [0, 1, 2, 3];
    private static readonly double[,] Plateau =
    {
        { 0, 0, 0, 0 },
        { 0, 1, 1, 0 },
        { 0, 1, 1, 0 },
        { 0, 0, 0, 0 },
    };

    private static double Area(PointF[] ring)
    {
        double twice = 0;
        for (int i = 0; i < ring.Length; i++)
        {
            var a = ring[i];
            var b = ring[(i + 1) % ring.Length];
            twice += (double)a.X * b.Y - (double)b.X * a.Y;
        }

        return Math.Abs(twice) / 2;
    }

    [Fact]
    public void ExtractBands_APlateauAtABandsLowerEdge_IsNotAboveIt_SoTheBandHoldsNothing()
    {
        var bands = MarchingSquares.ExtractBands(Four, Four, Plateau, [0, 1, 2]);

        Assert.Empty(bands[1].Polygons);
    }

    [Fact]
    public void ExtractBands_TheValuesThatEqualABandsLowerEdge_AreNotInTheRegionAboveIt()
    {
        var bands = MarchingSquares.ExtractBands(Four, Four, Plateau, [-1, 0, 1]);

        Assert.Equal(7, bands[1].Polygons.Sum(Area), 6);
    }
}
