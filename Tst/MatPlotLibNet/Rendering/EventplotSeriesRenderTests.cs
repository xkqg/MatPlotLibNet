// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies SVG output of <see cref="EventplotSeries"/> rendering.</summary>
public class EventplotSeriesRenderTests
{
    private static readonly double[][] Positions = [[1.0, 2.0, 3.0], [4.0, 5.0]];

    [Fact]
    public void Eventplot_SvgContainsLines()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Eventplot(Positions))
            .ToSvg();
        Assert.Contains("<line", svg);
    }

    [Fact]
    public void Eventplot_FluentShortcut_ProducesSvg()
    {
        string svg = Plt.Create()
            .Eventplot(Positions)
            .ToSvg();
        Assert.Contains("<svg", svg);
    }
}
