// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Tests.Numerics;

/// <summary>Verifies <see cref="HexGrid"/> binning geometry and vertex math.</summary>
public class HexGridTests
{
    // --- ComputeHexBins ---

    /// <summary>All points are binned (no points are dropped).</summary>
    [Fact]
    public void ComputeHexBins_AllPointsBinned()
    {
        double[] x = [1.0, 1.5, 2.0, 2.5, 3.0];
        double[] y = [1.0, 1.5, 2.0, 2.5, 3.0];
        var bins = HexGrid.ComputeHexBins(x, y, 0.0, 4.0, 0.0, 4.0, gridSize: 5);
        int total = bins.Values.Sum();
        Assert.Equal(x.Length, total);
    }

    /// <summary>Coincident points accumulate in the same bin.</summary>
    [Fact]
    public void ComputeHexBins_CoincidentPoints_AccumulateInSameBin()
    {
        double[] x = [2.0, 2.0, 2.0];
        double[] y = [2.0, 2.0, 2.0];
        var bins = HexGrid.ComputeHexBins(x, y, 0.0, 4.0, 0.0, 4.0, gridSize: 5);
        Assert.Single(bins);
        Assert.Equal(3, bins.Values.Single());
    }

    /// <summary>Empty data returns empty dictionary.</summary>
    [Fact]
    public void ComputeHexBins_EmptyData_ReturnsEmpty()
    {
        var bins = HexGrid.ComputeHexBins([], [], 0.0, 1.0, 0.0, 1.0, gridSize: 5);
        Assert.Empty(bins);
    }

    /// <summary>Returns at least one non-empty bin for non-empty input.</summary>
    [Fact]
    public void ComputeHexBins_NonEmptyInput_HasAtLeastOneBin()
    {
        double[] x = [0.5, 1.0, 1.5, 2.0];
        double[] y = [0.5, 1.0, 1.5, 2.0];
        var bins = HexGrid.ComputeHexBins(x, y, 0.0, 3.0, 0.0, 3.0, gridSize: 4);
        Assert.NotEmpty(bins);
    }

    // --- HexagonVertices ---

    /// <summary>Returns exactly 6 vertices.</summary>
    [Fact]
    public void HexagonVertices_Returns6Vertices()
    {
        var verts = HexGrid.HexagonVertices(0.0, 0.0, 1.0);
        Assert.Equal(6, verts.Length);
    }

    /// <summary>All vertices are equidistant from the center (radius = hexSize).</summary>
    [Fact]
    public void HexagonVertices_AllVerticesEquidistantFromCenter()
    {
        double cx = 3.0, cy = 5.0, size = 2.0;
        var verts = HexGrid.HexagonVertices(cx, cy, size);
        foreach (var (vx, vy) in verts)
        {
            double dist = Math.Sqrt((vx - cx) * (vx - cx) + (vy - cy) * (vy - cy));
            Assert.Equal(size, dist, precision: 10);
        }
    }

    /// <summary>Adjacent vertices are equally spaced in angle (60° apart).</summary>
    [Fact]
    public void HexagonVertices_EquallySpacedAngles()
    {
        var verts = HexGrid.HexagonVertices(0.0, 0.0, 1.0);
        for (int i = 0; i < 6; i++)
        {
            var (x1, y1) = verts[i];
            var (x2, y2) = verts[(i + 1) % 6];
            double edge = Math.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));
            Assert.Equal(1.0, edge, precision: 10); // regular hex: edge = radius
        }
    }

    // --- HexCenter ---

    /// <summary>Hex center (0, 0) maps to origin in local data coordinates.</summary>
    [Fact]
    public void HexCenter_Origin_MapsToOffset()
    {
        double hexSize = 1.0;
        var (cx, cy) = HexGrid.HexCenter(0, 0, hexSize, xOffset: 0.0, yOffset: 0.0);
        Assert.Equal(0.0, cx, precision: 10);
        Assert.Equal(0.0, cy, precision: 10);
    }
}
