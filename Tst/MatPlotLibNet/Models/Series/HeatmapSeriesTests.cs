// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Models.Series;

public class HeatmapSeriesTests
{
    [Fact]
    public void Constructor_StoresData()
    {
        var data = new double[,] { { 1, 2 }, { 3, 4 } };
        var series = new HeatmapSeries(data);
        Assert.Equal(data, series.Data);
    }

    [Fact]
    public void DefaultColorMap_IsNull()
    {
        var series = new HeatmapSeries(new double[,] { { 1 } });
        Assert.Null(series.ColorMap);
    }
}
