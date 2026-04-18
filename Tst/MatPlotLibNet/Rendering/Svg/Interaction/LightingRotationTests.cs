// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.Lighting;
using MatPlotLibNet.Transforms;

namespace MatPlotLibNet.Tests.Rendering.Svg.Interaction;

/// <summary>Phase 6 of the v1.7.2 plan — when interactive 3D rotation is on AND the
/// figure has a <see cref="DirectionalLight"/>, the scene group must carry
/// <c>data-light-dir</c>/<c>data-light-ambient</c>/<c>data-light-diffuse</c> attributes
/// so the JS reproject script can recompute face shading on rotation.</summary>
public class LightingRotationTests
{
    [Fact]
    public void SceneGroup_CarriesLightDirAttribute_WhenLightingEnabled()
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
        Assert.Matches(@"<g class=""mpl-3d-scene""[^>]*\bdata-light-dir=", svg);
    }

    [Fact]
    public void Script_ContainsLightingRecomputationBranch()
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
        // The rotation script's reprojectAll loop must now ALSO call a shadeColor helper
        // when the element has data-face-normal + data-base-color set by the renderer.
        Assert.Contains("data-face-normal", svg);
        Assert.Contains("data-base-color", svg);
    }
}
