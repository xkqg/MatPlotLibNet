// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="Bar3DSeries"/> default properties and construction.</summary>
public class Bar3DSeriesTests
{
    private static readonly double[] X = [0.0, 1.0];
    private static readonly double[] Y = [0.0, 0.0];
    private static readonly double[] Z = [3.0, 5.0];
    private static readonly double[] S = [1.0];

    [Fact]
    public void Constructor_StoresXYZ()
    {
        var s = new Bar3DSeries(X, Y, Z);
        Assert.Equal(X, (double[])s.X);
        Assert.Equal(Y, (double[])s.Y);
        Assert.Equal(Z, (double[])s.Z);
    }

    [Fact] public void BarWidth_DefaultsTo0Point5() => Assert.Equal(0.5, new Bar3DSeries(S, S, S).BarWidth);
    [Fact] public void Color_DefaultsToNull() => Assert.Null(new Bar3DSeries(S, S, S).Color);
    [Fact] public void ToSeriesDto_ReturnsTypeBar3d() => Assert.Equal("bar3d", new Bar3DSeries(S, S, S).ToSeriesDto().Type);

    [Fact]
    public void Accept_DispatchesToVisitor()
    {
        var s = new Bar3DSeries(S, S, S);
        var v = new TestSeriesVisitor();
        s.Accept(v, null!);
        Assert.Equal(nameof(Bar3DSeries), v.LastVisited);
    }
}
