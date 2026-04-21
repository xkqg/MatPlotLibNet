// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies SVG output of <see cref="PointplotSeries"/> rendering.</summary>
public class PointplotSeriesRenderTests
{
    private static readonly double[][] Datasets = [[1.0, 2.0, 3.0, 2.5], [4.0, 5.0, 4.5, 5.5]];

    [Fact]
    public void Pointplot_SvgContainsCircles()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Pointplot(Datasets))
            .ToSvg();
        Assert.Contains("<circle", svg);
    }

    [Fact]
    public void Pointplot_SvgContainsLines()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Pointplot(Datasets))
            .ToSvg();
        Assert.Contains("<line", svg);
    }

    [Fact]
    public void Pointplot_FluentShortcut_ProducesSvg()
    {
        string svg = Plt.Create()
            .Pointplot(Datasets)
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    // ── Branch coverage ──────────────────────────────────────────────────────

    [Fact]
    public void Pointplot_EmptyDatasets_EarlyReturn()
    {
        // PointplotSeriesRenderer.Render: Datasets.Length == 0 branch
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Pointplot([]))
            .ToSvg();
        Assert.StartsWith("<svg", svg);
    }

    [Fact]
    public void Pointplot_EmptyInnerDataset_ContinueBranch()
    {
        // PointplotSeriesRenderer.Render: data.Length == 0 continue-branch
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Pointplot([[], [1.0, 2.0, 3.0]]))
            .ToSvg();
        Assert.Contains("<circle", svg);
    }

    [Fact]
    public void Pointplot_SingleValueDataset_ComputeCI_ReturnsZero()
    {
        // PointplotSeriesRenderer.ComputeCI: v.Length < 2 branch → returns 0
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Pointplot([[5.0]]))
            .ToSvg();
        Assert.Contains("<circle", svg);
    }
}
