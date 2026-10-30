// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>What a heatmap's cells are today, without an aspect: a cell of a 10 by 10 heatmap is as wide as the plot area
/// is wide over ten and as tall as it is tall over ten, so its shape is the plot area's, and the plot area is the shape of
/// the figure less its margins. These ratios (cell height over cell width) are what <c>SetAspect(1)</c> has to change and
/// what everything else has to leave alone.</summary>
public class HeatmapCellAspectTests
{
    private static double CellRatio(int width, int height)
    {
        var figure = Plt.Create().WithSize(width, height)
            .AddSubPlot(1, 1, 1, ax => ax.Heatmap(new double[10, 10]))
            .Build();
        var plot = new ChartRenderer().ComputeLayout(figure, new SvgRenderContext()).PlotAreas[0];
        var range = figure.SubPlots[0].ComputeDataRanges(figure.Theme);

        return plot.Height / (range.YMax - range.YMin) / (plot.Width / (range.XMax - range.XMin));
    }

    [Theory]
    [InlineData(600, 600, 0.994)]
    [InlineData(500, 350, 0.696)]
    [InlineData(900, 500, 0.552)]
    public void WithoutAnAspect_ACellHasTheShapeOfThePlotArea(int width, int height, double expected)
    {
        Assert.Equal(expected, CellRatio(width, height), 3);
    }
}
