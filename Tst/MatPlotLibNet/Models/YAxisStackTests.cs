// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Models;

/// <summary>Verifies the Y axis container of an <see cref="Axes"/>: one stack per side, the primary Y axis as slot 0
/// of the left side, as many further axes on either side as the caller adds, and the outward-only numbering the
/// interaction wire keeps.</summary>
public class YAxisStackTests
{
    private static LineSeries Line(string label) => new([0.0, 1], [0.0, 1]) { Label = label };

    [Fact]
    public void Add_CalledFourTimes_HoldsFourSlots()
    {
        var axes = new Axes();

        var added = Enumerable.Range(0, 4).Select(_ => axes.YAxes.Right.Add()).ToList();

        Assert.Equal(4, axes.YAxes.Right.Count);
        Assert.Equal(added, axes.YAxes.Right);
        Assert.Equal(4, added.Select(s => s.Axis).Distinct().Count());
        Assert.All(added, s => Assert.Equal(AxisSide.Right, s.Side));
    }

    [Fact]
    public void Add_OnTheLeft_StacksPastThePrimary_AsOftenAsCalled()
    {
        var axes = new Axes();

        var first = axes.YAxes.Left.Add();
        var second = axes.YAxes.Left.Add();

        Assert.Equal(3, axes.YAxes.Left.Count);
        Assert.Same(first, axes.YAxes.Left[1]);
        Assert.Same(second, axes.YAxes.Left[2]);
        Assert.NotSame(axes.YAxis, first.Axis);
        Assert.Equal(AxisSide.Left, second.Side);
    }

    [Fact]
    public void LeftSlotZero_IsThePrimaryYAxis()
    {
        var axes = new Axes();
        var primary = axes.YAxes.Left[0];

        Assert.Same(axes.YAxis, primary.Axis);
        Assert.Same(axes.Series, primary.Series);
        Assert.Equal(AxisSide.Left, primary.Side);
        Assert.Empty(axes.YAxes.Right);
    }

    [Fact]
    public void LeftSlotZero_SharesOneSeriesListWithTheAxes_BothWays()
    {
        var axes = new Axes();
        var viaAxes = axes.AddSeries(Line("viaAxes"));
        var viaSlot = axes.YAxes.Left[0].AddSeries(Line("viaSlot"));

        Assert.Equal([viaAxes, viaSlot], axes.Series);
        Assert.Equal([viaAxes, viaSlot], axes.YAxes.Left[0].Series);
    }

    [Fact]
    public void FirstOrAdd_NeverStacks()
    {
        var axes = new Axes();

        var first = axes.YAxes.Right.FirstOrAdd();
        var again = axes.YAxes.Right.FirstOrAdd();
        axes.YAxes.Right.Add();
        var afterAStack = axes.YAxes.Right.FirstOrAdd();

        Assert.Same(first, again);
        Assert.Same(first, afterAStack);
        Assert.Equal(2, axes.YAxes.Right.Count);
        Assert.Same(axes.YAxes.Left[0], axes.YAxes.Left.FirstOrAdd());
        Assert.Single(axes.YAxes.Left);
    }

    [Fact]
    public void OutwardIndex_IsContainerIndexMinusOneLeft_AndContainerIndexRight()
    {
        var axes = new Axes();
        axes.YAxes.Left.Add();
        axes.YAxes.Left.Add();
        axes.YAxes.Right.Add();
        axes.YAxes.Right.Add();

        Assert.Equal(1, axes.YAxes.Left.OutwardOffset);
        Assert.Equal(0, axes.YAxes.Right.OutwardOffset);
        Assert.Same(axes.YAxes.Left[1], axes.YAxes.Left.SlotAtOutward(0));
        Assert.Same(axes.YAxes.Left[2], axes.YAxes.Left.SlotAtOutward(1));
        Assert.Same(axes.YAxes.Right[0], axes.YAxes.Right.SlotAtOutward(0));
        Assert.Same(axes.YAxes.Right[1], axes.YAxes.Right.SlotAtOutward(1));
    }

    /// <summary>The D7 skip rule on the container: an outward index that names no slot is answered with
    /// <see langword="null"/>, never an exception, and the left side's -1 never reaches the primary.</summary>
    [Theory]
    [InlineData(AxisSide.Left, -1)]
    [InlineData(AxisSide.Left, 1)]
    [InlineData(AxisSide.Right, -1)]
    [InlineData(AxisSide.Right, 1)]
    public void SlotAtOutward_PastEitherEnd_IsNull(AxisSide side, int outward)
    {
        var axes = new Axes();
        axes.YAxes.Left.Add();
        axes.YAxes.Right.Add();

        Assert.Null(axes.YAxes.StackOn(side).SlotAtOutward(outward));
    }

    [Theory]
    [InlineData(AxisSide.Left)]
    [InlineData(AxisSide.Right)]
    public void SlotAtOutward_OnASideWithNoOutwardSlot_IsNull(AxisSide side)
    {
        var axes = new Axes();

        Assert.Null(axes.YAxes.StackOn(side).SlotAtOutward(0));
    }

