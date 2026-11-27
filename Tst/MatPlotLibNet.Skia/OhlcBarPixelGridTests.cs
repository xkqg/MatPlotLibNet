// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;
using SkiaSharp;

namespace MatPlotLibNet.Skia.Tests;

/// <summary>The lines of an OHLC bar in a raster export stand on the pixel grid the way matplotlib's Agg backend puts a
/// <c>vlines</c> and an <c>hlines</c> there. A <c>LineCollection</c> of horizontal and vertical segments, drawn at <c>lines.linewidth</c>
/// (1 pt under the classic style, 1.39 px at 100 dpi), goes on a whole pixel, half a pixel on for an odd width: matplotlib 3.11.2 draws
/// the lines of <c>classic/ohlc_bar.png</c> as one column (or row) of the full colour with #FFCDCD on each side, wherever the price
/// falls. Off the grid the same width covers two columns in part and often no column fully.</summary>
public class OhlcBarPixelGridTests
{
    internal const int Bars = 37;
    private static readonly SKColor Blue = new(0, 0, 255);

    /// <summary>Several bars that rose, all with the same range 1 to 9 and each with its own open and close (so each tick stands at
    /// a different fraction of a pixel), over an X range that puts every bar at a different fraction of a pixel too.</summary>
    private static (Figure Figure, Rect Plot) Several(Theme theme)
    {
        double[] high = [.. Enumerable.Repeat(9.0, Bars)], low = [.. Enumerable.Repeat(1.0, Bars)];
        double[] open = [.. Enumerable.Range(0, Bars).Select(i => 3.1 + i * 0.0731)];
        double[] close = [.. open.Select(v => v + 1.37)];
        var figure = Plt.Create().WithSize(800, 600).WithTheme(theme)
            .AddSubPlot(1, 1, 1, ax => ax
                .OhlcBar(open, high, low, close, null, s => s.UpColor = Colors.Blue)
                .SetXLim(-0.5, Bars - 0.5).SetYLim(0, 10))
            .Build();
        return (figure, figure.PlotAreaOnSkia());
    }

    private static double XOf(Rect plot, int bar) => plot.X + (bar + 0.5) * plot.Width / Bars;

    /// <summary>The number of bars whose high-low line has a column of exactly the series colour in the row at a price of 2.</summary>
    internal static int BarsWhoseLineHasAFullColumn(Theme theme)
    {
        var (figure, plot) = Several(theme);
        using var picture = SKBitmap.Decode(figure.ToPng());
        int row = (int)(plot.Y + plot.Height * 0.8);
        int full = 0;
        for (int bar = 0; bar < Bars; bar++)
        {
            int x = (int)Math.Round(XOf(plot, bar));
            if (Enumerable.Range(x - 2, 5).Any(c => picture.GetPixel(c, row) == Blue)) full++;
        }

        return full;
    }

    [Fact]
    public void EveryOpenAndCloseTick_FillsARowOfThePixelGridWithItsColour_UnderTheClassicTheme()
    {
        var (figure, plot) = Several(Theme.MatplotlibClassic);
        using var picture = SKBitmap.Decode(figure.ToPng());
        for (int bar = 0; bar < Bars; bar++)
        {
            double open = 3.1 + bar * 0.0731, close = open + 1.37;
            int x = (int)Math.Round(XOf(plot, bar));
            foreach (var (price, column) in new[] { (open, x - 3), (close, x + 3) })
            {
                int centre = (int)Math.Round(plot.Y + plot.Height * (1 - price / 10));
                Assert.True(Enumerable.Range(centre - 2, 5).Any(r => picture.GetPixel(column, r) == Blue),
                    $"bar {bar}: no row of the full colour at {price:F3} (column {column}).");
            }
        }
    }

}
