// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>An axis as one render places it (<see cref="PlacedAxis"/>), on any edge, drawn by the terms of its edge (<see cref="EdgeTerms"/>,
/// held by <c>EdgeTermsTests</c>): its tick drawing made and hung in one place, the line it hangs from set where it is placed, and the seat
/// its series hold in the identity order (<see cref="SeriesSlotExtensions.YSlotSeats"/>).</summary>
public class PlacedAxisTests
{
    private static readonly Rect Plot = new(100, 20, 400, 300);

    /// <summary>A slot's tick labels are drawn and measured in one context: the side's alignment (away from the plot), its
    /// own formatter or the uniform format of its ticks, its rotation, the theme's tick font and foreground, hung from its line.</summary>
    [Theory]
    [InlineData("Left", TextAlignment.Right)]
    [InlineData("Right", TextAlignment.Left)]
    public void APlacedSlotsTickDraw_AlignsAwayFromThePlot_InTheThemesTickFont(string side, TextAlignment alignment)
    {
        var axes = new Axes();
        var slot = axes.YAxes.StackOn(Enum.Parse<AxisSide>(side)).Add();
        slot.Axis.MajorTicks = slot.Axis.MajorTicks with { LabelRotation = 15 };
        var theme = Theme.Default;
        var placed = new CartesianAxisPlacement(axes, default, new RecordingRenderContext(), theme)
            .PlaceSlot(slot, new DataRange(0, 1, 0, 1)) with { Ticks = [0.0, 0.5, 1.0], EdgeLine = 42 };

        var draw = placed.TickDraw(theme, axes.Spines);

        Assert.Equal(side == "Left" ? TickSide.Left : TickSide.Right, draw.Side);
        Assert.Equal(alignment, draw.Alignment);
        Assert.Equal(15, draw.LabelRotation);
        Assert.Equal(42, draw.AxisEdge);
        Assert.Equal(axes.Spines.Left.HalfWidthIn(theme), draw.SpineHalf);
        Assert.Equal(theme.TickFont(), draw.BaseFont);
        Assert.Equal(theme.ForegroundText, draw.Foreground);
        Assert.Null(draw.Formatter);
        Assert.Equal("0.5", draw.LabelText(0.5));
    }

    /// <summary>The primary Y axis hangs from the left spine wherever that was moved to; a Y axis past the primary is a twin with a frame
    /// of its own and stands at its place past the plot's edge, the edge itself at offset 0, whatever the host's spine on that side does.
    /// matplotlib 3.11.2, read with python: after <c>ax.spines["right"].set_position(("axes", 0.8))</c> a <c>twinx()</c> axis' tick stands
    /// at the axes' right edge (x 576.0), not at the host's moved spine (476.8); a <c>twiny()</c> axis' at the axes' top likewise.</summary>
    [Fact]
    public void ASlotPastThePrimary_StandsAtItsPlace_NotAtTheHostsMovedSpine()
    {
        var axes = new Axes();
        axes.Plot([0.0, 1], [0.0, 1]);
        axes.YAxes.Right.Add().AddSeries(new LineSeries([0.0, 1], [0.0, 2]));
        axes.YAxes.Right.Add().AddSeries(new LineSeries([0.0, 1], [0.0, 3]));
        axes.Spines = axes.Spines with { Right = axes.Spines.Right with { Position = SpinePosition.Axes, PositionValue = 0.8 } };
        var placement = new CartesianAxisPlacement(axes, Plot, new RecordingRenderContext(), Theme.Default);
        var range = placement.ComputeDataRanges();

        var placed = placement.PlaceYSlots(placement.PlacePrimary(range, placement.PrimaryView(range)));

        Assert.Equal(Plot.X, placed[0].EdgeLine, 9);
        Assert.Equal(Plot.X + Plot.Width, placed[1].EdgeLine, 9);
        Assert.Equal(0, placed[1].Offset);
        Assert.True(placed[2].Offset > 0);
        Assert.Equal(EdgeTerms.Right.EdgeAt(Plot, placed[2].Offset), placed[2].EdgeLine, 9);
    }

    /// <summary>The top X axis stands at the plot's top, a linked X axis below the plot draws the bottom spine as its own line and stands at
    /// its walked place.</summary>
    [Fact]
    public void TheTopAxisStandsAtThePlotsTop_AndALinkedAxisBelowThePlot_AtItsPlace_DrawingTheBottomSpine()
    {
        var axes = new Axes();
        axes.Plot([0.0, 1], [0.0, 1]);
        axes.PlotXSecondary([0.0, 5], [0.0, 1]);
        axes.YAxes.Bottom.Add().Axis.Link = AxisTransform.Linear(2, 0);
        var placement = new CartesianAxisPlacement(axes, Plot, new RecordingRenderContext(), Theme.Default);
        var range = placement.ComputeDataRanges();

        var top = placement.PlaceDrawnTopAxis(range)!.Value;
        var bottom = Assert.Single(placement.PlaceBottomAxes(range, 30));

        Assert.Same(EdgeTerms.Top, top.Edge);
        Assert.Equal(Plot.Y, top.EdgeLine);
        Assert.Same(axes.XSecondarySeries, top.Series);
        Assert.Null(top.Line);
        Assert.Same(axes.Spines.Bottom, bottom.Line);
        Assert.Equal(30 + axes.YAxes.TitleGapUnder(Theme.Default), bottom.Offset, 9);
        Assert.Equal(EdgeTerms.Bottom.EdgeAt(Plot, bottom.Offset), bottom.EdgeLine, 9);
        Assert.True(bottom.Band > 0);
    }

