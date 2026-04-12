// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies SVG output of <see cref="PolarHeatmapSeries"/> rendering.</summary>
public class PolarHeatmapRenderTests
{
    private static double[,] MakeData(int thetaBins = 8, int rBins = 4)
    {
        var d = new double[thetaBins, rBins];
        for (int t = 0; t < thetaBins; t++)
            for (int r = 0; r < rBins; r++)
                d[t, r] = t + r * 0.5;
        return d;
    }

    [Fact]
    public void PolarHeatmap_RendersWithoutError()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.PolarHeatmap(MakeData(), 8, 4))
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void PolarHeatmap_SvgContainsPolygonElements()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.PolarHeatmap(MakeData(), 8, 4))
            .ToSvg();
        Assert.Contains("<polygon", svg);
    }

    [Fact]
    public void PolarHeatmap_WithColorMap_RendersWithoutError()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.PolarHeatmap(MakeData(), 8, 4,
                s => s.ColorMap = ColorMapRegistry.Get("plasma")))
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void PolarHeatmap_SingleCell_RendersWithoutError()
    {
        var data = new double[1, 1] { { 1.0 } };
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.PolarHeatmap(data, 1, 1))
            .ToSvg();
        Assert.Contains("<svg", svg);
    }
}
