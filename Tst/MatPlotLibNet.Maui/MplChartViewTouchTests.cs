// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Streaming;
using MatPlotLibNet.Rendering;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using NSubstitute;
using MauiPoint = Microsoft.Maui.Graphics.Point;
using MauiRect = Microsoft.Maui.Graphics.Rect;

namespace MatPlotLibNet.Maui.Tests;

/// <summary>The MAUI view drew a chart and nothing else: a finger could not pan it, zoom it, pick a point or go home. A touch
/// surface reports a drag, a pinch and a tap through MAUI's gesture recognizers and the graphics view's own touch events, and
/// the view hands them to <see cref="TouchGestureTranslator"/>, which turns them into the input every other head gives the
/// interaction host. These tests drive the view the way the platform does, through the recognizers' controllers and
/// <see cref="IGraphicsView"/>, and judge it by what happens to the figure.</summary>
public class MplChartViewTouchTests
{
    private const int GestureId = 1;

    private static Figure NewFigure() => Plt.Create()
        .AddSubPlot(1, 1, 1, ax => ax.Plot([40.0, 60.0], [40.0, 60.0]).SetXLim(0, 100).SetYLim(0, 100))
        .Build();

    private static double Span(Figure figure) => figure.SubPlots[0].XAxis.Max!.Value - figure.SubPlots[0].XAxis.Min!.Value;

    /// <summary>A view of the size the figure is drawn at, the way the chart pages place it: the head draws a figure at the
    /// size it asks for, so view pixels and figure pixels are the same.</summary>
    private static MplChartView Show(Figure figure, bool interactive = true)
    {
        var view = new MplChartView { Figure = figure, IsInteractive = interactive };
        ((IView)view).Arrange(new MauiRect(0, 0, figure.Width, figure.Height));
        Paint(view);
        return view;
    }

    private static ICanvas Paint(MplChartView view)
    {
        var canvas = Substitute.For<ICanvas>();
        view.Drawable!.Draw(canvas, new RectF(0, 0, 800, 600));
        return canvas;
    }

    private static MauiRect PlotAreaOf(Figure figure)
    {
        var layout = ChartRendererDefault.Current.ComputeLayout(figure, new MauiGraphicsRenderContext(Substitute.For<ICanvas>()));
        var area = ChartLayout.Create(figure, layout).GetPlotArea(0);
        return new MauiRect(area.X, area.Y, area.Width, area.Height);
    }

    /// <summary>The pixel of a data position in a plot of 0..100 on both axes.</summary>
    private static PointF PixelOf(MauiRect area, double x, double y) =>
        new((float)(area.X + x / 100.0 * area.Width), (float)(area.Y + area.Height - y / 100.0 * area.Height));

    private static void FingerDown(MplChartView view, PointF at) => ((IGraphicsView)view).StartInteraction([at]);

    private static PanGestureRecognizer PanOf(MplChartView view) => view.GestureRecognizers.OfType<PanGestureRecognizer>().Single();

    private static void Pan(MplChartView view, double totalX, double totalY, bool finish = true)
    {
        var pan = (IPanGestureController)PanOf(view);
        pan.SendPanStarted(view, GestureId);
        pan.SendPan(view, totalX / 2, totalY / 2, GestureId);
        pan.SendPan(view, totalX, totalY, GestureId);
        if (finish) pan.SendPanCompleted(view, GestureId);
    }

    private static void Pinch(MplChartView view, double scale, double originX = 0.5, double originY = 0.5)
    {
        var pinch = (IPinchGestureController)view.GestureRecognizers.OfType<PinchGestureRecognizer>().Single();
        pinch.SendPinchStarted(view, new MauiPoint(originX, originY));
        pinch.SendPinch(view, scale, new MauiPoint(originX, originY));
        pinch.SendPinchEnded(view);
    }

    /// <summary>MAUI keeps the tap sender internal; the recognizer is driven through the method the platform calls.</summary>
    private static void Tap(MplChartView view, int taps, PointF at)
    {
        var recognizer = view.GestureRecognizers.OfType<TapGestureRecognizer>().Single(t => t.NumberOfTapsRequired == taps);
        var send = typeof(TapGestureRecognizer).GetMethod("SendTapped", BindingFlags.NonPublic | BindingFlags.Instance)!;
        send.Invoke(recognizer, [view, (Func<IElement?, MauiPoint?>)(_ => new MauiPoint(at.X, at.Y))]);
    }

