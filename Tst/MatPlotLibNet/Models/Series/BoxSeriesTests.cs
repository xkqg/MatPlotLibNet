// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="BoxSeries"/> behaviour; what it starts from and the settings it stores are held by <see cref="Serialization.RestingValueCorpusTests"/> and the drawn-property round trips.</summary>
public class BoxSeriesTests
{
    // ── the range follows where the boxes are drawn ────────────────────────────────────────────────────────────────

    private static IAxesContext NoLimits => new TestAxesContext();

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
        var range = new BoxSeries([[1.0, 2, 3]]) { Positions = [3] }.ComputeDataRange(new TestAxesContext(-3, 7));

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
        var range = new BoxSeries([[1.0, 2, 3]]) { Vert = false, Positions = [3] }.ComputeDataRange(new TestAxesContext(YAxisMin: -2, YAxisMax: 9));

        Assert.Equal(-2, range.YMin);
        Assert.Equal(9, range.YMax);
        Assert.Equal(1, range.XMin);
        Assert.Equal(3, range.XMax);
    }

}
