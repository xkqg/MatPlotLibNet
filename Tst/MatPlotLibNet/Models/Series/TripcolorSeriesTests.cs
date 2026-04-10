// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Models.Series;

public class TripcolorSeriesTests
{
    private static readonly double[] X = [0.0, 1.0, 0.5, 1.0, 0.0];
    private static readonly double[] Y = [0.0, 0.0, 0.5, 1.0, 1.0];
    private static readonly double[] Z = [1.0, 2.0, 3.0, 2.0, 1.0];

    [Fact] public void Constructor_StoresXYZ()
    {
        var s = new TripcolorSeries(X, Y, Z);
        Assert.Equal(X, (double[])s.X); Assert.Equal(Y, (double[])s.Y); Assert.Equal(Z, (double[])s.Z);
    }

    [Fact] public void Triangles_DefaultsToNull() => Assert.Null(new TripcolorSeries(X, Y, Z).Triangles);
    [Fact] public void ColorMap_DefaultsToNull() => Assert.Null(new TripcolorSeries(X, Y, Z).ColorMap);
    [Fact] public void ToSeriesDto_ReturnsTypeTripcolor() => Assert.Equal("tripcolor", new TripcolorSeries(X, Y, Z).ToSeriesDto().Type);

    [Fact]
    public void Accept_DispatchesToVisitor()
    {
        var s = new TripcolorSeries(X, Y, Z);
        var v = new TestSeriesVisitor();
        s.Accept(v, null!);
        Assert.Equal(nameof(TripcolorSeries), v.LastVisited);
    }

    [Fact]
    public void ToSeriesDto_IncludesZData()
    {
        var s = new TripcolorSeries(X, Y, Z);
        Assert.Equal(Z, s.ToSeriesDto().ZData);
    }

    [Fact]
    public void ToSeriesDto_IncludesTrianglesWhenSet()
    {
        var s = new TripcolorSeries(X, Y, Z) { Triangles = [0, 1, 2] };
        Assert.Equal([0, 1, 2], s.ToSeriesDto().Triangles);
    }
}
