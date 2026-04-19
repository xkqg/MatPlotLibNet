// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;

namespace MatPlotLibNet.Blazor.Tests.Infrastructure;

/// <summary>Phase X.11.c (v1.7.2, 2026-04-19) — xunit IClassFixture that spins up a
/// real Kestrel server on a random localhost port with the SignalR ChartHub mapped.
/// The fixture exposes <see cref="HubUrl"/> so tests that exercise
/// <see cref="ChartSubscriptionClient.ConnectAsync"/> can connect through the actual
/// SignalR HTTP transport — covering the full ConnectAsync body + the On&lt;...&gt;
/// closures + StartAsync code path without mocks.
///
/// Why real Kestrel (not TestServer): <see cref="ChartSubscriptionClient.ConnectAsync"/>
/// builds its own <see cref="HubConnection"/> internally with no transport-injection
/// hook, so the only way to drive it through real code is a real listening socket.
/// Random port (`http://127.0.0.1:0`) avoids fixed-port conflicts in parallel CI runs.
/// Created once per test class (xunit fixture lifetime).</summary>
public sealed class StreamingHostFixture : IAsyncDisposable
{
    private readonly WebApplication _app;

    public string HubUrl { get; }

    /// <summary>The hub-side <see cref="IChartPublisher"/> resolved from DI. Tests can
    /// publish frames that flow back through SignalR to the connected client.</summary>
    public IChartPublisher Publisher { get; }

    public StreamingHostFixture()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.Services.AddMatPlotLibNetSignalR();
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        _app = builder.Build();
        _app.MapChartHub();
        _app.StartAsync().GetAwaiter().GetResult();

        var addresses = _app.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!;
        var baseUrl = addresses.Addresses.First();
        HubUrl = $"{baseUrl}/charts-hub";

        Publisher = _app.Services.GetRequiredService<IChartPublisher>();
    }

    public async ValueTask DisposeAsync()
    {
        await _app.StopAsync();
        await _app.DisposeAsync();
    }
}
