// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Diagnostics;
using Microsoft.AspNetCore.SignalR.Client;

namespace MatPlotLibNet.Blazor;

/// <summary>SignalR-based implementation of <see cref="IChartSubscriptionClient"/> for Blazor and .NET clients.</summary>
public sealed class ChartSubscriptionClient : IChartSubscriptionClient
{
    private readonly Func<string, HubConnection> _hubFactory;
    private readonly Func<HubConnection, string, Func<string, string, Task>, IDisposable> _registerHandler;

    private HubConnection? _hub;
    private IDisposable? _svgUpdatedToken;
    private IDisposable? _chartUpdatedToken;
    private Func<string, string, Task>? _onSvgUpdated;
    private Func<string, string, Task>? _onChartUpdated;
    private bool _disposed;

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

        await _hub.StartAsync(ct);
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
            await _hub.InvokeAsync("Subscribe", chartId, ct);
    }

    /// <inheritdoc/>
    public async Task UnsubscribeAsync(string chartId, CancellationToken ct = default)
    {
        if (_hub is not null)
            await _hub.InvokeAsync("Unsubscribe", chartId, ct);
    }

    /// <inheritdoc/>
    public void OnSvgUpdated(Func<string, string, Task> handler) => _onSvgUpdated = handler;

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
