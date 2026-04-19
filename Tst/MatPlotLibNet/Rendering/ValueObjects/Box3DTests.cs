// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Rendering.ValueObjects;

/// <summary>Phase X.4 follow-up (v1.7.2, 2026-04-19) — drives every public member of
/// <see cref="Box3D"/>. Pre-X the value-object was at 60%L because no test file
/// existed; only implicit usage via 3D series' ComputeDataRange hit the two
/// constructors + ToContribution. The three <c>With*</c> methods were untested.</summary>
public class Box3DTests
{
    [Fact]
    public void Constructor_FromRanges_ExposesExtents()
    {
        var box = new Box3D(new Range1D(0, 1), new Range1D(2, 3), new Range1D(4, 5));
        Assert.Equal(0, box.X.Lo);
        Assert.Equal(1, box.X.Hi);
        Assert.Equal(2, box.Y.Lo);
        Assert.Equal(5, box.Z.Hi);
    }

    [Fact]
    public void Constructor_FromScalars_WrapsRangesCorrectly()
    {
        var box = new Box3D(0, 1, 2, 3, 4, 5);
        Assert.Equal(new Range1D(0, 1), box.X);
        Assert.Equal(new Range1D(2, 3), box.Y);
        Assert.Equal(new Range1D(4, 5), box.Z);
    }

    [Fact]
    public void WithX_ReplacesOnlyXExtent()
    {
        var original = new Box3D(0, 1, 2, 3, 4, 5);
        var modified = original.WithX(new Range1D(10, 20));
        Assert.Equal(new Range1D(10, 20), modified.X);
        Assert.Equal(original.Y, modified.Y);
        Assert.Equal(original.Z, modified.Z);
    }

    [Fact]
    public void WithY_ReplacesOnlyYExtent()
    {
        var original = new Box3D(0, 1, 2, 3, 4, 5);
        var modified = original.WithY(new Range1D(10, 20));
        Assert.Equal(original.X, modified.X);
        Assert.Equal(new Range1D(10, 20), modified.Y);
        Assert.Equal(original.Z, modified.Z);
    }

    [Fact]
    public void WithZ_ReplacesOnlyZExtent()
    {
        var original = new Box3D(0, 1, 2, 3, 4, 5);
        var modified = original.WithZ(new Range1D(10, 20));
        Assert.Equal(original.X, modified.X);
        Assert.Equal(original.Y, modified.Y);
        Assert.Equal(new Range1D(10, 20), modified.Z);
    }

    [Fact]
    public void ToContribution_WithoutStickyArgs_WrapsAllExtents()
    {
        var box = new Box3D(0, 1, 2, 3, 4, 5);
        var contrib = box.ToContribution();
        Assert.Equal(0, contrib.XMin);
        Assert.Equal(1, contrib.XMax);
        Assert.Equal(2, contrib.YMin);
        Assert.Equal(3, contrib.YMax);
        Assert.Equal(4, contrib.ZMin);
        Assert.Equal(5, contrib.ZMax);
        Assert.Null(contrib.StickyXMin);
        Assert.Null(contrib.StickyZMax);
    }

    [Fact]
    public void ToContribution_WithStickyArgs_AttachesThem()
    {
        var box = new Box3D(0, 1, 2, 3, 4, 5);
        var contrib = box.ToContribution(stickyXMin: 0, stickyZMax: 100);
        Assert.Equal(0, contrib.StickyXMin);
        Assert.Equal(100, contrib.StickyZMax);
        Assert.Null(contrib.StickyXMax);
        Assert.Null(contrib.StickyYMin);
    }
}
