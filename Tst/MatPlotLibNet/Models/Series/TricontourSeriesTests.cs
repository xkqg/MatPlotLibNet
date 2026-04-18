// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Models.Series;

public class TricontourSeriesTests
{
    private static readonly double[] X = [0.0, 1.0, 0.5, 1.0, 0.0];
    private static readonly double[] Y = [0.0, 0.0, 0.5, 1.0, 1.0];
    private static readonly double[] Z = [1.0, 2.0, 3.0, 2.0, 1.0];

    [Fact] public void Constructor_StoresXYZ()
    {
        var s = new TricontourSeries(X, Y, Z);
        Assert.Equal(X, (double[])s.X); Assert.Equal(Y, (double[])s.Y); Assert.Equal(Z, (double[])s.Z);
    }

    [Fact]
    public void ToSeriesDto_IncludesLevels()
    {
        var s = new TricontourSeries(X, Y, Z) { Levels = 5 };
        Assert.Equal(5, s.ToSeriesDto().Levels);
    }
}
