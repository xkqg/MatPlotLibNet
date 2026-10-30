// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A coordinate system is a promise that something draws it. <see cref="AxesRenderer.Create"/> falls back to
/// the Cartesian renderer for a value it does not know, which keeps an unknown number from throwing but also means a
/// value that was added to <see cref="CoordinateSystem"/> and never given a renderer would draw as a Cartesian plot
/// without a word. This walks every defined value through the factory and requires each one but the Cartesian one to
/// get a renderer of its own.</summary>
public class CoordinateSystemRendererContractTests
{
    [Fact]
    public void EveryCoordinateSystemButCartesian_GetsARendererOfItsOwn()
    {
        foreach (var system in Enum.GetValues<CoordinateSystem>().Where(s => s != CoordinateSystem.Cartesian))
        {
            var axes = new Axes { CoordinateSystem = system };
            var renderer = AxesRenderer.Create(axes, new Rect(50, 50, 300, 200), new SvgRenderContext(), Theme.Default);

            Assert.False(renderer is CartesianAxesRenderer,
                $"CoordinateSystem.{system} falls back to the Cartesian renderer: AxesRenderer has no factory row for it");
        }
    }

    [Fact]
    public void TheCartesianCoordinateSystem_IsDrawnByTheCartesianRenderer()
    {
        var renderer = AxesRenderer.Create(new Axes(), new Rect(50, 50, 300, 200), new SvgRenderContext(), Theme.Default);

        Assert.IsType<CartesianAxesRenderer>(renderer);
    }
}
