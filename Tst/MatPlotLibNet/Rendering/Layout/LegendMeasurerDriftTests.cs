// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.LegendRendering;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Rendering.Svg;

namespace MatPlotLibNet.Tests.Rendering.Layout;

/// <summary>
/// The margin an outside legend reserves (<see cref="LegendMeasurer.MeasureBox"/>) is the box that is drawn
/// (<see cref="LegendLayoutCalculator.Compute(Legend, IReadOnlyList{string}, Rect)"/>): one size, whatever the font, the title,
/// the columns or the position. A measurer that works the size out its own way reserves a different room than the
/// legend takes and clips or leaves a gap.
/// </summary>
public class LegendMeasurerDriftTests
{
    [Theory]
    [InlineData(LegendPosition.OutsideBottom, 20.0, "T", 1)]    // the title font took the theme's size in one and the legend's in the other
    [InlineData(LegendPosition.OutsideRight, null, null, 1)]
    [InlineData(LegendPosition.UpperRight, 11.0, "Title", 2)]
    [InlineData(LegendPosition.OutsideTop, 20.0, null, 3)]
    [InlineData(LegendPosition.Best, 16.0, "Signals", 1)]
    [InlineData(LegendPosition.OutsideLeft, 9.0, "A longer legend title", 2)]
    public void TheMeasuredBox_IsTheBoxTheCalculatorLaysOut(LegendPosition position, double? fontSize, string? title, int columns)
    {
        var figure = Plt.Create().WithSize(600, 400)
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([0.0, 1.0], [0.0, 1.0], s => s.Label = "alpha")
                .Plot([0.0, 1.0], [1.0, 0.0], s => s.Label = "beta gamma")
                .Plot([0.0, 1.0], [0.5, 0.5], s => s.Label = "delta")
                .WithLegend(l => l with { Position = position, FontSize = fontSize, Title = title, NCols = columns }))
            .Build();
        var axes = figure.SubPlots[0];
        var ctx = new SvgRenderContext();

        var measured = LegendMeasurer.MeasureBox(axes, ctx, figure.Theme);
        var laidOut = new LegendLayoutCalculator(figure.Theme, ctx)
            .Compute(axes.Legend, axes.LegendEntries(false).Select(e => e.Label).ToList(), new Rect(0, 0, 500, 300));

        Assert.Equal(laidOut.BoxWidth, measured.Width, 9);
        Assert.Equal(laidOut.BoxHeight, measured.Height, 9);
    }
}
