// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="BrokenBarSeries"/> default properties, construction, and serialization.</summary>
public class BrokenBarSeriesTests
{
    [Fact]
    public void Constructor_StoresRanges()
    {
        var ranges = new (double, double)[][] { [(1.0, 2.0), (4.0, 1.0)] };
        var series = new BrokenBarSeries(ranges);
        Assert.Equal(ranges, series.Ranges);
    }

    [Fact]
    public void BarHeight_DefaultsTo0p8()
    {
        var series = new BrokenBarSeries([]);
        Assert.Equal(0.8, series.BarHeight);
    }

    [Fact]
    public void Labels_DefaultsToNull()
    {
        var series = new BrokenBarSeries([]);
        Assert.Null(series.Labels);
    }

    [Fact]
    public void ToSeriesDto_ReturnsTypeBrokenbar()
    {
        var series = new BrokenBarSeries([[(1.0, 2.0)]]);
        Assert.Equal("brokenbar", series.ToSeriesDto().Type);
    }

    [Fact]
    public void ToSeriesDto_IncludesRangeStartsAndWidths()
    {
        var ranges = new (double, double)[][] { [(1.0, 2.0), (5.0, 3.0)] };
        var series = new BrokenBarSeries(ranges);
        var dto = series.ToSeriesDto();
        Assert.NotNull(dto.RangeStarts);
        Assert.NotNull(dto.RangeWidths);
        Assert.Equal([1.0, 5.0], dto.RangeStarts![0]);
        Assert.Equal([2.0, 3.0], dto.RangeWidths![0]);
    }

}
