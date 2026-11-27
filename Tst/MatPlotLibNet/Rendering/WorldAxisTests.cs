// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>One axis of the world box a 3-D projection maps its data into (matplotlib's <c>world_transformation</c>): the data range
/// along it, widened when flat, and the share of the box it spans, held once for X, Y and Z alike.</summary>
public class WorldAxisTests
{
    [Fact]
    public void AValue_IsMeasuredFromTheLowEnd_AndScaledToTheAxisShareOfTheBox()
    {
        var axis = Projection3D.WorldAxis.Of(new Range1D(2, 6), 25.0 / 21.0);

        Assert.Equal(3, axis.From(5));
        Assert.Equal(25.0 / 21.0 / 4, axis.Scale);
        Assert.Equal(25.0 / 21.0 * 3 / 4, axis.Share(5));
        Assert.Equal(25.0 / 21.0 / 2, axis.Middle);
    }

    /// <summary>A flat range is widened by half a unit each side first, so the axis still has a width to scale by.</summary>
    [Fact]
    public void AFlatRange_IsWidenedBeforeItIsScaled()
    {
        var axis = Projection3D.WorldAxis.Of(new Range1D(3, 3), 1);

        Assert.Equal(new Range1D(2.5, 3.5), axis.Range);
        Assert.Equal(1, axis.Scale);
    }

    /// <summary>A range set high to low scales the other way round, as matplotlib draws an inverted 3-D axis.</summary>
    [Fact]
    public void AReversedRange_ScalesTheOtherWayRound()
    {
        Assert.Equal(-0.5, Projection3D.WorldAxis.Of(new Range1D(4, 2), 1).Scale);
    }
}
