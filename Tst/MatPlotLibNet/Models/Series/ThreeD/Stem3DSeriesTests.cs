// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="Stem3DSeries"/> default properties and construction.</summary>
public class Stem3DSeriesTests
{
    private static readonly double[] X = [1.0, 2.0];
    private static readonly double[] Y = [3.0, 4.0];
    private static readonly double[] Z = [5.0, 6.0];
    private static readonly double[] S = [1.0];

    [Fact]
    public void Constructor_StoresXYZ()
    {
        var s = new Stem3DSeries(X, Y, Z);
        Assert.Equal(X, (double[])s.X);
        Assert.Equal(Y, (double[])s.Y);
        Assert.Equal(Z, (double[])s.Z);
    }

    [Fact] public void MarkerSize_DefaultsTo6() => Assert.Equal(6, new Stem3DSeries(S, S, S).MarkerSize);
    [Fact] public void Color_DefaultsToNull() => Assert.Null(new Stem3DSeries(S, S, S).Color);
    [Fact] public void ToSeriesDto_ReturnsTypeStem3d() => Assert.Equal("stem3d", new Stem3DSeries(S, S, S).ToSeriesDto().Type);

    [Fact]
    public void Accept_DispatchesToVisitor()
    {
        var s = new Stem3DSeries(S, S, S);
        var v = new TestSeriesVisitor();
        s.Accept(v, null!);
        Assert.Equal(nameof(Stem3DSeries), v.LastVisited);
    }
}
