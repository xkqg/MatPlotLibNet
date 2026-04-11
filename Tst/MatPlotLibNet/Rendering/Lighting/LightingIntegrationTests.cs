// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Lighting;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.Lighting;

/// <summary>Integration tests verifying lighting pipeline end-to-end.</summary>
public class LightingIntegrationTests
{
    private static string RenderAxes(Axes axes)
    {
        var ctx = new SvgRenderContext();
        var renderer = AxesRenderer.Create(axes, new Rect(10, 10, 380, 280), ctx, Theme.Default);
        renderer.Render();
        var sb = new System.Text.StringBuilder();
        ctx.WriteTo(sb);
        return sb.ToString();
    }

    [Fact]
    public void Axes_LightSource_DefaultIsNull()
    {
        var axes = new Axes();
        Assert.Null(axes.LightSource);
    }

    [Fact]
    public void Surface_WithLighting_RendersWithoutError()
    {
        double[] x = [0, 5, 10];
        double[] y = [0, 5, 10];
        double[,] z = { { 0, 1, 2 }, { 3, 4, 5 }, { 6, 7, 8 } };

        var axes = new Axes
        {
            LightSource = new DirectionalLight(1, 1, 1)
        };
        axes.Surface(x, y, z);

        var svg = RenderAxes(axes);
        Assert.Contains("<polygon", svg);
    }

    [Fact]
    public void Surface_WithLighting_FaceColorsVary()
    {
        double[] x = [0, 5, 10];
        double[] y = [0, 5, 10];
        double[,] z = { { 0, 1, 2 }, { 3, 4, 5 }, { 6, 7, 8 } };

        // Without lighting
        var axesNoLight = new Axes();
        axesNoLight.Surface(x, y, z);
        string svgNoLight = RenderAxes(axesNoLight);

        // With lighting from one side
        var axesLight = new Axes
        {
            LightSource = new DirectionalLight(0, 0, 1, Ambient: 0.2, Diffuse: 0.8)
        };
        axesLight.Surface(x, y, z);
        string svgLight = RenderAxes(axesLight);

        // Lighting modulates colors, so SVGs must differ
        Assert.NotEqual(svgNoLight, svgLight);
    }

    [Fact]
    public void Bar3D_WithLighting_RendersWithoutError()
    {
        double[] x = [1, 2, 3];
        double[] y = [1, 2, 3];
        double[] z = [4, 5, 6];

        var axes = new Axes
        {
            LightSource = new DirectionalLight(1, 0, 1)
        };
        axes.Bar3D(x, y, z);

        var svg = RenderAxes(axes);
        Assert.Contains("<polygon", svg);
    }

    [Fact]
    public void AxesBuilder_WithLighting_SetsLightSource()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Surface([0, 5, 10], [0, 5, 10], new double[,] { { 0, 1, 2 }, { 3, 4, 5 }, { 6, 7, 8 } })
                .WithLighting(1, 1, 1))
            .Build();

        Assert.NotNull(figure.SubPlots[0].LightSource);
    }

    [Fact]
    public void FigureBuilder_WithLighting_SetsOnDefaultAxes()
    {
        var figure = Plt.Create()
            .Surface([0, 5, 10], [0, 5, 10], new double[,] { { 0, 1, 2 }, { 3, 4, 5 }, { 6, 7, 8 } })
            .WithLighting(0, 0, 1)
            .Build();

        Assert.NotNull(figure.SubPlots[0].LightSource);
    }
}
