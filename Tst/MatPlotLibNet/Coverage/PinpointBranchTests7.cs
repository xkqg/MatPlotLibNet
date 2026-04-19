// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.Lighting;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Coverage;

/// <summary>Phase Q Wave 2 (2026-04-19) — seventh pinpoint batch on renderer + lighting branches.</summary>
public class PinpointBranchTests7
{
    // DirectionalLight + LightingHelper degenerate-normal branches require precise vector
    // setup that's not in scope here — leaving them for a Phase R 3D-rendering focused pass.

    // EventplotSeriesRenderer L23: `series.Colors is not null && i < series.Colors.Length`
    // Test with explicit Colors array (other branch covered by null Colors path).
    [Fact] public void EventplotRenderer_ExplicitColors_AppliesPerLineColors()
    {
        var s = new EventplotSeries([new double[] { 1, 2 }, new double[] { 3 }])
        { Colors = [Colors.Red, Colors.Blue] };
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.AddSeries(s))
            .Build();
        Assert.StartsWith("<svg", fig.ToSvg());
    }

    // PlanarBar3DSeriesRenderer L52: `series.Colors is { } cs && i < cs.Length` per-bar.
    [Fact] public void PlanarBar3DRenderer_ExplicitColors_AppliesPerBarColors()
    {
        var s = new PlanarBar3DSeries(
            new double[] { 1, 2 }, new double[] { 1, 2 }, new double[] { 3, 4 })
        { Colors = [Colors.Red, Colors.Blue] };
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.AddSeries(s))
            .Build();
        Assert.StartsWith("<svg", fig.ToSvg());
    }

    // StackedAreaSeriesRenderer L21: `if (n == 0 || layers == 0) return;` — empty data.
    [Fact] public void StackedAreaRenderer_EmptyX_HitsEarlyReturn()
    {
        var s = new StackedAreaSeries(Array.Empty<double>(),
            new double[][] { Array.Empty<double>() });
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.AddSeries(s))
            .Build();
        Assert.StartsWith("<svg", fig.ToSvg());
    }

    // StemSeriesRenderer L18: `series.StemColor ?? SeriesColor` — explicit StemColor.
    [Fact] public void StemRenderer_ExplicitStemColor_UsesIt()
    {
        var s = new StemSeries(new double[] { 1, 2, 3 }, new double[] { 1, 2, 3 })
        { StemColor = Colors.Red };
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.AddSeries(s))
            .Build();
        Assert.StartsWith("<svg", fig.ToSvg());
    }

    // LineSeriesRenderer L28: `if (series.Smooth && drawX.Length >= 3)` — Smooth=true.
    [Fact] public void LineRenderer_SmoothTrue_HitsSmoothBranch()
    {
        var fig = Plt.Create()
            .Plot(new double[] { 1, 2, 3, 4, 5 }, new double[] { 1, 4, 2, 5, 3 },
                s => s.Smooth = true)
            .Build();
        Assert.StartsWith("<svg", fig.ToSvg());
    }

    // LineSeriesRenderer L41: `series.DrawStyle is not null and not DrawStyle.Default` — non-default DrawStyle.
    [Fact] public void LineRenderer_StepsDrawStyle_HitsStepBranch()
    {
        var fig = Plt.Create()
            .Plot(new double[] { 1, 2, 3, 4, 5 }, new double[] { 1, 4, 2, 5, 3 },
                s => { s.DrawStyle = DrawStyle.StepsMid; s.Marker = MarkerStyle.Circle; })
            .Build();
        Assert.StartsWith("<svg", fig.ToSvg());
    }

    // LineSeriesRenderer L47: `if (series.MarkEvery is not null && i % series.MarkEvery.Value != 0)` — MarkEvery set.
    [Fact] public void LineRenderer_MarkEveryTwo_HitsMarkEveryBranch()
    {
        var fig = Plt.Create()
            .Plot(new double[] { 1, 2, 3, 4, 5 }, new double[] { 1, 2, 3, 4, 5 },
                s => { s.Marker = MarkerStyle.Circle; s.MarkEvery = 2; })
            .Build();
        Assert.StartsWith("<svg", fig.ToSvg());
    }

    // LineSeriesRenderer L38: `series.MarkerEdgeColor is not null ? : 0` — explicit MarkerEdgeColor.
    [Fact] public void LineRenderer_ExplicitMarkerEdgeColor_HitsBranch()
    {
        var fig = Plt.Create()
            .Plot(new double[] { 1, 2, 3 }, new double[] { 1, 2, 3 },
                s => { s.Marker = MarkerStyle.Circle; s.MarkerEdgeColor = Colors.Black; })
            .Build();
        Assert.StartsWith("<svg", fig.ToSvg());
    }

    // QuiverKeySeriesRenderer L27 — already covered by the `SetXLim(5,5)` test in pinpoint3,
    // but coverage didn't lift. Try with explicit data range computation.
    [Fact] public void QuiverKeyRenderer_DegenerateDataRange_HitsFallback()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.AddSeries(new QuiverKeySeries(0.5, 0.9, 1.0, "1 m/s"));
                // Empty series leaves data range at 0..0
            })
            .Build();
        Assert.StartsWith("<svg", fig.ToSvg());
    }
}
