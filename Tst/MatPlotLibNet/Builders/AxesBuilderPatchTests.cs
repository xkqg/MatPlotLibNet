// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Patches;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Builders;

/// <summary><see cref="AxesBuilder.AddPatch"/>: one method for every kind of patch (the caller makes the patch, styled through
/// its own properties), so a kind added later needs no new method here.</summary>
public class AxesBuilderPatchTests
{
    [Fact]
    public void AddPatch_PutsThePatchOnTheSubplot_AndTheChainGoesOn()
    {
        var rectangle = new Rectangle(new(1, 1), 2, 3) { FaceColor = Colors.Red };
        AxesBuilder? chained = null;

        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax =>
        {
            var returned = ax.AddPatch(rectangle);
            chained = returned;
            Assert.Same(ax, returned);
        }).Build();

        Assert.NotNull(chained);
        Assert.Same(rectangle, Assert.Single(figure.SubPlots[0].Patches));
    }

    [Fact]
    public void EveryKindOfPatch_GoesThroughTheOneMethod_InTheOrderAdded()
    {
        var arrow = new Arrow(new(0, 0), 1, 1);
        var polygon = new Polygon([new(0, 0), new(1, 0), new(0, 1)]) { Closed = false };
        var rectangle = new Rectangle(new(0, 0), 1, 1) { Angle = 30 };

        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.AddPatch(arrow).AddPatch(polygon).AddPatch(rectangle)).Build();

        Assert.Equal(new Patch[] { arrow, polygon, rectangle }, figure.SubPlots[0].Patches);
    }

    [Fact]
    public void AddingNoPatch_IsRefused_BeforeAnythingIsAdded()
    {
        var builder = new AxesBuilder();

        var refused = Assert.Throws<ArgumentNullException>(() => builder.AddPatch(null!));

        Assert.Equal("patch", refused.ParamName);
    }

    [Fact]
    public void AddPatch_TakesOneParameter()
    {
        var method = typeof(AxesBuilder).GetMethod(nameof(AxesBuilder.AddPatch))!;

        Assert.Equal([typeof(Patch)], method.GetParameters().Select(parameter => parameter.ParameterType));
        Assert.Equal(typeof(AxesBuilder), method.ReturnType);
    }
}
