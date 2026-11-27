// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="HeatmapSeries"/> behaviour; what it starts from and the settings it stores are held by <see cref="Serialization.RestingValueCorpusTests"/> and the drawn-property round trips.</summary>
public class HeatmapSeriesTests
{
    [Fact]
    public void GetColorBarRange_AllEqualData_RunsFromTheValueToOneAboveIt()
    {
        var series = new HeatmapSeries(new double[,] { { 5, 5 }, { 5, 5 } });
        var range = series.GetColorBarRange();
        Assert.Equal(5, range.Min);
        Assert.Equal(6, range.Max);
    }
}
