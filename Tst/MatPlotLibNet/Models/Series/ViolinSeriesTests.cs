// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="ViolinSeries"/> default properties and construction.</summary>
public class ViolinSeriesTests
{
    /// <summary>Verifies that the constructor stores datasets.</summary>
    [Fact]
    public void Constructor_StoresData()
    {
        double[][] datasets = [[1.0, 2.0, 3.0]];
        var series = new ViolinSeries(datasets);
        Assert.Equal(datasets, series.Datasets);
    }

    /// <summary>Verifies that Alpha defaults to 0.3 (matplotlib violinplot default).</summary>
    [Fact]
    public void DefaultAlpha_Is0Point3()
    {
        var series = new ViolinSeries([[1.0]]);
        Assert.Equal(0.3, series.Alpha);
    }

    /// <summary>Verifies that ShowMeans defaults to false.</summary>
    [Fact]
    public void DefaultShowMeans_IsFalse()
    {
        var series = new ViolinSeries([[1.0]]);
        Assert.False(series.ShowMeans);
    }

    /// <summary>Verifies that ShowMedians defaults to false.</summary>
    [Fact]
    public void DefaultShowMedians_IsFalse()
    {
        var series = new ViolinSeries([[1.0]]);
        Assert.False(series.ShowMedians);
    }

    /// <summary>Verifies that ShowExtrema defaults to true.</summary>
    [Fact]
    public void DefaultShowExtrema_IsTrue()
    {
        var series = new ViolinSeries([[1.0]]);
        Assert.True(series.ShowExtrema);
    }

    /// <summary>Verifies that Widths defaults to 0.5.</summary>
    [Fact]
    public void DefaultWidths_Is0Point5()
    {
        var series = new ViolinSeries([[1.0]]);
        Assert.Equal(0.5, series.Widths);
    }

    /// <summary>Verifies that Side defaults to ViolinSide.Both.</summary>
    [Fact]
    public void DefaultSide_IsBoth()
    {
        var series = new ViolinSeries([[1.0]]);
        Assert.Equal(ViolinSide.Both, series.Side);
    }

    /// <summary>Verifies that Positions defaults to null.</summary>
    [Fact]
    public void DefaultPositions_IsNull()
    {
        var series = new ViolinSeries([[1.0]]);
        Assert.Null(series.Positions);
    }

    /// <summary>Verifies that Side can be set to Low.</summary>
    [Fact]
    public void Side_CanBeSet()
    {
        var series = new ViolinSeries([[1.0]]) { Side = ViolinSide.Low };
        Assert.Equal(ViolinSide.Low, series.Side);
    }

    /// <summary>Covers the explicit XAxisMin/XAxisMax branch arms in ComputeDataRange.</summary>
    [Fact]
    public void ComputeDataRange_HonoursContextXAxisLimits()
    {
        var series = new ViolinSeries([[1.0, 2.0, 3.0]]);
        var range = series.ComputeDataRange(new ContextWithXLimits(-2, 9));
        Assert.Equal(-2, range.XMin);
        Assert.Equal(9, range.XMax);
        Assert.Equal(1, range.YMin);
        Assert.Equal(3, range.YMax);
    }

    // ── the range follows where the violins are drawn ──────────────────────────────────────────────────────────────
    // A sample whose values are all equal puts every observation inside the window at the lowest heights, so the body is as
    // wide as it can be: Widths on each side of the position. That makes the extents below exact by hand.

    private static IAxesContext NoLimits => new ContextWithXLimits(null, null);

    private static readonly double[] Spike = [5.0, 5.0, 5.0, 5.0];

    /// <summary>Without Positions the violins keep the room around the default positions 0..n-1 that they always had.</summary>
    [Fact]
    public void ComputeDataRange_WithoutPositions_KeepsTheRoomAroundTheDefaultPositions()
    {
        var range = new ViolinSeries([[1.0, 2, 3], [2.0, 3, 4]]).ComputeDataRange(NoLimits);

        Assert.Equal(-1, range.XMin);
        Assert.Equal(2, range.XMax);
    }

    /// <summary>A violin placed by Positions ranges over where it is drawn, not over 0..n-1: measured before the fix, a single
    /// violin at position 3 still reported -1 to 1, so it was drawn off the plot.</summary>
    [Fact]
    public void ComputeDataRange_WithPositions_RangesOverTheViolinAtItsPosition()
    {
        var range = new ViolinSeries([Spike]) { Positions = [3], ShowExtrema = false }.ComputeDataRange(NoLimits);

        Assert.Equal(2.5, range.XMin!.Value, 12);
        Assert.Equal(3.5, range.XMax!.Value, 12);
    }

    /// <summary>The body of a half violin reaches one side of its position only (High: right, Low: left), so the range
    /// does not make room on the side that is not drawn.</summary>
    [Theory]
    [InlineData(ViolinSide.Both, 2.5, 3.5)]
    [InlineData(ViolinSide.High, 3.0, 3.5)]
    [InlineData(ViolinSide.Low, 2.5, 3.0)]
    public void ComputeDataRange_WithPositions_IsSideAware(ViolinSide side, double xMin, double xMax)
    {
        var range = new ViolinSeries([Spike]) { Positions = [3], ShowExtrema = false, Side = side }.ComputeDataRange(NoLimits);

        Assert.Equal(xMin, range.XMin!.Value, 12);
        Assert.Equal(xMax, range.XMax!.Value, 12);
    }

