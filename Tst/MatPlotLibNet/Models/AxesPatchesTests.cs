// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Patches;

namespace MatPlotLibNet.Tests.Models;

/// <summary><see cref="Axes.AddPatch{TPatch}"/>, matplotlib's <c>add_patch</c>: the patch made by the caller joins the axes'
/// list as it is, in the order it was added, and comes back with its own type so it can be configured further.</summary>
public class AxesPatchesTests
{
    [Fact]
    public void AnAxes_HasNoPatch_UntilOneIsAdded()
    {
        Assert.Empty(new Axes().Patches);
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
