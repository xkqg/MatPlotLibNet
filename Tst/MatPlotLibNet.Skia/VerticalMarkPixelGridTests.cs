// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;
using SkiaSharp;

namespace MatPlotLibNet.Skia.Tests;

/// <summary>A vertical mark in a raster export stands on the pixel grid the way matplotlib's Agg backend puts it there: a stroke of
/// horizontal and vertical segments lies on a whole pixel, half a pixel on when its width rounds to an odd number of pixels, so a
/// 1 pt mark under the classic style is one column of its colour with the rest spilt onto the two beside it, wherever it falls.
/// A leaf names the mark and where on the plot a row is clear of the mark's head.</summary>
public abstract class VerticalMarkPixelGridTests
{
    private static readonly SKColor Blue = new(0, 0, 255);

    /// <summary>Draws the one blue mark at <paramref name="value"/> on an axes whose X range is 0 to 620 and Y range -1 to 1.</summary>
    /// <param name="axes">The axes to draw it on.</param>
    /// <param name="value">Where the mark stands on the X axis.</param>
    protected abstract AxesBuilder WithMark(AxesBuilder axes, double value);

    /// <summary>How far down the plot the row that is read lies, clear of the head of the mark.</summary>
    protected abstract double RowFraction { get; }

    /// <summary>One mark, the X range as wide as the plot is in pixels, with the mark on a pixel edge: the column it is drawn into when
    /// snapped is the one on the right of the edge.</summary>
    private OneMark At(Theme theme)
    {
        Figure Build(double value) => Plt.Create().WithSize(800, 600).WithTheme(theme)
            .AddSubPlot(1, 1, 1, ax => WithMark(ax, value).SetXLim(0, 620).SetYLim(-1, 1))
            .Build();

        var plot = Build(330).PlotAreaOnSkia();
        int edge = (int)Math.Ceiling(plot.X + 330 * plot.Width / 620);
        double value = (edge - plot.X) * 620 / plot.Width;
        return new OneMark(Build(value), edge, (int)(plot.Y + plot.Height * RowFraction));
    }

    private readonly record struct OneMark(Figure Figure, int Edge, int Row);

    [Fact]
    public void AMark_FillsOneColumnOfThePixelGridWithItsColour_AndSpillsTheRestOntoTheTwoBesideIt()
    {
        var mark = At(Theme.MatplotlibClassic);
        using var picture = SKBitmap.Decode(mark.Figure.ToPng());

        Assert.Equal(Blue, picture.GetPixel(mark.Edge, mark.Row));
        // 1 pt is 1.39 px: a column of blue and 0.19 px on each side, over the white plot: 255 * (1 - 0.19) = 207.
        Assert.InRange(picture.GetPixel(mark.Edge - 1, mark.Row).Red, 200, 210);
        Assert.InRange(picture.GetPixel(mark.Edge + 1, mark.Row).Red, 200, 210);
    }

    [Fact]
    public void ATheme_ThatDoesNotAskForIt_LeavesTheMarkWhereItStands()
    {
        var mark = At(Theme.Default);
        using var picture = SKBitmap.Decode(mark.Figure.ToPng());

        // 1 px centred on a pixel edge: half a pixel on each of the two columns around it, a blend in both, a column of blue in none.
        Assert.InRange(picture.GetPixel(mark.Edge - 1, mark.Row).Red, 100, 160);
        Assert.InRange(picture.GetPixel(mark.Edge, mark.Row).Red, 100, 160);
    }
}
