// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using MatPlotLibNet.AspNetCore;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Interactive;

/// <summary>Embedded Kestrel web server that serves interactive charts in the browser.</summary>
public sealed class ChartServer : IAsyncDisposable
{
    private static readonly Lazy<ChartServer> _instance = new(() => new ChartServer());

    /// <summary>Gets the process-wide singleton instance.</summary>
    public static ChartServer Instance => _instance.Value;

    private WebApplication? _app;
    private readonly SemaphoreSlim _startLock = new(1, 1);
    private readonly ConcurrentDictionary<string, Figure> _figures = new();
    private IChartPublisher? _publisher;
    private bool _disposed;

    /// <summary>Gets the port the server is listening on, or 0 if not started.</summary>
    public int Port { get; private set; }

    /// <summary>Gets whether the server is currently running.</summary>
    public bool IsRunning => _app is not null;

    internal ChartServer() { }

    /// <summary>Ensures the embedded server is started asynchronously. Thread-safe.</summary>
    internal async Task EnsureStartedAsync(CancellationToken ct = default)
    {
        if (_app is not null) return;

        await _startLock.WaitAsync(ct);
        try
        {
            if (_app is not null) return;

            var builder = WebApplication.CreateSlimBuilder();
            builder.Services.AddMatPlotLibNetSignalR();
            builder.WebHost.UseUrls("http://127.0.0.1:0");

            var app = builder.Build();

            ConfigureRoutes(app, _figures, () => Port);
            app.MapChartHub();

            await app.StartAsync(ct);

            var addresses = app.Services.GetRequiredService<IServer>()
                .Features.Get<IServerAddressesFeature>()!;
            Port = new Uri(addresses.Addresses.First()).Port;

            _publisher = app.Services.GetRequiredService<IChartPublisher>();
            _app = app;
        }
        finally
        {
            _startLock.Release();
        }
    }

    /// <summary>Ensures the embedded server is started. Synchronous wrapper for non-async contexts.</summary>
    internal void EnsureStarted() => EnsureStartedAsync().GetAwaiter().GetResult();

    /// <summary>Registers a figure and returns its chart ID.</summary>
    internal string RegisterFigure(Figure figure)
    {
        var chartId = Guid.NewGuid().ToString("N");
        _figures[chartId] = figure;
        return chartId;
    }

    /// <summary>Updates a registered figure and pushes the change to connected browsers.</summary>
    internal async Task UpdateFigureAsync(string chartId, Figure figure)
    {
        _figures[chartId] = figure;
        if (_publisher is not null)
            await _publisher.PublishSvgAsync(chartId, figure);
    }

    /// <summary>Gets the full URL for a chart page.</summary>
    internal string GetFigureUrl(string chartId) => $"http://127.0.0.1:{Port}/chart/{chartId}";

    /// <summary>Configures routes on an endpoint builder. Extracted for testability.</summary>
    internal static void ConfigureRoutes(IEndpointRouteBuilder endpoints, ConcurrentDictionary<string, Figure> figures, Func<int> portProvider)
    {
        endpoints.MapGet("/chart/{chartId}", (string chartId) =>
        {
            if (!figures.TryGetValue(chartId, out var figure))
                return Results.NotFound();

            var svg = ChartServices.SvgRenderer.Render(figure);
            var html = ChartPage.Generate(chartId, svg, portProvider());
            return Results.Content(html, "text/html");
        });

        endpoints.MapGet("/js/signalr.min.js", () =>
        {
            var assembly = typeof(ChartServer).Assembly;
            var stream = assembly.GetManifestResourceStream(
                "MatPlotLibNet.Interactive.Resources.signalr.min.js");
            if (stream is null)
                return Results.NotFound();
            return Results.Stream(stream, "application/javascript");
        });

        endpoints.MapChartHub();
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        _startLock.Dispose();
        if (_app is not null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }
}
