// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies SVG output of <see cref="HexbinSeries"/> rendering.</summary>
public class HexbinSeriesRenderTests
{
    private static readonly double[] X = Enumerable.Range(0, 30).Select(i => (double)(i % 6)).ToArray();
    private static readonly double[] Y = Enumerable.Range(0, 30).Select(i => (double)(i / 6)).ToArray();

    /// <summary>Hexbin renders to SVG without throwing.</summary>
    [Fact]
    public void Hexbin_RendersWithoutError()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Hexbin(X, Y))
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    /// <summary>SVG contains polygon elements for hex cells.</summary>
    [Fact]
    public void Hexbin_SvgContainsPolygons()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Hexbin(X, Y))
            .ToSvg();
        Assert.Contains("<polygon", svg);
    }

    /// <summary>Empty data renders without throwing.</summary>
    [Fact]
    public void Hexbin_EmptyData_RendersWithoutError()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Hexbin([], []))
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    /// <summary>Custom GridSize renders without error.</summary>
    [Fact]
    public void Hexbin_CustomGridSize_RendersWithoutError()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Hexbin(X, Y, s => s.GridSize = 5))
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    /// <summary>JSON round-trip preserves type tag "hexbin".</summary>
    [Fact]
    public void Hexbin_JsonRoundTrip_PreservesType()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Hexbin(X, Y))
            .Build();
        var json = new MatPlotLibNet.Serialization.ChartSerializer().ToJson(figure);
        Assert.Contains("\"type\":\"hexbin\"", json);

        var restored = new MatPlotLibNet.Serialization.ChartSerializer().FromJson(json);
        var series = restored.SubPlots[0].Series.OfType<HexbinSeries>().FirstOrDefault();
        Assert.NotNull(series);
    }
}
