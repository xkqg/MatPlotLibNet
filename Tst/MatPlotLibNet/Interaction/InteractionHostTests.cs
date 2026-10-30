// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Interaction;

/// <summary>The four native controls (WPF, Avalonia, Uno, MAUI) each wired an <see cref="InteractionController"/> by hand,
/// and each did it a little differently: one aimed it at the first figure for good, one lost the server sink, one never
/// offered a tooltip. The host is the one place that wiring lives: a control gives it the figure and the layout of every
/// paint and forwards the input, and the host keeps the controller aimed at the figure that is on screen.</summary>
public class InteractionHostTests
{
    private static readonly Rect Area = new(10, 10, 200, 100);

    private static Figure NewFigure(double y = 10.0)
    {
        var figure = Plt.Create().Plot([1.0, 2.0, 3.0, 4.0], [y, y * 4, y * 2, y * 3]).Build();
        figure.ChartId = "c";
        return figure;
    }

    private static ChartLayout LayoutOf(Figure figure) => ChartLayout.Create(figure, [Area]);

    private static ScrollInputArgs Wheel() => new(110, 60, 0, -120);

    [Fact]
    public void BeforeThePaint_InputDoesNothing()
    {
        var host = new InteractionHost();

        var thrown = Record.Exception(() =>
        {
            host.HandleScroll(Wheel());
            host.HandlePointerPressed(new PointerInputArgs(50, 30, PointerButton.Left, ModifierKeys.None));
            host.HandlePointerMoved(new PointerInputArgs(60, 40, PointerButton.Left, ModifierKeys.None));
            host.HandlePointerReleased(new PointerInputArgs(60, 40, PointerButton.Left, ModifierKeys.None));
            host.HandleKeyDown(new KeyInputArgs("Home"));
        });

        Assert.Null(thrown);
        Assert.Null(host.ActiveTooltip);
        Assert.Null(host.ActiveBrushSelect);
        Assert.Null(host.ActiveCrosshair);
        Assert.False(host.IsAttached);
    }

    [Fact]
    public void AfterAPaint_AWheelZoomsTheFigureAndAsksForARepaint()
    {
        var host = new InteractionHost();
        var figure = NewFigure();
        int repaints = 0;
        host.InvalidateRequested += () => repaints++;
        host.Update(figure, LayoutOf(figure));

        host.HandleScroll(Wheel());

        Assert.NotNull(figure.SubPlots[0].XAxis.Min);
        Assert.Equal(1, repaints);
    }

    [Fact]
    public void AFigureSwapped_ForAnother_IsTheOneTheNextGestureActsOn()
    {
        var host = new InteractionHost();
        var first = NewFigure();
        var second = NewFigure(20.0);
        host.Update(first, LayoutOf(first));

        host.Update(second, LayoutOf(second));
        host.HandleScroll(Wheel());

        Assert.NotNull(second.SubPlots[0].XAxis.Min);
        Assert.Null(first.SubPlots[0].XAxis.Min);   // the controller was not left aimed at the figure that went away
    }

    [Fact]
    public void ARepaintOfTheSameFigure_KeepsAGestureThatIsStillGoingOn()
    {
        var host = new InteractionHost();
        var figure = NewFigure();
        host.Update(figure, LayoutOf(figure));

        host.HandlePointerPressed(new PointerInputArgs(50, 30, PointerButton.Left, ModifierKeys.Shift));
        host.HandlePointerMoved(new PointerInputArgs(100, 60, PointerButton.Left, ModifierKeys.Shift));
        host.Update(figure, LayoutOf(figure));

        Assert.NotNull(host.ActiveBrushSelect);
    }

    [Fact]
    public void WithAServerSink_TheGestureIsSentAndTheFigureIsLeftForTheServer()
    {
        var sent = new List<FigureInteractionEvent>();
        var host = new InteractionHost { ServerEventSink = sent.Add };
        var figure = NewFigure();
        host.Update(figure, LayoutOf(figure));

        host.HandleScroll(Wheel());

        Assert.IsType<ZoomEvent>(Assert.Single(sent));
        Assert.Null(figure.SubPlots[0].XAxis.Min);
    }

