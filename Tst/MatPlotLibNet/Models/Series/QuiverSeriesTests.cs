// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="QuiverSeries"/> default properties and construction.</summary>
public class QuiverSeriesTests
{
    /// <summary>Verifies that the constructor stores X, Y, U, and V data.</summary>
    [Fact]
    public void Constructor_StoresData()
    {
        double[] x = [1, 2], y = [3, 4], u = [0.5, 0.5], v = [0.5, -0.5];
        var series = new QuiverSeries(x, y, u, v);
        Assert.Equal(x, series.XData);
        Assert.Equal(y, series.YData);
        Assert.Equal(u, series.UData);
        Assert.Equal(v, series.VData);
    }

    /// <summary>Verifies that Scale defaults to 1.</summary>
    [Fact]
    public void DefaultScale_Is1()
    {
        var series = new QuiverSeries([1.0], [2.0], [0.5], [0.5]);
        Assert.Equal(1.0, series.Scale);
    }

    /// <summary>Verifies that ArrowHeadSize defaults to 0.3.</summary>
    [Fact]
    public void DefaultArrowHeadSize_Is0Point3()
    {
        var series = new QuiverSeries([1.0], [2.0], [0.5], [0.5]);
        Assert.Equal(0.3, series.ArrowHeadSize);
    }

    /// <summary>Verifies that Color defaults to null.</summary>
    [Fact]
    public void DefaultColor_IsNull()
    {
        var series = new QuiverSeries([1.0], [2.0], [0.5], [0.5]);
        Assert.Null(series.Color);
    }
}
