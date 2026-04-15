// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Threading.Channels;
using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.AspNetCore;

/// <summary>Per-chart state holder used by <see cref="FigureRegistry"/>. Owns a single reader
/// task that drains interaction events from an unbounded channel, applies each event to the
/// figure via <see cref="FigureInteractionEvent.ApplyTo"/>, and publishes the updated SVG.
/// A burst of events drained in one read cycle is coalesced into a single publish — every
/// event still mutates the figure in order, but only one re-render + fan-out happens per batch.
/// Single-reader semantics mean no lock, no semaphore, no race.</summary>
internal sealed class ChartSession : IAsyncDisposable
{
    private readonly IChartPublisher _publisher;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _reader;

    public string ChartId { get; }
    public Figure Figure { get; }
    public Channel<FigureInteractionEvent> Events { get; }

    public ChartSession(string chartId, Figure figure, IChartPublisher publisher)
    {
        ChartId = chartId;
        Figure = figure;
        _publisher = publisher;
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
                var dirty = false;
                while (reader.TryRead(out var evt))
                {
                    evt.ApplyTo(Figure);
                    dirty = true;
                }

                if (dirty)
                    await _publisher.PublishSvgAsync(ChartId, Figure, ct).ConfigureAwait(false);
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
