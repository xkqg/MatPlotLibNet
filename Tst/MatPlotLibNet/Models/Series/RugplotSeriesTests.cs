// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="RugplotSeries"/> default properties, construction, and serialization.</summary>
public class RugplotSeriesTests
{
    private static readonly double[] Data = [1.0, 2.0, 3.0];
    private static readonly double[] Single = [1.0];

    [Fact]
    public void Constructor_StoresData()
    {
        var series = new RugplotSeries(Data);
        Assert.Equal((double[])series.Data, Data);
    }

    [Fact]
    public void Height_DefaultsTo0p05()
    {
        var series = new RugplotSeries(Single);
        Assert.Equal(0.05, series.Height);
    }

    [Fact]
    public void Alpha_DefaultsTo0p5()
    {
        var series = new RugplotSeries(Single);
        Assert.Equal(0.5, series.Alpha);
    }

    [Fact]
    public void LineWidth_DefaultsTo1()
    {
        var series = new RugplotSeries(Single);
        Assert.Equal(1.0, series.LineWidth);
    }

    [Fact]
    public void Color_DefaultsToNull()
    {
        var series = new RugplotSeries(Single);
        Assert.Null(series.Color);
    }

    [Fact]
    public void ToSeriesDto_ReturnsTypeRugplot()
    {
        var series = new RugplotSeries(Data);
        Assert.Equal("rugplot", series.ToSeriesDto().Type);
    }

    [Fact]
    public void ToSeriesDto_IncludesHeight()
    {
        var series = new RugplotSeries(Single) { Height = 0.1 };
        Assert.Equal(0.1, series.ToSeriesDto().RugHeight);
    }

    [Fact]
    public void Accept_DispatchesToVisitor()
    {
        var series = new RugplotSeries(Data);
        var visitor = new TestSeriesVisitor();
        series.Accept(visitor, null!);
        Assert.Equal(nameof(RugplotSeries), visitor.LastVisited);
    }
}
