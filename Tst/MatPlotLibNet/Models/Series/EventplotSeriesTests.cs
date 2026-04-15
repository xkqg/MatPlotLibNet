// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="EventplotSeries"/> default properties, construction, and serialization.</summary>
public class EventplotSeriesTests
{
    [Fact]
    public void Constructor_StoresPositions()
    {
        double[][] pos = [[1.0, 2.0], [3.0, 4.0]];
        var series = new EventplotSeries(pos);
        Assert.Equal(pos, series.Positions);
    }

    [Fact]
    public void LineWidth_DefaultsTo1()
    {
        var series = new EventplotSeries([]);
        Assert.Equal(1.0, series.LineWidth);
    }

    [Fact]
    public void LineLength_DefaultsTo1()
    {
        var series = new EventplotSeries([]);
        Assert.Equal(1.0, series.LineLength);
    }

    [Fact]
    public void Colors_DefaultsToNull()
    {
        var series = new EventplotSeries([]);
        Assert.Null(series.Colors);
    }

    [Fact]
    public void ToSeriesDto_ReturnsTypeEventplot()
    {
        var series = new EventplotSeries([[1.0, 2.0]]);
        Assert.Equal("eventplot", series.ToSeriesDto().Type);
    }

    [Fact]
    public void ToSeriesDto_IncludesEventPositions()
    {
        double[][] pos = [[1.0, 2.0]];
        var series = new EventplotSeries(pos);
        Assert.Equal(pos, series.ToSeriesDto().EventPositions);
    }

    [Fact]
    public void Accept_DispatchesToVisitor()
    {
        var series = new EventplotSeries([[1.0]]);
        var visitor = new TestSeriesVisitor();
        series.Accept(visitor, null!);
        Assert.Equal(nameof(EventplotSeries), visitor.LastVisited);
    }
}
