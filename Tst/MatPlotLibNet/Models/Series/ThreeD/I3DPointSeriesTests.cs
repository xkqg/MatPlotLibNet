// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="I3DPointSeries"/>: a 3-D point series hands its coordinates out through the interface, a quiver directly
/// and every other one through the <see cref="XYZSeries"/> base. The library ranges each from its own extent, so this is the reader the
/// interface is published for.</summary>
public class I3DPointSeriesTests
{
    [Fact]
    public void A3DPointSeries_HandsOutItsCoordinatesThroughTheInterface()
    {
        double[] x = [0, 1], y = [2, 3], z = [4, 5];
        I3DPointSeries[] series = [new Quiver3DSeries(x, y, z, x, y, z), new Scatter3DSeries(x, y, z)];

        Assert.All(series, s =>
        {
            Assert.Equal(x, s.X);
            Assert.Equal(y, s.Y);
            Assert.Equal(z, s.Z);
        });
    }
}
