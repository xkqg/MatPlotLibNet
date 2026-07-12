// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Serialization;

/// <summary>Verifies that a stat tile keeps ALL of its state across a JSON round-trip.
/// <para>Until v1.14 the tile persisted its value and accent colour but silently dropped its
/// <see cref="StatTileSeries.Format"/> and its <see cref="ChartSeries.Label"/> — a tile restored from the wire
/// came back reading <c>0.3</c> instead of <c>0.3 s</c>, with no caption. Same root cause as the gauge losing
/// its bands: model state that reaches no sink.</para></summary>
public class StatTileRoundTripTests
{
    private static Figure RoundTrip(Figure figure)
    {
        var serializer = new ChartSerializer();
        return serializer.FromJson(serializer.ToJson(figure));
    }

    private static StatTileSeries Tile(Figure figure) =>
        figure.SubPlots[0].Series.OfType<StatTileSeries>().Single();

    /// <summary>Every settable property of the tile survives the round-trip. Written as one assertion block
    /// per property so a future addition that forgets its DTO wiring fails HERE rather than in a dashboard.</summary>
    [Fact]
    public void EveryTileProperty_SurvivesRoundTrip()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.StatTile(0.3, s =>
            {
                s.Label = "Lag";
                s.Format = "0.0' s'";
                s.AccentColor = Colors.Orange;
            }))
            .Build();

        var tile = Tile(RoundTrip(figure));

        Assert.Equal(0.3, tile.Value);
        Assert.Equal("Lag", tile.Label);
        Assert.Equal("0.0' s'", tile.Format);
        Assert.Equal(Colors.Orange, tile.AccentColor);
    }

    /// <summary>The formatted headline — what an operator actually reads — is identical after the round-trip.
    /// A value that comes back without its unit is a different number on the wall.</summary>
    [Fact]
    public void TheFormattedHeadline_IsIdenticalAfterRoundTrip()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.StatTile(0.3, s => s.Format = "0.0' s'"))
            .Build();

        string before = Tile(figure).FormattedValue;
        string after = Tile(RoundTrip(figure)).FormattedValue;

        Assert.Equal("0.3 s", before);
        Assert.Equal(before, after);
    }

    /// <summary>A tile left on its defaults adds no format bytes — the golden stays byte-identical.</summary>
    [Fact]
    public void DefaultFormat_AddsNoBytes()
    {
        string json = new ChartSerializer().ToJson(Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.StatTile(42))
            .Build());

        Assert.DoesNotContain("0.##", json);
    }
}
