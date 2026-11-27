// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Serialization;

/// <summary>A relative rotation chart with no assets comes back from JSON with none. Its settings and its data travel under the
/// drawn-property contract and the GivenSeries pins.</summary>
public class RelativeRotationSerializationTests
{
    // ── Empty assets round-trip ───────────────────────────────────────────────

    [Fact]
    public void RoundTrip_EmptyAssets_PreservesShape()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.RelativeRotation([], [], []))
            .Build();
        var restored = figure.RoundTripped();
        var s = restored.SubPlots[0].Series.OfType<RelativeRotationSeries>().First();
        Assert.Empty(s.AssetCloses);
    }
}