    [Fact]
    public void TheTopAxis_IsPlacedOnlyWhenItIsDrawn_WithSeriesOrALink()
    {
        var axes = new Axes();
        axes.Plot([0.0, 1], [0.0, 1]);
        axes.TwinY();
        var placement = new CartesianAxisPlacement(axes, Plot, new RecordingRenderContext(), Theme.Default);
        var range = placement.ComputeDataRanges();

        Assert.Null(placement.PlaceDrawnTopAxis(range));
        axes.SecondaryXAxis!.Link = AxisTransform.Linear(2, 0);
        Assert.NotNull(placement.PlaceDrawnTopAxis(range));
        Assert.Null(new CartesianAxisPlacement(new Axes(), Plot, new RecordingRenderContext(), Theme.Default).PlaceDrawnTopAxis(range));
    }

    [Fact]
    public void TheOppositeDraw_HangsTheSameTicks_FromTheSpineAcrossThePlot()
    {
        var axes = new Axes();
        axes.Plot([0.0, 1], [0.0, 1]);
        var placement = new CartesianAxisPlacement(axes, Plot, new RecordingRenderContext(), Theme.Default);
        var range = placement.ComputeDataRanges();
        var x = placement.PlacePrimaryX(range, placement.PrimaryView(range));
        var draw = x.TickDraw(Theme.Default, axes.Spines);

        var opposite = x.OppositeDraw(draw, Plot, axes.Spines, Theme.Default);

        Assert.Equal(TickSide.Bottom, draw.Side);
        Assert.Equal(Plot.Y + Plot.Height, draw.AxisEdge, 9);
        Assert.Equal(TickSide.Top, opposite.Side);
        Assert.Equal(Plot.Y, opposite.AxisEdge, 9);
        Assert.Equal(draw.LabelRotation, opposite.LabelRotation);
    }

    [Fact]
    public void TheLabelsOnItsOwnSide_AreMeasured_AndNoneWhenTheyStandOffIt()
    {
        var axes = new Axes();
        axes.Plot([0.0, 1], [0.0, 1]);
        var placement = new CartesianAxisPlacement(axes, Plot, new RecordingRenderContext(), Theme.Default);
        var range = placement.ComputeDataRanges();
        var x = placement.PlacePrimaryX(range, placement.PrimaryView(range));

        Assert.True(x.OwnSideLabelReach(new RecordingRenderContext(), Theme.Default, axes.Spines) > 0);
        axes.XAxis.MajorTicks = axes.XAxis.MajorTicks with { LabelsOnOwnSide = false };
        Assert.Equal(0, x.OwnSideLabelReach(new RecordingRenderContext(), Theme.Default, axes.Spines));
    }

    [Fact]
    public void AnAxisPastThePrimary_IsDrawn_WithSeriesOrALink()
    {
        var axis = new Axis();

        Assert.False(axis.IsDrawnWith([]));
        Assert.True(axis.IsDrawnWith([new LineSeries([0.0], [0.0])]));
        axis.Link = AxisTransform.Linear(2, 0);
        Assert.True(axis.IsDrawnWith([]));
    }

    // ── the seats of the identity order ──

    [Fact]
    public void TheSeats_FollowTheIdentityOrder_EachSideNumberedOnItsOwnList()
    {
        var axes = new Axes();
        axes.Plot([0.0, 1], [0.0, 1]);
        axes.Plot([0.0, 1], [0.0, 1]);
        var right0 = axes.YAxes.Right.Add();
        right0.AddSeries(new LineSeries([0.0, 1], [0.0, 1]));
        right0.AddSeries(new LineSeries([0.0, 1], [0.0, 1]));
        var right1 = axes.YAxes.Right.Add();
        var left1 = axes.YAxes.Left.Add();
        left1.AddSeries(new LineSeries([0.0, 1], [0.0, 1]));
        var left2 = axes.YAxes.Left.Add();

        var seats = axes.YSlotSeats();

        Assert.Equal(
            [new YSlotSeat(axes.YAxes.Left[0], SeriesAxisPair.Primary, 0),
             new YSlotSeat(right0, SeriesAxisPair.SecondaryY, 0),
             new YSlotSeat(right1, SeriesAxisPair.SecondaryY, 2),
             new YSlotSeat(left1, SeriesAxisPair.SecondaryLeftY, 0),
             new YSlotSeat(left2, SeriesAxisPair.SecondaryLeftY, 1)],
            seats);
        Assert.Equal(axes.YAxes.IdentityOrder, seats.Select(s => s.Slot));
    }
}
