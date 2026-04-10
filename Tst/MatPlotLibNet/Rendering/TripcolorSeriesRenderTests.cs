// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies SVG output of <see cref="TripcolorSeries"/> rendering.</summary>
public class TripcolorSeriesRenderTests
{
    private static readonly double[] X = [0.0, 1.0, 0.5, 0.0, 1.0];
    private static readonly double[] Y = [0.0, 0.0, 1.0, 1.0, 1.0];
    private static readonly double[] Z = [1.0, 2.0, 3.0, 1.5, 2.5];

    [Fact]
    public void Tripcolor_RendersWithoutError()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Tripcolor(X, Y, Z))
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void Tripcolor_SvgContainsFilledPolygon()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Tripcolor(X, Y, Z))
            .ToSvg();
        Assert.True(svg.Contains("<polygon") || svg.Contains("<path"),
            "Expected SVG to contain filled triangle elements");
    }

    [Fact]
    public void Tripcolor_EmptyData_RendersWithoutError()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Tripcolor([], [], []))
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void Tripcolor_FluentShortcut_ProducesSvg()
    {
        string svg = Plt.Create()
            .Tripcolor(X, Y, Z)
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void Tripcolor_WithExplicitTriangles_RendersWithoutError()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Tripcolor(X, Y, Z, s => s.Triangles = new int[] { 0, 1, 2, 0, 2, 3 }))
            .ToSvg();
        Assert.Contains("<svg", svg);
    }
}
