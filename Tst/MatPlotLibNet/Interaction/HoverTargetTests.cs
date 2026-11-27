// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Interaction;

/// <summary>Verifies <see cref="IHoverTarget"/>, the one question a hover asks every series family it can land on (points,
/// a ternary diagram, a state timeline): the nearer of the best found so far and the series' own nearest point within
/// reach.</summary>
public class HoverTargetTests
{
    // Plot area: pixel [10,110]x[10,60], data [0,10]x[0,5] - 10 px per data unit on both axes.
    private static (Axes Axes, HoverSearch Search) SearchAt(ChartSeries series, double dataX, double dataY, double reach = 20)
    {
        var figure = Plt.Create().Build();
        var axes = figure.AddSubPlot();
        axes.AddSeries(series);
        axes.XAxis.Min = 0;
        axes.XAxis.Max = 10;
        axes.YAxis.Min = 0;
        axes.YAxis.Max = 5;
        var layout = ChartLayout.Create(figure, [new Rect(10, 10, 100, 50)]);
        return (axes, HoverSearch.At(axes, layout.ViewOf(0), dataX, dataY, reach));
    }

    private static NearestPointResult Found(double distance) => new("earlier", 9, 0, 0, distance);

    [Fact]
    public void TheThreeFamilies_AreHoverTargets_AndASeriesOfNoneIsNot()
    {
        Assert.IsAssignableFrom<IHoverTarget>(new LineSeries([1.0], [1.0]));
        Assert.IsAssignableFrom<IHoverTarget>(new StateTimelineSeries([]));
        Assert.IsAssignableFrom<IHoverTarget>(new TernaryScatterSeries([1.0], [1.0], [1.0]));
        Assert.IsNotAssignableFrom<IHoverTarget>(new PieSeries([1.0]));
    }

    [Fact]
    public void Points_TheNearestWithinReach_IsTheAnswer_WithItsRow()
    {
        var series = new LineSeries([2.0, 5.0, 8.0], [1.0, 3.0, 4.0]) { Label = "line" };
        var (_, search) = SearchAt(series, 5.1, 3.0);

        var hit = ((IHoverTarget)series).Nearest(search, 0, null);

        Assert.NotNull(hit);
        Assert.Equal(("line", 0, 5.0, 3.0, 1), (hit.SeriesLabel, hit.SeriesIndex, hit.DataX, hit.DataY, hit.PointIndex));
        Assert.Equal(1.0, hit.PixelDistance, precision: 9);
    }

    [Fact]
    public void Points_ACloserEarlierAnswer_IsKept()
    {
        var series = new LineSeries([5.0], [3.0]);
        var (_, search) = SearchAt(series, 5.1, 3.0);
        var earlier = Found(0.5);

        Assert.Same(earlier, ((IHoverTarget)series).Nearest(search, 0, earlier));
    }

    [Fact]
    public void Points_UnlabelledSeries_IsNamedByItsIndex()
    {
        var series = new LineSeries([5.0], [3.0]);
        var (_, search) = SearchAt(series, 5.0, 3.0);

        Assert.Equal("Series 4", ((IHoverTarget)series).Nearest(search, 4, null)!.SeriesLabel);
    }

    [Theory]
    [InlineData(true, 9.0)]   // out of reach
    [InlineData(false, 5.0)]  // hidden
    public void Points_OutOfReachOrHidden_LeaveTheEarlierAnswer(bool visible, double cursorX)
    {
        var series = new LineSeries([5.0], [3.0]) { Visible = visible };
        var (_, search) = SearchAt(series, cursorX, 3.0);

        Assert.Null(((IHoverTarget)series).Nearest(search, 0, null));
    }

    [Fact]
    public void Timeline_ASegmentUnderTheCursor_IsTheAnswer_EvenOverACloserEarlierOne()
    {
        var series = new StateTimelineSeries([new StateSegment(2, 4, "Up", Colors.Green)]) { Label = "state" };
        var (_, search) = SearchAt(series, 3.0, 1.0);

        var hit = ((IHoverTarget)series).Nearest(search, 2, Found(0.0));

        Assert.NotNull(hit);
        Assert.Equal(("state", 2, 3.0, 1.0, 0.0, 0), (hit.SeriesLabel, hit.SeriesIndex, hit.DataX, hit.DataY, hit.PixelDistance, hit.PointIndex));
        Assert.Equal("Up (2–4)", hit.Detail);
    }

    [Fact]
    public void Timeline_AGapOrAHiddenTimeline_LeavesTheEarlierAnswer()
    {
        var earlier = Found(3);
        var gap = new StateTimelineSeries([new StateSegment(2, 4, "Up", Colors.Green)]);
        var hidden = new StateTimelineSeries([new StateSegment(2, 4, "Up", Colors.Green)]) { Visible = false };

        Assert.Same(earlier, ((IHoverTarget)gap).Nearest(SearchAt(gap, 6.0, 1.0).Search, 0, earlier));
        Assert.Same(earlier, ((IHoverTarget)hidden).Nearest(SearchAt(hidden, 3.0, 1.0).Search, 0, earlier));
    }

    [Fact]
    public void Ternary_AHiddenDiagram_LeavesTheEarlierAnswer()
    {
        var series = new TernaryScatterSeries([1.0], [1.0], [1.0]) { Visible = false };
        var earlier = Found(1);

        Assert.Same(earlier, ((IHoverTarget)series).Nearest(SearchAt(series, 0.5, 0.3).Search, 0, earlier));
    }

    [Fact]
    public void Search_TheDistanceToAPoint_IsMeasuredInPixels()
    {
        var (_, search) = SearchAt(new LineSeries([1.0], [1.0]), 5.0, 3.0);

        Assert.Equal(5.0, search.PixelDistanceTo(5.3, 3.4), precision: 9);
        Assert.True(double.IsNaN(search.PixelDistanceTo(double.NaN, 3.0)));
    }
}
