// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="SurfaceSeries"/> default properties and construction.</summary>
public class SurfaceSeriesTests
{
    /// <summary>Verifies that the constructor stores X, Y, and Z data.</summary>
    [Fact]
    public void Constructor_StoresData()
    {
        double[] x = [1, 2, 3], y = [4, 5];
        var z = new double[,] { { 1, 2, 3 }, { 4, 5, 6 } };
        var series = new SurfaceSeries(x, y, z);
        Assert.Equal(x, series.X);
        Assert.Equal(y, series.Y);
        Assert.Equal(z, series.Z);
    }

    /// <summary>Verifies that Alpha defaults to 0.8.</summary>
    [Fact]
    public void DefaultAlpha_Is0Point8()
    {
        var series = new SurfaceSeries([1.0], [1.0], new double[,] { { 1 } });
        Assert.Equal(0.8, series.Alpha);
    }

    /// <summary>Verifies that ShowWireframe defaults to true.</summary>
    [Fact]
    public void DefaultShowWireframe_IsTrue()
    {
        var series = new SurfaceSeries([1.0], [1.0], new double[,] { { 1 } });
        Assert.True(series.ShowWireframe);
    }
}
