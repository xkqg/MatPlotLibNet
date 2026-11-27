// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Blazor.Tests;

/// <summary>The subscription client a test hands a live chart: it connects to nothing, keeps where it was sent and what it was
/// subscribed to, and hands the component's handlers what the test fires, the way a hub would. It declares exactly the members of
/// <see cref="IChartSubscriptionClient"/> that have no default body, so the interface's own subscription runs through it; a client that
/// also sends gestures derives a leaf that lists the interface again.</summary>
internal class RecordingSubscriptionClient : IChartSubscriptionClient
{
    private Func<string, string, Task>? _onSvg;
    private Func<string, string, Task>? _onJson;

    /// <summary>The hub address the client was told to connect to; <see langword="null"/> before it was.</summary>
    public string? HubUrl { get; private set; }

    /// <summary>The charts the client was subscribed to, in order.</summary>
    public List<string> Subscribed { get; } = [];

    /// <summary>Whether the client was disposed.</summary>
    public bool Disposed { get; private set; }

    public bool IsConnected => HubUrl is not null;

    public Task ConnectAsync(string hubUrl, CancellationToken ct = default)
    {
        HubUrl = hubUrl;
        return Task.CompletedTask;
    }

    public Task SubscribeAsync(string chartId, CancellationToken ct = default)
    {
        Subscribed.Add(chartId);
        return Task.CompletedTask;
    }

    public Task UnsubscribeAsync(string chartId, CancellationToken ct = default) => Task.CompletedTask;
    public void OnSvgUpdated(Func<string, string, Task> handler) => _onSvg = handler;
    public void OnChartUpdated(Func<string, string, Task> handler) => _onJson = handler;

    /// <summary>Hands the SVG handler a picture of a chart, the way a hub does, and returns once it has dealt with it.</summary>
    public void FireSvg(string chartId, string svg) => Handled(_onSvg?.Invoke(chartId, svg));

    /// <summary>Hands the JSON handler a chart, the way a hub does, and returns once it has dealt with it.</summary>
    public void FireJson(string chartId, string json) => Handled(_onJson?.Invoke(chartId, json));

    public ValueTask DisposeAsync()
    {
        Disposed = true;
        return ValueTask.CompletedTask;
    }

    private static void Handled(Task? handling)
    {
        if (handling is not null && !handling.Wait(TimeSpan.FromSeconds(5))) throw new TimeoutException("The handler did not finish.");
    }
}
