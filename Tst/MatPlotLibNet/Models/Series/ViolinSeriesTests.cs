// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

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
}
