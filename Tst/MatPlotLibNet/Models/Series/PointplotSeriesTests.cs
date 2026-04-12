// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="PointplotSeries"/> default properties, construction, and serialization.</summary>
public class PointplotSeriesTests
{
    [Fact]
    public void Constructor_StoresDatasets()
    {
        double[][] data = [[1.0, 2.0], [3.0, 4.0]];
        var series = new PointplotSeries(data);
        Assert.Equal(data, series.Datasets);
    }

    [Fact]
    public void MarkerSize_DefaultsTo8()
    {
        var series = new PointplotSeries([]);
        Assert.Equal(8.0, series.MarkerSize);
    }

    [Fact]
    public void CapSize_DefaultsTo0p2()
    {
        var series = new PointplotSeries([]);
        Assert.Equal(0.2, series.CapSize);
    }

    [Fact]
    public void ConfidenceLevel_DefaultsTo0p95()
    {
        var series = new PointplotSeries([]);
        Assert.Equal(0.95, series.ConfidenceLevel);
    }

    [Fact]
    public void Color_DefaultsToNull()
    {
        var series = new PointplotSeries([]);
        Assert.Null(series.Color);
    }

    [Fact]
    public void ToSeriesDto_ReturnsTypePointplot()
    {
        var series = new PointplotSeries([[1.0, 2.0]]);
        Assert.Equal("pointplot", series.ToSeriesDto().Type);
    }

    [Fact]
    public void Accept_DispatchesToVisitor()
    {
        var series = new PointplotSeries([[1.0, 2.0]]);
        var visitor = new TestSeriesVisitor();
        series.Accept(visitor, null!);
        Assert.Equal(nameof(PointplotSeries), visitor.LastVisited);
    }
}
