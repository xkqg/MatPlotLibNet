// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="Line3DSeries"/> default properties and construction.</summary>
public class Line3DSeriesTests
{
    private static readonly double[] X = [1, 2, 3];
    private static readonly double[] Y = [4, 5, 6];
    private static readonly double[] Z = [7, 8, 9];
    private static readonly double[] Single = [1.0];

    /// <summary>Verifies that the constructor stores X, Y, and Z data.</summary>
    [Fact]
    public void Constructor_StoresData()
    {
        var series = new Line3DSeries(X, Y, Z);
        Assert.Equal(X, (double[])series.X);
        Assert.Equal(Y, (double[])series.Y);
        Assert.Equal(Z, (double[])series.Z);
    }

    /// <summary>Verifies that Color defaults to null.</summary>
    [Fact]
    public void DefaultColor_IsNull()
    {
        var series = new Line3DSeries(Single, Single, Single);
        Assert.Null(series.Color);
    }

    /// <summary>Verifies that LineWidth defaults to 1.5.</summary>
    [Fact]
    public void DefaultLineWidth_Is1Point5()
    {
        var series = new Line3DSeries(Single, Single, Single);
        Assert.Equal(1.5, series.LineWidth);
    }

    /// <summary>Verifies that LineStyle defaults to Solid.</summary>
    [Fact]
    public void DefaultLineStyle_IsSolid()
    {
        var series = new Line3DSeries(Single, Single, Single);
        Assert.Equal(LineStyle.Solid, series.LineStyle);
    }

    /// <summary>Verifies that ToSeriesDto sets type to "line3d".</summary>
    [Fact]
    public void ToSeriesDto_TypeIsLine3D()
    {
        var series = new Line3DSeries(Single, Single, Single);
        var dto = series.ToSeriesDto();
        Assert.Equal("line3d", dto.Type);
    }
}
