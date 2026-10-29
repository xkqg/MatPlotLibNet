// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.Lighting;
using MatPlotLibNet.Transforms;

namespace MatPlotLibNet.Tests.Rendering.Svg.Interaction;

/// <summary>A lit figure with interactive 3-D rotation carries no recipe for shading in its SVG. The scene group once carried
/// <c>data-light-dir</c>, <c>data-light-ambient</c> and <c>data-light-diffuse</c>, and every face <c>data-face-normal</c> and
/// <c>data-base-color</c>, for a script to recompute the shading on rotation; no script reads them. The shading of a face is
/// worked out in data space, which a rotation does not change, so nothing needs recomputing, and a picture should not carry
/// what nothing reads.</summary>
public class LightingRotationTests
{
    [Fact]
    public void SceneGroup_CarriesNoLightAttribute_WhenLightingEnabled()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.WithLighting(dx: 0.5, dy: -0.5, dz: 1.0);
                ax.Surface([0, 5, 10], [0, 5, 10], new double[,] { { 0, 1, 2 }, { 3, 4, 5 }, { 6, 7, 8 } });
            })
            .With3DRotation()
            .Build();

        var svg = new SvgTransform().Render(figure);
        Assert.Contains("<g class=\"mpl-3d-scene\"", svg);
        Assert.DoesNotContain("data-light", svg);
    }

    [Fact]
    public void Faces_CarryNoShadingRecipe_WhenLightingEnabled()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.WithLighting(dx: 0.5, dy: -0.5, dz: 1.0);
                ax.Surface([0, 5, 10], [0, 5, 10], new double[,] { { 0, 1, 2 }, { 3, 4, 5 }, { 6, 7, 8 } });
            })
            .With3DRotation()
            .Build();

        var svg = new SvgTransform().Render(figure);
        Assert.Contains("data-v3d", svg);
        Assert.DoesNotContain("data-face-normal", svg);
        Assert.DoesNotContain("data-base-color", svg);
    }
}
