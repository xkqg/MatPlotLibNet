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
/// reader goes on with the next.
/// <para>When something else also writes the figure (a stream's scale mode), the session and that writer share a gate and each
/// holds it for the whole of what it does to the figure, so neither sees the other's work half done.</para></summary>
internal sealed class ChartSession : IRegisteredChart
{
    private readonly IChartPublisher _publisher;
    private readonly ICallerPublisher _callerPublisher;
    private readonly ChartSessionOptions _options;
    private readonly CancellationTokenSource _cts = new();
    private readonly object _gate;
    private readonly Task _reader;
    private long _droppedEvents;
    private long _faultedEvents;
    private volatile Exception? _lastFault;

    public string ChartId { get; }
    public Figure Figure { get; }
    public Channel<FigureInteractionEvent> Events { get; }

    /// <inheritdoc />
    public ChannelWriter<FigureInteractionEvent> Gestures => Events.Writer;

    public ChartSession(
        string chartId,
        Figure figure,
        IChartPublisher publisher,
        ICallerPublisher callerPublisher,
        ChartSessionOptions options,
        object? figureGate = null)
    {
        ChartId = chartId;
        Figure = figure;
        _gate = figureGate ?? new object();
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
                var batch = new ChartEventBatch();
                while (reader.TryRead(out var evt))
                {
                    await TakeAsync(evt, batch).ConfigureAwait(false);
                }

