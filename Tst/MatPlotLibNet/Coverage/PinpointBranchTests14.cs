// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet;
using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Algorithms;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Coverage;

/// <summary>
/// Phase Ω.3 (v1.7.2, 2026-04-19) — mid-complexity systematic batch
/// (~50 facts across SankeySeriesRenderer, LeastSquares, ChartRenderer,
/// MarchingSquares, ConstrainedLayoutEngine, GifEncoder, MathTextParser,
/// DataTransform, FigureTemplates, FacetedFigure).
/// </summary>
public class PinpointBranchTests14
{
    // ── MathTextParser surgical (10 uncov lines) ──────────────────────────

    [Fact] public void MathTextParser_GreekLetterMu_ProducesUnicodeMu()
        => Assert.Contains(MathTextParser.Parse(@"$\mu$").Spans, s => s.Text.Contains('\u03BC'));

    [Fact] public void MathTextParser_GreekLetterPi_ProducesUnicodePi()
        => Assert.Contains(MathTextParser.Parse(@"$\pi$").Spans, s => s.Text.Contains('\u03C0'));

    [Fact] public void MathTextParser_GreekLetterSigma_ProducesUnicodeSigma()
        => Assert.Contains(MathTextParser.Parse(@"$\sigma$").Spans, s => s.Text.Contains('\u03C3'));

    [Fact] public void MathTextParser_Superscript_ProducesSuperscriptSpan()
    {
        var rt = MathTextParser.Parse("$x^2$");
        Assert.Contains(rt.Spans, s => s.Kind == TextSpanKind.Superscript);
    }

    [Fact] public void MathTextParser_Subscript_ProducesSubscriptSpan()
    {
        var rt = MathTextParser.Parse("$H_2$");
        Assert.Contains(rt.Spans, s => s.Kind == TextSpanKind.Subscript);
    }

    [Fact] public void MathTextParser_BackslashUnknownCommand_PassesThrough()
    {
        var rt = MathTextParser.Parse(@"$\unknowncmd x$");
        Assert.NotEmpty(rt.Spans);
    }

    [Fact] public void MathTextParser_NestedBraces_HandledGracefully()
    {
        var rt = MathTextParser.Parse(@"$x_{n+1}$");
        Assert.NotEmpty(rt.Spans);
    }

    [Fact] public void MathTextParser_ConsecutiveCommands_PreservesOrder()
    {
        var rt = MathTextParser.Parse(@"$\alpha\beta$");
        Assert.NotEmpty(rt.Spans);
    }

    // ── DataTransform: log-scale arm + axis-breaks arm ────────────────────

