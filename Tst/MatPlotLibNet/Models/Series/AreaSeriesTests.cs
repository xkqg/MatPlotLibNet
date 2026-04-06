// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models.Series;

public class AreaSeriesTests
{
    [Fact]
    public void Constructor_StoresData()
    {
        double[] x = [1, 2, 3], y = [4, 5, 6];
        var series = new AreaSeries(x, y);
        Assert.Equal(x, series.XData);
        Assert.Equal(y, series.YData);
    }

    [Fact]
    public void YData2_DefaultsToNull()
    {
        var series = new AreaSeries([1.0], [2.0]);
        Assert.Null(series.YData2);
    }

    [Fact]
    public void DefaultColor_IsNull()
    {
        var series = new AreaSeries([1.0], [2.0]);
        Assert.Null(series.Color);
    }

    [Fact]
    public void DefaultAlpha_Is0Point3()
    {
        var series = new AreaSeries([1.0], [2.0]);
        Assert.Equal(0.3, series.Alpha);
    }

    [Fact]
    public void DefaultLineWidth_Is1Point5()
    {
        var series = new AreaSeries([1.0], [2.0]);
        Assert.Equal(1.5, series.LineWidth);
    }

    [Fact]
    public void DefaultLineStyle_IsSolid()
    {
        var series = new AreaSeries([1.0], [2.0]);
        Assert.Equal(LineStyle.Solid, series.LineStyle);
    }

    [Fact]
    public void DefaultFillColor_IsNull()
    {
        var series = new AreaSeries([1.0], [2.0]);
        Assert.Null(series.FillColor);
    }
}
