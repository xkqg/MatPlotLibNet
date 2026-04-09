// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.Algorithms;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies <see cref="MarchingSquares"/> iso-line extraction.</summary>
public class MarchingSquaresTests
{
    // 4x4 grid for x = [-1, 0, 1, 2], y = [-1, 0, 1, 2]
    // z[row, col] = x[col] + y[row]  (row index = y, col index = x)
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
}
