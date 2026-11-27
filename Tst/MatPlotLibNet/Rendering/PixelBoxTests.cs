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

    [Fact]
    public void ABoxThatRises_StandsOnItsFootAndReachesItsEnd()
    {
        var box = PixelBox.OnFoot(Plot.Linear(xMax: 4), Slot, foot: 2, end: 8, rising: true, BarOrientation.Vertical);

        Assert.Equal(new Point(200, 110), box.From);
        Assert.Equal(new Point(300, 290), box.To);
        Assert.Equal(new Rect(200, 110, 100, 180), box.Rect);
    }

    [Fact]
    public void ABoxThatFalls_KeepsItsFootAtTheLowerCornerOfItsRectangle()
    {
        // Foot 8, end 2: the box hangs from 8 down to 2; the rectangle is the same one, its clipped corner the foot's.
        var box = PixelBox.OnFoot(Plot.Linear(xMax: 4), Slot, foot: 8, end: 2, rising: false, BarOrientation.Vertical);

        Assert.Equal(new Point(200, 110), box.From);
        Assert.Equal(new Point(300, 290), box.To);
    }

    [Fact]
    public void ABoxLyingDown_RunsItsValueAlongX()
    {
        // The slot 1..2 is a stretch of the Y axis (30 px a unit, 320..290 px), the value 1..3 a stretch of X (100 px a unit).
        var box = PixelBox.OnFoot(Plot.Linear(xMax: 4), Slot, foot: 1, end: 3, rising: true, BarOrientation.Horizontal);

        Assert.Equal(new Point(200, 290), box.From);
        Assert.Equal(new Point(400, 320), box.To);
    }

    [Fact]
    public void AFootTheLogAxisCannotPlace_IsClippedToTheEdgeWhereTheAxisBegins()
    {
        var box = PixelBox.OnFoot(Plot.LogY(xMax: 4), Slot, foot: 0, end: 10, rising: true, BarOrientation.Vertical);

        Assert.Equal(350, box.To.Y);
        Assert.Equal(200, box.From.Y, 6);
        Assert.True(box.Rect.IsDrawable());
    }

    /// <summary>matplotlib 3.11.2's <c>ax.bar([1], [-10], bottom=[5])</c> on a log Y axis is the rectangle from 5 down to
    /// <c>-376917.85</c> (printed), which the plot cuts at its bottom edge: an end the log axis cannot place is cut there, as a
    /// foot is.</summary>
    [Fact]
    public void AnEndTheLogAxisCannotPlace_IsCutAtTheEdgeWhereTheAxisBegins()
    {
        var box = PixelBox.OnFoot(Plot.LogY(xMax: 4), Slot, foot: 10, end: -5, rising: false, BarOrientation.Vertical);

        Assert.Equal(200, box.From.Y, 6);
        Assert.Equal(350, box.To.Y);
        Assert.True(box.Rect.IsDrawable());
    }

    /// <summary>A box whose foot and end both lie beyond the log axis lies wholly below the plot: not drawn.</summary>
    [Fact]
    public void AFootAndAnEndTheLogAxisCannotPlace_LeaveABoxThatIsNotDrawn()
    {
        var box = PixelBox.OnFoot(Plot.LogY(xMax: 4), Slot, foot: 0, end: -5, rising: false, BarOrientation.Vertical);

        Assert.False(box.Rect.IsDrawable());
    }

    /// <summary>A slot reaching 0 or below on a log X axis is cut at the left edge, as matplotlib's <c>ax.bar([0.5, 2], [3, 4],
    /// width=1.2)</c> on a log X axis is (its first rectangle from x <c>-1131506.25</c>, printed); a slot wholly there is not
    /// drawn.</summary>
    [Fact]
    public void ASlotTheLogXAxisCannotPlace_IsCutAtTheLeftEdge_OrNotDrawnWhenWhollyThere()
    {
        var logX = Plot.LogX();

        var cut = PixelBox.OnFoot(logX, new CategorySlot(-0.1, 0.5, 1.1), foot: 0, end: 5, rising: true, BarOrientation.Vertical);
        var gone = PixelBox.OnFoot(logX, new CategorySlot(-2, -1.5, -1), foot: 0, end: 5, rising: true, BarOrientation.Vertical);

        Assert.Equal(100, cut.Rect.X);
        Assert.True(cut.Rect.Width > 0);
        Assert.False(gone.Rect.IsDrawable());
    }

    [Fact]
    public void OnAnAxisThatRunsBackwards_TheRectangleStaysOfPositiveSize()
    {
        var box = PixelBox.OnFoot(Plot.Linear(xMax: 4, yMin: 10, yMax: 0), Slot, foot: 2, end: 8, rising: true, BarOrientation.Vertical);

        Assert.True(box.Rect.Width > 0 && box.Rect.Height > 0);
        Assert.Equal(box.Rect.Y, Math.Min(box.From.Y, box.To.Y));
    }

    [Fact]
    public void AFootTheBackwardsLogAxisCannotPlace_IsClippedToTheUpperEdge()
    {
        var reversedLog = Plot.LogYReversed(xMax: 4);

        var box = PixelBox.OnFoot(reversedLog, Slot, foot: 0, end: 10, rising: true, BarOrientation.Vertical);

        Assert.Equal(50, box.To.Y);
    }
}
