// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="BoxSeries"/> default properties and construction.</summary>
public class BoxSeriesTests
{
    /// <summary>Verifies that the constructor stores datasets.</summary>
    [Fact]
    public void Constructor_StoresData()
    {
        double[][] datasets = [[1.0, 2.0, 3.0]];
        var series = new BoxSeries(datasets);
        Assert.Equal(datasets, series.Datasets);
    }

    /// <summary>Verifies that ShowOutliers defaults to true.</summary>
    [Fact]
    public void DefaultShowOutliers_IsTrue()
    {
        var series = new BoxSeries([[1.0]]);
        Assert.True(series.ShowOutliers);
    }

    /// <summary>Verifies that Widths defaults to 0.5.</summary>
    [Fact]
    public void DefaultWidths_Is0Point5()
    {
        var series = new BoxSeries([[1.0]]);
        Assert.Equal(0.5, series.Widths);
    }

    /// <summary>Verifies that Vert defaults to true.</summary>
    [Fact]
    public void DefaultVert_IsTrue()
    {
        var series = new BoxSeries([[1.0]]);
        Assert.True(series.Vert);
    }

    /// <summary>Verifies that Whis defaults to 1.5.</summary>
    [Fact]
    public void DefaultWhis_Is1Point5()
    {
        var series = new BoxSeries([[1.0]]);
        Assert.Equal(1.5, series.Whis);
    }

    /// <summary>Verifies that ShowMeans defaults to false.</summary>
    [Fact]
    public void DefaultShowMeans_IsFalse()
    {
        var series = new BoxSeries([[1.0]]);
        Assert.False(series.ShowMeans);
    }

    /// <summary>Verifies that Positions defaults to null.</summary>
    [Fact]
    public void DefaultPositions_IsNull()
    {
        var series = new BoxSeries([[1.0]]);
        Assert.Null(series.Positions);
    }

    /// <summary>Covers the explicit XAxisMin/XAxisMax branch arms in ComputeDataRange
    /// (otherwise only the null-coalescing fallbacks were exercised).</summary>
    [Fact]
    public void ComputeDataRange_HonoursContextXAxisLimits()
    {
        var series = new BoxSeries([[1.0, 2.0, 3.0]]);
        var range = series.ComputeDataRange(new ContextWithXLimits(-3, 7));
        Assert.Equal(-3, range.XMin);
        Assert.Equal(7, range.XMax);
        Assert.Equal(1, range.YMin);
        Assert.Equal(3, range.YMax);
    }

    // ── the range follows where the boxes are drawn ────────────────────────────────────────────────────────────────

    private static IAxesContext NoLimits => new ContextWithXLimits(null, null);

    /// <summary>Without Positions the boxes keep the room around the default positions 0..n-1 that they always had
    /// (matplotlib's: half a unit either side).</summary>
    [Fact]
    public void ComputeDataRange_WithoutPositions_KeepsTheRoomAroundTheDefaultPositions()
    {
        var range = new BoxSeries([[1.0, 2, 3], [2.0, 3, 4]]).ComputeDataRange(NoLimits);

        Assert.Equal(-0.5, range.XMin);
        Assert.Equal(1.5, range.XMax);
    }

    /// <summary>A box placed by Positions ranges over where it is drawn, half its width either side of its position:
    /// measured before the fix, a single box at position 3 still reported -0.5 to 0.5, so it was drawn off the plot.</summary>
    [Fact]
    public void ComputeDataRange_WithPositions_RangesOverTheBoxAtItsPosition()
    {
        var range = new BoxSeries([[1.0, 2, 3]]) { Positions = [3] }.ComputeDataRange(NoLimits);

        Assert.Equal(2.75, range.XMin!.Value, 12);
        Assert.Equal(3.25, range.XMax!.Value, 12);
        Assert.Equal(1, range.YMin);
        Assert.Equal(3, range.YMax);
    }

    /// <summary>The width is the box's own: the range reaches half of it.</summary>
    [Fact]
    public void ComputeDataRange_WithPositions_ReachesHalfTheWidthEitherSide()
    {
        var range = new BoxSeries([[1.0, 2, 3], [2.0, 3, 4]]) { Positions = [10, 20], Widths = 4 }.ComputeDataRange(NoLimits);

        Assert.Equal(8, range.XMin!.Value, 12);
        Assert.Equal(22, range.XMax!.Value, 12);
    }

    /// <summary>A box the position array does not reach stands at its index, as it is drawn.</summary>
    [Fact]
    public void ComputeDataRange_WithAShortPositionsArray_PlacesTheRestAtTheirIndex()
    {
        var range = new BoxSeries([[1.0, 2, 3], [2.0, 3, 4]]) { Positions = [10] }.ComputeDataRange(NoLimits);

        Assert.Equal(0.75, range.XMin!.Value, 12);
        Assert.Equal(10.25, range.XMax!.Value, 12);
    }

