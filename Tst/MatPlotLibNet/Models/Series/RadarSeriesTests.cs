// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Models.Series;

public class RadarSeriesTests
{
    [Fact]
    public void Constructor_StoresData()
    {
        string[] cats = ["A", "B", "C"];
        double[] vals = [1, 2, 3];
        var series = new RadarSeries(cats, vals);
        Assert.Equal(cats, series.Categories);
        Assert.Equal(vals, series.Values);
    }

    [Fact]
    public void DefaultAlpha_Is0Point25()
    {
        var series = new RadarSeries(["A"], [1.0]);
        Assert.Equal(0.25, series.Alpha);
    }

    [Fact]
    public void DefaultLineWidth_Is2()
    {
        var series = new RadarSeries(["A"], [1.0]);
        Assert.Equal(2.0, series.LineWidth);
    }

    [Fact]
    public void DefaultColor_IsNull()
    {
        var series = new RadarSeries(["A"], [1.0]);
        Assert.Null(series.Color);
    }

    [Fact]
    public void DefaultFillColor_IsNull()
    {
        var series = new RadarSeries(["A"], [1.0]);
        Assert.Null(series.FillColor);
    }

    [Fact]
    public void DefaultMaxValue_IsNull()
    {
        var series = new RadarSeries(["A"], [1.0]);
        Assert.Null(series.MaxValue);
    }
}
