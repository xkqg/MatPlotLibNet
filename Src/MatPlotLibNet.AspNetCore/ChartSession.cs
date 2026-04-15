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
/// caller response per batch. Single-reader semantics mean no lock, no semaphore, no race.</summary>
internal sealed class ChartSession : IAsyncDisposable
{
    private readonly IChartPublisher _publisher;
    private readonly ICallerPublisher _callerPublisher;
    private readonly ChartSessionOptions _options;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _reader;

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

                        case FigureNotificationEvent:
                            // Notification event with no handler registered — drop silently.
                            break;

                        default:
                            // Mutation event — axis limits, visibility, etc.
                            evt.ApplyTo(Figure);
                            mutated = true;
                            break;
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
