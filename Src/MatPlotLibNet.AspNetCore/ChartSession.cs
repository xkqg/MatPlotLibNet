// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Threading.Channels;
using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.AspNetCore;

/// <summary>Per-chart state holder used by <see cref="FigureRegistry"/>. Owns a single reader
/// task that drains interaction events from an unbounded channel, dispatches each event to
/// either a mutation path (axis limits / series visibility → re-render + publish) or a
/// notification path (user-registered handler, optional caller-only response). A burst of
/// events drained in one read cycle is coalesced into at most one publish + at most one
/// caller response per batch. Single-reader semantics mean no lock, no semaphore, no race.
/// An event that cannot be applied costs the chart only that event: one that names a subplot the figure lacks is
/// dropped (<see cref="DroppedEventCount"/>), one that throws is counted (<see cref="FaultedEventCount"/>), and the
/// reader goes on with the next.</summary>
internal sealed class ChartSession : IAsyncDisposable
{
    private readonly IChartPublisher _publisher;
    private readonly ICallerPublisher _callerPublisher;
    private readonly ChartSessionOptions _options;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _reader;
    private long _droppedEvents;
    private long _faultedEvents;
    private volatile Exception? _lastFault;

    public string ChartId { get; }
    public Figure Figure { get; }
    public Channel<FigureInteractionEvent> Events { get; }

    public ChartSession(
        string chartId,
        Figure figure,
        IChartPublisher publisher,
        ICallerPublisher callerPublisher,
        ChartSessionOptions options)
    {
        ChartId = chartId;
        Figure = figure;
        _publisher = publisher;
        _callerPublisher = callerPublisher;
        _options = options;
        Events = Channel.CreateUnbounded<FigureInteractionEvent>(
            new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });
        _reader = Task.Run(() => DrainAsync(_cts.Token));
    }

    /// <summary>How many events the reader took off the channel and could not apply because each named a subplot the figure
    /// does not have: a stale index (the figure changed after the client drew) or a crafted one. Such an event changes
    /// nothing and is not published for; this count is the trace it leaves.</summary>
    internal long DroppedEventCount => Interlocked.Read(ref _droppedEvents);

    /// <summary>How many events threw while the reader dealt with them: an event of the application that fails in its
    /// <c>ApplyTo</c>, or a handler registered through <see cref="ChartSessionOptions"/>. Each is the loss of that one
    /// event; the reader goes on with the next. A cancellation is not counted: it is how a session shuts down.</summary>
    internal long FaultedEventCount => Interlocked.Read(ref _faultedEvents);

    /// <summary>The exception of the event counted last by <see cref="FaultedEventCount"/>, or <see langword="null"/> when
    /// no event has failed.</summary>
    internal Exception? LastFault => _lastFault;

    private async Task DrainAsync(CancellationToken ct)
    {
        var reader = Events.Reader;
        try
        {
            while (await reader.WaitToReadAsync(ct).ConfigureAwait(false))
            {
                var mutated = false;
                string? tooltipHtml = null;
                string? tooltipConnectionId = null;

                while (reader.TryRead(out var evt))
                {
                    // One reader serves the chart for good, so what one event does to it, it does to the chart: an
                    // exception here would end the drain while the channel keeps accepting events nobody applies. The loss
                    // is held to the event that failed. Cancellation and a closed channel are the shutdown, and still end it.
                    try
                    {
                        switch (evt)
                        {
                            case BrushSelectEvent brush when _options.BrushSelectHandler is { } h:
                                await h(brush).ConfigureAwait(false);
                                break;

                            case HoverEvent hover when _options.HoverHandler is { } h:
                                var html = await h(hover).ConfigureAwait(false);
                                if (html is not null && hover.CallerConnectionId is not null)
                                {
                                    // Coalesce: later hovers in the same burst overwrite earlier ones.
                                    tooltipHtml = html;
                                    tooltipConnectionId = hover.CallerConnectionId;
                                }
                                break;

                            case DataCursorEvent clicked when _options.DataCursorHandler is { } h:
                                await h(clicked).ConfigureAwait(false);
                                break;

                            case FigureNotificationEvent:
                                // Notification event with no handler registered — drop silently.
                                break;

                            default:
                                // Mutation event — axis limits, visibility, etc. One that names a subplot the figure lacks
                                // changed nothing: it is counted, and no publish is made for it.
                                if (evt.TryApplyTo(Figure)) mutated = true;
                                else Interlocked.Increment(ref _droppedEvents);
                                break;
                        }
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException and not ChannelClosedException)
                    {
                        _lastFault = ex;
                        Interlocked.Increment(ref _faultedEvents);

                        // A mutation event that threw half-way may already have changed the figure: publish it like one that
                        // applied, so subscribers see what the server holds.
                        if (evt is not FigureNotificationEvent) mutated = true;
                    }
                }

                if (mutated)
                    await _publisher.PublishSvgAsync(ChartId, Figure, ct).ConfigureAwait(false);

                if (tooltipHtml is not null && tooltipConnectionId is not null)
                    await _callerPublisher.SendTooltipAsync(tooltipConnectionId, ChartId, tooltipHtml, ct).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) { /* shutdown */ }
        catch (ChannelClosedException) { /* shutdown */ }
    }

    public async ValueTask DisposeAsync()
    {
        Events.Writer.TryComplete();
        _cts.Cancel();
        try { await _reader.ConfigureAwait(false); }
        catch (OperationCanceledException) { }
        _cts.Dispose();
    }
}
