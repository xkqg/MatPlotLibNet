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
/// no shared mutable state exposed to callers.
///
/// v1.2.2 adds the <see cref="Register(string, Figure, Action{ChartSessionOptions})"/> overload
/// for per-chart notification handlers (brush-select, hover). The parameterless-options
/// overload stays backward-compatible for v1.2.0 users.</summary>
public sealed class FigureRegistry
{
    private readonly ConcurrentDictionary<string, ChartSession> _sessions = new();
    private readonly IChartPublisher _publisher;
    private readonly ICallerPublisher _callerPublisher;

    /// <summary>v1.2.2 primary constructor — used by DI. <see cref="ICallerPublisher"/> is
    /// registered alongside <see cref="IChartPublisher"/> by
    /// <c>SignalRExtensions.AddMatPlotLibNetSignalR</c>.</summary>
    public FigureRegistry(IChartPublisher publisher, ICallerPublisher callerPublisher)
    {
        _publisher = publisher;
        _callerPublisher = callerPublisher;
    }

    /// <summary>v1.2.0 compatibility constructor — used when the caller has no need for
    /// per-caller responses (e.g. unit tests that don't exercise <see cref="Interaction.HoverEvent"/>).
    /// Installs a no-op <see cref="ICallerPublisher"/> that silently drops tooltip sends.</summary>
    public FigureRegistry(IChartPublisher publisher)
        : this(publisher, NullCallerPublisher.Instance)
    {
    }

    private sealed class NullCallerPublisher : ICallerPublisher
    {
        public static readonly NullCallerPublisher Instance = new();
        public Task SendTooltipAsync(string connectionId, string chartId, string html,
            CancellationToken ct = default) => Task.CompletedTask;
    }

    /// <summary>Registers a figure under <paramref name="chartId"/> and starts its reader task.
    /// No notification handlers attached — use the <see cref="Register(string, Figure, Action{ChartSessionOptions})"/>
    /// overload if you want brush-select or hover callbacks. If a session already exists for
    /// the id, the previous session is disposed first.</summary>
    public void Register(string chartId, Figure figure) =>
        RegisterCore(chartId, figure, new ChartSessionOptions());

    /// <summary>Registers a figure with per-chart notification handlers (v1.2.2). Use the
    /// <paramref name="configure"/> action to attach brush-select and hover callbacks via
    /// <see cref="ChartSessionOptions.OnBrushSelect"/> and <see cref="ChartSessionOptions.OnHover"/>.</summary>
    public void Register(string chartId, Figure figure, Action<ChartSessionOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var options = new ChartSessionOptions();
        configure(options);
        RegisterCore(chartId, figure, options);
    }

    private void RegisterCore(string chartId, Figure figure, ChartSessionOptions options)
    {
        if (_sessions.TryRemove(chartId, out var existing))
            _ = existing.DisposeAsync();

        _sessions[chartId] = new ChartSession(chartId, figure, _publisher, _callerPublisher, options);
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
