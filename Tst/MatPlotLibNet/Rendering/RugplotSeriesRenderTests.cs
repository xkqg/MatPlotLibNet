// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies SVG output of <see cref="RugplotSeries"/> rendering.</summary>
public class RugplotSeriesRenderTests
{
    private static readonly double[] Data = [1.0, 2.0, 2.5, 3.0, 3.5, 4.0, 5.0];

    [Fact]
    public void Rugplot_RendersWithoutError()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Rugplot(Data))
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void Rugplot_SvgContainsLines()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Rugplot(Data))
            .ToSvg();
        Assert.Contains("<line", svg);
    }

    [Fact]
    public void Rugplot_EmptyData_RendersWithoutError()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Rugplot([]))
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void Rugplot_FluentShortcut_ProducesSvg()
    {
        string svg = Plt.Create()
            .Rugplot(Data)
            .ToSvg();
        Assert.Contains("<svg", svg);
    }
}
