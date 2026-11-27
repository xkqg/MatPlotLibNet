// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="BrokenBarSeries"/> behaviour; what it starts from and the settings it stores are held by <see cref="Serialization.RestingValueCorpusTests"/> and the drawn-property round trips.</summary>
public class BrokenBarSeriesTests
{
    /// <summary>A row is a slot the caller gave (one label a row, the rows of a Gantt chart share the extent), so a row with no segment
    /// keeps its place along Y under every theme, the matplotlib themes included: matplotlib's <c>broken_barh</c> draws one row a call and
    /// has no row to leave out. Only a series whose rows hold no segment at all holds no data.</summary>
    [Fact]
    public void ARowWithNoSegment_KeepsItsSlotUnderEveryTheme()
    {
        var series = new BrokenBarSeries([[new BarRange(1, 2)], []]) { BarHeight = 0.5 };
        var expected = new DataRangeContribution(1, 3, -0.25, 1.25);

        Assert.False(series.HoldsNoData);
        Assert.Equal(expected, series.ComputeDataRange(null!));
        Assert.Equal(expected, series.DataRangeUnder(null!, MatPlotLibNet.Styling.Theme.MatplotlibV2));
        Assert.Equal(expected, series.DataRangeUnder(null!, MatPlotLibNet.Styling.Theme.MatplotlibClassic));
    }
}
