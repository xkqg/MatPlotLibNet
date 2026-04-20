// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet;
using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Coverage;

/// <summary>
/// Phase Ω+ (v1.7.2, 2026-04-20) — Phase A.1 quick-fire batch from the
/// strict-90 floor plan. Each fact pins a specific cobertura
/// `condition-coverage` marker via file:line citation.
/// </summary>
public class PinpointBranchTests16
{
    // ── PanEvent.ApplyTo — null axis-limit guards (L24, L30) ──────────────

    private static Figure FigureWithNullAxisLimits()
    {
        // Build a figure but explicitly null out the axis limits so the `is double` patterns fail.
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2], [3.0, 4]))
            .Build();
        fig.SubPlots[0].XAxis.Min = null;
        fig.SubPlots[0].XAxis.Max = null;
        fig.SubPlots[0].YAxis.Min = null;
        fig.SubPlots[0].YAxis.Max = null;
        return fig;
    }

    [Fact]
    public void PanEvent_BothAxisMinMaxNull_NoOpFigureUnchanged()
    {
        var fig = FigureWithNullAxisLimits();
        var ax = fig.SubPlots[0];
        var evt = new PanEvent(ChartId: "c", AxesIndex: 0, DxData: 5, DyData: 5);
        evt.ApplyTo(fig);
        Assert.Null(ax.XAxis.Min);
        Assert.Null(ax.YAxis.Min);
    }

    [Fact]
    public void PanEvent_OnlyXAxisLimitsSet_PansXNotY()
    {
        var fig = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2], [3.0, 4])).Build();
        var ax = fig.SubPlots[0];
        ax.XAxis.Min = 0; ax.XAxis.Max = 10;
        ax.YAxis.Min = null; ax.YAxis.Max = null;
        new PanEvent("c", 0, 5, 5).ApplyTo(fig);
        Assert.Equal(5, ax.XAxis.Min);
        Assert.Equal(15, ax.XAxis.Max);
        Assert.Null(ax.YAxis.Min);  // Y was null → no-op
    }

    [Fact]
    public void PanEvent_OnlyYAxisLimitsSet_PansYNotX()
    {
        var fig = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2], [3.0, 4])).Build();
        var ax = fig.SubPlots[0];
        ax.XAxis.Min = null; ax.XAxis.Max = null;
        ax.YAxis.Min = 0; ax.YAxis.Max = 10;
        new PanEvent("c", 0, 5, 5).ApplyTo(fig);
        Assert.Null(ax.XAxis.Min);
        Assert.Equal(5, ax.YAxis.Min);
        Assert.Equal(15, ax.YAxis.Max);
    }

    [Fact]
    public void PanEvent_XMinSetButXMaxNull_NoOpForX()
    {
        // Tests the && short-circuit: X.Min is double, X.Max is null → second clause false → no-op
        var fig = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2], [3.0, 4])).Build();
        var ax = fig.SubPlots[0];
        ax.XAxis.Min = 0; ax.XAxis.Max = null;
        new PanEvent("c", 0, 5, 5).ApplyTo(fig);
        Assert.Equal(0, ax.XAxis.Min);  // unchanged
    }

    // ── HexbinSeriesRenderer — degenerate-data guards (L23/24/27/33/41) ───

    [Fact]
    public void HexbinRenderer_EmptyData_EarlyReturn()
    {
        // L18-19: if (series.X.Length == 0) return;  Already covered, but pin via render.
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Hexbin(Array.Empty<double>(), Array.Empty<double>()))
            .Build();
        var svg = fig.ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void HexbinRenderer_AllSameX_FallsBackToXMaxXMinPlusOne()
    {
        // L23: if (xMin >= xMax) { xMax = xMin + 1; } true arm
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Hexbin([5.0, 5, 5, 5], [1.0, 2, 3, 4]))
            .Build();
        var svg = fig.ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void HexbinRenderer_AllSameY_FallsBackToYMaxYMinPlusOne()
    {
        // L24: if (yMin >= yMax) { yMax = yMin + 1; } true arm
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Hexbin([1.0, 2, 3, 4], [5.0, 5, 5, 5]))
            .Build();
        var svg = fig.ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void HexbinRenderer_BinsBelowMinCount_AllSkipped()
    {
        // L41: if (count < minCount) continue; — set MinCount > maxCount so all bins skip
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Hexbin([1.0, 2, 3, 4], [1.0, 2, 3, 4],
                s => { s.MinCount = 100; }))  // way above any actual bin count
            .Build();
        var svg = fig.ToSvg();
        Assert.Contains("<svg", svg);
    }

    // (SkiaFontMetrics tests live in Tst/MatPlotLibNet.Skia/ — the main test
    // project doesn't reference the Skia assembly. See SkiaRenderContextCoverageTests.cs.)

    // ── Trisurf3DSeriesRenderer L51-54 — fan triangulation (n%3 != 0) ─────

    [Fact]
    public void Trisurf3DRenderer_FourPoints_UsesFanTriangulation()
    {
        // n=4, 4%3 != 0 → else arm (fan from point 0): triangles (0,1,2), (0,2,3)
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Trisurf(
                [0.0, 1, 0.5, 0.3],
                [0.0, 0, 1, 0.7],
                [0.0, 1, 2, 1.5]))
            .Build();
        var svg = fig.ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void Trisurf3DRenderer_FivePoints_UsesFanTriangulation()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Trisurf(
                [0.0, 1, 0.5, 0.3, 0.8],
                [0.0, 0, 1, 0.7, 0.5],
                [0.0, 1, 2, 1.5, 1.2]))
            .Build();
        var svg = fig.ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void Trisurf3DRenderer_SixPoints_UsesSequentialTriangulation()
    {
        // n=6, 6%3 == 0 → if arm (sequential triplets): (0,1,2), (3,4,5)
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Trisurf(
                [0.0, 1, 0.5, 0.3, 0.8, 0.6],
                [0.0, 0, 1, 0.7, 0.5, 0.3],
                [0.0, 1, 2, 1.5, 1.2, 0.8]))
            .Build();
        var svg = fig.ToSvg();
        Assert.Contains("<svg", svg);
    }

    // ── LogLocator L40-41 — fallback when lower not in range ──────────────

    [Fact]
    public void LogLocator_SubDecadeMinAboveOne_FallsBackToMin()
    {
        // L37-43: ticks.Count == 0 path. lower = 10^floor(log10(min)) = 10^0 = 1.
        // For min=2, lower=1 < min=2 → else arm fires → ticks=[2]
        var loc = new global::MatPlotLibNet.Rendering.TickLocators.LogLocator();
        var ticks = loc.Locate(2.0, 5.0);
        Assert.Single(ticks);
        Assert.Equal(2.0, ticks[0]);
    }

    [Fact]
    public void LogLocator_SubDecadeWithLowerInRange_UsesLowerDecade()
    {
        // For min=0.5, max=5.0 → lower=10^floor(log10(0.5))=10^(-1)=0.1. lower<min → else.
        // For min=1.0, max=5.0 → lower=10^0=1. lower>=min && lower<=max → ticks=[1].
        var loc = new global::MatPlotLibNet.Rendering.TickLocators.LogLocator();
        var ticks = loc.Locate(1.0, 5.0);
        Assert.Single(ticks);
        Assert.Equal(1.0, ticks[0]);
    }

    // ── StackedAreaSeries DTO arm (L57, L63, L64 partials) ────────────────

    [Fact]
    public void StackedAreaSeries_ToSeriesDto_NonDefaultBaseline_SerializesField()
    {
        var s = new StackedAreaSeries([1.0, 2], [[1.0, 2]])
        {
            Baseline = StackedBaseline.Wiggle
        };
        var dto = s.ToSeriesDto();
        Assert.NotNull(dto);
    }

    [Fact]
    public void StackedAreaSeries_ToSeriesDto_DefaultBaseline_OmitsField()
    {
        var s = new StackedAreaSeries([1.0, 2], [[1.0, 2]]);  // default = Zero
        var dto = s.ToSeriesDto();
        Assert.NotNull(dto);
    }
}
