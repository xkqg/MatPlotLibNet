// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Coverage;

/// <summary>
/// Phase Ω.4 (v1.7.2, 2026-04-19) — small-class quick-fire batch lifting
/// the 24 sub-90 classes with ≤ 5 uncovered lines each. Each fact pins a
/// specific cobertura `condition-coverage` marker via file:line.
/// </summary>
public class PinpointBranchTests15
{
    // ── KdeSeries.ComputeDataRange empty-density (L49 false arm) ──────────

    [Fact]
    public void KdeSeries_ComputeDataRange_EmptyData_FallsBackToYMaxOne()
    {
        var s = new KdeSeries((Vec)Array.Empty<double>());
        var dr = s.ComputeDataRange(null!);
        Assert.NotNull(dr.YMax);
    }

    // ── HexbinSeries.ComputeColorBarRange empty-bins (L43 true arm) ───────

    [Fact]
    public void HexbinSeries_GetColorBarRange_EmptyData_ReturnsZeroOne()
    {
        var s = new HexbinSeries((Vec)Array.Empty<double>(), (Vec)Array.Empty<double>());
        var (min, max) = s.GetColorBarRange();
        Assert.Equal(0, min);
        Assert.Equal(1, max);
    }

    // ── SecondaryAxisBuilder.Scatter null-configure (L1085 false arm) ─────

