// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies SVG output of <see cref="StripplotSeries"/> rendering.</summary>
public class StripplotSeriesRenderTests
{
    private static readonly double[][] Datasets = [[1.0, 2.0, 3.0], [4.0, 5.0, 6.0]];

    [Fact]
    public void Stripplot_RendersWithoutError()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Stripplot(Datasets))
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void Stripplot_SvgContainsCircles()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Stripplot(Datasets))
            .ToSvg();
        Assert.Contains("<circle", svg);
    }

    [Fact]
    public void Stripplot_EmptyDatasets_RendersWithoutError()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Stripplot([]))
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void Stripplot_FluentShortcut_ProducesSvg()
    {
        string svg = Plt.Create()
            .Stripplot(Datasets)
            .ToSvg();
        Assert.Contains("<svg", svg);
    }
}
