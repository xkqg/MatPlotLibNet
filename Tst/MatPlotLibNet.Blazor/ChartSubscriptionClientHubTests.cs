// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Blazor.Tests.Infrastructure;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Blazor.Tests;

/// <summary>Phase X.11.c (v1.7.2, 2026-04-19) — exercises the
/// <see cref="ChartSubscriptionClient"/> ConnectAsync + Subscribe + UpdateChartSvg
/// receive flow against a real ASP.NET Core SignalR hub provided by the
/// <see cref="StreamingHostFixture"/>. Pre-X.11.c the ConnectAsync body (lines 19-39)
/// was 0%-covered because no test reached a live hub. This class:
///   - Exercises ConnectAsync's full body (HubConnectionBuilder configuration,
///     On&lt;...&gt; handler registration, StartAsync)
///   - Exercises SubscribeAsync's hub-non-null arm (line 44 true)
///   - Exercises the OnSvgUpdated callback closure (line 26-30) by publishing a
///     frame from the hub side and asserting the callback fires
///   - Exercises UnsubscribeAsync's hub-non-null arm (line 51 true)
///   - Exercises DisposeAsync's hub-non-null arm (line 64 true) including the
///     try/catch around DisposeAsync.</summary>
public class ChartSubscriptionClientHubTests : IClassFixture<StreamingHostFixture>
{
    private readonly StreamingHostFixture _fixture;

    public ChartSubscriptionClientHubTests(StreamingHostFixture fixture) => _fixture = fixture;

    /// <summary>ConnectAsync against a real hub → IsConnected returns true.
    /// Disposing closes the connection cleanly.</summary>
    [Fact]
    public async Task ConnectAsync_AgainstRealHub_IsConnected()
    {
        await using var client = new ChartSubscriptionClient();
        await client.ConnectAsync(_fixture.HubUrl);
        Assert.True(client.IsConnected);
    }

    /// <summary>Full subscribe → publish → callback flow. The OnSvgUpdated closure
    /// (line 26-30) only fires when a published "UpdateChartSvg" message arrives;
    /// this exercises the closure body for the chartId+svg arguments.</summary>
    [Fact]
    public async Task Subscribe_ThenPublishSvg_InvokesOnSvgUpdatedCallback()
    {
        await using var client = new ChartSubscriptionClient();
        var tcs = new TaskCompletionSource<(string id, string svg)>();
        client.OnSvgUpdated((id, svg) => { tcs.TrySetResult((id, svg)); return Task.CompletedTask; });
        await client.ConnectAsync(_fixture.HubUrl);
        await client.SubscribeAsync("chart-x11c");

        var fig = Plt.Create().WithTitle("Live X.11.c").Plot([1.0], [2.0]).Build();
        await _fixture.Publisher.PublishSvgAsync("chart-x11c", fig);

        var received = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal("chart-x11c", received.id);
        Assert.Contains("Live X.11.c", received.svg);
    }

    /// <summary>Subscribe + Unsubscribe round-trip — both line 44 and line 51 hub-non-null
    /// arms run. After Unsubscribe, subsequent publishes do NOT invoke the callback.</summary>
    [Fact]
    public async Task Unsubscribe_StopsCallbackFromFiring()
    {
        await using var client = new ChartSubscriptionClient();
        var fired = false;
        client.OnSvgUpdated((id, svg) => { fired = true; return Task.CompletedTask; });
        await client.ConnectAsync(_fixture.HubUrl);
        await client.SubscribeAsync("chart-x11c-unsub");
        await client.UnsubscribeAsync("chart-x11c-unsub");

        var fig = Plt.Create().Plot([1.0], [2.0]).Build();
        await _fixture.Publisher.PublishSvgAsync("chart-x11c-unsub", fig);
        await Task.Delay(300);

        Assert.False(fired);
    }

    /// <summary>DisposeAsync after a real connect runs the line 64 true arm + the
    /// inner try/catch (line 66-67). Forward-regression guard for the cleanup contract.</summary>
    [Fact]
    public async Task DisposeAsync_AfterConnect_ClosesConnectionCleanly()
    {
        var client = new ChartSubscriptionClient();
        await client.ConnectAsync(_fixture.HubUrl);
        Assert.True(client.IsConnected);
        await client.DisposeAsync();
    }
}
