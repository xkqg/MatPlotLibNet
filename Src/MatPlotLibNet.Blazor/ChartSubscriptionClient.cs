// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.SignalR.Client;

namespace MatPlotLibNet.Blazor;

/// <summary>SignalR-based implementation of <see cref="IChartSubscriptionClient"/> for Blazor and .NET clients.</summary>
public sealed class ChartSubscriptionClient : IChartSubscriptionClient
{
    private HubConnection? _hub;
    private Func<string, string, Task>? _onSvgUpdated;
    private Func<string, string, Task>? _onChartUpdated;

    /// <inheritdoc/>
    public bool IsConnected => _hub?.State == HubConnectionState.Connected;

    /// <inheritdoc/>
    public async Task ConnectAsync(string hubUrl, CancellationToken ct = default)
    {
        _hub = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .WithAutomaticReconnect()
            .Build();

        _hub.On<string, string>("UpdateChartSvg", async (id, svg) =>
        {
            if (_onSvgUpdated is not null)
                await _onSvgUpdated(id, svg);
        });

        _hub.On<string, string>("UpdateChart", async (id, json) =>
        {
            if (_onChartUpdated is not null)
                await _onChartUpdated(id, json);
        });

        await _hub.StartAsync(ct);
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
        if (_hub is not null)
        {
            try { await _hub.DisposeAsync(); }
            catch { /* connection may already be closed */ }
        }
    }
}