    /// <summary>The series that stand past the primary on a side are counted by the stack itself: the left side's slot 0
    /// is the primary Y axis, whose series are <see cref="Axes.Series"/> and never part of the outward numbering.</summary>
    [Fact]
    public void OutwardSeriesCount_CountsOnlyTheSeriesOfTheSlotsPastThePrimary()
    {
        var axes = new Axes();
        axes.AddSeries(Line("primary"));
        var right0 = axes.YAxes.Right.Add();
        right0.AddSeries(Line("r0a"));
        right0.AddSeries(Line("r0b"));
        axes.YAxes.Right.Add().AddSeries(Line("r1"));
        axes.YAxes.Left.Add().AddSeries(Line("l1"));

        Assert.Equal(3, axes.YAxes.Right.OutwardSeriesCount);
        Assert.Equal(1, axes.YAxes.Left.OutwardSeriesCount);
    }

    [Theory]
    [InlineData(AxisSide.Left)]
    [InlineData(AxisSide.Right)]
    public void OutwardSeriesCount_OnASideWithNoOutwardSlot_IsZero(AxisSide side)
    {
        var axes = new Axes();
        axes.AddSeries(Line("primary"));

        Assert.Equal(0, axes.YAxes.StackOn(side).OutwardSeriesCount);
    }

    [Theory]
    [InlineData(AxisSide.Left)]
    [InlineData(AxisSide.Right)]
    public void StackOn_NamesTheStackOfThatSide(AxisSide side)
    {
        var axes = new Axes();

        var stack = axes.YAxes.StackOn(side);

        Assert.Equal(side, stack.Side);
        Assert.Same(side == AxisSide.Left ? axes.YAxes.Left : axes.YAxes.Right, stack);
    }

    [Fact]
    public void IdentityOrder_IsPrimaryRightLeftOutwardTopX()
    {
        var axes = new Axes();
        axes.AddXSecondarySeries(Line("top"));
        axes.YAxes.Left.Add().AddSeries(Line("left0"));
        axes.AddSeries(Line("primary0"));
        var right0 = axes.YAxes.Right.Add();
        right0.AddSeries(Line("right0a"));
        axes.YAxes.Right.Add().AddSeries(Line("right1"));
        right0.AddSeries(Line("right0b"));
        axes.YAxes.Left.Add().AddSeries(Line("left1"));

        Assert.Equal(
            [axes.YAxes.Left[0], axes.YAxes.Right[0], axes.YAxes.Right[1], axes.YAxes.Left[1], axes.YAxes.Left[2]],
            axes.YAxes.IdentityOrder);
        Assert.Equal(["primary0", "right0a", "right0b", "right1", "left0", "left1", "top"],
            axes.AllSeries.Select(s => s.Label));
    }

    [Fact]
    public void AStack_EnumeratesNearestThePlotFirst_ThroughBothEnumerators()
    {
        var axes = new Axes();
        var a = axes.YAxes.Right.Add();
        var b = axes.YAxes.Right.Add();

        var untyped = new List<object>();
        foreach (var slot in (IEnumerable)axes.YAxes.Right) untyped.Add(slot);

        Assert.Equal([a, b], axes.YAxes.Right.ToList());
        Assert.Equal([a, b], untyped);
    }

    [Fact]
    public void YAxisSlot_AddSeries_Null_Throws()
    {
        var axes = new Axes();

        Assert.Throws<ArgumentNullException>(() => axes.YAxes.Right.Add().AddSeries<LineSeries>(null!));
    }

    /// <summary>A series added to a slot past slot 0 lands on that slot's own list and on no other: slot 0 of the same side
    /// stays empty, and so does the left side's primary.</summary>
    [Fact]
    public void AddSeries_OnASlotPastSlotZero_LandsOnThatSlotsOwnList_NotOnSlotZero()
    {
        var axes = new Axes();
        axes.YAxes.Right.FirstOrAdd();
        var second = axes.YAxes.Right.Add();
        var onTheLeft = axes.YAxes.Left.Add();

        var onSecond = second.AddSeries(Line("second"));
        var onLeft = onTheLeft.AddSeries(Line("left"));

        Assert.Empty(axes.YAxes.Right[0].Series);
        Assert.Same(onSecond, Assert.Single(second.Series));
        Assert.Same(onLeft, Assert.Single(onTheLeft.Series));
        Assert.Empty(axes.Series);
    }

    [Fact]
    public void HasExplicitLimit_SeesALimitOnAnySlotOfEitherSide()
    {
        var axes = new Axes();
        axes.YAxes.Right.Add();
        var outer = axes.YAxes.Left.Add();
        Assert.False(axes.HasExplicitLimit);

        outer.Axis.Min = 0;
        outer.Axis.Max = 1;

        Assert.True(axes.HasExplicitLimit);
    }
}
