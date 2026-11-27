// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Interaction;

/// <summary>A native control's host builds its controller through the factory it is given and talks to it through
/// <see cref="IInteractionController"/> only, so a controller written outside the library takes the built-in one's place. What
/// the controller sends it sends through its context's sink, which the host made: the gesture reaches the figure (or the
/// server), the control's clicked point and its handler, whichever controller sent it. These tests hand the host a stand-in.</summary>
public class InteractionHostControllerSeamTests
{
    private static readonly Rect Area = new(10, 10, 200, 100);

    /// <summary>A controller of the caller's own: it records what the host forwards and sends what a test tells it to through
    /// the context it was made with.</summary>
    private sealed class StandInController(InteractionContext context) : IInteractionController
    {
        public InteractionContext Context { get; } = context;
        public readonly List<string> Calls = [];
        public IChartLayout? Layout;
        public HoverTooltipContent? Tooltip { get; set; }

        public event Action? InvalidateRequested;

        public void HandlePointerPressed(PointerInputArgs args) => Calls.Add("pressed");
        public void HandlePointerMoved(PointerInputArgs args) => Calls.Add("moved");
        public void HandlePointerReleased(PointerInputArgs args) => Calls.Add("released");
        public void HandleScroll(ScrollInputArgs args) => Calls.Add("scroll");
        public void HandleKeyDown(KeyInputArgs args) => Calls.Add("key");
        public BrushSelectState? ActiveBrushSelect => null;
        public HoverTooltipContent? ActiveTooltip => Tooltip;
        public CrosshairState? ActiveCrosshair => null;
        public void UpdateLayout(IChartLayout layout) => Layout = layout;

        public void Invalidate() => InvalidateRequested?.Invoke();
        public void Send(FigureInteractionEvent evt) => Context.Sink(evt);
    }

    private static Figure NewFigure()
    {
        var figure = Plt.Create().Plot([1.0, 2.0, 3.0], [1.0, 4.0, 2.0]).Build();
        figure.ChartId = "c";
        return figure;
    }

    /// <summary>A host whose factory makes a stand-in, and the list of every stand-in it made.</summary>
    private static (InteractionHost Host, List<StandInController> Made) HostOfStandIns(Action? repaint = null)
    {
        var made = new List<StandInController>();
        var host = repaint is null ? new InteractionHost() : new InteractionHost(repaint);
        host.ControllerFactory = context =>
        {
            var controller = new StandInController(context);
            made.Add(controller);
            return controller;
        };
        return (host, made);
    }

    [Fact]
    public void TheHost_BuildsItsControllerThroughTheFactory_AndTalksToItThroughTheInterface()
    {
        var (host, made) = HostOfStandIns();
        var figure = NewFigure();
        var firstLayout = ChartLayout.Create(figure, [Area]);
        var secondLayout = ChartLayout.Create(figure, [Area]);

        host.Update(figure, firstLayout);
        host.Update(figure, secondLayout);
        var controller = Assert.Single(made);                     // one controller for one figure
        controller.Tooltip = new HoverTooltipContent("y = 4", 1, 2);
        host.HandlePointerPressed(new PointerInputArgs(1, 1, PointerButton.Left, ModifierKeys.None));
        host.HandlePointerMoved(new PointerInputArgs(1, 1, PointerButton.None, ModifierKeys.None));
        host.HandlePointerReleased(new PointerInputArgs(1, 1, PointerButton.Left, ModifierKeys.None));
        host.HandleScroll(new ScrollInputArgs(1, 1, 0, 1));
        host.HandleKeyDown(new KeyInputArgs("Home"));

        Assert.Same(figure, controller.Context.Figure);
        Assert.Equal("c", controller.Context.ChartId);
        Assert.Same(secondLayout, controller.Layout);              // a repaint replaced the layout only
        Assert.Equal(["pressed", "moved", "released", "scroll", "key"], controller.Calls);
        Assert.Equal("y = 4", host.ActiveTooltip?.Text);
    }

    [Fact]
    public void WhatACallersControllerSends_ReachesTheFigure_TheClickedPoint_AndTheHandler()
    {
        int repaints = 0;
        var (host, made) = HostOfStandIns(() => repaints++);
        var handler = new RecordingChartEventHandler();
        host.Handler = handler;
        var clicked = new List<PinnedAnnotation>();
        host.DataPointClicked += clicked.Add;
        var figure = NewFigure();
        host.Update(figure, ChartLayout.Create(figure, [Area]));
        var controller = Assert.Single(made);
        var point = new PinnedAnnotation("s", 1, 2, 3, 4, 0);
        var zoom = new ZoomEvent("c", 0, 1.5, 2.5, 0, 1);
        var pin = new DataCursorEvent("c", 0, point);

        controller.Send(zoom);
        controller.Send(pin);
        controller.Invalidate();

        Assert.Equal(1.5, figure.SubPlots[0].XAxis.Min);           // no server: the gesture is applied to the figure in place
        Assert.Equal(2, repaints);                                 // once for the zoom that changed it, once when asked; the pin changes nothing
        Assert.Equal(point, Assert.Single(clicked));
        Assert.Equal([zoom, pin], handler.Events);
    }

    [Fact]
    public void WithAServerSink_WhatACallersControllerSends_GoesToTheServer_AndTheFigureIsLeftForIt()
    {
        int repaints = 0;
        var (host, made) = HostOfStandIns(() => repaints++);
        var handler = new RecordingChartEventHandler();
        host.Handler = handler;
        var sent = new List<FigureInteractionEvent>();
        host.ServerEventSink = sent.Add;
        var figure = NewFigure();
        host.Update(figure, ChartLayout.Create(figure, [Area]));
        var zoom = new ZoomEvent("c", 0, 1.5, 2.5, 0, 1);

        Assert.Single(made).Send(zoom);

        Assert.Same(zoom, Assert.Single(sent));
        Assert.Null(figure.SubPlots[0].XAxis.Min);
        Assert.Equal(0, repaints);
        Assert.Same(zoom, Assert.Single(handler.Events));
    }

    [Fact]
    public void AFactorySetAfterAPaint_MakesTheControllerOfTheNextPaint()
    {
        var host = new InteractionHost();
        var figure = NewFigure();
        host.Update(figure, ChartLayout.Create(figure, [Area]));
        var made = new List<StandInController>();

        host.ControllerFactory = context =>
        {
            var controller = new StandInController(context);
            made.Add(controller);
            return controller;
        };
        var attachedBeforeThePaint = host.IsAttached;
        host.HandleScroll(new ScrollInputArgs(1, 1, 0, 1));
        host.Update(figure, ChartLayout.Create(figure, [Area]));
        host.HandleScroll(new ScrollInputArgs(1, 1, 0, 1));

        Assert.False(attachedBeforeThePaint);
        Assert.Equal(["scroll"], Assert.Single(made).Calls);
        Assert.Throws<ArgumentNullException>(() => host.ControllerFactory = null!);
    }
}
