// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Tests.Rendering;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MatPlotLibNet.AspNetCore.Tests;

/// <summary>One host and ONE hub connection for the whole class: the chart hub, and the two chart endpoints, each building
/// the composed chart a request names. A connection costs seconds to open and close through the test server, so the shapes
/// share it; each publishes on a chart id of its own.</summary>
public sealed class ComposedChartHostFixture : IAsyncLifetime
{
    private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> _json = new();
    private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> _svg = new();
    private HubConnection _connection = null!;

    public IHost Host { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        Host = new HostBuilder()
            .ConfigureWebHost(web =>
            {
                web.UseTestServer();
                web.ConfigureServices(services =>
                {
                    services.AddMatPlotLibNetSignalR();
                    services.AddRouting();
                });
                web.Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapChartHub();
                        endpoints.MapChartEndpoint("/api/composed/json", context => ComposedFigure(context.Request.Query));
                        endpoints.MapChartSvgEndpoint("/api/composed/svg", context => ComposedFigure(context.Request.Query));
                    });
                });
            })
            .Start();

        _connection = new HubConnectionBuilder()
            .WithUrl("http://localhost/charts-hub", options => options.HttpMessageHandlerFactory = _ => Host.GetTestServer().CreateHandler())
            .Build();
        _connection.On<string, string>("UpdateChart", (id, json) => Waiting(_json, id).TrySetResult(json));
        _connection.On<string, string>("UpdateChartSvg", (id, svg) => Waiting(_svg, id).TrySetResult(svg));
        await _connection.StartAsync();
    }

    private static TaskCompletionSource<string> Waiting(ConcurrentDictionary<string, TaskCompletionSource<string>> pending, string chartId) =>
        pending.GetOrAdd(chartId, _ => new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously));

    private static Figure ComposedFigure(IQueryCollection query) =>
        ComposedChartShapes.Named(query["name"].ToString()).Build(bool.Parse(query["missing"].ToString())).Build();

    private IChartPublisher Publisher => Host.Services.GetRequiredService<IChartPublisher>();

    /// <summary>Publishes <paramref name="figure"/> as JSON on <paramref name="chartId"/> and returns what the subscribed client receives.</summary>
    public async Task<string> JsonReceivedAsync(string chartId, Figure figure, CancellationToken ct)
    {
        var received = Waiting(_json, chartId);
        await _connection.InvokeAsync("Subscribe", chartId, cancellationToken: ct);
        await Publisher.PublishAsync(chartId, figure, ct);
        return await received.Task.WaitAsync(TimeSpan.FromSeconds(10), ct);
    }

    /// <summary>Publishes <paramref name="figure"/> as SVG on <paramref name="chartId"/> and returns what the subscribed client receives.</summary>
    public async Task<string> SvgReceivedAsync(string chartId, Figure figure, CancellationToken ct)
    {
        var received = Waiting(_svg, chartId);
        await _connection.InvokeAsync("Subscribe", chartId, cancellationToken: ct);
        await Publisher.PublishSvgAsync(chartId, figure, ct);
        return await received.Task.WaitAsync(TimeSpan.FromSeconds(10), ct);
    }

    public async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync();
        await Host.StopAsync(CancellationToken.None);
        Host.Dispose();
    }
}

/// <summary>A composed chart is its parts in JSON, and a client that is sent that JSON draws the chart from it. Every shape
/// is sent through the transports of this package and must come out as the picture the server draws: over the SignalR hub (the
/// JSON the client turns into a figure, and the SVG the server draws for it) and from the chart endpoints. Compared against
/// the figure's own SVG, which the round-trip suite has shown equal to the figure read back from its JSON.</summary>
public class ComposedChartTransportTests : IClassFixture<ComposedChartHostFixture>
{
    private readonly ComposedChartHostFixture _fixture;

    public ComposedChartTransportTests(ComposedChartHostFixture fixture) => _fixture = fixture;

    private static Figure BuildFigure(string name, bool missing) => ComposedChartShapes.Named(name).Build(missing).Build();

    [Theory]
    [MemberData(nameof(ComposedChartShapes.NamesAndVariants), MemberType = typeof(ComposedChartShapes))]
    public async Task TheJsonPublishedOverTheHub_DrawsTheFiguresPicture(string name, bool missing)
    {
        var figure = BuildFigure(name, missing);

        string json = await _fixture.JsonReceivedAsync($"json-{name}-{missing}", figure, TestContext.Current.CancellationToken);

        Assert.Equal(figure.ToSvg(), ChartServices.Serializer.FromJson(json).ToSvg());
    }

    [Theory]
    [MemberData(nameof(ComposedChartShapes.NamesAndVariants), MemberType = typeof(ComposedChartShapes))]
    public async Task TheSvgPublishedOverTheHub_IsTheFiguresPicture(string name, bool missing)
    {
        var figure = BuildFigure(name, missing);

        string svg = await _fixture.SvgReceivedAsync($"svg-{name}-{missing}", figure, TestContext.Current.CancellationToken);

        Assert.Equal(figure.ToSvg(), svg);
    }

    /// <summary>The biggest chart the verbs make is a ridgeline of many groups: six groups of 200 observations write 43 KB of
    /// JSON and 128 KB of SVG, past the 32 KB a hub accepts from a client. Messages the server sends have no such limit, and
    /// the figure must arrive whole, in both forms.</summary>
    [Fact]
    public async Task ASixGroupRidgeline_WhoseJsonAndSvgAreFarBeyond32KB_ArrivesWhole()
    {
        var ct = TestContext.Current.CancellationToken;
        NamedSample[] groups =
        [.. Enumerable.Range(0, 6).Select(g => new NamedSample($"Group {g}",
            [.. Enumerable.Range(0, 200).Select(i => g * 2.0 + ((i * 7919 + g * 104729) % 1000 / 1000.0 - 0.5 + (i * 31 + g) % 1000 / 1000.0 - 0.5) * 3.0)]))];
        var figure = Plt.Create().WithSize(900, 600).AddSubPlot(1, 1, 1, ax => ax.Ridgeline(groups)).Build();
        Assert.True(figure.ToJson().Length > 32 * 1024 && figure.ToSvg().Length > 32 * 1024, "the figure is meant to be larger than 32 KB in both forms");

        string json = await _fixture.JsonReceivedAsync("big-json", figure, ct);
        string svg = await _fixture.SvgReceivedAsync("big-svg", figure, ct);

        Assert.Equal(figure.ToSvg(), ChartServices.Serializer.FromJson(json).ToSvg());
        Assert.Equal(figure.ToSvg(), svg);
    }

    [Theory]
    [MemberData(nameof(ComposedChartShapes.NamesAndVariants), MemberType = typeof(ComposedChartShapes))]
    public async Task TheChartEndpoints_ServeTheFiguresPicture(string name, bool missing)
    {
        var ct = TestContext.Current.CancellationToken;
        var figure = BuildFigure(name, missing);
        using var client = _fixture.Host.GetTestClient();

        string json = await client.GetStringAsync($"/api/composed/json?name={name}&missing={missing}", ct);
        string svg = await client.GetStringAsync($"/api/composed/svg?name={name}&missing={missing}", ct);

        Assert.Equal(figure.ToSvg(), ChartServices.Serializer.FromJson(json).ToSvg());
        Assert.Equal(figure.ToSvg(), svg);
    }
}