    // ── what the view is until it is told ───────────────────────────────────────

    [Fact]
    public void AViewIsNotInteractive_UntilItIsToldTo_AndAFingerOnItPansWhenDraggingIsTheDefault()
    {
        var view = new MplChartView();

        Assert.False(view.IsInteractive);
        Assert.Equal(DragGestureAction.Pan, view.DragAction);
    }

    [Fact]
    public void ADragOnAViewThatIsNotInteractive_MovesNothing()
    {
        var figure = NewFigure();
        var view = Show(figure, interactive: false);
        var area = PlotAreaOf(figure);

        FingerDown(view, PixelOf(area, 5, 95));
        Pan(view, 60, 0);

        Assert.Equal(0.0, figure.SubPlots[0].XAxis.Min);
    }

    [Fact]
    public void AGestureBeforeTheFirstPaint_IsIgnored()
    {
        var figure = NewFigure();
        var view = new MplChartView { Figure = figure, IsInteractive = true };

        var thrown = Record.Exception(() =>
        {
            FingerDown(view, new PointF(100, 100));
            Pan(view, 60, 0);
            Pinch(view, 1.5);
            Tap(view, 2, new PointF(100, 100));
        });

        Assert.Null(thrown);
        Assert.Equal(0.0, figure.SubPlots[0].XAxis.Min);
    }

    // ── a finger drags ──────────────────────────────────────────────────────────

    [Fact]
    public void ADrag_PansTheChart_AndTheDataGoesWithTheFinger()
    {
        var figure = NewFigure();
        var view = Show(figure);
        var area = PlotAreaOf(figure);

        FingerDown(view, PixelOf(area, 5, 95));
        Pan(view, 60, 0);

        double shift = 60 / area.Width * 100.0;
        Assert.Equal(-shift, figure.SubPlots[0].XAxis.Min!.Value, 6);
        Assert.Equal(100.0, Span(figure), 6);
    }

    [Fact]
    public void ADragThatTheFingerNeverStartedOnTheView_IsIgnored()
    {
        var figure = NewFigure();
        var view = Show(figure);

        Pan(view, 60, 0);   // the recognizer reported a pan, but the view saw no finger go down

        Assert.Equal(0.0, figure.SubPlots[0].XAxis.Min);
    }

    [Fact]
    public void TheDragAction_IsWhatADragDoes()
    {
        var figure = NewFigure();
        var view = Show(figure);
        view.DragAction = DragGestureAction.RectangleZoom;
        var area = PlotAreaOf(figure);

        FingerDown(view, PixelOf(area, 10, 90));
        Pan(view, area.Width * 0.5, area.Height * 0.5);

        Assert.InRange(Span(figure), 45, 55);   // a rectangle over half the plot zooms to half of it
    }

    [Fact]
    public void ABrushDrag_ShowsItsBandWhileTheFingerIsDown_AndNotAfter()
    {
        var figure = NewFigure();
        var view = Show(figure);
        view.DragAction = DragGestureAction.BrushSelect;
        var area = PlotAreaOf(figure);
        var from = PixelOf(area, 10, 90);
        FingerDown(view, from);

        Pan(view, 80, 40, finish: false);
        var during = Paint(view);
        ((IPanGestureController)PanOf(view)).SendPanCompleted(view, GestureId);
        var after = Paint(view);

        during.Received().DrawRectangle(from.X, from.Y, 80f, 40f);
        after.DidNotReceive().DrawRectangle(from.X, from.Y, 80f, 40f);
    }

    [Fact]
    public void ADragTheSystemTookAway_ZoomsNothing()
    {
        var figure = NewFigure();
        var view = Show(figure);
        view.DragAction = DragGestureAction.RectangleZoom;
        var area = PlotAreaOf(figure);
        FingerDown(view, PixelOf(area, 10, 90));

        Pan(view, area.Width * 0.5, area.Height * 0.5, finish: false);
        ((IPanGestureController)PanOf(view)).SendPanCanceled(view, GestureId);

        Assert.Equal(0.0, figure.SubPlots[0].XAxis.Min);
        Assert.Equal(100.0, Span(figure), 6);
    }

