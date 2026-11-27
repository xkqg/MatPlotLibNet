// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Models.Series;

public class QuiverKeySeriesTests
{
    [Fact]
    public void ComputeDataRange_ReturnsAllNulls()
    {
        var s = new QuiverKeySeries(0.5, 0.9, 1.0, "1 m/s");
        var range = s.ComputeDataRange(null!);
        Assert.Null(range.XMin); Assert.Null(range.XMax);
        Assert.Null(range.YMin); Assert.Null(range.YMax);
    }
}
