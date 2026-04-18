// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Models.Series;

public class BarbsSeriesTests
{
    private static readonly double[] X = [0.0, 1.0, 2.0];
    private static readonly double[] Y = [0.0, 0.0, 0.0];
    private static readonly double[] Speed = [10.0, 20.0, 50.0];
    private static readonly double[] Dir = [0.0, 90.0, 180.0];

    [Fact] public void Constructor_StoresXYSpeedDirection()
    {
        var s = new BarbsSeries(X, Y, Speed, Dir);
        Assert.Equal(X, (double[])s.X); Assert.Equal(Y, (double[])s.Y);
        Assert.Equal(Speed, (double[])s.Speed); Assert.Equal(Dir, (double[])s.Direction);
    }

    [Fact]
    public void ToSeriesDto_IncludesSpeedAndDirection()
    {
        var s = new BarbsSeries(X, Y, Speed, Dir);
        var dto = s.ToSeriesDto();
        Assert.Equal(Speed, dto.Speed);
        Assert.Equal(Dir, dto.Direction);
    }
}