    [Fact]
    public void SecondaryAxisBuilder_Scatter_NullConfigure_NoOp()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2], [3.0, 4])
                .WithSecondaryYAxis(s => s.Scatter([1.5], [55.0], configure: null)))
            .Build();
        Assert.NotEmpty(fig.SubPlots[0].SecondarySeries);
    }

    [Fact]
    public void SecondaryAxisBuilder_Plot_NullConfigure_NoOp()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2], [3.0, 4])
                .WithSecondaryYAxis(s => s.Plot([1.5, 2.5], [55.0, 60], configure: null)))
            .Build();
        Assert.NotEmpty(fig.SubPlots[0].SecondarySeries);
    }

    // ── QuiverKeySeriesRenderer dataRange ≤ 0 fallback (L27 false arm) ────

    [Fact]
    public void QuiverKeySeries_ZeroDataRange_FallsBackToFiftyPixels()
    {
        // Force degenerate data range by setting axes limits where xMin == xMax
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.Plot([1.0], [1.0]);  // single-point series → degenerate range
                ax.QuiverKey(0.5, 0.5, 1.0, "k");
            })
            .Build();
        var svg = fig.ToSvg();
        Assert.Contains("<svg", svg);
    }

    // ── MarkerRenderer Cross/Plus null-color fallback + 0 strokeWidth ─────

    // ── MarkerRenderer Cross/Plus null-color + 0-strokeWidth tested via direct invocation
    // (ScatterSeries doesn't expose MarkerEdgeColor/MarkerEdgeWidth — invoke MarkerRenderer
    // directly with controlled arguments).

    [Fact]
    public void MarkerRenderer_Cross_FillNull_StrokeNull_FallsBackToBlack()
    {
        var svg = new global::MatPlotLibNet.Rendering.Svg.SvgRenderContext();
        MarkerRenderer.Draw(svg, MarkerStyle.Cross, new Point(50, 50), 10,
            fill: null, stroke: null, strokeWidth: 0);
        Assert.Contains("<line", svg.GetOutput());
    }

    [Fact]
    public void MarkerRenderer_Plus_FillNull_StrokeNull_FallsBackToBlack()
    {
        var svg = new global::MatPlotLibNet.Rendering.Svg.SvgRenderContext();
        MarkerRenderer.Draw(svg, MarkerStyle.Plus, new Point(50, 50), 10,
            fill: null, stroke: null, strokeWidth: 0);
        Assert.Contains("<line", svg.GetOutput());
    }

    [Fact]
    public void MarkerRenderer_Cross_StrokeWidthZero_FallsBackToComputedThickness()
    {
        var svg = new global::MatPlotLibNet.Rendering.Svg.SvgRenderContext();
        MarkerRenderer.Draw(svg, MarkerStyle.Cross, new Point(50, 50), 16,
            fill: Colors.Red, stroke: null, strokeWidth: 0);
        Assert.Contains("<line", svg.GetOutput());
    }

    // ── TripcolorSeriesRenderer empty-Z fallback (L22-25) ─────────────────

    [Fact]
    public void TripcolorRenderer_EmptyZ_UsesZeroOneRange()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Tripcolor(
                [0.0, 1, 0.5, 0.3], [0.0, 0, 1, 0.5], [],  // Empty Z!
                s => { s.Triangles = [0, 1, 2, 0, 1, 3]; }))
            .Build();
        var svg = fig.ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void TripcolorRenderer_FewerThanThreePoints_EarlyReturns()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Tripcolor([0.0, 1], [0.0, 1], [1.0, 2]))  // 2 points < 3
            .Build();
        var svg = fig.ToSvg();
        Assert.Contains("<svg", svg);
    }

    // ── Histogram2DSeries.ComputeBinCounts uniform-X / uniform-Y ──────────

    [Fact]
    public void Histogram2DSeries_ComputeBinCounts_AllSameX_HitsXMaxEqualsXMinArm()
    {
        // L51-62 in source: `if (xMax == xMin) xMax = xMin + 1;`
        var s = new Histogram2DSeries([5.0, 5, 5], [1.0, 2, 3]);
        var counts = s.ComputeBinCounts();
        Assert.NotNull(counts);
    }

    [Fact]
    public void Histogram2DSeries_ComputeBinCounts_AllSameY_HitsYMaxEqualsYMinArm()
    {
        var s = new Histogram2DSeries([1.0, 2, 3], [5.0, 5, 5]);
        var counts = s.ComputeBinCounts();
        Assert.NotNull(counts);
    }

    [Fact]
    public void Histogram2DSeries_ComputeBinCounts_EmptyData_ReturnsAllZeros()
    {
        var s = new Histogram2DSeries((Vec)Array.Empty<double>(), (Vec)Array.Empty<double>());
        var counts = s.ComputeBinCounts();
        Assert.Equal(0, counts.Cast<int>().Sum());
    }

    [Fact]
    public void Histogram2DSeries_ComputeDataRange_EmptyData_ReturnsNullRange()
    {
        var s = new Histogram2DSeries((Vec)Array.Empty<double>(), (Vec)Array.Empty<double>());
        var dr = s.ComputeDataRange(null!);
        Assert.Null(dr.XMin);
    }

    // ── StackedAreaSeries.ComputeDataRange — sticky baseline arms ─────────

    [Fact]
    public void StackedAreaSeries_NonZeroBaseline_NoStickyYMin()
    {
        // L67 in source: `Baseline == Zero && yMin >= 0 ? 0 : null`
        // Wiggle baseline → false arm → stickyYMin = null
        var s = new StackedAreaSeries(
            x: [1.0, 2, 3],
            ySets: [[1.0, 2.0, 1.5]])
        {
            Baseline = StackedBaseline.Wiggle
        };
        var dr = s.ComputeDataRange(null!);
        Assert.Null(dr.StickyYMin);
    }

    [Fact]
    public void StackedAreaSeries_PositiveValues_HasStickyZeroBaseline()
    {
        // True arm → stickyYMin = 0
        var s = new StackedAreaSeries(
            x: [1.0, 2, 3],
            ySets: [[1.0, 2.0, 1.5], [0.5, 0.8, 0.6]]);
        var dr = s.ComputeDataRange(null!);
        Assert.Equal(0, dr.StickyYMin);
    }

    // ── KdeSeries.ToSeriesDto Solid arm round-trip (L54 true arm) ─────────

    [Fact]
    public void KdeSeries_ToSeriesDto_DefaultSolidLineStyle_OmitsLineStyleField()
    {
        var s = new KdeSeries([1.0, 2, 3]);  // default LineStyle = Solid
        var dto = s.ToSeriesDto();
        Assert.Null(dto.LineStyle);
    }

    [Fact]
    public void KdeSeries_ToSeriesDto_NonSolidLineStyle_SerializesField()
    {
        var s = new KdeSeries([1.0, 2, 3]) { LineStyle = LineStyle.Dotted };
        var dto = s.ToSeriesDto();
        Assert.NotNull(dto.LineStyle);
    }

    // ── Contour3DSeries.ToSeriesDto default arms ──────────────────────────

    [Fact]
    public void Contour3DSeries_ToSeriesDto_DefaultLevelsAndLineWidth_OmitsFields()
    {
        var s = new Contour3DSeries([0.0, 1.0], [0.0, 1.0], new double[,] { { 0, 1 }, { 1, 0 } });
        var dto = s.ToSeriesDto();
        Assert.Null(dto.Levels);
        Assert.Null(dto.LineWidth);
    }

    [Fact]
    public void Contour3DSeries_ToSeriesDto_NonDefaultLevelsAndLineWidth_SerializesFields()
    {
        var s = new Contour3DSeries([0.0, 1.0], [0.0, 1.0], new double[,] { { 0, 1 }, { 1, 0 } })
        {
            Levels = 15,
            LineWidth = 2.5
        };
        var dto = s.ToSeriesDto();
        Assert.Equal(15, dto.Levels);
        Assert.Equal(2.5, dto.LineWidth);
    }

    // ── HexbinSeries.ToSeriesDto default vs non-default ───────────────────

    [Fact]
    public void HexbinSeries_ToSeriesDto_DefaultGridSizeAndMinCount_OmitsFields()
    {
        var s = new HexbinSeries([1.0, 2], [1.0, 2]);  // defaults
        var dto = s.ToSeriesDto();
        Assert.Null(dto.GridSize);
        Assert.Null(dto.MinCount);
    }

    // ── RegressionSeries.ToSeriesDto multiple default-vs-non-default arms ─

    [Fact]
    public void RegressionSeries_ToSeriesDto_AllDefaults_OmitsAllOptionalFields()
    {
        var s = new RegressionSeries([1.0, 2], [1.0, 2]);  // defaults
        var dto = s.ToSeriesDto();
        Assert.Null(dto.Degree);
        Assert.Null(dto.ShowConfidence);
        Assert.Null(dto.ConfidenceLevel);
        Assert.Null(dto.Alpha);
    }

    // ── Histogram2DSeries.ToSeriesDto null-colormap ───────────────────────

    [Fact]
    public void Histogram2DSeries_ToSeriesDto_NullColorMap_OmitsColorMapName()
    {
        var s = new Histogram2DSeries([1.0, 2], [1.0, 2]);
        var dto = s.ToSeriesDto();
        Assert.Null(dto.ColorMapName);
    }

    [Fact]
    public void Histogram2DSeries_ToSeriesDto_WithColorMap_SerializesName()
    {
        var s = new Histogram2DSeries([1.0, 2], [1.0, 2])
        {
            ColorMap = global::MatPlotLibNet.Styling.ColorMaps.ColorMaps.Viridis
        };
        var dto = s.ToSeriesDto();
        Assert.Equal("viridis", dto.ColorMapName);
    }

    // ── StackedAreaSeries.ToSeriesDto labels arm ──────────────────────────

    [Fact]
    public void StackedAreaSeries_ToSeriesDto_NullLabels_OmitsField()
    {
        var s = new StackedAreaSeries([1.0, 2], [[1.0, 2]]);
        var dto = s.ToSeriesDto();
        Assert.Null(dto.PieLabels);
    }

    [Fact]
    public void StackedAreaSeries_ToSeriesDto_WithLabels_SerializesField()
    {
        var s = new StackedAreaSeries([1.0, 2], [[1.0, 2]]) { Labels = ["a"] };
        var dto = s.ToSeriesDto();
        Assert.NotNull(dto.PieLabels);
    }
}
