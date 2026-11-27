// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>An image stands on its axes as matplotlib's <c>imshow</c> stands it under the matplotlib themes. matplotlib 3.11.2,
/// <c>AxesImage.set_extent</c>: the extent of an image of <c>rows</c> by <c>cols</c> is <c>(-0.5, cols - 0.5, rows - 0.5, -0.5)</c>
/// (<c>image.origin = 'upper'</c>), <c>self.sticky_edges.x[:] = [xmin, xmax]</c>, <c>self.sticky_edges.y[:] = [ymin, ymax]</c>, and
/// <c>self.axes.set_xlim(xmin, xmax)</c>, <c>self.axes.set_ylim(ymin, ymax)</c>: the axes shows the image's extent exactly, rounded out to
/// no tick under the classic style (printed: <c>xlim (-0.5, 219.5)</c>, <c>ylim (169.5, -0.5)</c> for 170 by 220 under both styles), the Y
/// axis running down. A line added past the image widens the view by the margin while the sticky edges hold the image's side
/// (printed under the default style: <c>xlim (-0.5, 315.025)</c> for a line to 300). The library's own themes keep an image filling
/// its plot over 0 to the columns and 0 to the rows.</summary>
public sealed class ImshowExtentTests
{
    private static readonly Rect Plot = new(80, 40, 440, 330);

    private static Axes ImageOf(int rows, int cols)
    {
        var axes = new Axes();
        axes.Image(new double[rows, cols]);
        return axes;
    }

    [Theory]
    [InlineData("classic")]
    [InlineData("v2")]
    public void AnImage_UnderTheMatplotlibThemes_ShowsItsExactExtent_RowZeroOnTop(string themeId)
    {
        var theme = themeId.AsTheme();

        var range = ImageOf(170, 220).ComputeDataRanges(theme);

        Assert.Equal(new DataRange(-0.5, 219.5, 169.5, -0.5), range);
    }

    [Fact]
    public void V2_ALinePastTheImage_WidensTheViewByTheMargin_TheImagesEdgesStaySticky()
    {
        var axes = ImageOf(170, 220);
        axes.Plot([0.0, 300.0], [0.0, 10.0]);

        var range = axes.ComputeDataRanges(Theme.MatplotlibV2);

        Assert.Equal(-0.5, range.XMin, 9);
        Assert.Equal(315.025, range.XMax, 9);
        Assert.Equal(new(169.5, -0.5), (range.YMin, range.YMax));
    }

    [Fact]
    public void V2_AYLimitSetLowToHigh_IsKept_AndTheFirstRowIsDrawnAtTheBottom()
    {
        var axes = ImageOf(2, 3);
        axes.YAxis.Min = -0.5;
        axes.YAxis.Max = 1.5;

        var (cells, transform) = Cells(axes, Theme.MatplotlibV2);

        Assert.Equal(new(-0.5, 1.5), (transform.DataYMin, transform.DataYMax));
        // Row 0 is the first cell drawn; its lower edge stands on y = -0.5, the bottom of the plot.
        Assert.Equal(transform.DataToPixel(-0.5, -0.5).Y, cells[0].Y + cells[0].Height, 6);
    }

    [Fact]
    public void V2_TheCellsStandOnTheirExtent_NotOnTheWholePlot_WhenALineReachesPastThem()
    {
        var axes = ImageOf(2, 3);
        axes.Plot([0.0, 10.0], [0.0, 1.0]);

        var (cells, transform) = Cells(axes, Theme.MatplotlibV2);

        var topLeft = transform.DataToPixel(-0.5, -0.5);
        var bottomRight = transform.DataToPixel(2.5, 1.5);
        Assert.Equal(6, cells.Count);
        Assert.Equal(topLeft.X, cells[0].X, 6);
        Assert.Equal(topLeft.Y, cells[0].Y, 6);
        Assert.Equal(bottomRight.X, cells[^1].X + cells[^1].Width, 6);
        Assert.Equal(bottomRight.Y, cells[^1].Y + cells[^1].Height, 6);
        Assert.True(bottomRight.X < Plot.Right - 100);
    }

    [Fact]
    public void TheLibrarysOwnTheme_KeepsAnImageFillingItsPlot_OverZeroToTheColumnsAndRows()
    {
        var (cells, _) = Cells(ImageOf(2, 3), Theme.Default);

        Assert.Equal(Plot.X, cells[0].X, 6);
        Assert.Equal(Plot.Y, cells[0].Y, 6);
        Assert.Equal(Plot.Right, cells[^1].X + cells[^1].Width, 6);
        Assert.Equal(Plot.Bottom, cells[^1].Y + cells[^1].Height, 6);
    }

    [Theory]
    [InlineData("default")]
    [InlineData("dark")]
    public void TheLibrarysOwnThemes_RangeAnImageOverZeroToTheColumnsAndRows_SoTheTicksNameTheCellsItFills(string themeId)
    {
        // The cells fill the plot, so a margin or a nice bound past the cells would put tick labels where no cell stands
        // (measured before: -11..250 and -8.5..178.5 for 220 columns and 170 rows under the default theme).
        var theme = themeId.AsTheme();

        var range = ImageOf(170, 220).ComputeDataRanges(theme);

        Assert.Equal(new DataRange(0, 220, 0, 170), range);
    }

    [Fact]
    public void TheLibrarysOwnTheme_ALinePastTheImage_WidensTheView_AndTheCellsKeepTheirPlace_TheFirstRowOnTop()
    {
        var axes = ImageOf(2, 3);
        axes.Plot([0.0, 10.0], [0.0, 1.0]);

        var range = axes.ComputeDataRanges(Theme.Default);
        var (cells, transform) = Cells(axes, Theme.Default);

        Assert.Equal(0, range.XMin);
        Assert.True(range.XMax > 10);
        // Cell [0, 0] stands at the top left of the image, x 0 and y = rows; the last at x = cols, y 0.
        var topLeft = transform.DataToPixel(0, 2);
        var bottomRight = transform.DataToPixel(3, 0);
        Assert.Equal(topLeft.X, cells[0].X, 6);
        Assert.Equal(topLeft.Y, cells[0].Y, 6);
        Assert.Equal(bottomRight.X, cells[^1].X + cells[^1].Width, 6);
        Assert.Equal(bottomRight.Y, cells[^1].Y + cells[^1].Height, 6);
    }

    [Fact]
    public void AnImageWithNoCell_AddsNothingToTheRange_UnderEveryTheme()
    {
        IThemedRange empty = new ImageSeries(new double[0, 4]);

        Assert.Equal(default, empty.RangeUnder(null!, Theme.MatplotlibV2));
        Assert.Equal(default, empty.RangeUnder(null!, Theme.Default));
        Assert.Equal(default, new ImageSeries(new double[3, 0]).ComputeDataRange(null!));
    }

    /// <summary>The cells the image draws (the rectangles drawn in a colour of the image's map, in drawing order) and the transform
    /// the axes drew with.</summary>
    private static (List<Rect> Cells, DataTransform Transform) Cells(Axes axes, Theme theme)
    {
        var ctx = new RecordingRenderContext();
        var renderer = new CartesianAxesRenderer(axes, Plot, ctx, theme);
        renderer.Render();
        var cells = ctx.Of<DrawRectangleCall>()
            .Where(a => a.Fill != theme.AxesBackground && a.Stroke is null && a.StrokeThickness == 0)
            .Select(a => a.Rect).Where(r => r != Plot).ToList();
        return (cells, renderer.DrawnTransform!);
    }
}
