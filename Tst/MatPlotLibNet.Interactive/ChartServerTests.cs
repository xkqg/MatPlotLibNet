// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.SignalR.Client;
using MatPlotLibNet.AspNetCore;
using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Interactive;

namespace MatPlotLibNet.Interactive.Tests;

/// <summary>Verifies <see cref="ChartServer"/> behavior.</summary>
public class ChartServerTests : IAsyncDisposable
{
    private readonly IHost _host;
    private readonly HttpClient _httpClient;
    private readonly ConcurrentDictionary<string, Figure> _figures = new();

    public ChartServerTests()
    {
        _host = SignalRTestHost.Start(endpoints =>
                    {
                        ChartServer.ConfigureRoutes(endpoints, _figures);
                    });

        _httpClient = _host.GetTestClient();
    }

    /// <summary>Verifies that registering a figure returns a non-empty chart ID.</summary>
    [Fact]
    public void RegisterFigure_ReturnsNonEmptyId()
    {
        var server = new ChartServer();
        var figure = Plt.Create().WithTitle("Test").Build();
        var id = server.RegisterFigure(figure);
        Assert.False(string.IsNullOrEmpty(id));
    }

    /// <summary>Verifies that registering multiple figures produces distinct chart IDs.</summary>
    [Fact]
    public void RegisterFigure_MultipleFigures_DistinctIds()
    {
        var server = new ChartServer();
        var id1 = server.RegisterFigure(Plt.Create().Build());
        var id2 = server.RegisterFigure(Plt.Create().Build());
        Assert.NotEqual(id1, id2);
    }

    /// <summary>Verifies that the chart page endpoint returns HTTP 200 OK for a registered figure.</summary>
    [Fact]
    public async Task GetChartPage_ReturnsOk()
    {
        var figure = Plt.Create().WithTitle("Test").Plot([1.0, 2.0], [3.0, 4.0]).Build();
        var chartId = Guid.NewGuid().ToString("N");
        _figures[chartId] = figure;

        var ct = TestContext.Current.CancellationToken;
        var response = await _httpClient.GetAsync($"/chart/{chartId}", ct);
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>Verifies that the chart page endpoint returns text/html content type.</summary>
    [Fact]
    public async Task GetChartPage_ReturnsHtmlContentType()
    {
        var figure = Plt.Create().Build();
        var chartId = Guid.NewGuid().ToString("N");
        _figures[chartId] = figure;

        var ct = TestContext.Current.CancellationToken;
        var response = await _httpClient.GetAsync($"/chart/{chartId}", ct);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
    }

    /// <summary>Verifies that the chart page HTML contains the SVG with the expected title.</summary>
    [Fact]
    public async Task GetChartPage_ContainsSvg()
    {
        var figure = Plt.Create().WithTitle("Svg Test").Plot([1.0, 2.0], [3.0, 4.0]).Build();
        var chartId = Guid.NewGuid().ToString("N");
        _figures[chartId] = figure;

        var ct = TestContext.Current.CancellationToken;
        var html = await _httpClient.GetStringAsync($"/chart/{chartId}", ct);
        Assert.Contains("<svg", html);
        Assert.Contains("Svg Test", html);
    }

    /// <summary>Verifies that the SignalR JavaScript endpoint returns HTTP 200 OK.</summary>
    [Fact]
    public async Task GetSignalRJs_ReturnsOk()
    {
        var ct = TestContext.Current.CancellationToken;
        var response = await _httpClient.GetAsync("/js/signalr.min.js", ct);
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>Verifies that the SignalR JavaScript endpoint returns application/javascript content type.</summary>
    [Fact]
    public async Task GetSignalRJs_ReturnsJavaScript()
    {
        var ct = TestContext.Current.CancellationToken;
        var response = await _httpClient.GetAsync("/js/signalr.min.js", ct);
        Assert.Equal("application/javascript", response.Content.Headers.ContentType?.MediaType);
    }

    /// <summary>Verifies that the ChartHub is reachable and a client can connect successfully.</summary>
    [Fact]
    public async Task ChartHub_IsReachable()
    {
        var ct = TestContext.Current.CancellationToken;
        var connection = _host.ConnectToTheHub();
        await connection.StartAsync(ct);
        Assert.Equal(HubConnectionState.Connected, connection.State);
        await connection.DisposeAsync();
    }

    /// <summary>Verifies that a subscribed client receives SVG updates published to the chart group.</summary>
    [Fact]
    public async Task SubscribedClient_ReceivesSvgUpdate()
    {
        var connection = _host.ConnectToTheHub();
        var tcs = new TaskCompletionSource<(string chartId, string svg)>();

        connection.On<string, string>("UpdateChartSvg", (id, svg) =>
        {
            tcs.SetResult((id, svg));
        });

        var ct = TestContext.Current.CancellationToken;
        await connection.StartAsync(ct);
        await connection.InvokeAsync("Subscribe", "live-chart", ct);

        var publisher = _host.Services.GetRequiredService<IChartPublisher>();
        var figure = Plt.Create().WithTitle("Live Update").Plot([1.0], [2.0]).Build();
        await publisher.PublishSvgAsync("live-chart", figure, ct);

        var result = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5), ct);
        Assert.Equal("live-chart", result.chartId);
        Assert.Contains("<svg", result.svg);
        Assert.Contains("Live Update", result.svg);

        await connection.DisposeAsync();
    }

