// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

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

    /// <summary>Registers a callback invoked when an SVG update is received, and returns a token that removes it again.
    /// Several callbacks may be registered at once, each of which is called for every update: two components that show the
    /// same chart through one client each get their own. <see cref="OnSvgUpdated"/> keeps its meaning of ONE callback that
    /// a later call replaces, and is still called alongside these.
    /// <para>Default interface member: a client that only has <see cref="OnSvgUpdated"/> registers the callback with it
    /// (so a later registration replaces it, as it always did) and returns a token that removes nothing.</para></summary>
    /// <param name="handler">Called with the chart id and the SVG.</param>
    /// <returns>A token whose disposal removes the callback.</returns>
    IDisposable SubscribeSvgUpdates(Func<string, string, Task> handler)
    {
        OnSvgUpdated(handler);
        return NullSubscription.Instance;
    }

    /// <summary>Registers a callback invoked when a JSON chart update is received. Parameters: chartId, json.</summary>
    void OnChartUpdated(Func<string, string, Task> handler);

    /// <summary><see langword="true"/> while the connection to the hub is open; <see langword="false"/> before
    /// <see cref="ConnectAsync"/> and after the client is disposed.</summary>
    bool IsConnected { get; }
}
