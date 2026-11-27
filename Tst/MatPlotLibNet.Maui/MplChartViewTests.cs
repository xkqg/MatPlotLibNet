// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models.Streaming;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MauiPoint = Microsoft.Maui.Graphics.Point;
using MauiRect = Microsoft.Maui.Graphics.Rect;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui;
using NSubstitute;
using System.Reflection;
namespace MatPlotLibNet.Maui.Tests;

/// <summary>Where the view's figure comes from: the figure it is handed, or the frames of the animation source it follows. Following
/// itself (let go of the one before, listen to the one now) is the core's <c>AnimationSourceFollowerTests</c>; a frame is posted to
/// MAUI's main thread, which a test has no loop for.</summary>
public class MplChartViewTests
{
    [Fact]
    public void TheFigureAndTheAnimationSource_ReadBackAsGiven_AndTheSourceFollowedIsTheOneSetLast()
    {
        var figure = Plt.Create().Plot([1.0, 2.0], [3.0, 4.0]).Build();

        AnimationSourceReadback.Verify(new MplChartView { Figure = figure }, figure, static v => v.Figure, static v => v.AnimationSource,
            static (v, source) => v.AnimationSource = source);
    }

    private const int GestureId = 1;

    private static Figure NewFigure() => Plt.Create()
        .AddSubPlot(1, 1, 1, ax => ax.Plot([40.0, 60.0], [40.0, 60.0]).SetXLim(0, 100).SetYLim(0, 100))
        .Build();

    private static MplChartView Show(Figure figure, IChartEventHandler? handler = null)
    {
        var view = new MplChartView { Figure = figure, IsInteractive = true, ChartEventHandler = handler };
        ((IView)view).Arrange(new MauiRect(0, 0, figure.Width, figure.Height));
        view.Drawable!.Draw(Substitute.For<ICanvas>(), new RectF(0, 0, 800, 600));
        return view;
    }

    private static MauiRect PlotAreaOf(Figure figure)
    {
        var layout = ChartRendererDefault.Current.ComputeLayout(figure, MauiRig.Fresh().Context);
        var area = ChartLayout.Create(figure, layout).GetPlotArea(0);
        return new MauiRect(area.X, area.Y, area.Width, area.Height);
    }

    private static PointF PixelOf(MauiRect area, double x, double y) =>
        new((float)(area.X + x / 100.0 * area.Width), (float)(area.Y + area.Height - y / 100.0 * area.Height));

    private static PanGestureRecognizer PanOf(MplChartView view) => view.GestureRecognizers.OfType<PanGestureRecognizer>().Single();

    private static double Span(Figure figure) => figure.SubPlots[0].XAxis.Max!.Value - figure.SubPlots[0].XAxis.Min!.Value;

    [Fact]
    public void AViewWithNoFigure_DrawsNothing()
    {
        var view = new MplChartView();

        var thrown = Record.Exception(() => view.Drawable!.Draw(Substitute.For<ICanvas>(), new RectF(0, 0, 100, 100)));

        Assert.Null(thrown);
    }

    [Fact]
    public void APanWithNoFingerThatWentDown_IsNotADrag()
    {
        var figure = NewFigure();
        var view = Show(figure);
        ((IGraphicsView)view).StartInteraction([]);   // a touch event that carries no touch
        var before = Span(figure);

        var pan = (IPanGestureController)PanOf(view);
        pan.SendPanStarted(view, GestureId);
        pan.SendPan(view, 30, 0, GestureId);
        pan.SendPanCompleted(view, GestureId);

        Assert.Equal(before, Span(figure), 9);
    }

    [Fact]
    public void ACancelledPan_EndsTheDragWhereItWas_AndTheNextDragStartsAgain()
    {
        var figure = NewFigure();
        var view = Show(figure);
        var area = PlotAreaOf(figure);
        ((IGraphicsView)view).StartInteraction([PixelOf(area, 50, 50)]);
        var pan = (IPanGestureController)PanOf(view);

        pan.SendPanStarted(view, GestureId);
        pan.SendPan(view, 20, 0, GestureId);
        pan.SendPanCanceled(view, GestureId);
        var thrown = Record.Exception(() =>
        {
            ((IGraphicsView)view).StartInteraction([PixelOf(area, 50, 50)]);
            pan.SendPanStarted(view, GestureId);
            pan.SendPan(view, 20, 0, GestureId);
            pan.SendPanCompleted(view, GestureId);
        });

        Assert.Null(thrown);
    }

    [Fact]
    public void AHoverWithNoTouch_AndAHoverOverNothing_LeaveNoTooltip()
    {
        var figure = NewFigure();
        var view = Show(figure);
        var area = PlotAreaOf(figure);

        ((IGraphicsView)view).MoveHoverInteraction([]);
        ((IGraphicsView)view).MoveHoverInteraction([PixelOf(area, 5, 95)]);   // far from both points

        Assert.Null(Record.Exception(() => ((IGraphicsView)view).EndHoverInteraction()));
    }

    [Fact]
    public void AHandlerThatThrows_WithNobodyListeningForFailures_IsHarmless()
    {
        var figure = NewFigure();
        var view = Show(figure, new RecordingChartEventHandler { ThrowAtOnce = new InvalidOperationException("boom") });
        var area = PlotAreaOf(figure);
        ((IGraphicsView)view).StartInteraction([PixelOf(area, 50, 50)]);
        var pan = (IPanGestureController)PanOf(view);

        var thrown = Record.Exception(() =>
        {
            pan.SendPanStarted(view, GestureId);
            pan.SendPan(view, 30, 0, GestureId);
            pan.SendPanCompleted(view, GestureId);
        });

        Assert.Null(thrown);
    }

    /// <summary>MAUI keeps the tap sender internal; the recognizer is driven through the method the platform calls.</summary>
    private static void Tap(MplChartView view, int taps, PointF at)
    {
        var recognizer = view.GestureRecognizers.OfType<TapGestureRecognizer>().Single(t => t.NumberOfTapsRequired == taps);
        var send = typeof(TapGestureRecognizer).GetMethod("SendTapped", BindingFlags.NonPublic | BindingFlags.Instance)!;
        send.Invoke(recognizer, [view, (Func<IElement?, MauiPoint?>)(_ => new MauiPoint(at.X, at.Y))]);
    }

    [Fact]
    public void ATapOnAPoint_WithNobodyListeningForIt_IsHarmless()
    {
        var figure = NewFigure();
        var view = Show(figure);
        var at = PixelOf(PlotAreaOf(figure), 40, 40);

        Assert.Null(Record.Exception(() => Tap(view, 1, at)));
    }

    [Fact]
    public void AStreamingView_ReadsBackTheFigureItWasGiven()
    {
        using var streaming = new StreamingFigure(NewFigure());
        var view = new MplStreamingChartView { StreamingFigure = streaming };

        Assert.Same(streaming, view.StreamingFigure);
    }
}