    /// <summary>The ticks at the extrema stand 0.3 widths to either side of the position whichever side the body is drawn
    /// on, so a half violin that shows them reaches that far on the side it does not draw.</summary>
    [Fact]
    public void ComputeDataRange_WithPositions_ReachesTheExtremaTicksOnBothSides()
    {
        var range = new ViolinSeries([Spike]) { Positions = [3], Side = ViolinSide.High }.ComputeDataRange(NoLimits);

        Assert.Equal(3 - 0.3 * 0.5, range.XMin!.Value, 12);
        Assert.Equal(3.5, range.XMax!.Value, 12);
    }

    /// <summary>The body reaches a whole width at its widest (the density is scaled by its peak), and the median and mean lines
    /// are half a width across: they lie inside the body, so they never widen the room a violin is drawn in. Before the body was a
    /// density, a sample with nothing near any one height had a body of a tenth of the width and the lines (a quarter) were wider.</summary>
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void ComputeDataRange_WithPositions_ReachesTheBodyAtItsWidest_WhichTheMedianAndMeanLinesLieInside(bool medians, bool means)
    {
        var series = new ViolinSeries([[1.0, 2, 3, 4, 5]])
            { Positions = [3], ShowExtrema = false, ShowMedians = medians, ShowMeans = means };

        var range = series.ComputeDataRange(NoLimits);

        Assert.Equal(3 - 0.5, range.XMin!.Value, 12);
        Assert.Equal(3 + 0.5, range.XMax!.Value, 12);
    }

    /// <summary>A violin the position array does not reach stands at its index, as it is drawn.</summary>
    [Fact]
    public void ComputeDataRange_WithAShortPositionsArray_PlacesTheRestAtTheirIndex()
    {
        var range = new ViolinSeries([Spike, Spike]) { Positions = [10], ShowExtrema = false }.ComputeDataRange(NoLimits);

        Assert.Equal(0.5, range.XMin!.Value, 12);
        Assert.Equal(10.5, range.XMax!.Value, 12);
    }

    /// <summary>A dataset with nothing to draw is not drawn, so it makes no room, wherever it is placed.</summary>
    [Fact]
    public void ComputeDataRange_WithPositions_MakesNoRoomForADatasetThatDrawsNothing()
    {
        var range = new ViolinSeries([Spike, [double.NaN]]) { Positions = [3, 90], ShowExtrema = false }.ComputeDataRange(NoLimits);

        Assert.Equal(2.5, range.XMin!.Value, 12);
        Assert.Equal(3.5, range.XMax!.Value, 12);
    }

    /// <summary>A series that places violins and has none to draw contributes nothing along X.</summary>
    [Fact]
    public void ComputeDataRange_WithPositionsAndNothingToDraw_ContributesNoX()
    {
        var range = new ViolinSeries([[double.NaN]]) { Positions = [3] }.ComputeDataRange(NoLimits);

        Assert.Null(range.XMin);
        Assert.Null(range.XMax);
    }

    /// <summary>An empty Positions array places nothing: the violins stand where they always did.</summary>
    [Fact]
    public void ComputeDataRange_WithEmptyPositions_IsTheDefaultRange()
    {
        var range = new ViolinSeries([[1.0, 2, 3], [2.0, 3, 4]]) { Positions = [] }.ComputeDataRange(NoLimits);

        Assert.Equal(-1, range.XMin);
        Assert.Equal(2, range.XMax);
    }

    /// <summary>A violin wider than the default room is not cut by it: at a width of 3 the body reaches 3 either side.</summary>
    [Fact]
    public void ComputeDataRange_WithoutPositions_WidensForAViolinWiderThanTheDefaultRoom()
    {
        var range = new ViolinSeries([Spike]) { Widths = 3, ShowExtrema = false }.ComputeDataRange(NoLimits);

        Assert.Equal(-3, range.XMin!.Value, 12);
        Assert.Equal(3, range.XMax!.Value, 12);
    }

    /// <summary>A limit set on the X axis stays the range, whatever the positions.</summary>
    [Fact]
    public void ComputeDataRange_WithPositions_HonoursContextXAxisLimits()
    {
        var range = new ViolinSeries([Spike]) { Positions = [3] }.ComputeDataRange(new ContextWithXLimits(-2, 9));

        Assert.Equal(-2, range.XMin);
        Assert.Equal(9, range.XMax);
    }

    /// <summary>A violin plot read back from JSON that carries no datasets is an empty violin plot, not an error.</summary>
    [Fact]
    public void ReadBackWithoutDatasets_IsAnEmptyViolinPlot() =>
        Assert.Empty(ViolinSeries.FromSeriesDto(new Axes(), new global::MatPlotLibNet.Serialization.SeriesDto { Type = "violin" }).Datasets);

    private sealed record ContextWithXLimits(double? XAxisMin, double? XAxisMax) : IAxesContext
    {
        public double? YAxisMin => null;
        public double? YAxisMax => null;
        public BarMode BarMode => BarMode.Grouped;
        public IReadOnlyList<ISeries> AllSeries => [];
    }
}
