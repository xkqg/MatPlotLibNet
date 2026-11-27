// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Serialization;

/// <summary>A stat tile read back from JSON writes its headline as it did before: a value that comes back without its unit is a
/// different number on the wall. Its settings travel under the drawn-property contract.</summary>
public class StatTileRoundTripTests
{
    private static StatTileSeries Tile(Figure figure) =>
        figure.SubPlots[0].Series.OfType<StatTileSeries>().Single();

    /// <summary>The formatted headline — what an operator actually reads — is identical after the round-trip.
    /// A value that comes back without its unit is a different number on the wall.</summary>
    [Fact]
    public void TheFormattedHeadline_IsIdenticalAfterRoundTrip()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.StatTile(0.3, s => s.Format = "0.0' s'"))
            .Build();

        string before = Tile(figure).FormattedValue;
        string after = Tile(figure.RoundTripped()).FormattedValue;

        Assert.Equal("0.3 s", before);
        Assert.Equal(before, after);
    }
}
