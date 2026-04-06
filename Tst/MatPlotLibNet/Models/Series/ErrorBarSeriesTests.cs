// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Models.Series;

public class ErrorBarSeriesTests
{
    [Fact]
    public void Constructor_StoresData()
    {
        double[] x = [1, 2, 3], y = [4, 5, 6], eLow = [0.5, 0.5, 0.5], eHigh = [1, 1, 1];
        var series = new ErrorBarSeries(x, y, eLow, eHigh);
        Assert.Equal(x, series.XData);
        Assert.Equal(y, series.YData);
        Assert.Equal(eLow, series.YErrorLow);
        Assert.Equal(eHigh, series.YErrorHigh);
    }

    [Fact]
    public void XErrorLow_DefaultsToNull()
    {
        var series = new ErrorBarSeries([1.0], [2.0], [0.1], [0.1]);
        Assert.Null(series.XErrorLow);
    }

    [Fact]
    public void XErrorHigh_DefaultsToNull()
    {
        var series = new ErrorBarSeries([1.0], [2.0], [0.1], [0.1]);
        Assert.Null(series.XErrorHigh);
    }

    [Fact]
    public void DefaultCapSize_Is5()
    {
        var series = new ErrorBarSeries([1.0], [2.0], [0.1], [0.1]);
        Assert.Equal(5.0, series.CapSize);
    }

    [Fact]
    public void DefaultLineWidth_Is1Point5()
    {
        var series = new ErrorBarSeries([1.0], [2.0], [0.1], [0.1]);
        Assert.Equal(1.5, series.LineWidth);
    }

    [Fact]
    public void DefaultColor_IsNull()
    {
        var series = new ErrorBarSeries([1.0], [2.0], [0.1], [0.1]);
        Assert.Null(series.Color);
    }
}
