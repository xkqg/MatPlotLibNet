// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>v1.11.0 — Verifies SVG output of <see cref="RelativeRotationSeries"/> rendering.</summary>
public class RelativeRotationRenderTests
{
    private static double[] Flat(int n, double v = 100.0) => Enumerable.Repeat(v, n).ToArray();
    private static double[] Rising(int n, double start = 100.0, double step = 1.0) =>
        Enumerable.Range(0, n).Select(i => start + i * step).ToArray();

    private static int CountOccurrences(string haystack, string needle)
    {
        int count = 0, idx = 0;
        while ((idx = haystack.IndexOf(needle, idx, StringComparison.Ordinal)) >= 0)
        { count++; idx += needle.Length; }
        return count;
    }

    private static string RenderSvg(Action<RelativeRotationSeries>? configure = null)
    {
        // 60 bars: enough for DualEma(10,26) to produce valid momentum
        // (rsRatioValid = 60-25 = 35 ≥ longP=26, so the second EMA pass succeeds).
        var assetCloses   = new[] { Rising(60), Flat(60, 95) };
        var benchClose    = Flat(60);
        var labels        = new[] { "ETH", "BNB" };
        return Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.RelativeRotation(assetCloses, benchClose, labels, configure))
            .ToSvg();
    }

    // ── Smoke tests ──────────────────────────────────────────────────────────

    [Fact]
    public void SimpleRrg_RendersValidSvg()
    {
        string svg = RenderSvg();
        Assert.Contains("<svg", svg);
        Assert.Contains("</svg>", svg);
    }

    [Fact]
    public void EmptyAssets_RendersValidSvg()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.RelativeRotation([], [], []))
            .ToSvg();
        Assert.Contains("<svg", svg);
        Assert.Contains("</svg>", svg);
    }

    // ── Quadrant grid ────────────────────────────────────────────────────────

    [Fact]
    public void ShowQuadrantGrid_True_EmitsRectElements()
    {
        // Four quadrant fills → at least 4 <rect> elements beyond the plot frame.
        string svgOn  = RenderSvg(s => s.ShowQuadrantGrid = true);
        string svgOff = RenderSvg(s => s.ShowQuadrantGrid = false);
        int rectsOn  = CountOccurrences(svgOn,  "<rect ");
        int rectsOff = CountOccurrences(svgOff, "<rect ");
        Assert.True(rectsOn > rectsOff,
            $"ShowQuadrantGrid=true ({rectsOn} rects) must exceed off ({rectsOff}).");
    }

    [Fact]
    public void ShowQuadrantGrid_True_EmitsCrossairLines()
    {
        // Crosshair = 2 dashed lines (horizontal + vertical through 100/100).
        string svgOn  = RenderSvg(s => s.ShowQuadrantGrid = true);
        string svgOff = RenderSvg(s => s.ShowQuadrantGrid = false);
        int linesOn  = CountOccurrences(svgOn,  "<line ");
        int linesOff = CountOccurrences(svgOff, "<line ");
        Assert.True(linesOn > linesOff,
            $"ShowQuadrantGrid=true ({linesOn} lines) must exceed off ({linesOff}).");
    }

    // ── Asset heads ──────────────────────────────────────────────────────────

    [Fact]
    public void Render_EmitsCirclePerAssetHead()
    {
        // 2 assets → at least 2 <circle> elements for the asset heads.
        string svg = RenderSvg();
        int circles = CountOccurrences(svg, "<circle ");
        Assert.True(circles >= 2, $"Expected ≥2 circles for 2 assets, got {circles}.");
    }

    // ── Tail polylines ───────────────────────────────────────────────────────

    [Fact]
    public void Render_EmitsPolylineElementsForTails()
    {
        // Each asset with a valid tail emits at least 1 <polyline> or <line> for segments.
        string svg = RenderSvg(s => { s.ShortPeriod = 3; s.LongPeriod = 5; s.TailLength = 4; });
        bool hasTailElements = svg.Contains("<polyline ") || svg.Contains("<line ");
        Assert.True(hasTailElements, "Expected polyline or line elements for tails.");
    }

    [Fact]
    public void ShowQuadrantGrid_False_SameCircleCount()
    {
        // Toggling the quadrant grid should not add/remove asset head circles.
        string svgOn  = RenderSvg(s => s.ShowQuadrantGrid = true);
        string svgOff = RenderSvg(s => s.ShowQuadrantGrid = false);
        Assert.Equal(CountOccurrences(svgOn, "<circle "), CountOccurrences(svgOff, "<circle "));
    }

    // ── Asset labels ─────────────────────────────────────────────────────────

    [Fact]
    public void Render_EmitsAssetLabelsAsText()
    {
        string svg = RenderSvg();
        Assert.Contains("ETH", svg);
        Assert.Contains("BNB", svg);
    }

    // ── ZScore formula ───────────────────────────────────────────────────────

    [Fact]
    public void ZScoreFormula_RendersValidSvg()
    {
        string svg = RenderSvg(s => { s.Formula = RrgFormula.ZScore; s.ShortPeriod = 5; });
        Assert.Contains("<svg", svg);
    }

    // ── LogReturn formula ─────────────────────────────────────────────────────

    [Fact]
    public void LogReturnFormula_RendersValidSvg()
    {
        string svg = RenderSvg(s => { s.Formula = RrgFormula.LogReturn; s.ShortPeriod = 5; s.LongPeriod = 10; });
        Assert.Contains("<svg", svg);
    }

    // ── Single-asset branch ───────────────────────────────────────────────────

    [Fact]
    public void SingleAsset_RendersValidSvg()
    {
        // Covers the rsData.Length > 1 ? ... : 0.0 false branch (hue = 0.0 for single asset).
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax =>
                ax.RelativeRotation([Rising(60)], Flat(60), ["ETH"]))
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    // ── All-NaN tail branch ───────────────────────────────────────────────────

    [Fact]
    public void AllNaNTail_DoesNotCrash()
    {
        // Periods larger than data → all rsRatio + rsMomentum NaN → DrawAsset returns
        // early at the points.Count == 0 guard without rendering any circles.
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax =>
                ax.RelativeRotation([Rising(30), Flat(30, 95)], Flat(30), ["ETH", "BNB"], s =>
                {
                    s.ShortPeriod = 25;
                    s.LongPeriod  = 50;  // > data length → EMA returns empty → all NaN
                }))
            .ToSvg();
        Assert.Contains("<svg", svg);
        Assert.DoesNotContain("<circle ", svg);  // no heads drawn
    }

    // ── Absorption overlay ────────────────────────────────────────────────────

    [Fact]
    public void AbsorptionOverlay_EmitsCirclePerTailPoint()
    {
        // Without overlay: 1 circle per asset (head only).
        // With AbsorptionRatioPerBar: one circle per tail point per asset.
        const int TailLen   = 4;
        const int NumAssets = 2;
        var absorption = Enumerable.Repeat(0.4, 60).ToArray();

        string svgOverlay = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.RelativeRotation(
                [Rising(60), Flat(60, 95)], Flat(60), ["ETH", "BNB"], s =>
                {
                    s.ShortPeriod           = 3;
                    s.LongPeriod            = 5;
                    s.TailLength            = TailLen;
                    s.AbsorptionRatioPerBar = absorption;
                }))
            .ToSvg();

        string svgBase = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.RelativeRotation(
                [Rising(60), Flat(60, 95)], Flat(60), ["ETH", "BNB"], s =>
                {
                    s.ShortPeriod = 3;
                    s.LongPeriod  = 5;
                    s.TailLength  = TailLen;
                }))
            .ToSvg();

        int circlesOverlay = CountOccurrences(svgOverlay, "<circle ");
        int circlesBase    = CountOccurrences(svgBase,    "<circle ");

        Assert.True(circlesOverlay > circlesBase,
            $"Absorption overlay ({circlesOverlay} circles) must exceed baseline ({circlesBase}).");
        Assert.Equal(TailLen * NumAssets, circlesOverlay);
    }

    [Fact]
    public void EnbOverlay_EmitsCirclePerTailPoint()
    {
        const int TailLen   = 4;
        const int NumAssets = 2;
        var enb = Enumerable.Repeat(3.0, 60).ToArray();

        string svgOverlay = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.RelativeRotation(
                [Rising(60), Flat(60, 95)], Flat(60), ["ETH", "BNB"], s =>
                {
                    s.ShortPeriod = 3;
                    s.LongPeriod  = 5;
                    s.TailLength  = TailLen;
                    s.EnbPerBar   = enb;
                }))
            .ToSvg();

        string svgBase = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.RelativeRotation(
                [Rising(60), Flat(60, 95)], Flat(60), ["ETH", "BNB"], s =>
                {
                    s.ShortPeriod = 3;
                    s.LongPeriod  = 5;
                    s.TailLength  = TailLen;
                }))
            .ToSvg();

        int circlesOverlay = CountOccurrences(svgOverlay, "<circle ");
        int circlesBase    = CountOccurrences(svgBase,    "<circle ");

        Assert.True(circlesOverlay > circlesBase,
            $"ENB overlay ({circlesOverlay} circles) must exceed baseline ({circlesBase}).");
        Assert.Equal(TailLen * NumAssets, circlesOverlay);
    }

    [Fact]
    public void BothOverlays_EmitsCirclePerTailPoint()
    {
        const int TailLen   = 4;
        const int NumAssets = 2;

        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.RelativeRotation(
                [Rising(60), Flat(60, 95)], Flat(60), ["ETH", "BNB"], s =>
                {
                    s.ShortPeriod           = 3;
                    s.LongPeriod            = 5;
                    s.TailLength            = TailLen;
                    s.AbsorptionRatioPerBar = Enumerable.Repeat(0.5, 60).ToArray();
                    s.EnbPerBar             = Enumerable.Repeat(2.5, 60).ToArray();
                }))
            .ToSvg();

        Assert.Equal(TailLen * NumAssets, CountOccurrences(svg, "<circle "));
    }

    // ── Fluent API coverage ────────────────────────────────────────────────────

    [Fact]
    public void Axes_RelativeRotation_WithConfigure_InvokesCallback()
    {
        // Directly call Axes.RelativeRotation with a non-null configure to cover
        // the configure?.Invoke(series) non-null branch (unreachable via AxesBuilder).
        var figure = Plt.Create().AddSubPlot(1, 1, 1, _ => { }).Build();
        var axes   = figure.SubPlots[0];
        var s = axes.RelativeRotation(
            [Rising(60), Flat(60, 95)], Flat(60), ["ETH", "BNB"],
            configure: series => series.Formula = RrgFormula.ZScore);
        Assert.Equal(RrgFormula.ZScore, s.Formula);
    }

    [Fact]
    public void FigureBuilder_RelativeRotation_BuildsWithoutException()
    {
        // Exercises FigureBuilder.RelativeRotation so the method body is line-covered.
        var figure = Plt.Create()
            .RelativeRotation([Rising(60)], Flat(60), ["ETH"])
            .Build();
        Assert.NotNull(figure);
    }
}
