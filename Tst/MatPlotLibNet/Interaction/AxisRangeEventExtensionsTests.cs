// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Interaction;

/// <summary>The one place a list of <see cref="SecondaryAxisRange"/> is applied to an <see cref="Axes"/>. The numbering is the
/// interaction wire's, outward only: index 0 on a side is the first slot PAST the primary Y axis, so the container's left
/// slot 0 (the primary) is not addressable at all. The conversion to a container slot is
/// <see cref="YAxisStack.SlotAtOutward"/>'s, and a change that names no slot is skipped.</summary>
public class AxisRangeEventExtensionsTests
{
    private static Axes FourAxes() => Plt.Create()
        .AddSubPlot(1, 1, 1, ax => ax
            .Plot([1.0, 2], [3.0, 4])
            .WithAddRightYAxis(y => y.Plot([1.0, 2], [50.0, 60]))
            .WithAddRightYAxis(y => y.Plot([1.0, 2], [500.0, 600]))
            .WithAddLeftYAxis(y => y.Plot([1.0, 2], [5.0, 6]))
            .WithAddLeftYAxis(y => y.Plot([1.0, 2], [7.0, 8])))
        .Build().SubPlots[0];

    /// <summary>Index <c>i</c> on a side is stack index <c>i</c> on the right and <c>i + 1</c> on the left, where slot 0 is the
    /// primary; each range lands on its own slot and on no other, the primary Y axis included.</summary>
    [Fact]
    public void ApplySecondaryRanges_NumbersTheSlotsPastThePrimary_PerSide()
    {
        var axes = FourAxes();

        axes.ApplySecondaryRanges(
        [
            new(AxisSide.Right, 0, new DataRange(0, 1, 10, 11)),
            new(AxisSide.Right, 1, new DataRange(0, 1, 20, 21)),
            new(AxisSide.Left, 0, new DataRange(0, 1, 30, 31)),
            new(AxisSide.Left, 1, new DataRange(0, 1, 40, 41)),
        ]);

        AssertRange(axes.YAxes.Right[0], 10, 11);
        AssertRange(axes.YAxes.Right[1], 20, 21);
        AssertRange(axes.YAxes.Left[1], 30, 31);
        AssertRange(axes.YAxes.Left[2], 40, 41);
        Assert.Null(axes.YAxis.Min);
        Assert.Null(axes.YAxis.Max);
    }

    private static void AssertRange(YAxisSlot slot, double min, double max)
    {
        Assert.Equal(min, slot.Axis.Min);
        Assert.Equal(max, slot.Axis.Max);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ApplySecondaryRanges_WithNoRanges_ChangesNothing(bool empty)
    {
        var axes = FourAxes();

        axes.ApplySecondaryRanges(empty ? [] : null);

        Assert.All(axes.YAxes.IdentityOrder, slot =>
        {
            Assert.Null(slot.Axis.Min);
            Assert.Null(slot.Axis.Max);
        });
    }

    /// <summary>A change that names no slot — below 0, one past the last slot of its side, any index on a side with no axis
    /// beyond the primary — writes nowhere, and above all not to the primary Y axis a negative or zero-based mix-up would
    /// reach. The changes around it are still applied.</summary>
    [Theory]
    [InlineData(AxisSide.Left, -1)]
    [InlineData(AxisSide.Left, 2)]
    [InlineData(AxisSide.Right, -1)]
    [InlineData(AxisSide.Right, 2)]
    [InlineData(AxisSide.Left, int.MinValue)]
    [InlineData(AxisSide.Right, int.MaxValue)]
    public void ApplySecondaryRanges_ForAMissingSlot_SkipsItAndAppliesTheRest(AxisSide side, int slotIndex)
    {
        var axes = FourAxes();

        axes.ApplySecondaryRanges(
        [
            new(side, slotIndex, new DataRange(0, 1, 111, 222)),
            new(AxisSide.Right, 0, new DataRange(0, 1, 10, 11)),
        ]);

        Assert.Equal(10.0, axes.YAxes.Right[0].Axis.Min);
        Assert.Equal(11.0, axes.YAxes.Right[0].Axis.Max);
        Assert.All(axes.YAxes.IdentityOrder.Where(slot => slot != axes.YAxes.Right[0]), slot => Assert.Null(slot.Axis.Min));
    }

    [Theory]
    [InlineData(AxisSide.Left)]
    [InlineData(AxisSide.Right)]
    public void ApplySecondaryRanges_OnASideWithNoSlotPastThePrimary_SkipsEveryIndex(AxisSide side)
    {
        var axes = Plt.Create().Plot([1.0, 2], [3.0, 4]).Build().SubPlots[0];

        axes.ApplySecondaryRanges(
        [
            new(side, 0, new DataRange(0, 1, 111, 222)),
            new(side, 1, new DataRange(0, 1, 111, 222)),
        ]);

        Assert.Null(axes.YAxis.Min);
        Assert.Null(axes.YAxis.Max);
        Assert.Empty(axes.YAxes.Right);
    }

    /// <summary>A slot whose own series fix the Y range (a state timeline) keeps it, as the primary does; a slot with a line
    /// beside the timeline, or with no series yet, does not.</summary>
    [Fact]
    public void ApplySecondaryRanges_ForASlotWhoseSeriesFixTheYRange_SkipsIt()
    {
        var axes = new Axes();
        var timeline = axes.YAxes.Right.Add();
        timeline.AddSeries(new StateTimelineSeries([new StateSegment(0, 1, "Up", Colors.Green)]));
        var mixed = axes.YAxes.Right.Add();
        mixed.AddSeries(new StateTimelineSeries([new StateSegment(0, 1, "Up", Colors.Green)]));
        mixed.AddSeries(new LineSeries([0.0, 1], [0.0, 1]));
        var empty = axes.YAxes.Left.Add();

        axes.ApplySecondaryRanges(
        [
            new(AxisSide.Right, 0, new DataRange(0, 1, 10, 11)),
            new(AxisSide.Right, 1, new DataRange(0, 1, 20, 21)),
            new(AxisSide.Left, 0, new DataRange(0, 1, 30, 31)),
        ]);

        Assert.Null(timeline.Axis.Min);
        Assert.Null(timeline.Axis.Max);
        Assert.Equal(20.0, mixed.Axis.Min);
        Assert.Equal(21.0, mixed.Axis.Max);
        Assert.Equal(30.0, empty.Axis.Min);
        Assert.Equal(31.0, empty.Axis.Max);
    }
}
