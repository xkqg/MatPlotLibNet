// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Models.Series;

public class BoxSeriesTests
{
    [Fact]
    public void Constructor_StoresData()
    {
        double[][] datasets = [[1.0, 2.0, 3.0]];
        var series = new BoxSeries(datasets);
        Assert.Equal(datasets, series.Datasets);
    }

    [Fact]
    public void DefaultShowOutliers_IsTrue()
    {
        var series = new BoxSeries([[1.0]]);
        Assert.True(series.ShowOutliers);
    }

    [Fact]
    public void DefaultColor_IsNull()
    {
        var series = new BoxSeries([[1.0]]);
        Assert.Null(series.Color);
    }

    [Fact]
    public void Accept_CallsCorrectVisitorMethod()
    {
        var series = new BoxSeries([[1.0]]);
        var visitor = new TestSeriesVisitor();
        series.Accept(visitor, null!);
        Assert.Equal(nameof(BoxSeries), visitor.LastVisited);
    }
}
