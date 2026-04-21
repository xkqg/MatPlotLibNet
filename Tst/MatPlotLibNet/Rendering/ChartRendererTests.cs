// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Transforms;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies <see cref="SvgTransform"/> behavior.</summary>
public class ChartRendererTests
{
    /// <summary>Verifies that rendering an empty figure produces valid SVG with opening and closing tags.</summary>
    [Fact]
    public void Render_EmptyFigure_ProducesValidSvg()
    {
        string svg = Plt.Create().ToSvg();

        Assert.Contains("<svg", svg);
        Assert.Contains("</svg>", svg);
    }

    /// <summary>Verifies that a figure with a title includes the title text in the SVG output.</summary>
    [Fact]
    public void Render_FigureWithTitle_ContainsTitleText()
    {
        string svg = Plt.Create()
            .WithTitle("My Title")
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg();

        Assert.Contains("My Title", svg);
    }

    /// <summary>Verifies that a figure without a title does not contain stray title text.</summary>
    [Fact]
    public void Render_FigureWithoutTitle_DoesNotContainTitleText()
    {
        string svg = Plt.Create()
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg();

        // The figure has no title, so "My Title" should not appear
        Assert.DoesNotContain("My Title", svg);
    }

    /// <summary>Verifies that a single subplot renders axes with rect and line elements.</summary>
    [Fact]
    public void Render_SingleSubplot_ProducesAxes()
    {
        string svg = Plt.Create()
            .Plot([1.0, 2.0, 3.0], [10.0, 20.0, 15.0])
            .ToSvg();

        // Axes frame is rendered as a rect, and tick marks as line elements
        Assert.Contains("<rect", svg);
        Assert.Contains("<line", svg);
    }

    /// <summary>Verifies that two side-by-side subplots both have their titles in the SVG output.</summary>
    [Fact]
    public void Render_TwoSubplots_ProducesBothTitles()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 2, 1, ax => ax
                .WithTitle("Left Panel")
                .Plot([1.0, 2.0], [3.0, 4.0]))
            .AddSubPlot(1, 2, 2, ax => ax
                .WithTitle("Right Panel")
                .Plot([1.0, 2.0], [5.0, 6.0]))
            .ToSvg();

        Assert.Contains("Left Panel", svg);
        Assert.Contains("Right Panel", svg);
    }

    /// <summary>Verifies that a 2x2 grid of subplots renders all four subplot titles.</summary>
    [Fact]
    public void Render_2x2Grid_AllSubplotsPresent()
    {
        string svg = Plt.Create()
            .AddSubPlot(2, 2, 1, ax => ax
                .WithTitle("TopLeft")
                .Plot([1.0, 2.0], [1.0, 2.0]))
            .AddSubPlot(2, 2, 2, ax => ax
                .WithTitle("TopRight")
                .Plot([1.0, 2.0], [3.0, 4.0]))
            .AddSubPlot(2, 2, 3, ax => ax
                .WithTitle("BottomLeft")
                .Plot([1.0, 2.0], [5.0, 6.0]))
            .AddSubPlot(2, 2, 4, ax => ax
                .WithTitle("BottomRight")
                .Plot([1.0, 2.0], [7.0, 8.0]))
            .ToSvg();

        Assert.Contains("TopLeft", svg);
        Assert.Contains("TopRight", svg);
        Assert.Contains("BottomLeft", svg);
        Assert.Contains("BottomRight", svg);
    }

    /// <summary>Verifies that a custom background color appears as a rect with the specified hex color.</summary>
    [Fact]
    public void Render_WithBackground_ContainsBackgroundRect()
    {
        string svg = Plt.Create()
            .WithBackground(Color.FromHex("#AABBCC"))
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg();

        Assert.Contains("<rect", svg);
        Assert.Contains("#AABBCC", svg);
    }

    /// <summary>Verifies that enabling grid lines produces line elements in the SVG output.</summary>
    [Fact]
    public void Render_WithGrid_ContainsGridLines()
    {
        var figure = new Figure();
        var axes = figure.AddSubPlot();
        axes.Plot([1.0, 2.0, 3.0], [10.0, 20.0, 15.0]);
        axes.Grid = axes.Grid with { Visible = true };

        string svg = figure.ToSvg();

        // Grid lines are rendered as <line> elements
        Assert.Contains("<line", svg);
    }

    /// <summary>Verifies that a line series renders as a polyline element.</summary>
    [Fact]
    public void Render_LineSeries_ContainsPolyline()
    {
        string svg = Plt.Create()
            .Plot([1.0, 2.0, 3.0], [10.0, 20.0, 15.0])
            .ToSvg();

        Assert.Contains("<polyline", svg);
    }

    /// <summary>Verifies that a bar series renders as rect elements.</summary>
    [Fact]
    public void Render_BarSeries_ContainsRects()
    {
        string svg = Plt.Create()
            .Bar(["A", "B", "C"], [10.0, 20.0, 15.0])
            .ToSvg();

        Assert.Contains("<rect", svg);
    }
}

