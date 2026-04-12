// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies SVG output of <see cref="TricontourSeries"/> rendering.</summary>
public class TricontourSeriesRenderTests
{
    private static readonly double[] X = [0.0, 1.0, 0.5, 1.0, 0.0, 0.5];
    private static readonly double[] Y = [0.0, 0.0, 0.5, 1.0, 1.0, 0.25];
    private static readonly double[] Z = [1.0, 2.0, 3.0, 2.0, 1.0, 2.5];

    [Fact]
    public void Tricontour_RendersWithoutError()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Tricontour(X, Y, Z))
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void Tricontour_SvgContainsPolylineOrPath()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Tricontour(X, Y, Z))
            .ToSvg();
        Assert.True(svg.Contains("<polyline") || svg.Contains("<path") || svg.Contains("<line"),
            "Expected SVG to contain contour line elements");
    }

    [Fact]
    public void Tricontour_EmptyData_RendersWithoutError()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Tricontour([], [], []))
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void Tricontour_FluentShortcut_ProducesSvg()
    {
        string svg = Plt.Create()
            .Tricontour(X, Y, Z)
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void Tricontour_CustomLevels_RendersWithoutError()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Tricontour(X, Y, Z, s => s.Levels = 5))
            .ToSvg();
        Assert.Contains("<svg", svg);
    }
}
