// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="Scatter3DSeries"/> default properties and construction.</summary>
public class Scatter3DSeriesTests
{
    /// <summary>Verifies that the constructor stores X, Y, and Z data.</summary>
    [Fact]
    public void Constructor_StoresData()
    {
        double[] x = [1, 2, 3], y = [4, 5, 6], z = [7, 8, 9];
        var series = new Scatter3DSeries(x, y, z);
        Assert.Equal(x, series.X);
        Assert.Equal(y, series.Y);
        Assert.Equal(z, series.Z);
    }

    /// <summary>Verifies that Color defaults to null.</summary>
    [Fact]
    public void DefaultColor_IsNull()
    {
        var series = new Scatter3DSeries([1.0], [2.0], [3.0]);
        Assert.Null(series.Color);
    }

    /// <summary>Verifies that MarkerSize defaults to 6.</summary>
    [Fact]
    public void DefaultMarkerSize_Is6()
    {
        var series = new Scatter3DSeries([1.0], [2.0], [3.0]);
        Assert.Equal(6, series.MarkerSize);
    }
}
