// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.Layout;

namespace MatPlotLibNet.Tests.Rendering.Layout;

/// <summary>The one rule for where an axes' need goes in one direction of the grid: the margin of an edge its cell touches, the gap
/// beside it otherwise, the needs that meet in a gap stacked, and the margins and the gap held in the bounds of the layout.</summary>
public sealed class AxisMarginsTests
{
    private static readonly Sides<bool> Alone = new(Left: true, Bottom: true, Top: true, Right: true);
    private static readonly Sides<bool> Inside = new(Left: false, Bottom: false, Top: false, Right: false);
    private static readonly Sides<bool> FirstColumn = new(Left: true, Bottom: true, Top: true, Right: false);
    private static readonly Sides<bool> LastColumn = new(Left: false, Bottom: true, Top: true, Right: true);
    private static readonly Sides<bool> TopRow = new(Left: true, Bottom: false, Top: true, Right: true);
    private static readonly Sides<bool> BottomRow = new(Left: true, Bottom: true, Top: false, Right: true);

    private static readonly LayoutPads NoShare = LayoutPads.Library;
    private static readonly LayoutPads ShareFive = LayoutPads.Library with { Shared = 5 };

    /// <summary>No bound at all: what the needs give is what is read.</summary>
    private static readonly LayoutBounds Unbounded = new(
        new Sides<MarginBounds>(MarginBounds.None, MarginBounds.None, MarginBounds.None, MarginBounds.None), MarginBounds.None, MarginBounds.None);

    /// <summary>Two columns that made their demands, stacked with no share and held to the library's bounds.</summary>
    private static ConstrainedLayoutEngine.AxisMargins Demanded(AxisNeeds first, AxisNeeds last)
    {
        var across = ConstrainedLayoutEngine.AxisMargins.Across(0, 0, 0);
        across.Demand(FirstColumn, first);
        across.Demand(LastColumn, last);
        across.Stacked(NoShare);
        across.Bound(LayoutBounds.Library);
        return across;
    }

    [Fact]
    public void AnAxesOnBothEdges_WidensBothMargins_AndNotTheGap()
    {
        var across = ConstrainedLayoutEngine.AxisMargins.Across(10, 10, 20);

        across.Demand(Alone, new AxisNeeds(Start: 30, End: 40, StartBand: 0, EndBand: 0));
        across.Stacked(NoShare);

        Assert.Equal((30.0, 40.0, 20.0), (across.Start, across.End, across.Gap));
    }

    [Fact]
    public void ANeedNarrowerThanTheMarginItHas_LeavesTheMargin()
    {
        var across = ConstrainedLayoutEngine.AxisMargins.Across(50, 50, 0);

        across.Demand(Alone, new AxisNeeds(30, 40, 0, 0));

        Assert.Equal((50.0, 50.0), (across.Start, across.End));
    }

    /// <summary>Two columns: the first column's right need and the last column's left need meet in the gap, each with its pad, and
    /// the gap holds both, less what the pads share.</summary>
    [Theory]
    [InlineData(0, 70)]
    [InlineData(5, 65)]
    public void TheNeedsThatMeetInAGap_Stack(double shared, double gap)
    {
        var across = ConstrainedLayoutEngine.AxisMargins.Across(0, 0, 10);

        across.Demand(FirstColumn, new AxisNeeds(Start: 25, End: 30, 0, 0));
        across.Demand(LastColumn, new AxisNeeds(Start: 40, End: 15, 0, 0));
        across.Stacked(LayoutPads.Library with { Shared = shared });

        Assert.Equal((25.0, 15.0, gap), (across.Start, across.End, across.Gap));
    }

    [Fact]
    public void AGapNeverHoldsLessThanTheWidestNeedInIt_WhateverThePadsShare()
    {
        var across = ConstrainedLayoutEngine.AxisMargins.Across(0, 0, 0);

        across.Demand(FirstColumn, new AxisNeeds(0, 3, 0, 0));
        across.Demand(LastColumn, new AxisNeeds(40, 0, 0, 0));
        across.Stacked(LayoutPads.Library with { Shared = 10 });

        Assert.Equal(40, across.Gap);
    }

    [Fact]
    public void DownTheGrid_TheTopIsTheStart_AndTheBottomTheEnd()
    {
        var down = ConstrainedLayoutEngine.AxisMargins.Down(0, 0, 0);

        down.Demand(TopRow, new AxisNeeds(Start: 12, End: 30, 0, 0));
        down.Demand(BottomRow, new AxisNeeds(Start: 20, End: 18, 0, 0));
        down.Stacked(NoShare);

        Assert.Equal((12.0, 18.0, 50.0), (down.Start, down.End, down.Gap));
    }

