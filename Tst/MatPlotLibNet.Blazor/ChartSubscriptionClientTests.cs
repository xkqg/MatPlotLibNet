// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Blazor.Tests;

/// <summary>Phase X.11.b (v1.7.2, 2026-04-19) — drives the no-hub paths of
/// <see cref="ChartSubscriptionClient"/> from 0%L to ~70%L without spinning up a
/// real SignalR hub. Real-hub coverage (ConnectAsync body + the On&lt;...&gt; closures
/// + StartAsync) requires a fixture pulling in MatPlotLibNet.AspNetCore — that's
/// covered separately in X.11.c via the AspNetCore.Tests project's existing harness.
///
/// This file pins:
///   - IsConnected with a never-connected client → false (_hub null)
///   - SubscribeAsync without ConnectAsync → no-op
///   - UnsubscribeAsync without ConnectAsync → no-op
///   - OnSvgUpdated / OnChartUpdated handler setters
///   - DisposeAsync without prior connect → no-op</summary>
public class ChartSubscriptionClientTests
{
    [Fact]
    public void IsConnected_NeverConnected_ReturnsFalse()
    {
        var client = new ChartSubscriptionClient();
        Assert.False(client.IsConnected);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SubscribeAndUnsubscribeAsync_WithoutConnect_AreNoOps(bool subscribe)
    {
        var client = new ChartSubscriptionClient();
        await (subscribe ? client.SubscribeAsync("c1", TestContext.Current.CancellationToken) : client.UnsubscribeAsync("c1", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DisposeAsync_WithoutConnect_NoOp()
    {
        var client = new ChartSubscriptionClient();
        await client.DisposeAsync();
    }

    /// <summary>The client holds two handlers of the same shape, one for the picture a chart is redrawn as and one for its data;
    /// a test of either runs for both.</summary>
    private static void Handle(ChartSubscriptionClient client, bool picture, Func<string, string, Task> handler)
    {
        if (picture) client.OnSvgUpdated(handler);
        else client.OnChartUpdated(handler);
    }

    /// <summary>Hands the client an update the way the hub does, as a picture or as data.</summary>
    private static Task Update(ChartSubscriptionClient client, bool picture, string id, string payload) =>
        picture ? client.HandleSvgUpdatedAsync(id, payload) : client.HandleChartUpdatedAsync(id, payload);

    /// <summary>Handler-setter overwrite - setting a handler twice replaces the previous one (no event collection internally).
    /// Forward-regression guard for the contract that handlers are single-slot, not multicast.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AHandlerSetTwice_OverwritesThePrevious(bool picture)
    {
        var client = new ChartSubscriptionClient();
        int first = 0, second = 0;
        Handle(client, picture, (id, payload) => { first++; return Task.CompletedTask; });
        Handle(client, picture, (id, payload) => { second++; return Task.CompletedTask; });

        await Update(client, picture, "c1", "{}");

        Assert.Equal(0, first);
        Assert.Equal(1, second);
    }

    /// <summary>An update with no handler registered is the null arm: a no-op.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AnUpdate_WithNoHandler_IsANoOp(bool picture)
    {
        await Update(new ChartSubscriptionClient(), picture, "chart1", "{}");
    }

    /// <summary>An update with a registered handler invokes it with the chart's id and the payload.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AnUpdate_WithAHandler_InvokesItWithTheIdAndThePayload(bool picture)
    {
        var client = new ChartSubscriptionClient();
        string? receivedId = null;
        string? receivedPayload = null;
        Handle(client, picture, (id, payload) => { receivedId = id; receivedPayload = payload; return Task.CompletedTask; });

        await Update(client, picture, "c2", "{\"x\":1}");

        Assert.Equal("c2", receivedId);
        Assert.Equal("{\"x\":1}", receivedPayload);
    }
}
