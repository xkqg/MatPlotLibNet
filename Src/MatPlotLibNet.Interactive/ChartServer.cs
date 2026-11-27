// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

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
using MatPlotLibNet.Interaction;
using MatPlotLibNet.Rendering.Svg;
using System.Net;

namespace MatPlotLibNet.Interactive;

/// <summary>Embedded Kestrel web server that serves interactive charts in the browser.</summary>
/// <remarks>A server is started or disposed, never both: a start and a dispose take the same lock, so a dispose that comes
/// while a start is under way waits for it and then stops what it started, and a start that comes after a dispose is
/// refused.</remarks>
public sealed class ChartServer : IAsyncDisposable
{
    private static readonly Lazy<ChartServer> _instance = new(() => new ChartServer());

    /// <summary>Where the server listens and is reached: the loopback by its number, so the address a browser is sent to is the one
    /// the server listens on, and it cannot resolve to another.</summary>
    private const string Loopback = "http://127.0.0.1";

    /// <summary>The path of the chart hub on the server; a page reaches it on the origin it was served from.</summary>
    internal const string HubPath = "/charts-hub";

    /// <summary>Gets the process-wide singleton instance.</summary>
    public static ChartServer Instance => _instance.Value;

    private volatile WebApplication? _app;
    private readonly SemaphoreSlim _startLock = new(1, 1);
    private readonly ConcurrentDictionary<string, Figure> _figures = new();
    private volatile IChartPublisher? _publisher;
    private volatile bool _disposed;
    private volatile int _port;

    /// <summary>Gets the port the server is listening on, or 0 if not started.</summary>
    public int Port => _port;

    /// <summary>Gets whether the server is currently running.</summary>
    public bool IsRunning => _app is not null;

    internal ChartServer() { }

