// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.SeriesRenderers;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A box that stands on a foot is turned into pixels by one rule, whichever renderer draws it. These are the
/// numbers of that rule on a linear, a log and a backwards axis, for a box that rises and one that falls, upright and lying
/// down.</summary>
public class PixelBoxTests
{
    private static readonly Rect Plot = new(100, 50, 400, 300);

    private static readonly CategorySlot Slot = CategorySlot.Of(1);

    /// <summary>X 0..4 over 100..500 px, Y 0..10 over 350..50 px: one X unit is 100 px, one Y unit is 30 px.</summary>
    private static DataTransform Linear() => new(0, 4, 0, 10, Plot);

    /// <summary>The same plot with a log Y axis over 1..100 (two decades across 300 px).</summary>
    private static DataTransform LogY() => new(0, 4, 0, 2, Plot, null, null, 0, 4, 1, 100, AxisScale.Linear, AxisScale.Log, 1, 1);

    /// <summary>The same plot with Y running backwards, 10 at the bottom and 0 at the top.</summary>
    private static DataTransform BackwardsY() => new(0, 4, 10, 0, Plot, null, null, 0, 4, 10, 0);

    [Fact]
    public void ABoxThatRises_StandsOnItsFootAndReachesItsEnd()
    {
        var box = Linear().BoxOnFoot(Slot, foot: 2, end: 8, rising: true, BarOrientation.Vertical);

        Assert.Equal(new Point(200, 110), box.From);
        Assert.Equal(new Point(300, 290), box.To);
        Assert.Equal(new Rect(200, 110, 100, 180), box.Rect);
    }

    [Fact]
    public void ABoxThatFalls_KeepsItsFootAtTheLowerCornerOfItsRectangle()
    {
        // Foot 8, end 2: the box hangs from 8 down to 2; the rectangle is the same one, its clipped corner the foot's.
        var box = Linear().BoxOnFoot(Slot, foot: 8, end: 2, rising: false, BarOrientation.Vertical);

        Assert.Equal(new Point(200, 110), box.From);
        Assert.Equal(new Point(300, 290), box.To);
    }

    [Fact]
    public void ABoxLyingDown_RunsItsValueAlongX()
    {
        // The slot 1..2 is a stretch of the Y axis (30 px a unit, 320..290 px), the value 1..3 a stretch of X (100 px a unit).
        var box = Linear().BoxOnFoot(Slot, foot: 1, end: 3, rising: true, BarOrientation.Horizontal);

        Assert.Equal(new Point(200, 290), box.From);
        Assert.Equal(new Point(400, 320), box.To);
    }

    [Fact]
    public void AFootTheLogAxisCannotPlace_IsClippedToTheEdgeWhereTheAxisBegins()
    {
        var box = LogY().BoxOnFoot(Slot, foot: 0, end: 10, rising: true, BarOrientation.Vertical);

        Assert.Equal(350, box.To.Y);
        Assert.Equal(200, box.From.Y, 6);
        Assert.True(box.Rect.IsDrawable());
    }

    [Fact]
    public void AnEndTheLogAxisCannotPlace_LeavesABoxThatIsNotDrawn()
    {
        var box = LogY().BoxOnFoot(Slot, foot: 1, end: -5, rising: true, BarOrientation.Vertical);

        Assert.False(box.Rect.IsDrawable());
    }

    [Fact]
    public void OnAnAxisThatRunsBackwards_TheRectangleStaysOfPositiveSize()
    {
        var box = BackwardsY().BoxOnFoot(Slot, foot: 2, end: 8, rising: true, BarOrientation.Vertical);

        Assert.True(box.Rect.Width > 0 && box.Rect.Height > 0);
        Assert.Equal(box.Rect.Y, Math.Min(box.From.Y, box.To.Y));
    }

    [Fact]
    public void AFootTheBackwardsLogAxisCannotPlace_IsClippedToTheUpperEdge()
    {
        var reversedLog = new DataTransform(0, 4, 2, 0, Plot, null, null, 0, 4, 100, 1, AxisScale.Linear, AxisScale.Log, 1, 1);

        var box = reversedLog.BoxOnFoot(Slot, foot: 0, end: 10, rising: true, BarOrientation.Vertical);

        Assert.Equal(50, box.To.Y);
    }
}
