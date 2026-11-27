// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Bunit;
using MatPlotLibNet.AspNetCore;
using MatPlotLibNet.Blazor.Tests.Infrastructure;
using MatPlotLibNet.Models;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Blazor.Tests;

/// <summary>One real hub and ONE real client connection for the whole class (a connection costs time to open and close), the
/// way a Blazor page reaches a chart: the server publishes the figure, the client receives what the hub sends.</summary>
public sealed class ComposedChartClientFixture : IAsyncLifetime
{
    private readonly StreamingHostFixture _host = new();
    private readonly ChartSubscriptionClient _client = new();
    private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> _json = new();
    private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> _svg = new();

    public async ValueTask InitializeAsync()
    {
        _client.OnChartUpdated((id, json) => { Waiting(_json, id).TrySetResult(json); return Task.CompletedTask; });
        _client.OnSvgUpdated((id, svg) => { Waiting(_svg, id).TrySetResult(svg); return Task.CompletedTask; });
        await _client.ConnectAsync(_host.HubUrl);
    }

    private static TaskCompletionSource<string> Waiting(ConcurrentDictionary<string, TaskCompletionSource<string>> pending, string chartId) =>
        pending.GetOrAdd(chartId, _ => new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously));

    public async Task<string> JsonReceivedAsync(string chartId, Figure figure, CancellationToken ct)
    {
        var received = Waiting(_json, chartId);
        await _client.SubscribeAsync(chartId, ct);
        await _host.Publisher.PublishAsync(chartId, figure, ct);
        return await received.Task.WaitAsync(TimeSpan.FromSeconds(10), ct);
    }

    public async Task<string> SvgReceivedAsync(string chartId, Figure figure, CancellationToken ct)
    {
        var received = Waiting(_svg, chartId);
        await _client.SubscribeAsync(chartId, ct);
        await _host.Publisher.PublishSvgAsync(chartId, figure, ct);
        return await received.Task.WaitAsync(TimeSpan.FromSeconds(10), ct);
    }

    public async ValueTask DisposeAsync()
    {
        await _client.DisposeAsync();
        await _host.DisposeAsync();
    }
}

