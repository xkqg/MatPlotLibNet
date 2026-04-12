// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="StemSeries"/> default properties and construction.</summary>
public class StemSeriesTests
{
    /// <summary>Verifies that the constructor stores X and Y data.</summary>
    [Fact]
    public void Constructor_StoresData()
    {
        double[] x = [1.0, 2.0], y = [3.0, 4.0];
        var series = new StemSeries(x, y);
        Assert.Equal(x, series.XData);
        Assert.Equal(y, series.YData);
    }

    /// <summary>Verifies that Marker defaults to Circle.</summary>
    [Fact]
    public void DefaultMarker_IsCircle()
    {
        var series = new StemSeries([1.0], [2.0]);
        Assert.Equal(MarkerStyle.Circle, series.Marker);
    }

    /// <summary>Verifies that MarkerColor defaults to null.</summary>
    [Fact]
    public void DefaultMarkerColor_IsNull()
    {
        var series = new StemSeries([1.0], [2.0]);
        Assert.Null(series.MarkerColor);
    }

    /// <summary>Verifies that StemColor defaults to null.</summary>
    [Fact]
    public void DefaultStemColor_IsNull()
    {
        var series = new StemSeries([1.0], [2.0]);
        Assert.Null(series.StemColor);
    }
}
