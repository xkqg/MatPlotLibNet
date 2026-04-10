// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="ResidualSeries"/> default properties, construction, and serialization.</summary>
public class ResidualSeriesTests
{
    private static readonly double[] X = [1.0, 2.0, 3.0, 4.0, 5.0];
    private static readonly double[] Y = [2.1, 3.9, 6.1, 7.9, 10.1];

    [Fact]
    public void Constructor_StoresXAndY()
    {
        var series = new ResidualSeries(X, Y);
        Assert.Equal(X, (double[])series.XData);
        Assert.Equal(Y, (double[])series.YData);
    }

    [Fact]
    public void Degree_DefaultsTo1()
    {
        var series = new ResidualSeries(X, Y);
        Assert.Equal(1, series.Degree);
    }

    [Fact]
    public void MarkerSize_DefaultsTo6()
    {
        var series = new ResidualSeries(X, Y);
        Assert.Equal(6.0, series.MarkerSize);
    }

    [Fact]
    public void Color_DefaultsToNull()
    {
        var series = new ResidualSeries(X, Y);
        Assert.Null(series.Color);
    }

    [Fact]
    public void ShowZeroLine_DefaultsToTrue()
    {
        var series = new ResidualSeries(X, Y);
        Assert.True(series.ShowZeroLine);
    }

    [Fact]
    public void ToSeriesDto_ReturnsTypeResidual()
    {
        var series = new ResidualSeries(X, Y);
        Assert.Equal("residual", series.ToSeriesDto().Type);
    }

    [Fact]
    public void ToSeriesDto_IncludesDegree()
    {
        var series = new ResidualSeries(X, Y) { Degree = 2 };
        Assert.Equal(2, series.ToSeriesDto().Degree);
    }

    [Fact]
    public void Accept_DispatchesToVisitor()
    {
        var series = new ResidualSeries(X, Y);
        var visitor = new TestSeriesVisitor();
        series.Accept(visitor, null!);
        Assert.Equal(nameof(ResidualSeries), visitor.LastVisited);
    }
}
