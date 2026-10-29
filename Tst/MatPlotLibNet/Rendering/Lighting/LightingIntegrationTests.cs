// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
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

    private static readonly Regex PolygonFill = new("<polygon[^>]* fill=\"(?<f>#[0-9A-Fa-f]{6})\"", RegexOptions.Compiled);

    /// <summary>The fill of every polygon of the picture, as upper-case hex.</summary>
    private static HashSet<string> Fills(string svg) =>
        [.. PolygonFill.Matches(svg).Select(m => m.Groups["f"].Value.ToUpperInvariant())];

    private static Axes FlatSurface(ILightSource? light)
    {
        var axes = new Axes { LightSource = light };
        double[] x = [0.0, 10], y = [0.0, 10];
        axes.Surface(x, y, new double[,] { { 3, 3 }, { 3, 3 } });
        return axes;
    }

    /// <summary>Measured before the change: <c>WithLighting(1, 1, 1, 0.6, 0.4)</c> drew the same picture as the default light,
    /// because the surface shaded with matplotlib's constants (0.65 and 0.35) and read only the direction of the light.</summary>
    [Fact]
    public void Surface_UnderALightOfOtherAmbientAndDiffuse_IsShadedByThem()
    {
        var light = new DirectionalLight(1, 1, 1, Ambient: 0.6, Diffuse: 0.4);
        var (nx, ny, nz) = Vec3.FaceNormal(new(0, 0, 3), new(10, 0, 3), new(0, 10, 3));
        var withDefault = Fills(RenderAxes(FlatSurface(new DirectionalLight(1, 1, 1))));
        var baseColor = Color.FromHex(Fills(RenderAxes(FlatSurface(null))).Except(withDefault).Single());

        string lit = RenderAxes(FlatSurface(light));

        Assert.NotEqual(RenderAxes(FlatSurface(new DirectionalLight(1, 1, 1))), lit);
        Assert.Contains(((ILightSource)light).Shade(baseColor, nx, ny, nz).ToHex().ToUpperInvariant(), Fills(lit));
    }

    /// <summary>The same question for a 3-D bar: every face of the prism is shaded by the ambient and the diffuse of the light.</summary>
    [Fact]
    public void Bar3D_UnderALightOfOtherAmbientAndDiffuse_IsShadedByThem()
    {
        var light = new DirectionalLight(1, 1, 1, Ambient: 0.6, Diffuse: 0.4);
        Axes Bars(ILightSource? source)
        {
            var axes = new Axes { LightSource = source };
            double[] x = [1.0, 2], y = [1.0, 2], z = [4.0, 5];
            axes.Bar3D(x, y, z);
            return axes;
        }
        var baseColor = Theme.Default.CycleColors[0];

        string lit = RenderAxes(Bars(light));

        Assert.NotEqual(RenderAxes(Bars(new DirectionalLight(1, 1, 1))), lit);
        var fills = Fills(lit);
        ILightSource source = light;
        foreach (var normal in new[] { (0.0, 0.0, 1.0), (0.0, 0.0, -1.0), (0.0, -1.0, 0.0), (0.0, 1.0, 0.0), (-1.0, 0.0, 0.0), (1.0, 0.0, 0.0) })
            Assert.Contains(source.Shade(baseColor, normal.Item1, normal.Item2, normal.Item3).ToHex().ToUpperInvariant(), fills);
    }

    /// <summary>Measured before the change: a 3-D bar ignored a light of the caller's own and was drawn with the fixed per-face
    /// opacities of an unlit bar, where a surface was modulated by it.</summary>
    [Fact]
    public void Bar3D_UnderACustomLightSource_IsShadedByIt()
    {
        double[] x = [1.0, 2], y = [1.0, 2], z = [4.0, 5];
        var axes = new Axes { LightSource = new HalfLight() };
        axes.Bar3D(x, y, z);
        var unlit = new Axes();
        unlit.Bar3D(x, y, z);
        var baseColor = Theme.Default.CycleColors[0];

        string lit = RenderAxes(axes);

        Assert.NotEqual(RenderAxes(unlit), lit);
        Assert.Contains(baseColor.Modulate(0.5).ToHex().ToUpperInvariant(), Fills(lit));
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

    /// <summary>A light source of the caller's own (ILightSource is public) modulates each face by the intensity it
    /// reports; only a <see cref="DirectionalLight"/> gets matplotlib's shading.</summary>
    [Fact]
    public void Surface_WithACustomLightSource_IsModulatedByItsIntensity()
    {
        var axes = new Axes { LightSource = new HalfLight() };
        axes.Surface([0, 5, 10], [0, 5, 10], new double[,] { { 0, 1, 2 }, { 3, 4, 5 }, { 6, 7, 8 } });
        var unlit = new Axes();
        unlit.Surface([0, 5, 10], [0, 5, 10], new double[,] { { 0, 1, 2 }, { 3, 4, 5 }, { 6, 7, 8 } });

        string lit = RenderAxes(axes);

        Assert.Contains("<polygon", lit);
        Assert.NotEqual(RenderAxes(unlit), lit);
    }

    private sealed class HalfLight : ILightSource
    {
        public double ComputeIntensity(double nx, double ny, double nz) => 0.5;
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
