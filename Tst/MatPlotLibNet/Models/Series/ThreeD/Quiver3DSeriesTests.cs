// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="Quiver3DSeries"/> behaviour; what it starts from and the settings it stores are held by <see cref="Serialization.RestingValueCorpusTests"/> and the drawn-property round trips.</summary>
public class Quiver3DSeriesTests
{
    /// <summary>Verifies that ComputeDataRange includes arrow tips.</summary>
    [Fact]
    public void ComputeDataRange_IncludesArrowTips()
    {
        double[] x = [0], y = [0], z = [0];
        double[] u = [2], v = [3], w = [4];
        var series = new Quiver3DSeries(x, y, z, u, v, w);
        var range = series.ComputeDataRange(null!);
        Assert.Equal(0, range.XMin);
        Assert.Equal(2, range.XMax);
        Assert.Equal(0, range.YMin);
        Assert.Equal(3, range.YMax);
        Assert.Equal(0, range.ZMin);
        Assert.Equal(4, range.ZMax);
    }
}
