// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Interaction;

/// <summary>A finger is not a mouse: it has no wheel, no modifier keys and no button, only a drag, a pinch and a tap. The
/// interaction controller is written for the mouse, so a translator turns the gestures a touch surface reports into the pointer
/// input the controller understands: a drag does what the control says a drag does (pan, rectangle zoom, brush, span, rotate),
/// a pinch is the wheel, a double tap is Home and a tap is a click. It lives in core so that it is tested, and gated, without a
/// device.</summary>
public class TouchGestureTranslatorTests
{
    private static readonly Rect Area = new(10, 10, 200, 100);

    private static (Figure Figure, InteractionHost Host, TouchGestureTranslator Touch, List<FigureInteractionEvent> Events) Chart(Figure? given = null)
    {
        var figure = given ?? Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot([10.0, 50.0, 90.0], [10.0, 25.0, 40.0]).SetXLim(0, 100).SetYLim(0, 50)).Build();
        figure.ChartId = "c";
        var events = new List<FigureInteractionEvent>();
        var host = new InteractionHost { Handler = new Collect(events) };
        host.Update(figure, ChartLayout.Create(figure, [Area]));
        return (figure, host, new TouchGestureTranslator(host), events);
    }

    private sealed class Collect(List<FigureInteractionEvent> into) : IChartEventHandler
    {
        public ValueTask OnEventAsync(FigureInteractionEvent evt, ChartEventContext context)
        {
            into.Add(evt);
            return default;
        }
    }

    /// <summary>How far the data moves when a finger drags a pan by <paramref name="pixels"/>: the pixels as a share of the
    /// plot area the chart is drawn in, of the 100 data units its X axis spans.</summary>
    private static double PanShift(Figure figure, double pixels) =>
        pixels / ChartLayout.Create(figure, [Area]).GetPlotArea(0).Width * 100.0;

    // A finger that comes down within a few pixels of a data point clicks that point instead of panning: the drags of these
    // tests start in open plot area (the points sit at the pixels (30, 90), (110, 60) and (190, 30)).
    private static void Drag(TouchGestureTranslator touch, double fromX, double fromY, double byX, double byY)
    {
        touch.DragStarted(fromX, fromY);
        touch.DragMoved(fromX, fromY, byX / 2, byY / 2);
        touch.DragMoved(fromX, fromY, byX, byY);
        touch.DragEnded(fromX, fromY, byX, byY);
    }

    // ── what a drag does ────────────────────────────────────────────────────────

    [Fact]
    public void ADragPans_ByDefault_AndTheDataFollowsTheFinger()
    {
        var (figure, _, touch, events) = Chart();

        double shift = PanShift(figure, 40);

        Drag(touch, 150, 100, 40, 0);   // 40 px to the right: the data goes with the finger

        Assert.Equal(-shift, figure.SubPlots[0].XAxis.Min!.Value, 6);
        Assert.Equal(100.0 - shift, figure.SubPlots[0].XAxis.Max!.Value, 6);
        Assert.All(events, e => Assert.IsType<PanEvent>(e));
    }

    [Fact]
    public void ADragWithTheRectangleZoomAction_ZoomsIntoTheRectangleItDraws()
    {
        var (figure, _, touch, events) = Chart();
        touch.DragAction = DragGestureAction.RectangleZoom;

        Drag(touch, 60, 30, 100, 50);

        var zoom = Assert.IsType<RectangleZoomEvent>(Assert.Single(events));
        Assert.True(zoom.XMax - zoom.XMin < 100);
        Assert.NotNull(figure.SubPlots[0].XAxis.Min);
    }

    [Fact]
    public void ADragWithTheBrushAction_SelectsWithoutMovingTheView()
    {
        var (figure, _, touch, events) = Chart();
        touch.DragAction = DragGestureAction.BrushSelect;

        Drag(touch, 60, 30, 100, 50);

        Assert.IsType<BrushSelectEvent>(Assert.Single(events));
        Assert.Equal(0.0, figure.SubPlots[0].XAxis.Min);   // the limits are the ones it was given
        Assert.Equal(100.0, figure.SubPlots[0].XAxis.Max);
    }

    [Fact]
    public void ADragWithTheSpanAction_SelectsASpanOfX()
    {
        var (_, _, touch, events) = Chart();
        touch.DragAction = DragGestureAction.SpanSelect;

        Drag(touch, 60, 30, 100, 0);

        Assert.IsType<SpanSelectEvent>(Assert.Single(events));
    }

    [Fact]
    public void ADragWithTheRotateAction_TurnsThe3DCamera()
    {
        var figure = Plt.Create().Surface([0.0, 1.0], [0.0, 1.0], new double[,] { { 1.0, 2.0 }, { 3.0, 4.0 } }).Build();
        double azimuth = figure.SubPlots[0].Azimuth;
        var (_, _, touch, _) = Chart(figure);
        touch.DragAction = DragGestureAction.Rotate;

        Drag(touch, 100, 60, 40, 10);

        Assert.NotEqual(azimuth, figure.SubPlots[0].Azimuth);
    }

    [Fact]
    public void ADragThatStartsOutsideThePlot_DoesNothing()
    {
        var (figure, _, touch, events) = Chart();

        Drag(touch, 400, 300, 40, 0);

        Assert.Empty(events);
        Assert.Equal(0.0, figure.SubPlots[0].XAxis.Min);
    }

    [Fact]
    public void ACancelledDrag_LeavesNothingHalfDone_ForTheNextOne()
    {
        var (figure, _, touch, _) = Chart();
        touch.DragAction = DragGestureAction.BrushSelect;
        touch.DragStarted(60, 30);
        touch.DragMoved(60, 30, 20, 10);

        touch.DragCancelled();
        touch.DragAction = DragGestureAction.Pan;
        double shift = PanShift(figure, 40);

        Drag(touch, 150, 100, 40, 0);

        Assert.Equal(-shift, figure.SubPlots[0].XAxis.Min!.Value, 6);   // the new drag is a clean pan
    }

    [Theory]
    [InlineData(DragGestureAction.RectangleZoom)]
    [InlineData(DragGestureAction.SpanSelect)]
    [InlineData(DragGestureAction.Pan)]
    public void ACancelledDrag_ZoomsAndSelectsNothing(DragGestureAction action)
    {
        var (_, _, touch, events) = Chart();
        touch.DragAction = action;
        touch.DragStarted(60, 30);
        touch.DragMoved(60, 30, 80, 40);
        events.Clear();   // a pan has moved the view by now; the cancel adds nothing to that

        touch.DragCancelled();

        Assert.Empty(events);
    }

    [Fact]
    public void ADragEndsWithoutItsFingerDown_IsIgnored()
    {
        var (_, _, touch, events) = Chart();

        touch.DragMoved(60, 30, 10, 10);
        touch.DragEnded(60, 30, 10, 10);
        touch.DragCancelled();

        Assert.Empty(events);
    }

    [Fact]
    public void ADragStartedWhileAnotherIsOpen_EndsTheOldOneFirst()
    {
        var (figure, _, touch, _) = Chart();
        touch.DragAction = DragGestureAction.BrushSelect;
        touch.DragStarted(60, 30);
        touch.DragAction = DragGestureAction.Pan;

        double shift = PanShift(figure, 40);

        Drag(touch, 150, 100, 40, 0);

        Assert.Equal(-shift, figure.SubPlots[0].XAxis.Min!.Value, 6);   // the pan is a pan, not the old brush
    }

    [Fact]
    public void WhatIsLeftOfAPinch_IsDroppedWhenTheFingersLift()
    {
        var (figure, _, touch, _) = Chart();
        for (int i = 0; i < 10; i++) touch.Pinched(1.01, 110, 60);   // 10 small steps: not yet a notch
        touch.PinchEnded();

        for (int i = 0; i < 10; i++) touch.Pinched(1.01, 110, 60);   // 10 more would be a notch together, but not after the lift

        Assert.Equal(0.0, figure.SubPlots[0].XAxis.Min);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(double.PositiveInfinity)]
    public void APinchThatIsNotANumberOfTimes_ChangesNothing(double scale)
    {
        var (figure, _, touch, events) = Chart();

        touch.Pinched(scale, 110, 60);

        Assert.Empty(events);
        Assert.Equal(0.0, figure.SubPlots[0].XAxis.Min);
    }

    // ── pinch, taps, hover ──────────────────────────────────────────────────────

    [Fact]
    public void APinchOut_ZoomsIn_AndAPinchIn_ZoomsOut()
    {
        var (figure, _, touch, _) = Chart();
        double before = figure.SubPlots[0].XAxis.Max!.Value - figure.SubPlots[0].XAxis.Min!.Value;

        touch.Pinched(1.6, 110, 60);
        double zoomedIn = figure.SubPlots[0].XAxis.Max!.Value - figure.SubPlots[0].XAxis.Min!.Value;
        touch.Pinched(0.5, 110, 60);
        double zoomedOut = figure.SubPlots[0].XAxis.Max!.Value - figure.SubPlots[0].XAxis.Min!.Value;

        Assert.True(zoomedIn < before);
        Assert.True(zoomedOut > zoomedIn);
    }

    [Fact]
    public void ManySmallPinchSteps_ZoomAsMuchAsOneBigOne()
    {
        var (small, _, touchSmall, _) = Chart();
        var (big, _, touchBig, _) = Chart();

        for (int i = 0; i < 20; i++) touchSmall.Pinched(1.04, 110, 60);
        touchBig.Pinched(Math.Pow(1.04, 20), 110, 60);

        double SmallSpan(Figure f) => f.SubPlots[0].XAxis.Max!.Value - f.SubPlots[0].XAxis.Min!.Value;
        Assert.InRange(SmallSpan(small) / SmallSpan(big), 0.85, 1.18);   // within a notch of each other
    }

    [Fact]
    public void ATinyPinch_IsNotAZoomYet_ButIsNotLost()
    {
        var (figure, _, touch, _) = Chart();

        touch.Pinched(1.01, 110, 60);
        Assert.Equal(0.0, figure.SubPlots[0].XAxis.Min);   // below a notch

        for (int i = 0; i < 20; i++) touch.Pinched(1.01, 110, 60);
        Assert.NotEqual(0.0, figure.SubPlots[0].XAxis.Min!.Value);   // they add up
    }

    [Fact]
    public void ADoubleTap_IsHome()
    {
        var (figure, _, touch, events) = Chart(Plt.Create().Plot([10.0, 50.0, 90.0], [10.0, 25.0, 40.0]).Build());
        string first = figure.ToJson();
        touch.Pinched(1.8, 110, 60);
        Assert.NotEqual(first, figure.ToJson());
        // The repaint that follows every gesture gives the host the layout of the chart as it is now.
        // (Home asks the figure for the view it started from; nothing else is needed.)

        touch.DoubleTapped(110, 60);

        Assert.Equal(first, figure.ToJson());
        Assert.IsType<ResetEvent>(events[^1]);
    }

    [Fact]
    public void ATapOnAPoint_IsAClick()
    {
        var (_, host, touch, _) = Chart();
        var clicked = new List<PinnedAnnotation>();
        host.DataPointClicked += clicked.Add;
        double px = Area.X + 50.0 / 100.0 * Area.Width;
        double py = Area.Y + Area.Height - 25.0 / 50.0 * Area.Height;

        touch.Tapped(px, py);

        Assert.Single(clicked);
    }

    [Fact]
    public void ATouchMovingOverTheChartWithoutPressing_IsHover()
    {
        var (_, host, touch, _) = Chart();
        double px = Area.X + 50.0 / 100.0 * Area.Width;
        double py = Area.Y + Area.Height - 25.0 / 50.0 * Area.Height;

        touch.Hovered(px, py);

        Assert.NotNull(host.ActiveTooltip);
    }
}
