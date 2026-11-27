// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using Xunit;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>The seats of an axes' Y slots in identity order, which the series passes, the legend and the end labels walk on every draw
/// and the layout on every measure: a list read in place, never copied into a new one.</summary>
public class YSlotSeatsTests
{
    private static Axes Crowded()
    {
        var axes = new Axes();
        axes.Plot([0.0, 1], [0.0, 1]);
        axes.Plot([0.0, 1], [0.0, 1]);
        var right0 = axes.YAxes.Right.Add();
        right0.AddSeries(new LineSeries([0.0, 1], [0.0, 1]));
        right0.AddSeries(new LineSeries([0.0, 1], [0.0, 1]));
        axes.YAxes.Right.Add();
        var left1 = axes.YAxes.Left.Add();
        left1.AddSeries(new LineSeries([0.0, 1], [0.0, 1]));
        axes.YAxes.Left.Add();
        return axes;
    }

    [Fact]
    public void TheSeats_CountEveryPrimarySlotAndEveryOutwardSlotOfBothSides()
    {
        var seats = Crowded().YSlotSeats();

        Assert.Equal(5, seats.Count);
        Assert.Single(new Axes().YSlotSeats());
    }

    [Fact]
    public void ASeatAtAnIndex_IsTheSeatTheWalkMeetsThere()
    {
        var seats = Crowded().YSlotSeats();

        var walked = new List<YSlotSeat>();
        foreach (var seat in seats) walked.Add(seat);

        Assert.Equal(walked, Enumerable.Range(0, seats.Count).Select(i => seats[i]));
        Assert.Equal(walked, seats.AsEnumerable());
    }

    [Fact]
    public void TheSeats_AreWalkedAsAnyListIs_ThroughTheInterfacesAndAfterAReset()
    {
        var seats = Crowded().YSlotSeats();
        var expected = Enumerable.Range(0, seats.Count).Select(i => seats[i]).ToList();

        var untyped = new List<object>();
        foreach (var seat in (System.Collections.IEnumerable)seats) untyped.Add(seat);
        Assert.Equal(expected.Cast<object>(), untyped);

        var walk = seats.GetEnumerator();
        Assert.True(walk.MoveNext());
        Assert.Equal(expected[0], ((System.Collections.IEnumerator)walk).Current);
        walk.Reset();
        Assert.True(walk.MoveNext());
        Assert.Equal(expected[0], walk.Current);
        walk.Dispose();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(5)]
    public void ASeatPastEitherEnd_ThrowsAsAListDoes(int index) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Crowded().YSlotSeats()[index]);

    [Fact]
    public void WalkingTheSeats_AllocatesNothing()
    {
        var axes = Crowded();
        long Walk()
        {
            long sum = 0;
            foreach (var seat in axes.YSlotSeats()) sum += seat.FirstIndexInPair;
            return sum;
        }

        Walk();
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int round = 0; round < 1000; round++) Walk();
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.True(allocated < 1_000, $"1000 walks allocated {allocated} bytes");
    }
}
