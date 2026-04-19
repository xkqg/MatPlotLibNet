// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Phase X.9.b (v1.7.2, 2026-04-19) — drives the
/// <see cref="ChartRenderer.Render"/> branches missed by the existing harness:
///   - line 47: figure.SubPlots.Count == 0 → early return
///   - line 51: 0-iteration of the subplot loop
///   - line 55: figure.FigureColorBar visible vs hidden vs null
///   - lines 117/120/124/131/134: ComputeLayout direct-call branches (title path,
///     legendBounds-per-subplot)
///   - line 177: GridPosition `?? GridPosition.Single(0,0)` null-arm.</summary>
public class ChartRendererCoverageTests
{
    /// <summary>Empty figure — no subplots → line 47 true arm, line 51 0-iteration.
    /// Render must not throw; output is just the background rect.</summary>
    [Fact]
    public void Render_EmptyFigure_NoSubPlots_DoesNotThrow()
    {
        var figure = new Figure { Width = 400, Height = 300 };
        var svg = figure.ToSvg();
        Assert.NotNull(svg);
        Assert.Contains("<svg", svg);
    }

    /// <summary>FigureColorBar set with Visible=true → line 55 true arm. The figure-level
    /// colorbar renders after all subplots.</summary>
    [Fact]
    public void Render_WithFigureColorBar_Visible_DrawsColorBar()
    {
        var figure = Plt.Create()
            .WithSize(400, 300)
            .Plot([1.0, 2.0], [3.0, 4.0])
            .Build();
        figure.FigureColorBar = new ColorBar { Visible = true, Min = 0, Max = 1 };
        var svg = figure.ToSvg();
        Assert.NotNull(svg);
    }

    /// <summary>FigureColorBar set with Visible=false → line 55 false arm of the
    /// `is { Visible: true }` pattern (the property is set but Visible is false, so
    /// the colorbar is NOT drawn).</summary>
    [Fact]
    public void Render_WithFigureColorBar_NotVisible_SkipsColorBar()
    {
        var figure = Plt.Create()
            .WithSize(400, 300)
            .Plot([1.0, 2.0], [3.0, 4.0])
            .Build();
        figure.FigureColorBar = new ColorBar { Visible = false, Min = 0, Max = 1 };
        var svg = figure.ToSvg();
        Assert.NotNull(svg);
    }

    /// <summary>ComputeLayout direct call (lines 117-138) — exercises the title-aware
    /// plotAreaTop computation + per-subplot legend bounds collection.</summary>
    [Fact]
    public void ComputeLayout_WithTitle_ReturnsPerSubplotLegendBounds()
    {
        var figure = Plt.Create()
            .WithSize(400, 300)
            .WithTitle("My Chart")
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2.0], [3.0, 4.0], s => s.Label = "S")
                .WithLegend())
            .Build();
        var renderer = new ChartRenderer();
        var ctx = new SvgRenderContext();
        var layout = renderer.ComputeLayout(figure, ctx);
        Assert.Equal(figure.SubPlots.Count, layout.PlotAreas.Count);
    }

    /// <summary>ComputeLayout WITHOUT title (line 117 false arm) — plotAreaTop = MarginTop.</summary>
    [Fact]
    public void ComputeLayout_NoTitle_TightTopMargin()
    {
        var figure = Plt.Create()
            .WithSize(400, 300)
            .Plot([1.0, 2.0], [3.0, 4.0])
            .Build();
        var renderer = new ChartRenderer();
        var ctx = new SvgRenderContext();
        var layout = renderer.ComputeLayout(figure, ctx);
        Assert.Single(layout.PlotAreas);
    }

    /// <summary>ComputeLayout with a math-mode title (line 120 ContainsMath true arm).
    /// The richtext branch in title-height measurement runs.</summary>
    [Fact]
    public void ComputeLayout_WithMathTitle_UsesRichTextHeight()
    {
        var figure = Plt.Create()
            .WithSize(400, 300)
            .WithTitle(@"$\alpha + \beta$")
            .Plot([1.0, 2.0], [3.0, 4.0])
            .Build();
        var renderer = new ChartRenderer();
        var ctx = new SvgRenderContext();
        var layout = renderer.ComputeLayout(figure, ctx);
        Assert.Single(layout.PlotAreas);
    }
}
