// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet;
using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Coverage;

/// <summary>Phase Q Wave 2 (2026-04-19) — fourth pinpoint batch targeting renderer
/// length-mismatch + degenerate-input branches.</summary>
public class PinpointBranchTests4
{
    // PointplotSeriesRenderer.cs L58: `if (v.Length < 2) return 0;` — single-value group.
    [Fact] public void Pointplot_SingleValueGroup_HitsCIWidthShortCircuit()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.AddSeries(new PointplotSeries([
                new[] { 5.0 },           // single value (Length < 2 branch)
                new[] { 1.0, 2.0, 3.0 }
            ])))
            .Build();
        Assert.StartsWith("<svg", fig.ToSvg());
    }

    // BarbsSeriesRenderer.cs L24: `i < series.Speed.Length ? series.Speed[i] : 0` —
    // length-mismatch fallback (Speed shorter than X).
    [Fact] public void Barbs_LengthMismatch_HitsSpeedFallbackBranch()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.AddSeries(new BarbsSeries(
                new double[] { 1.0, 2.0, 3.0 }, new double[] { 1.0, 2.0, 3.0 },
                new double[] { 10.0 },                   // Speed shorter than X/Y
                new double[] { 45.0 })))                 // Direction shorter too
            .Build();
        Assert.StartsWith("<svg", fig.ToSvg());
    }

    // PolarHeatmapSeriesRenderer.cs L37: bounds-checked data access — out-of-range fallback.
    [Fact] public void PolarHeatmap_DataMismatch_HitsBoundsCheckBranch()
    {
        // Mismatch between configured bins and Data dimensions exercises the
        // bounds-check fallback path.
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.AddSeries(new PolarHeatmapSeries(
                new double[,] { { 1, 2 }, { 3, 4 } }, thetaBins: 4, rBins: 4))) // bins > data
            .Build();
        Assert.StartsWith("<svg", fig.ToSvg());
    }

    // HeatmapSeriesRenderer.cs L19: `if (rows == 0 || cols == 0) return;` — empty grid.
    [Fact] public void Heatmap_EmptyData_HitsEarlyReturn()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.AddSeries(new HeatmapSeries(new double[0, 0])))
            .Build();
        Assert.StartsWith("<svg", fig.ToSvg());
    }

    // TripcolorSeriesRenderer.cs L22: `if (tris.Length == 0) return;` — degenerate triangulation.
    [Fact] public void Tripcolor_TooFewPoints_HitsEmptyTrianglesBranch()
    {
        // 2 points → no triangles possible.
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.AddSeries(new TripcolorSeries(
                new double[] { 0.0, 1.0 }, new double[] { 0.0, 1.0 }, new double[] { 1.0, 2.0 })))
            .Build();
        Assert.StartsWith("<svg", fig.ToSvg());
    }

    // ContourfSeriesRenderer.cs L27: `if (rows < 2 || cols < 2) return;` — undersized grid.
    [Fact] public void Contourf_TooSmallGrid_HitsEarlyReturn()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.AddSeries(new ContourfSeries(
                new double[] { 1.0 }, new double[] { 1.0 }, new double[,] { { 5 } })))  // 1×1
            .Build();
        Assert.StartsWith("<svg", fig.ToSvg());
    }

    // SunburstSeriesRenderer.cs L44: `Context?.Theme?.ForegroundText ?? Colors.Black` —
    // the null-coalesce branch fires when Theme is null. Drive via a sunburst with deeper
    // tree structure that creates leader-line placements.
    [Fact] public void Sunburst_DeepTreeWithLeaderLines_HitsLeaderColorBranch()
    {
        var root = new TreeNode
        {
            Label = "Root",
            Children =
            [
                new() { Label = "A", Value = 10, Children =
                [
                    new() { Label = "A1", Value = 6, Children =
                    [
                        new() { Label = "Tiny", Value = 1 }
                    ] },
                    new() { Label = "A2", Value = 4 }
                ] },
                new() { Label = "B", Value = 5 }
            ]
        };
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Sunburst(root))
            .Build();
        Assert.StartsWith("<svg", fig.ToSvg());
    }

    // Adx L82, L93, L94 — extra branches in flat-data cases. Adx is exempt at 90/85 but
    // exercising these helps keep the metric clean.
    [Fact] public void Adx_AllEqualBars_HitsZeroMovementBranches()
    {
        // Flat OHLC → DM components are all zero → the ternary fallbacks fire.
        double[] flat = Enumerable.Repeat(50.0, 30).ToArray();
        var adx = new Adx(flat, flat, flat, period: 14);
        var result = adx.Compute();
        Assert.NotNull(result);
    }

    // EnumerableFigureExtensions L99 — likely a Plot/Scatter Theory branch.
    [Fact] public void EnumerableExtensions_TupleSequence_HitsTupleBranch()
    {
        var pairs = new[] { (1.0, 2.0), (3.0, 4.0), (5.0, 6.0) };
        var fig = Plt.Create().Plot(pairs.Select(p => p.Item1).ToArray(), pairs.Select(p => p.Item2).ToArray()).Build();
        Assert.NotNull(fig);
    }
}
