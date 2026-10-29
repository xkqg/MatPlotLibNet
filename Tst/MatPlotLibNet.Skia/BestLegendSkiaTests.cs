// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using SkiaSharp;

namespace MatPlotLibNet.Skia.Tests;

/// <summary>A Best legend stands where the marks are fewest whichever context lays the chart out: the vector one and the Skia
/// one measure the legend's text a little differently, and the place both choose is the same.</summary>
public sealed class BestLegendSkiaTests
{
    private static Models.Figure RisingLine()
    {
        double[] ramp = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
        return Plt.Create().WithSize(500, 350)
            .AddSubPlot(1, 1, 1, ax => ax.Plot(ramp, ramp, s => s.Label = "signal").WithLegend())
            .Build();
    }

    [Fact]
    public void ABestLegend_OverARisingLine_StandsInTheUpperLeft_UnderSvgAndSkiaAlike()
    {
        var figure = RisingLine();
        using var bitmap = new SKBitmap((int)figure.Width, (int)figure.Height);
        using var canvas = new SKCanvas(bitmap);

        var skia = new ChartRenderer().ComputeLayout(figure, new SkiaRenderContext(canvas));
        var svg = new ChartRenderer().ComputeLayout(figure, new MatPlotLibNet.Rendering.Svg.SvgRenderContext());

        foreach (var layout in new[] { skia, svg })
        {
            var plot = layout.PlotAreas[0];
            var entry = Assert.Single(layout.LegendItems[0]).Bounds;
            Assert.True(entry.X < plot.X + plot.Width / 2, $"the legend entry stands at x {entry.X}, right of the middle of the plot area");
            Assert.True(entry.Y < plot.Y + plot.Height / 2, $"the legend entry stands at y {entry.Y}, below the middle of the plot area");
        }
    }
}
