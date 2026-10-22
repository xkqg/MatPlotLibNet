// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series.XY;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Extensions;

/// <summary>The one owner of the missing value: NaN is missing, and only a finite value can be drawn, so a range is
/// taken over the finite values alone (matplotlib leaves NaN and infinity out of its data limits).</summary>
public class MissingValueExtensionsTests
{
    [Theory]
    [InlineData(double.NaN, true)]
    [InlineData(0.0, false)]
    [InlineData(double.PositiveInfinity, false)]
    [InlineData(double.NegativeInfinity, false)]
    public void OnlyNaN_IsMissing(double value, bool missing) => Assert.Equal(missing, value.IsMissing());

    [Theory]
    [InlineData(-3.5, true)]
    [InlineData(double.NaN, false)]
    [InlineData(double.PositiveInfinity, false)]
    [InlineData(double.NegativeInfinity, false)]
    public void OnlyAFiniteValue_IsDrawable(double value, bool drawable) => Assert.Equal(drawable, value.IsDrawable());

    [Fact]
    public void TheRangeOfAnArray_LeavesMissingAndInfiniteValuesOut()
    {
        double[] values = [3, double.NaN, -2, double.PositiveInfinity, 7, double.NegativeInfinity];

        Assert.Equal(new Range1D(-2, 7), values.DrawableRange());
    }

    [Fact]
    public void TheRangeOfAnArrayWithNoDrawableValue_IsNone()
    {
        Assert.Null(new[] { double.NaN, double.PositiveInfinity }.DrawableRange());
        Assert.Null(Array.Empty<double>().DrawableRange());
    }

    /// <summary>A log axis cannot place 0 or a negative value either, so on a log axis those are left out too.</summary>
    [Fact]
    public void TheRangeOnALogAxis_LeavesNonPositiveValuesOut()
    {
        double[] values = [0, 2, -5, double.NaN, 20, double.PositiveInfinity];

        Assert.Equal(new Range1D(2, 20), values.DrawableRange(AxisScale.Log));
        Assert.Equal(new Range1D(-5, 20), values.DrawableRange(AxisScale.Linear));
        Assert.Null(new double[] { 0, -1 }.DrawableRange(AxisScale.Log));
    }

    [Fact]
    public void TheRangeOfSeveralArrays_SpansTheirDrawableValues()
    {
        double[][] datasets = [[3, double.NaN], [], [-1, double.PositiveInfinity, 8]];

        Assert.Equal(new Range1D(-1, 8), datasets.DrawableRange());
        Assert.Null(new[] { new[] { double.NaN }, Array.Empty<double>() }.DrawableRange());
    }

    [Fact]
    public void TheRangeOfAGrid_LeavesMissingAndInfiniteValuesOut()
    {
        double[,] cells = { { double.NaN, 4 }, { -1, double.NegativeInfinity } };

        Assert.Equal(new Range1D(-1, 4), cells.DrawableRange());
    }

    [Fact]
    public void TheRangeOfAGridWithNoDrawableValue_IsNone() =>
        Assert.Null(new[,] { { double.NaN } }.DrawableRange());

    // ---- where a line breaks ----------------------------------------------------------------------------------------

    [Fact]
    public void APoint_IsDrawable_OnlyWhenBothItsCoordinatesAre()
    {
        Assert.True(new Point(1, 2).IsDrawable());
        Assert.False(new Point(double.NaN, 2).IsDrawable());
        Assert.False(new Point(1, double.PositiveInfinity).IsDrawable());
    }

    [Fact]
    public void ThePoints_BreakIntoRunsAtEveryPointThatCannotBeDrawn()
    {
        Point[] points = [new(0, 0), new(1, double.NaN), new(2, 2), new(3, 3), new(double.NegativeInfinity, 4), new(5, 5)];

        var runs = points.DrawableRuns().Select(run => run.Select(p => p.X).ToArray()).ToList();

        Assert.Equal(new[] { new[] { 0.0 }, new[] { 2.0, 3.0 }, new[] { 5.0 } }, runs);
    }

    /// <summary>A line without a gap is drawn from the very list it was given: no copy, the same bytes as before.</summary>
    [Fact]
    public void PointsWithoutAGap_AreOneRun_TheVeryListGiven()
    {
        Point[] points = [new(0, 0), new(1, 1)];

        Assert.Same(points, Assert.Single(points.DrawableRuns()));
    }

