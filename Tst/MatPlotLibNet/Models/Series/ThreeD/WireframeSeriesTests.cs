// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="WireframeSeries"/> default properties and construction.</summary>
public class WireframeSeriesTests
{
    /// <summary>Verifies that the constructor stores X, Y, and Z data.</summary>
    [Fact]
    public void Constructor_StoresData()
    {
        double[] x = [1, 2, 3], y = [4, 5];
        var z = new double[,] { { 1, 2, 3 }, { 4, 5, 6 } };
        var series = new WireframeSeries(x, y, z);
        Assert.Equal(x, series.X);
        Assert.Equal(y, series.Y);
        Assert.Equal(z, series.Z);
    }

    /// <summary>Verifies that Color defaults to null.</summary>
    [Fact]
    public void DefaultColor_IsNull()
    {
        var series = new WireframeSeries([1.0], [1.0], new double[,] { { 1 } });
        Assert.Null(series.Color);
    }

    /// <summary>Verifies that LineWidth defaults to 0.5.</summary>
    [Fact]
    public void DefaultLineWidth_Is0Point5()
    {
        var series = new WireframeSeries([1.0], [1.0], new double[,] { { 1 } });
        Assert.Equal(0.5, series.LineWidth);
    }
}
