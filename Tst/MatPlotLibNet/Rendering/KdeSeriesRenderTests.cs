// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies SVG output of <see cref="KdeSeries"/> rendering.</summary>
public class KdeSeriesRenderTests
{
    private static readonly double[] Data = [1.0, 2.0, 2.5, 3.0, 3.5, 4.0, 5.0];

    /// <summary>KDE renders to SVG without throwing.</summary>
    [Fact]
    public void Kde_RendersWithoutError()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Kde(Data))
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    /// <summary>SVG contains a polyline element (the density curve).</summary>
    [Fact]
    public void Kde_SvgContainsPolyline()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Kde(Data))
            .ToSvg();
        Assert.Contains("<polyline", svg);
    }

    /// <summary>Fill=true produces a polygon element for the shaded area.</summary>
    [Fact]
    public void Kde_Fill_True_ContainsPolygon()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Kde(Data, s => s.Fill = true))
            .ToSvg();
        Assert.Contains("<polygon", svg);
    }

    /// <summary>Fill=false omits the polygon element.</summary>
    [Fact]
    public void Kde_Fill_False_NoPolygon()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Kde(Data, s => s.Fill = false))
            .ToSvg();
        Assert.DoesNotContain("<polygon", svg);
    }

    /// <summary>Empty data renders without throwing.</summary>
    [Fact]
    public void Kde_EmptyData_RendersWithoutError()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Kde([]))
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    /// <summary>Explicit bandwidth is accepted without error.</summary>
    [Fact]
    public void Kde_ExplicitBandwidth_RendersWithoutError()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Kde(Data, s => s.Bandwidth = 0.5))
            .ToSvg();
        Assert.Contains("<polyline", svg);
    }

    /// <summary>FigureBuilder shortcut Kde() produces valid SVG.</summary>
    [Fact]
    public void Kde_FluentShortcut_ProducesSvg()
    {
        string svg = Plt.Create()
            .Kde(Data)
            .ToSvg();
        Assert.Contains("<svg", svg);
    }
}
