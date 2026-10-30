// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using Avalonia.Input;
using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Streaming;

namespace MatPlotLibNet.Avalonia.Tests;

/// <summary>What the Avalonia package decides for itself, apart from the platform events it forwards: which button an Avalonia
/// pointer change is for, what the timer says when a frame is due, what the controls hand back about their wiring, and what
/// the streaming control lets go of when its figure is replaced.</summary>
public class AvaloniaGlueTests
{
    [Theory]
    [InlineData(PointerUpdateKind.LeftButtonPressed, PointerButton.Left)]
    [InlineData(PointerUpdateKind.RightButtonPressed, PointerButton.Right)]
    [InlineData(PointerUpdateKind.MiddleButtonPressed, PointerButton.Middle)]
    [InlineData(PointerUpdateKind.LeftButtonReleased, PointerButton.None)]
    [InlineData(PointerUpdateKind.Other, PointerButton.None)]
    public void APointerPressed_IsForTheButtonThatWentDown(PointerUpdateKind kind, PointerButton expected) =>
        Assert.Equal(expected, AvaloniaInputAdapter.ButtonPressedBy(kind));

    [Theory]
    [InlineData(PointerUpdateKind.LeftButtonReleased, PointerButton.Left)]
    [InlineData(PointerUpdateKind.RightButtonReleased, PointerButton.Right)]
    [InlineData(PointerUpdateKind.MiddleButtonReleased, PointerButton.Middle)]
    [InlineData(PointerUpdateKind.LeftButtonPressed, PointerButton.None)]
    [InlineData(PointerUpdateKind.Other, PointerButton.None)]
    public void APointerReleased_IsForTheButtonThatCameUp(PointerUpdateKind kind, PointerButton expected) =>
        Assert.Equal(expected, AvaloniaInputAdapter.ButtonReleasedBy(kind));

    [Theory]
    [InlineData(true, false, false, PointerButton.Left)]
    [InlineData(true, true, true, PointerButton.Left)]
    [InlineData(false, true, true, PointerButton.Right)]
    [InlineData(false, false, true, PointerButton.Middle)]
    [InlineData(false, false, false, PointerButton.None)]
    public void APointerMoved_IsMadeWithTheLeftButtonBeforeTheRightBeforeTheMiddle(bool left, bool right, bool middle, PointerButton expected) =>
        Assert.Equal(expected, AvaloniaInputAdapter.ButtonHeld(left, right, middle));

    [Fact]
    public void TheTimer_SaysAFrameIsDue_ToWhoeverListens_AndToNobodyWhenNobodyDoes()
    {
        var timer = new AvaloniaAnimationTimer();
        timer.RaiseTick();   // nobody listens: nothing happens
        int ticks = 0;
        timer.Tick += (sender, _) =>
        {
            Assert.Same(timer, sender);
            ticks++;
        };

        timer.RaiseTick();

        Assert.Equal(1, ticks);
    }

    [Fact]
    public async Task ServerInteraction_MakesTheControlInteractive_AndSendsItsGesturesThroughTheDelegate()
    {
        var control = new MplChartControl();
        var sent = new List<string>();

        var same = control.WithServerInteraction((method, _) =>
        {
            sent.Add(method);
            return Task.CompletedTask;
        });

        Assert.Same(control, same);
        Assert.True(control.IsInteractive);
        Assert.NotNull(control.ServerEventSink);
        control.ServerEventSink!(new ZoomEvent("c", 0, 0, 1, 0, 1));
        await Task.Yield();
        Assert.Equal(["OnZoom"], sent);
    }

    [Fact]
    public void AControlThatIsNotBeingBrushed_HasNoBrushBand()
    {
        var control = new MplChartControl();

        Assert.Null(control.ActiveBrushSelect);
        Assert.Null(((IInteractiveChart)control).ActiveBrushSelect);
        Assert.Null(((IInteractiveChart)new MplStreamingChartControl()).ActiveBrushSelect);
    }

    [Fact]
    public void TheStreamingControl_ReadsBackWhatItWasGiven_AndLetsGoOfAFigureItIsTakenOffOf()
    {
        using var first = new StreamingFigure(Plt.Create().Plot([1.0, 2.0], [3.0, 4.0]).Build());
        using var second = new StreamingFigure(Plt.Create().Plot([1.0, 2.0], [3.0, 4.0]).Build());
        var handler = new SilentHandler();
        var control = new MplStreamingChartControl { IsInteractive = true, ChartEventHandler = handler };

        control.StreamingFigure = first;
        Assert.Same(first, control.StreamingFigure);
        control.StreamingFigure = second;

        Assert.True(control.IsInteractive);
        Assert.Same(handler, control.ChartEventHandler);
        Assert.Same(second, control.StreamingFigure);
        Assert.Equal(0, Listeners(first));
        Assert.Equal(1, Listeners(second));
    }

    private static int Listeners(StreamingFigure figure) =>
        ((typeof(StreamingFigure).GetField(nameof(StreamingFigure.RenderRequested), BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(figure) as Delegate)?.GetInvocationList().Length) ?? 0;

    private sealed class SilentHandler : IChartEventHandler
    {
        public ValueTask OnEventAsync(FigureInteractionEvent evt, ChartEventContext context) => default;
    }
}
