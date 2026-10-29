// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A Y axis slot as one render places it (<see cref="PlacedYSlot"/>), the side terms it is drawn by
/// (<see cref="YSideTerms"/>) and the seat its series hold in the identity order
/// (<see cref="SeriesSlotExtensions.YSlotSeats"/>).</summary>
public class PlacedYSlotTests
{
    private static readonly Rect Plot = new(100, 20, 400, 300);

    // ── the side terms ──

    [Fact]
    public void TheSideTerms_OfEachSide_AreDerivedOnce_FromItsSide()
    {
        Assert.Equal(YSideTerms.Left, YSideTerms.Of(AxisSide.Left));
        Assert.Equal(YSideTerms.Right, YSideTerms.Of(AxisSide.Right));
        Assert.Equal(TickSide.Left, YSideTerms.Left.TickSide);
        Assert.Equal(TickSide.Right, YSideTerms.Right.TickSide);
        Assert.Equal(TextAlignment.Right, YSideTerms.Left.LabelAlignment);
        Assert.Equal(TextAlignment.Left, YSideTerms.Right.LabelAlignment);
        Assert.Equal(SeriesAxisPair.SecondaryLeftY, YSideTerms.Left.OutwardPair);
        Assert.Equal(SeriesAxisPair.SecondaryY, YSideTerms.Right.OutwardPair);
    }

    [Theory]
    [InlineData(AxisSide.Left, 0, 100)]
    [InlineData(AxisSide.Left, 30, 70)]
    [InlineData(AxisSide.Right, 0, 500)]
    [InlineData(AxisSide.Right, 30, 530)]
    public void ASlotsEdge_StandsItsOffsetOutPastThePlotsEdgeOnItsSide(AxisSide side, double offset, double edge)
    {
        Assert.Equal(edge, YSideTerms.Of(side).EdgeX(Plot, offset));
    }

    [Theory]
    [InlineData(AxisSide.Left)]
    [InlineData(AxisSide.Right)]
    public void Hang_FacesTheDrawingToItsSide_AndHangsItFromTheEdge_HalfTheSidesSpineIn(AxisSide side)
    {
        var spines = new SpinesConfig
        {
            Left = new SpineConfig { LineWidth = 2 },
            Right = new SpineConfig { LineWidth = 3 },
        };
        var terms = YSideTerms.Of(side);

        var hung = terms.Hang(new TickDrawContext { Side = TickSide.Bottom, Alignment = TextAlignment.Center }, 42, spines);

        Assert.Equal(terms.TickSide, hung.Side);
        Assert.Equal(terms.LabelAlignment, hung.Alignment);
        Assert.Equal(42, hung.AxisEdge);
        Assert.Equal(side == AxisSide.Left ? 1.0 : 1.5, hung.SpineHalf);
    }

    // ── a placed slot ──

    /// <summary>A slot's tick labels are drawn and measured in one context: the side's alignment (away from the plot), its
    /// own formatter or the uniform format of its ticks, its rotation, the theme's tick font and foreground.</summary>
    [Theory]
    [InlineData("Left", TextAlignment.Right)]
    [InlineData("Right", TextAlignment.Left)]
    public void APlacedSlotsTickDraw_AlignsAwayFromThePlot_InTheThemesTickFont(string side, TextAlignment alignment)
    {
        var axes = new Axes();
        var slot = axes.YAxes.StackOn(Enum.Parse<AxisSide>(side)).Add();
        slot.Axis.MajorTicks = slot.Axis.MajorTicks with { LabelRotation = 15 };
        var theme = Theme.Default;
        var placed = new CartesianAxesRenderer(axes, default, new RecordingRenderContext(), theme)
            .PlaceSlot(slot, new MinMaxRange(0, 1)) with { Ticks = [0.0, 0.5, 1.0] };

        var draw = placed.TickDraw(theme);

        Assert.Equal(side == "Left" ? TickSide.Left : TickSide.Right, draw.Side);
        Assert.Equal(alignment, draw.Alignment);
        Assert.Equal(15, draw.LabelRotation);
        Assert.Equal(ThemedFontProvider.TickFont(theme), draw.BaseFont);
        Assert.Equal(theme.ForegroundText, draw.Foreground);
        Assert.Null(draw.Formatter);
        Assert.Equal("0.5", draw.LabelText(0.5));
    }

    [Fact]
    public void APlacedSlotsEdge_IsItsSidesEdge_PastItsOffset()
    {
        var axes = new Axes();
        var slot = axes.YAxes.Left.Add();
        var placed = new CartesianAxesRenderer(axes, Plot, new RecordingRenderContext(), Theme.Default)
            .PlaceSlot(slot, new MinMaxRange(0, 1)) with { Offset = 25 };

        Assert.Equal(YSideTerms.Left, placed.Terms);
        Assert.Equal(75, placed.EdgeX(Plot));
    }

    // ── the seats of the identity order ──

    [Fact]
    public void TheSeats_FollowTheIdentityOrder_EachSideNumberedOnItsOwnList()
    {
        var axes = new Axes();
        axes.Plot([0.0, 1], [0.0, 1]);
        axes.Plot([0.0, 1], [0.0, 1]);
        var right0 = axes.YAxes.Right.Add();
        right0.AddSeries(new MatPlotLibNet.Models.Series.LineSeries([0.0, 1], [0.0, 1]));
        right0.AddSeries(new MatPlotLibNet.Models.Series.LineSeries([0.0, 1], [0.0, 1]));
        var right1 = axes.YAxes.Right.Add();
        var left1 = axes.YAxes.Left.Add();
        left1.AddSeries(new MatPlotLibNet.Models.Series.LineSeries([0.0, 1], [0.0, 1]));
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
