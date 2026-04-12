// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies SVG output of <see cref="BrokenBarSeries"/> rendering.</summary>
public class BrokenBarSeriesRenderTests
{
    private static readonly (double, double)[][] Ranges = [[(1.0, 2.0), (4.0, 1.5)], [(0.5, 3.0)]];

    [Fact]
    public void BrokenBar_RendersWithoutError()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.BrokenBarH(Ranges))
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void BrokenBar_SvgContainsRectangles()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.BrokenBarH(Ranges))
            .ToSvg();
        Assert.Contains("<rect", svg);
    }

    [Fact]
    public void BrokenBar_EmptyRanges_RendersWithoutError()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.BrokenBarH([]))
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void BrokenBar_FluentShortcut_ProducesSvg()
    {
        string svg = Plt.Create()
            .BrokenBarH(Ranges)
            .ToSvg();
        Assert.Contains("<svg", svg);
    }
}
