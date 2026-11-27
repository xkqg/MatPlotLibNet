// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Interaction.Modifiers;

/// <summary>A figure and the layout a pointer is mapped through, which the interaction tests hold a modifier or a controller to.</summary>
/// <param name="Figure">The figure.</param>
/// <param name="Layout">Its layout.</param>
public readonly record struct FigureLayout(Figure Figure, IChartLayout Layout)
{
    /// <summary>The pixel rectangle the one plot area of the figures below fills.</summary>
    public static Rect PlotArea { get; } = new(10, 10, 100, 50);

    /// <summary>A figure of one line over the x range 0 to 10 and the y range 0 to 5, laid out in <see cref="PlotArea"/>.</summary>
    public static FigureLayout Plain()
    {
        var figure = Plt.Create().Plot([1.0, 2.0], [3.0, 4.0]).Build();
        figure.SubPlots[0].XAxis.Min = 0;
        figure.SubPlots[0].XAxis.Max = 10;
        figure.SubPlots[0].YAxis.Min = 0;
        figure.SubPlots[0].YAxis.Max = 5;
        return new FigureLayout(figure, ChartLayout.Create(figure, [PlotArea]));
    }

    /// <summary>A figure of one line over the x range 0 to 100 and the y range 0 to 50, laid out in a pixel rectangle of 200 by 100 at
    /// (10, 10), under the chart id <c>c1</c> the modifiers of these tests are made with: the plot a drag across most of it is made over.</summary>
    public static FigureLayout Wide()
    {
        var figure = Plt.Create().Plot([1.0, 2.0], [3.0, 4.0]).Build();
        figure.ChartId = "c1";
        figure.SubPlots[0].XAxis.Min = 0; figure.SubPlots[0].XAxis.Max = 100;
        figure.SubPlots[0].YAxis.Min = 0; figure.SubPlots[0].YAxis.Max = 50;
        return new FigureLayout(figure, ChartLayout.Create(figure, [new Rect(10, 10, 200, 100)]));
    }

    /// <summary>A figure of one line with a second line on a right Y axis over 0 to 500, laid out in <see cref="PlotArea"/>: the plot whose
    /// secondary axis a gesture must move or reset to its own range.</summary>
    public static FigureLayout WithARightAxis()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2.0], [3.0, 4.0])
                .WithAddRightYAxis(y2 => y2.SetYLim(0, 500).Plot([1.0, 2.0], [100.0, 200.0])))
            .Build();
        return new FigureLayout(figure, ChartLayout.Create(figure, [PlotArea]));
    }

    /// <summary>A figure of one three-dimensional plot with a scatter point, laid out in <see cref="PlotArea"/>.</summary>
    public static FigureLayout ThreeD()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Scatter3D(new double[] { 1.0 }, new double[] { 2.0 }, new double[] { 3.0 }))
            .Build();
        return new FigureLayout(figure, ChartLayout.Create(figure, [PlotArea]));
    }
}
