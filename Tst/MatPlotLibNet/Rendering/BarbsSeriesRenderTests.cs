// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies SVG output of <see cref="BarbsSeries"/> rendering.</summary>
public class BarbsSeriesRenderTests
{
    private static readonly double[] X = [0.0, 1.0, 2.0, 3.0];
    private static readonly double[] Y = [0.0, 0.0, 0.0, 0.0];
    private static readonly double[] Speed = [5.0, 15.0, 25.0, 55.0];
    private static readonly double[] Dir = [0.0, 45.0, 90.0, 270.0];

    [Fact]
    public void Barbs_RendersWithoutError()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Barbs(X, Y, Speed, Dir))
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void Barbs_SvgContainsLines()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Barbs(X, Y, Speed, Dir))
            .ToSvg();
        Assert.Contains("<line", svg);
    }

    [Fact]
    public void Barbs_EmptyData_RendersWithoutError()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Barbs([], [], [], []))
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void Barbs_FluentShortcut_ProducesSvg()
    {
        string svg = Plt.Create()
            .Barbs(X, Y, Speed, Dir)
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void Barbs_SinglePoint_RendersWithoutError()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Barbs([0.0], [0.0], [10.0], [45.0]))
            .ToSvg();
        Assert.Contains("<svg", svg);
    }
}
