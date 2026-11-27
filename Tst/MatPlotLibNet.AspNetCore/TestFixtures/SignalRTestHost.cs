// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MatPlotLibNet.Tests;

/// <summary>The in-memory web host the server tests run against: the SignalR services of the library, routing, and the endpoints a test
/// names (the chart hub when it names none).</summary>
public static class SignalRTestHost
{
    /// <summary>Starts the host.</summary>
    /// <param name="endpoints">Maps the endpoints of the host; <see langword="null"/> maps the chart hub only.</param>
    /// <returns>The running host; the test disposes it.</returns>
    public static IHost Start(Action<IEndpointRouteBuilder>? endpoints = null) => new HostBuilder()
        .ConfigureWebHost(web =>
        {
            web.UseTestServer();
            web.ConfigureServices(services => services.AddMatPlotLibNetSignalR().AddRouting());
            web.Configure(app => app.UseRouting().UseEndpoints(map =>
            {
                if (endpoints is null)
                {
                    map.MapChartHub();
                }
                else
                {
                    endpoints(map);
                }
            }));
        })
        .Start();
}
