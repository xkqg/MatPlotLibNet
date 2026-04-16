// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="BarSeries"/> default properties and construction.</summary>
public class BarSeriesTests
{
    /// <summary>Verifies that the constructor stores categories and values.</summary>
    [Fact]
    public void Constructor_StoresData()
    {
        string[] cats = ["A", "B"];
        double[] vals = [1, 2];
        var series = new BarSeries(cats, vals);
        Assert.Equal(cats, series.Categories);
        Assert.Equal(vals, series.Values);
    }

    /// <summary>Verifies that Orientation defaults to Vertical.</summary>
    [Fact]
    public void DefaultOrientation_IsVertical()
    {
        var series = new BarSeries(["A"], [1.0]);
        Assert.Equal(BarOrientation.Vertical, series.Orientation);
    }

    /// <summary>Verifies that BarWidth defaults to 0.8.</summary>
    [Fact]
    public void DefaultBarWidth_Is0Point8()
    {
        var series = new BarSeries(["A"], [1.0]);
        Assert.Equal(0.8, series.BarWidth);
    }

    /// <summary>Verifies that Alpha defaults to 1.0.</summary>
    [Fact]
    public void DefaultAlpha_Is1()
    {
        var series = new BarSeries(["A"], [1.0]);
        Assert.Equal(1.0, series.Alpha);
    }

    /// <summary>Verifies that LineWidth defaults to 0.0.</summary>
    [Fact]
    public void DefaultLineWidth_Is0()
    {
        var series = new BarSeries(["A"], [1.0]);
        Assert.Equal(0.0, series.LineWidth);
    }

    /// <summary>Verifies that Align defaults to Center.</summary>
    [Fact]
    public void DefaultAlign_IsCenter()
    {
        var series = new BarSeries(["A"], [1.0]);
        Assert.Equal(BarAlignment.Center, series.Align);
    }

    /// <summary>Verifies that Alpha can be set.</summary>
    [Fact]
    public void Alpha_CanBeSet()
    {
        var series = new BarSeries(["A"], [1.0]);
        series.Alpha = 0.5;
        Assert.Equal(0.5, series.Alpha);
    }

    /// <summary>Verifies that Align can be set.</summary>
    [Fact]
    public void Align_CanBeSet()
    {
        var series = new BarSeries(["A"], [1.0]);
        series.Align = BarAlignment.Edge;
        Assert.Equal(BarAlignment.Edge, series.Align);
    }

    /// <summary>Verifies that LineWidth can be set.</summary>
    [Fact]
    public void LineWidth_CanBeSet()
    {
        var series = new BarSeries(["A"], [1.0]);
        series.LineWidth = 1.5;
        Assert.Equal(1.5, series.LineWidth);
    }

    /// <summary>Verifies that multiple BarSeries can share the same categories (prerequisite for grouped layout).</summary>
    [Fact]
    public void MultipleSeries_ShareCategories()
    {
        string[] cats = ["A", "B", "C"];
        var s1 = new BarSeries(cats, [1, 2, 3]);
        var s2 = new BarSeries(cats, [4, 5, 6]);
        Assert.Same(s1.Categories, s2.Categories);
        Assert.Equal(3, s1.Values.Length);
        Assert.Equal(3, s2.Values.Length);
    }
}
