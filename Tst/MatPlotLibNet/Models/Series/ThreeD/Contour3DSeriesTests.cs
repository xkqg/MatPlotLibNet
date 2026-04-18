// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="Contour3DSeries"/> default properties and construction.</summary>
public class Contour3DSeriesTests
{
    private static readonly double[] X = [1, 2, 3];
    private static readonly double[] Y = [4, 5];
    private static readonly double[,] Z = { { 1, 2, 3 }, { 4, 5, 6 } };
    private static readonly double[] SingleX = [1.0];
    private static readonly double[] SingleY = [2.0];
    private static readonly double[,] SingleZ = { { 1.0 } };

    /// <summary>Verifies that the constructor stores X, Y, and Z grid data.</summary>
    [Fact]
    public void Constructor_StoresData()
    {
        var series = new Contour3DSeries(X, Y, Z);
        Assert.Equal(X, series.X);
        Assert.Equal(Y, series.Y);
        Assert.Equal(Z, series.Z);
    }

    /// <summary>Verifies that Levels defaults to 10.</summary>
    [Fact]
    public void DefaultLevels_Is10()
    {
        var series = new Contour3DSeries(SingleX, SingleY, SingleZ);
        Assert.Equal(10, series.Levels);
    }

    /// <summary>Verifies that LineWidth defaults to 1.0.</summary>
    [Fact]
    public void DefaultLineWidth_Is1Point0()
    {
        var series = new Contour3DSeries(SingleX, SingleY, SingleZ);
        Assert.Equal(1.0, series.LineWidth);
    }

    /// <summary>Verifies that ToSeriesDto sets type to "contour3d".</summary>
    [Fact]
    public void ToSeriesDto_TypeIsContour3D()
    {
        var series = new Contour3DSeries(SingleX, SingleY, SingleZ);
        var dto = series.ToSeriesDto();
        Assert.Equal("contour3d", dto.Type);
    }
}
