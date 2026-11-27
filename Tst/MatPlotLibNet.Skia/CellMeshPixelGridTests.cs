// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;
using SkiaSharp;

namespace MatPlotLibNet.Skia.Tests;

/// <summary>The cells of a colour-mapped mesh in a raster export stand on the pixel grid, as matplotlib draws them: a heatmap is an
/// image (<c>imshow</c>, <c>matshow</c>), resampled so that each pixel takes the colour of the cell its centre falls in, and a 2-D
/// histogram and a mesh are a <c>QuadMesh</c> (<c>hist2d</c> draws <c>pcolormesh</c>), whose quads Agg snaps to whole pixels (printed with
/// python, matplotlib 3.11.2: <c>QuadMesh.get_snap()</c> True, no edge). So two cells meet on a pixel edge and no pixel is a blend of the
/// two or of a cell and the ground. A cell drawn where it stands meets its neighbour inside a pixel, which the antialiasing paints half and
/// half over the ground: a light seam between every two cells. The matplotlib themes ask for it
/// (<see cref="RasterLook.StrokesOnPixelGrid"/>); the library's own themes leave the cells where they stand.</summary>
public class CellMeshPixelGridTests
{
    private static readonly double[,] Three = { { 0, 0.5, 1 } };

    private static readonly double[,] Seven = { { 0, 1.0 / 6, 2.0 / 6, 3.0 / 6, 4.0 / 6, 5.0 / 6, 1 } };

    /// <summary>The meshes, each of seven cells across whose values run evenly from the least to the most, so their colours are the
    /// colour map's at 0, 1/6, ... 1.</summary>
    public static TheoryData<string> Meshes => ["heatmap", "hist2d", "pcolormesh"];

    private static Figure Mesh(string kind, Theme theme) => Plt.Create().WithSize(800, 600).WithTheme(theme)
        .AddSubPlot(1, 1, 1, ax =>
        {
            switch (kind)
            {
                case "heatmap":
                    ax.Heatmap(Seven, s => s.ColorMap = ColorMaps.Viridis);
                    break;
                case "hist2d":
                    // Column c holds c + 1 points in every row, so the counts run 1 to 7 across: the seven colours of the map.
                    var points = new List<(double X, double Y)>();
                    for (int r = 0; r < 7; r++)
                        for (int c = 0; c < 7; c++)
                            for (int k = 0; k <= c; k++) points.Add((c + 0.5, r + 0.5));
                    ax.Histogram2D([.. points.Select(p => p.X)], [.. points.Select(p => p.Y)], 7, s => s.ColorMap = ColorMaps.Viridis);
                    break;
                default:
                    ax.Pcolormesh([0.0, 1, 2, 3, 4, 5, 6, 7], [0.0, 1], Seven, s => s.ColorMap = ColorMaps.Viridis);
                    break;
            }
        })
        .Build();

    private static SKColor Sk(Color color) => new(color.R, color.G, color.B);

    /// <summary>The colours the pixels of the row through the middle of the cells take, between the frame's columns, the margin the
    /// axes keeps around the cells left out.</summary>
    private static HashSet<SKColor> RowColours(Figure figure)
    {
        var plot = figure.PlotAreaOnSkia();
        using var picture = SKBitmap.Decode(figure.ToPng());
        int row = (int)(plot.Y + plot.Height / 2);
        var ground = picture.GetPixel((int)Math.Ceiling(plot.X) + 2, (int)Math.Ceiling(plot.Y) + 2);
        var colours = new HashSet<SKColor>();
        for (int x = (int)Math.Ceiling(plot.X) + 2; x < (int)(plot.X + plot.Width) - 2; x++) colours.Add(picture.GetPixel(x, row));
        colours.Remove(ground);
        return colours;
    }

    private static HashSet<SKColor> SevenColours() => [.. Enumerable.Range(0, 7).Select(k => Sk(ColorMaps.Viridis.GetColor(k / 6.0)))];

    [Fact]
    public void Heatmap_UnderTheMatplotlibThemes_PaintsEveryPixelOfACellInItsColour_WithNoSeam()
    {
        HashSet<SKColor> cells = [Sk(ColorMaps.Viridis.GetColor(0)), Sk(ColorMaps.Viridis.GetColor(0.5)), Sk(ColorMaps.Viridis.GetColor(1))];
        Figure Of(Theme theme) => Plt.Create().WithSize(800, 600).WithTheme(theme)
            .AddSubPlot(1, 1, 1, ax => ax.Heatmap(Three, s => s.ColorMap = ColorMaps.Viridis)).Build();

        Assert.Subset(cells, RowColours(Of(Theme.MatplotlibV2)));
        Assert.Subset(cells, RowColours(Of(Theme.MatplotlibClassic)));
    }

    [Theory]
    [MemberData(nameof(Meshes))]
    public void EveryMesh_UnderTheMatplotlibThemes_PaintsEveryPixelOfACellInItsColour_WithNoSeam(string kind)
    {
        Assert.Subset(SevenColours(), RowColours(Mesh(kind, Theme.MatplotlibV2)));
        Assert.Subset(SevenColours(), RowColours(Mesh(kind, Theme.MatplotlibClassic)));
    }

    [Theory]
    [MemberData(nameof(Meshes))]
    public void EveryMesh_UnderTheLibrarysThemes_LeavesTheCellsWhereTheyStand(string kind)
    {
        // Seven cells over the plot's width meet inside pixels: the blends are there.
        Assert.True(RowColours(Mesh(kind, Theme.Default)).Count > 7);
    }
}
