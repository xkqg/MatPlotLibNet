// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Models.Series.Streaming;

namespace MatPlotLibNet.Tests.Models;

/// <summary>The series that can carry a <see cref="SeriesLabelPlacement"/> are a CLOSED set, pinned here the way
/// <see cref="SeriesCountContractTests"/> pins the series count. Placing a name at the end of a line only means
/// something where the end-label part knows how to draw it; a new carrier is a conscious decision that also needs
/// that part, the legend rule and the persistence to follow, so adding one turns this test red on purpose.</summary>
public class LabelPlacementCarrierContractTests
{
    /// <summary>The carriers the end-label part draws. Extend this list together with the part.</summary>
    private static readonly Type[] Carriers = [typeof(LineSeries), typeof(StreamingLineSeries)];

    [Fact]
    public void ExactlyTheLineSeriesAndTheStreamingLine_CarryALabelPlacement()
    {
        var carriers = typeof(ChartSeries).Assembly.GetTypes()
            .Where(t => t.IsClass && typeof(IHasLabelPlacement).IsAssignableFrom(t))
            .OrderBy(t => t.Name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(Carriers.OrderBy(t => t.Name, StringComparer.Ordinal), carriers);
    }
}
