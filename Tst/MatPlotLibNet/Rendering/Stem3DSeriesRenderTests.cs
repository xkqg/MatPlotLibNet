// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies SVG output of <see cref="Stem3DSeries"/> rendering.</summary>
public class Stem3DSeriesRenderTests
{
    private static readonly double[] X = [0.0, 1.0, 2.0];
    private static readonly double[] Y = [0.0, 1.0, 0.0];
    private static readonly double[] Z = [1.0, 2.0, 1.5];

    [Fact]
    public void Stem3D_RendersWithoutError()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Stem3D(X, Y, Z))
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void Stem3D_SvgContainsLinesAndCircles()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Stem3D(X, Y, Z))
            .ToSvg();
        Assert.True(svg.Contains("<line") || svg.Contains("<circle"),
            "Expected SVG to contain stem line and/or marker elements");
    }

    [Fact]
    public void Stem3D_EmptyData_RendersWithoutError()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Stem3D([], [], []))
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void Stem3D_FluentShortcut_ProducesSvg()
    {
        string svg = Plt.Create()
            .Stem3D(X, Y, Z)
            .ToSvg();
        Assert.Contains("<svg", svg);
    }
}
