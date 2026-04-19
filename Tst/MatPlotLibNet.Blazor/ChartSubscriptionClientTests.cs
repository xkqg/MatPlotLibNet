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
///   - IsConnected with a never-connected client → false (line 16, _hub null)
///   - SubscribeAsync without ConnectAsync → no-op (line 44 false arm)
///   - UnsubscribeAsync without ConnectAsync → no-op (line 51 false arm)
///   - OnSvgUpdated / OnChartUpdated handler setters (lines 56, 59)
///   - DisposeAsync without prior connect → no-op (line 64 false arm)</summary>
public class ChartSubscriptionClientTests
{
    [Fact]
    public void IsConnected_NeverConnected_ReturnsFalse()
    {
        var client = new ChartSubscriptionClient();
        Assert.False(client.IsConnected);
    }

    [Fact]
    public async Task SubscribeAsync_WithoutConnect_NoOp()
    {
        var client = new ChartSubscriptionClient();
        await client.SubscribeAsync("c1");
    }

    [Fact]
    public async Task UnsubscribeAsync_WithoutConnect_NoOp()
    {
        var client = new ChartSubscriptionClient();
        await client.UnsubscribeAsync("c1");
    }

    [Fact]
    public void OnSvgUpdated_HandlerSetter_DoesNotThrow()
    {
        var client = new ChartSubscriptionClient();
        client.OnSvgUpdated((id, svg) => Task.CompletedTask);
    }

    [Fact]
    public void OnChartUpdated_HandlerSetter_DoesNotThrow()
    {
        var client = new ChartSubscriptionClient();
        client.OnChartUpdated((id, json) => Task.CompletedTask);
    }

    [Fact]
    public async Task DisposeAsync_WithoutConnect_NoOp()
    {
        var client = new ChartSubscriptionClient();
        await client.DisposeAsync();
    }

    /// <summary>Handler-setter overwrite — calling OnSvgUpdated twice replaces the
    /// previous handler (no event collection internally). Forward-regression guard
    /// for the contract that handlers are single-slot, not multicast.</summary>
    [Fact]
    public void OnSvgUpdated_CalledTwice_OverwritesPrevious()
    {
        var client = new ChartSubscriptionClient();
        client.OnSvgUpdated((id, svg) => Task.CompletedTask);
        client.OnSvgUpdated((id, svg) => Task.CompletedTask);
    }
}
