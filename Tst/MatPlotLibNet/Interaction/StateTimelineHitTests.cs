// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Interaction;

/// <summary>
/// A replay application lays its events out as a <see cref="StateTimelineSeries"/> and needs to know which event
/// the reader is pointing at: hover names it, a click selects it so the video can seek. Both gestures resolve
/// their target through <see cref="NearestPointFinder"/>, which only ever looked at <see cref="XYSeries"/> — a
/// timeline was invisible to it, so a segment gave no tooltip and no click. Raised in discussion #22.
/// <para>A segment is hit by being under the cursor, not by being near it: it is a span, not a point, so the
/// answer is the segment containing the cursor's X, and its index in <see cref="StateTimelineSeries.Segments"/>
/// is the identity that keys back to the application's own event.</para>
/// </summary>
public class StateTimelineHitTests
{
    // Plot area pixel [0,100]x[0,50]; data X [0,100] so one pixel is one unit, Y [0,1].
    private static readonly StateSegment[] Events =
    [
        new(0, 15, "Event 1", Colors.Blue),
        new(30, 45, "Event 2", Colors.Orange),
        new(60, 90, "Event 3", Colors.Green),
    ];

    private static Figure Timeline(StateSegment[]? segments = null, string? label = "Replay", bool visible = true)
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.StateTimeline(segments ?? Events, s => { s.Label = label; s.Visible = visible; }))
            .Build();
        figure.SubPlots[0].XAxis.Min = 0;
        figure.SubPlots[0].XAxis.Max = 100;
        figure.SubPlots[0].YAxis.Min = 0;
        figure.SubPlots[0].YAxis.Max = 1;
        return figure;
    }

    private static ChartLayout LayoutOf(Figure figure) => ChartLayout.Create(figure, [new Rect(0, 0, 100, 50)]);

    private static NearestPointResult? Find(Figure figure, double dataX) =>
        NearestPointFinder.Find(figure, 0, dataX, 0.5, LayoutOf(figure));

    [Fact]
    public void ACursorInsideASegment_FindsThatSegment()
    {
        var result = Find(Timeline(), 35);

        Assert.NotNull(result);
        Assert.Equal(1, result.PointIndex);
        Assert.Equal(0, result.SeriesIndex);
        Assert.Equal("Replay", result.SeriesLabel);
        Assert.Equal(0.0, result.PixelDistance);
    }

    [Fact]
    public void TheHit_SaysWhereOnTheTimelineTheReaderPointed()
    {
        // A seek wants the instant under the cursor, not only the segment around it.
        var result = Find(Timeline(), 35);

        Assert.NotNull(result);
        Assert.Equal(35.0, result.DataX, precision: 6);
    }

    [Fact]
    public void ACursorInTheGapBetweenSegments_FindsNothing()
    {
        Assert.Null(Find(Timeline(), 20));
    }

    [Fact]
    public void ACursorOnASegmentsEdge_CountsAsInsideIt()
    {
        Assert.Equal(1, Find(Timeline(), 30)!.PointIndex);
        Assert.Equal(1, Find(Timeline(), 45)!.PointIndex);
    }

    [Fact]
    public void WhenSegmentsOverlap_TheOneDrawnLastWins()
    {
        // Later segments are painted over earlier ones, so the one the reader can see is the one that answers.
        StateSegment[] overlapping = [new(0, 50, "Under", Colors.Blue), new(20, 40, "Over", Colors.Orange)];

        Assert.Equal(1, Find(Timeline(overlapping), 30)!.PointIndex);
        Assert.Equal(0, Find(Timeline(overlapping), 10)!.PointIndex);
    }

    [Fact]
    public void WhenTwoTimelinesShareTheAxes_TheOneDrawnLastAnswers()
    {
        // Series are painted in the order they were added, so the later timeline covers the earlier one.
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .StateTimeline([new StateSegment(0, 50, "Under", Colors.Blue)], s => s.Label = "First")
                .StateTimeline([new StateSegment(20, 40, "Over", Colors.Orange)], s => s.Label = "Second"))
            .Build();
        figure.SubPlots[0].XAxis.Min = 0;
        figure.SubPlots[0].XAxis.Max = 100;
        figure.SubPlots[0].YAxis.Min = 0;
        figure.SubPlots[0].YAxis.Max = 1;

        var result = Find(figure, 30);

        Assert.Equal("Second", result!.SeriesLabel);
        Assert.Equal(1, result.SeriesIndex);
    }

    [Fact]
    public void AHiddenTimeline_IsNotHit()
    {
        Assert.Null(Find(Timeline(visible: false), 35));
    }

    [Fact]
    public void ATimelineWithoutALabel_IsNamedLikeAnyOtherSeries()
    {
        Assert.Equal("Series 0", Find(Timeline(label: null), 35)!.SeriesLabel);
    }

    [Fact]
    public void TheHit_DescribesTheSegmentInWords()
    {
        // The tooltip cannot show "(35, 0.5)" for a span: it names the event and the stretch it covers.
        var result = Find(Timeline(), 35);

        Assert.Equal("Event 2 (30–45)", result!.Detail);
    }

    [Fact]
    public void AHoverOverASegment_ShowsTheEventAndItsSpan()
    {
        var figure = Timeline();
        var ctrl = InteractionController.CreateLocal(figure, LayoutOf(figure));

        ctrl.HandlePointerMoved(new PointerInputArgs(35, 25, PointerButton.None, ModifierKeys.None));

        Assert.NotNull(ctrl.ActiveTooltip);
        Assert.Equal("Replay: Event 2 (30–45)", ctrl.ActiveTooltip.Text);
        Assert.Equal(1, ctrl.ActiveTooltip.PointIndex);
    }

    [Fact]
    public void AClickOnASegment_NamesWhichSegmentItWas()
    {
        var figure = Timeline();
        var ctrl = InteractionController.CreateLocal(figure, LayoutOf(figure));
        var clicked = new List<PinnedAnnotation>();
        ctrl.DataPointClicked += clicked.Add;

        ctrl.HandlePointerPressed(new PointerInputArgs(35, 25, PointerButton.Left, ModifierKeys.None));
        ctrl.HandlePointerReleased(new PointerInputArgs(35, 25, PointerButton.Left, ModifierKeys.None));

        var point = Assert.Single(clicked);
        Assert.Equal(1, point.PointIndex);
        Assert.Equal(0, point.SeriesIndex);
        Assert.Equal(35.0, point.DataX, precision: 6);
    }

    [Fact]
    public void AClickInTheGap_IsNotAClickOnASegment()
    {
        var figure = Timeline();
        var ctrl = InteractionController.CreateLocal(figure, LayoutOf(figure));
        var clicked = new List<PinnedAnnotation>();
        ctrl.DataPointClicked += clicked.Add;

        ctrl.HandlePointerPressed(new PointerInputArgs(20, 25, PointerButton.Left, ModifierKeys.None));

        Assert.Empty(clicked);
    }

    [Fact]
    public void AReferenceLine_CanBeMovedAfterItIsDrawn()
    {
        // A playback marker moves every frame. Rebuilding the whole figure to move one line would also throw
        // away the reader's zoom, so the line itself has to be movable.
        ReferenceLine? marker = null;
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.StateTimeline(Events).SetXLim(0, 100).AxVLine(10, line => marker = line))
            .Build();
        var before = figure.ToSvg();

        marker!.Value = 70;

        Assert.Equal(70.0, marker.Value);
        Assert.NotEqual(before, figure.ToSvg());
    }
}
