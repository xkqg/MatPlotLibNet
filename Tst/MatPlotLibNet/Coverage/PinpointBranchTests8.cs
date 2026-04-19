// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Coverage;

/// <summary>Phase Q Wave 2 (2026-04-19) — eighth pinpoint batch on more renderer branches.</summary>
public class PinpointBranchTests8
{
    // StreamplotSeriesRenderer L25: `if (nx < 2 || ny < 2) return;`
    [Fact] public void StreamplotRenderer_TooSmallGrid_HitsEarlyReturn()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.AddSeries(new StreamplotSeries(
                new double[] { 0.0 }, new double[] { 0.0 },
                new double[,] { { 1 } }, new double[,] { { 0 } })))
            .Build();
        Assert.StartsWith("<svg", fig.ToSvg());
    }

    // HexbinSeriesRenderer L23: `if (xMin >= xMax) { xMax = xMin + 1; }` — degenerate range.
    [Fact] public void HexbinRenderer_AllSamePoint_HitsDegenerateRangeBranch()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.AddSeries(new HexbinSeries(
                new double[] { 5, 5, 5 }, new double[] { 5, 5, 5 })))
            .Build();
        Assert.StartsWith("<svg", fig.ToSvg());
    }

    // RadarSeriesRenderer L19: `series.FillColor ?? ApplyAlpha(color, series.Alpha)` — explicit FillColor.
    [Fact] public void RadarRenderer_ExplicitFillColor_UsesIt()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Radar(new[] { "A", "B", "C" }, new[] { 1.0, 2.0, 3.0 },
                s => s.FillColor = Colors.Salmon))
            .Build();
        Assert.StartsWith("<svg", fig.ToSvg());
    }

    // RadarSeriesRenderer L25: `series.MaxValue ?? series.Values.Max()` — explicit MaxValue.
    [Fact] public void RadarRenderer_ExplicitMaxValue_UsesIt()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Radar(new[] { "A", "B", "C" }, new[] { 1.0, 2.0, 3.0 },
                s => s.MaxValue = 10.0))
            .Build();
        Assert.StartsWith("<svg", fig.ToSvg());
    }

    // Scatter3DSeriesRenderer L19: `if (series.X.Length == 0) return;` empty data.
    [Fact] public void Scatter3DRenderer_EmptyData_HitsEarlyReturn()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.AddSeries(new Scatter3DSeries(
                Array.Empty<double>(), Array.Empty<double>(), Array.Empty<double>())))
            .Build();
        Assert.StartsWith("<svg", fig.ToSvg());
    }

    // Trisurf3DSeriesRenderer L45: `if (n % 3 == 0)` — triangle-count branch.
    // 3 points triangulate to 1 triangle (n=3, n % 3 == 0 path).
    [Fact] public void Trisurf3DRenderer_TriangleCountModulo3_HitsBranch()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.AddSeries(new Trisurf3DSeries(
                new double[] { 0.0, 1, 0.5 }, new double[] { 0.0, 0, 1 }, new double[] { 1.0, 2, 3 })))
            .Build();
        Assert.StartsWith("<svg", fig.ToSvg());
    }

    // MarkerRenderer L47/L105 — exercises remaining marker shape branches via every MarkerStyle.
    [Theory]
    [InlineData(MarkerStyle.Triangle)]
    [InlineData(MarkerStyle.TriangleDown)]
    [InlineData(MarkerStyle.TriangleLeft)]
    [InlineData(MarkerStyle.TriangleRight)]
    [InlineData(MarkerStyle.Diamond)]
    [InlineData(MarkerStyle.Cross)]
    [InlineData(MarkerStyle.Plus)]
    [InlineData(MarkerStyle.Star)]
    [InlineData(MarkerStyle.Pentagon)]
    [InlineData(MarkerStyle.Hexagon)]
    public void MarkerRenderer_EveryShape_RendersWithoutCrash(MarkerStyle marker)
    {
        var fig = Plt.Create()
            .Plot(new double[] { 1, 2, 3 }, new double[] { 1, 2, 3 },
                s => { s.Marker = marker; s.MarkerSize = 8; })
            .Build();
        Assert.StartsWith("<svg", fig.ToSvg());
    }

    // Plus a stroke-only marker variant (fill = null) — exercised by clearing the
    // Scatter color so the renderer falls into the no-fill branch.
    [Fact] public void MarkerRenderer_NoFillColor_HitsNullFillBranch()
    {
        var fig = Plt.Create()
            .Scatter(new double[] { 1, 2, 3 }, new double[] { 1, 2, 3 },
                s => { s.Marker = MarkerStyle.Square; s.Color = null; })
            .Build();
        Assert.StartsWith("<svg", fig.ToSvg());
    }
}
