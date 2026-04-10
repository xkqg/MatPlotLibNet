// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="CountSeries"/> default properties, construction, and serialization.</summary>
public class CountSeriesTests
{
    [Fact]
    public void Constructor_StoresValues()
    {
        string[] values = ["a", "b", "a", "c"];
        var series = new CountSeries(values);
        Assert.Equal(values, series.Values);
    }

    [Fact]
    public void BarWidth_DefaultsTo0p8()
    {
        var series = new CountSeries([]);
        Assert.Equal(0.8, series.BarWidth);
    }

    [Fact]
    public void Color_DefaultsToNull()
    {
        var series = new CountSeries([]);
        Assert.Null(series.Color);
    }

    [Fact]
    public void Orientation_DefaultsToVertical()
    {
        var series = new CountSeries([]);
        Assert.Equal(BarOrientation.Vertical, series.Orientation);
    }

    [Fact]
    public void ComputeCounts_ReturnsCorrectFrequencies()
    {
        var series = new CountSeries(["a", "b", "a", "c", "a"]);
        var counts = series.ComputeCounts();
        Assert.Equal(3, counts["a"]);
        Assert.Equal(1, counts["b"]);
        Assert.Equal(1, counts["c"]);
    }

    [Fact]
    public void ToSeriesDto_ReturnsTypeCount()
    {
        var series = new CountSeries(["a", "b"]);
        Assert.Equal("count", series.ToSeriesDto().Type);
    }

    [Fact]
    public void Accept_DispatchesToVisitor()
    {
        var series = new CountSeries(["a", "b"]);
        var visitor = new TestSeriesVisitor();
        series.Accept(visitor, null!);
        Assert.Equal(nameof(CountSeries), visitor.LastVisited);
    }
}
