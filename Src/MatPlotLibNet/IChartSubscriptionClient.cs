// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet;

/// <summary>
/// Client-side abstraction for subscribing to real-time chart updates via SignalR.
/// Implement this interface per UI framework (Blazor C#, Angular TypeScript, etc.).
/// </summary>
public interface IChartSubscriptionClient : IAsyncDisposable
{
    /// <summary>Connects to the SignalR hub at the specified URL.</summary>
    Task ConnectAsync(string hubUrl, CancellationToken ct = default);

    /// <summary>Subscribes to updates for the specified chart.</summary>
    Task SubscribeAsync(string chartId, CancellationToken ct = default);

    /// <summary>Unsubscribes from chart updates.</summary>
    Task UnsubscribeAsync(string chartId, CancellationToken ct = default);

    /// <summary>Registers a callback invoked when an SVG update is received. Parameters: chartId, svg.</summary>
    void OnSvgUpdated(Func<string, string, Task> handler);

    /// <summary>Registers a callback invoked when a JSON chart update is received. Parameters: chartId, json.</summary>
    void OnChartUpdated(Func<string, string, Task> handler);

    /// <summary>Gets whether the client is currently connected to the hub.</summary>
    bool IsConnected { get; }
}
