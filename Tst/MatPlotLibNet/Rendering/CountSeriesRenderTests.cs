// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies SVG output of <see cref="CountSeries"/> rendering.</summary>
public class CountSeriesRenderTests
{
    private static readonly string[] Values = ["a", "b", "a", "c", "a", "b"];

    [Fact]
    public void Countplot_RendersWithoutError()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Countplot(Values))
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void Countplot_SvgContainsRectangles()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Countplot(Values))
            .ToSvg();
        Assert.Contains("<rect", svg);
    }

    [Fact]
    public void Countplot_EmptyValues_RendersWithoutError()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Countplot([]))
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void Countplot_FluentShortcut_ProducesSvg()
    {
        string svg = Plt.Create()
            .Countplot(Values)
            .ToSvg();
        Assert.Contains("<svg", svg);
    }
}
