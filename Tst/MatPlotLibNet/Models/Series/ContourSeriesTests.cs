// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="ContourSeries"/> default properties and construction.</summary>
public class ContourSeriesTests
{
    /// <summary>Verifies that the constructor stores X, Y, and Z data.</summary>
    [Fact]
    public void Constructor_StoresData()
    {
        double[] x = [1.0, 2.0], y = [3.0, 4.0];
        var z = new double[,] { { 1, 2 }, { 3, 4 } };
        var series = new ContourSeries(x, y, z);
        Assert.Equal(x, series.XData);
        Assert.Equal(y, series.YData);
        Assert.Equal(z, series.ZData);
    }

    /// <summary>Verifies that Levels defaults to 10.</summary>
    [Fact]
    public void DefaultLevels_Is10()
    {
        var series = new ContourSeries([1.0], [1.0], new double[,] { { 1 } });
        Assert.Equal(10, series.Levels);
    }

    /// <summary>Verifies that Filled defaults to false.</summary>
    [Fact]
    public void DefaultFilled_IsFalse()
    {
        var series = new ContourSeries([1.0], [1.0], new double[,] { { 1 } });
        Assert.False(series.Filled);
    }

    /// <summary>Verifies that ColorMap defaults to null.</summary>
    [Fact]
    public void DefaultColorMap_IsNull()
    {
        var series = new ContourSeries([1.0], [1.0], new double[,] { { 1 } });
        Assert.Null(series.ColorMap);
    }
}
