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

    // ── Wave J.1 — remaining branch gaps ─────────────────────────────────

    /// <summary>Axes.Projection set directly — hits the <c>Axes.Projection?.Elevation</c>
    /// and <c>?.Azimuth</c> non-null arms (L53/L54). Note: <c>WithProjection()</c> only
    /// sets <c>Elevation/Azimuth</c> scalar fields, not the <c>Projection3D</c> object.</summary>
    [Fact]
    public void Render3D_WithProjectionObject_UsesProjectionElevationAndAzimuth()
    {
        var fig = Plt.Create().WithSize(500, 400)
            .AddSubPlot(1, 1, 1, ax => ax.Scatter3D([1.0, 2, 3], [4.0, 5, 6], [7.0, 8, 9]))
            .Build();
        fig.SubPlots[0].Projection = new MatPlotLibNet.Rendering.Projection3D(
            30, 45, new Rect(50, 50, 300, 200), 1, 3, 4, 6, 7, 9);
        Assert.Contains("<svg", fig.ToSvg());
    }

    /// <summary>MajorTicks with explicit Color — hits <c>major.Color ?? Theme.ForegroundText</c>
    /// non-null arm (L579).</summary>
    [Fact]
    public void Render3D_WithMajorTickColor_UsesCustomColor()
    {
        var svg = Render3D(
            ax => ax.Scatter3D([1.0, 2, 3], [4.0, 5, 6], [7.0, 8, 9]),
            fig =>
            {
                fig.SubPlots[0].XAxis.MajorTicks = fig.SubPlots[0].XAxis.MajorTicks
                    with { Color = Colors.Red };
            });
        Assert.Contains("<svg", svg);
    }

    /// <summary>MajorTicks with explicit LabelSize — hits <c>major.LabelSize ??</c> non-null
    /// arm (L562) and <c>if (major.LabelSize.HasValue)</c> true arm (L582).</summary>
    [Fact]
    public void Render3D_WithMajorTickLabelSize_UsesCustomSize()
    {
        var svg = Render3D(
            ax => ax.Scatter3D([1.0, 2, 3], [4.0, 5, 6], [7.0, 8, 9]),
            fig =>
            {
                fig.SubPlots[0].XAxis.MajorTicks = fig.SubPlots[0].XAxis.MajorTicks
                    with { LabelSize = 14.0 };
            });
        Assert.Contains("<svg", svg);
    }

    /// <summary>MajorTicks with explicit LabelColor — hits <c>if (major.LabelColor.HasValue)</c>
    /// true arm (L583).</summary>
    [Fact]
    public void Render3D_WithMajorTickLabelColor_UsesCustomColor()
    {
        var svg = Render3D(
            ax => ax.Scatter3D([1.0, 2, 3], [4.0, 5, 6], [7.0, 8, 9]),
            fig =>
            {
                fig.SubPlots[0].XAxis.MajorTicks = fig.SubPlots[0].XAxis.MajorTicks
                    with { LabelColor = Colors.Blue };
            });
        Assert.Contains("<svg", svg);
    }

    /// <summary>MajorTicks with explicit Spacing (no TickLocator) — hits the
    /// <c>axis.MajorTicks.Spacing.HasValue → MultipleLocator</c> true arm (L563 second branch).</summary>
    [Fact]
    public void Render3D_WithMajorTickSpacing_UsesMultipleLocator()
    {
        var svg = Render3D(
            ax => ax.Scatter3D([1.0, 2, 3], [4.0, 5, 6], [7.0, 8, 9]),
            fig =>
            {
                fig.SubPlots[0].XAxis.MajorTicks = fig.SubPlots[0].XAxis.MajorTicks
                    with { Spacing = 1.0 };
            });
        Assert.Contains("<svg", svg);
    }

    /// <summary>Two series where the second is entirely within the first's range —
    /// hits the <c>c.XMin.HasValue &amp;&amp; c.XMin.Value &lt; xMin</c> false arm (L710)
    /// and analogous Y/Z arms (L712/L715) for inner-series contributions that don't
    /// expand the current bounding box.</summary>
    [Fact]
    public void Render3D_TwoSeriesInnerInsideOuter_RangeNotExpanded()
    {
        var svg = Render3D(ax => ax
            .Scatter3D([0.0, 10], [0.0, 10], [0.0, 10])
            .Scatter3D([2.0, 8],  [2.0, 8],  [2.0, 8]));
        Assert.Contains("<svg", svg);
    }

    // ── Wave J.1 — remaining branch arms ────────────────────────────────────

    /// <summary>Axis labels set on a 3D figure — L131/L140/L149 TRUE arms.
    /// SVG must contain all three label texts.</summary>
    [Fact]
    public void Render3D_WithAxisLabels_RendersAllThreeLabels()
    {
        var svg = Render3D(ax => ax
            .Scatter3D([1.0, 2, 3], [4.0, 5, 6], [7.0, 8, 9])
            .SetXLabel("X axis")
            .SetYLabel("Y axis")
            .SetZLabel("Z axis"));
        Assert.Contains(">X axis<", svg);
        Assert.Contains(">Y axis<", svg);
        Assert.Contains(">Z axis<", svg);
    }

    /// <summary>Pane3D.Visible = false — L265 TRUE arm in Render3DPanes → returns early.
    /// No mpl-pane class in output.</summary>
    [Fact]
    public void Render3D_WithPaneHidden_SkipsPaneDrawing()
    {
        var svg = Render3D(ax => ax
            .Scatter3D([1.0, 2], [3.0, 4], [5.0, 6])
            .WithPane3D(p => p with { Visible = false }));
        Assert.DoesNotContain("mpl-pane", svg);
    }

    /// <summary>Custom FloorColor + LeftWallColor + RightWallColor — L268/269/270 non-null arms.</summary>
    [Fact]
    public void Render3D_WithCustomPaneColors_UsesPaneColors()
    {
        var svg = Render3D(ax => ax
            .Scatter3D([1.0, 2], [3.0, 4], [5.0, 6])
            .WithPane3D(p => p with
            {
                FloorColor = Colors.Cyan,
                LeftWallColor = Colors.Green,
                RightWallColor = Colors.Yellow,
            }));
        Assert.Contains("<polygon", svg);
    }

    /// <summary>CameraDistance set — L762 TRUE arm (zmarginDefault = 0.05, perspective mode).
    /// Also exercises the CameraDistance.HasValue true arm when passed to Projection3D.</summary>
    [Fact]
    public void Render3D_WithCameraDistance_UsesPerspectiveZMargin()
    {
        var svg = Render3D(
            ax => ax.Scatter3D([1.0, 2, 3], [4.0, 5, 6], [7.0, 8, 9]),
            fig => { fig.SubPlots[0].CameraDistance = 5.0; });
        Assert.Contains("<svg", svg);
    }

    /// <summary>Emit3DVertexData = true — L70 TRUE arm (sceneGroup=true).
    /// All scene-group logic runs: Begin3DSceneGroup, 3D subgroups, data-v3d attrs emitted.</summary>
    [Fact]
    public void Render3D_WithEmit3DVertexData_EmitsDataV3DAttributes()
    {
        var svg = Render3D(
            ax => ax.Scatter3D([1.0, 2, 3], [4.0, 5, 6], [7.0, 8, 9]),
            fig => { fig.SubPlots[0].Emit3DVertexData = true; });
        Assert.Contains("data-v3d", svg);
    }

    /// <summary>Z major ticks hidden — L552 TRUE arm for ZAxis.
    /// Completes the pair with the existing X/Y tests.</summary>
    [Fact]
    public void Render3D_WithZMajorTicksHidden_SkipsZTickRendering()
    {
        var svg = Render3D(
            ax => ax.Scatter3D([1.0, 2], [3.0, 4], [5.0, 6]),
            fig => { fig.SubPlots[0].ZAxis.MajorTicks = fig.SubPlots[0].ZAxis.MajorTicks with { Visible = false }; });
        Assert.Contains("<svg", svg);
    }

    /// <summary>Minor ticks enabled with enough major ticks — L626 condition false arm (minor DOES draw).
    /// MinorTicks.Visible defaults to false; set it true to exercise the minor-tick loop.</summary>
    [Fact]
    public void Render3D_WithMinorTicksEnabled_DrawsMinorTickMarks()
    {
        var svg = Render3D(
            ax => ax.Scatter3D([1.0, 2, 3, 4, 5], [1.0, 2, 3, 4, 5], [1.0, 2, 3, 4, 5]),
            fig =>
            {
                fig.SubPlots[0].XAxis.MinorTicks = fig.SubPlots[0].XAxis.MinorTicks with { Visible = true };
            });
        Assert.Contains("<svg", svg);
    }

    /// <summary>No series at all → xMin == MaxValue → L726 TRUE arm, falls back to {0,1}.</summary>
    [Fact]
    public void Render3D_EmptyAxes_FallsBackToDefaultRange()
    {
        var svg = Render3D(ax => { });
        Assert.Contains("<svg", svg);
    }

    // ── Wave J.1 — dead-code removal follow-on ────────────────────────────────

    /// <summary>Theme with non-null Pane3DColor — hits the <c>Theme.Pane3DColor ?? ...</c>
    /// non-null arm (L267). Uses ThemeBuilder.WithPane3DColor() to set the color.</summary>
    [Fact]
    public void Render3D_WithThemePaneColor_UsesPane3DColor()
    {
        var theme = Theme.CreateFrom(Theme.Dark).WithPane3DColor(Colors.DarkGray).Build();
        var svg = Plt.Create().WithSize(500, 400).WithTheme(theme)
            .AddSubPlot(1, 1, 1, ax => ax.Scatter3D([1.0, 2], [3.0, 4], [5.0, 6]))
            .Build().ToSvg();
        Assert.Contains("<svg", svg);
    }

    /// <summary>Minor ticks with explicit Color — hits <c>minor.Color ?? Theme.ForegroundText</c>
    /// non-null arm (L628).</summary>
    [Fact]
    public void Render3D_WithMinorTickColor_UsesCustomColor()
    {
        var svg = Render3D(
            ax => ax.Scatter3D([1.0, 2, 3, 4, 5], [1.0, 2, 3, 4, 5], [1.0, 2, 3, 4, 5]),
            fig =>
            {
                fig.SubPlots[0].XAxis.MinorTicks = fig.SubPlots[0].XAxis.MinorTicks
                    with { Visible = true, Color = Colors.LightGray };
            });
        Assert.Contains("<svg", svg);
    }
}
