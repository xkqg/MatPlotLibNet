// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>What the series of an axes ask of it: a tight range, a Y axis that runs down, an aspect. Each kind that asks says so itself
/// (<see cref="IAsksOfAxes"/>); the axes folds what they say in one walk (<c>AxesRangeExtensions.AskUnder</c>), which the range, the layout
/// and the SVG writer read. matplotlib's rules: <c>Axes.hexbin</c> requests a tight autoscale, <c>ContourSet</c> autoscales tight, imshow sets
/// the limits to the image's extent with the first row on top, cartopy's <c>GeoAxes</c> sets an aspect of one.</summary>
public class AxesAskTests
{
    /// <summary>A map of the box 0..10 by 0..10.</summary>
    private static TestMapSeries BoxMap => new(new(0, 10, 0, 10), _ => []);

    private static double[,] Grid => new double[,] { { 0, 1 }, { 2, 3 } };

    private static AxesAsk AskOf(ISeries series, Theme theme) => ((IAsksOfAxes)series).AskUnder(theme);

    [Fact]
    public void AHexbin_AsksForATightRange_UnderEveryTheme()
    {
        var hexbin = new HexbinSeries([1.0, 2.0], [1.0, 2.0]);

        Assert.Equal(new AxesAsk(Tight: true, RowsDown: false), AskOf(hexbin, Theme.Default));
        Assert.Equal(new AxesAsk(Tight: true, RowsDown: false), AskOf(hexbin, Theme.MatplotlibV2));
    }

    [Fact]
    public void TheContours_AskForATightRange_UnderAThemeThatNamesAnAutolimitMode_AndNothingElsewhere()
    {
        ISeries[] contours = [new ContourSeries([0.0, 1.0], [0.0, 1.0], Grid), new ContourfSeries([0.0, 1.0], [0.0, 1.0], Grid),
            new TricontourSeries(new double[] { 0.0, 1.0, 0.5 }, new double[] { 0.0, 0.0, 1.0 }, new double[] { 1.0, 2.0, 3.0 })];

        foreach (var contour in contours)
        {
            Assert.Equal(new AxesAsk(Tight: true, RowsDown: false), AskOf(contour, Theme.MatplotlibV2));
            Assert.Equal(default, AskOf(contour, Theme.Default));
        }
    }

    [Fact]
    public void AnImage_AsksForItsExtentRowsDownAndSquareCells_UnderTheImshowLook_AndNothingUnderTheLibrarys()
    {
        var image = new ImageSeries(Grid);

        Assert.Equal(new AxesAsk(Tight: true, RowsDown: true, Aspect: 1.0), AskOf(image, Theme.MatplotlibV2));
        Assert.Equal(default, AskOf(image, Theme.Default));
    }

    [Fact]
    public void AMap_AsksForATightRange_AndAnAspectOfOne_UnderEveryTheme()
    {
        Assert.Equal(new AxesAsk(Tight: true, RowsDown: false, Aspect: 1.0), AskOf(BoxMap, Theme.Default));
        Assert.Equal(new AxesAsk(Tight: true, RowsDown: false, Aspect: 1.0), AskOf(BoxMap, Theme.MatplotlibClassic));
    }

    [Fact]
    public void TheAxes_FoldsWhatItsSeriesAsk_InOneWalk()
    {
        var axes = new Axes();
        axes.Plot([0.0, 1.0], [0.0, 1.0]);
        Assert.Equal(default, axes.AskUnder(Theme.MatplotlibV2));

        axes.AddSeries(BoxMap);
        axes.AddSeries(new ImageSeries(Grid));
        var ask = axes.AskUnder(Theme.MatplotlibV2);

        Assert.True(ask.Tight);
        Assert.True(ask.RowsDown);
        Assert.Equal(1.0, ask.Aspect);
    }

    [Fact]
    public void AnImagesRowsRunDown_OnlyWhileTheYAxisHasNoLimitOfItsOwn()
    {
        var axes = new Axes();
        axes.AddSeries(new ImageSeries(Grid));
        axes.YAxis.Max = 5;

        var ask = axes.AskUnder(Theme.MatplotlibV2);

        Assert.True(ask.Tight);
        Assert.False(ask.RowsDown);
    }

    [Fact]
    public void TheAspectAPlotHas_IsTheOneItWasGiven_ElseTheOneItsSeriesAsk()
    {
        var axes = new Axes();
        axes.AddSeries(BoxMap);
        Assert.Equal(1.0, axes.AspectUnder(Theme.Default));

        axes.Aspect = 2.5;
        Assert.Equal(2.5, axes.AspectUnder(Theme.Default));

        Assert.Null(new Axes().AspectUnder(Theme.Default));
    }
}
