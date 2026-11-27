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
    /// <summary>An empty band array is preserved as empty — an explicit "no bands" is a different intent from
    /// "use the defaults", and the round-trip must not collapse the two.</summary>
    [Fact]
    public void EmptyBandArray_RoundTripsAsEmptyNotNull()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Gauge(42, g => g.Ranges = []))
            .Build();

        var gauge = figure.RoundTripped().SubPlots[0].Series.OfType<GaugeSeries>().Single();

        Assert.NotNull(gauge.Ranges);
        Assert.Empty(gauge.Ranges);
    }
}
