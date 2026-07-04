// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Blazor.Tests.Infrastructure;
using MatPlotLibNet.Models;
using Microsoft.AspNetCore.SignalR.Client;

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
        await client.ConnectAsync(_fixture.HubUrl, TestContext.Current.CancellationToken);
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
        await client.ConnectAsync(_fixture.HubUrl, TestContext.Current.CancellationToken);
        await client.SubscribeAsync("chart-x11c", TestContext.Current.CancellationToken);

        var fig = Plt.Create().WithTitle("Live X.11.c").Plot([1.0], [2.0]).Build();
        await _fixture.Publisher.PublishSvgAsync("chart-x11c", fig, TestContext.Current.CancellationToken);

        var received = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
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
        await client.ConnectAsync(_fixture.HubUrl, TestContext.Current.CancellationToken);
        await client.SubscribeAsync("chart-x11c-unsub", TestContext.Current.CancellationToken);
        await client.UnsubscribeAsync("chart-x11c-unsub", TestContext.Current.CancellationToken);

        var fig = Plt.Create().Plot([1.0], [2.0]).Build();
        await _fixture.Publisher.PublishSvgAsync("chart-x11c-unsub", fig, TestContext.Current.CancellationToken);
        await Task.Delay(300, TestContext.Current.CancellationToken);

        Assert.False(fired);
    }

    /// <summary>DisposeAsync after a real connect runs the line 64 true arm + the
    /// inner try/catch (line 66-67). Forward-regression guard for the cleanup contract.</summary>
    [Fact]
    public async Task DisposeAsync_AfterConnect_ClosesConnectionCleanly()
    {
        var client = new ChartSubscriptionClient();
        await client.ConnectAsync(_fixture.HubUrl, TestContext.Current.CancellationToken);
        Assert.True(client.IsConnected);
        await client.DisposeAsync();
    }

    /// <summary>BUG B1(a) repro (council-main-a5fe781-20260704.stap3.md, blocking #1) —
    /// a second ConnectAsync call must dispose the prior HubConnection before building
    /// a new one. Pre-fix, the prior hub was silently overwritten and orphaned — kept
    /// alive forever by WithAutomaticReconnect (zombie reconnect loop). Uses the
    /// internal hub-factory seam (drives the real ConnectAsync flow against the real
    /// hub fixture; only the returned HubConnection references are captured for
    /// post-hoc inspection, production logic is untouched).</summary>
    [Fact]
    public async Task ConnectAsync_CalledTwice_DisposesPriorConnection()
    {
        var builtHubs = new List<HubConnection>();
        await using var client = new ChartSubscriptionClient(
            hubUrl =>
            {
                var hub = ChartSubscriptionClient.BuildHubConnection(hubUrl);
                builtHubs.Add(hub);
                return hub;
            },
            ChartSubscriptionClient.RegisterHandler);

        await client.ConnectAsync(_fixture.HubUrl, TestContext.Current.CancellationToken);
        await client.ConnectAsync(_fixture.HubUrl, TestContext.Current.CancellationToken);

        Assert.Equal(2, builtHubs.Count);
        Assert.Equal(HubConnectionState.Disconnected, builtHubs[0].State);
        Assert.Equal(HubConnectionState.Connected, builtHubs[1].State);
    }

    /// <summary>BUG B1(b) repro — the IDisposable tokens returned by hub.On(...) must be
    /// disposed (unregistered) rather than discarded. Pre-fix, ConnectAsync's two
    /// `_hub.On&lt;string,string&gt;(...)` return values were never captured, so a
    /// handler could never be unregistered short of disposing the whole hub. Uses the
    /// internal handler-registration seam to wrap (not replace) the real token so the
    /// real ConnectAsync/DisposeAsync flow still runs unchanged.</summary>
    [Fact]
    public async Task DisposeAsync_DisposesSubscriptionTokens()
    {
        var disposedCount = 0;
        var client = new ChartSubscriptionClient(
            ChartSubscriptionClient.BuildHubConnection,
            (hub, methodName, handler) =>
            {
                var real = ChartSubscriptionClient.RegisterHandler(hub, methodName, handler);
                return new SpyDisposable(real, () => disposedCount++);
            });

        await client.ConnectAsync(_fixture.HubUrl, TestContext.Current.CancellationToken);
        await client.DisposeAsync();

        Assert.Equal(2, disposedCount);
    }

    /// <summary>Wraps a real IDisposable token so tests can observe Dispose() calls
    /// without altering the underlying (real) unregister behavior.</summary>
    private sealed class SpyDisposable(IDisposable inner, Action onDisposed) : IDisposable
    {
        public void Dispose()
        {
            inner.Dispose();
            onDisposed();
        }
    }
}
