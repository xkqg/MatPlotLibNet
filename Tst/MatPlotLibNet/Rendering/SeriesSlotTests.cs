// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>The one owner of "the colour of series i": which slot a series holds, what the theme hands out at that
/// slot, and which colour wins when the series names its own. Every site that draws a series or its legend swatch
/// asks this owner, so the line, the swatch and (later) an end label cannot disagree.</summary>
public class SeriesSlotTests
{
    private static readonly PropCycler RedGreenBlue =
        new PropCyclerBuilder().WithColors(Colors.Red, Colors.Green, Colors.Blue).Build();

    private static readonly Theme CyclerTheme =
        Theme.CreateFrom(Theme.Default).WithPropCycler(RedGreenBlue).Build();

    // ── Theme: what a slot is worth ────────────────────────────────────────────

    [Fact]
    public void WithoutACycler_theSlotTakesTheCycleColour_andCarriesNoCycledProperties()
    {
        var slot = Theme.Default.SeriesSlot(1);

        Assert.Equal(Theme.Default.CycleColors[1], slot.CycleColor);
        Assert.Null(slot.CycledProperties);
    }

    [Fact]
    public void WithoutACycler_theCycleColourWrapsAround()
    {
        int n = Theme.Default.CycleColors.Length;

        Assert.Equal(Theme.Default.CycleColors[2], Theme.Default.SeriesSlot(n + 2).CycleColor);
    }

    [Fact]
    public void TheCyclerBeatsTheCycleColour_andHandsOverItsWholeSet()
    {
        var slot = CyclerTheme.SeriesSlot(1);

        Assert.Equal(Colors.Green, slot.CycleColor);
        Assert.Equal(RedGreenBlue[1], slot.CycledProperties);
    }

    // ── Which colour wins for a series ─────────────────────────────────────────

    [Fact]
    public void AnExplicitColourWins_overTheSlot()
    {
        var line = new LineSeries([0.0, 1], [0.0, 1]) { Color = Colors.Blue };

        Assert.Equal(Colors.Blue, CyclerTheme.SeriesSlot(0).ColorOf(line));
    }

    [Fact]
    public void AnUnsetColourTakesTheSlot()
    {
        var line = new LineSeries([0.0, 1], [0.0, 1]);

        Assert.Equal(Colors.Red, CyclerTheme.SeriesSlot(0).ColorOf(line));
    }

    [Fact]
    public void ASeriesWithoutAColourOfItsOwnTakesTheSlot()
    {
        var heatmap = new HeatmapSeries(new double[,] { { 1, 2 }, { 3, 4 } });
        Assert.False(typeof(IHasColor).IsAssignableFrom(heatmap.GetType()));

        Assert.Equal(Colors.Green, CyclerTheme.SeriesSlot(1).ColorOf(heatmap));
    }

    // ── Axes: which slot a series holds ────────────────────────────────────────

    private static Axes OneTwoThree()
    {
        var axes = new Axes();
        axes.Plot([0.0, 1], [0.0, 1]);
        axes.PlotSecondary([0.0, 1], [0.0, 1]);
        axes.PlotSecondary([0.0, 1], [0.0, 1]);
        axes.PlotXSecondary([0.0, 1], [0.0, 1]);
        axes.PlotXSecondary([0.0, 1], [0.0, 1]);
        axes.PlotXSecondary([0.0, 1], [0.0, 1]);
        return axes;
    }

    [Fact]
    public void APrimarySeriesHoldsItsOwnIndex()
    {
        Assert.Equal(0, OneTwoThree().SlotIndex(SeriesAxisPair.Primary, 0));
    }

    [Fact]
    public void ASecondaryYSeriesContinuesAfterThePrimarySeries()
    {
        Assert.Equal(2, OneTwoThree().SlotIndex(SeriesAxisPair.SecondaryY, 1));
    }

    [Fact]
    public void ASecondaryXSeriesContinuesAfterThePrimaryAndTheSecondaryYSeries()
    {
        Assert.Equal(5, OneTwoThree().SlotIndex(SeriesAxisPair.SecondaryX, 2));
    }

    [Fact]
    public void TheSlotsFollowTheOrderOfAllSeries()
    {
        var axes = OneTwoThree();
        var all = axes.AllSeries.ToList();

        Assert.Equal(all.IndexOf(axes.Series[0]), axes.SlotIndex(SeriesAxisPair.Primary, 0));
        Assert.Equal(all.IndexOf(axes.SecondarySeries[1]), axes.SlotIndex(SeriesAxisPair.SecondaryY, 1));
        Assert.Equal(all.IndexOf(axes.XSecondarySeries[2]), axes.SlotIndex(SeriesAxisPair.SecondaryX, 2));
    }

    [Fact]
    public void AnUnknownAxisPairIsRefused()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Axes().SlotIndex((SeriesAxisPair)99, 0));
    }

    [Fact]
    public void TheAxesSlotIsTheThemesSlotAtThatIndex()
    {
        var axes = OneTwoThree();

        Assert.Equal(CyclerTheme.SeriesSlot(4), axes.SeriesSlot(CyclerTheme, SeriesAxisPair.SecondaryX, 1));
    }
}