    [Fact]
    public void PointsThatCannotBeDrawnAtAll_HaveNoRun()
    {
        Assert.Empty(new[] { new Point(double.NaN, 0) }.DrawableRuns());
        Assert.Empty(Array.Empty<Point>().DrawableRuns());
    }

    [Fact]
    public void TheDataRuns_BreakWhereAnyOfTheArraysHasNoDrawableValue()
    {
        double[] x = [0, 1, 2, 3, 4, 5];
        double[] y = [0, double.NaN, 2, 3, 4, 5];
        double[] y2 = [0, 1, 2, 3, double.NaN, 5];

        Assert.Equal(new IndexRange[] { new(0, 1), new(2, 6) }, x.DrawableIndexRuns(y));
        Assert.Equal(new IndexRange[] { new(0, 1), new(2, 4), new(5, 6) }, x.DrawableIndexRuns(y, y2));
    }

    [Fact]
    public void ARunOfAnArray_IsTheVeryArray_WhenItCoversAllOfIt()
    {
        double[] values = [1, 2, 3];

        Assert.Same(values, values.Slice(new(0, 3)));
        Assert.Equal(new[] { 2.0, 3.0 }, values.Slice(new(1, 3)));
    }

    // ---- what a renderer asks before it draws a mark ----------------------------------------------------------------

    [Theory]
    [InlineData(1, 2, 3, 4, true)]
    [InlineData(double.NaN, 2, 3, 4, false)]
    [InlineData(1, double.NaN, 3, 4, false)]
    [InlineData(1, 2, double.PositiveInfinity, 4, false)]
    [InlineData(1, 2, 3, double.NaN, false)]
    public void ARectangle_IsDrawable_OnlyWhenItsCornerAndSizeAre(double x, double y, double width, double height, bool drawable) =>
        Assert.Equal(drawable, new Rect(x, y, width, height).IsDrawable());

    [Theory]
    [InlineData(0, 0, 1, 1, true)]
    [InlineData(double.NaN, 0, 1, 1, false)]
    [InlineData(0, 0, 1, double.NegativeInfinity, false)]
    public void ALine_IsDrawable_OnlyWhenBothItsEndsAre(double x1, double y1, double x2, double y2, bool drawable) =>
        Assert.Equal(drawable, new Point(x1, y1).CanDrawLineTo(new Point(x2, y2)));

    /// <summary>A box or a violin is drawn from the observations that are there: a missing observation and an infinity
    /// are left out of the sample, as they are left out of its range.</summary>
    [Fact]
    public void TheDrawableValuesOfASample_LeaveMissingAndInfiniteObservationsOut() =>
        Assert.Equal(new[] { 3.0, -1, 2 }, new[] { 3, double.NaN, -1, double.PositiveInfinity, 2 }.DrawableValues());

    [Fact]
    public void ASampleWithoutAGap_IsItsOwnDrawableValues_TheVeryArray()
    {
        double[] sample = [1, 2, 3];

        Assert.Same(sample, sample.DrawableValues());
    }

    /// <summary>What a box and a violin are drawn from, and ranged over: the drawable observations, smallest first.</summary>
    [Fact]
    public void TheSortedDrawableValuesOfASample_AreItsDrawableValuesSmallestFirst() =>
        Assert.Equal(new[] { -1.0, 2, 3 }, new[] { 3, double.NaN, -1, double.PositiveInfinity, 2 }.SortedDrawableValues());

    [Fact]
    public void TheSortedDrawableValuesOfASampleWithoutAGap_AreACopy_SoSortingNeverReordersTheSeriesData()
    {
        double[] sample = [3, 1, 2];

        var sorted = sample.SortedDrawableValues();

        Assert.Equal(new[] { 1.0, 2, 3 }, sorted);
        Assert.Equal(new[] { 3.0, 1, 2 }, sample);
    }

    [Fact]
    public void ASampleOfNothingDrawable_HasNoSortedDrawableValues() =>
        Assert.Empty(new[] { double.NaN, double.NegativeInfinity }.SortedDrawableValues());

