// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="KdeSeries"/> default properties, construction, and serialization.</summary>
public class KdeSeriesTests
{
    /// <summary>Constructor stores the data array.</summary>
    [Fact]
    public void Constructor_StoresData()
    {
        double[] data = [1.0, 2.0, 3.0];
        var series = new KdeSeries(data);
        Assert.Equal(data, series.Data);
    }

    /// <summary>Bandwidth defaults to null (auto Silverman).</summary>
    [Fact]
    public void Bandwidth_DefaultsToNull()
    {
        var series = new KdeSeries([1.0, 2.0, 3.0]);
        Assert.Null(series.Bandwidth);
    }

    /// <summary>Fill defaults to true.</summary>
    [Fact]
    public void Fill_DefaultsToTrue()
    {
        var series = new KdeSeries([1.0]);
        Assert.True(series.Fill);
    }

    /// <summary>Alpha defaults to 0.3.</summary>
    [Fact]
    public void Alpha_DefaultsTo0p3()
    {
        var series = new KdeSeries([1.0]);
        Assert.Equal(0.3, series.Alpha);
    }

    /// <summary>LineWidth defaults to 1.5.</summary>
    [Fact]
    public void LineWidth_DefaultsTo1p5()
    {
        var series = new KdeSeries([1.0]);
        Assert.Equal(1.5, series.LineWidth);
    }

    /// <summary>LineStyle defaults to Solid.</summary>
    [Fact]
    public void LineStyle_DefaultsToSolid()
    {
        var series = new KdeSeries([1.0]);
        Assert.Equal(LineStyle.Solid, series.LineStyle);
    }

    /// <summary>Color defaults to null.</summary>
    [Fact]
    public void Color_DefaultsToNull()
    {
        var series = new KdeSeries([1.0]);
        Assert.Null(series.Color);
    }

    /// <summary>Bandwidth can be set and read back.</summary>
    [Fact]
    public void Bandwidth_CanBeSet()
    {
        var series = new KdeSeries([1.0, 2.0, 3.0]) { Bandwidth = 0.5 };
        Assert.Equal(0.5, series.Bandwidth);
    }

    /// <summary>ToSeriesDto returns type "kde".</summary>
    [Fact]
    public void ToSeriesDto_ReturnsTypeKde()
    {
        var series = new KdeSeries([1.0, 2.0]);
        Assert.Equal("kde", series.ToSeriesDto().Type);
    }

    /// <summary>ToSeriesDto includes Data array.</summary>
    [Fact]
    public void ToSeriesDto_IncludesData()
    {
        double[] data = [1.0, 2.0, 3.0];
        var series = new KdeSeries(data);
        Assert.Equal(data, series.ToSeriesDto().Data);
    }

    /// <summary>ToSeriesDto serializes Bandwidth when set.</summary>
    [Fact]
    public void ToSeriesDto_IncludesBandwidthWhenSet()
    {
        var series = new KdeSeries([1.0, 2.0]) { Bandwidth = 0.7 };
        Assert.Equal(0.7, series.ToSeriesDto().Bandwidth);
    }

    /// <summary>Accept dispatches to the correct visitor method.</summary>
    [Fact]
    public void Accept_DispatchesToVisitor()
    {
        var series = new KdeSeries([1.0, 2.0, 3.0]);
        var visitor = new TestSeriesVisitor();
        series.Accept(visitor, null!);
        Assert.Equal(nameof(KdeSeries), visitor.LastVisited);
    }
}
