// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Models.Series;

public class QuiverKeySeriesTests
{
    [Fact] public void Constructor_StoresXYULabel()
    {
        var s = new QuiverKeySeries(0.5, 0.9, 2.0, "2 m/s");
        Assert.Equal(0.5, s.X); Assert.Equal(0.9, s.Y); Assert.Equal(2.0, s.U); Assert.Equal("2 m/s", s.Label);
    }

    [Fact] public void FontSize_DefaultsTo12() => Assert.Equal(12, new QuiverKeySeries(0.5, 0.9, 1.0, "1 m/s").FontSize);
    [Fact] public void ToSeriesDto_ReturnsTypeQuiverkey() => Assert.Equal("quiverkey", new QuiverKeySeries(0.5, 0.9, 1.0, "1 m/s").ToSeriesDto().Type);

    [Fact]
    public void ComputeDataRange_ReturnsAllNulls()
    {
        var s = new QuiverKeySeries(0.5, 0.9, 1.0, "1 m/s");
        var range = s.ComputeDataRange(null!);
        Assert.Null(range.XMin); Assert.Null(range.XMax);
        Assert.Null(range.YMin); Assert.Null(range.YMax);
    }
}
