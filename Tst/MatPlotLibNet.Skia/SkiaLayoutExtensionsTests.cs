// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Skia.Tests;

public sealed class SkiaLayoutExtensionsTests
{
    [Fact]
    public void TheFirstPlotArea_OfAFigure_LiesInsideTheFigure()
    {
        var figure = Plt.Create().WithSize(600, 400).AddSubPlot(1, 1, 1, ax => ax.Plot([0.0, 1.0], [0.0, 1.0])).Build();

        var plot = figure.PlotAreaOnSkia();

        Assert.InRange(plot.X, 1, 300);
        Assert.InRange(plot.Right, 300, 600);
        Assert.InRange(plot.Bottom, 100, 399);
    }
}
