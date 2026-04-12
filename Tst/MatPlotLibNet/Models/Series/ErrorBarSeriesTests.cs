// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="ErrorBarSeries"/> default properties and construction.</summary>
public class ErrorBarSeriesTests
{
    /// <summary>Verifies that the constructor stores X, Y, and error data.</summary>
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

    /// <summary>Verifies that XErrorLow defaults to null.</summary>
    [Fact]
    public void XErrorLow_DefaultsToNull()
    {
        var series = new ErrorBarSeries([1.0], [2.0], [0.1], [0.1]);
        Assert.Null(series.XErrorLow);
    }

    /// <summary>Verifies that XErrorHigh defaults to null.</summary>
    [Fact]
    public void XErrorHigh_DefaultsToNull()
    {
        var series = new ErrorBarSeries([1.0], [2.0], [0.1], [0.1]);
        Assert.Null(series.XErrorHigh);
    }

    /// <summary>Verifies that CapSize defaults to 5.</summary>
    [Fact]
    public void DefaultCapSize_Is5()
    {
        var series = new ErrorBarSeries([1.0], [2.0], [0.1], [0.1]);
        Assert.Equal(5.0, series.CapSize);
    }

    /// <summary>Verifies that LineWidth defaults to 1.5.</summary>
    [Fact]
    public void DefaultLineWidth_Is1Point5()
    {
        var series = new ErrorBarSeries([1.0], [2.0], [0.1], [0.1]);
        Assert.Equal(1.5, series.LineWidth);
    }

    /// <summary>Verifies that Color defaults to null.</summary>
    [Fact]
    public void DefaultColor_IsNull()
    {
        var series = new ErrorBarSeries([1.0], [2.0], [0.1], [0.1]);
        Assert.Null(series.Color);
    }

    /// <summary>Verifies that ELineWidth defaults to null.</summary>
    [Fact]
    public void DefaultELineWidth_IsNull()
    {
        var series = new ErrorBarSeries([1.0], [2.0], [0.1], [0.1]);
        Assert.Null(series.ELineWidth);
    }

    /// <summary>Verifies that CapThick defaults to null.</summary>
    [Fact]
    public void DefaultCapThick_IsNull()
    {
        var series = new ErrorBarSeries([1.0], [2.0], [0.1], [0.1]);
        Assert.Null(series.CapThick);
    }

    /// <summary>Verifies that ErrorEvery defaults to 1.</summary>
    [Fact]
    public void DefaultErrorEvery_Is1()
    {
        var series = new ErrorBarSeries([1.0], [2.0], [0.1], [0.1]);
        Assert.Equal(1, series.ErrorEvery);
    }

    /// <summary>Verifies that ErrorEvery can be set.</summary>
    [Fact]
    public void ErrorEvery_CanBeSet()
    {
        var series = new ErrorBarSeries([1.0, 2.0, 3.0], [4.0, 5.0, 6.0], [0.1, 0.1, 0.1], [0.1, 0.1, 0.1])
        {
            ErrorEvery = 2
        };
        Assert.Equal(2, series.ErrorEvery);
    }
}
