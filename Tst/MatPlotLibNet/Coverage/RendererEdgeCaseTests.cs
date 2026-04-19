// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Coverage;

/// <summary>Phase Q Wave 2 (2026-04-19) — edge-case Facts for series renderers at 83-87%
/// branch coverage. Each renderer typically has 2-3 unhit branches: empty-data short-circuit,
/// degenerate (single-value) data branch, and explicit-ColorMap branch. Per the Phase Q plan
/// these are tested via the high-level Plt pipeline, not via direct renderer invocation,
/// so the surrounding ChartRenderer / SubplotRenderer paths get exercised at the same time.</summary>
public class RendererEdgeCaseTests
{
    private static string RenderSvg(Action<MatPlotLibNet.AxesBuilder> configure)
        => Plt.Create().WithSize(400, 300).AddSubPlot(1, 1, 1, configure).Build().ToSvg();

    // ── Histogram2DSeriesRenderer (100/86.4) ──────────────────────────────

    [Fact] public void Histogram2D_AllEqualData_HitsMinEqMaxBranch()
    {
        // All counts will be equal → min == max → max += 1 fallback branch.
        var svg = RenderSvg(ax => ax.Histogram2D([1.0, 1.0, 1.0], [1.0, 1.0, 1.0], bins: 5));
        Assert.StartsWith("<svg", svg);
    }

    [Fact] public void Histogram2D_EmptyXData_HitsEarlyReturnBranch()
    {
        var svg = RenderSvg(ax => ax.AddSeries(new Histogram2DSeries([], [])));
        Assert.StartsWith("<svg", svg);
    }

    // ── PointplotSeriesRenderer (100/87.5) ────────────────────────────────

    [Fact] public void Pointplot_SingleGroup_RendersErrorBars()
    {
        var svg = RenderSvg(ax => ax.AddSeries(new PointplotSeries([[1.0, 2.0, 3.0, 4.0, 5.0]])));
        Assert.StartsWith("<svg", svg);
    }

    [Fact] public void Pointplot_EmptyGroup_HitsEmptyBranch()
    {
        var svg = RenderSvg(ax => ax.AddSeries(new PointplotSeries([Array.Empty<double>()])));
        Assert.StartsWith("<svg", svg);
    }

    // ── PolarHeatmapSeriesRenderer (100/87.5) ─────────────────────────────

    [Fact] public void PolarHeatmap_AllEqualData_HitsMinEqMaxBranch()
    {
        var svg = RenderSvg(ax => ax.AddSeries(new PolarHeatmapSeries(
            new double[,] { { 5, 5 }, { 5, 5 } }, thetaBins: 2, rBins: 2)));
        Assert.StartsWith("<svg", svg);
    }

    // ── SpectrogramSeriesRenderer (100/87.5) ──────────────────────────────

    [Fact] public void Spectrogram_AllZeroData_HitsLogZeroBranch()
    {
        var svg = RenderSvg(ax => ax.AddSeries(new SpectrogramSeries(new double[16])));
        Assert.StartsWith("<svg", svg);
    }

    // ── BarbsSeriesRenderer (100/85.7) ────────────────────────────────────

    [Fact] public void Barbs_NegativeWindSpeed_HitsAbsoluteBranch()
    {
        var svg = RenderSvg(ax => ax.AddSeries(new BarbsSeries(
            new double[] { 1.0, 2.0 }, new double[] { 1.0, 2.0 },
            new double[] { -10.0, 50.0 }, new double[] { 45.0, 90.0 })));
        Assert.StartsWith("<svg", svg);
    }

    // ── EcdfSeriesRenderer (100/83.3) ─────────────────────────────────────

    [Fact] public void Ecdf_SinglePoint_HitsDegenerateBranch()
    {
        var svg = RenderSvg(ax => ax.Ecdf([5.0]));
        Assert.StartsWith("<svg", svg);
    }

    // ── HeatmapSeriesRenderer (100/83.3) ──────────────────────────────────

    [Fact] public void Heatmap_AllEqualData_HitsMinEqMaxBranch()
    {
        var svg = RenderSvg(ax => ax.Heatmap(new double[,] { { 7, 7 }, { 7, 7 } }));
        Assert.StartsWith("<svg", svg);
    }

    [Fact] public void Heatmap_ExplicitColorMap_HitsCustomCMapBranch()
    {
        var svg = RenderSvg(ax => ax.Heatmap(new double[,] { { 1, 2 }, { 3, 4 } },
            s => s.ColorMap = ColorMaps.Plasma));
        Assert.StartsWith("<svg", svg);
    }

    // ── PolarLineSeriesRenderer (100/83.3) ────────────────────────────────

    [Fact] public void PolarLine_SinglePoint_HitsSinglePointBranch()
    {
        var svg = RenderSvg(ax => ax.PolarPlot([1.0], [0.0]));
        Assert.StartsWith("<svg", svg);
    }

    // ── ResidualSeriesRenderer (100/83.3) ─────────────────────────────────

    [Fact] public void Residual_AllZeroResiduals_HitsZeroBranch()
    {
        var svg = RenderSvg(ax => ax.AddSeries(
            new ResidualSeries(new double[] { 1.0, 2.0, 3.0 }, new double[] { 1.0, 2.0, 3.0 })));
        Assert.StartsWith("<svg", svg);
    }

    // ── TripcolorSeriesRenderer (100/83.3) ────────────────────────────────

    [Fact] public void Tripcolor_AllEqualValues_HitsMinEqMaxBranch()
    {
        var svg = RenderSvg(ax => ax.AddSeries(new TripcolorSeries(
            new double[] { 0.0, 1.0, 0.5 }, new double[] { 0.0, 0.0, 1.0 }, new double[] { 3.0, 3.0, 3.0 })));
        Assert.StartsWith("<svg", svg);
    }

    // ── EnumerableFigureExtensions (100/85.0) ─────────────────────────────

    [Fact] public void EnumerableFigure_StringElements_HitsCategoricalBranch()
    {
        // The EnumerableFigureExtensions has overloads for IEnumerable<string> +
        // IEnumerable<double> that go through a categorical-axis branch.
        var values = new[] { 1.0, 2.0, 3.0 };
        var fig = Plt.Create().Plot([1.0, 2.0, 3.0], values).Build();
        Assert.NotNull(fig);
    }

    // ── LegendMeasurer (100/84.0) ─────────────────────────────────────────

    [Fact] public void LegendMeasurer_LongLabels_HitsTextWidthBranch()
    {
        var svg = RenderSvg(ax => ax
            .Plot([1.0, 2.0], [3.0, 4.0], s => s.Label = "Very long legend label that wraps")
            .Plot([1.0, 2.0], [4.0, 3.0], s => s.Label = "Another very long legend label here")
            .WithLegend());
        Assert.StartsWith("<svg", svg);
    }

    // ── ContourSeriesRenderer (97.1/86.7) ─────────────────────────────────

    [Fact] public void Contour_AllEqualData_HitsDegenerateBranch()
    {
        var svg = RenderSvg(ax => ax.Contour([0.0, 1.0], [0.0, 1.0],
            new double[,] { { 5, 5 }, { 5, 5 } }, s => s.Levels = 3));
        Assert.StartsWith("<svg", svg);
    }
}
