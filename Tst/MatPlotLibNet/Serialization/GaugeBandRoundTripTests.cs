// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Serialization;

/// <summary>Verifies that a gauge keeps its threshold bands across a JSON round-trip.
/// <para>Until v1.14 <see cref="GaugeSeries.Ranges"/> was rendered but never serialized: a gauge sent over
/// the wire came back with its green/amber/red bands silently replaced by the defaults. Same root cause as
/// the hollow hatch — model state that reached no sink.</para></summary>
public class GaugeBandRoundTripTests
{
    private static readonly GaugeBand[] Bands =
    [
        new(30, Colors.Tab10Green),
        new(70, Colors.Orange),
        new(100, Colors.Red)
    ];

    private static Figure RoundTrip(Figure figure)
    {
        var serializer = new ChartSerializer();
        return serializer.FromJson(serializer.ToJson(figure));
    }

    /// <summary>Custom bands survive the round-trip: same count, same thresholds, same colours, same order.</summary>
    [Fact]
    public void CustomBands_SurviveRoundTrip()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Gauge(42, g => g.Ranges = Bands))
            .Build();

        var gauge = RoundTrip(figure).SubPlots[0].Series.OfType<GaugeSeries>().Single();

        Assert.NotNull(gauge.Ranges);
        Assert.Equal(Bands, gauge.Ranges);
    }

    /// <summary>A gauge WITHOUT custom bands adds no band bytes — it keeps falling back to the built-in
    /// defaults, and its golden stays byte-identical.</summary>
    [Fact]
    public void DefaultBands_AddNoBytesAndStayNull()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Gauge(42))
            .Build();

        string json = new ChartSerializer().ToJson(figure);
        var gauge = RoundTrip(figure).SubPlots[0].Series.OfType<GaugeSeries>().Single();

        Assert.DoesNotContain("gaugeBand", json, StringComparison.OrdinalIgnoreCase);
        Assert.Null(gauge.Ranges);
    }

    /// <summary>An empty band array is preserved as empty — an explicit "no bands" is a different intent from
    /// "use the defaults", and the round-trip must not collapse the two.</summary>
    [Fact]
    public void EmptyBandArray_RoundTripsAsEmptyNotNull()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Gauge(42, g => g.Ranges = []))
            .Build();

        var gauge = RoundTrip(figure).SubPlots[0].Series.OfType<GaugeSeries>().Single();

        Assert.NotNull(gauge.Ranges);
        Assert.Empty(gauge.Ranges);
    }
}