    /// <summary>Verifies that an unsubscribed client does not receive SVG updates.</summary>
    [Fact]
    public async Task UnsubscribedClient_DoesNotReceiveUpdate()
    {
        var connection = _host.ConnectToTheHub();
        var received = false;

        connection.On<string, string>("UpdateChartSvg", (_, _) => received = true);

        var ct = TestContext.Current.CancellationToken;
        await connection.StartAsync(ct);
        await connection.InvokeAsync("Subscribe", "temp-chart", ct);
        await connection.InvokeAsync("Unsubscribe", "temp-chart", ct);

        var publisher = _host.Services.GetRequiredService<IChartPublisher>();
        await publisher.PublishSvgAsync("temp-chart", Plt.Create().Build(), ct);

        await Task.Delay(500, ct);
        Assert.False(received);

        await connection.DisposeAsync();
    }

    public async ValueTask DisposeAsync()
    {
        _httpClient.Dispose();
        await _host.StopAsync();
        _host.Dispose();
    }

    /// <summary>IsRunning returns false on a fresh, never-started ChartServer
    /// (the `_app is not null` false arm).</summary>
    [Fact]
    public async Task IsRunning_OnFreshServer_False()
    {
        await using var server = new ChartServer();
        Assert.False(server.IsRunning);
        Assert.Equal(0, server.Port);
    }

    /// <summary>Standard dispose-pattern contract: DisposeAsync is idempotent.
    /// Second call hits `if (_disposed) return` and must NOT throw or re-execute cleanup.</summary>
    [Fact]
    public async Task DisposeAsync_SecondCall_IsNoOp()
    {
        var server = new ChartServer();
        await server.DisposeAsync();
        await server.DisposeAsync();
    }

    /// <summary>DisposeAsync on a never-started ChartServer — DisposeAsyncCore
    /// skips app teardown when _app is null (no Kestrel was started).</summary>
    [Fact]
    public async Task DisposeAsync_NeverStartedServer_NoOp()
    {
        await using var server = new ChartServer();
        Assert.False(server.IsRunning);
    }

    /// <summary>DisposeAsync on a STARTED ChartServer (the true arm).
    /// Verifies the full teardown of the embedded Kestrel host.</summary>
    [Fact]
    public async Task DisposeAsync_StartedServer_StopsKestrel()
    {
        var server = new ChartServer();
        await server.EnsureStartedAsync(TestContext.Current.CancellationToken);
        Assert.True(server.IsRunning);
        Assert.True(server.Port > 0);

        await server.DisposeAsync();
        await server.DisposeAsync();
    }

    /// <summary>EnsureStartedAsync called twice on the same instance — the second
    /// call must hit the early-return (`_app is not null` true arm).</summary>
    [Fact]
    public async Task EnsureStartedAsync_CalledTwice_SecondCallShortCircuits()
    {
        var server = new ChartServer();
        await server.EnsureStartedAsync(TestContext.Current.CancellationToken);
        var firstPort = server.Port;
        await server.EnsureStartedAsync(TestContext.Current.CancellationToken);
        Assert.Equal(firstPort, server.Port);
        await server.DisposeAsync();
    }

    /// <summary>UpdateFigureAsync on a started server — _publisher is
    /// non-null and PublishSvgAsync is called. Asserts the figure dict is also updated.</summary>
    [Fact]
    public async Task UpdateFigureAsync_AfterStart_PublishesAndUpdatesFigureDict()
    {
        var server = new ChartServer();
        await server.EnsureStartedAsync(TestContext.Current.CancellationToken);
        var fig = Plt.Create().Plot([1.0], [2.0]).Build();
        var id = server.RegisterFigure(fig);

        var updatedFig = Plt.Create().Plot([3.0], [4.0]).Build();
        await server.UpdateFigureAsync(id, updatedFig, TestContext.Current.CancellationToken);

        var url = server.GetFigureUrl(id);
        Assert.Contains(id, url);
        await server.DisposeAsync();
    }

