// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="VoxelSeries"/> default properties and construction.</summary>
public class VoxelSeriesTests
{
    private static readonly bool[,,] Filled = new bool[2, 3, 4];

    /// <summary>Verifies that the constructor stores the Filled array.</summary>
    [Fact]
    public void Constructor_StoresData()
    {
        var series = new VoxelSeries(Filled);
        Assert.Same(Filled, series.Filled);
    }

    /// <summary>Verifies that Alpha defaults to 0.8.</summary>
    [Fact]
    public void DefaultAlpha_Is0Point8()
    {
        var series = new VoxelSeries(Filled);
        Assert.Equal(0.8, series.Alpha);
    }

    /// <summary>Verifies that ComputeDataRange returns 0..dim for each axis.</summary>
    [Fact]
    public void ComputeDataRange_Returns0ToDim()
    {
        var series = new VoxelSeries(Filled);
        var range = series.ComputeDataRange(null!);
        Assert.Equal(0, range.XMin);
        Assert.Equal(2, range.XMax);
        Assert.Equal(0, range.YMin);
        Assert.Equal(3, range.YMax);
        Assert.Equal(0, range.ZMin);
        Assert.Equal(4, range.ZMax);
    }

    /// <summary>Verifies that ToSeriesDto sets type to "voxels".</summary>
    [Fact]
    public void ToSeriesDto_TypeIsVoxels()
    {
        var series = new VoxelSeries(Filled);
        var dto = series.ToSeriesDto();
        Assert.Equal("voxels", dto.Type);
    }
}
