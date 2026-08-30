// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Tests.Serialization;

/// <summary>A tile's link survives the wire — a wall pushed over SignalR must land with the same drill-downs
/// it was built with. Same root cause as the tile once losing its format: model state that reaches no sink.</summary>
public class StatTileLinkRoundTripTests
{
    private static StatTileSeries RoundTrip(Action<StatTileSeries> configure)
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.StatTile(1, configure)).Build();
        var serializer = new ChartSerializer();
        var back = serializer.FromJson(serializer.ToJson(figure));
        return back.SubPlots[0].Series.OfType<StatTileSeries>().Single();
    }

    [Fact]
    public void TheUrlAndExpandedState_SurviveRoundTrip()
    {
        var tile = RoundTrip(t => { t.Url = "/?panel=processes"; t.Expanded = true; });

        Assert.Equal("/?panel=processes", tile.Url);
        Assert.True(tile.Expanded);
    }

    [Fact]
    public void AnUnlinkedTile_AddsNoBytes()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.StatTile(1)).Build();

        string json = new ChartSerializer().ToJson(figure);

        Assert.DoesNotContain("url", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("expanded", json, StringComparison.OrdinalIgnoreCase);
    }
}
