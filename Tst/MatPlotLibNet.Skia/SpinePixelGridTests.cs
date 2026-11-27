// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;
using SkiaSharp;

namespace MatPlotLibNet.Skia.Tests;

/// <summary>A spine in a raster export stands on the pixel grid the way matplotlib's Agg backend puts it there. A stroke of
/// horizontal and vertical segments is moved to a whole pixel and, when its width rounds to an odd number of pixels, half a pixel
/// on, so its line fills one column or row exactly and the rest of its width spills evenly onto the two beside it. matplotlib 3.11
/// draws the left spine of <c>classic/line.png</c> (its axes at x = 100, a spine of 1 pt = 1.39 px) as the columns 99, 100 and 101 =
/// #999999, #000000 and #CDCDCD: a column of black with 19 percent of the line's colour on each side, never two columns of grey.
/// The spine of a Spine patch has projecting caps (<c>matplotlib.spines.Spine.__init__</c>), so the corners are solid. A line on the
/// pixel edge, as an unmoved one lands, is two columns of grey, which two platforms round differently.</summary>
public class SpinePixelGridTests
{
    private static readonly SKColor Black = new(0, 0, 0);

    private static Figure Classic(double dpi = 96) => Plt.Create().WithSize(800, 600).WithDpi(dpi).WithTheme(Theme.MatplotlibClassic)
        .AddSubPlot(1, 1, 1, ax => ax.Plot([0.0, 10.0], [0.0, 10.0]).SetXLim(0, 10).SetYLim(0, 10))
        .Build();

    private static int Snapped(double edge, double scale) => (int)Math.Floor(edge * scale + 0.5);

    [Theory]
    [InlineData(96, 1)]
    [InlineData(192, 2)]
    public void ASpine_FillsOneColumnOrRowOfThePixelGridWithItsColour(double dpi, int scale)
    {
        var figure = Classic(dpi);
        var plot = figure.PlotAreaOnSkia();
        using var picture = SKBitmap.Decode(figure.ToPng());
        int middleRow = Snapped(plot.Y + plot.Height / 2, scale);
        int middleColumn = Snapped(plot.X + plot.Width / 2, scale);

        Assert.Equal(Black, picture.GetPixel(Snapped(plot.X, scale), middleRow));
        Assert.Equal(Black, picture.GetPixel(Snapped(plot.X + plot.Width, scale), middleRow));
        Assert.Equal(Black, picture.GetPixel(middleColumn, Snapped(plot.Y, scale)));
        Assert.Equal(Black, picture.GetPixel(middleColumn, Snapped(plot.Y + plot.Height, scale)));
    }

    [Fact]
    public void ASpine_SpillsItsRestEvenlyOntoTheTwoColumnsBesideIt()
    {
        var figure = Classic();
        var plot = figure.PlotAreaOnSkia();
        using var picture = SKBitmap.Decode(figure.ToPng());
        int row = Snapped(plot.Y + plot.Height * 0.37, 1);   // between two ticks, which would draw over the column beside the spine
        int column = Snapped(plot.X, 1);

        var outside = picture.GetPixel(column - 1, row);
        var inside = picture.GetPixel(column + 1, row);

        // 1 pt is 1.39 px: a column of black and 0.19 px on each side, over the figure's grey outside and the white axes inside.
        Assert.InRange(outside.Red, 150, 160);   // 191 * (1 - 0.19) = 155
        Assert.InRange(inside.Red, 200, 210);    // 255 * (1 - 0.19) = 207
    }

    /// <summary>The top spine spills onto the rows above and below it as the left one does onto its columns: matplotlib's Agg covers
    /// each pixel by the area the line takes of it (<c>classic/line.png</c>: the rows beside the top spine are 19 percent black, as the
    /// columns beside the left one are). Skia covers the rows of a stroke in quarters of a pixel, so the rows beside a horizontal line
    /// were a quarter black where matplotlib's are 19 percent.</summary>
    [Fact]
    public void ASpine_SpillsItsRestEvenlyOntoTheTwoRowsBesideIt()
    {
        var figure = Classic();
        var plot = figure.PlotAreaOnSkia();
        using var picture = SKBitmap.Decode(figure.ToPng());
        int column = Snapped(plot.X + plot.Width * 0.37, 1);   // between two ticks
        int row = Snapped(plot.Y, 1);

        var outside = picture.GetPixel(column, row - 1);
        var inside = picture.GetPixel(column, row + 1);

        Assert.InRange(outside.Red, 150, 160);   // 191 * (1 - 0.19) = 155
        Assert.InRange(inside.Red, 200, 210);    // 255 * (1 - 0.19) = 207
    }

    [Fact]
    public void TheCornersOfTheFrame_AreSolid()
    {
        var figure = Classic();
        var plot = figure.PlotAreaOnSkia();
        using var picture = SKBitmap.Decode(figure.ToPng());

        Assert.Equal(Black, picture.GetPixel(Snapped(plot.X, 1), Snapped(plot.Y, 1)));
        Assert.Equal(Black, picture.GetPixel(Snapped(plot.X + plot.Width, 1), Snapped(plot.Y, 1)));
        Assert.Equal(Black, picture.GetPixel(Snapped(plot.X, 1), Snapped(plot.Y + plot.Height, 1)));
        Assert.Equal(Black, picture.GetPixel(Snapped(plot.X + plot.Width, 1), Snapped(plot.Y + plot.Height, 1)));
    }

    [Fact]
    public void ATheme_ThatDoesNotAskForIt_LeavesTheSpineWhereTheLayoutPutIt()
    {
        var figure = Plt.Create().WithSize(800, 600).WithTheme(Theme.Default)
            .AddSubPlot(1, 1, 1, ax => ax.Plot([0.0, 10.0], [0.0, 10.0]).SetXLim(0, 10).SetYLim(0, 10))
            .Build();
        var plot = figure.PlotAreaOnSkia();
        using var picture = SKBitmap.Decode(figure.ToPng());
        int row = Snapped(plot.Y + plot.Height * 0.37, 1);
        int column = Snapped(plot.X, 1);

        // 0.8 px centred on the pixel edge at the plot's left: 0.4 px on each of the two columns around it.
        Assert.InRange(Math.Abs(picture.GetPixel(column - 1, row).Red - picture.GetPixel(column, row).Red), 0, 2);
    }

    [Fact]
    public void AContextMadeForAnythingButARasterExport_LeavesTheSpineWhereItStands()
    {
        var figure = Classic();
        var plot = figure.PlotAreaOnSkia();
        using var bitmap = new SKBitmap(800, 600);
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(SKColors.White);
            new ChartRenderer().Render(figure, new SkiaRenderContext(canvas));
        }

        // The left spine is centred on the pixel edge at the plot's left, so no column of it is fully black.
        Assert.NotEqual(Black, bitmap.GetPixel(Snapped(plot.X, 1), Snapped(plot.Y + plot.Height / 2, 1)));
    }
}
