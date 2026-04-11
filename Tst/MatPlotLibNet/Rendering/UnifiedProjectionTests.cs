// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies that the unified Projection3D is threaded through to all 3D series renderers.</summary>
public class UnifiedProjectionTests
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
    public void ThreeDAxes_UsesAxesElevation()
    {
        double[] x = [0, 5, 10];
        double[] y = [0, 5, 10];
        double[,] z = { { 0, 1, 2 }, { 3, 4, 5 }, { 6, 7, 8 } };

        var axesDefault = new Axes();
        axesDefault.Surface(x, y, z);

        var axesCustom = new Axes { Elevation = 60 };
        axesCustom.Surface(x, y, z);

        string svgDefault = RenderAxes(axesDefault);
        string svgCustom = RenderAxes(axesCustom);

        // Different elevation should produce different SVG polygon coordinates
        Assert.NotEqual(svgDefault, svgCustom);
    }

    [Fact]
    public void ThreeDAxes_UsesAxesAzimuth()
    {
        double[] x = [0, 5, 10];
        double[] y = [0, 5, 10];
        double[,] z = { { 0, 1, 2 }, { 3, 4, 5 }, { 6, 7, 8 } };

        var axesDefault = new Axes();
        axesDefault.Surface(x, y, z);

        var axesCustom = new Axes { Azimuth = 0 };
        axesCustom.Surface(x, y, z);

        string svgDefault = RenderAxes(axesDefault);
        string svgCustom = RenderAxes(axesCustom);

        Assert.NotEqual(svgDefault, svgCustom);
    }

    [Fact]
    public void ThreeD_PerspectiveProjection_RendersSvg()
    {
        double[] x = [0, 5, 10];
        double[] y = [0, 5, 10];
        double[,] z = { { 0, 1, 2 }, { 3, 4, 5 }, { 6, 7, 8 } };

        var axes = new Axes { CameraDistance = 5.0 };
        axes.Surface(x, y, z);

        string svg = RenderAxes(axes);

        Assert.Contains("<polygon", svg);
    }
}
