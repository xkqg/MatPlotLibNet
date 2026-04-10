// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies SVG output of <see cref="ContourfSeries"/> rendering.</summary>
public class ContourfRenderTests
{
    // 3x3 grid with clear gradient for visible contour bands
    private static readonly double[] X = [0.0, 1.0, 2.0];
    private static readonly double[] Y = [0.0, 1.0, 2.0];
    private static readonly double[,] Z = { { 0, 1, 2 }, { 1, 2, 3 }, { 2, 3, 4 } };

    /// <summary>ContourfSeries renders to SVG without throwing.</summary>
    [Fact]
    public void Contourf_RendersWithoutError()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Contourf(X, Y, Z))
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    /// <summary>SVG output contains polygon elements (filled bands).</summary>
    [Fact]
    public void Contourf_SvgContainsPolygons()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Contourf(X, Y, Z))
            .ToSvg();
        Assert.Contains("<polygon", svg);
    }

    /// <summary>ShowLines=true adds polyline iso-line overlays.</summary>
    [Fact]
    public void Contourf_ShowLines_AddsPolylines()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Contourf(X, Y, Z, s => s.ShowLines = true))
            .ToSvg();
        Assert.Contains("<polyline", svg);
    }

    /// <summary>ShowLines=false omits polyline iso-lines but polygons still present.</summary>
    [Fact]
    public void Contourf_ShowLinesFalse_PolygonsPresent()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Contourf(X, Y, Z, s => s.ShowLines = false))
            .ToSvg();
        Assert.Contains("<polygon", svg);
    }

    /// <summary>Alpha < 1 produces an opacity attribute in the SVG.</summary>
    [Fact]
    public void Contourf_Alpha_ProducesOpacityAttribute()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Contourf(X, Y, Z, s => s.Alpha = 0.5))
            .ToSvg();
        Assert.Contains("opacity", svg);
    }

    /// <summary>Contourf via fluent FigureBuilder shortcut produces SVG.</summary>
    [Fact]
    public void Contourf_FluentShortcut_ProducesSvg()
    {
        string svg = Plt.Create()
            .Contourf(X, Y, Z)
            .ToSvg();
        Assert.Contains("<svg", svg);
    }
}
