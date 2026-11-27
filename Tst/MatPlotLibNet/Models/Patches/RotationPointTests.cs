// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models.Patches;

namespace MatPlotLibNet.Tests.Models.Patches;

/// <summary>The three values of matplotlib's <c>rotation_point</c> (<c>'xy'</c>, <c>'center'</c>, an <c>(x, y)</c> pair) as one
/// value: what each one is, and where each puts the pivot of a rectangle of a given size.</summary>
public class RotationPointTests
{
    [Fact]
    public void ADefaultRotationPoint_IsTheAnchor()
    {
        Assert.Equal(RotationPoint.Anchor, default);
        Assert.False(RotationPoint.Anchor.IsCenter);
        Assert.Null(RotationPoint.Anchor.Point);
    }

    [Fact]
    public void TheCentre_IsNotTheAnchorAndHoldsNoPoint()
    {
        Assert.True(RotationPoint.Center.IsCenter);
        Assert.Null(RotationPoint.Center.Point);
        Assert.NotEqual(RotationPoint.Anchor, RotationPoint.Center);
    }

    [Fact]
    public void APointGiven_IsKeptAndDiffersFromBothNames()
    {
        var at = RotationPoint.At(new(3, -1));

        Assert.False(at.IsCenter);
        Assert.Equal(new DataPoint(3, -1), at.Point);
        Assert.NotEqual(RotationPoint.Anchor, at);
        Assert.NotEqual(RotationPoint.Center, at);
        Assert.Equal(RotationPoint.At(new(3, -1)), at);
        Assert.NotEqual(RotationPoint.At(new(3, 1)), at);
    }

    [Fact]
    public void TheAnchorSitsAtNoOffset_TheCentreAtHalfTheSignedSize_APointAtItsDifferenceFromTheAnchor()
    {
        var anchor = new DataPoint(1, 1);

        Assert.Equal(new DataPoint(0, 0), RotationPoint.Anchor.OffsetFrom(anchor, 2, 1));
        Assert.Equal(new DataPoint(1, 0.5), RotationPoint.Center.OffsetFrom(anchor, 2, 1));
        Assert.Equal(new DataPoint(-1, -0.5), RotationPoint.Center.OffsetFrom(anchor, -2, -1));
        Assert.Equal(new DataPoint(2, -1), RotationPoint.At(new(3, 0)).OffsetFrom(anchor, 2, 1));
    }
}
