// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="Trisurf3DSeries"/> default properties and construction.</summary>
public class Trisurf3DSeriesTests
{
    private static readonly double[] X = [1, 2, 3];
    private static readonly double[] Y = [4, 5, 6];
    private static readonly double[] Z = [7, 8, 9];
    private static readonly double[] Single = [1.0];

    /// <summary>Verifies that the constructor stores X, Y, and Z data.</summary>
    [Fact]
    public void Constructor_StoresData()
    {
        var series = new Trisurf3DSeries(X, Y, Z);
        Assert.Equal(X, (double[])series.X);
        Assert.Equal(Y, (double[])series.Y);
        Assert.Equal(Z, (double[])series.Z);
    }

    /// <summary>Verifies that Alpha defaults to 0.8.</summary>
    [Fact]
    public void DefaultAlpha_Is0Point8()
    {
        var series = new Trisurf3DSeries(Single, Single, Single);
        Assert.Equal(0.8, series.Alpha);
    }

    /// <summary>Verifies that ShowWireframe defaults to true.</summary>
    [Fact]
    public void DefaultShowWireframe_IsTrue()
    {
        var series = new Trisurf3DSeries(Single, Single, Single);
        Assert.True(series.ShowWireframe);
    }

    /// <summary>Verifies that ToSeriesDto sets type to "trisurf".</summary>
    [Fact]
    public void ToSeriesDto_TypeIsTrisurf()
    {
        var series = new Trisurf3DSeries(Single, Single, Single);
        var dto = series.ToSeriesDto();
        Assert.Equal("trisurf", dto.Type);
    }
}
