// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests;

/// <summary>How a test draws what it holds: an axes into a rectangle (the recorded calls), a figure's first plot area, a number's
/// distance to another, the outline of a shape without its repeated points.</summary>
public sealed class DrawExtensionsTests
{
    private static readonly Rect Area = new(80, 40, 400, 300);

    [Fact]
    public void AnAxes_DrawnIntoARectangle_RecordsTheCallsOfItsRenderer()
    {
        var axes = new Axes();
        axes.AddSeries(new LineSeries([0.0, 1.0, 2.0], [1.0, 3.0, 2.0]));

        var ctx = axes.Drawn(Area);

        Assert.NotEmpty(ctx.Of<DrawLinesCall>());
        Assert.Equal(ctx.Calls.Count, axes.Drawn(Area, Theme.Default).Calls.Count);
    }

    [Fact]
    public void AnAxes_DrawnUnderAnotherTheme_IsDrawnWithThatTheme()
    {
        var axes = new Axes();
        axes.AddSeries(new LineSeries([0.0, 1.0, 2.0], [1.0, 3.0, 2.0]));

        Assert.NotEqual(axes.Drawn(Area).Calls.Count, axes.Drawn(Area, Theme.MatplotlibV2).Calls.Count);
    }

    [Fact]
    public void AFigure_HasItsFirstPlotArea_AsTheLayoutPlacesIt()
    {
        var figure = Plt.Create().WithSize(600, 400).AddSubPlot(1, 1, 1, ax => ax.Plot([0.0, 1.0], [0.0, 1.0])).Build();

        var plot = figure.FirstPlotArea();

        Assert.InRange(plot.X, 1, 599);
        Assert.InRange(plot.Width, 100, 600);
        Assert.Equal(plot, figure.FirstPlotArea(new RecordingRenderContext()));
    }

    [Fact]
    public void AFigure_Drawn_RecordsTheCallsOfTheChartRenderer()
    {
        var figure = Plt.Create().WithSize(600, 400).AddSubPlot(1, 1, 1, ax => ax.Plot([0.0, 1.0], [0.0, 1.0])).Build();

        Assert.NotEmpty(figure.Drawn().Of<DrawLinesCall>());
    }

    [Fact]
    public void TheDistanceOfTwoPoints_IsTheLengthOfTheLineBetweenThem()
    {
        Assert.Equal(5.0, new Point(0, 0).DistanceTo(new Point(3, 4)), 12);
        Assert.Equal(0.0, new Point(2, 2).DistanceTo(new Point(2, 2)));
    }

    [Fact]
    public void TheCornersOfAnOutline_DropAPointThatRepeatsOrLiesOnTheLineBetweenItsNeighbours()
    {
        Point[] outline = [new(0, 0), new(0, 0), new(1, 0), new(2, 0), new(2, 2), new(2, 4), new(0, 4)];

        Assert.Equal([new Point(0, 0), new Point(2, 0), new Point(2, 4), new Point(0, 4)], outline.Corners());
    }

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("AnAxes_RenderedToSvg_WritesTheMarkOfItsSeries", () =>
        {
            var axes = new Axes();
            axes.AddSeries(new LineSeries([0.0, 1.0, 2.0], [1.0, 3.0, 2.0]));
            return axes.RenderedSvg(Area);
        }, "<polyline"),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();
}
