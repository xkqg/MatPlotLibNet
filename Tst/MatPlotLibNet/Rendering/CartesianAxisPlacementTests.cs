// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.TestFixtures;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Where a Cartesian axes' axes stand (<see cref="CartesianAxisPlacement"/>): the range of the primary and of every
/// axis past it, the transforms they are drawn with, their ticks, each Y slot's band and outward offset, and the top X axis. The
/// draw, the layout's reserve and the interaction views all place an axis here, so the band the layout reserves for a slot is
/// the band the draw stacks it by, and two figures with every kind of axis past the primary draw the same bytes they drew before
/// the placement left the renderer.</summary>
public class CartesianAxisPlacementTests
{
    private static readonly double[] X = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10];

    private static double[] Times(double factor) => [.. X.Select(v => (v * 7 % 11) * factor)];

    /// <summary>A primary line, a right slot with series, a right slot linked to the primary, a right slot with neither (placed
    /// and charged its band, not drawn), a left slot with series, and a top X axis with series of its own.</summary>
    private static Figure EveryAxisPastThePrimary() => Plt.Create()
        .WithSize(900, 500)
        .WithTitle("Every axis past the primary")
        .AddSubPlot(1, 1, 1, ax => ax
            .Plot(X, Times(1)).SetXLabel("x").SetYLabel("primary")
            .WithAddRightYAxis(s => s.Plot(X, Times(100)).SetYLabel("hundreds"))
            .WithAddRightYAxis(s => s.Link(AxisTransform.Linear(2.5)).SetYLabel("linked"))
            .WithAddRightYAxis(s => s.SetYLabel("empty"))
            .WithAddLeftYAxis(s => s.Plot(X, Times(10)).SetYLabel("tens"))
            .WithSecondaryXAxis(top => top.PlotXSecondary(Times(1000), X).SetXLabel("top")))
        .Build();

    /// <summary>A tight layout, a top X axis linked to the primary and two slots past the primary on the right.</summary>
    private static Figure ALinkedTopAxisUnderATightLayout() => Plt.Create()
        .WithSize(700, 450)
        .TightLayout()
        .AddSubPlot(1, 1, 1, ax => ax
            .Plot(X, Times(1)).SetYLabel("primary")
            .WithAddRightYAxis(s => s.Plot(X, Times(1000)).SetYLabel("thousands"))
            .WithAddRightYAxis(s => s.Plot(X, Times(100)))
            .WithSecondaryXAxis(top => top.Link(AxisTransform.Linear(4)).SetXLabel("quarters")))
        .Build();

    /// <summary>The SHA-256 of each figure's SVG as the renderer drew it before its placement moved to
    /// <see cref="CartesianAxisPlacement"/>, with the eleven changes since that are meant, each a setting the default theme
    /// (<see cref="Theme.MatplotlibV2"/>) takes from matplotlib. A spine without a width is as wide as
    /// <c>axes.linewidth</c> (0.8 pt, 1.11 px), so the ticks that hang from the spine start 0.16 px further out; a series line
    /// without a width is as wide as <c>lines.linewidth</c> (1.5 pt, 2.08 px); a figure title is <c>figure.titlesize</c> 'large'
    /// (1.2 times the font, 16.67 px) and <c>figure.titleweight</c> normal; the text, spines and ticks are <c>text.color</c>,
    /// <c>axes.edgecolor</c> and <c>xtick.color</c>, black. The pins were drawn again for each and for nothing else: the first
    /// moved only <c>line</c> elements (the spines' stroke width and the ticks' start), the second only the series'
    /// <c>polyline</c> stroke width, 1.5 to 2.08, the third only the figure title's <c>text</c> element (font-size 17.89 bold to
    /// 16.67, y 24.1 to 23), the fourth only the colour #262626, which became #000000 (143 and 113 times); the fifth the pads of the titles, an axes title
    /// <c>axes.titlepad</c> (6 pt, 8.33 px where the layout kept 8) and an axis title <c>axes.labelpad</c> (4 pt, 5.56 px where the X title kept 6 and
    /// a Y title and the gap between two stacked slots 4), which moved the titles, what the layout reserves for them and so the plot area; the sixth the ticks of every axis that names no locator,
    /// placed as matplotlib's default <c>AutoLocator</c> places them (as many bins as the axis' length holds, at most nine, and the step of
    /// 1, 2, 2.5, 5 or 10 at or above the range over them): steps of 2, 5, 20, 200 and 2000 where the library's eight intervals took 1,
    /// 2.5, 10, 100 and 1000, which moved only the tick marks (<c>line</c>) and their labels (<c>text</c>); the seventh the tick marks of
    /// the primary axes drawn at the axis' z-order (<c>axes.axisbelow</c>: 1.5 under the default style, under a line at 2), which moved only
    /// the order of the primary pair's twelve tick labels and twelve marks, the labels now first and the marks after them, every element
    /// unchanged; the eighth the colour cycles started again on every twin axis, as matplotlib's <c>twinx</c> and <c>twiny</c> are axes
    /// with cycles of their own (<c>ax2._get_lines</c> is not <c>ax._get_lines</c>), which moved only the stroke of the line on each Y
    /// slot past the primary and on the top axis, #FF7F0E, #2CA02C and #D62728 to #1F77B4; the ninth the corner of a line, which matplotlib joins
    /// 'round' (<c>lines.solid_joinstyle</c>): every series' <c>polyline</c> gained <c>stroke-linejoin="round"</c> and nothing else (the old pins
    /// are the hashes of the new bytes with that attribute taken out); the tenth the pad of the tight layout, matplotlib's <c>tight_layout</c> pad of 1.08
    /// times the font (15 px where the layout kept 8 above the title and right of the last axis), which moved only the plot area of the tight figure
    /// (its top 59.39 to 66.39, its right edge 575.33 to 568.33) and so the places of its marks and labels: 77 of its 126 elements, the same
    /// elements in the same order; the eleventh the clip of every series, as matplotlib clips every artist to the axes (<c>clip_on</c>):
    /// each series' marks gained a <c>clipPath</c> of the plot area and the group drawn in it, 4 and 3 of them, and nothing else (the old pins
    /// are the hashes of the new bytes with those taken out); the twelfth one rule for where a text stands on its anchor (the box's): a Y tick
    /// label is centred on its tick, its baseline 0.35 of its ascent below it where it stood 4 px below (24 and 18 labels, 4.86 where it
    /// was 4 under these 13.89 px labels), and the figure title hangs from its pad by its ascent (y 23 to 24.67): nothing else moved (the
    /// old pins are the hashes of the new bytes with those y put back); the thirteenth the room above a top axis' title, which ends at the
    /// top of the title's box as the room below the plot does, the descent facing the tick labels counted once (matplotlib stands the axes
    /// title above <c>xaxis.get_tightbbox().ymax</c>): the tight figure's plot top 66.39 to 63.61 (0.2 of its 13.89 px axis title), which
    /// moved only the y of its marks and labels, 67 of its 99 lines, the same elements in the same order; the fourteenth the top axis' title
    /// set on its baseline the gap above the tick labels, as matplotlib sets its top X label (<c>va='baseline'</c>), where it stood its
    /// descent higher: in the first figure only that title's y (25.28 to 28.06, 0.2 of 13.89 px), in the tight one the plot top 63.61 to
    /// 60.83 and so the y of its marks and labels, 66 lines, the same elements in the same order. Every value in the figures is a whole number
    /// and every tick step a whole number, so the bytes are the same on every platform.</summary>
    public static TheoryData<string, string> PinnedFigures() => new()
    {
        { "every axis past the primary", "ED0DC7211E4B01FA43D617DF9B8299DDB05DEF7714409212BBAC3DFAE4E66612" },
        { "a linked top axis under a tight layout", "E9182426611BF4B3D457FF929AC85B194ECBB7256654BCB3D4B0CE8422696458" },
    };

    [Theory]
    [MemberData(nameof(PinnedFigures))]
    public void AFigureWithAxesPastThePrimary_DrawsTheBytesItDrewBeforeThePlacementMoved(string figure, string sha256)
    {
        var drawn = (figure == "every axis past the primary" ? EveryAxisPastThePrimary() : ALinkedTopAxisUnderATightLayout()).ToSvg();

        Assert.Equal(sha256, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(drawn))));
    }

    /// <summary>The layout reserves every slot past the primary, on either side, the band the placement gives it beside a plot
    /// of the densest ticks a slot can draw (<see cref="AutoLocator.FullDensityLength"/>): a slot with series, a linked slot and
    /// a slot with neither alike.</summary>
    [Fact]
    public void TheLayout_ReservesEverySlotTheBandThePlacementGivesIt()
    {
        var axes = EveryAxisPastThePrimary().SubPlots[0];
        var ctx = new RecordingRenderContext();
        var placement = new CartesianAxisPlacement(axes, new Rect(0, 0, 0, AutoLocator.FullDensityLength), ctx, Theme.Default);
        var range = placement.ComputeDataRanges();
        var engine = new AxesEdgeNeeds();

        double[] right = [.. axes.YAxes.Right.Select(slot => placement.SlotBand(placement.PlaceSlot(slot, range)))];
        double[] left = [.. axes.YAxes.Left.Skip(axes.YAxes.Left.OutwardOffset)
            .Select(slot => placement.SlotBand(placement.PlaceSlot(slot, range)))];

        Assert.Equal(3, right.Length);
        Assert.Single(left);
        Assert.All(right.Concat(left), band => Assert.True(band > 0));
        Assert.Equal(right, engine.SecondaryAxisBands(axes, ctx, Theme.Default));
        Assert.Equal(left, engine.LeftAxisBands(axes, ctx, Theme.Default));
    }

    /// <summary>Made for a draw, the placement reads the draw's range of the subplot, worked out once for every placement the draw
    /// makes; made outside one, it ranges the axes itself, by the same rule.</summary>
    [Fact]
    public void ThePrimaryRange_IsTheDrawsWhenItHasOne_AndTheAxesOwnOtherwise()
    {
        var figure = new Figure();
        var series = figure.AddSubPlot().AddSeries(new CountingRangeSeries([0.0, 1, 2], [1.0, 4, 9]));
        var axes = figure.SubPlots[0];
        var ranges = new FigureRanges(figure);
        var plot = new Rect(0, 0, 400, 300);

        var inTheDraw = new CartesianAxisPlacement(axes, plot, new RecordingRenderContext(), figure.Theme, ranges);
        var first = inTheDraw.ComputeDataRanges();
        var second = inTheDraw.ComputeDataRanges();
        int callsInTheDraw = series.RangeCalls;
        var alone = new CartesianAxisPlacement(axes, plot, new RecordingRenderContext(), figure.Theme).ComputeDataRanges();

        Assert.Equal(1, callsInTheDraw);
        Assert.Equal(first, second);
        Assert.Equal(axes.ComputeDataRanges(figure.Theme), alone);
        Assert.Equal(alone, first);
    }

    private static readonly Rect Plot = new(0, 0, 400, 300);

    // ── PrimaryTicks: the primary ticks of the X or the Y axis over the axes' range ──────────────────────

    private static double[] PrimaryTicksOf(Axes axes, Rect plot, bool isX)
    {
        var placement = new CartesianAxisPlacement(axes, plot, new RecordingRenderContext(), Theme.Default);
        return placement.PrimaryTicks(placement.ComputeDataRanges(), axes.PrimaryTickSource(isX, Theme.Default), isX);
    }

    /// <summary>An axis' own locator places its ticks over the axes' range, each axis by its own.</summary>
    [Fact]
    public void PrimaryTicks_OfAnAxisWithALocator_AreTheLocatorsOverTheRange()
    {
        var axes = Plt.Create().OneAxes(ax => ax.Plot([0.0, 10.0], [0.0, 3.0]).SetXLim(0, 10).SetYLim(0, 3)
            .SetXTickLocator(new MatPlotLibNet.Rendering.TickLocators.MultipleLocator(2.5)));

        Assert.Equal([0.0, 2.5, 5.0, 7.5, 10.0], PrimaryTicksOf(axes, Plot, isX: true));
    }

    /// <summary>A tick spacing with no locator lays the ticks out every spacing.</summary>
    [Fact]
    public void PrimaryTicks_OfAnAxisWithASpacing_StandEverySpacing()
    {
        var axes = Plt.Create().OneAxes(ax => ax.Plot([0.0, 1.0], [0.0, 1.0]).SetXLim(0, 1).SetYLim(0, 1));
        axes.YAxis.MajorTicks = axes.YAxis.MajorTicks with { Spacing = 0.25 };

        Assert.Equal([0.0, 0.25, 0.5, 0.75, 1.0], PrimaryTicksOf(axes, Plot, isX: false));
    }

    /// <summary>With neither, the default placement for the plot's length that way: 8 intervals along a plot of 240 px or
    /// more, fewer along a shorter one.</summary>
    [Theory]
    [InlineData(400.0, 300.0, true, 8)]
    [InlineData(400.0, 300.0, false, 8)]
    [InlineData(100.0, 300.0, true, 3)]
    [InlineData(400.0, 150.0, false, 5)]
    public void PrimaryTicks_ByDefault_AreTheDefaultPlacementForThePlotsLength(double width, double height, bool isX, int intervals)
    {
        var axes = Plt.Create().OneAxes(ax => ax.Plot([0.0, 1.0], [0.0, 1.0]).SetXLim(0, 1).SetYLim(0, 1));

        Assert.Equal(new MatPlotLibNet.Rendering.TickLocators.AutoLocator(intervals).Locate(0, 1),
            PrimaryTicksOf(axes, new Rect(0, 0, width, height), isX));
    }

    /// <summary>An axis the series stand categories along ticks every category's slot centre.</summary>
    [Fact]
    public void PrimaryTicks_OfACategoryAxis_StandOnTheCategories()
    {
        var axes = Plt.Create().OneAxes(ax => ax.Bar(["a", "b", "c"], [1.0, 2.0, 3.0]));
        var range = new CartesianAxisPlacement(axes, Plot, new RecordingRenderContext(), Theme.Default).ComputeDataRanges();
        var expected = new[] { "a", "b", "c" }.AsCategoryTicks().Locator.Locate(range.XMin, range.XMax);

        Assert.Equal(expected, PrimaryTicksOf(axes, Plot, isX: true));
        Assert.Equal(3, expected.Length);
    }

    /// <summary>A tick inside a break of its axis is left out; one on the break's edge stays.</summary>
    [Fact]
    public void PrimaryTicks_InsideABreak_AreLeftOut()
    {
        var axes = Plt.Create().OneAxes(ax => ax.Plot([0.0, 10.0], [0.0, 1.0]).SetXLim(0, 10).SetYLim(0, 1)
            .SetXTickLocator(new MatPlotLibNet.Rendering.TickLocators.MultipleLocator(2)));
        axes.AddXBreak(3, 6);

        Assert.Equal([0.0, 2.0, 6.0, 8.0, 10.0], PrimaryTicksOf(axes, Plot, isX: true));
    }
}
