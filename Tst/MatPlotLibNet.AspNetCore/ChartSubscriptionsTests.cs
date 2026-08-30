// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.AspNetCore.Tests;

/// <summary>A publisher that renders to a group nobody has joined does the work for no one. SignalR groups
/// carry no membership count, so the hub keeps its own: which connections subscribed to which chart — the
/// one question a render lane asks before it spends a frame ("is anyone looking?").</summary>
public class ChartSubscriptionsTests
{
    [Fact]
    public void AChartNobodyJoined_HasNoSubscribers()
    {
        var subs = new ChartSubscriptions();

        Assert.False(subs.HasSubscribers("ops-processes"));
        Assert.Equal(0, subs.Count("ops-processes"));
    }

    [Fact]
    public void Subscribe_ThenUnsubscribe_RoundTrips()
    {
        var subs = new ChartSubscriptions();

        subs.Subscribe("ops-processes", "conn-1");
        Assert.True(subs.HasSubscribers("ops-processes"));
        Assert.Equal(1, subs.Count("ops-processes"));

        subs.Unsubscribe("ops-processes", "conn-1");
        Assert.False(subs.HasSubscribers("ops-processes"));
    }

    [Fact]
    public void TheSameConnectionSubscribingTwice_CountsOnce()
    {
        var subs = new ChartSubscriptions();

        subs.Subscribe("a", "conn-1");
        subs.Subscribe("a", "conn-1");

        Assert.Equal(1, subs.Count("a"));
    }

    /// <summary>A browser that closes never calls Unsubscribe; the hub's disconnect sweeps the connection out
    /// of every chart it joined, or a lane keeps rendering for a tab that is gone.</summary>
    [Fact]
    public void ADisconnectedConnection_LeavesEveryChart()
    {
        var subs = new ChartSubscriptions();
        subs.Subscribe("a", "conn-1");
        subs.Subscribe("b", "conn-1");
        subs.Subscribe("a", "conn-2");

        subs.Disconnected("conn-1");

        Assert.Equal(1, subs.Count("a"));
        Assert.False(subs.HasSubscribers("b"));
    }

    [Fact]
    public void UnsubscribingAnUnknownPair_IsHarmless()
    {
        var subs = new ChartSubscriptions();

        subs.Unsubscribe("never", "conn-9");
        subs.Disconnected("conn-9");

        Assert.False(subs.HasSubscribers("never"));
    }
}
