// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;
using SkiaSharp;

namespace MatPlotLibNet.Skia.Tests;

/// <summary>A horizontal or vertical reference line in a raster export stands on the pixel grid, as matplotlib's <c>axhline</c> and
/// <c>axvline</c> do: Agg snaps a line made of horizontal or vertical segments to the middle of a pixel row or column (a width that
/// rounds to one pixel is half a pixel on), so a line one pixel wide and a little over fills that row and the rest spills a few
/// percent onto its neighbours. A line left where it stands straddles two rows and paints each of them about half. The matplotlib
/// themes ask for it (<see cref="RasterLook.StrokesOnPixelGrid"/>), as they do for the spines; the library's own themes leave the line
/// where it stands.</summary>
public class ReferenceLinePixelGridTests
{
    /// <summary>A width of 0.8 points at the 100 dpi of matplotlib's pictures, as <c>axhline(linewidth=0.8)</c> draws.</summary>
    private const double Width = 0.8 * 100 / 72;

    private static Figure WithLine(Theme theme, double level, Orientation orientation) => Plt.Create().WithSize(800, 600).WithTheme(theme)
        .AddSubPlot(1, 1, 1, ax =>
        {
            ax.Plot([0.0, 10.0], [0.0, 10.0]);
            Action<ReferenceLine> style = line => { line.Color = Colors.Red; line.LineStyle = LineStyle.Solid; line.LineWidth = Width; };
            _ = orientation == Orientation.Horizontal ? ax.AxHLine(level, style) : ax.AxVLine(level, style);
        })
        .Build();

    private static SKBitmap Picture(Figure figure) => SKBitmap.Decode(figure.ToPng());

    private static bool IsRed(SKColor c) => c.Red > 200 && c.Green < 40 && c.Blue < 40;

    /// <summary>How many of the rows (or columns) are painted in the line's own colour somewhere along the middle of the plot.</summary>
    private static int PureRows(SKBitmap picture, Orientation orientation)
    {
        int pure = 0;
        int length = orientation == Orientation.Horizontal ? picture.Height : picture.Width;
        for (int i = 0; i < length; i++)
        {
            // Dashes: look along the line for the strongest pixel of this row or column.
            bool isPure = false;
            for (int along = 300; along < 500; along++)
                isPure |= IsRed(orientation == Orientation.Horizontal ? picture.GetPixel(along, i) : picture.GetPixel(i, along));
            if (isPure) pure++;
        }
        return pure;
    }

    // Levels over a plot 0..10 high or wide whose pixel is, for each of them, on a different fraction: the unsnapped line straddles
    // two rows for some of them whatever the plot's size.
    private static readonly double[] LevelValues = [1.13, 2.5, 3.77, 4.31, 5.5, 6.62, 7.05, 8.4, 9.21];

    public static TheoryData<double> Levels => [.. LevelValues];

    [Theory]
    [MemberData(nameof(Levels))]
    public void AHorizontalLine_UnderTheMatplotlibThemes_FillsOneRowInItsColour(double level)
    {
        foreach (var theme in new[] { Theme.MatplotlibClassic, Theme.MatplotlibV2 })
        {
            using var picture = Picture(WithLine(theme, level, Orientation.Horizontal));

            Assert.Equal(1, PureRows(picture, Orientation.Horizontal));
        }
    }

    [Theory]
    [MemberData(nameof(Levels))]
    public void AVerticalLine_UnderTheMatplotlibThemes_FillsOneColumnInItsColour(double level)
    {
        foreach (var theme in new[] { Theme.MatplotlibClassic, Theme.MatplotlibV2 })
        {
            using var picture = Picture(WithLine(theme, level, Orientation.Vertical));

            Assert.Equal(1, PureRows(picture, Orientation.Vertical));
        }
    }

    [Fact]
    public void TheLibrarysThemes_LeaveTheLineWhereItStands_SoSomeLevelStraddlesTwoRows()
    {
        var straddles = LevelValues.Count(level =>
        {
            using var picture = Picture(WithLine(Theme.Default, level, Orientation.Horizontal));
            return PureRows(picture, Orientation.Horizontal) != 1;
        });

        Assert.True(straddles > 0);
    }
}
