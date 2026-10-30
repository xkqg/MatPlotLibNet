// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.AspNetCore.Tests;

/// <summary>A handler that does real work awaits it, and the session waits for it before it takes the next event: what it sees
/// is the figure after the event, in the order the events arrived, however long it takes. The handlers of the other tests
/// complete at once; this one does not.</summary>
public class ChartSessionAsyncHandlerTests
{
    private sealed class SlowRecording : IChartEventHandler
    {
        public ConcurrentQueue<FigureInteractionEvent> Seen { get; } = new();

        public async ValueTask OnEventAsync(FigureInteractionEvent evt, ChartEventContext context)
        {
            await Task.Yield();
            Seen.Enqueue(evt);
        }
    }

    private sealed class SilentPublisher : IChartPublisher
    {
        public Task PublishAsync(string chartId, Figure figure, CancellationToken ct = default) => Task.CompletedTask;

        public Task PublishSvgAsync(string chartId, Figure figure, CancellationToken ct = default) => Task.CompletedTask;
    }

    private static Figure Keyed()
    {
        var figure = new Figure { Width = 500, Height = 400, ChartId = "c" };
        figure.AddSubPlot(1, 1, 1).Scatter([1.0, 2.0, 3.0, 4.0], [10.0, 20.0, 30.0, 40.0]).RowKeys = ["a", "b", "c", "d"];
        return figure;
    }

    [Fact]
    public async Task AHandlerThatCompletesLater_IsWaitedFor_ForTheEventAndForTheRowsABrushCovers()
    {
        var handler = new SlowRecording();
        var options = new ChartSessionOptions();
        options.Handle(handler);
        await using var session = new ChartSession("c", Keyed(), new SilentPublisher(), new NullCallerPublisher(), options);

        Assert.True(session.Events.Writer.TryWrite(new ZoomEvent("c", 0, 0.5, 4.5, 5, 45)));
        Assert.True(session.Events.Writer.TryWrite(new BrushSelectEvent("c", 0, 1.5, 5, 3.5, 35)));
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);
        while (handler.Seen.Count < 3 && DateTime.UtcNow < deadline) await Task.Delay(10, TestContext.Current.CancellationToken);

        Assert.Equal(
            [typeof(ZoomEvent), typeof(BrushSelectEvent), typeof(RowSelectionEvent)],
            handler.Seen.Select(e => e.GetType()).ToArray());
    }
}
