// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Streaming;

namespace MatPlotLibNet.AspNetCore;

/// <summary>Per-chart registry of live <see cref="Figure"/> instances paired with a channel-based
/// pub/sub pipeline. Each registered chart gets one <see cref="ChartSession"/> with its own
/// unbounded channel and single reader task, so hub methods simply publish events (microseconds)
/// and return — mutation + re-render happens off the hub call stack. No locks, no semaphores,
/// no shared mutable state exposed to callers.
///
/// The <see cref="Register(string, Figure, Action{ChartSessionOptions})"/> overload attaches
/// per-chart notification handlers (brush-select, hover); the overload without options attaches none.</summary>
public sealed class FigureRegistry
{
    private readonly ConcurrentDictionary<string, ChartSession> _sessions = new();
    private readonly ConcurrentDictionary<string, StreamingChartSession> _streamingSessions = new();
    private readonly IChartPublisher _publisher;
    private readonly ICallerPublisher _callerPublisher;

    /// <summary>The constructor dependency injection uses. <see cref="ICallerPublisher"/> is
    /// registered alongside <see cref="IChartPublisher"/> by
    /// <c>SignalRExtensions.AddMatPlotLibNetSignalR</c>.</summary>
    public FigureRegistry(IChartPublisher publisher, ICallerPublisher callerPublisher)
    {
        _publisher = publisher;
        _callerPublisher = callerPublisher;
    }

    /// <summary>Constructor for a caller that has no need for
    /// per-caller responses (e.g. unit tests that don't exercise <see cref="Interaction.HoverEvent"/>).
    /// Installs a no-op <see cref="ICallerPublisher"/> that silently drops tooltip sends.</summary>
    public FigureRegistry(IChartPublisher publisher)
        : this(publisher, NullCallerPublisher.Instance)
    {
    }

    /// <summary>Registers a figure under <paramref name="chartId"/> and starts its reader task.
    /// No notification handlers attached — use the <see cref="Register(string, Figure, Action{ChartSessionOptions})"/>
    /// overload if you want brush-select or hover callbacks. If a session already exists for
    /// the id, the previous session is disposed first.</summary>
    public void Register(string chartId, Figure figure) =>
        RegisterCore(chartId, figure, new ChartSessionOptions());

    /// <summary>Registers a figure with per-chart notification handlers. Use the
    /// <paramref name="configure"/> action to attach brush-select and hover callbacks via
    /// <see cref="ChartSessionOptions.OnBrushSelect"/> and <see cref="ChartSessionOptions.OnHover"/>.</summary>
    public void Register(string chartId, Figure figure, Action<ChartSessionOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var options = new ChartSessionOptions();
        configure(options);
        RegisterCore(chartId, figure, options);
    }

    /// <summary>Registers a streaming figure for server-push live updates. The streaming figure's
    /// <see cref="StreamingFigure.RenderRequested"/> event triggers automatic SVG publishing to
    /// all connected clients via <see cref="IChartPublisher"/>.</summary>
    public void RegisterStreaming(string chartId, StreamingFigure streamingFigure) =>
        RegisterStreamingCore(chartId, streamingFigure, new ChartSessionOptions());

    /// <summary>Registers a streaming figure with per-chart handlers, as
    /// <see cref="Register(string, Figure, Action{ChartSessionOptions})"/> does for a figure that does not stream. A viewer
    /// can zoom, pan and press Home on a streaming chart: the gestures are applied to the figure the stream also writes,
    /// and an axes the viewer has moved is left where they put it until Home hands it back to the scale mode.</summary>
    public void RegisterStreaming(string chartId, StreamingFigure streamingFigure, Action<ChartSessionOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var options = new ChartSessionOptions();
        configure(options);
        RegisterStreamingCore(chartId, streamingFigure, options);
    }

    private void RegisterStreamingCore(string chartId, StreamingFigure streamingFigure, ChartSessionOptions options)
    {
        // The session that is replaced is the one this registration displaced, whichever registration ran first: two
        // registrations of one id at the same time cannot leave a session that nobody disposes.
        StreamingChartSession? displaced = null;
        var session = new StreamingChartSession(chartId, streamingFigure, _publisher, _callerPublisher, options);
        _streamingSessions.AddOrUpdate(chartId, session, (_, existing) => { displaced = existing; return session; });
        if (displaced is not null) _ = displaced.DisposeAsync();
    }

    private void RegisterCore(string chartId, Figure figure, ChartSessionOptions options)
    {
        ChartSession? displaced = null;
        var session = new ChartSession(chartId, figure, _publisher, _callerPublisher, options);
        _sessions.AddOrUpdate(chartId, session, (_, existing) => { displaced = existing; return session; });
        if (displaced is not null) _ = displaced.DisposeAsync();
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
        _sessions.TryGetValue(chartId, out var session) ? session.Events.Writer.TryWrite(evt)
        : _streamingSessions.TryGetValue(chartId, out var streaming) && streaming.Gestures.TryWrite(evt);
}
