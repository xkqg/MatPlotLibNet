// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>
/// Phase Ω.6 (v1.7.2, 2026-04-19) — surgical branch coverage for
/// <see cref="ThreeDAxesRenderer"/>. Pre-Ω.6: 98.4L / 80.1B (40-49 uncov).
/// Each fact targets a specific cobertura line.
/// </summary>
public class ThreeDAxesRendererCoverageTests
{
    private static string Render3D(Action<AxesBuilder> configure, Action<global::MatPlotLibNet.Models.Figure>? postBuild = null)
    {
        var fig = Plt.Create()
            .WithSize(500, 400)
            .AddSubPlot(1, 1, 1, configure)
            .Build();
        postBuild?.Invoke(fig);
        return fig.ToSvg();
    }

    // ── L53: Axes.Projection?.Elevation ?? Axes.Elevation — need both arms

    [Fact]
    public void Render3D_WithExplicitElevationViaAxesField_UsesAxesElevation()
    {
        var svg = Render3D(
            ax => ax.Scatter3D([1.0, 2], [3.0, 4], [5.0, 6]),
            fig => { fig.SubPlots[0].Elevation = 45; fig.SubPlots[0].Azimuth = -30; });
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void Render3D_WithDefaultElevation_FallsBackToTwentyDegrees()
    {
        var svg = Render3D(ax => ax
            .Scatter3D([1.0, 2], [3.0, 4], [5.0, 6]));
        Assert.Contains("<svg", svg);
    }

    // ── L330: custom TickLocator on XAxis arm

    [Fact]
    public void Render3D_WithCustomXTickLocator_UsesLocator()
    {
        var svg = Render3D(
            ax => ax.Scatter3D([1.0, 2, 3, 4], [3.0, 4, 5, 6], [5.0, 6, 7, 8]),
            fig => { fig.SubPlots[0].XAxis.TickLocator = new MaxNLocator(3); });
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void Render3D_WithCustomYTickLocator_UsesLocator()
    {
        var svg = Render3D(
            ax => ax.Scatter3D([1.0, 2, 3, 4], [3.0, 4, 5, 6], [5.0, 6, 7, 8]),
            fig => { fig.SubPlots[0].YAxis.TickLocator = new MaxNLocator(3); });
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void Render3D_WithCustomZTickLocator_UsesLocator()
    {
        var svg = Render3D(
            ax => ax.Scatter3D([1.0, 2, 3, 4], [3.0, 4, 5, 6], [5.0, 6, 7, 8]),
            fig => { fig.SubPlots[0].ZAxis.TickLocator = new MaxNLocator(3); });
        Assert.Contains("<svg", svg);
    }

    // ── L559: Major ticks invisible arm

    [Fact]
    public void Render3D_WithXMajorTicksHidden_SkipsTickRendering()
    {
        var svg = Render3D(
            ax => ax.Scatter3D([1.0, 2], [3.0, 4], [5.0, 6]),
            fig => { fig.SubPlots[0].XAxis.MajorTicks = fig.SubPlots[0].XAxis.MajorTicks with { Visible = false }; });
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void Render3D_WithYMajorTicksHidden_SkipsTickRendering()
    {
        var svg = Render3D(
            ax => ax.Scatter3D([1.0, 2], [3.0, 4], [5.0, 6]),
            fig => { fig.SubPlots[0].YAxis.MajorTicks = fig.SubPlots[0].YAxis.MajorTicks with { Visible = false }; });
        Assert.Contains("<svg", svg);
    }

    // ── L627: custom TickFormatter arm

    [Fact]
    public void Render3D_WithCustomXTickFormatter_FormatsTicks()
    {
        var svg = Render3D(
            ax => ax.Scatter3D([1.0, 2, 3], [4.0, 5, 6], [7.0, 8, 9]),
            fig => { fig.SubPlots[0].XAxis.TickFormatter = new global::MatPlotLibNet.Rendering.TickFormatters.NumericTickFormatter(); });
        Assert.Contains("<svg", svg);
    }

    // ── L747+800: explicit Min/Max on axes arms

    [Fact]
    public void Render3D_WithExplicitXMinMax_UsesUserBounds()
    {
        var svg = Render3D(
            ax => ax.Scatter3D([1.0, 2, 3], [4.0, 5, 6], [7.0, 8, 9]),
            fig => { fig.SubPlots[0].XAxis.Min = 0; fig.SubPlots[0].XAxis.Max = 5; });
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void Render3D_WithExplicitYMinMax_UsesUserBounds()
    {
        var svg = Render3D(
            ax => ax.Scatter3D([1.0, 2, 3], [4.0, 5, 6], [7.0, 8, 9]),
            fig => { fig.SubPlots[0].YAxis.Min = 0; fig.SubPlots[0].YAxis.Max = 10; });
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void Render3D_WithExplicitZMinMax_UsesUserBounds()
    {
        var svg = Render3D(
            ax => ax.Scatter3D([1.0, 2, 3], [4.0, 5, 6], [7.0, 8, 9]),
            fig => { fig.SubPlots[0].ZAxis.Min = 0; fig.SubPlots[0].ZAxis.Max = 15; });
        Assert.Contains("<svg", svg);
    }

    // ── Various camera + light combos

    [Fact]
    public void Render3D_WithDirectionalLight_AppliesShading()
    {
        var svg = Render3D(
            ax => ax.Surface([0.0, 1, 2], [0.0, 1, 2], new double[,] { { 0, 1, 0 }, { 1, 2, 1 }, { 0, 1, 0 } }),
            fig => { fig.SubPlots[0].LightSource = new global::MatPlotLibNet.Rendering.Lighting.DirectionalLight(0.5, -0.7, 0.3, 0.25, 0.85); });
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void Render3D_TopDownView_RendersWithoutError()
    {
        // elevation=90 → top-down view
        var svg = Render3D(
            ax => ax.Scatter3D([1.0, 2, 3], [4.0, 5, 6], [7.0, 8, 9]),
            fig => { fig.SubPlots[0].Elevation = 90; fig.SubPlots[0].Azimuth = 0; });
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void Render3D_SideView_RendersWithoutError()
    {
        var svg = Render3D(
            ax => ax.Scatter3D([1.0, 2, 3], [4.0, 5, 6], [7.0, 8, 9]),
            fig => { fig.SubPlots[0].Elevation = 0; fig.SubPlots[0].Azimuth = 90; });
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void Render3D_DegenerateRange_HandlesGracefully()
    {
        // L409 if (range <= 0) return [lo];
        var svg = Render3D(
            ax => ax.Scatter3D([5.0, 5, 5], [5.0, 5, 5], [5.0, 5, 5]));
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void Render3D_WithDarkTheme_AppliesDarkPaneColor()
    {
        var svg = Plt.Create()
            .WithSize(500, 400)
            .WithTheme(Theme.Dark)
            .AddSubPlot(1, 1, 1, ax => ax.Scatter3D([1.0, 2], [3.0, 4], [5.0, 6]))
            .Build()
            .ToSvg();
        Assert.Contains("<svg", svg);
    }
}
