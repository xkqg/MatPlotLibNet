// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>A box plot and a violin plot read back from a wire record with no datasets are empty plots, not a failure.</summary>
public class BoxAndViolinReadBackTests
{
    public static TheoryData<Func<System.Collections.IEnumerable>> TheReadBacks => new()
    {
        () => BoxSeries.FromSeriesDto(new Axes(), new global::MatPlotLibNet.Serialization.SeriesDto { Type = "box" }).Datasets,
        () => ViolinSeries.FromSeriesDto(new Axes(), new global::MatPlotLibNet.Serialization.SeriesDto { Type = "violin" }).Datasets,
    };

    [Theory]
    [MemberData(nameof(TheReadBacks))]
    public void ReadBackWithoutDatasets_IsAnEmptyPlot(Func<System.Collections.IEnumerable> readBack) => Assert.Empty(readBack());
}
