// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="CountSeries"/> behaviour; what it starts from and the settings it stores are held by <see cref="Serialization.RestingValueCorpusTests"/> and the drawn-property round trips.</summary>
public class CountSeriesTests
{
    [Fact]
    public void ComputeCounts_ReturnsCorrectFrequencies()
    {
        var series = new CountSeries(["a", "b", "a", "c", "a"]);
        var counts = series.ComputeCounts();
        Assert.Equal(3, counts["a"]);
        Assert.Equal(1, counts["b"]);
        Assert.Equal(1, counts["c"]);
    }
}
