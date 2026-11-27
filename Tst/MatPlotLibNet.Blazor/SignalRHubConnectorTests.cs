// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.SignalR.Client;

namespace MatPlotLibNet.Blazor.Tests;

/// <summary>The two calls a <see cref="ChartSubscriptionClient"/> makes to SignalR when it connects, behind one seam
/// (<see cref="IHubConnector"/>): build a connection to a hub, and listen to one of the hub's methods. SignalR's connector builds a
/// connection that reconnects by itself and is not started yet; listening hands back the token that stops it.</summary>
public class SignalRHubConnectorTests
{
    [Fact]
    public async Task Connect_BuildsAConnectionThatIsNotStartedYet()
    {
        await using var hub = new SignalRHubConnector().Connect("http://localhost/chart-hub");

        Assert.Equal(HubConnectionState.Disconnected, hub.State);
    }

    [Fact]
    public async Task Listen_HandsBackTheTokenThatStopsTheListening()
    {
        var connector = new SignalRHubConnector();
        await using var hub = connector.Connect("http://localhost/chart-hub");

        using var token = connector.Listen(hub, "UpdateChartSvg", (_, _) => Task.CompletedTask);

        Assert.NotNull(token);
    }
}
