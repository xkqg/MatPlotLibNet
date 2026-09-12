// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Tests.Numerics;

/// <summary>How crowded each point's neighbourhood is, counted rather than estimated. A two-dimensional kernel
/// estimate costs a pass over every other point for every point; a grid costs one pass over all of them, which
/// is what makes it usable at the point counts where a density-coloured scatter is worth drawing at all.</summary>
public class PointDensityTests
{
    [Fact]
    public void AnEvenlySpreadCloud_GivesEveryPointTheSameCount()
    {
        // Nine points on a regular 3x3 lattice, binned into a 3x3 grid: one per cell, so one count each.
        double[] x = [0, 1, 2, 0, 1, 2, 0, 1, 2];
        double[] y = [0, 0, 0, 1, 1, 1, 2, 2, 2];

        var density = PointDensity.PerPoint(x, y, bins: 3);

        Assert.Equal(9, density.Length);
        Assert.Single(density.Distinct());
        Assert.Equal(1.0, density[0]);
    }

    [Fact]
    public void ACrowdedSpot_ScoresHigherThanALonelyOne()
    {
        // Four points crammed into one corner, one point alone in the other.
        double[] x = [0.01, 0.02, 0.03, 0.04, 10.0];
        double[] y = [0.01, 0.02, 0.03, 0.04, 10.0];

        var density = PointDensity.PerPoint(x, y, bins: 10);

        Assert.True(density[0] > density[4], $"crowded {density[0]} should beat lonely {density[4]}");
        Assert.Equal(1.0, density[4]);
    }

    [Fact]
    public void OnePoint_IsItsOwnNeighbourhood()
    {
        var density = PointDensity.PerPoint([5.0], [5.0], bins: 8);

        Assert.Equal([1.0], density);
    }

    [Fact]
    public void NoPoints_IsNoDensities()
    {
        Assert.Empty(PointDensity.PerPoint([], [], bins: 8));
    }

    [Fact]
    public void PointsOnOneVerticalLine_AreStillCounted()
    {
        // The x span is zero. A naive scaling divides by it; every point must still land in a cell.
        double[] x = [3.0, 3.0, 3.0, 3.0];
        double[] y = [0.0, 1.0, 2.0, 3.0];

        var density = PointDensity.PerPoint(x, y, bins: 4);

        Assert.Equal(4, density.Length);
        Assert.All(density, d => Assert.Equal(1.0, d));
    }

    [Fact]
    public void PointsOnOneHorizontalLine_AreStillCounted()
    {
        var density = PointDensity.PerPoint([0.0, 1.0, 2.0, 3.0], [7.0, 7.0, 7.0, 7.0], bins: 4);

        Assert.All(density, d => Assert.Equal(1.0, d));
    }

    [Fact]
    public void EveryPointInOneSpot_CountsThemAll()
    {
        var density = PointDensity.PerPoint([2.0, 2.0, 2.0], [2.0, 2.0, 2.0], bins: 8);

        Assert.All(density, d => Assert.Equal(3.0, d));
    }

    [Fact]
    public void TheGridSize_FollowsThePointCount_WhenNoneIsGiven()
    {
        // Left to itself it aims for roughly ten points per cell, so the colours say something about the shape
        // rather than about the sampling: too few cells and everything is one colour, too many and every point
        // is alone in its own.
        Assert.Equal(4, PointDensity.BinsFor(1));
        Assert.Equal(4, PointDensity.BinsFor(100));
        Assert.Equal(22, PointDensity.BinsFor(5000));
        Assert.Equal(128, PointDensity.BinsFor(10_000_000));
    }

    [Fact]
    public void TwoAxesOfDifferentLengths_AreRefused()
    {
        Assert.Throws<ArgumentException>(() => PointDensity.PerPoint([1.0, 2.0], [1.0], bins: 4));
    }
}