    /// <summary>Ensures the embedded server is started asynchronously. Thread-safe.</summary>
    /// <remarks>The internal await chain uses <c>ConfigureAwait(false)</c> throughout, so
    /// blocking on this task from a captured <see cref="SynchronizationContext"/> does not
    /// deadlock.</remarks>
    internal async Task EnsureStartedAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_app is not null) return;

        await _startLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_app is not null) return;

            var builder = WebApplication.CreateSlimBuilder();
            builder.Services.AddMatPlotLibNetSignalR();
            builder.WebHost.UseUrls(Loopback + ":0");

            var app = builder.Build();

            ConfigureRoutes(app, _figures);

            await app.StartAsync(ct).ConfigureAwait(false);

            var addresses = app.Services.GetRequiredService<IServer>()
                .Features.Get<IServerAddressesFeature>()!;
            _port = new Uri(addresses.Addresses.First()).Port;

            _publisher = app.Services.GetRequiredService<IChartPublisher>();
            _app = app;
        }
        finally
        {
            _startLock.Release();
        }
    }

    /// <summary>Registers a figure and returns its chart ID.</summary>
    internal string RegisterFigure(Figure figure)
    {
        var chartId = Guid.NewGuid().ToString("N");
        _figures[chartId] = figure;
        return chartId;
    }

    /// <summary>Lets go of a registered figure: its page is no longer served. Called when its handle is disposed
    /// (<see cref="InteractiveFigure.Dispose"/>).</summary>
    /// <param name="chartId">The chart to let go of.</param>
    internal void ReleaseFigure(string chartId) => _figures.TryRemove(chartId, out _);

    /// <summary>Whether the server holds a figure for <paramref name="chartId"/>.</summary>
    internal bool Holds(string chartId) => _figures.ContainsKey(chartId);

    /// <summary>Updates a registered figure and pushes the change to connected browsers.</summary>
    /// <param name="chartId">The chart to update.</param>
    /// <param name="figure">Its new figure.</param>
    /// <param name="ct">Ends the push to the browsers; an animation that is stopped passes its own.</param>
    internal async Task UpdateFigureAsync(string chartId, Figure figure, CancellationToken ct = default)
    {
        _figures[chartId] = figure;
        if (_publisher is not null)
            await _publisher.PublishSvgAsync(chartId, figure, ct).ConfigureAwait(false);
    }

    /// <summary>Gets the full URL for a chart page.</summary>
    internal string GetFigureUrl(string chartId) => $"{Loopback}:{Port.ToString(System.Globalization.CultureInfo.InvariantCulture)}/chart/{chartId}";

    private static readonly Func<Stream?> _defaultSignalRLoader = () =>
        typeof(ChartServer).Assembly.GetManifestResourceStream(
            "MatPlotLibNet.Interactive.Resources.signalr.min.js");

    /// <summary>Configures routes on an endpoint builder. Extracted for testability.</summary>
    /// <param name="endpoints">The endpoint route builder to register routes on.</param>
    /// <param name="figures">The shared figure registry keyed by chart ID.</param>
    /// <param name="signalRLoader">Overrides the embedded-JS loader; pass <see langword="null"/> to use the default assembly resource.</param>
    internal static void ConfigureRoutes(IEndpointRouteBuilder endpoints, ConcurrentDictionary<string, Figure> figures, Func<Stream?>? signalRLoader = null)
    {
        var jsLoader = signalRLoader ?? _defaultSignalRLoader;

        endpoints.MapGet("/chart/{chartId}", (string chartId) =>
        {
            if (!figures.TryGetValue(chartId, out var figure))
                return Results.NotFound();

            var svg = SvgRendererDefault.Current.Render(figure);
            var html = Generate(chartId, svg);
            return Results.Content(html, "text/html");
        });

        endpoints.MapGet("/js/signalr.min.js", () =>
        {
            var stream = jsLoader();
            if (stream is null)
                return Results.NotFound();
            return Results.Stream(stream, "application/javascript");
        });

        endpoints.MapChartHub(HubPath);
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        await DisposeAsyncCore().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }

    private async ValueTask DisposeAsyncCore()
    {
        // A start under way holds the lock: waiting for it means the app it puts up is seen here and stopped.
        await _startLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_app is { } app)
            {
                _app = null;
                _publisher = null;
                await app.StopAsync().ConfigureAwait(false);
                await app.DisposeAsync().ConfigureAwait(false);
            }
        }
        finally
        {
            // Not disposed: a start that waited behind this dispose still takes and releases the lock on its way to being
            // refused, and a SemaphoreSlim whose wait handle was never asked for holds nothing a dispose would free.
            _startLock.Release();
        }
    }

    /// <summary>Generates the HTML page containing the chart SVG and SignalR client script.</summary>
    /// <param name="chartId">The chart identifier for SignalR subscription.</param>
    /// <param name="initialSvg">The initial SVG content to embed.</param>
    /// <returns>A complete HTML document string.</returns>
    internal static string Generate(string chartId, string initialSvg)
    {
        var encodedChartId = WebUtility.HtmlEncode(chartId);

        return $$"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
                <meta charset="utf-8" />
                <meta name="viewport" content="width=device-width, initial-scale=1" />
                <title>MatPlotLibNet - {{encodedChartId}}</title>
                <style>
                    * { margin: 0; padding: 0; box-sizing: border-box; }
                    body {
                        display: flex;
                        justify-content: center;
                        align-items: center;
                        min-height: 100vh;
                        background: #fafafa;
                        font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif;
                    }
                    #chart-container {
                        background: white;
                        border-radius: 8px;
                        box-shadow: 0 2px 8px rgba(0,0,0,0.1);
                        padding: 16px;
                    }
                    #chart-container svg { display: block; }
                    #status {
                        position: fixed;
                        bottom: 8px;
                        right: 8px;
                        font-size: 12px;
                        color: #999;
                    }
                </style>
            </head>
            <body>
                <div id="chart-container">{{initialSvg}}</div>
                <div id="status">Connected</div>

                <script src="/js/signalr.min.js"></script>
                <script>
                    const chartId = "{{encodedChartId}}";
                    const container = document.getElementById("chart-container");
                    const status = document.getElementById("status");

                    const connection = new signalR.HubConnectionBuilder()
                        .withUrl("{{ChartServer.HubPath}}")
                        .withAutomaticReconnect()
                        .build();

                    connection.on("{{HubMethodNames.UpdateChartSvg}}", (id, svg) => {
                        if (id === chartId) {
                            container.innerHTML = svg;
                        }
                    });

                    // Only the parts that changed: each is the whole <g data-mpl-part> element to put in place of the one of
                    // the same name, and a new root tag brings the root's new attributes (a zoom moves the view it carries).
                    function parseInSvg(root, markup) {
                        return new DOMParser().parseFromString('<svg xmlns="' + root.namespaceURI + '">' + markup + '</svg>', "image/svg+xml").documentElement;
                    }
                    function applyParts(parts) {
                        const root = container.querySelector("svg");
                        if (!root) return;
                        if (parts.rootTag) {
                            const fresh = new DOMParser().parseFromString(parts.rootTag + "</svg>", "image/svg+xml").documentElement;
                            for (const a of Array.from(root.attributes))
                                if (!a.name.startsWith("xmlns") && !fresh.hasAttribute(a.name)) root.removeAttribute(a.name);
                            for (const a of Array.from(fresh.attributes))
                                if (!a.name.startsWith("xmlns")) root.setAttribute(a.name, a.value);
                        }
                        const swaps = [parts.background, parts.overlay, ...(parts.axes || []).map(a => a.markup)].filter(Boolean);
                        for (const markup of swaps) {
                            const fresh = parseInSvg(root, markup).firstElementChild;
                            if (!fresh) continue;
                            const name = fresh.getAttribute("{{SvgPartMarker.Attribute}}");
                            const old = Array.from(root.children).find(c => c.getAttribute("{{SvgPartMarker.Attribute}}") === name);
                            if (old) old.replaceWith(document.importNode(fresh, true));
                        }
                    }
                    connection.on("{{HubMethodNames.UpdateChartParts}}", (id, parts) => {
                        if (id === chartId) {
                            applyParts(parts);
                        }
                    });

                    connection.onreconnecting(() => { status.textContent = "Reconnecting..."; });
                    connection.onreconnected(() => {
                        status.textContent = "Connected";
                        connection.invoke("{{HubMethodNames.SubscribeToParts}}", chartId);
                    });
                    connection.onclose(() => { status.textContent = "Disconnected"; });

                    async function start() {
                        try {
                            await connection.start();
                            await connection.invoke("{{HubMethodNames.SubscribeToParts}}", chartId);
                            status.textContent = "Connected";
                        } catch (err) {
                            status.textContent = "Connection failed";
                            setTimeout(start, 3000);
                        }
                    }

                    start();
                </script>
            </body>
            </html>
            """;
    }
}
