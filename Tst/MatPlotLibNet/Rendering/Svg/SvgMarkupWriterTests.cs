// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;

namespace MatPlotLibNet.Tests.Rendering.Svg;

/// <summary>What the SVG surface writes for the browser (<see cref="Rendering.Svg.SvgRenderContext.SvgMarkupWriter"/>): groups, links, the views and scenes the
/// scripts read, and the opacity wrapper. Every one opens outside the clips, so a group drawn in a clip only ever holds marks.</summary>
public class SvgMarkupWriterTests
{
    private static readonly RectGeometry Box = new(new Rect(0, 0, 10, 10));

    /// <summary>A buffer and the markup writer over it.</summary>
    private sealed class MarkupSurface
    {
        internal SvgElementBuffer Buffer { get; } = new(default);

        internal SvgRenderContext.SvgMarkupWriter Markup;

        internal MarkupSurface() => Markup = new SvgRenderContext.SvgMarkupWriter(Buffer);

        internal string Written => Buffer.Markup.ToString();
    }

    [Fact]
    public void AGroup_OpensWithItsClass_AndAnAccessibleOneWithItsEscapedName()
    {
        var surface = new MarkupSurface();

        surface.Markup.BeginGroup("legend");
        surface.Markup.BeginAccessibleGroup("series", "a&b");
        surface.Markup.EndGroup();

        Assert.Equal("<g class=\"legend\">\n<g class=\"series\" aria-label=\"a&amp;b\">\n</g>\n", surface.Written);
    }

    [Fact]
    public void ALink_CarriesItsTarget_ItsNameAndWhetherWhatItDisclosesIsShown()
    {
        var surface = new MarkupSurface();

        surface.Markup.BeginHyperlink("u?a=1&b=2", "name", expanded: true);
        surface.Markup.EndHyperlink();
        surface.Markup.BeginHyperlink("v", null, expanded: false);
        surface.Markup.BeginHyperlink("w", "", expanded: null);

        Assert.Equal("<a href=\"u?a=1&amp;b=2\" style=\"cursor:pointer\" aria-label=\"name\" aria-expanded=\"true\">\n</a>\n"
            + "<a href=\"v\" style=\"cursor:pointer\" aria-expanded=\"false\">\n<a href=\"w\" style=\"cursor:pointer\">\n", surface.Written);
    }

    [Fact]
    public void ASeriesGroup_CarriesItsSlot_WhenTheDrawWritesInteractiveAttributes()
    {
        var surface = new MarkupSurface();

        Assert.True(surface.Markup.OpenSeriesGroup(new SvgEmit(null, true, false, null, false), "L", 3, null));

        Assert.Equal("<g class=\"series\" data-series-index=\"3\" aria-label=\"L\">\n", surface.Written);
    }

    [Fact]
    public void ASeriesGroup_WithoutInteractiveAttributes_IsNamedByItsLabel_OrNotOpened()
    {
        var surface = new MarkupSurface();
        var emit = new SvgEmit(null, false, false, null, false);

        Assert.True(surface.Markup.OpenSeriesGroup(emit, "L", 3, null));
        Assert.False(surface.Markup.OpenSeriesGroup(emit, null, 4, null));
        Assert.False(surface.Markup.OpenSeriesGroup(emit, "", 5, null));

        Assert.Equal("<g class=\"series\" aria-label=\"L\">\n", surface.Written);
    }

    [Fact]
    public void APatchGroup_CarriesTheIndexItsLegendRowToggles()
    {
        var surface = new MarkupSurface();

        surface.Markup.OpenPatchGroup(2, "P");

        Assert.Equal("<g class=\"patch\" data-series-index=\"2\" aria-label=\"P\">\n", surface.Written);
    }

    [Fact]
    public void ALegendEntry_CarriesTheIndexItToggles_AndATooltipGroupItsTitle()
    {
        var surface = new MarkupSurface();

        surface.Markup.BeginLegendItemGroup(2, "L");
        surface.Markup.BeginLegendItemGroup(3, null);
        surface.Markup.BeginTooltipGroup("a<b");

        Assert.Equal("<g data-legend-index=\"2\" style=\"cursor:pointer\" aria-label=\"L\">\n"
            + "<g data-legend-index=\"3\" style=\"cursor:pointer\">\n<g><title>a&lt;b</title>\n", surface.Written);
    }

    [Fact]
    public void AScene_CarriesItsCamera_ItsFacesAndThePlotArea_AtTheRestingDistanceWhenItNamesNone()
    {
        var surface = new MarkupSurface();
        var faces = new CubeFaceSelection(new(CubeAxis.X, CubeSide.Min), new(CubeAxis.Y, CubeSide.Max), new(CubeAxis.Z, CubeSide.Min));

        surface.Markup.Begin3DSceneGroup(30, -60, null, new Rect(1, 2, 3, 4), faces);

        Assert.StartsWith("<g class=\"mpl-3d-scene\" data-elevation=\"30\" data-azimuth=\"-60\" data-faces=\"010\" data-distance=\""
            + Camera3D.RestingDistance.ToString(System.Globalization.CultureInfo.InvariantCulture) + "\" data-plot-x=\"1\"", surface.Written);
        Assert.EndsWith(">\n", surface.Written);
    }

    [Fact]
    public void ATierOfAScene_TakesWhatWasAskedOfIt()
    {
        var surface = new MarkupSurface();
        surface.Buffer.SetNextData("v3d", "1,2,3");

        surface.Markup.Begin3DSubgroup("mpl-3d-marker");

        Assert.Equal("<g class=\"mpl-3d-marker\" data-v3d=\"1,2,3\">\n", surface.Written);
    }

    [Fact]
    public void TheOpacityWrapper_IsReplacedByTheNextOpacity_AndClosedByFullOpacity()
    {
        var surface = new MarkupSurface();

        surface.Markup.WrapInOpacity(0.5);
        surface.Markup.WrapInOpacity(0.25);
        surface.Markup.WrapInOpacity(1);
        surface.Markup.WrapInOpacity(1);

        Assert.Equal("<g opacity=\"0.5\">\n</g>\n<g opacity=\"0.25\">\n</g>\n", surface.Written);
    }

    [Fact]
    public void EveryGroup_OpensOutsideTheClips()
    {
        var surface = new MarkupSurface();
        surface.Buffer.PushClip(Box);

        surface.Markup.BeginGroup("legend");

        Assert.EndsWith("<g clip-path=\"url(#clip-0)\">\n</g>\n<g class=\"legend\">\n", surface.Written);
    }

    [Fact]
    public void TheViewOfAnAxes_StandsOutsideTheClips_AsEveryGroupDoes()
    {
        var surface = new MarkupSurface();
        surface.Buffer.PushClip(Box);

        surface.Markup.WriteAxesView(1, new Rect(1, 2, 3, 4), null, null);

        Assert.EndsWith("<g clip-path=\"url(#clip-0)\">\n</g>\n<g data-axes-index=\"1\" data-plot-x=\"1\" data-plot-y=\"2\" data-plot-w=\"3\" data-plot-h=\"4\" />\n",
            surface.Written);
    }
}
