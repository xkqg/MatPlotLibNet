// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Text.Json;
using MatPlotLibNet.Diagnostics;
using Microsoft.AspNetCore.SignalR.Client;

namespace MatPlotLibNet.Blazor;

/// <summary>SignalR-based implementation of <see cref="IChartSubscriptionClient"/> for Blazor and .NET clients. It is
/// also what a live chart's script reaches the server through (<see cref="IChartGestureSender"/>) and hears about the
/// other viewers from (<see cref="IChartMessageSource"/>): one connection per viewer, owned here.</summary>
public sealed class ChartSubscriptionClient : IChartSubscriptionClient, IChartGestureSender, IChartMessageSource
{
    private readonly Func<string, HubConnection> _hubFactory;
    private readonly Func<HubConnection, string, Func<string, string, Task>, IDisposable> _registerHandler;

    private HubConnection? _hub;
    private IDisposable? _svgUpdatedToken;
    private IDisposable? _chartUpdatedToken;
    private Func<string, string, Task>? _onSvgUpdated;
    private Func<string, string, Task>? _onChartUpdated;
    private bool _disposed;

    // Several callbacks can listen at once (two components showing one chart through this client), and a connection that
    // came back must join its charts again, so both are kept as sets that a callback can leave.
    private readonly ConcurrentDictionary<Guid, Func<string, string, Task>> _svgSubscribers = new();
    private readonly ConcurrentDictionary<Guid, Func<string, JsonElement, Task>> _messageSubscribers = new();
    private readonly ConcurrentDictionary<string, byte> _subscribedCharts = new();
    private readonly List<IDisposable> _messageTokens = [];

    /// <summary>Creates a client that is not yet connected; call <see cref="ConnectAsync"/> with the hub's URL to open its one connection.</summary>
    public ChartSubscriptionClient() : this(BuildHubConnection, RegisterHandler) { }

    /// <summary>Test-only seam (internal, reachable from MatPlotLibNet.Blazor.Tests via
    /// InternalsVisibleTo): lets tests observe/wrap hub construction and handler
    /// registration without altering ConnectAsync/DisposeAsync logic itself. Production
    /// code always uses the parameterless constructor, whose defaults call the very same
    /// <see cref="BuildHubConnection"/> / <see cref="RegisterHandler"/> statics.</summary>
    internal ChartSubscriptionClient(
        Func<string, HubConnection> hubFactory,
        Func<HubConnection, string, Func<string, string, Task>, IDisposable> registerHandler)
    {
        _hubFactory = hubFactory;
        _registerHandler = registerHandler;
    }

    internal static HubConnection BuildHubConnection(string hubUrl) =>
        new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .WithAutomaticReconnect()
            .Build();

    internal static IDisposable RegisterHandler(HubConnection hub, string methodName, Func<string, string, Task> handler) =>
        hub.On(methodName, handler);

    /// <inheritdoc/>
    public bool IsConnected => _hub?.State == HubConnectionState.Connected;

    /// <summary>The server's name for this connection, or <see langword="null"/> while not connected.</summary>
    internal string? ConnectionId => _hub?.ConnectionId;

    /// <inheritdoc/>
    public async Task ConnectAsync(string hubUrl, CancellationToken ct = default)
    {
        // BUG B1(a): a prior _hub must be fully torn down before rebuilding — otherwise
        // it is silently orphaned and, thanks to WithAutomaticReconnect, keeps
        // reconnecting forever (zombie connection, resource leak).
        await DisposeCurrentHubAsync();

        _hub = _hubFactory(hubUrl);

        // BUG B1(b): capture the On(...) tokens so they can be explicitly unregistered
        // (DisposeCurrentHubAsync) instead of being discarded, which made them
        // impossible to unregister short of disposing the whole hub.
        _svgUpdatedToken = _registerHandler(_hub, "UpdateChartSvg", HandleSvgUpdatedAsync);
        _chartUpdatedToken = _registerHandler(_hub, "UpdateChart", HandleChartUpdatedAsync);
        RegisterMessageHandlers(_hub);
        _hub.Reconnected += _ => HandleReconnectedAsync();

        await _hub.StartAsync(ct);
    }

    /// <summary>The messages about the other viewers and the tooltip the server computes for this one, each handed to the
    /// message subscribers as the name of the client method and its arguments.</summary>
    private void RegisterMessageHandlers(HubConnection hub)
    {
        _messageTokens.Add(hub.On<string, string>("ReceiveTooltipContent",
            (chartId, html) => DeliverMessageAsync("ReceiveTooltipContent", chartId, html)));
        _messageTokens.Add(hub.On<string, string, string, int, double, double>("ReceiveCursor",
            (chartId, viewerId, viewerName, axesIndex, x, y) =>
                DeliverMessageAsync("ReceiveCursor", chartId, viewerId, viewerName, axesIndex, x, y)));
        _messageTokens.Add(hub.On<string, string, string>("ViewChanged",
            (chartId, viewerId, viewerName) => DeliverMessageAsync("ViewChanged", chartId, viewerId, viewerName)));
        _messageTokens.Add(hub.On<string, string>("ViewerLeft",
            (chartId, viewerId) => DeliverMessageAsync("ViewerLeft", chartId, viewerId)));
    }

