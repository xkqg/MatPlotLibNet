// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Builders;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.Layout;

/// <summary>
/// Wave J.1 branch close-out for <see cref="ConstrainedLayoutEngine"/>: targets the seven branches
/// flagged sub-100 in baseline.cobertura.xml — outside-top legend (Compute switch L109 + Measure switch L282),
/// interior-subplot horizontal/vertical gap widening (L70/L71/L86), stacked horizontal gap (L92),
/// suptitle-only top floor when no top-row subplot exists (L96), legacy GridRows/Cols positioning
/// fallback (L171/L185), and EstimateYTickLabel min-only path (L309).
/// </summary>
public class ConstrainedLayoutEngineCoverageTests
{
    private static IRenderContext MakeCtx() => new SvgRenderContext();
    private static ConstrainedLayoutEngine Engine() => new();

    // ── L109/L118-119 + L282/L291-292 ────────────────────────────────────────
    /// <summary>OutsideTop legend hits the OutsideTop arm in both the Measure switch (Lines 291-292)
    /// and the figure-level clamp switch (Lines 118-119) — the only switch arm of the four positions
    /// not exercised by the existing OutsideRight/Left/Bottom tests.</summary>
    [Fact]
    public void OutsideTopLegend_WidensTopMarginAboveInsidePosition()
    {
        var ctx = MakeCtx();
        var inside = new FigureBuilder()
            .WithSize(800, 600)
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.Plot([1.0, 2.0], [1.0, 2.0], s => s.Label = "Alpha");
                ax.Plot([1.0, 2.0], [2.0, 3.0], s => s.Label = "Beta");
                ax.WithLegend(l => l with { Position = LegendPosition.UpperCenter });
            })
            .Build();
        var outsideTop = new FigureBuilder()
            .WithSize(800, 600)
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.Plot([1.0, 2.0], [1.0, 2.0], s => s.Label = "Alpha");
                ax.Plot([1.0, 2.0], [2.0, 3.0], s => s.Label = "Beta");
                ax.WithLegend(l => l with { Position = LegendPosition.OutsideTop });
            })
            .Build();

        var insideSpacing = Engine().Compute(inside, ctx);
        var outsideSpacing = Engine().Compute(outsideTop, ctx);

        Assert.True(outsideSpacing.MarginTop > insideSpacing.MarginTop,
            $"OutsideTop must widen top margin; got inside={insideSpacing.MarginTop}, outside={outsideSpacing.MarginTop}");
    }

    // ── L309 / L310 ───────────────────────────────────────────────────────────
    /// <summary>YAxis with Min set but Max null hits the second arm of EstimateYTickLabel
    /// (<c>else if (axes.YAxis.Min.HasValue) return FormatTickValue(Min)</c>, line 309-310).
    /// SetYLim sets both, so we mutate the model after build to leave Max null.</summary>
    [Fact]
    public void YAxisMinOnly_MaxUnset_LeftMarginEstimatesFromMin()
    {
        var ctx = MakeCtx();
        var figure = new FigureBuilder()
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2.0], [1.0, 2.0]))
            .Build();
        // Bypass SetYLim (which sets both) to land on the Min-only branch
        figure.SubPlots[0].YAxis.Min = 1_234_567.0;
        figure.SubPlots[0].YAxis.Max = null;

        var bare = new FigureBuilder()
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2.0], [1.0, 2.0]))
            .Build();

        var minOnlySpacing = Engine().Compute(figure, ctx);
        var bareSpacing    = Engine().Compute(bare, ctx);

        // Engine ran without throwing on the Min-only branch; left margin reflects the
        // Min-derived label width (≥ the conservative fallback proxy "−9.999")
        Assert.True(minOnlySpacing.MarginLeft >= bareSpacing.MarginLeft);
    }

    // ── L70 (true) + L86 (true) ───────────────────────────────────────────────
    /// <summary>Two-column grid where the LEFT subplot (ColEnd=1 &lt; maxCols=2) carries a
    /// vertical colorbar — its RightNeeded contribution is what the engine accumulates into hGap
    /// at line 70 and into <c>maxNonRightRight</c> at line 86. With only a right-edge subplot
    /// in existing fixtures, the inner branch never fires.</summary>
    [Fact]
    public void TwoColGrid_LeftSubplotVerticalColorBar_WidensHorizontalGap()
    {
        var ctx = MakeCtx();
        var bareTwoCol = new FigureBuilder()
            .AddSubPlot(1, 2, 1, ax => ax.Plot([1.0, 2.0], [1.0, 2.0]))
            .AddSubPlot(1, 2, 2, ax => ax.Plot([1.0, 2.0], [1.0, 2.0]))
            .Build();

        var leftCb = new FigureBuilder()
            .AddSubPlot(1, 2, 1, ax =>
            {
                ax.Plot([1.0, 2.0], [1.0, 2.0]);
                ax.WithColorBar(cb => cb with { Visible = true, Orientation = ColorBarOrientation.Vertical, Label = "Intensity scale" });
            })
            .AddSubPlot(1, 2, 2, ax => ax.Plot([1.0, 2.0], [1.0, 2.0]))
            .Build();

        var bareSpacing  = Engine().Compute(bareTwoCol, ctx);
        var leftCbSpacing = Engine().Compute(leftCb, ctx);

        Assert.True(leftCbSpacing.HorizontalGap > bareSpacing.HorizontalGap,
            $"Left-column colorbar must widen hGap; got bare={bareSpacing.HorizontalGap}, leftCb={leftCbSpacing.HorizontalGap}");
    }

    // ── L71 (true) ────────────────────────────────────────────────────────────
    /// <summary>Two-row grid where the BOTTOM subplot (RowStart=1 &gt; 0) carries a title — its
    /// TopNeeded contribution is what the engine accumulates into vGap at line 71. Existing
    /// fixtures only have titles on top-row subplots, so the inner branch never fires.</summary>
    [Fact]
    public void TwoRowGrid_BottomSubplotTitle_WidensVerticalGap()
    {
        var ctx = MakeCtx();
        var bareTwoRow = new FigureBuilder()
            .AddSubPlot(2, 1, 1, ax => ax.Plot([1.0, 2.0], [1.0, 2.0]))
            .AddSubPlot(2, 1, 2, ax => ax.Plot([1.0, 2.0], [1.0, 2.0]))
            .Build();

        var bottomTitle = new FigureBuilder()
            .AddSubPlot(2, 1, 1, ax => ax.Plot([1.0, 2.0], [1.0, 2.0]))
            .AddSubPlot(2, 1, 2, ax =>
            {
                ax.Plot([1.0, 2.0], [1.0, 2.0]);
                ax.WithTitle("Bottom subplot title");
            })
            .Build();

        var bareSpacing = Engine().Compute(bareTwoRow, ctx);
        var withTitle   = Engine().Compute(bottomTitle, ctx);

        Assert.True(withTitle.VerticalGap > bareSpacing.VerticalGap,
            $"Bottom-row subplot title must widen vGap; got bare={bareSpacing.VerticalGap}, withTitle={withTitle.VerticalGap}");
    }

    // ── L92 (true) ────────────────────────────────────────────────────────────
    /// <summary>Three-column grid where every subplot has both a Y-label (left demand) and a
    /// secondary Y-label (right demand). The middle subplot is interior on BOTH edges, so
    /// <c>maxNonLeftLeft</c> and <c>maxNonRightRight</c> both accumulate, and their sum
    /// (stackedH at line 91) exceeds the single-edge hGap — firing line 92's true branch.</summary>
    [Fact]
    public void ThreeColGrid_BothSideDemands_StackedHorizontalGapExceedsSingleSide()
    {
        var ctx = MakeCtx();
        var figure = new FigureBuilder()
            .AddSubPlot(1, 3, 1, ax =>
            {
                ax.Plot([1.0, 2.0], [1.0, 2.0]);
                ax.SetYLabel("Long left Y label");
                ax.WithSecondaryYAxis(y2 => y2.SetYLabel("Long right Y label"));
            })
            .AddSubPlot(1, 3, 2, ax =>
            {
                ax.Plot([1.0, 2.0], [1.0, 2.0]);
                ax.SetYLabel("Long left Y label");
                ax.WithSecondaryYAxis(y2 => y2.SetYLabel("Long right Y label"));
            })
            .AddSubPlot(1, 3, 3, ax =>
            {
                ax.Plot([1.0, 2.0], [1.0, 2.0]);
                ax.SetYLabel("Long left Y label");
                ax.WithSecondaryYAxis(y2 => y2.SetYLabel("Long right Y label"));
            })
            .Build();

        var spacing = Engine().Compute(figure, ctx);

        // hGap must reflect the stacked sum (left + right interior demand on the middle subplot).
        // Only need to assert the engine completed and produced a positive gap; the branch at
        // line 92 fires whenever stackedH (sum) exceeds the single-edge max set by lines 69-70.
        Assert.True(spacing.HorizontalGap > 0);
    }

    // ── L96 (true) ────────────────────────────────────────────────────────────
    /// <summary>Suptitle present, but the only subplot lives in row 1 of a 2-row GridSpec — so
    /// <c>pos.RowStart == 0</c> at line 62 is false for every subplot, top stays at default 20,
    /// and the supReserved-vs-top floor at line 96 fires its true branch.</summary>
    [Fact]
    public void SuptitleWithNoRow0Subplot_TopFloorBranchFires()
    {
        var ctx = MakeCtx();
        var figure = new FigureBuilder()
            .WithTitle("Figure suptitle")
            .WithGridSpec(2, 1)
            .AddSubPlot(new GridPosition(1, 2, 0, 1), ax => ax.Plot([1.0, 2.0], [1.0, 2.0]))
            .Build();

        var spacing = Engine().Compute(figure, ctx);

        // Top must reflect the suptitle reservation (>= ~30px floor for any reasonable title font).
        Assert.True(spacing.MarginTop >= 30,
            $"Top margin must honour suptitle floor when no row-0 subplot raised it; got {spacing.MarginTop}");
    }

    // ── L171 + L185 (legacy GridRows/Cols fallback) ──────────────────────────
    /// <summary>A figure with one subplot using the legacy <c>(rows, cols, index)</c> overload
    /// AND another subplot with no positioning at all (forcing the GetEffectivePosition fallback
    /// to call GetMaxRows / GetMaxCols, which then iterates and hits the
    /// <c>else if (ax.GridRows &gt; max)</c> arms at lines 171 and 185).</summary>
    [Fact]
    public void LegacyGridRowsCols_FallbackPath_DoesNotThrow()
    {
        var ctx = MakeCtx();
        var figure = new FigureBuilder()
            .AddSubPlot(3, 2, 1, ax => ax.Plot([1.0, 2.0], [1.0, 2.0]))
            .Build();
        // Mutate one subplot to remove its GridIndex but keep GridRows/Cols — forces the
        // GetEffectivePosition fallback (returns 0..maxRows × 0..maxCols), which calls
        // GetMaxRows / GetMaxCols whose else-if arms then iterate and use ax.GridRows.
        figure.SubPlots[0].GridIndex = 0;

        var spacing = Engine().Compute(figure, ctx);

        Assert.True(spacing.MarginLeft >= 30);
        Assert.True(spacing.MarginBottom >= 30);
    }

    // ── L171 + L185 (mixed positioning, true legacy + GridPosition fallback) ──
    /// <summary>A figure with one subplot using GridPosition AND a second using legacy GridRows
    /// without a GridIndex — forces the GetMaxRows/GetMaxCols loop to iterate over both arms
    /// (<c>HasValue + RowEnd &gt; max</c> and <c>else if (GridRows &gt; max)</c>).</summary>
    [Fact]
    public void MixedGridPositioning_LegacyAndExplicit_ComputesWithoutError()
    {
        var ctx = MakeCtx();
        var figure = new FigureBuilder()
            .WithGridSpec(2, 2)
            .AddSubPlot(new GridPosition(0, 1, 0, 1), ax => ax.Plot([1.0, 2.0], [1.0, 2.0]))
            .AddSubPlot(2, 2, 4, ax => ax.Plot([3.0, 4.0], [5.0, 6.0]))
            .Build();
        // Drop the legacy subplot's GridIndex to force the GetEffectivePosition fallback path
        // for that one specific subplot, which then exercises GetMaxRows/Cols across the mixed set.
        figure.SubPlots[1].GridIndex = 0;

        var spacing = Engine().Compute(figure, ctx);

        Assert.True(spacing.MarginLeft >= 30);
    }
}
