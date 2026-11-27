// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.TextMeasurement;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>What an edge of the plot means to the axis that hangs its ticks from it, worked out once per edge
/// (<see cref="EdgeTerms"/>): the four edges are each other's mirror image, and every value below is the one the drawing had at
/// each of its sites before.</summary>
public class EdgeTermsTests
{
    private static readonly Rect Plot = new(100, 20, 400, 300);

    public static TheoryData<string> Sides => new() { "Bottom", "Top", "Left", "Right" };

    [Theory]
    [MemberData(nameof(Sides))]
    public void TheTermsOfASide_AreDerivedOnce_AndNameTheirSide(string name)
    {
        var side = Enum.Parse<TickSide>(name);
        var edge = EdgeTerms.Of(side);

        Assert.Same(edge, EdgeTerms.Of(side));
        Assert.Equal(side, edge.Side);
        Assert.Equal(side is TickSide.Bottom or TickSide.Top, edge.AlongX);
        Assert.Equal(side is TickSide.Bottom or TickSide.Right ? 1 : -1, edge.Outward);
    }

    [Fact]
    public void AValueThatIsNoSide_IsRefused()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => EdgeTerms.Of((TickSide)99));
        Assert.Throws<ArgumentOutOfRangeException>(() => EdgeTerms.Of((AxisSide)99));
    }

    [Fact]
    public void ASideOfTheContainer_IsTheEdgeItsAxesStandOn()
    {
        Assert.Same(EdgeTerms.Left, EdgeTerms.Of(AxisSide.Left));
        Assert.Same(EdgeTerms.Right, EdgeTerms.Of(AxisSide.Right));
        Assert.Same(EdgeTerms.Bottom, EdgeTerms.Of(AxisSide.Bottom));
    }

    [Fact]
    public void EachEdge_MeetsTheSpineOnItsSide_AndNumbersTheSeriesPastThePrimaryOnItsList()
    {
        var spines = new SpinesConfig
        {
            Top = new SpineConfig { LineWidth = 1 },
            Bottom = new SpineConfig { LineWidth = 2 },
            Left = new SpineConfig { LineWidth = 3 },
            Right = new SpineConfig { LineWidth = 4 },
        };

        Assert.Same(spines.Top, EdgeTerms.Top.SpineOf(spines));
        Assert.Same(spines.Bottom, EdgeTerms.Bottom.SpineOf(spines));
        Assert.Same(spines.Left, EdgeTerms.Left.SpineOf(spines));
        Assert.Same(spines.Right, EdgeTerms.Right.SpineOf(spines));
        Assert.Equal(SpineEdge.Left, EdgeTerms.Left.SpineEdge);
        Assert.Equal(SeriesAxisPair.SecondaryLeftY, EdgeTerms.Left.OutwardPair);
        Assert.Equal(SeriesAxisPair.SecondaryY, EdgeTerms.Right.OutwardPair);
        Assert.Equal(SeriesAxisPair.SecondaryX, EdgeTerms.Top.OutwardPair);
        Assert.Equal(SeriesAxisPair.Primary, EdgeTerms.Bottom.OutwardPair);
    }

    /// <summary>A label below the plot hangs from its end when turned, one above it from its start (the point reflection), and both
    /// are centred upright; a label beside the plot is anchored away from it at any rotation.</summary>
    [Theory]
    [InlineData("Bottom", 0.0, TextAlignment.Center)]
    [InlineData("Bottom", 30.0, TextAlignment.Right)]
    [InlineData("Top", 0.0, TextAlignment.Center)]
    [InlineData("Top", -45.0, TextAlignment.Left)]
    [InlineData("Left", 0.0, TextAlignment.Right)]
    [InlineData("Left", 20.0, TextAlignment.Right)]
    [InlineData("Right", 0.0, TextAlignment.Left)]
    [InlineData("Right", 20.0, TextAlignment.Left)]
    public void ALabel_IsAnchoredByItsEdge_AndTheRotationItIsDrawnAt(string name, double rotation, TextAlignment alignment)
    {
        var side = Enum.Parse<TickSide>(name);
        Assert.Equal(alignment, EdgeTerms.Of(side).AlignmentFor(rotation));
    }

    [Theory]
    [InlineData("Left", 0, 100)]
    [InlineData("Left", 30, 70)]
    [InlineData("Right", 0, 500)]
    [InlineData("Right", 30, 530)]
    [InlineData("Bottom", 0, 320)]
    [InlineData("Bottom", 25, 345)]
    [InlineData("Top", 0, 20)]
    [InlineData("Top", 15, 5)]
    public void AnEdge_StandsItsOffsetOutPastThePlotsEdgeOnItsSide(string name, double offset, double edge)
    {
        var side = Enum.Parse<TickSide>(name);
        Assert.Equal(edge, EdgeTerms.Of(side).EdgeAt(Plot, offset));
    }

    /// <summary>A spine left where it is stands on the plot's edge on its side; a spine moved to a data value stands there.</summary>
    [Fact]
    public void TheSpineOfAnEdge_StandsWhereItWasPut()
    {
        var transform = new DataTransform(0, 10, 0, 10, Plot);
        var spines = new SpinesConfig
        {
            Bottom = new SpineConfig { Position = SpinePosition.Data, PositionValue = 5 },
            Right = new SpineConfig { Position = SpinePosition.Data, PositionValue = 5 },
        };

        Assert.Equal(20, EdgeTerms.Top.SpineAt(spines, Plot, transform));
        Assert.Equal(100, EdgeTerms.Left.SpineAt(spines, Plot, transform));
        Assert.Equal(170, EdgeTerms.Bottom.SpineAt(spines, Plot, transform), 9);
        Assert.Equal(300, EdgeTerms.Right.SpineAt(spines, Plot, transform), 9);
    }

    [Theory]
    [MemberData(nameof(Sides))]
    public void Hang_FacesTheDrawingToItsEdge_AndHangsItFromTheEdge_HalfTheEdgesSpineIn(string name)
    {
        var side = Enum.Parse<TickSide>(name);
        var spines = new SpinesConfig
        {
            Top = new SpineConfig { LineWidth = 1 },
            Bottom = new SpineConfig { LineWidth = 2 },
            Left = new SpineConfig { LineWidth = 3 },
            Right = new SpineConfig { LineWidth = 4 },
        };
        var edge = EdgeTerms.Of(side);
        var drawn = new TickDrawContext { Side = TickSide.Bottom, Alignment = TextAlignment.Center, LabelRotation = 30, Theme = Theme.Default };

        var hung = edge.Hang(drawn, 42, spines, Theme.Default);

        Assert.Equal(side, hung.Side);
        Assert.Equal(edge.AlignmentFor(30), hung.Alignment);
        Assert.Equal(42, hung.AxisEdge);
        Assert.Equal(edge.SpineOf(spines).HalfWidthIn(Theme.Default), hung.SpineHalf);
        Assert.Equal(30, hung.LabelRotation);
        Assert.Equal(edge.Facing(drawn) with { AxisEdge = 42, SpineHalf = hung.SpineHalf }, hung);
    }

    /// <summary>Where a tick label is anchored: past the mark and the pad (<c>reach</c>) out from the edge, level with its tick. How the
    /// label stands on that point is its vertical alignment (<see cref="EdgeTerms.LabelVertical"/>), which the box turns into a baseline.</summary>
    [Theory]
    [InlineData("Bottom", 250.0, 300 + 7.0)]
    [InlineData("Top", 250.0, 300 - 7.0)]
    [InlineData("Left", 300 - 7.0, 250.0)]
    [InlineData("Right", 300 + 7.0, 250.0)]
    public void ALabel_StandsPastTheMarkAndPad_OnItsEdgesSide(string name, double x, double y)
    {
        var side = Enum.Parse<TickSide>(name);
        Assert.Equal(new Point(x, y), EdgeTerms.Of(side).LabelAnchor(250, 300, 7));
    }

    /// <summary>How a tick label stands on its anchor, as matplotlib's tick labels do (<c>va</c>): it hangs from it below a bottom edge
    /// ('top'), stands on its baseline above a top edge ('baseline'), and is centred on its tick beside a left or a right edge
    /// ('center_baseline', centred by the box's one centre rule).</summary>
    [Theory]
    [InlineData("Bottom", TextVerticalAlignment.Top)]
    [InlineData("Top", TextVerticalAlignment.Baseline)]
    [InlineData("Left", TextVerticalAlignment.Center)]
    [InlineData("Right", TextVerticalAlignment.Center)]
    public void ALabel_StandsOnItsAnchorByItsEdgesVerticalAlignment(string name, TextVerticalAlignment vertical)
    {
        Assert.Equal(vertical, EdgeTerms.Of(Enum.Parse<TickSide>(name)).LabelVertical);
    }

    /// <summary>The offset text of an axis stands once at its end: below the labels of a bottom edge and above those of a top edge,
    /// ending at the plot's right; above the plot's top for a left or a right edge, starting at its left or ending at its right.</summary>
    [Theory]
    [InlineData("Bottom", 500.0, 320 + 15 + 3.0, TextAlignment.Right, TextVerticalAlignment.Top)]
    [InlineData("Top", 500.0, 20 - 15 - 3.0, TextAlignment.Right, TextVerticalAlignment.Baseline)]
    [InlineData("Left", 100.0, 17.0, TextAlignment.Left, TextVerticalAlignment.Baseline)]
    [InlineData("Right", 500.0, 17.0, TextAlignment.Right, TextVerticalAlignment.Baseline)]
    public void TheOffsetText_StandsAtTheEndOfItsAxis(string name, double x, double y, TextAlignment alignment, TextVerticalAlignment vertical)
    {
        var side = Enum.Parse<TickSide>(name);
        var edge = EdgeTerms.Of(side);

        var at = edge.OffsetTextAt(Plot, edge.EdgeAt(Plot, 0), 15, 3);

        Assert.Equal(new TextPlacement(new Point(x, y), alignment) { Vertical = vertical }, at);
    }

    /// <summary>How far a turned label reaches away from its edge, from the box it is drawn in turned and upright: below a bottom
    /// edge its height and what the turn carries its foot further down, above a top edge its height and what the turn carries its
    /// top further up, beside a left or a right edge the far side of the turned box.</summary>
    [Fact]
    public void ATurnedLabel_ReachesAwayFromItsEdge_ByTheFarSideOfItsTurnedBox()
    {
        var size = new TextExtent(60, 14.4, 12);
        var turned = new Rect(-52, -40, 50, 45);
        var upright = new Rect(-60, -12, 60, 14.4);

        Assert.Equal(14.4 + (5 - 2.4), EdgeTerms.Bottom.TurnedAway(turned, upright, size), 12);
        Assert.Equal(14.4 + (40 - 12), EdgeTerms.Top.TurnedAway(turned, upright, size), 12);
        Assert.Equal(52, EdgeTerms.Left.TurnedAway(turned, upright, size), 12);
        Assert.Equal(-2, EdgeTerms.Right.TurnedAway(turned, upright, size), 12);
    }

    /// <summary>The ink of a title beside an edge: its ascent faces the tick labels below a bottom edge; above a top edge its baseline
    /// faces them, as matplotlib sets its top X label (<c>va='baseline'</c>), its descent dipping into the gap; a turned title's baseline
    /// faces them on the left and the body of its text on the right, its full ascent or descent reaching away.</summary>
    [Fact]
    public void TheInkOfATitle_FacesTheTickLabelsOfItsEdge()
    {
        Assert.Equal(new FacingInk(FacingInk.Ascent, FacingInk.Descent), EdgeTerms.Bottom.Ink);
        Assert.Equal(new FacingInk(0, FacingInk.Ascent, FacingInk.Descent), EdgeTerms.Top.Ink);
        Assert.Equal(new FacingInk(FacingInk.TurnedSink, FacingInk.Ascent), EdgeTerms.Left.Ink);
        Assert.Equal(new FacingInk(FacingInk.TurnedRise, FacingInk.Descent), EdgeTerms.Right.Ink);
    }

    /// <summary>The edge past a strip that runs along a direction, where a colour bar hangs its marks: the foot of one along X, the right
    /// side of one along Y; it stands on the strip's far side across it.</summary>
    [Fact]
    public void TheEdgePastAStrip_IsItsFootAlongX_AndItsRightSideAlongY()
    {
        var strip = new Rect(10, 20, 300, 40);

        Assert.Same(EdgeTerms.Bottom, EdgeTerms.Past(true));
        Assert.Same(EdgeTerms.Right, EdgeTerms.Past(false));
        Assert.Equal(60, EdgeTerms.Past(true).EdgeAt(strip, 0));
        Assert.Equal(310, EdgeTerms.Past(false).EdgeAt(strip, 0));
    }
}
