// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interactive;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;

namespace MatPlotLibNet.Interactive.Tests;

/// <summary>The chart server answers 404, not a failure, where it has nothing to serve: the chart page of an unknown id, and the SignalR
/// script when the injected loader returns null (the null-loader branch of ConfigureRoutes).</summary>
public class ChartServerNotFoundTests
{
    public static TheoryData<Action<IEndpointRouteBuilder>, string> RoutesWithNothingBehindThem => new()
    {
        { endpoints => ChartServer.ConfigureRoutes(endpoints, new(), signalRLoader: () => null), "/js/signalr.min.js" },
        { endpoints => ChartServer.ConfigureRoutes(endpoints, new()), "/chart/unknown-id" },
    };

    [Theory]
    [MemberData(nameof(RoutesWithNothingBehindThem))]
    public async Task ARouteWithNothingBehindIt_Returns404(Action<IEndpointRouteBuilder> configure, string url)
    {
        using var host = SignalRTestHost.Start(configure);
        using var client = host.GetTestClient();

        var response = await client.GetAsync(url, TestContext.Current.CancellationToken);

        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
        await host.StopAsync(TestContext.Current.CancellationToken);
    }
}
