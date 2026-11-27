// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Xml.Linq;
using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;
using SkiaSharp;

namespace MatPlotLibNet.Skia.Tests;

/// <summary>A legend's frame in a raster export stands on the pixel grid the way matplotlib's Agg backend puts it there. matplotlib
/// 3.11.2: the frame (<c>legend.get_frame()</c>) is drawn at <c>patch.linewidth</c>, 1 pt, with <c>get_snap()</c> True and a mitred
/// join, so under the classic style its top edge is one row of black with 19 percent of it on each side, as in <c>classic/ind_sma.png</c>
/// (the rows over the frame: #CDCDCD, #000000, #CDCDCD). The library's own themes keep their half-pixel frame where it falls.</summary>
public class LegendFramePixelGridTests
{
    private static Figure Legend(Theme theme) => Plt.Create().WithSize(800, 600).WithTheme(theme)
        .AddSubPlot(1, 1, 1, ax => ax.Plot([0.0, 1.0], [0.0, 1.0], s => s.Label = "series").WithLegend(l => l with { Position = LegendPosition.UpperRight }))
        .Build();

    /// <summary>The middle column of the frame and the first row of the picture with ink in it above the frame's middle, as the SVG of the
    /// same figure places the frame.</summary>
    private static (int Column, int Row) TopEdge(Figure figure, SKBitmap picture)
    {
        var frame = figure.ToSvg().Xml().Descendants()
            .First(e => (string?)e.Attribute("class") == "legend").Elements().First(e => e.Name.LocalName is "rect" or "path");
        int column = (int)(frame.Number("x") + frame.Number("width") / 2);
        int row = Enumerable.Range((int)frame.Number("y") - 3, 6).First(y => picture.GetPixel(column, y).Red < 250);
        return (column, row);
    }

    [Fact]
    public void UnderClassic_TheTopOfTheFrame_IsOneRowOfBlack_WithItsRestOnTheRowsBesideIt()
    {
        var figure = Legend(Theme.MatplotlibClassic);
        using var picture = SKBitmap.Decode(figure.ToPng());
        var (column, row) = TopEdge(figure, picture);

        Assert.InRange(picture.GetPixel(column, row).Red, 200, 210);       // 255 * (1 - 0.19) = 207
        Assert.Equal(0, picture.GetPixel(column, row + 1).Red);
        Assert.InRange(picture.GetPixel(column, row + 2).Red, 200, 210);
    }
}
