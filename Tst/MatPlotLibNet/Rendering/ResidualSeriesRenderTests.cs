// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies SVG output of <see cref="ResidualSeries"/> rendering.</summary>
public class ResidualSeriesRenderTests
{
    private static readonly double[] X = [1.0, 2.0, 3.0, 4.0, 5.0];
    private static readonly double[] Y = [2.1, 3.9, 6.1, 7.9, 10.1];

    [Fact]
    public void Residual_RendersWithoutError()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Residplot(X, Y))
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void Residual_SvgContainsCircles()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Residplot(X, Y))
            .ToSvg();
        Assert.Contains("<circle", svg);
    }

    [Fact]
    public void Residual_ShowZeroLine_ContainsLine()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Residplot(X, Y, s => s.ShowZeroLine = true))
            .ToSvg();
        Assert.Contains("<line", svg);
    }

    [Fact]
    public void Residual_ShowZeroLineFalse_RendersWithoutError()
    {
        // Verifies that disabling the zero line does not cause a rendering error
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Residplot(X, Y, s => s.ShowZeroLine = false))
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void Residual_FluentShortcut_ProducesSvg()
    {
        string svg = Plt.Create()
            .Residplot(X, Y)
            .ToSvg();
        Assert.Contains("<svg", svg);
    }
}