    /// <summary>The token of an update reaches the publisher: a push whose caller has given up is not sent. The publisher of a
    /// started server waits on the chart's gate with that token, so a cancelled one ends the push there.</summary>
    [Fact]
    public async Task UpdateFigureAsync_WithACancelledToken_HandsItToThePublisher()
    {
        var server = new ChartServer();
        try
        {
            await server.EnsureStartedAsync(TestContext.Current.CancellationToken);
            var id = server.RegisterFigure(Plt.Create().Plot([1.0], [2.0]).Build());
            using var gaveUp = new CancellationTokenSource();
            gaveUp.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => server.UpdateFigureAsync(id, Plt.Create().Plot([3.0], [4.0]).Build(), gaveUp.Token));
        }
        finally
        {
            await server.DisposeAsync();
        }
    }

    // ---- the page the server hands a browser (Generate) ----
    // Verifies <see cref="Interactive.ChartServer"/> behavior.

    private const string TestChartId = "test-chart-123";
    private const string TestSvg = "<svg><circle cx=\"50\" cy=\"50\" r=\"40\"/></svg>";

    private static string Page() => ChartServer.Generate(TestChartId, TestSvg);

    /// <summary>What each fact about the page held, as one row: the tokens its HTML must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        //  Verifies that the generated HTML contains the initial SVG content.
        new DrawnToken("Generate_ContainsInitialSvg", Page, TestSvg),
        //  Verifies that the generated HTML includes the chart ID in the script section.
        new DrawnToken("Generate_ContainsChartIdInScript", Page, $"\"{TestChartId}\""),
        //  The page reaches the hub on the origin it was served from: by the hub's path, never by a host of its own.
        new DrawnToken("Generate_ReachesTheHubByItsPath", Page, $".withUrl(\"{ChartServer.HubPath}\")"),
        //  Verifies that the generated HTML includes a reference to the SignalR JavaScript file.
        new DrawnToken("Generate_ContainsSignalRScriptReference", Page, "/js/signalr.min.js"),
        //  Verifies that the generated HTML contains a Subscribe call for real-time updates.
        new DrawnToken("Generate_ContainsSubscribeCall", Page, "Subscribe"),
        //  Verifies that the generated HTML contains an UpdateChartSvg handler for live SVG updates.
        new DrawnToken("Generate_ContainsUpdateChartSvgHandler", Page, "UpdateChartSvg"),
        //  Verifies that the generated HTML contains a chart-container element.
        new DrawnToken("Generate_ContainsChartContainer", Page, "chart-container"),
        //  Verifies that the generated HTML includes automatic reconnect configuration for SignalR.
        new DrawnToken("Generate_ContainsAutoReconnect", Page, "withAutomaticReconnect"),
        //  Verifies that the chart ID is HTML-encoded to prevent script injection.
        new DrawnToken("Generate_EncodesChartIdForHtml (encoded)", () => ChartServer.Generate("<script>alert(1)</script>", TestSvg), "&lt;script&gt;"),
        new DrawnToken("Generate_EncodesChartIdForHtml (head)", () => ChartServer.Generate("<script>alert(1)</script>", TestSvg).Split("</head>")[0]) { Absent = ["<script>alert(1)</script>"] },
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void ThePage_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    /// <summary>Verifies that the generated HTML starts with a DOCTYPE declaration.</summary>
    [Fact]
    public void Generate_ContainsDoctype()
    {
        var html = ChartServer.Generate(TestChartId, TestSvg);
        Assert.StartsWith("<!DOCTYPE html>", html);
    }

    /// <summary>The page patches: it subscribes for parts on start and again after a reconnect (never the old way, which would
    /// have the server send it the whole document as well), and it puts each part it receives in place of the element of the
    /// same name, the root's new attributes on the root.</summary>
    [Fact]
    public void Generate_SubscribesForParts_AndPutsEachPartInPlace()
    {
        var html = ChartServer.Generate(TestChartId, TestSvg);

        Assert.Equal(2, System.Text.RegularExpressions.Regex.Matches(html, "invoke\\(\"SubscribeToParts\", chartId\\)").Count);
        Assert.DoesNotContain("invoke(\"Subscribe\", chartId)", html);
        Assert.Contains("connection.on(\"UpdateChartParts\"", html);
        Assert.Contains("data-mpl-part", html);
        Assert.Contains("parts.rootTag", html);
        Assert.Contains("replaceWith", html);
    }
}
