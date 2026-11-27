// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;
using SkiaSharp;

namespace MatPlotLibNet.Skia.Tests;

/// <summary>The marks of a chart in a raster export stand on the pixel grid the way matplotlib's Agg backend puts them there, under the
/// classic style, and anywhere across their columns under a theme that does not ask for it. The OHLC bars: the lines of the bars are one
/// column of the full colour (a <c>vlines</c> and an <c>hlines</c> at 1 pt). The point plot: a vertical line of two vertices goes on whole
/// pixels as wide as the style's line (1.8 pt, 2.5 px under the classic style, which Agg centres on a pixel: one full column with a column
/// of the same blend on each side); off the grid the same width stands anywhere across its columns, and the two beside the fullest are
/// rarely alike.</summary>
public class PixelGridThemeTests
{
    private const int Groups = 37;
    private static readonly SKColor Blue = new(0, 0, 255);

    private static int GroupsWhoseIntervalIsCentredOnAColumn(Theme theme)
    {
        double[][] data = [.. Enumerable.Range(0, Groups).Select(_ => Enumerable.Range(0, 10).Select(i => (double)i).ToArray())];
        var figure = Plt.Create().WithSize(800, 600).WithTheme(theme)
            .AddSubPlot(1, 1, 1, ax => ax
                .Pointplot(data, s => s.Color = Colors.Blue)
                .SetXLim(-0.5, Groups - 0.5).SetYLim(0, 9))
            .Build();
        var plot = figure.PlotAreaOnSkia();
        using var picture = SKBitmap.Decode(figure.ToPng());

        // Between the dot (at the mean, 4.5) and the end of the interval (4.5 + 2.17): a price of 6.
        int row = (int)(plot.Y + plot.Height * (1 - 6.0 / 9));
        int count = 0;
        for (int group = 0; group < Groups; group++)
        {
            int x = (int)Math.Round(plot.X + (group + 0.5) * plot.Width / Groups);
            for (int column = x - 3; column < x + 3; column++)
            {
                if (picture.GetPixel(column, row) == Blue && picture.GetPixel(column - 1, row) == picture.GetPixel(column + 1, row)) { count++; break; }
            }
        }

        return count;
    }

    /// <summary>Each chart's count of the marks that stand on a column of the grid under a theme, with how many marks it draws.</summary>
    public static TheoryData<Func<Theme, int>, int> TheCharts => new()
    {
        { OhlcBarPixelGridTests.BarsWhoseLineHasAFullColumn, OhlcBarPixelGridTests.Bars },
        { GroupsWhoseIntervalIsCentredOnAColumn, Groups },
    };

    [Theory]
    [MemberData(nameof(TheCharts))]
    public void EveryMark_StandsOnAColumnOfThePixelGrid_UnderTheClassicTheme(Func<Theme, int> marksOnAColumn, int marks) =>
        Assert.Equal(marks, marksOnAColumn(Theme.MatplotlibClassic));

    [Theory]
    [MemberData(nameof(TheCharts))]
    public void ATheme_ThatDoesNotAskForIt_LeavesTheMarksWhereTheyStand(Func<Theme, int> marksOnAColumn, int marks) =>
        Assert.True(marksOnAColumn(Theme.Default) < marks);
}
