// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Lighting;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.Lighting;

/// <summary>Integration tests verifying lighting pipeline end-to-end.</summary>
public class LightingIntegrationTests
{
    /// <summary>The fill of every polygon of the picture, as upper-case hex.</summary>
    private static HashSet<string> Fills(string svg) =>
        [.. svg.Elements("polygon").Select(polygon => polygon["fill"]).OfType<string>().Where(fill => fill.StartsWith('#')).Select(fill => fill.ToUpperInvariant())];

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
        var withDefault = Fills(FlatSurface(new DirectionalLight(1, 1, 1)).RenderedSvg(new Rect(10, 10, 380, 280)));
        var baseColor = Color.FromHex(Fills(FlatSurface(null).RenderedSvg(new Rect(10, 10, 380, 280))).Except(withDefault).Single());

        string lit = FlatSurface(light).RenderedSvg(new Rect(10, 10, 380, 280));

        Assert.NotEqual(FlatSurface(new DirectionalLight(1, 1, 1)).RenderedSvg(new Rect(10, 10, 380, 280)), lit);
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

        string lit = Bars(light).RenderedSvg(new Rect(10, 10, 380, 280));

        Assert.NotEqual(Bars(new DirectionalLight(1, 1, 1)).RenderedSvg(new Rect(10, 10, 380, 280)), lit);
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
        var axes = new Axes { LightSource = new TestLightSource(0.5) };
        axes.Bar3D(x, y, z);
        var unlit = new Axes();
        unlit.Bar3D(x, y, z);
        var baseColor = Theme.Default.CycleColors[0];

        string lit = axes.RenderedSvg(new Rect(10, 10, 380, 280));

        Assert.NotEqual(unlit.RenderedSvg(new Rect(10, 10, 380, 280)), lit);
        Assert.Contains(baseColor.Modulate(0.5).ToHex().ToUpperInvariant(), Fills(lit));
    }

    /// <summary>matplotlib's <c>bar3d</c> shades by default (<c>shade=True</c>): with no light given, <c>art3d._shade_colors</c>
    /// lights the faces from <c>LightSource(azdeg=225, altdeg=19.4712)</c>. The expected fills are matplotlib 3.11's own,
    /// printed by <c>to_hex(_shade_colors('#1f77b4', _generate_normals(cuboid)))</c>: top <c>#185b8a</c>, the -y and -x faces
    /// <c>#1b699f</c>, the +y and +x faces <c>#0d324b</c>. Measured before the change: an unlit bar drew its base colour at
    /// three opacities, no face as matplotlib draws it.</summary>
    [Fact]
    public void Bar3D_WithNoLight_IsShadedAsMatplotlibShadesIt()
    {
        var axes = new Axes();
        double[] x = [1.0, 2], y = [1.0, 2], z = [4.0, 5];
        axes.Bar3D(x, y, z).Color = Color.FromHex("#1F77B4");

        var fills = Fills(axes.RenderedSvg(new Rect(10, 10, 380, 280)));

        Assert.Contains("#185B8A", fills);
        Assert.Contains("#1B699F", fills);
        Assert.Contains("#0D324B", fills);
    }

    [Fact]
    public void Axes_LightSource_DefaultIsNull()
    {
        var axes = new Axes();
        Assert.Null(axes.LightSource);
    }

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("Surface_WithLighting_RendersWithoutError", () =>
        {
            double[] x = [0, 5, 10];
            double[] y = [0, 5, 10];
            double[,] z = { { 0, 1, 2 }, { 3, 4, 5 }, { 6, 7, 8 } };
            var axes = new Axes
            {
                LightSource = new DirectionalLight(1, 1, 1)
            };
            axes.Surface(x, y, z);
            return axes.RenderedSvg(new Rect(10, 10, 380, 280));
        }, "<polygon"),
        new DrawnToken("Bar3D_WithLighting_RendersWithoutError", () =>
        {
            double[] x = [1, 2, 3];
            double[] y = [1, 2, 3];
            double[] z = [4, 5, 6];
            var axes = new Axes
            {
                LightSource = new DirectionalLight(1, 0, 1)
            };
            axes.Bar3D(x, y, z);
            return axes.RenderedSvg(new Rect(10, 10, 380, 280));
        }, "<polygon"),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    /// <summary>A light source of the caller's own (ILightSource is public) modulates each face by the intensity it
    /// reports; only a <see cref="DirectionalLight"/> gets matplotlib's shading.</summary>
    [Fact]
    public void Surface_WithACustomLightSource_IsModulatedByItsIntensity()
    {
        var axes = new Axes { LightSource = new TestLightSource(0.5) };
        axes.Surface([0, 5, 10], [0, 5, 10], new double[,] { { 0, 1, 2 }, { 3, 4, 5 }, { 6, 7, 8 } });
        var unlit = new Axes();
        unlit.Surface([0, 5, 10], [0, 5, 10], new double[,] { { 0, 1, 2 }, { 3, 4, 5 }, { 6, 7, 8 } });

        string lit = axes.RenderedSvg(new Rect(10, 10, 380, 280));

        Assert.Contains("<polygon", lit);
        Assert.NotEqual(unlit.RenderedSvg(new Rect(10, 10, 380, 280)), lit);
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
        string svgNoLight = axesNoLight.RenderedSvg(new Rect(10, 10, 380, 280));

        // With lighting from one side
        var axesLight = new Axes
        {
            LightSource = new DirectionalLight(0, 0, 1, Ambient: 0.2, Diffuse: 0.8)
        };
        axesLight.Surface(x, y, z);
        string svgLight = axesLight.RenderedSvg(new Rect(10, 10, 380, 280));

        // Lighting modulates colors, so SVGs must differ
        Assert.NotEqual(svgNoLight, svgLight);
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
