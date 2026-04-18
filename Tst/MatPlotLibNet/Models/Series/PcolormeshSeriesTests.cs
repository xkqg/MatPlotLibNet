// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="PcolormeshSeries"/> default properties, construction, and serialization.</summary>
public class PcolormeshSeriesTests
{
    private static readonly double[] X = [0.0, 1.0, 2.0]; // M+1=3, M=2 cols
    private static readonly double[] Y = [0.0, 1.0, 2.0]; // N+1=3, N=2 rows
    private static readonly double[,] C = { { 1.0, 2.0 }, { 3.0, 4.0 } };

    [Fact]
    public void Constructor_StoresXYC()
    {
        var series = new PcolormeshSeries(X, Y, C);
        Assert.Equal((double[])series.X, X);
        Assert.Equal((double[])series.Y, Y);
        Assert.Equal(C, series.C);
    }

    [Fact]
    public void Normalizer_DefaultsToNull()
    {
        var series = new PcolormeshSeries(X, Y, C);
        Assert.Null(series.Normalizer);
    }

    [Fact]
    public void GetColorBarRange_ReturnsMinMax()
    {
        var series = new PcolormeshSeries(X, Y, C);
        var (min, max) = series.GetColorBarRange();
        Assert.Equal(1.0, min);
        Assert.Equal(4.0, max);
    }

    [Fact]
    public void ToSeriesDto_ReturnsTypePcolormesh()
    {
        var series = new PcolormeshSeries(X, Y, C);
        Assert.Equal("pcolormesh", series.ToSeriesDto().Type);
    }

}
