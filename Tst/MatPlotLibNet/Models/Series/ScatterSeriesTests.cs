// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="ScatterSeries"/> default properties and construction.</summary>
public class ScatterSeriesTests
{
    /// <summary>Verifies that the constructor stores X and Y data.</summary>
    [Fact]
    public void Constructor_StoresData()
    {
        double[] x = [1, 2], y = [3, 4];
        var series = new ScatterSeries(x, y);
        Assert.Equal(x, series.XData);
        Assert.Equal(y, series.YData);
    }

    /// <summary>Verifies that Marker defaults to Circle.</summary>
    [Fact]
    public void DefaultMarker_IsCircle()
    {
        var series = new ScatterSeries([1.0], [2.0]);
        Assert.Equal(MarkerStyle.Circle, series.Marker);
    }

    /// <summary>Verifies that Alpha defaults to 1.0.</summary>
    [Fact]
    public void DefaultAlpha_IsOne()
    {
        var series = new ScatterSeries([1.0], [2.0]);
        Assert.Equal(1.0, series.Alpha);
    }

    /// <summary>Verifies that Sizes defaults to null.</summary>
    [Fact]
    public void Sizes_DefaultNull()
    {
        var series = new ScatterSeries([1.0], [2.0]);
        Assert.Null(series.Sizes);
    }
}
