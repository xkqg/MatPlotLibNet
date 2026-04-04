// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Models.Series;

public class ViolinSeriesTests
{
    [Fact]
    public void Constructor_StoresData()
    {
        double[][] datasets = [[1.0, 2.0, 3.0]];
        var series = new ViolinSeries(datasets);
        Assert.Equal(datasets, series.Datasets);
    }

    [Fact]
    public void DefaultAlpha_Is0Point7()
    {
        var series = new ViolinSeries([[1.0]]);
        Assert.Equal(0.7, series.Alpha);
    }

    [Fact]
    public void DefaultColor_IsNull()
    {
        var series = new ViolinSeries([[1.0]]);
        Assert.Null(series.Color);
    }

    [Fact]
    public void Accept_CallsCorrectVisitorMethod()
    {
        var series = new ViolinSeries([[1.0]]);
        var visitor = new TestSeriesVisitor();
        series.Accept(visitor, null!);
        Assert.Equal(nameof(ViolinSeries), visitor.LastVisited);
    }
}
