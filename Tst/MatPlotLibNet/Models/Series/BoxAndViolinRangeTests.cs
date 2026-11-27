// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>The range a box plot and a violin report along X, which they decide alike: the limits of the axes win, and without them the
/// range is the room around the positions the glyphs stand at. What differs is the room (half a unit for a box, one unit for a violin).</summary>
public class BoxAndViolinRangeTests
{
    private static readonly double[][] TwoSamples = [[1.0, 2, 3], [2.0, 3, 4]];

    private static DataRangeContribution RangeOf(bool box, double[][] samples, IAxesContext context, double[]? positions = null) =>
        box
            ? new BoxSeries(samples) { Positions = positions }.ComputeDataRange(context)
            : new ViolinSeries(samples) { Positions = positions }.ComputeDataRange(context);

    /// <summary>The explicit XAxisMin/XAxisMax branch arms in ComputeDataRange (otherwise only the null-coalescing fallbacks were exercised).</summary>
    [Theory]
    [InlineData(true, -3, 7)]
    [InlineData(false, -2, 9)]
    public void ComputeDataRange_HonoursContextXAxisLimits(bool box, double xMin, double xMax)
    {
        var range = RangeOf(box, [[1.0, 2.0, 3.0]], new TestAxesContext(xMin, xMax));

        Assert.Equal(xMin, range.XMin);
        Assert.Equal(xMax, range.XMax);
        Assert.Equal(1, range.YMin);
        Assert.Equal(3, range.YMax);
    }

    /// <summary>Without Positions the glyphs keep the room around the default positions 0..n-1 that they always had (a box: half a
    /// unit either side, matplotlib's; a violin: a unit). An empty Positions array places nothing: they stand where they always did.</summary>
    [Theory]
    [InlineData(true, false, -0.5, 1.5)]
    [InlineData(true, true, -0.5, 1.5)]
    [InlineData(false, false, -1, 2)]
    [InlineData(false, true, -1, 2)]
    public void ComputeDataRange_WithoutPositions_KeepsTheRoomAroundTheDefaultPositions(bool box, bool emptyPositions, double xMin, double xMax)
    {
        var range = RangeOf(box, TwoSamples, new TestAxesContext(), emptyPositions ? [] : null);

        Assert.Equal(xMin, range.XMin);
        Assert.Equal(xMax, range.XMax);
    }
}