    private async Task DeliverMessageAsync(string method, params object[] args)
    {
        if (_messageSubscribers.IsEmpty) return;

        var json = JsonSerializer.SerializeToElement(args);
        foreach (var subscriber in _messageSubscribers.Values)
            await subscriber(method, json);
    }

    /// <summary>A lost connection that came back is a new connection to the server: it has none of the groups the old one
    /// joined. Every chart this client had subscribed to is subscribed to again, so a chart does not go silent for good
    /// after a network blip.</summary>
    internal async Task HandleReconnectedAsync()
    {
        var hub = _hub;
        if (hub is null) return;

        foreach (var chartId in _subscribedCharts.Keys)
        {
            try
            {
                await hub.InvokeAsync("Subscribe", chartId);
            }
            catch (Exception ex)
            {
                ChartDiagnostics.Emit(new ChartDiagnostic(
                    nameof(ChartSubscriptionClient),
                    $"Subscribing to '{chartId}' again after a reconnect failed: {ex.Message}",
                    ex));
            }
        }
    }

    /// <summary>Disposes the current hub's subscription tokens and the hub itself (if
    /// present), then clears both fields. Shared by <see cref="ConnectAsync"/> (before
    /// rebuilding a new hub) and <see cref="DisposeAsync"/> (final teardown).</summary>
    private async Task DisposeCurrentHubAsync()
    {
        _svgUpdatedToken?.Dispose();
        _svgUpdatedToken = null;
        _chartUpdatedToken?.Dispose();
        _chartUpdatedToken = null;
        foreach (var token in _messageTokens) token.Dispose();
        _messageTokens.Clear();

        if (_hub is not null)
        {
            var hub = _hub;
            _hub = null;
            try
            {
                await hub.DisposeAsync();
            }
            catch (Exception ex)
            {
                // Kept as catch-all (not narrowed to e.g. ObjectDisposedException/
                // InvalidOperationException) on purpose: Dispose/DisposeAsync must never throw
                // (standard .NET guidance — callers use `await using`/finally blocks that assume
                // teardown cannot fail), and an already-closing/closed hub racing with this dispose
                // is legitimate, not a bug. Narrowing here would let an unanticipated hub-internal
                // exception escape a teardown path, which is strictly worse than today. What
                // changes is observability: report the race via ChartDiagnostics so operators can
                // see it instead of it vanishing silently.
                ChartDiagnostics.Emit(new ChartDiagnostic(
                    nameof(ChartSubscriptionClient),
                    $"Hub dispose raced during teardown and was suppressed: {ex.Message}",
                    ex));
            }
        }
    }

    internal async Task HandleSvgUpdatedAsync(string id, string svg)
    {
        if (_onSvgUpdated is not null)
            await _onSvgUpdated(id, svg);

        foreach (var subscriber in _svgSubscribers.Values)
            await subscriber(id, svg);
    }

    internal async Task HandleChartUpdatedAsync(string id, string json)
    {
        if (_onChartUpdated is not null)
            await _onChartUpdated(id, json);
    }

    /// <inheritdoc/>
    public async Task SubscribeAsync(string chartId, CancellationToken ct = default)
    {
        if (_hub is not null)
        {
            await _hub.InvokeAsync("Subscribe", chartId, ct);
            _subscribedCharts[chartId] = 0;
        }
    }

    /// <inheritdoc/>
    public async Task UnsubscribeAsync(string chartId, CancellationToken ct = default)
    {
        if (_hub is not null)
        {
            _subscribedCharts.TryRemove(chartId, out _);
            await _hub.InvokeAsync("Unsubscribe", chartId, ct);
        }
    }

    /// <inheritdoc/>
    public void OnSvgUpdated(Func<string, string, Task> handler) => _onSvgUpdated = handler;

    /// <inheritdoc/>
    public IDisposable SubscribeSvgUpdates(Func<string, string, Task> handler)
    {
        var key = Guid.NewGuid();
        _svgSubscribers[key] = handler;
        return new Unsubscription(() => _svgSubscribers.TryRemove(key, out _));
    }

    /// <inheritdoc/>
    public IDisposable SubscribeMessages(Func<string, JsonElement, Task> handler)
    {
        var key = Guid.NewGuid();
        _messageSubscribers[key] = handler;
        return new Unsubscription(() => _messageSubscribers.TryRemove(key, out _));
    }

    /// <inheritdoc/>
    public Task SendGestureAsync(string method, JsonElement payload, CancellationToken ct = default) =>
        _hub is { State: HubConnectionState.Connected } hub ? hub.SendAsync(method, payload, ct) : Task.CompletedTask;

    private sealed class Unsubscription(Action remove) : IDisposable
    {
        public void Dispose() => remove();
    }

    /// <inheritdoc/>
    public void OnChartUpdated(Func<string, string, Task> handler) => _onChartUpdated = handler;

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        await DisposeCurrentHubAsync();
    }
}