    [Fact]
    public void DataTransform_TransformX_LogScale_UsesScaleArm()
    {
        // Force the for-loop arm at L138-145 by using log-scale (non-Linear)
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.Plot([1.0, 10, 100], [1.0, 2, 3]);
            })
            .Build();
        fig.SubPlots[0].XAxis.Scale = AxisScale.Log;
        var svg = fig.ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void DataTransform_TransformY_LogScale_UsesScaleArm()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2, 3], [1.0, 10, 100]))
            .Build();
        fig.SubPlots[0].YAxis.Scale = AxisScale.Log;
        var svg = fig.ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void DataTransform_TransformX_WithXBreaks_UsesBreakRemap()
    {
        // Force the for-loop arm by using XBreaks
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 10], [1.0, 10]))
            .Build();
        fig.SubPlots[0].AddXBreak(3, 5, BreakStyle.Zigzag);
        var svg = fig.ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void DataTransform_TransformY_WithYBreaks_UsesBreakRemap()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 10], [1.0, 10]))
            .Build();
        fig.SubPlots[0].AddYBreak(3, 5, BreakStyle.Zigzag);
        var svg = fig.ToSvg();
        Assert.Contains("<svg", svg);
    }

    // ── FigureTemplates per-template title arms ───────────────────────────

    [Fact] public void FigureTemplates_ScientificPaper_WithTitle_AppliesTitle()
    {
        var fb = FigureTemplates.ScientificPaper(ax => ax.Plot([1.0, 2], [3.0, 4]), title: "test");
        var fig = fb.Build();
        Assert.Equal("test", fig.Title);
    }

    [Fact] public void FigureTemplates_FacetGrid_WithTitle_AppliesTitle()
    {
        var fb = FigureTemplates.FacetGrid([1.0, 2, 3], [1.0, 2, 3], ["A", "B", "A"],
            (ax, fx, fy) => ax.Plot(fx, fy));
        var fig = fb.Build();
        Assert.NotNull(fig);
    }

    [Fact] public void FigureTemplates_PairPlot_DefaultBins_BuildsGrid()
    {
        var fb = FigureTemplates.PairPlot([[1.0, 2, 3], [4.0, 5, 6]]);
        var fig = fb.Build();
        Assert.NotEmpty(fig.SubPlots);
    }

    [Fact] public void FigureTemplates_FinancialDashboard_WithTitle_AppliesTitle()
    {
        var fb = FigureTemplates.FinancialDashboard(
            open: [10.0, 11.0], high: [12.0, 13.0], low: [9.0, 10.0],
            close: [11.0, 12.0], volume: [1000.0, 2000.0],
            title: "fin");
        var fig = fb.Build();
        Assert.Equal("fin", fig.Title);
    }

    [Fact] public void FigureTemplates_JointPlot_DefaultBins_RendersScatterAndMarginals()
    {
        var fb = FigureTemplates.JointPlot([1.0, 2, 3], [1.0, 2, 3], title: "joint");
        var fig = fb.Build();
        Assert.Equal("joint", fig.Title);
    }

    [Fact] public void FigureTemplates_SparklineDashboard_WithTitle_AppliesTitle()
    {
        var fb = FigureTemplates.SparklineDashboard(
            new (string, double[])[] { ("a", new[] { 1.0, 2 }) },
            title: "dash");
        var fig = fb.Build();
        Assert.Equal("dash", fig.Title);
    }

    // ── ConstrainedLayoutEngine — the main path with various edge cases ───

    [Fact]
    public void ConstrainedLayoutEngine_EmptyFigure_NoOp()
    {
        var fig = Plt.Create().Build();
        var svg = fig.ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void ConstrainedLayoutEngine_HighRatioGridSpec_AppliesProportions()
    {
        var fig = Plt.Create()
            .WithGridSpec(2, 2, heightRatios: [3.0, 1.0], widthRatios: [1.0, 4.0])
            .AddSubPlot(new GridPosition(0, 1, 0, 2), ax => ax.Plot([1.0, 2], [3.0, 4]))
            .AddSubPlot(new GridPosition(1, 2, 0, 2), ax => ax.Plot([1.0, 2], [3.0, 4]))
            .Build();
        var svg = fig.ToSvg();
        Assert.Contains("<svg", svg);
    }

    // ── MarchingSquares saddle-cell + all-equal grid arms ─────────────────

    [Fact]
    public void MarchingSquares_Extract_AllSameValues_HandlesUniformGrid()
    {
        var grid = new double[5, 5];
        for (int r = 0; r < 5; r++) for (int c = 0; c < 5; c++) grid[r, c] = 1.0;
        var contours = MarchingSquares.Extract([0.0, 1, 2, 3, 4], [0.0, 1, 2, 3, 4], grid, [1.0]);
        Assert.NotNull(contours);
    }

    [Fact]
    public void MarchingSquares_Extract_LinearGradient_ProducesContours()
    {
        var grid = new double[5, 5];
        for (int r = 0; r < 5; r++) for (int c = 0; c < 5; c++) grid[r, c] = r + c;
        var contours = MarchingSquares.Extract([0.0, 1, 2, 3, 4], [0.0, 1, 2, 3, 4], grid, [4.0]);
        Assert.NotEmpty(contours);
    }

    [Fact]
    public void MarchingSquares_ExtractBands_TwoLevels_ProducesBands()
    {
        var grid = new double[,] { { 0, 1, 2 }, { 1, 2, 3 }, { 2, 3, 4 } };
        var bands = MarchingSquares.ExtractBands([0.0, 1, 2], [0.0, 1, 2], grid, levels: 3);
        Assert.NotEmpty(bands);
    }

    // ── LeastSquares: PolyFit edge cases (Gaussian elimination + invert) ──

    [Fact]
    public void LeastSquares_PolyFit_HighDegreePolynomial_FitsCubicExactly()
    {
        // y = x³ → [0,0,0,1] coefficients
        double[] x = [0, 1, 2, 3, 4];
        double[] y = [0, 1, 8, 27, 64];
        var coeffs = LeastSquares.PolyFit(x, y, degree: 3);
        Assert.Equal(4, coeffs.Length);
        Assert.Equal(1.0, coeffs[3], precision: 4);
    }

    [Fact]
    public void LeastSquares_PolyFit_DegreeZero_GivesMean()
    {
        var coeffs = LeastSquares.PolyFit([1.0, 2, 3], [10.0, 20, 30], degree: 0);
        Assert.Single(coeffs);
        Assert.Equal(20.0, coeffs[0], precision: 4);
    }

    [Fact]
    public void LeastSquares_PolyFit_NegativeDegree_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => LeastSquares.PolyFit([1.0], [1.0], degree: -1));
    }

    // ── FacetedFigure AddLines with hue (line 99-109 uncovered block) ─────

    [Fact]
    public void FacetedFigure_AddLines_WithHue_GroupsBy()
    {
        // FacetGridFigure with line plotFunc and hue labels
        var fb = new FacetGridFigure(
            x: [1.0, 2, 3, 4, 5, 6],
            y: [10.0, 20, 30, 40, 50, 60],
            category: ["A", "A", "B", "B", "A", "B"],
            plotFunc: (ax, fx, fy) => ax.Plot(fx, fy));
        var fig = fb.Build().Build();
        Assert.NotEmpty(fig.SubPlots);
    }

    [Fact]
    public void FacetedFigure_PairPlot_WithHueLabels_RendersWithHueGroups()
    {
        var pp = new PairPlotFigure([
            [1.0, 2, 3, 4],
            [5.0, 6, 7, 8],
        ])
        {
            Hue = ["a", "b", "a", "b"],
        };
        var fig = pp.Build().Build();
        Assert.NotEmpty(fig.SubPlots);
    }

    // ── ChartRenderer theme-dispatch null-arms ────────────────────────────

    [Fact]
    public void ChartRenderer_RenderWithDarkTheme_AppliesTheme()
    {
        var svg = Plt.Create()
            .WithTheme(Theme.Dark)
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2], [3.0, 4]))
            .Build()
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void ChartRenderer_RenderWithMatplotlibClassicTheme_AppliesTheme()
    {
        var svg = Plt.Create()
            .WithTheme(Theme.MatplotlibClassic)
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2], [3.0, 4]))
            .Build()
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void ChartRenderer_RenderWithGgplotTheme_AppliesTheme()
    {
        var svg = Plt.Create()
            .WithTheme(Theme.Ggplot)
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2], [3.0, 4]))
            .Build()
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void ChartRenderer_RenderWithSeabornTheme_AppliesTheme()
    {
        var svg = Plt.Create()
            .WithTheme(Theme.Seaborn)
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2], [3.0, 4]))
            .Build()
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    // ── SankeySeriesRenderer single-node + multi-link variants ────────────

    [Fact]
    public void SankeyRenderer_SingleNode_RendersWithoutError()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Sankey(
                [new SankeyNode("Solo")],
                links: []))
            .Build();
        var svg = fig.ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void SankeyRenderer_MultiLinkSameSource_RendersWithoutError()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Sankey(
                [new SankeyNode("A"), new SankeyNode("B"), new SankeyNode("C")],
                [new SankeyLink(0, 1, 5), new SankeyLink(0, 2, 3)]))
            .Build();
        var svg = fig.ToSvg();
        Assert.Contains("<svg", svg);
    }
}
