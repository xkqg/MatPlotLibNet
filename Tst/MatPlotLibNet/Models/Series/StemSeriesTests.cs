// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
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

    /// <summary>The stems stand on the baseline at 0, so the baseline is a sticky edge of the Y range. Along X nothing is
    /// sticky: the first and last stem are marks like any other and keep the axis margin, as matplotlib's stem does (it
    /// sets no sticky edge at all). Before, X was sticky at both ends of the data, so the first and last head stood on the
    /// plot's left and right edge, half of each outside the frame.</summary>
    [Fact]
    public void TheRange_StaysStickyOnTheBaselineOnly()
    {
        var axes = new Axes();

        var range = new StemSeries([1.0, 2.0, 3.0], [1.0, 2.0, 3.0]).ComputeDataRange(new AxesContextAdapter(axes));

        Assert.Equal(1.0, range.XMin);
        Assert.Equal(3.0, range.XMax);
        Assert.Null(range.StickyXMin);
        Assert.Null(range.StickyXMax);
        Assert.Equal(0.0, range.StickyYMin);
        Assert.Null(range.StickyYMax);
    }
}
