// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="ViolinSeries"/> default properties and construction.</summary>
public class ViolinSeriesTests
{
    /// <summary>Verifies that the constructor stores datasets.</summary>
    [Fact]
    public void Constructor_StoresData()
    {
        double[][] datasets = [[1.0, 2.0, 3.0]];
        var series = new ViolinSeries(datasets);
        Assert.Equal(datasets, series.Datasets);
    }

    /// <summary>Verifies that Alpha defaults to 0.7.</summary>
    [Fact]
    public void DefaultAlpha_Is0Point7()
    {
        var series = new ViolinSeries([[1.0]]);
        Assert.Equal(0.7, series.Alpha);
    }

    /// <summary>Verifies that Color defaults to null.</summary>
    [Fact]
    public void DefaultColor_IsNull()
    {
        var series = new ViolinSeries([[1.0]]);
        Assert.Null(series.Color);
    }

    /// <summary>Verifies that ShowMeans defaults to false.</summary>
    [Fact]
    public void DefaultShowMeans_IsFalse()
    {
        var series = new ViolinSeries([[1.0]]);
        Assert.False(series.ShowMeans);
    }

    /// <summary>Verifies that ShowMedians defaults to false.</summary>
    [Fact]
    public void DefaultShowMedians_IsFalse()
    {
        var series = new ViolinSeries([[1.0]]);
        Assert.False(series.ShowMedians);
    }

    /// <summary>Verifies that ShowExtrema defaults to true.</summary>
    [Fact]
    public void DefaultShowExtrema_IsTrue()
    {
        var series = new ViolinSeries([[1.0]]);
        Assert.True(series.ShowExtrema);
    }

    /// <summary>Verifies that Widths defaults to 0.5.</summary>
    [Fact]
    public void DefaultWidths_Is0Point5()
    {
        var series = new ViolinSeries([[1.0]]);
        Assert.Equal(0.5, series.Widths);
    }

    /// <summary>Verifies that Side defaults to ViolinSide.Both.</summary>
    [Fact]
    public void DefaultSide_IsBoth()
    {
        var series = new ViolinSeries([[1.0]]);
        Assert.Equal(ViolinSide.Both, series.Side);
    }

    /// <summary>Verifies that Positions defaults to null.</summary>
    [Fact]
    public void DefaultPositions_IsNull()
    {
        var series = new ViolinSeries([[1.0]]);
        Assert.Null(series.Positions);
    }

    /// <summary>Verifies that Side can be set to Low.</summary>
    [Fact]
    public void Side_CanBeSet()
    {
        var series = new ViolinSeries([[1.0]]) { Side = ViolinSide.Low };
        Assert.Equal(ViolinSide.Low, series.Side);
    }
}
