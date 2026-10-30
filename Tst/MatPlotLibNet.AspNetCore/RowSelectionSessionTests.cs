// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.AspNetCore.Tests;

/// <summary>A viewer's brush reaches the server as a rectangle in data units. When the chart's rows have keys the server also
/// tells the application which rows the rectangle covers, to the handler that already hears every event, right after the brush
/// and naming the same viewer. The figure is not touched, so nothing is republished for it.</summary>
public class RowSelectionSessionTests
{
    private sealed class Recording : IChartEventHandler
    {
        public ConcurrentQueue<(FigureInteractionEvent Event, ChartEventContext Context)> Seen { get; } = new();

        public ValueTask OnEventAsync(FigureInteractionEvent evt, ChartEventContext context)
        {
            Seen.Enqueue((evt, context));
            return default;
        }
    }

    private sealed class Publisher : IChartPublisher
    {
        public int Svgs;

        public Task PublishAsync(string chartId, Figure figure, CancellationToken ct = default) => Task.CompletedTask;

        public Task PublishSvgAsync(string chartId, Figure figure, CancellationToken ct = default)
        {
            Interlocked.Increment(ref Svgs);
            return Task.CompletedTask;
        }
    }

    private sealed class Callers : ICallerPublisher
    {
        public Task SendTooltipAsync(string connectionId, string chartId, string html, CancellationToken ct = default) => Task.CompletedTask;
    }

    private static async Task<(Recording Handler, Publisher Publisher)> Run(Figure figure, params FigureInteractionEvent[] events)
    {
        var handler = new Recording();
        var publisher = new Publisher();
        var reached = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var options = new ChartSessionOptions().OnDataCursor(_ =>
        {
            reached.TrySetResult();
            return default;
        });
        options.Handle(handler);
        await using var session = new ChartSession("c", figure, publisher, new Callers(), options);

        foreach (var evt in events) Assert.True(session.Events.Writer.TryWrite(evt));
        Assert.True(session.Events.Writer.TryWrite(new DataCursorEvent("c", 0, new PinnedAnnotation("end", 0, 0, 0, 0, 0))));
        await reached.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        return (handler, publisher);
    }

    private static Figure Keyed()
    {
        var figure = new Figure { Width = 500, Height = 400, ChartId = "c" };
        figure.AddSubPlot(1, 1, 1).Scatter([1.0, 2.0, 3.0, 4.0], [10.0, 20.0, 30.0, 40.0]).RowKeys = ["a", "b", "c", "d"];
        return figure;
    }

    [Fact]
    public async Task ABrushOverKeyedRows_IsFollowedByTheRowsItCovers_ForTheSameViewer()
    {
        var brush = new BrushSelectEvent("c", 0, 1.5, 5, 3.5, 35).StampedBy("conn-1", "Viewer 1");

        var (handler, _) = await Run(Keyed(), brush);

        var seen = handler.Seen.ToArray();
        Assert.IsType<BrushSelectEvent>(seen[0].Event);
        var rows = Assert.IsType<RowSelectionEvent>(seen[1].Event);
        Assert.Equal(["b", "c"], rows.Rows);
        Assert.Equal("conn-1", seen[1].Context.ViewerId);
        Assert.Equal("Viewer 1", seen[1].Context.ViewerName);
        Assert.Equal("conn-1", rows.ViewerId);
    }

    [Fact]
    public async Task ABrushOverRowsWithNoKeys_IsTheBrushAlone()
    {
        var figure = new Figure { Width = 500, Height = 400, ChartId = "c" };
        figure.AddSubPlot(1, 1, 1).Scatter([1.0, 2.0], [10.0, 20.0]);

        var (handler, _) = await Run(figure, new BrushSelectEvent("c", 0, 0, 0, 5, 50));

        Assert.DoesNotContain(handler.Seen, s => s.Event is RowSelectionEvent);
        Assert.Contains(handler.Seen, s => s.Event is BrushSelectEvent);
    }

    [Fact]
    public async Task TheRows_AreNotAReasonToPublishTheFigureAgain()
    {
        var (_, publisher) = await Run(Keyed(), new BrushSelectEvent("c", 0, 1.5, 5, 3.5, 35));

        Assert.Equal(0, publisher.Svgs);   // a brush and the rows it covers observe the figure; they never change it
    }
}
