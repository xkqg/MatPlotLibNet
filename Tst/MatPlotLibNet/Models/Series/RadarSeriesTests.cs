// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="RadarSeries"/> default properties and construction.</summary>
public class RadarSeriesTests
{
    /// <summary>Verifies that the constructor stores categories and values.</summary>
    [Fact]
    public void Constructor_StoresData()
    {
        string[] cats = ["A", "B", "C"];
        double[] vals = [1, 2, 3];
        var series = new RadarSeries(cats, vals);
        Assert.Equal(cats, series.Categories);
        Assert.Equal(vals, series.Values);
    }

    /// <summary>Verifies that Alpha defaults to 0.25.</summary>
    [Fact]
    public void DefaultAlpha_Is0Point25()
    {
        var series = new RadarSeries(["A"], [1.0]);
        Assert.Equal(0.25, series.Alpha);
    }

    /// <summary>Verifies that LineWidth defaults to 2.</summary>
    [Fact]
    public void DefaultLineWidth_Is2()
    {
        var series = new RadarSeries(["A"], [1.0]);
        Assert.Equal(2.0, series.LineWidth);
    }

    /// <summary>Verifies that FillColor defaults to null.</summary>
    [Fact]
    public void DefaultFillColor_IsNull()
    {
        var series = new RadarSeries(["A"], [1.0]);
        Assert.Null(series.FillColor);
    }

    /// <summary>Verifies that MaxValue defaults to null.</summary>
    [Fact]
    public void DefaultMaxValue_IsNull()
    {
        var series = new RadarSeries(["A"], [1.0]);
        Assert.Null(series.MaxValue);
    }
}