    // ── a pinch, a tap, a double tap, a finger hovering ─────────────────────────

    [Fact]
    public void APinchOut_ZoomsInAboutThePointBetweenTheFingers()
    {
        var figure = NewFigure();
        var view = Show(figure);

        Pinch(view, 1.6);

        Assert.True(Span(figure) < 100);
        Assert.InRange(figure.SubPlots[0].XAxis.Min!.Value + Span(figure) / 2, 45, 55);   // centred on where it was
    }

    [Fact]
    public void ADoubleTap_ReturnsToTheViewTheChartStartedFrom()
    {
        var figure = NewFigure();
        var view = Show(figure);
        Pinch(view, 1.8);
        Paint(view);   // the repaint after a gesture gives the host the layout it now has
        Assert.True(Span(figure) < 100);

        Tap(view, 2, PixelOf(PlotAreaOf(figure), 5, 95));

        Assert.Equal(0.0, figure.SubPlots[0].XAxis.Min!.Value, 6);
        Assert.Equal(100.0, figure.SubPlots[0].XAxis.Max!.Value, 6);
    }

    [Fact]
    public void ATapOnAPoint_RaisesDataPointClicked_WithThePointItHit()
    {
        var figure = NewFigure();
        var view = Show(figure);
        var clicked = new List<PinnedAnnotation>();
        view.DataPointClicked += clicked.Add;

        Tap(view, 1, PixelOf(PlotAreaOf(figure), 40, 40));

        var point = Assert.Single(clicked);
        Assert.Equal(40.0, point.DataX, 6);
    }

    [Fact]
    public void APointerOverAPoint_ShowsItsTooltip_AndLeavingItClearsIt()
    {
        var figure = NewFigure();
        var view = Show(figure);
        var over = PixelOf(PlotAreaOf(figure), 60, 60);

        ((IGraphicsView)view).StartHoverInteraction([over]);
        ((IGraphicsView)view).MoveHoverInteraction([over]);
        string? shown = ToolTipProperties.GetText(view)?.ToString();
        ((IGraphicsView)view).EndHoverInteraction();

        Assert.False(string.IsNullOrEmpty(shown));
        Assert.True(string.IsNullOrEmpty(ToolTipProperties.GetText(view)?.ToString()));
    }

    // ── what the view acts on ───────────────────────────────────────────────────

    [Fact]
    public void AViewHandedAnotherFigure_ActsOnThatOneAfterItsNextPaint()
    {
        var first = NewFigure();
        var second = NewFigure();
        var view = Show(first);
        view.Figure = second;
        Paint(view);
        var area = PlotAreaOf(second);

        FingerDown(view, PixelOf(area, 5, 95));
        Pan(view, 60, 0);

        Assert.Equal(0.0, first.SubPlots[0].XAxis.Min);
        Assert.NotEqual(0.0, second.SubPlots[0].XAxis.Min!.Value);
    }

    [Fact]
    public void ARepaintAfterTheViewStoppedBeingInteractive_LeavesTheNextGestureUnheard()
    {
        var figure = NewFigure();
        var view = Show(figure);
        view.IsInteractive = false;
        Paint(view);
        var area = PlotAreaOf(figure);

        FingerDown(view, PixelOf(area, 5, 95));
        Pan(view, 60, 0);

        Assert.Equal(0.0, figure.SubPlots[0].XAxis.Min);
    }

    [Fact]
    public void AStreamingView_IsPannedLikeAnyOther()
    {
        using var stream = new StreamingFigure(NewFigure());
        var view = new MplStreamingChartView { StreamingFigure = stream, IsInteractive = true };
        ((IView)view).Arrange(new MauiRect(0, 0, 800, 600));
        Paint(view);
        var area = PlotAreaOf(stream.Figure);

        FingerDown(view, PixelOf(area, 5, 95));
        Pan(view, 60, 0);

        Assert.NotEqual(0.0, stream.Figure.SubPlots[0].XAxis.Min!.Value);
    }
}
