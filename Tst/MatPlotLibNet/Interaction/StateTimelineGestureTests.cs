// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Interaction;

/// <summary>
/// A replay application wants three things from the same timeline at once: click an event to seek to it, drag to
/// move along the recording, and scroll to zoom in on a stretch. They compete for the same left button, and a
/// timeline is mostly segments, so a drag that starts on one must still pan. The vertical axis of a timeline has
/// no meaning (segments fill the bar), so pan and zoom move along X only. Raised in discussion #22.
/// </summary>
public class StateTimelineGestureTests
{
    private static readonly StateSegment[] Events =
    [
        new(0, 15, "Event 1", Colors.Blue),
        new(30, 45, "Event 2", Colors.Orange),
        new(60, 90, "Event 3", Colors.Green),
    ];

    /// <summary>A timeline on screen: the controller driving it, the figure it changes, and the clicks it reported.</summary>
    private readonly record struct Rig(InteractionController Controller, Figure Figure, List<PinnedAnnotation> Clicks);

    // Plot area pixel [0,100]x[0,50]; data X [0,100] so one pixel is one unit, Y [0,1].
    private static Rig Open()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.StateTimeline(Events).SetXLim(0, 100))
            .Build();
        figure.SubPlots[0].YAxis.Min = 0;
        figure.SubPlots[0].YAxis.Max = 1;
        var controller = InteractionController.CreateLocal(figure, ChartLayout.Create(figure, [new Rect(0, 0, 100, 50)]));
        var clicks = new List<PinnedAnnotation>();
        controller.DataPointClicked += clicks.Add;
        return new Rig(controller, figure, clicks);
    }

    private static PointerInputArgs At(double x, double y, PointerButton button = PointerButton.Left) =>
        new(x, y, button, ModifierKeys.None);

    private static void Drag(InteractionController c, double fromX, double toX, double y = 25)
    {
        c.HandlePointerPressed(At(fromX, y));
        c.HandlePointerMoved(At((fromX + toX) / 2, y));
        c.HandlePointerMoved(At(toX, y));
        c.HandlePointerReleased(At(toX, y));
    }

    [Fact]
    public void ADragThatStartsOnASegment_PansTheTimeline()
    {
        var (controller, figure, clicks) = Open();

        Drag(controller, fromX: 35, toX: 25);   // inside Event 2, ten units to the left

        Assert.Equal(10.0, figure.SubPlots[0].XAxis.Min!.Value, precision: 6);
        Assert.Empty(clicks);                   // pulling the recording along is not choosing an event
    }

    [Fact]
    public void ADragInTheGapBetweenSegments_StillPansTheTimeline()
    {
        var (controller, figure, _) = Open();

        Drag(controller, fromX: 20, toX: 10);

        Assert.Equal(10.0, figure.SubPlots[0].XAxis.Min!.Value, precision: 6);
    }

    [Fact]
    public void APressAndReleaseOnASegment_IsAClickOnIt()
    {
        var (controller, _, clicks) = Open();

        controller.HandlePointerPressed(At(35, 25));
        controller.HandlePointerReleased(At(35, 25));

        var click = Assert.Single(clicks);
        Assert.Equal(1, click.PointIndex);
    }

    [Fact]
    public void APressOnASegment_IsNotYetAClick()
    {
        // Until the button comes up the reader may still be starting a drag.
        var (controller, _, clicks) = Open();

        controller.HandlePointerPressed(At(35, 25));

        Assert.Empty(clicks);
    }

    [Fact]
    public void ADragAcrossTheTimeline_NeverMovesItVertically()
    {
        var (controller, figure, _) = Open();

        controller.HandlePointerPressed(At(35, 10));
        controller.HandlePointerMoved(At(30, 40));
        controller.HandlePointerReleased(At(30, 40));

        Assert.Equal(0.0, figure.SubPlots[0].YAxis.Min!.Value);
        Assert.Equal(1.0, figure.SubPlots[0].YAxis.Max!.Value);
    }

    [Fact]
    public void ScrollingOverATimeline_ZoomsAlongTimeOnly()
    {
        var (controller, figure, _) = Open();

        controller.HandleScroll(new ScrollInputArgs(50, 25, 0, -3.0, ModifierKeys.None));

        Assert.True(figure.SubPlots[0].XAxis.Max!.Value - figure.SubPlots[0].XAxis.Min!.Value < 100.0);
        Assert.Equal(0.0, figure.SubPlots[0].YAxis.Min!.Value);
        Assert.Equal(1.0, figure.SubPlots[0].YAxis.Max!.Value);
    }

    [Fact]
    public void AnAxesThatMixesATimelineWithALine_StillPansAndZoomsBothWays()
    {
        // Only an axes made of timelines alone has a vertical range that means nothing.
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.StateTimeline(Events).Plot([0.0, 100.0], [0.0, 1.0]).SetXLim(0, 100))
            .Build();
        figure.SubPlots[0].YAxis.Min = 0;
        figure.SubPlots[0].YAxis.Max = 1;
        var controller = InteractionController.CreateLocal(figure, ChartLayout.Create(figure, [new Rect(0, 0, 100, 50)]));

        controller.HandlePointerPressed(At(20, 10));    // the gap between Event 1 and Event 2
        controller.HandlePointerMoved(At(20, 30));
        controller.HandlePointerReleased(At(20, 30));

        Assert.NotEqual(0.0, figure.SubPlots[0].YAxis.Min!.Value);
    }

    [Fact]
    public void AFewPixelsOfJitterBetweenPressAndRelease_IsStillAClick()
    {
        var (controller, figure, clicks) = Open();

        controller.HandlePointerPressed(At(35, 25));
        controller.HandlePointerMoved(At(37, 26));
        controller.HandlePointerReleased(At(37, 26));

        Assert.Equal(1, Assert.Single(clicks).PointIndex);
        Assert.Equal(0.0, figure.SubPlots[0].XAxis.Min!.Value);   // a click moves nothing
    }

    [Fact]
    public void ADragThatComesBackToWhereItStarted_IsStillADrag()
    {
        // Once the pointer has gone further than a click allows, returning to the start does not turn it back
        // into a click: the reader pulled the recording and put it down again.
        var (controller, _, clicks) = Open();

        controller.HandlePointerPressed(At(35, 25));
        controller.HandlePointerMoved(At(50, 25));
        controller.HandlePointerMoved(At(35, 25));
        controller.HandlePointerReleased(At(35, 25));

        Assert.Empty(clicks);
    }

    [Fact]
    public void WithoutAPanModifier_ASegmentIsClickedOnPress()
    {
        // The four-argument constructor is the one every existing caller uses; it has nothing to hand a drag to.
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.StateTimeline(Events).SetXLim(0, 100)).Build();
        var pinned = new List<FigureInteractionEvent>();
        var modifier = new DataCursorModifier("c", ChartLayout.Create(figure, [new Rect(0, 0, 100, 50)]), figure, pinned.Add);

        var press = At(35, 25);
        Assert.True(modifier.HandlesPointerPressed(press));
        modifier.OnPointerPressed(press);
        modifier.OnPointerMoved(At(36, 25));     // nothing is pending, so a move is ignored
        modifier.OnPointerReleased(At(36, 25));  // and so is the release

        Assert.IsType<DataCursorEvent>(Assert.Single(pinned));
    }

    [Fact]
    public void ATimelineWithoutExplicitLimits_PansAndZoomsToo()
    {
        // The reader of the documentation builds the timeline and never calls SetXLim; the axes autoscale to the
        // segments. Dragging and scrolling must work from that state, not only after the range was written down.
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.StateTimeline(Events)).Build();
        var controller = InteractionController.CreateLocal(figure, ChartLayout.Create(figure, [new Rect(0, 0, 100, 50)]));

        Drag(controller, fromX: 35, toX: 25);

        Assert.NotNull(figure.SubPlots[0].XAxis.Min);   // the drag alone must have written the range it moved to
    }

    [Fact]
    public void ALineChartWithoutExplicitLimits_PansToo()
    {
        // The same dead pan hit every chart that scales itself, not only timelines.
        var figure = Plt.Create().Plot([0.0, 50, 100], [0.0, 5, 3]).Build();
        var controller = InteractionController.CreateLocal(figure, ChartLayout.Create(figure, [new Rect(0, 0, 100, 50)]));
        Drag(controller, fromX: 35, toX: 25);
        Assert.NotNull(figure.SubPlots[0].XAxis.Min);
    }
}
