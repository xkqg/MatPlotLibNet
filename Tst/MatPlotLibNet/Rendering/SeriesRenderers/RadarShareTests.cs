// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.SeriesRenderers;

/// <summary>Which radar of an axes draws the shared web and the frame, and on which outer value, at the class's own granularity.</summary>
public sealed class RadarShareTests
{
    private static readonly string[] Spokes = ["a", "b", "c"];

    private static RadarSeries Radar(double top, bool visible = true) => new(Spokes, [1, 2, top]) { Visible = visible };

    [Fact]
    public void UnderTheLibrarysOwnThemes_EachRadarDrawsItsOwnWebAndFrame_OnItsOwnOuterValue()
    {
        var small = Radar(3);
        ISeries[] all = [small, Radar(30)];

        Assert.Equal(new RadarSeriesRenderer.RadarShare(3, true, true), RadarSeriesRenderer.ShareOf(small, all, Theme.Default));
    }

    [Fact]
    public void ARadarDrawnOnItsOwn_DrawsItsWebAndFrame()
    {
        var radar = Radar(3);

        Assert.Equal(new RadarSeriesRenderer.RadarShare(3, true, true), RadarSeriesRenderer.ShareOf(radar, null, Theme.MatplotlibV2));
        Assert.Equal(new RadarSeriesRenderer.RadarShare(3, true, true), RadarSeriesRenderer.ShareOf(radar, [Radar(30)], Theme.MatplotlibV2));
    }

    [Fact]
    public void UnderTheMatplotlibThemes_TheFirstDrawsTheWeb_TheLastTheFrame_OnTheLargestOuterValue_PastOtherSeries()
    {
        var first = Radar(3);
        var last = Radar(30);
        ISeries[] all = [new LineSeries([0.0, 1.0], [0.0, 1.0]), first, last];

        Assert.Equal(new RadarSeriesRenderer.RadarShare(30, true, false), RadarSeriesRenderer.ShareOf(first, all, Theme.MatplotlibV2));
        Assert.Equal(new RadarSeriesRenderer.RadarShare(30, false, true), RadarSeriesRenderer.ShareOf(last, all, Theme.MatplotlibClassic));
    }

    [Fact]
    public void AHiddenRadar_DrawsNeither_ButItsValueStillSetsTheScale()
    {
        var hidden = Radar(50, visible: false);
        var shown = Radar(3);
        ISeries[] all = [hidden, shown];

        Assert.Equal(new RadarSeriesRenderer.RadarShare(50, true, true), RadarSeriesRenderer.ShareOf(shown, all, Theme.MatplotlibV2));
    }

    [Fact]
    public void TheOuterValue_IsTheMaxValueGiven_ElseTheLargestValue_AndOneWhenThereIsNoneAbove0()
    {
        Assert.Equal(7, new RadarSeries(Spokes, [1, 2, 3]) { MaxValue = 7 }.OuterValue);
        Assert.Equal(3, new RadarSeries(Spokes, [1, double.NaN, 3]).OuterValue);
        Assert.Equal(1, new RadarSeries(Spokes, [-1, -2, -3]).OuterValue);
        Assert.Equal(1, new RadarSeries(Spokes, [double.NaN, double.NaN, double.NaN]).OuterValue);
    }
}
