// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Tests.Numerics;

/// <summary>Verifies <see cref="MarchingSquares"/> iso-line extraction.</summary>
public class MarchingSquaresTests
{
    // 4x4 grid for x = [-1, 0, 1, 2], y = [-1, 0, 1, 2]
    // z[row, col] = x[col] + y[row]  (row index = y, col index = x)
    private static readonly double[] XGrid = [-1, 0, 1, 2];
    private static readonly double[] YGrid = [-1, 0, 1, 2];
    private static readonly double[,] ZGrid = SampleSurface.Ramp(XGrid, YGrid).Z;

    /// <summary>Returns a non-empty result for a valid level.</summary>
    [Fact]
    public void Extract_ReturnsContourLines_ForValidLevel()
    {
        var lines = MarchingSquares.Extract(XGrid, YGrid, ZGrid, [0.0]);
        Assert.NotEmpty(lines);
    }

    /// <summary>Each returned ContourLine carries the correct level.</summary>
    [Fact]
    public void Extract_LevelMatchesRequested()
    {
        var lines = MarchingSquares.Extract(XGrid, YGrid, ZGrid, [0.0]);
        foreach (var line in lines)
            Assert.Equal(0.0, line.Level);
    }

    /// <summary>Each ContourLine has at least two points (one segment).</summary>
    [Fact]
    public void Extract_EachLineHasAtLeastTwoPoints()
    {
        var lines = MarchingSquares.Extract(XGrid, YGrid, ZGrid, [0.0]);
        foreach (var line in lines)
            Assert.True(line.Points.Length >= 2, $"Contour line at level {line.Level} has {line.Points.Length} points");
    }

    /// <summary>Extracts multiple levels, one ContourLine per level when the function crosses each level.</summary>
    [Fact]
    public void Extract_MultipleLevels_ReturnsOneGroupPerLevel()
    {
        double[] levels = [-1.0, 0.0, 1.0];
        var lines = MarchingSquares.Extract(XGrid, YGrid, ZGrid, levels);
        // All three levels cross the monotone z=x+y surface
        var foundLevels = lines.Select(l => l.Level).Distinct().ToArray();
        Assert.Equal(3, foundLevels.Length);
    }

    /// <summary>Returns empty when the level is outside the Z range (no crossings).</summary>
    [Fact]
    public void Extract_ReturnsEmpty_WhenLevelOutsideRange()
    {
        var lines = MarchingSquares.Extract(XGrid, YGrid, ZGrid, [99.0]);
        Assert.Empty(lines);
    }

    [Theory]
    [InlineData(1, 5)]   // rows < 2
    [InlineData(5, 1)]   // cols < 2
    [InlineData(0, 0)]   // empty grid
    [InlineData(1, 1)]   // single point
    public void DegenerateGrid_ReturnsEmpty(int rows, int cols)
    {
        // Build minimal-ish grid arrays of given shape.
        var x = new double[cols];
        var y = new double[rows];
        var z = new double[rows, cols];
        var contours = MarchingSquares.Extract(x, y, z, new[] { 0.5 });
        Assert.Empty(contours);
    }

    [Fact]
    public void AllEqualGrid_NoContoursProduced()
    {
        // Every cell has identical Z value. No iso-level can cross a flat surface
        // unless the level exactly matches the value (which the algorithm treats as
        // case 0/15 with all corners >= or all corners < — both early-return).
        double[] x = { 0, 1, 2, 3 };
        double[] y = { 0, 1, 2, 3 };
        var z = new double[4, 4];
        for (int r = 0; r < 4; r++)
            for (int c = 0; c < 4; c++)
                z[r, c] = 5.0;

        // Level above the constant value → all corners < level → case 0 everywhere
        var above = MarchingSquares.Extract(x, y, z, new[] { 10.0 });
        Assert.Empty(above);

        // Level below → all corners >= level → case 15 everywhere
        var below = MarchingSquares.Extract(x, y, z, new[] { 1.0 });
        Assert.Empty(below);
    }

    [Fact]
    public void TwoByTwoGrid_WithCrossingLevel_ProducesSingleSegment()
    {
        // Minimal valid grid (2x2). Place a level that splits the cell:
        //   z =  [[0, 0],
        //         [10, 10]]
        // Level 5 crosses horizontally → exactly one contour line through the cell.
        double[] x = { 0, 1 };
        double[] y = { 0, 1 };
        var z = new double[2, 2] { { 0, 0 }, { 10, 10 } };

        var contours = MarchingSquares.Extract(x, y, z, new[] { 5.0 });
        Assert.Single(contours);
        // The contour line must have at least 2 points (a segment).
        Assert.True(contours[0].Points.Length >= 2,
            $"Expected segment with 2+ points, got {contours[0].Points.Length}");
    }
}
