// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="Quiver3DSeries"/> default properties and construction.</summary>
public class Quiver3DSeriesTests
{
    private static readonly double[] X = [1, 2, 3];
    private static readonly double[] Y = [4, 5, 6];
    private static readonly double[] Z = [7, 8, 9];
    private static readonly double[] U = [0.1, 0.2, 0.3];
    private static readonly double[] V = [0.4, 0.5, 0.6];
    private static readonly double[] W = [0.7, 0.8, 0.9];
    private static readonly double[] Single = [1.0];
    private static readonly double[] SingleDir = [0.5];

    /// <summary>Verifies that the constructor stores X, Y, Z, U, V, and W data.</summary>
    [Fact]
    public void Constructor_StoresData()
    {
        var series = new Quiver3DSeries(X, Y, Z, U, V, W);
        Assert.Equal(X, (double[])series.X);
        Assert.Equal(Y, (double[])series.Y);
        Assert.Equal(Z, (double[])series.Z);
        Assert.Equal(U, (double[])series.U);
        Assert.Equal(V, (double[])series.V);
        Assert.Equal(W, (double[])series.W);
    }

    /// <summary>Verifies that ArrowLength defaults to 1.0.</summary>
    [Fact]
    public void DefaultArrowLength_Is1Point0()
    {
        var series = new Quiver3DSeries(Single, Single, Single, SingleDir, SingleDir, SingleDir);
        Assert.Equal(1.0, series.ArrowLength);
    }

    /// <summary>Verifies that ComputeDataRange includes arrow tips.</summary>
    [Fact]
    public void ComputeDataRange_IncludesArrowTips()
    {
        double[] x = [0], y = [0], z = [0];
        double[] u = [2], v = [3], w = [4];
        var series = new Quiver3DSeries(x, y, z, u, v, w);
        var range = series.ComputeDataRange(null!);
        Assert.Equal(0, range.XMin);
        Assert.Equal(2, range.XMax);
        Assert.Equal(0, range.YMin);
        Assert.Equal(3, range.YMax);
        Assert.Equal(0, range.ZMin);
        Assert.Equal(4, range.ZMax);
    }

    /// <summary>Verifies that ToSeriesDto sets type to "quiver3d".</summary>
    [Fact]
    public void ToSeriesDto_TypeIsQuiver3D()
    {
        var series = new Quiver3DSeries(Single, Single, Single, SingleDir, SingleDir, SingleDir);
        var dto = series.ToSeriesDto();
        Assert.Equal("quiver3d", dto.Type);
    }
}