                await PublishAsync(batch, ct).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) { /* shutdown */ }
        catch (ChannelClosedException) { /* shutdown */ }
    }

    /// <summary>Deals with one event of the batch: a notification goes to the callback registered for its kind, every other
    /// event is applied to the figure, and the application's handler is told about each event that was taken. One reader serves
    /// the chart for good, so what one event does to it, it does to the chart: an exception here would end the drain while the
    /// channel keeps accepting events nobody applies. The loss is held to the event that failed. Cancellation and a closed
    /// channel are the shutdown, and still end it.</summary>
    /// <remarks>Only this method is async: the steps it awaits complete without a state machine of their own when the work is
    /// synchronous (a mutation, a handler that has already finished), which is the common case of a burst of gestures.</remarks>
    private async ValueTask TakeAsync(FigureInteractionEvent evt, ChartEventBatch batch)
    {
        try
        {
            bool taken = true;
            if (evt is FigureNotificationEvent notification)
            {
                await NotifyAsync(notification, batch).ConfigureAwait(false);
            }
            else
            {
                taken = Apply(evt, batch);
            }

            if (taken && _options.EventHandler is { } handler)
            {
                await TellHandlerAsync(handler, evt).ConfigureAwait(false);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException and not ChannelClosedException)
        {
            Fault(ex);
            batch.Faulted(evt);
        }
    }

    /// <summary>Runs the callback registered for the kind of <paramref name="evt"/>; a hover's answer becomes the viewer's
    /// tooltip. A notification with no callback for its kind runs nothing.</summary>
    private ValueTask NotifyAsync(FigureNotificationEvent evt, ChartEventBatch batch) => evt switch
    {
        BrushSelectEvent brush when _options.BrushSelectHandler is { } h => h(brush),
        HoverEvent hover when _options.HoverHandler is { } h => TooltipAsync(h, hover, batch),
        DataCursorEvent clicked when _options.DataCursorHandler is { } h => h(clicked),
        _ => default,
    };

    /// <summary>Asks the hover callback for the tooltip of <paramref name="hover"/> and keeps it for the viewer who hovered.</summary>
    private static async ValueTask TooltipAsync(Func<HoverEvent, ValueTask<string?>> handler, HoverEvent hover, ChartEventBatch batch)
    {
        if (await handler(hover).ConfigureAwait(false) is { } html && hover.CallerConnectionId is { } connectionId)
        {
            batch.Tooltip(connectionId, html);
        }
    }

    /// <summary>Applies a mutation event (axis limits, visibility, ...) under the figure's gate. One that names a subplot the
    /// figure lacks changed nothing: it is counted, no publish is made for it, and the handler is not told.</summary>
    private bool Apply(FigureInteractionEvent evt, ChartEventBatch batch)
    {
        bool applied;
        lock (_gate) { applied = evt.TryApplyTo(Figure); }
        if (applied)
        {
            batch.Applied(evt);
        }
        else
        {
            Interlocked.Increment(ref _droppedEvents);
        }

        return applied;
    }

    /// <summary>Tells the application's handler about an event that was taken. A brush over rows with keys is also said as the
    /// rows it covers, to the same viewer, right after it; only a brush has rows to say, so every other event stays off the
    /// figure's gate.</summary>
    private ValueTask TellHandlerAsync(IChartEventHandler handler, FigureInteractionEvent evt) =>
        evt is BrushSelectEvent ? TellBrushAsync(handler, evt) : handler.OnEventAsync(evt, ContextOf(evt));

    /// <summary>A brush, then the rows it covers when it covers rows with keys.</summary>
    private async ValueTask TellBrushAsync(IChartEventHandler handler, FigureInteractionEvent brush)
    {
        await handler.OnEventAsync(brush, ContextOf(brush)).ConfigureAwait(false);

        RowSelectionEvent? rows;
        lock (_gate) { rows = brush.RowsCoveredOn(Figure); }
        if (rows is not null)
        {
            await handler.OnEventAsync(rows, ContextOf(brush)).ConfigureAwait(false);
        }
    }

    /// <summary>Publishes what the batch left: the figure once when it changed, then who moved the shared view, then each
    /// viewer's tooltip. The tail of the batch is guarded like an event: a publisher that fails (a hub that is shutting down, a
    /// renderer that throws on a figure the application has just changed) costs this batch, and the reader goes on.</summary>
    private async Task PublishAsync(ChartEventBatch batch, CancellationToken ct)
    {
        if (batch.Mutated)
        {
            // The publisher renders the figure before it awaits anything, so taking the gate for the call is taking it
            // for the render: whoever else writes this figure (a stream's scale mode) cannot do it half-way through.
            await GuardedAsync(() => { lock (_gate) { return _publisher.PublishSvgAsync(ChartId, Figure, ct); } }).ConfigureAwait(false);
        }

        if (batch.Mover is { } mover)
        {
            await GuardedAsync(() => _publisher.PublishViewChangeAsync(ChartId, mover.ViewerId, mover.ViewerName, ct)).ConfigureAwait(false);
        }

        foreach (var (connectionId, html) in batch.Tooltips)
        {
            await GuardedAsync(() => _callerPublisher.SendTooltipAsync(connectionId, ChartId, html, ct)).ConfigureAwait(false);
        }
    }

    /// <summary>Runs one send of the batch's tail, counting a failure as a fault instead of letting it end the reader.</summary>
    private async Task GuardedAsync(Func<Task> send)
    {
        try
        {
            await send().ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException and not ChannelClosedException)
        {
            Fault(ex);
        }
    }

    private void Fault(Exception ex)
    {
        _lastFault = ex;
        Interlocked.Increment(ref _faultedEvents);
    }

    /// <summary>Who an event came from, for the handler: the viewer the hub stamped on it. An event that did not come over
    /// a connection has none.</summary>
    private static ChartEventContext ContextOf(FigureInteractionEvent evt) => evt.ViewerContext;

    public async ValueTask DisposeAsync()
    {
        Events.Writer.TryComplete();
        _cts.Cancel();
        try { await _reader.ConfigureAwait(false); }
        catch (OperationCanceledException) { }
        _cts.Dispose();
    }

    /// <summary>What one read cycle of a <see cref="ChartSession"/>'s reader leaves to publish: whether the figure changed, the
    /// viewer who moved the shared view last, and the latest tooltip of each viewer who hovered. The reader records every event
    /// it takes here and publishes the batch once, so a burst of events costs at most one render, one view change and one tooltip
    /// per viewer.</summary>
    /// <remarks>Only the session's single reader writes a batch, and it starts a new one for every read cycle, so a batch needs no
    /// lock.</remarks>
    internal sealed class ChartEventBatch
    {
        private Dictionary<string, string>? _tooltips;
        private FigureInteractionEvent? _lastMover;

        /// <summary>Whether an event of the batch changed the figure, or may have: one that threw half-way counts too.</summary>
        internal bool Mutated { get; private set; }

        /// <summary>The viewer the others are told moved the shared view: the last event of the batch that changed the view and
        /// came from a viewer, when it names both the viewer's id and name; otherwise <see langword="null"/>. Events are applied in
        /// the order they arrive, so the last one is the view everybody now sees.</summary>
        internal ViewMover? Mover =>
            _lastMover is { ViewerId: { } id, ViewerName: { } name } ? new ViewMover(id, name) : null;

        /// <summary>The latest tooltip of each viewer who hovered in the batch, keyed by the viewer's connection.</summary>
        internal IEnumerable<KeyValuePair<string, string>> Tooltips =>
            _tooltips ?? (IEnumerable<KeyValuePair<string, string>>)[];

        /// <summary>Records an event that changed the figure; one that changes the view and came from a viewer becomes the
        /// mover.</summary>
        internal void Applied(FigureInteractionEvent evt)
        {
            Mutated = true;
            if (evt.ViewerId is not null && evt.ChangesView) _lastMover = evt;
        }

        /// <summary>Records an event that threw: a mutation event may already have changed the figure half-way, so the batch
        /// publishes as if it had, and subscribers see what the server holds. A notification changes nothing.</summary>
        internal void Faulted(FigureInteractionEvent evt)
        {
            if (evt is not FigureNotificationEvent) Mutated = true;
        }

        /// <summary>Records the tooltip a hover of <paramref name="connectionId"/> produced; a later hover of the same connection
        /// overwrites it.</summary>
        internal void Tooltip(string connectionId, string html) => (_tooltips ??= [])[connectionId] = html;
    }

    /// <summary>The viewer who moved the shared view: the id and the name the others are told.</summary>
    /// <param name="ViewerId">The viewer's id, as the hub stamped it on the event.</param>
    /// <param name="ViewerName">The viewer's display name.</param>
    internal readonly record struct ViewMover(string ViewerId, string ViewerName);
}
