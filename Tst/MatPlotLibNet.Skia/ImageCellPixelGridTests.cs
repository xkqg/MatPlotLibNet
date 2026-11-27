// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;
using SkiaSharp;

namespace MatPlotLibNet.Skia.Tests;

/// <summary>An image's cells in a raster export are one fill of the cell grid, as matplotlib draws an image: <c>imshow</c> resamples the
/// image to the pixels it covers, so each pixel takes the colour of the cell its centre falls in and no pixel is a blend of two cells or of a
/// cell and the ground. Cells drawn one rectangle each, antialiased, meet inside a pixel wherever an edge falls between two pixels: a faint
/// lattice of lighter lines over the whole image. The matplotlib themes ask for it (<see cref="RasterLook.StrokesOnPixelGrid"/>); the
/// library's own themes leave the cells where they stand, as they leave a heatmap's.</summary>
public class ImageCellPixelGridTests
{
    /// <summary>A 7 x 9 image of one value: every cell the low end of viridis.</summary>
    private static double[,] OneValue()
    {
        var data = new double[7, 9];
        return data;
    }

    private static Figure ImageOf(Theme theme) => Plt.Create().WithSize(800, 600).WithTheme(theme)
        .AddSubPlot(1, 1, 1, ax => ax.Image(OneValue(), s => { s.ColorMap = ColorMaps.Viridis; s.Interpolation = "nearest"; }))
        .Build();

    /// <summary>The colours of every pixel of the image ten pixels in from its edges, clear of the classic style's ticks, which point in
    /// over it.</summary>
    private static HashSet<SKColor> InsideColours(Theme theme)
    {
        var figure = ImageOf(theme);
        var plot = figure.PlotAreaOnSkia();
        using var picture = SKBitmap.Decode(figure.ToPng());
        var colours = new HashSet<SKColor>();
        for (int y = (int)Math.Ceiling(plot.Y) + 10; y < (int)(plot.Y + plot.Height) - 10; y++)
            for (int x = (int)Math.Ceiling(plot.X) + 10; x < (int)(plot.X + plot.Width) - 10; x++)
                colours.Add(picture.GetPixel(x, y));
        return colours;
    }

    private static SKColor Low => new(ColorMaps.Viridis.GetColor(0).R, ColorMaps.Viridis.GetColor(0).G, ColorMaps.Viridis.GetColor(0).B);

    [Theory]
    [InlineData("v2")]
    [InlineData("classic")]
    public void MatplotlibThemes_PaintEveryPixelOfTheImageInItsCellsColour_WithNoSeam(string themeId)
    {
        var theme = themeId.AsTheme();

        Assert.Equal([Low], InsideColours(theme));
    }

    [Fact]
    public void TheLibrarysThemes_LeaveTheCellsWhereTheyStand()
    {
        // Nine cells over the plot's width meet inside pixels: the blends are there, as a heatmap's are under these themes.
        Assert.True(InsideColours(Theme.Default).Count > 1);
    }
}