    /// <summary>The end of a line, for the legend's value and the end-of-line label alike: the last point, in data
    /// order, whose x and y are both drawable and whose x lies in the window.</summary>
    [Fact]
    public void TheLastDrawablePoint_SkipsATrailingGapInEitherCoordinate()
    {
        Assert.Equal(1, new[] { 0.0, 1, 2 }.LastDrawableIndex([1, 2, double.NaN]));
        Assert.Equal(1, new[] { 0.0, 1, double.NaN }.LastDrawableIndex([1, 2, 3]));
        Assert.Equal(2, new[] { 0.0, 1, 2 }.LastDrawableIndex([1, 2, 3]));
    }

    [Fact]
    public void TheLastDrawablePoint_IsTheLastInTheWindow()
    {
        double[] x = [0, 1, 2, 3];

        Assert.Equal(2, x.LastDrawableIndex([1, 1, 1, 1], xMin: 1, xMax: 2));
        Assert.Equal(-1, x.LastDrawableIndex([1, 1, 1, 1], xMin: 5, xMax: 6));
    }

    [Fact]
    public void TheLastDrawablePoint_PairsOnlyTheIndicesBothArraysHave()
    {
        Assert.Equal(1, new[] { 0.0, 1 }.LastDrawableIndex([1, 2, 3]));
        Assert.Equal(-1, new[] { double.NaN }.LastDrawableIndex([1]));
        Assert.Equal(-1, Array.Empty<double>().LastDrawableIndex([]));
    }

    [Fact]
    public void TheDrawablePairs_LeaveOutAPairWithAMissingOrInfiniteValueOnEitherSide()
    {
        var pairs = new[] { 1.0, double.NaN, 3, 4 }.DrawablePairs([10, 20, double.PositiveInfinity, 40]);

        Assert.Equal([1.0, 4], pairs.X);
        Assert.Equal([10.0, 40], pairs.Y);
    }

    [Fact]
    public void PairsWithoutAGap_AreTheVeryArrays()
    {
        double[] x = [1, 2], y = [3, 4];

        var pairs = x.DrawablePairs(y);

        Assert.Same(x, pairs.X);
        Assert.Same(y, pairs.Y);
    }

    /// <summary>Arrays of different lengths are no pairs; they are handed back for the fit to refuse, as before.</summary>
    [Fact]
    public void ArraysOfDifferentLengths_AreHandedBackUnpaired()
    {
        double[] x = [1, double.NaN, 3], y = [3, 4];

        var pairs = x.DrawablePairs(y);

        Assert.Same(x, pairs.X);
        Assert.Same(y, pairs.Y);
    }

    [Fact]
    public void TheExtentOfSpans_LeavesOutASpanWithAnEndThatCannotBeDrawn_AndRunsEitherWay()
    {
        Range1D[] spans = [new(5, 2), new(double.NaN, 100), new(-50, double.PositiveInfinity), new(3, 7)];

        Assert.Equal(new Range1D(2, 7), spans.DrawableRange());
        Assert.Null(new Range1D[] { new(double.NaN, 1) }.DrawableRange());
    }

    /// <summary>A grid cell can be drawn only when its four corner values and its four grid lines can: one missing value
    /// anywhere among the eight masks it.</summary>
    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    public void AGridCell_IsDrawable_OnlyWhenAllItsCornersAndGridLinesAre(int missing)
    {
        double[] x = [0, 1], y = [0, 1];
        double[,] z = { { 1, 2 }, { 3, 4 } };
        switch (missing)
        {
            case 0: x[0] = double.NaN; break;
            case 1: x[1] = double.PositiveInfinity; break;
            case 2: y[0] = double.NaN; break;
            case 3: y[1] = double.NaN; break;
            case 4: z[0, 0] = double.NaN; break;
            case 5: z[0, 1] = double.NaN; break;
            case 6: z[1, 0] = double.NegativeInfinity; break;
            case 7: z[1, 1] = double.NaN; break;
        }

        Assert.Equal(missing < 0, z.IsDrawableCell(x, y, 0, 0));
    }

    [Fact]
    public void ARangeFoldedValueByValue_TakesInOnlyWhatItsAxisCanPlace()
    {
        var range = Range1D.Empty.Including(3).Including(double.NaN).Including(double.NegativeInfinity).Including(-1);
        var onLog = Range1D.Empty.Including(0, AxisScale.Log).Including(5, AxisScale.Log);

        Assert.Equal(new Range1D(-1, 3), range.OrNone());
        Assert.Equal(new Range1D(5, 5), onLog.OrNone());
        Assert.Null(Range1D.Empty.Including(double.NaN).OrNone());
    }
}
