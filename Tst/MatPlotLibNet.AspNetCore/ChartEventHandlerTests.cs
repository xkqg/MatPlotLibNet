// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.AspNetCore.Tests;

/// <summary>One handler for everything a viewer does to a chart. A chart used to offer a callback for each of three
/// gestures (a brush, a hover, a click) and nothing for the rest, so a zoom, a pan, a legend toggle or a reset could not
/// be observed in .NET code at all. <see cref="IChartEventHandler"/> receives every event once, after the callback written
/// for its kind, and a session survives whatever the handler, or the publisher behind it, throws.</summary>
public class ChartEventHandlerTests
{
    [Fact]
    public async Task TheHandler_SeesEveryEventOnce_MutationsAndNotificationsAlike()
    {
        var handler = new RecordingChartEventHandler();
        var rig = new SessionRig(handler);

        rig.Send(new ZoomEvent("c1", 0, 0.5, 2.5, 1, 9));
        rig.Send(new LegendToggleEvent("c1", 0, 0));
        rig.Send(new BrushSelectEvent("c1", 0, 0, 0, 1, 1));
        rig.Send(new ResetEvent("c1", 0, 1, 3, 4, 6));
        await rig.DrainedAsync();

        Assert.Equal(["ZoomEvent", "LegendToggleEvent", "BrushSelectEvent", "ResetEvent", "DataCursorEvent"],
            handler.Names);
        await rig.Session.DisposeAsync();
    }

    [Fact]
    public async Task TheCallbackOfTheKind_RunsBeforeTheHandler()
    {
        var order = new ConcurrentQueue<string>();
        var handler = new RecordingChartEventHandler { WhileHandling = () => order.Enqueue("handler") };
        var rig = new SessionRig(handler, o => o.OnBrushSelect(_ =>
        {
            order.Enqueue("callback");
            return default;
        }));

        rig.Send(new BrushSelectEvent("c1", 0, 0, 0, 1, 1));
        await rig.DrainedAsync();

        Assert.Equal(["callback", "handler"], order.Take(2));
        await rig.Session.DisposeAsync();
    }

    [Fact]
    public async Task AnEventTheFigureCouldNotTake_IsNotPassedToTheHandler()
    {
        var handler = new RecordingChartEventHandler();
        var rig = new SessionRig(handler);

        rig.Send(new ZoomEvent("c1", 9, 0, 1, 0, 1));   // names a subplot the figure lacks
        await rig.DrainedAsync();

        Assert.DoesNotContain(handler.Events, e => e is ZoomEvent);
        Assert.Equal(1, rig.Session.DroppedEventCount);
        await rig.Session.DisposeAsync();
    }

    [Fact]
    public async Task AHandlerThatThrows_IsCounted_AndTheNextEventIsStillTaken()
    {
        var rig = new SessionRig(new RecordingChartEventHandler { ThrowAtOnce = new InvalidOperationException("the handler failed"), ThrowWhen = e => e is ZoomEvent });

        rig.Send(new ZoomEvent("c1", 0, 0.5, 2.5, 1, 9));
        rig.Send(new LegendToggleEvent("c1", 0, 0));
        await rig.DrainedAsync();

        Assert.Equal(1, rig.Session.FaultedEventCount);
        Assert.IsType<InvalidOperationException>(rig.Session.LastFault);
        Assert.Equal(0.5, rig.Figure.SubPlots[0].XAxis.Min!.Value, 6);              // the zoom itself was applied before the handler failed
        Assert.False(((MatPlotLibNet.Models.Series.ChartSeries)rig.Figure.SubPlots[0].Series[0]).Visible);   // and the event behind it was taken
        await rig.Session.DisposeAsync();
    }

    [Fact]
    public async Task AFailedPublish_DoesNotEndTheReader()
    {
        var rig = new SessionRig(handler: null);
        bool failed = false;
        rig.Publisher.WhilePublishing = () =>
        {
            if (!failed)
            {
                failed = true;
                throw new InvalidOperationException("the publish failed");
            }
        };

        rig.Send(new ZoomEvent("c1", 0, 0.5, 2.5, 1, 9));
        await rig.DrainedAsync();
        // The marker is dealt with before the batch's publish is made, so the fault is awaited, not read at once.
        await Eventually.HoldsAsync(() => rig.Session.FaultedEventCount == 1);   // the publish failure is counted like any other fault

        rig.Send(new ZoomEvent("c1", 0, 1.0, 2.0, 2, 8));
        await rig.DrainedAsync();

        Assert.Equal(1.0, rig.Figure.SubPlots[0].XAxis.Min!.Value, 6);
        await rig.Publisher.WaitForAtLeastAsync(2);            // the second zoom was published: the reader was still alive
        await rig.Session.DisposeAsync();
    }

    [Fact]
    public async Task TwoViewersHoveringInOneBurst_EachGetTheirOwnTooltip()
    {
        var rig = new SessionRig(handler: null, o => o.OnHover(e => ValueTask.FromResult<string?>($"tip for {e.CallerConnectionId}")));

        rig.Send(new HoverEvent("c1", 0, 1, 4, CallerConnectionId: "A"));
        rig.Send(new HoverEvent("c1", 0, 2, 5, CallerConnectionId: "B"));
        await rig.DrainedAsync();

        Assert.Equal(["A", "B"], rig.Callers.Calls.Select(s => s.ConnectionId).Order());
        Assert.Equal("tip for A", rig.Callers.Calls.Single(s => s.ConnectionId == "A").Html);
        await rig.Session.DisposeAsync();
    }

    [Fact]
    public async Task ABurstOfHoversFromOneViewer_IsAnsweredOnce_WithTheLatest()
    {
        var rig = new SessionRig(handler: null, o => o.OnHover(e => ValueTask.FromResult<string?>($"x={e.X}")));

        rig.Send(new HoverEvent("c1", 0, 1, 4, CallerConnectionId: "A"));
        rig.Send(new HoverEvent("c1", 0, 2, 5, CallerConnectionId: "A"));
        rig.Send(new HoverEvent("c1", 0, 3, 6, CallerConnectionId: "A"));
        await rig.DrainedAsync();

        var only = Assert.Single(rig.Callers.Calls);
        Assert.Equal("x=3", only.Html);
        await rig.Session.DisposeAsync();
    }
}
