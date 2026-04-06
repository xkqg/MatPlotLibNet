// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="AreaSeries"/> default properties and construction.</summary>
public class AreaSeriesTests
{
    /// <summary>Verifies that the constructor stores X and Y data.</summary>
    [Fact]
    public void Constructor_StoresData()
    {
        double[] x = [1, 2, 3], y = [4, 5, 6];
        var series = new AreaSeries(x, y);
        Assert.Equal(x, series.XData);
        Assert.Equal(y, series.YData);
    }

    /// <summary>Verifies that YData2 defaults to null.</summary>
    [Fact]
    public void YData2_DefaultsToNull()
    {
        var series = new AreaSeries([1.0], [2.0]);
        Assert.Null(series.YData2);
    }

    /// <summary>Verifies that Color defaults to null.</summary>
    [Fact]
    public void DefaultColor_IsNull()
    {
        var series = new AreaSeries([1.0], [2.0]);
        Assert.Null(series.Color);
    }

    /// <summary>Verifies that Alpha defaults to 0.3.</summary>
    [Fact]
    public void DefaultAlpha_Is0Point3()
    {
        var series = new AreaSeries([1.0], [2.0]);
        Assert.Equal(0.3, series.Alpha);
    }

    /// <summary>Verifies that LineWidth defaults to 1.5.</summary>
    [Fact]
    public void DefaultLineWidth_Is1Point5()
    {
        var series = new AreaSeries([1.0], [2.0]);
        Assert.Equal(1.5, series.LineWidth);
    }

    /// <summary>Verifies that LineStyle defaults to Solid.</summary>
    [Fact]
    public void DefaultLineStyle_IsSolid()
    {
        var series = new AreaSeries([1.0], [2.0]);
        Assert.Equal(LineStyle.Solid, series.LineStyle);
    }

    /// <summary>Verifies that FillColor defaults to null.</summary>
    [Fact]
    public void DefaultFillColor_IsNull()
    {
        var series = new AreaSeries([1.0], [2.0]);
        Assert.Null(series.FillColor);
    }
}
