// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace MatPlotLibNet.AspNetCore.Tests;

/// <summary>The in-memory host and the hub connection every server test starts from.</summary>
public sealed class SignalRTestHostTests
{
    [Fact]
    public async Task TheDefaultHost_ServesTheChartHub_ToAConnectionOfItsOwn()
    {
        using var host = SignalRTestHost.Start();
        await using var connection = host.ConnectToTheHub();

        await connection.StartAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HubConnectionState.Connected, connection.State);
        Assert.NotNull(host.Services.GetService<FigureRegistry>());
    }

    [Fact]
    public async Task AHostWithEndpointsOfItsOwn_ServesThose_AndNotTheHubUnlessItMapsIt()
    {
        using var host = SignalRTestHost.Start(map => map.MapGet("/ping", () => "pong"));

        using var client = host.GetTestClient();

        Assert.Equal("pong", await client.GetStringAsync("/ping", TestContext.Current.CancellationToken));
        await using var connection = host.ConnectToTheHub();
        await Assert.ThrowsAnyAsync<Exception>(() => connection.StartAsync(TestContext.Current.CancellationToken));
    }
}
