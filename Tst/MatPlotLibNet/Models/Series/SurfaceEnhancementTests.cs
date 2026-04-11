// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="SurfaceSeries"/> EdgeColor, RowStride, and ColStride enhancements.</summary>
public class SurfaceEnhancementTests
{
    private static SurfaceSeries MakeSurface() =>
        new([0.0, 1.0], [0.0, 1.0], new double[2, 2]);

    [Fact]
    public void SurfaceSeries_EdgeColor_DefaultsToNull()
    {
        Assert.Null(MakeSurface().EdgeColor);
    }

    [Fact]
    public void SurfaceSeries_RowStride_DefaultsTo1()
    {
        Assert.Equal(1, MakeSurface().RowStride);
    }

    [Fact]
    public void SurfaceSeries_ColStride_DefaultsTo1()
    {
        Assert.Equal(1, MakeSurface().ColStride);
    }

    [Fact]
    public void SurfaceSeries_EdgeColor_CanBeSet()
    {
        var s = MakeSurface();
        s.EdgeColor = new Color(255, 0, 0);
        Assert.Equal(new Color(255, 0, 0), s.EdgeColor);
    }

    [Fact]
    public void SurfaceSeries_RowStride_CanBeSet()
    {
        var s = MakeSurface();
        s.RowStride = 2;
        Assert.Equal(2, s.RowStride);
    }

    [Fact]
    public void SurfaceSeries_ColStride_CanBeSet()
    {
        var s = MakeSurface();
        s.ColStride = 3;
        Assert.Equal(3, s.ColStride);
    }
}
