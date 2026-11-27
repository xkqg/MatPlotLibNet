// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Patches;

namespace MatPlotLibNet.Tests.Models;

/// <summary><see cref="Axes.AddPatch{TPatch}"/>, matplotlib's <c>add_patch</c>: the patch made by the caller joins the axes'
/// list as it is, in the order it was added, and comes back with its own type so it can be configured further.</summary>
public class AxesPatchesTests
{
    /// <summary>Bytes a new axes allocates: every figure pays them for each subplot, so a list kept for a feature most axes never
    /// use (where each patch stands among the series) is made when the first patch is added, not with the axes. Measured: 1,744 B on
    /// net10.0 Release (the three build-time requests ShareXKey, ShareYKey and BarSlotX included); the ceiling is one 8-byte field
    /// above it.</summary>
    private const long NewAxesBytes = 1_752;

    [Fact]
    public void AnAxes_AllocatesNoListForItsPatches_UntilOneIsAdded()
    {
        _ = new Axes();
        Axes axes;

        long before = GC.GetAllocatedBytesForCurrentThread();
        axes = new Axes();
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.True(allocated <= NewAxesBytes, $"A new axes allocated {allocated} bytes; the ceiling is {NewAxesBytes}.");
        axes.AddPatch(new Rectangle(new(0, 0), 1, 1));
        axes.AddPatch(new Rectangle(new(1, 1), 1, 1));
        Assert.Equal(2, axes.Patches.Count);
    }

    [Fact]
    public void AddPatch_ReturnsThePatchItWasGiven_WithItsOwnType()
    {
        var axes = new Axes();
        var rectangle = new Rectangle(new(0, 0), 2, 1);

        Rectangle added = axes.AddPatch(rectangle);

        Assert.Same(rectangle, added);
        Assert.Same(rectangle, Assert.Single(axes.Patches));
    }

    [Fact]
    public void ThePatches_StandInTheOrderTheyWereAdded()
    {
        var axes = new Axes();
        var arrow = axes.AddPatch(new Arrow(new(0, 0), 1, 1));
        var polygon = axes.AddPatch(new Polygon([new(0, 0), new(1, 0), new(0, 1)]));
        var rectangle = axes.AddPatch(new Rectangle(new(1, 1), 1, 1));

        Assert.Equal(new Patch[] { arrow, polygon, rectangle }, axes.Patches);
    }

    [Fact]
    public void AddingNoPatch_IsRefused_AndAddsNothing()
    {
        var axes = new Axes();

        var refused = Assert.Throws<ArgumentNullException>(() => axes.AddPatch<Rectangle>(null!));

        Assert.Equal("patch", refused.ParamName);
        Assert.Empty(axes.Patches);
    }

    [Fact]
    public void APatchAdded_IsThePatchTheAxesHolds_SoAChangeMadeAfterwardsIsDrawn()
    {
        var axes = new Axes();
        var rectangle = axes.AddPatch(new Rectangle(new(0, 0), 2, 1));

        rectangle.Angle = 45;

        Assert.Equal(45, ((Rectangle)axes.Patches[0]).Angle);
    }
}
