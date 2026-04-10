// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="HexbinSeries"/> default properties and serialization.</summary>
public class HexbinSeriesTests
{
    /// <summary>Constructor stores X and Y data.</summary>
    [Fact]
    public void Constructor_StoresXAndYData()
    {
        double[] x = [1.0, 2.0];
        double[] y = [3.0, 4.0];
        var series = new HexbinSeries(x, y);
        Assert.Equal(x, series.X);
        Assert.Equal(y, series.Y);
    }

    /// <summary>GridSize defaults to 20.</summary>
    [Fact]
    public void GridSize_DefaultsTo20()
    {
        var series = new HexbinSeries([], []);
        Assert.Equal(20, series.GridSize);
    }

    /// <summary>MinCount defaults to 1.</summary>
    [Fact]
    public void MinCount_DefaultsTo1()
    {
        var series = new HexbinSeries([], []);
        Assert.Equal(1, series.MinCount);
    }

    /// <summary>ColorMap defaults to null.</summary>
    [Fact]
    public void ColorMap_DefaultsToNull()
    {
        var series = new HexbinSeries([], []);
        Assert.Null(series.ColorMap);
    }

    /// <summary>ToSeriesDto returns type "hexbin".</summary>
    [Fact]
    public void ToSeriesDto_ReturnsTypeHexbin()
    {
        var series = new HexbinSeries([1.0], [1.0]);
        Assert.Equal("hexbin", series.ToSeriesDto().Type);
    }

    /// <summary>Accept dispatches to the correct visitor method.</summary>
    [Fact]
    public void Accept_DispatchesToVisitor()
    {
        var series = new HexbinSeries([1.0, 2.0], [1.0, 2.0]);
        var visitor = new TestSeriesVisitor();
        series.Accept(visitor, null!);
        Assert.Equal(nameof(HexbinSeries), visitor.LastVisited);
    }
}