    /// <summary>What stands above every axes at the start (the figure's title) is stacked under the need of an axes on that edge, not
    /// under one inside the grid.</summary>
    [Fact]
    public void ALiftAtTheStart_IsAddedToTheNeedOfAnAxesOnTheStartEdgeOnly()
    {
        var down = ConstrainedLayoutEngine.AxisMargins.Down(0, 0, 0, aboveTopRow: 25);

        down.Demand(TopRow, new AxisNeeds(Start: 10, End: 0, 0, 0));
        down.Demand(BottomRow, new AxisNeeds(Start: 30, End: 0, 0, 0));
        down.Stacked(NoShare);

        Assert.Equal((35.0, 30.0), (down.Start, down.Gap));
    }

    [Theory]
    [InlineData(true, 11.0, 22.0)]
    [InlineData(false, 13.0, 24.0)]
    public void ANeedAtAnEdge_GoesToTheMarginOrTheGapWithItsOwnValue(bool atStart, double margin, double gap)
    {
        var across = ConstrainedLayoutEngine.AxisMargins.Across(0, 0, 0);

        if (atStart)
        {
            across.NeedAtStart(FirstColumn, atEdge: margin, inGap: 99);
            across.NeedAtStart(LastColumn, atEdge: 99, inGap: gap);
        }
        else
        {
            across.NeedAtEnd(LastColumn, atEdge: margin, inGap: 99);
            across.NeedAtEnd(FirstColumn, atEdge: 99, inGap: gap);
        }

        across.Stacked(NoShare);

        Assert.Equal((margin, gap), (atStart ? across.Start : across.End, across.Gap));
    }

    /// <summary>A need measured only on the start edge is measured for an axes there, and never for one inside the grid.</summary>
    [Fact]
    public void ANeedOfTheStartEdge_IsMeasuredOnlyForAnAxesOnThatEdge()
    {
        var counter = new Counter();
        var down = ConstrainedLayoutEngine.AxisMargins.Down(5, 0, 0);

        down.NeedAtStartEdge(BottomRow, new CountedNeed(counter, 99));
        down.NeedAtStartEdge(TopRow, new CountedNeed(counter, 17));

        Assert.Equal(1, counter.Reads);
        Assert.Equal(17, down.Start);
    }

    /// <summary>The title of a lower row stands in the gap the theme keeps unless the row above draws below it: it is stacked on an
    /// end need there, and only then; it is never measured for an axes in the first row.</summary>
    [Fact]
    public void ANeedBesideAnEndNeed_IsStackedOnlyWhenAnEndNeedIsInTheGap()
    {
        var counter = new Counter();
        var alone = ConstrainedLayoutEngine.AxisMargins.Down(0, 0, 10);
        alone.BesideEnd(TopRow, new CountedNeed(counter, 99));
        alone.BesideEnd(BottomRow, new CountedNeed(counter, 30));
        alone.Stacked(NoShare);

        var stacked = ConstrainedLayoutEngine.AxisMargins.Down(0, 0, 10);
        stacked.BesideEnd(BottomRow, new CountedNeed(counter, 30));
        stacked.NeedAtEnd(TopRow, atEdge: 0, inGap: 25);
        stacked.Stacked(NoShare);

        Assert.Equal(2, counter.Reads);
        Assert.Equal(10, alone.Gap);
        Assert.Equal(55, stacked.Gap);
    }

    [Fact]
    public void ABandOnAnInsideSide_WidensTheGapPastTheRestingGap_AndOnAnEdgeNothing()
    {
        var across = ConstrainedLayoutEngine.AxisMargins.Across(0, 0, 40);

        across.Widen(Alone, atStart: 100, atEnd: 100);
        Assert.Equal(40, across.Gap);

        across.Widen(FirstColumn, atStart: 100, atEnd: 15);
        Assert.Equal(55, across.Gap);

        across.Widen(LastColumn, atStart: 30, atEnd: 100);
        Assert.Equal(70, across.Gap);
    }

    [Fact]
    public void WhatStandsAtTheEndPastEveryAxes_WidensTheEndMargin_AndLiftsItsCap()
    {
        var across = ConstrainedLayoutEngine.AxisMargins.Across(0, 20, 0);

        across.AtEnd(180);
        across.Bound(LayoutBounds.Library);

        Assert.Equal(180, across.End);
    }

