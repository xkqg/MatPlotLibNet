// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.AspNetCore;

/// <summary>Per-chart registry of live <see cref="Figure"/> instances paired with a channel-based
/// pub/sub pipeline. Each registered chart gets one <see cref="ChartSession"/> with its own
/// unbounded channel and single reader task, so hub methods simply publish events (microseconds)
/// and return — mutation + re-render happens off the hub call stack. No locks, no semaphores,
/// no shared mutable state exposed to callers.</summary>
public sealed class FigureRegistry
{
    private readonly ConcurrentDictionary<string, ChartSession> _sessions = new();
    private readonly IChartPublisher _publisher;

    public FigureRegistry(IChartPublisher publisher) => _publisher = publisher;

    /// <summary>Registers a figure under <paramref name="chartId"/> and starts its reader task.
    /// If a session already exists for the id, the previous session is disposed first.</summary>
    public void Register(string chartId, Figure figure)
    {
        if (_sessions.TryRemove(chartId, out var existing))
            _ = existing.DisposeAsync();

        _sessions[chartId] = new ChartSession(chartId, figure, _publisher);
    }

    /// <summary>Disposes the session for <paramref name="chartId"/>. Safe to call with an unknown id.</summary>
    public async ValueTask UnregisterAsync(string chartId)
    {
        if (_sessions.TryRemove(chartId, out var session))
            await session.DisposeAsync().ConfigureAwait(false);
    }

    /// <summary>Publishes an interaction event to the session for <paramref name="chartId"/>.
    /// Returns <see langword="true"/> if the write was accepted, <see langword="false"/> if the
    /// chart is not registered or the session is shutting down. Fire-and-forget — the caller
    /// does not await mutation or re-render.</summary>
    public bool Publish(string chartId, FigureInteractionEvent evt) =>
        _sessions.TryGetValue(chartId, out var session) && session.Events.Writer.TryWrite(evt);
}
