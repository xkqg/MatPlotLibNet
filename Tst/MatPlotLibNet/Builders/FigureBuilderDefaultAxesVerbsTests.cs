// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Patches;

namespace MatPlotLibNet.Tests.Builders;

/// <summary>The verbs a chart of one axes takes on the figure (<see cref="FigureBuilderDefaultAxesVerbs"/>): <c>AddPatch</c> puts
/// the patch on the figure's default axes, as the other overlays of the figure builder (<c>AxHSpan</c>, <c>Annotate</c>) do.
/// That every forward draws what the axes verb of the same name draws is <c>VerbContractTests</c>' theory.</summary>
public class FigureBuilderDefaultAxesVerbsTests
{
    [Fact]
    public void AddPatch_PutsThePatchOnTheDefaultAxes_BesideItsSeries()
    {
        var rectangle = new Rectangle(new(1, 1), 2, 3);

        var figure = Plt.Create().Plot([1.0, 2, 3], [2.0, 4, 3]).AddPatch(rectangle).Build();

        var axes = Assert.Single(figure.SubPlots);
        Assert.Single(axes.Series);
        Assert.Same(rectangle, Assert.Single(axes.Patches));
    }

    [Fact]
    public void AFigureOfPatchesAlone_HasTheDefaultAxes_ThatHoldsThem()
    {
        var polygon = new Polygon([new(0, 0), new(1, 0), new(0, 1)]);
        var arrow = new Arrow(new(0, 0), 1, 1);

        var figure = Plt.Create().AddPatch(polygon).AddPatch(arrow).Build();

        Assert.Equal(new Patch[] { polygon, arrow }, Assert.Single(figure.SubPlots).Patches);
    }

    [Fact]
    public void AddPatch_ReturnsTheFigureBuilder_SoTheChainGoesOn()
    {
        var builder = Plt.Create();

        Assert.Same(builder, builder.AddPatch(new Rectangle(new(0, 0), 1, 1)));
    }

    [Fact]
    public void AddingNoPatch_IsRefused()
    {
        var refused = Assert.Throws<ArgumentNullException>(() => Plt.Create().AddPatch(null!));

        Assert.Equal("patch", refused.ParamName);
    }
}
