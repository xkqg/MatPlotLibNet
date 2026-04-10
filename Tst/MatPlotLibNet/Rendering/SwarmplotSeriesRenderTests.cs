// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies SVG output of <see cref="SwarmplotSeries"/> rendering.</summary>
public class SwarmplotSeriesRenderTests
{
    private static readonly double[][] Datasets = [[1.0, 2.0, 2.0, 3.0], [4.0, 5.0, 5.0]];

    [Fact]
    public void Swarmplot_RendersWithoutError()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Swarmplot(Datasets))
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void Swarmplot_SvgContainsCircles()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Swarmplot(Datasets))
            .ToSvg();
        Assert.Contains("<circle", svg);
    }

    [Fact]
    public void Swarmplot_EmptyDatasets_RendersWithoutError()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Swarmplot([]))
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void Swarmplot_FluentShortcut_ProducesSvg()
    {
        string svg = Plt.Create()
            .Swarmplot(Datasets)
            .ToSvg();
        Assert.Contains("<svg", svg);
    }
}
