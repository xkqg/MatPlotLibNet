// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;
using SkiaSharp;

namespace MatPlotLibNet.Skia.Tests;

/// <summary>An end-of-line label reaches the raster output: the Skia context draws it, in the line's colour, in the
/// column right of the plot area, where nothing else of the figure is painted.</summary>
public sealed class DirectLabelSkiaTests
{
    private static Models.Figure Figure(bool direct) => Plt.Create().WithSize(500, 300)
        .AddSubPlot(1, 1, 1, ax =>
        {
            ax.Plot([0.0, 1.0, 2.0], [1.0, 3.0, 2.0], s => { s.Label = "Revenue"; s.Color = Colors.Red; });
            if (direct) ax.WithDirectLabel();
            ax.WithLegend(l => l with { Visible = false });
        })
        .Build();

    /// <summary>Red-dominant pixels right of the plot area: the label's ink, anti-aliased against white.</summary>
    private static int RedInkRightOf(Models.Figure figure)
    {
        using var bitmap = new SKBitmap((int)figure.Width, (int)figure.Height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.White);
        var ctx = new SkiaRenderContext(canvas);
        var area = new ChartRenderer().ComputeLayout(figure, ctx).PlotAreas[0];

        new ChartRenderer().Render(figure, ctx);
        canvas.Flush();

        int count = 0;
        for (int x = (int)Math.Ceiling(area.X + area.Width) + 2; x < bitmap.Width; x++)
            for (int y = 0; y < bitmap.Height; y++)
            {
                var p = bitmap.GetPixel(x, y);
                if (p.Red > 150 && p.Green < 110 && p.Blue < 110) count++;
            }
        return count;
    }

    [Fact]
    public void ADirectlyLabelledLine_IsNamedInItsColour_RightOfThePlot()
    {
        Assert.True(RedInkRightOf(Figure(direct: true)) > 10, "no red ink right of the plot: the label was not painted");
        Assert.Equal(0, RedInkRightOf(Figure(direct: false)));
    }
}
