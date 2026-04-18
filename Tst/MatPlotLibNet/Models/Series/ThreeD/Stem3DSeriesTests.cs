// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

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

}
