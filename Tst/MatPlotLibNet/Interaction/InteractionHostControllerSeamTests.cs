// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Interaction;

/// <summary><see cref="IInteractionController"/> had one implementation and no reader: the host every native control shares
/// held the concrete controller, so the interface described a seam nothing went through. The host now talks to the
/// controller through the interface (and hears the clicked point and every handled event through the internal interface
/// beside it), and is built with the factory that makes a controller; these tests hand it a stand-in.</summary>
public class InteractionHostControllerSeamTests
{
    private static readonly Rect Area = new(10, 10, 200, 100);

    private sealed class StandInController : IHostedInteractionController
    {
        public readonly List<string> Calls = [];
        public IChartLayout? Layout;
        public HoverTooltipContent? Tooltip { get; set; }

        public event Action? InvalidateRequested;
        public event Action<PinnedAnnotation>? DataPointClicked;
        public event Action<FigureInteractionEvent>? EventHandled;

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
        public void Click(PinnedAnnotation point) => DataPointClicked?.Invoke(point);
        public void Handled(FigureInteractionEvent evt) => EventHandled?.Invoke(evt);
    }

    private static Figure NewFigure()
    {
        var figure = Plt.Create().Plot([1.0, 2.0, 3.0], [1.0, 4.0, 2.0]).Build();
        figure.ChartId = "c";
        return figure;
    }

    [Fact]
    public void TheHost_BuildsItsControllerThroughTheFactory_AndTalksToItThroughTheInterface()
    {
        var made = new List<(Figure Figure, Action<FigureInteractionEvent>? Sink)>();
        var controller = new StandInController { Tooltip = new HoverTooltipContent("y = 4", 1, 2) };
        var host = new InteractionHost((figure, _, sink) =>
        {
            made.Add((figure, sink));
            return controller;
        });
        var figure = NewFigure();
        var firstLayout = ChartLayout.Create(figure, [Area]);
        var secondLayout = ChartLayout.Create(figure, [Area]);

        host.Update(figure, firstLayout);
        host.Update(figure, secondLayout);
        host.HandlePointerPressed(new PointerInputArgs(1, 1, PointerButton.Left, ModifierKeys.None));
        host.HandlePointerMoved(new PointerInputArgs(1, 1, PointerButton.None, ModifierKeys.None));
        host.HandlePointerReleased(new PointerInputArgs(1, 1, PointerButton.Left, ModifierKeys.None));
        host.HandleScroll(new ScrollInputArgs(1, 1, 0, 1));
        host.HandleKeyDown(new KeyInputArgs("Home"));

        Assert.Same(figure, Assert.Single(made).Figure);              // one controller for one figure
        Assert.Null(made[0].Sink);                                    // no server sink: the controller keeps the figure
        Assert.Same(secondLayout, controller.Layout);                 // a repaint replaced the layout only
        Assert.Equal(["pressed", "moved", "released", "scroll", "key"], controller.Calls);
        Assert.Equal("y = 4", host.ActiveTooltip?.Text);
    }

    [Fact]
    public void WhatTheControllerSays_ReachesTheControl()
    {
        var controller = new StandInController();
        var handler = new RecordingChartEventHandler();
        int repaints = 0;
        var host = new InteractionHost((_, _, _) => controller, () => repaints++) { Handler = handler };
        var clicked = new List<PinnedAnnotation>();
        host.DataPointClicked += clicked.Add;
        var figure = NewFigure();
        host.Update(figure, ChartLayout.Create(figure, [Area]));
        var point = new PinnedAnnotation("s", 1, 2, 3, 4, 0);
        var zoom = new ZoomEvent("c", 0, 0, 1, 0, 1);

        controller.Invalidate();
        controller.Click(point);
        controller.Handled(zoom);

        Assert.Equal(1, repaints);
        Assert.Equal(point, Assert.Single(clicked));
        Assert.Same(zoom, Assert.Single(handler.Events));
    }

    [Fact]
    public void AServerSink_IsHandedToTheFactory()
    {
        Action<FigureInteractionEvent>? given = null;
        var host = new InteractionHost((_, _, sink) =>
        {
            given = sink;
            return new StandInController();
        });
        Action<FigureInteractionEvent> sink = _ => { };
        host.ServerEventSink = sink;
        var figure = NewFigure();

        host.Update(figure, ChartLayout.Create(figure, [Area]));

        Assert.Same(sink, given);
    }
}
