// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Tests.TestFixtures;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>The data range of each subplot for one draw: worked out the first time the draw asks, and the same value for every
/// later ask in that draw, so the renderers that measure a subplot and the one that draws it read one range. An axes that is not
/// a subplot of the figure as the draw began (an inset, or one added since) is kept from its first ask until another such axes is
/// asked for: the parent asks for an inset's range when it draws the inset's zoom indicator, then the inset's render asks, and its
/// own insets are asked for after it, so one kept axes ranges each inset once a draw.</summary>
public class FigureRangesTests
{
    private static readonly double[] X = [0, 1, 2, 3];

    private static (Figure Figure, CountingRangeSeries First, CountingRangeSeries Second) TwoSubplots()
    {
        var figure = new Figure();
        var first = figure.AddSubPlot().AddSeries(new CountingRangeSeries(X, [1.0, 4, 9, 16]));
        var second = figure.AddSubPlot().AddSeries(new CountingRangeSeries(X, [-5.0, 0, 5, 10]));
        return (figure, first, second);
    }

    [Fact]
    public void TheRangeOfASubplot_IsTheRangeItsAxesWorksOut()
    {
        var (figure, _, _) = TwoSubplots();
        var expected = figure.SubPlots[1].ComputeDataRanges(figure.Theme);

        Assert.Equal(expected, new FigureRanges(figure).RangeOf(figure.SubPlots[1]).Range);
    }

    [Fact]
    public void TheRangeOfASubplot_IsWorkedOutOnceHoweverOftenItIsAsked()
    {
        var (figure, first, _) = TwoSubplots();
        var ranges = new FigureRanges(figure);

        var once = ranges.RangeOf(figure.SubPlots[0]).Range;
        var again = ranges.RangeOf(figure.SubPlots[0]).Range;
        _ = ranges.RangeOf(figure.SubPlots[0]).Range;

        Assert.Equal(once, again);
        Assert.Equal(1, first.RangeCalls);
    }

    [Fact]
    public void EachSubplot_HasItsOwnRange_AndIsWorkedOutOnlyWhenAskedFor()
    {
        var (figure, first, second) = TwoSubplots();
        var ranges = new FigureRanges(figure);

        var lower = ranges.RangeOf(figure.SubPlots[1]).Range;

        Assert.Equal(0, first.RangeCalls);
        Assert.Equal(1, second.RangeCalls);
        Assert.NotEqual(lower, ranges.RangeOf(figure.SubPlots[0]).Range);
    }

    [Fact]
    public void AnAxesThatIsNoSubplotOfTheFigure_IsRangedOnceWhileItIsTheOneKept()
    {
        var (figure, _, _) = TwoSubplots();
        var inset = new Axes();
        var counter = inset.AddSeries(new CountingRangeSeries(X, [2.0, 3, 4, 5]));
        var ranges = new FigureRanges(figure);

        var once = ranges.RangeOf(inset).Range;
        var again = ranges.RangeOf(inset).Range;

        Assert.Equal(once, again);
        Assert.Equal(1, counter.RangeCalls);
    }

    [Fact]
    public void ASubplotAddedAfterTheDrawBegan_IsKeptLikeAnInset()
    {
        var (figure, _, _) = TwoSubplots();
        var ranges = new FigureRanges(figure);
        var late = figure.AddSubPlot();
        var counter = late.AddSeries(new CountingRangeSeries(X, [2.0, 3, 4, 5]));

        var once = ranges.RangeOf(late).Range;
        var again = ranges.RangeOf(late).Range;

        Assert.Equal(once, again);
        Assert.Equal(1, counter.RangeCalls);
    }

    /// <summary>One axes outside the subplots is kept at a time: asking for a second one replaces the first, so the first is
    /// ranged again when it is asked for after it, and each answer is still that axes' own range.</summary>
    [Fact]
    public void AnotherInsetAskedFor_ReplacesTheKeptOne()
    {
        var (figure, _, _) = TwoSubplots();
        var first = new Axes();
        var firstCounter = first.AddSeries(new CountingRangeSeries(X, [2.0, 3, 4, 5]));
        var second = new Axes();
        var secondCounter = second.AddSeries(new CountingRangeSeries(X, [-20.0, -10, 0, 10]));
        var ranges = new FigureRanges(figure);

        var firstRange = ranges.RangeOf(first).Range;
        var secondRange = ranges.RangeOf(second).Range;
        _ = ranges.RangeOf(second).Range;
        var firstAgain = ranges.RangeOf(first).Range;

        Assert.NotEqual(firstRange, secondRange);
        Assert.Equal(firstRange, firstAgain);
        Assert.Equal(2, firstCounter.RangeCalls);
        Assert.Equal(1, secondCounter.RangeCalls);
    }

    /// <summary>The cell a colour bar stands in: a subplot's cell when the draw kept the cells, the rectangle the parent gave an
    /// inset when it held it, nothing for any other axes.</summary>
    [Fact]
    public void TheCellOfAnAxes_IsKeptForASubplotAndHeldForAnInset_AndOtherwiseUnknown()
    {
        var (figure, _, _) = TwoSubplots();
        var inset = new Axes();
        var ranges = new FigureRanges(figure);

        Assert.Null(ranges.CellOf(figure.SubPlots[0]));
        Assert.Null(ranges.CellOf(inset));

        ranges.Cells = [new Rect(1, 2, 3, 4), new Rect(5, 6, 7, 8)];
        Assert.Equal(new Rect(5, 6, 7, 8), ranges.CellOf(figure.SubPlots[1]));
        Assert.Null(ranges.CellOf(inset));

        ranges.HoldCellOf(inset, new Rect(9, 10, 11, 12));
        Assert.Equal(new Rect(9, 10, 11, 12), ranges.CellOf(inset));
        Assert.Equal(new Rect(1, 2, 3, 4), ranges.CellOf(figure.SubPlots[0]));
        Assert.Null(ranges.CellOf(new Axes()));

        ranges.HoldCellOf(inset, new Rect(13, 14, 15, 16));
        Assert.Equal(new Rect(13, 14, 15, 16), ranges.CellOf(inset));
    }

    /// <summary>An inset whose cell was held and a draw that kept no subplot cells: the inset's cell is still known.</summary>
    [Fact]
    public void AnInsetsCell_IsKnownWhenTheDrawKeptNoSubplotCells()
    {
        var (figure, _, _) = TwoSubplots();
        var inset = new Axes();
        var ranges = new FigureRanges(figure);
        ranges.HoldCellOf(inset, new Rect(1, 1, 2, 2));

        Assert.Null(ranges.Cells);
        Assert.Equal(new Rect(1, 1, 2, 2), ranges.CellOf(inset));
        Assert.Null(ranges.CellOf(figure.SubPlots[0]));
    }
}
