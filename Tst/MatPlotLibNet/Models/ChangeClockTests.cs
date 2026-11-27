// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Tests.Models;

/// <summary>The clock every change is stamped with, and the list that stamps its own: what a mark says, what an item
/// contributes to its holder's newest change, and that only a real change moves a mark.</summary>
public sealed class ChangeClockTests
{
    [Fact]
    public void EveryMark_IsLaterThanTheOneBefore()
    {
        long first = ChangeClock.Next();
        long second = ChangeClock.Next();

        Assert.True(second > first);
    }

    [Fact]
    public void ChangingASlotToAnotherValue_StoresItAndStampsALaterMark()
    {
        int slot = 1;
        long stamp = ChangeClock.Next();
        long before = stamp;

        Assert.True(ChangeClock.Change(ref slot, 2, ref stamp));

        Assert.Equal(2, slot);
        Assert.True(stamp > before);
    }

    [Fact]
    public void ChangingASlotToTheValueItHolds_LeavesTheMarkAlone()
    {
        string slot = "same";
        long stamp = 7;

        Assert.False(ChangeClock.Change(ref slot, "same", ref stamp));

        Assert.Equal(7, stamp);
    }

    [Fact]
    public void TheLaterOfTwoMarks_IsTheLarger()
    {
        Assert.Equal(9, ChangeClock.Latest(9, 4));
        Assert.Equal(9, ChangeClock.Latest(4, 9));
    }

    [Fact]
    public void WhatAnItemContributes_DependsOnWhetherItCanTell()
    {
        var series = new LineSeries([1.0], [1.0]);
        series.Label = "a";

        Assert.Equal(((IChangeTracked)series).LatestChange, ChangeClock.LatestOf(series));
        Assert.Equal(0, ChangeClock.LatestOf(null));
        Assert.Equal(0, ChangeClock.LatestOf(new AxisBreak(1, 2)));          // a record of the library: it cannot change
        Assert.Equal(long.MaxValue, ChangeClock.LatestOf(new ForeignSeries(new(0, 1, 0, 1)))); // written outside the library: it may
    }

    [Fact]
    public void AnAxesHoldingASeriesWrittenOutsideTheLibrary_AlwaysCountsAsChanged()
    {
        var axes = new Axes();
        axes.AddSeries(new ForeignSeries(new(0, 1, 0, 1)));

        Assert.Equal(long.MaxValue, axes.LatestChange);
    }

    [Fact]
    public void AStampedList_StampsWhatIsAddedAndTakenAway_AndNotWhatIsNotThere()
    {
        var list = new StampedList<AxisBreak>();
        var a = new AxisBreak(1, 2);
        long atStart = list.LatestChange;

        list.Add(a);
        long added = list.LatestChange;
        Assert.True(added > atStart);

        Assert.False(list.Remove(new AxisBreak(5, 6)));
        Assert.Equal(added, list.LatestChange);

        Assert.True(list.Remove(a));
        long removed = list.LatestChange;
        Assert.True(removed > added);
        Assert.Empty(list);

        list.Add(a);
        list.Add(new AxisBreak(3, 4));
        long two = list.LatestChange;
        list.RemoveAt(0);
        Assert.True(list.LatestChange > two);
        Assert.Equal(new AxisBreak(3, 4), Assert.Single(list));
        Assert.Equal(list.LatestChange, list.LatestAddOrRemove);
    }

    [Fact]
    public void AFormatterChangedInPlace_MarksTheAxisThatHoldsIt_Changed()
    {
        var axes = new Axes();
        var dates = new MatPlotLibNet.Rendering.TickFormatters.DateTickFormatter();
        var eng = new MatPlotLibNet.Rendering.TickFormatters.EngFormatter();
        axes.XAxis.TickFormatter = dates;
        axes.YAxis.TickFormatter = eng;

        long before = axes.LatestChange;
        dates.DateFormat = "HH:mm";
        Assert.True(axes.LatestChange > before, "a date formatter");

        before = axes.LatestChange;
        eng.Sep = "";
        Assert.True(axes.LatestChange > before, "an engineering formatter");
        Assert.Equal(((IChangeTracked)eng).LatestChange, axes.LatestChange);
    }

    [Fact]
    public void AnAxisAndAFigure_TellTheirNewestChangeThroughTheInterfaceToo()
    {
        var figure = new Figure();
        var axis = figure.AddSubPlot().XAxis;
        axis.Label = "x";
        figure.Title = "t";

        Assert.Equal(axis.LatestChange, ((IChangeTracked)axis).LatestChange);
        Assert.Equal(figure.LatestChange, ((IChangeTracked)figure).LatestChange);
    }

    [Fact]
    public void AnEmptyStampedList_HoldsNothing_AndRefusesAnIndex()
    {
        var list = new StampedList<string>();

        Assert.Empty(list);
        Assert.Equal(0, list.LatestChange);
        Assert.False(list.Remove("absent"));
        Assert.Throws<ArgumentOutOfRangeException>(() => list[0]);
        Assert.Throws<ArgumentOutOfRangeException>(() => list.RemoveAt(0));
        foreach (var item in list) Assert.Fail($"an empty list enumerated {item}");
    }

    [Fact]
    public void AStampedList_IsNewestOfItsOwnStampAndItsItems()
    {
        var list = new StampedList<LineSeries>();
        var line = new LineSeries([1.0], [1.0]);
        list.Add(line);
        long added = list.LatestAddOrRemove;

        line.Label = "later";

        Assert.Equal(added, list.LatestAddOrRemove);
        Assert.True(list.LatestChange > added);
        Assert.Same(line, list[0]);
        Assert.Equal([line], list.Cast<LineSeries>());
        Assert.Equal([line], ((System.Collections.IEnumerable)list).Cast<LineSeries>());
    }
}
