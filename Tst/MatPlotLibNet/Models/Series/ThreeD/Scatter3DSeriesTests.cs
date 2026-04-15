// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="Scatter3DSeries"/> default properties and construction.</summary>
public class Scatter3DSeriesTests
{
    private static readonly double[] X = [1, 2, 3];
    private static readonly double[] Y = [4, 5, 6];
    private static readonly double[] Z = [7, 8, 9];
    private static readonly double[] Single = [1.0];

    /// <summary>Verifies that the constructor stores X, Y, and Z data.</summary>
    [Fact]
    public void Constructor_StoresData()
    {
        var series = new Scatter3DSeries(X, Y, Z);
        Assert.Equal(X, (double[])series.X);
        Assert.Equal(Y, (double[])series.Y);
        Assert.Equal(Z, (double[])series.Z);
    }

    /// <summary>Verifies that Color defaults to null.</summary>
    [Fact]
    public void DefaultColor_IsNull()
    {
        var series = new Scatter3DSeries(Single, Single, Single);
        Assert.Null(series.Color);
    }

    /// <summary>Verifies that MarkerSize defaults to 6.</summary>
    [Fact]
    public void DefaultMarkerSize_Is6()
    {
        var series = new Scatter3DSeries(Single, Single, Single);
        Assert.Equal(6, series.MarkerSize);
    }
}
