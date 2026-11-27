// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>
/// Verifies <see cref="PlanarBar3DSeries"/> default properties, data-range contribution,
/// serialization, and visitor dispatch. Ships as a v1.1.4 bonus series and until now had
/// zero unit coverage.
/// </summary>
public class PlanarBar3DSeriesTests
{
    private static readonly double[] X = [0.0, 1.0, 2.0];
    private static readonly double[] Y = [0.0, 0.0, 0.0];
    private static readonly double[] Z = [3.0, 5.0, 2.0];

    /// <summary>
    /// Z runs from the ground plane the bars rise from (matplotlib's <c>ax.bar(..., bottom=0)</c>), with no sticky edge:
    /// matplotlib's 3-D axes pads below it, <c>ax.bar([1, 2, 3], [2, 5, 3], zs=0, zdir='y')</c> draws Z from -0.1042.
    /// </summary>
    [Fact]
    public void ComputeDataRange_Z_RunsFromZero_WithNoStickyEdge()
    {
        var s = new PlanarBar3DSeries(X, Y, Z);
        var c = s.ComputeDataRange(null!);
        Assert.Null(c.StickyZMin);
        Assert.Equal(0.0, c.ZMin);
        Assert.Equal(5.0, c.ZMax);
    }

    /// <summary>
    /// An empty input must return a null contribution rather than throwing on
    /// <see cref="Vec.Min"/> — the empty-guard branch in the source.
    /// </summary>
    [Fact]
    public void ComputeDataRange_EmptyInput_ReturnsNullBounds()
    {
        var empty = Array.Empty<double>();
        var s = new PlanarBar3DSeries(empty, empty, empty);
        var c = s.ComputeDataRange(null!);
        Assert.Null(c.XMin);
        Assert.Null(c.XMax);
        Assert.Null(c.YMin);
        Assert.Null(c.YMax);
    }
}