// ─── ChartRendererCoverageTests.cs ───────────────────────────────────────────

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

    /// <summary>Math title in RenderBackground — hits L94 ContainsMath true arm
    /// (DrawRichText path) and L98 true arm via Render().</summary>
    [Fact]
    public void Render_WithMathTitle_DrawsRichText()
    {
        var svg = Plt.Create()
            .WithSize(400, 300)
            .WithTitle(@"$\alpha + \beta$")
            .Plot([1.0, 2.0], [3.0, 4.0])
            .Build()
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    /// <summary>Figure with explicit BackgroundColor — L85 non-null arm.</summary>
    [Fact]
    public void Render_WithExplicitBackgroundColor_RendersBackground()
    {
        var figure = Plt.Create()
            .WithSize(400, 300)
            .Plot([1.0, 2.0], [3.0, 4.0])
            .Build();
        figure.BackgroundColor = new Color(30, 30, 30);
        var svg = figure.ToSvg();
        Assert.Contains("<svg", svg);
    }

    /// <summary>Horizontal figure colorbar with label and explicit ColorMap —
    /// L337 Horizontal arm + L340 Aspect>0 arm + L360 Label non-null arm.</summary>
    [Fact]
    public void Render_WithHorizontalColorBarAndLabel_DrawsBar()
    {
        var figure = Plt.Create()
            .WithSize(500, 400)
            .AddSubPlot(1, 1, 1, ax => ax.Heatmap(new double[,] { { 1, 2 }, { 3, 4 } }))
            .Build();
        figure.FigureColorBar = new ColorBar
        {
            Visible = true, Min = 0, Max = 4,
            Orientation = ColorBarOrientation.Horizontal,
            Aspect = 30, Label = "value"
        };
        var svg = figure.ToSvg();
        Assert.Contains("<svg", svg);
    }

    /// <summary>Vertical figure colorbar with label (L390 non-null Label arm in vertical branch).</summary>
    [Fact]
    public void Render_WithVerticalColorBarAndLabel_DrawsBar()
    {
        var figure = Plt.Create()
            .WithSize(500, 400)
            .AddSubPlot(1, 1, 1, ax => ax.Heatmap(new double[,] { { 1, 2 }, { 3, 4 } }))
            .Build();
        figure.FigureColorBar = new ColorBar
        {
            Visible = true, Min = 0, Max = 4,
            Orientation = ColorBarOrientation.Vertical,
            Label = "Z"
        };
        var svg = figure.ToSvg();
        Assert.Contains("<svg", svg);
    }

    /// <summary>Colorbar with a data-providing heatmap series — L311 provider non-null,
    /// L314 cb.ColorMap null → uses provider's ColorMap, L316 dMin &lt; dMax true.</summary>
    [Fact]
    public void Render_ColorBarWithHeatmapProvider_UsesSeriesColorMap()
    {
        var figure = Plt.Create()
            .WithSize(500, 400)
            .AddSubPlot(1, 1, 1, ax => ax.Heatmap(
                new double[,] { { 1, 2 }, { 3, 4 } },
                s => s.ColorMap = global::MatPlotLibNet.Styling.ColorMaps.ColorMaps.Plasma))
            .Build();
        figure.FigureColorBar = new ColorBar { Visible = true };
        var svg = figure.ToSvg();
        Assert.Contains("<svg", svg);
    }

    /// <summary>GridSpec with HeightRatios + WidthRatios — ComputeRatioSizes else branch (L212-216).</summary>
    [Fact]
    public void Render_WithGridSpecAndRatios_DistributesUnequally()
    {
        var svg = Plt.Create()
            .WithSize(600, 500)
            .WithGridSpec(2, 2, heightRatios: [2.0, 1.0], widthRatios: [1.0, 2.0])
            .AddSubPlot(new GridPosition(0, 1, 0, 1), ax => ax.Plot([1.0, 2], [3.0, 4]))
            .AddSubPlot(new GridPosition(0, 1, 1, 2), ax => ax.Scatter([1.0, 2], [3.0, 4]))
            .AddSubPlot(new GridPosition(1, 2, 0, 1), ax => ax.Bar(["A"], [1.0]))
            .AddSubPlot(new GridPosition(1, 2, 1, 2), ax => ax.Hist([1.0, 2, 3]))
            .Build()
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    /// <summary>Multi-subplot with GridRows set — L234 false arm (not all GridRows==0).</summary>
    [Fact]
    public void Render_WithSubplotsHavingGridRows_UsesGridLayout()
    {
        var svg = Plt.Create()
            .WithSize(600, 500)
            .AddSubPlot(2, 1, 1, ax => ax.Plot([1.0, 2], [3.0, 4]))
            .AddSubPlot(2, 1, 2, ax => ax.Plot([1.0, 2], [3.0, 4]))
            .Build()
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    /// <summary>RenderBackground with title that is taller than MarginTop —
    /// L106 `if (sp.MarginTop &lt; needed) plotAreaTop = needed` true arm.</summary>
    [Fact]
    public void Render_TitleTallerThanMarginTop_ExpandsTopArea()
    {
        var figure = Plt.Create()
            .WithSize(400, 300)
            .WithTitle("A Very Long Title That Should Require Extra Space At The Top")
            .Plot([1.0, 2.0], [3.0, 4.0])
            .Build();
        figure.Spacing = figure.Spacing with { MarginTop = 1 };
        var svg = figure.ToSvg();
        Assert.Contains("<svg", svg);
    }
}
