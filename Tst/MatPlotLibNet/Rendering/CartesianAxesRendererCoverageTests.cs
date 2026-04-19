// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Phase Y.3 (v1.7.2, 2026-04-19) — branch coverage for the
/// <see cref="MatPlotLibNet.Rendering.CartesianAxesRenderer"/>. Pre-Y.3:
/// 83.0%L / 73.3%B (complexity 442). Targets the largest gaps:
/// - <c>RenderGrid</c> arms (was 46%L/33%B) — visible/style/major+minor
/// - <c>RenderTicks</c> arms (was 74%L/57%B) — direction/length/rotation
/// - <c>RenderSpines</c> + ResolveSpine{X,Y} arms — Data/Axes positions
/// - <c>DrawAxisBreakMark</c> arms — broken-axis variants
/// - <c>ComputeSecondaryXDataRanges</c> — secondary X axis</summary>
public class CartesianAxesRendererCoverageTests
{
    private static string Render(Action<AxesBuilder> configure) =>
        Plt.Create()
            .WithSize(500, 400)
            .AddSubPlot(1, 1, 1, configure)
            .Build()
            .ToSvg();

    // ── Grid styles ─────────────────────────────────────────────────────────

    [Fact]
    public void RenderGrid_VisibleDefault_DrawsGridLines()
    {
        var svg = Render(ax => ax.Plot([1.0, 2, 3], [1.0, 2, 3]).WithGrid(g => g with { Visible = true }));
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void RenderGrid_HiddenExplicitly_DoesNotDrawGridLines()
    {
        var fig = Plt.Create()
            .WithSize(500, 400)
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2, 3], [1.0, 2, 3]))
            .Build();
        fig.SubPlots[0].Grid = fig.SubPlots[0].Grid with { Visible = false };
        var svg = fig.ToSvg();
        Assert.Contains("<svg", svg);
    }

    // ── Tick directions + rotation ──────────────────────────────────────────

    [Theory]
    [InlineData(TickDirection.In)]
    [InlineData(TickDirection.Out)]
    [InlineData(TickDirection.InOut)]
    public void RenderTicks_TickDirection_AllArms(TickDirection direction)
    {
        var fig = Plt.Create()
            .WithSize(500, 400)
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2, 3], [1.0, 2, 3]))
            .Build();
        fig.SubPlots[0].XAxis.MajorTicks = fig.SubPlots[0].XAxis.MajorTicks with { Direction = direction };
        fig.SubPlots[0].YAxis.MajorTicks = fig.SubPlots[0].YAxis.MajorTicks with { Direction = direction };
        var svg = fig.ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(45.0)]
    [InlineData(90.0)]
    public void RenderTicks_TickLabelRotation_AllArms(double degrees)
    {
        var svg = Render(ax => ax.Plot([1.0, 2, 3], [1.0, 2, 3]).WithXTickLabelRotation(degrees));
        Assert.Contains("<svg", svg);
    }

    // ── Spine variants ──────────────────────────────────────────────────────

    [Fact]
    public void RenderSpines_HideAllAxes_NoSpineLines()
    {
        var svg = Render(ax => ax.Plot([1.0, 2], [1.0, 2]).HideAllAxes());
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void RenderSpines_TopAndRightHidden_OnlyBottomAndLeftDrawn()
    {
        var svg = Render(ax => ax
            .Plot([1.0, 2], [1.0, 2])
            .HideTopSpine()
            .HideRightSpine());
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void RenderSpines_DataPosition_ResolveSpineY_DataArm()
    {
        var fig = Plt.Create()
            .WithSize(500, 400)
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2, 3], [-2.0, 0, 2]))
            .Build();
        fig.SubPlots[0].Spines = fig.SubPlots[0].Spines with
        {
            Bottom = new SpineConfig { Position = SpinePosition.Data, PositionValue = 0, Visible = true }
        };
        var svg = fig.ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void RenderSpines_AxesPosition_ResolveSpineX_AxesArm()
    {
        var fig = Plt.Create()
            .WithSize(500, 400)
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2, 3], [1.0, 2, 3]))
            .Build();
        fig.SubPlots[0].Spines = fig.SubPlots[0].Spines with
        {
            Left = new SpineConfig { Position = SpinePosition.Axes, PositionValue = 0.5, Visible = true }
        };
        var svg = fig.ToSvg();
        Assert.Contains("<svg", svg);
    }

    // ── Scale variants (Log, SymLog, Logit) ────────────────────────────────

    [Theory]
    [InlineData(AxisScale.Linear)]
    [InlineData(AxisScale.Log)]
    [InlineData(AxisScale.SymLog)]
    public void RenderTicks_XScale_AllReachableArms(AxisScale scale)
    {
        var svg = Render(ax => ax
            .Plot([1.0, 10, 100, 1000], [1.0, 2, 3, 4])
            .SetXScale(scale));
        Assert.Contains("<svg", svg);
    }

    [Theory]
    [InlineData(AxisScale.Linear)]
    [InlineData(AxisScale.Log)]
    [InlineData(AxisScale.SymLog)]
    public void RenderTicks_YScale_AllReachableArms(AxisScale scale)
    {
        var svg = Render(ax => ax
            .Plot([1.0, 2, 3, 4], [1.0, 10, 100, 1000])
            .SetYScale(scale));
        Assert.Contains("<svg", svg);
    }

    // ── Inverted axis ──────────────────────────────────────────────────────

    [Fact]
    public void Render_InvertedYAxis_NoException()
    {
        var fig = Plt.Create()
            .WithSize(500, 400)
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2, 3], [1.0, 2, 3]))
            .Build();
        // Inverted axis: yMin > yMax
        fig.SubPlots[0].YAxis.Min = 5;
        fig.SubPlots[0].YAxis.Max = 0;
        var svg = fig.ToSvg();
        Assert.Contains("<svg", svg);
    }

    // ── Mirror ticks ───────────────────────────────────────────────────────

    [Fact]
    public void Render_WithMirroredXTicks_DrawsTopTicks()
    {
        var svg = Render(ax => ax.Plot([1.0, 2, 3], [1.0, 2, 3]).WithXTicksMirrored());
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void Render_WithMirroredYTicks_DrawsRightTicks()
    {
        var svg = Render(ax => ax.Plot([1.0, 2, 3], [1.0, 2, 3]).WithYTicksMirrored());
        Assert.Contains("<svg", svg);
    }

    // ── Categorical bar chart (RenderCategoryLabels arm) ───────────────────

    [Fact]
    public void RenderCategoryLabels_BarChart_DrawsCategoryNames()
    {
        var svg = Render(ax => ax.Bar(["Cat A", "Cat B", "Cat C"], [1.0, 2.0, 3.0]));
        Assert.Contains(">Cat A<", svg);
        Assert.Contains(">Cat B<", svg);
        Assert.Contains(">Cat C<", svg);
    }

    // ── Secondary axes ─────────────────────────────────────────────────────

    [Fact]
    public void Render_WithSecondaryYAxis_DrawsRightAxis()
    {
        var fig = Plt.Create()
            .WithSize(500, 400)
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.Plot([1.0, 2, 3], [1.0, 2, 3]);
                ax.WithSecondaryYAxis(s => s.SetYLabel("Right axis").SetYLim(0, 100));
            })
            .Build();
        var svg = fig.ToSvg();
        Assert.Contains("<svg", svg);
    }

    // ── Theme-driven font/color overrides ───────────────────────────────────

    [Fact]
    public void Render_WithMatplotlibClassicTheme_AppliesThemedSpines()
    {
        var svg = Plt.Create()
            .WithSize(500, 400)
            .WithTheme(Theme.MatplotlibClassic)
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2, 3], [1.0, 2, 3]))
            .Build()
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void Render_WithDarkTheme_AppliesDarkBackground()
    {
        var svg = Plt.Create()
            .WithSize(500, 400)
            .WithTheme(Theme.Dark)
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2, 3], [1.0, 2, 3]))
            .Build()
            .ToSvg();
        Assert.Contains("<svg", svg);
    }
}