    [Fact]
    public void ASinkSetAfterThePaint_TakesEffectFromTheNextPaint()
    {
        var sent = new List<FigureInteractionEvent>();
        var host = new InteractionHost();
        var figure = NewFigure();
        host.Update(figure, LayoutOf(figure));

        host.ServerEventSink = sent.Add;
        host.Update(figure, LayoutOf(figure));
        host.HandleScroll(Wheel());

        Assert.IsType<ZoomEvent>(Assert.Single(sent));
    }

    [Fact]
    public void AClickedPoint_IsPassedOn_AndStillIsAfterAFigureSwap()
    {
        var host = new InteractionHost();
        var clicked = new List<PinnedAnnotation>();
        host.DataPointClicked += clicked.Add;
        var first = NewFigure();
        var second = NewFigure();
        host.Update(first, LayoutOf(first));
        host.Update(second, LayoutOf(second));

        // (2,40) of the second figure sits at the pixel below.
        var layout = LayoutOf(second);
        var pixel = ClickOnPoint(second, layout, 1);
        host.HandlePointerPressed(new PointerInputArgs(pixel.X, pixel.Y, PointerButton.Left, ModifierKeys.None));

        Assert.Single(clicked);
    }

    [Fact]
    public void Home_AfterAZoom_ReturnsTheFigureToItsFirstView()
    {
        var host = new InteractionHost();
        var figure = NewFigure();
        string first = figure.ToJson();
        host.Update(figure, LayoutOf(figure));

        host.HandleScroll(Wheel());
        host.Update(figure, LayoutOf(figure));
        host.HandleKeyDown(new KeyInputArgs("Home"));

        Assert.Equal(first, figure.ToJson());
    }

    private sealed class RecordingHandler : IChartEventHandler
    {
        public List<string> Seen { get; } = [];
        public Exception? Throw { get; init; }

        public ValueTask OnEventAsync(FigureInteractionEvent evt, ChartEventContext context)
        {
            Seen.Add(evt.GetType().Name);
            return Throw is null ? default : ValueTask.FromException(Throw);
        }
    }

    [Fact]
    public void TheHandler_IsToldOfEveryGesture_ThatReachedTheFigure()
    {
        var handler = new RecordingHandler();
        var host = new InteractionHost { Handler = handler };
        var figure = NewFigure();
        host.Update(figure, LayoutOf(figure));

        host.HandleScroll(Wheel());
        host.Update(figure, LayoutOf(figure));
        host.HandleKeyDown(new KeyInputArgs("Home"));

        Assert.Equal(["ZoomEvent", "ResetEvent"], handler.Seen);
    }

    [Fact]
    public void TheHandler_IsToldAfterTheGestureHasBeenApplied()
    {
        var figure = NewFigure();
        double? limitSeenByHandler = null;
        var host = new InteractionHost { Handler = new LimitProbe(() => limitSeenByHandler = figure.SubPlots[0].XAxis.Min) };
        host.Update(figure, LayoutOf(figure));

        host.HandleScroll(Wheel());

        Assert.NotNull(limitSeenByHandler);
    }

    private sealed class LimitProbe(Action probe) : IChartEventHandler
    {
        public ValueTask OnEventAsync(FigureInteractionEvent evt, ChartEventContext context)
        {
            probe();
            return default;
        }
    }

    [Fact]
    public void AHandlerThatFails_IsReported_AndDoesNotReachTheControl()
    {
        var failures = new List<Exception>();
        var host = new InteractionHost { Handler = new RecordingHandler { Throw = new InvalidOperationException("boom") } };
        host.HandlerFaulted += failures.Add;
        var figure = NewFigure();
        host.Update(figure, LayoutOf(figure));

        var thrown = Record.Exception(() => host.HandleScroll(Wheel()));

        Assert.Null(thrown);
        Assert.NotNull(figure.SubPlots[0].XAxis.Min);   // the gesture itself was applied
        Assert.IsType<InvalidOperationException>(Assert.Single(failures));
    }

