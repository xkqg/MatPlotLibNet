// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;

namespace MatPlotLibNet.Tests.Builders;

/// <summary>The verbs that lay data out as the cells of a grid (a spy plot, a calendar heatmap, a matrix shown) draw square
/// cells: the plot has an aspect of one, so the cells are square at any figure size, as the reference libraries draw them.</summary>
public class GridVerbAspectTests
{
    private static Figure Spy(int width, int height) => Plt.Create().WithSize(width, height)
        .AddSubPlot(1, 1, 1, ax => ax.Spy(Diagonal(8, 20))).Build();

    private static Figure Calendar(int width, int height)
    {
        var first = new DateTime(2025, 1, 1);
        DateTime[] days = [.. Enumerable.Range(0, 365).Select(day => first.AddDays(day))];
        double[] values = [.. days.Select(day => (double)(day.DayOfYear * 37 % 11))];
        return Plt.Create().WithSize(width, height).AddSubPlot(1, 1, 1, ax => ax.CalendarHeatmap(days, values)).Build();
    }

    private static double[,] Diagonal(int rows, int columns)
    {
        var matrix = new double[rows, columns];
        for (int r = 0; r < rows; r++) matrix[r, r % columns] = 1;
        return matrix;
    }

    private static double UnitsPerPixelRatio(Figure figure)
    {
        var plot = new ChartRenderer().ComputeLayout(figure, new SvgRenderContext()).PlotAreas[0];
        var range = figure.SubPlots[0].ComputeDataRanges(figure.Theme);
        return Math.Abs((range.XMax - range.XMin) / plot.Width) / Math.Abs((range.YMax - range.YMin) / plot.Height);
    }

    [Theory]
    [InlineData(500, 500)]
    [InlineData(900, 400)]
    [InlineData(400, 900)]
    public void ASpyPlot_HasSquareCells_AtAnyFigureSize(int width, int height)
    {
        Assert.Equal(1.0, UnitsPerPixelRatio(Spy(width, height)), 9);
    }

    [Theory]
    [InlineData(1100, 300)]
    [InlineData(700, 700)]
    [InlineData(500, 900)]
    public void ACalendarHeatmap_HasSquareCells_AtAnyFigureSize(int width, int height)
    {
        Assert.Equal(1.0, UnitsPerPixelRatio(Calendar(width, height)), 9);
    }

    [Fact]
    public void TheAspectIsOne_OnBothVerbs()
    {
        Assert.Equal(1.0, Spy(500, 500).SubPlots[0].Aspect);
        Assert.Equal(1.0, Calendar(1100, 300).SubPlots[0].Aspect);
    }
}