    [Fact]
    public void WhatTheAxesAtTheEndDrawPastTheirSpine_IsReadFromTheEndColumnOnly()
    {
        var across = ConstrainedLayoutEngine.AxisMargins.Across(0, 0, 0);

        across.Demand(FirstColumn, new AxisNeeds(0, 0, 0, 0, PastEnd: 90));
        across.Demand(LastColumn, new AxisNeeds(0, 0, 0, 0, PastEnd: 35));

        Assert.Equal(35, across.PastEnd);
    }

    /// <summary>The library's caps cut a margin that is only estimated, and a band measured on the edge lifts the cap to it.</summary>
    [Fact]
    public void TheCapOfAMargin_IsLiftedByWhatIsMeasuredOnItsEdge_AndNoWhereElse()
    {
        var estimated = ConstrainedLayoutEngine.AxisMargins.Across(0, 0, 0);
        estimated.Demand(Alone, new AxisNeeds(Start: 200, End: 200, StartBand: 0, EndBand: 0));
        estimated.Bound(LayoutBounds.Library);

        var measured = ConstrainedLayoutEngine.AxisMargins.Across(0, 0, 0);
        measured.Demand(Alone, new AxisNeeds(Start: 200, End: 200, StartBand: 200, EndBand: 160));
        measured.Bound(LayoutBounds.Library);

        var inside = ConstrainedLayoutEngine.AxisMargins.Across(0, 0, 0);
        inside.Demand(Alone, new AxisNeeds(Start: 200, End: 200, StartBand: 0, EndBand: 0));
        inside.Demand(Inside, new AxisNeeds(Start: 0, End: 0, StartBand: 300, EndBand: 300));
        inside.Bound(LayoutBounds.Library);

        Assert.Equal((120.0, 140.0), (estimated.Start, estimated.End));
        Assert.Equal((200.0, 160.0), (measured.Start, measured.End));
        Assert.Equal((120.0, 140.0), (inside.Start, inside.End));
    }

    /// <summary>A band measured at the start edge lifts the cap by the band and the lift above it (the figure's title over a top
    /// band); a band of nothing lifts nothing.</summary>
    [Fact]
    public void ABandAtTheStartEdge_LiftsTheCapPastTheLift()
    {
        var down = ConstrainedLayoutEngine.AxisMargins.Down(0, 0, 0, aboveTopRow: 40);

        down.Demand(TopRow, new AxisNeeds(Start: 120, End: 0, StartBand: 110, EndBand: 0));
        down.Bound(LayoutBounds.Library);

        Assert.Equal(150, down.Start);
    }

    [Fact]
    public void TheFloorOfAMargin_HoldsAMarginTheNeedsLeaveNarrower()
    {
        var across = ConstrainedLayoutEngine.AxisMargins.Across(0, 0, -4);

        across.Bound(LayoutBounds.Library);

        Assert.Equal((30.0, 10.0, 0.0), (across.Start, across.End, across.Gap));
    }

    /// <summary>The cap of a gap is lifted to the needs stacked in it only once an axes is measured to draw into it.</summary>
    [Fact]
    public void TheCapOfAGap_IsLiftedByTheStackedNeeds_WhenABandIsMeasuredInIt()
    {
        var estimated = Demanded(new AxisNeeds(0, 100, 0, 0), new AxisNeeds(100, 0, 0, 0));
        var measured = Demanded(new AxisNeeds(0, 100, 0, 100), new AxisNeeds(100, 0, 0, 0));
        var startBand = Demanded(new AxisNeeds(0, 100, 0, 0), new AxisNeeds(100, 0, 100, 0));

        Assert.Equal(150, estimated.Gap);
        Assert.Equal(200, measured.Gap);
        Assert.Equal(200, startBand.Gap);
    }

    [Fact]
    public void WithNoBound_TheMarginsAndTheGapAreWhatTheNeedsGive()
    {
        var across = ConstrainedLayoutEngine.AxisMargins.Across(0, 0, 0);

        across.Demand(FirstColumn, new AxisNeeds(500, 300, 0, 0));
        across.Demand(LastColumn, new AxisNeeds(400, 2, 0, 0));
        across.Stacked(ShareFive);
        across.Bound(Unbounded);

        Assert.Equal((500.0, 2.0, 695.0), (across.Start, across.End, across.Gap));
    }

    private sealed class Counter
    {
        internal int Reads;
    }

    private readonly record struct CountedNeed(Counter Counter, double Value) : ConstrainedLayoutEngine.ILayoutNeed
    {
        public double Need()
        {
            Counter.Reads++;
            return Value;
        }
    }
}