    /// <summary>A dataset with nothing to draw is not drawn, so it makes no room, wherever it is placed.</summary>
    [Fact]
    public void ComputeDataRange_WithPositions_MakesNoRoomForADatasetThatDrawsNothing()
    {
        var range = new BoxSeries([[1.0, 2, 3], [double.NaN]]) { Positions = [3, 90] }.ComputeDataRange(NoLimits);

        Assert.Equal(2.75, range.XMin!.Value, 12);
        Assert.Equal(3.25, range.XMax!.Value, 12);
    }

    /// <summary>A series that places boxes and has none to draw contributes nothing along X.</summary>
    [Fact]
    public void ComputeDataRange_WithPositionsAndNothingToDraw_ContributesNoX()
    {
        var range = new BoxSeries([[double.NaN]]) { Positions = [3] }.ComputeDataRange(NoLimits);

        Assert.Null(range.XMin);
        Assert.Null(range.XMax);
    }

    /// <summary>An empty Positions array places nothing: the boxes stand where they always did.</summary>
    [Fact]
    public void ComputeDataRange_WithEmptyPositions_IsTheDefaultRange()
    {
        var range = new BoxSeries([[1.0, 2, 3], [2.0, 3, 4]]) { Positions = [] }.ComputeDataRange(NoLimits);

        Assert.Equal(-0.5, range.XMin);
        Assert.Equal(1.5, range.XMax);
    }

    /// <summary>A box wider than the default room is not cut by it: at a width of 3 it reaches 1.5 either side of 0.</summary>
    [Fact]
    public void ComputeDataRange_WithoutPositions_WidensForABoxWiderThanTheDefaultRoom()
    {
        var range = new BoxSeries([[1.0, 2, 3]]) { Widths = 3 }.ComputeDataRange(NoLimits);

        Assert.Equal(-1.5, range.XMin!.Value, 12);
        Assert.Equal(1.5, range.XMax!.Value, 12);
    }

    /// <summary>A limit set on the X axis stays the range, whatever the positions.</summary>
    [Fact]
    public void ComputeDataRange_WithPositions_HonoursContextXAxisLimits()
    {
        var range = new BoxSeries([[1.0, 2, 3]]) { Positions = [3] }.ComputeDataRange(new ContextWithXLimits(-3, 7));

        Assert.Equal(-3, range.XMin);
        Assert.Equal(7, range.XMax);
    }

    /// <summary>Boxes that lie down (Vert = false) put the positions on the Y axis and the values on X, as they are
    /// drawn: measured before the fix, the range was the upright one whatever the orientation, so a horizontal box was
    /// drawn 700 px past the plot.</summary>
    [Fact]
    public void ComputeDataRange_ForBoxesThatLieDown_PutsThePositionsOnYAndTheValuesOnX()
    {
        var range = new BoxSeries([[1.0, 2, 3]]) { Vert = false, Positions = [3] }.ComputeDataRange(NoLimits);

        Assert.Equal(2.75, range.YMin!.Value, 12);
        Assert.Equal(3.25, range.YMax!.Value, 12);
        Assert.Equal(1, range.XMin);
        Assert.Equal(3, range.XMax);
    }

    [Fact]
    public void ComputeDataRange_ForBoxesThatLieDown_KeepsTheDefaultRoomAlongY()
    {
        var range = new BoxSeries([[1.0, 2, 3], [2.0, 3, 4]]) { Vert = false }.ComputeDataRange(NoLimits);

        Assert.Equal(-0.5, range.YMin);
        Assert.Equal(1.5, range.YMax);
        Assert.Equal(1, range.XMin);
        Assert.Equal(4, range.XMax);
    }

    /// <summary>A limit set on the axis that carries the positions bounds them: the Y axis for boxes that lie down.</summary>
    [Fact]
    public void ComputeDataRange_ForBoxesThatLieDown_HonoursTheYAxisLimits()
    {
        var range = new BoxSeries([[1.0, 2, 3]]) { Vert = false, Positions = [3] }.ComputeDataRange(new ContextWithYLimits(-2, 9));

        Assert.Equal(-2, range.YMin);
        Assert.Equal(9, range.YMax);
        Assert.Equal(1, range.XMin);
        Assert.Equal(3, range.XMax);
    }

    /// <summary>A box plot read back from JSON that carries no datasets is an empty box plot, not an error.</summary>
    [Fact]
    public void ReadBackWithoutDatasets_IsAnEmptyBoxPlot() =>
        Assert.Empty(BoxSeries.FromSeriesDto(new Axes(), new global::MatPlotLibNet.Serialization.SeriesDto { Type = "box" }).Datasets);

    private sealed record ContextWithXLimits(double? XAxisMin, double? XAxisMax) : IAxesContext
    {
        public double? YAxisMin => null;
        public double? YAxisMax => null;
        public BarMode BarMode => BarMode.Grouped;
        public IReadOnlyList<ISeries> AllSeries => [];
    }

    private sealed record ContextWithYLimits(double? YAxisMin, double? YAxisMax) : IAxesContext
    {
        public double? XAxisMin => null;
        public double? XAxisMax => null;
        public BarMode BarMode => BarMode.Grouped;
        public IReadOnlyList<ISeries> AllSeries => [];
    }
}
