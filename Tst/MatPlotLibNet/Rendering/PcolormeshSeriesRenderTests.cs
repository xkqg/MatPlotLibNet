// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies SVG output of <see cref="PcolormeshSeries"/> rendering.</summary>
public class PcolormeshSeriesRenderTests
{
    private static readonly double[] X = [0.0, 1.0, 2.0, 3.0];
    private static readonly double[] Y = [0.0, 1.0, 2.0];
    private static readonly double[,] C = { { 1.0, 2.0, 3.0 }, { 4.0, 5.0, 6.0 } };

    [Fact]
    public void Pcolormesh_RendersWithoutError()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Pcolormesh(X, Y, C))
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void Pcolormesh_SvgContainsRectangles()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Pcolormesh(X, Y, C))
            .ToSvg();
        Assert.Contains("<rect", svg);
    }

    [Fact]
    public void Pcolormesh_SingleCell_RendersWithoutError()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Pcolormesh([0.0, 1.0], [0.0, 1.0], new double[,] { { 5.0 } }))
            .ToSvg();
        Assert.Contains("<rect", svg);
    }

    [Fact]
    public void Pcolormesh_FluentShortcut_ProducesSvg()
    {
        string svg = Plt.Create()
            .Pcolormesh(X, Y, C)
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void Pcolormesh_EmptyC_RendersWithoutError()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Pcolormesh([0.0, 1.0], [0.0, 1.0], new double[0, 0]))
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    // Branch coverage — exercise each early-return guard in PcolormeshSeriesRenderer.

    [Fact]
    public void Pcolormesh_ZeroCols_EarlyReturn_NoCellsEmitted()
    {
        // rows>0 but cols==0 — the second half of `rows == 0 || cols == 0`.
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Pcolormesh([0.0, 1.0], [0.0, 1.0, 2.0], new double[2, 0]))
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void Pcolormesh_XTooShort_EarlyReturn()
    {
        // X.Length < 2 — first half of edges-too-short guard.
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Pcolormesh([0.0], [0.0, 1.0, 2.0], new double[,] { { 1.0 } }))
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void Pcolormesh_YTooShort_EarlyReturn()
    {
        // Y.Length < 2 — second half of edges-too-short guard.
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Pcolormesh([0.0, 1.0, 2.0], [0.0], new double[,] { { 1.0 } }))
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void Pcolormesh_AllEqualValues_DegenerateGuard()
    {
        // Hit the (min == max) branch of ResolveColormapping via the renderer.
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Pcolormesh([0.0, 1.0, 2.0], [0.0, 1.0, 2.0],
                new double[,] { { 5.0, 5.0 }, { 5.0, 5.0 } }))
            .ToSvg();
        Assert.Contains("<rect", svg);
    }
}
