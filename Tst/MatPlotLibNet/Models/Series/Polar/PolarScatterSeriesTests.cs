// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="PolarScatterSeries"/> default properties and construction.</summary>
public class PolarScatterSeriesTests
{
    /// <summary>Verifies that the constructor stores R and Theta data.</summary>
    [Fact]
    public void Constructor_StoresData()
    {
        double[] r = [1, 2], theta = [0, 1];
        var series = new PolarScatterSeries(r, theta);
        Assert.Equal(r, series.R);
        Assert.Equal(theta, series.Theta);
    }

    /// <summary>Verifies that MarkerSize defaults to 6.</summary>
    [Fact]
    public void DefaultMarkerSize_Is6()
    {
        var series = new PolarScatterSeries([1.0], [0.0]);
        Assert.Equal(6, series.MarkerSize);
    }

    /// <summary>Verifies that Color defaults to null.</summary>
    [Fact]
    public void DefaultColor_IsNull()
    {
        var series = new PolarScatterSeries([1.0], [0.0]);
        Assert.Null(series.Color);
    }
}
