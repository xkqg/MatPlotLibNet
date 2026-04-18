// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Bunit;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Blazor.Tests;

/// <summary>Phase J.1 of the v1.7.2 follow-on plan — behavioural coverage for
/// <see cref="MplLiveChart"/> SignalR subscription lifecycle. Pre-J.1 only
/// static render tests of <see cref="MplChart"/> existed; the live-chart
/// subscription / dispose path had zero coverage.
///
/// <para>Uses <see cref="FakeSubscriptionClient"/> injected via the
/// <c>Client</c> parameter so tests can drive the callback path and observe
/// re-render without a running SignalR server.</para></summary>
public class MplLiveChartLifecycleTests : BunitContext
{
    [Fact]
    public void OnAfterRender_CallsConnectAndSubscribe()
    {
        var fake = new FakeSubscriptionClient();
        var cut = Render<MplLiveChart>(parameters => parameters
            .Add(p => p.ChartId, "chart-1")
            .Add(p => p.HubUrl, "/hub")
            .Add(p => p.Client, fake));

        Assert.Equal("/hub", fake.ConnectCalledWithHubUrl);
        Assert.Equal("chart-1", fake.SubscribedChartId);
    }

    [Fact]
    public void ReceiveSvg_ForMatchingChartId_UpdatesDom()
    {
        var fake = new FakeSubscriptionClient();
        var cut = Render<MplLiveChart>(parameters => parameters
            .Add(p => p.ChartId, "chart-1")
            .Add(p => p.Client, fake));

        // Simulate server push.
        var svg = "<svg data-test=\"updated\"><text>hello</text></svg>";
        fake.FireSvgUpdate("chart-1", svg);
        cut.Render();  // flush

        Assert.Contains("data-test=\"updated\"", cut.Markup);
    }

    [Fact]
    public void ReceiveSvg_ForDifferentChartId_IsIgnored()
    {
        var fake = new FakeSubscriptionClient();
        var cut = Render<MplLiveChart>(parameters => parameters
            .Add(p => p.ChartId, "chart-1")
            .Add(p => p.Client, fake));

        fake.FireSvgUpdate("chart-2", "<svg data-unrelated=\"true\" />");
        cut.Render();

        Assert.DoesNotContain("data-unrelated", cut.Markup);
    }

    [Fact]
    public void InitialFigure_RendersBeforeSubscription()
    {
        var fake = new FakeSubscriptionClient();
        var figure = Plt.Create().Plot([1.0, 2.0], [3.0, 4.0]).Build();

        var cut = Render<MplLiveChart>(parameters => parameters
            .Add(p => p.ChartId, "chart-1")
            .Add(p => p.InitialFigure, figure)
            .Add(p => p.Client, fake));

        // Markup should contain SVG from InitialFigure even before any push.
        Assert.Contains("<svg", cut.Markup);
    }

    [Fact]
    public async Task DisposeAsync_DisposesClient()
    {
        var fake = new FakeSubscriptionClient();
        var cut = Render<MplLiveChart>(parameters => parameters
            .Add(p => p.ChartId, "chart-1")
            .Add(p => p.Client, fake));

        await ((IAsyncDisposable)cut.Instance).DisposeAsync();
        Assert.True(fake.Disposed);
    }

    /// <summary>Minimal in-process fake of <see cref="IChartSubscriptionClient"/> used
    /// by the bUnit tests above. Records what the component calls on it + exposes
    /// <see cref="FireSvgUpdate"/> so tests can simulate server pushes.</summary>
    private sealed class FakeSubscriptionClient : IChartSubscriptionClient
    {
        private Func<string, string, Task>? _onSvg;
        private Func<string, string, Task>? _onJson;

        public string? ConnectCalledWithHubUrl { get; private set; }
        public string? SubscribedChartId { get; private set; }
        public bool Disposed { get; private set; }
        public bool IsConnected { get; private set; }

        public Task ConnectAsync(string hubUrl, CancellationToken ct = default)
        {
            ConnectCalledWithHubUrl = hubUrl;
            IsConnected = true;
            return Task.CompletedTask;
        }
        public Task SubscribeAsync(string chartId, CancellationToken ct = default)
        {
            SubscribedChartId = chartId;
            return Task.CompletedTask;
        }
        public Task UnsubscribeAsync(string chartId, CancellationToken ct = default) => Task.CompletedTask;
        public void OnSvgUpdated(Func<string, string, Task> handler) => _onSvg = handler;
        public void OnChartUpdated(Func<string, string, Task> handler) => _onJson = handler;
        public void FireSvgUpdate(string chartId, string svg) => _onSvg?.Invoke(chartId, svg);
        public void FireJsonUpdate(string chartId, string json) => _onJson?.Invoke(chartId, json);
        public ValueTask DisposeAsync() { Disposed = true; return ValueTask.CompletedTask; }
    }
}
