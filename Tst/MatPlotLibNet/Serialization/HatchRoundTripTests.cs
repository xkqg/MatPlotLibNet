// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Serialization;

/// <summary>Verifies that a hatch survives a JSON round-trip — the third place the hatch used to vanish
/// (after the SVG and the raster backends). A style that renders but does not persist is a style that
/// disappears the moment a figure is sent over the wire.</summary>
public class HatchRoundTripTests
{
    private static Figure RoundTrip(Figure figure)
    {
        var serializer = new ChartSerializer();
        return serializer.FromJson(serializer.ToJson(figure));
    }

    /// <summary>A hatched bar comes back hatched, in the same pattern and the same colour.</summary>
    [Fact]
    public void BarSeries_Hatch_SurvivesRoundTrip()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Bar(["A"], [1.0], s =>
            {
                s.Hatch = HatchPattern.DiagonalCross;
                s.HatchColor = Colors.Red;
            }))
            .Build();

        var bar = RoundTrip(figure).SubPlots[0].Series.OfType<BarSeries>().Single();

        Assert.Equal(HatchPattern.DiagonalCross, bar.Hatch);
        Assert.Equal(Colors.Red, bar.HatchColor);
    }

    /// <summary>A hatched area comes back hatched.</summary>
    [Fact]
    public void AreaSeries_Hatch_SurvivesRoundTrip()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.FillBetween([0.0, 1], [1.0, 2], null, s =>
            {
                s.Hatch = HatchPattern.Horizontal;
                s.HatchColor = Colors.Black;
            }))
            .Build();

        var area = RoundTrip(figure).SubPlots[0].Series.OfType<AreaSeries>().Single();

        Assert.Equal(HatchPattern.Horizontal, area.Hatch);
        Assert.Equal(Colors.Black, area.HatchColor);
    }

    /// <summary>An unhatched series adds NO hatch bytes to the document. This is what keeps the 76-discriminator
    /// golden corpus byte-identical: the DTO omits nulls, so a default hatch must serialize as null, never as
    /// <c>"hatch":"none"</c>.</summary>
    [Fact]
    public void NoHatch_AddsNoBytesToTheDocument()
    {
        string json = new ChartSerializer().ToJson(Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Bar(["A"], [1.0]))
            .Build());

        Assert.DoesNotContain("atch", json);
    }

    /// <summary>A hatch without an explicit colour round-trips too: the pattern persists, the colour stays
    /// unset, and the renderer's contrast fallback does its work on the far side.</summary>
    [Fact]
    public void HatchWithoutColour_RoundTripsWithTheColourStillUnset()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Bar(["A"], [1.0], s => s.Hatch = HatchPattern.Dots))
            .Build();

        var bar = RoundTrip(figure).SubPlots[0].Series.OfType<BarSeries>().Single();

        Assert.Equal(HatchPattern.Dots, bar.Hatch);
        Assert.Null(bar.HatchColor);
    }
}
