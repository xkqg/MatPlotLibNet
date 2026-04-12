// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies SVG output of <see cref="Bar3DSeries"/> rendering.</summary>
public class Bar3DSeriesRenderTests
{
    private static readonly double[] X = [0.0, 1.0, 2.0];
    private static readonly double[] Y = [0.0, 0.0, 1.0];
    private static readonly double[] Z = [2.0, 3.5, 1.5];

    [Fact]
    public void Bar3D_RendersWithoutError()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Bar3D(X, Y, Z))
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void Bar3D_SvgContainsPolygons()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Bar3D(X, Y, Z))
            .ToSvg();
        Assert.True(svg.Contains("<polygon") || svg.Contains("<path"),
            "Expected SVG to contain filled polygon elements for bar faces");
    }

    [Fact]
    public void Bar3D_EmptyData_RendersWithoutError()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Bar3D([], [], []))
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void Bar3D_FluentShortcut_ProducesSvg()
    {
        string svg = Plt.Create()
            .Bar3D(X, Y, Z)
            .ToSvg();
        Assert.Contains("<svg", svg);
    }
}