    [Fact]
    public void WithAServerSink_TheHandlerIsToldToo()
    {
        var handler = new RecordingHandler();
        var host = new InteractionHost { ServerEventSink = _ => { }, Handler = handler };
        var figure = NewFigure();
        host.Update(figure, LayoutOf(figure));

        host.HandleScroll(Wheel());

        Assert.Equal(["ZoomEvent"], handler.Seen);
    }

    [Fact]
    public void TheServerSink_IsReadBackAsItWasSet()
    {
        Action<FigureInteractionEvent> sink = _ => { };
        var host = new InteractionHost { ServerEventSink = sink };

        Assert.Same(sink, host.ServerEventSink);
        Assert.Null(new InteractionHost().ServerEventSink);
    }

    [Fact]
    public void AMoveOverThePlot_GivesTheCrosshairItsPlace()
    {
        var host = new InteractionHost();
        var figure = NewFigure();
        host.Update(figure, LayoutOf(figure));

        host.HandlePointerMoved(new PointerInputArgs(60, 40, PointerButton.None, ModifierKeys.None));

        Assert.NotNull(host.ActiveCrosshair);
    }

    private sealed class ThrowingAtOnce : IChartEventHandler
    {
        public ValueTask OnEventAsync(FigureInteractionEvent evt, ChartEventContext context) =>
            throw new InvalidOperationException("at once");
    }

    [Fact]
    public void AHandlerThatThrowsBeforeItReturns_IsReported_AndDoesNotReachTheControl()
    {
        var failures = new List<Exception>();
        var host = new InteractionHost { Handler = new ThrowingAtOnce() };
        host.HandlerFaulted += failures.Add;
        var figure = NewFigure();
        host.Update(figure, LayoutOf(figure));

        var thrown = Record.Exception(() => host.HandleScroll(Wheel()));

        Assert.Null(thrown);
        Assert.Equal("at once", Assert.Single(failures).Message);
    }

    [Fact]
    public void AHandlerThatFails_WithNobodyListeningForFailures_IsHarmless()
    {
        var host = new InteractionHost { Handler = new RecordingHandler { Throw = new InvalidOperationException("boom") } };
        var figure = NewFigure();
        host.Update(figure, LayoutOf(figure));

        var thrown = Record.Exception(() => host.HandleScroll(Wheel()));

        Assert.Null(thrown);
        Assert.NotNull(figure.SubPlots[0].XAxis.Min);   // the gesture itself was applied
    }

    [Fact]
    public void AHandlerThatThrowsBeforeItReturns_WithNobodyListeningForFailures_IsHarmless()
    {
        var host = new InteractionHost { Handler = new ThrowingAtOnce() };
        var figure = NewFigure();
        host.Update(figure, LayoutOf(figure));

        Assert.Null(Record.Exception(() => host.HandleScroll(Wheel())));
    }

    [Fact]
    public void AClickOnAPoint_WithNobodyListeningForIt_IsHarmless()
    {
        var host = new InteractionHost();
        var figure = NewFigure();
        var layout = LayoutOf(figure);
        host.Update(figure, layout);
        var at = ClickOnPoint(figure, layout, 1);

        var thrown = Record.Exception(() =>
        {
            host.HandlePointerPressed(new PointerInputArgs(at.X, at.Y, PointerButton.Left, ModifierKeys.None));
            host.HandlePointerReleased(new PointerInputArgs(at.X, at.Y, PointerButton.Left, ModifierKeys.None));
        });

        Assert.Null(thrown);
    }

    private static Point ClickOnPoint(Figure figure, ChartLayout layout, int index)
    {
        var series = (MatPlotLibNet.Models.Series.XYSeries)figure.SubPlots[0].Series[0];
        var view = layout.GetDataRange(0);
        double px = Area.X + (series.XData[index] - view.XMin) / (view.XMax - view.XMin) * Area.Width;
        double py = Area.Y + Area.Height - (series.YData[index] - view.YMin) / (view.YMax - view.YMin) * Area.Height;
        return new Point(px, py);
    }
}
