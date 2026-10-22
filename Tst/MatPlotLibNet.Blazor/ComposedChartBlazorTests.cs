// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Bunit;
using MatPlotLibNet.AspNetCore;
using MatPlotLibNet.Blazor.Tests.Infrastructure;
using MatPlotLibNet.Models;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Blazor.Tests;

/// <summary>A live chart component is fed the JSON a server published and draws the chart from it. Every composed shape,
/// with and without a missing value, must come out of the component as the picture <see cref="MplChart"/> draws for the
/// figure itself: the same element, attribute by attribute.</summary>
public class ComposedChartLiveChartTests : BunitContext
{
    /// <summary>A client that connects to nothing and lets the test hand the component the JSON a hub would deliver.</summary>
    private sealed class FeedClient : IChartSubscriptionClient
    {
        private Func<string, string, Task>? _onJson;

        public bool IsConnected => true;

        public Task ConnectAsync(string hubUrl, CancellationToken ct = default) => Task.CompletedTask;

        public Task SubscribeAsync(string chartId, CancellationToken ct = default) => Task.CompletedTask;

        public Task UnsubscribeAsync(string chartId, CancellationToken ct = default) => Task.CompletedTask;

        public void OnSvgUpdated(Func<string, string, Task> handler) { }

        public void OnChartUpdated(Func<string, string, Task> handler) => _onJson = handler;

        public void Deliver(string chartId, string json) => _onJson!(chartId, json).Wait();

        public ValueTask DisposeAsync() => default;
    }

    [Theory]
    [MemberData(nameof(ComposedChartShapes.NamesAndVariants), MemberType = typeof(ComposedChartShapes))]
    public void ALiveChartFedTheJson_DrawsThePictureOfTheFigure(string name, bool missing)
    {
        var figure = ComposedChartShapes.Named(name).Build(missing).Build();
        var client = new FeedClient();
        var live = Render<MplLiveChart>(parameters => { parameters.Add(x => x.ChartId, "composed"); parameters.Add(x => x.Client, client); });

        client.Deliver("composed", figure.ToJson());
        live.Render();

        var drawn = Render<MplChart>(parameters => parameters.Add(x => x.Figure, figure));
        Assert.Equal(drawn.Find("svg").OuterHtml, live.Find("svg").OuterHtml);
    }
}

/// <summary>One real hub and ONE real client connection for the whole class (a connection costs time to open and close), the
/// way a Blazor page reaches a chart: the server publishes the figure, the client receives what the hub sends.</summary>
public sealed class ComposedChartClientFixture : IAsyncLifetime
{
    private readonly StreamingHostFixture _host = new();
    private readonly ChartSubscriptionClient _client = new();
    private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> _json = new();
    private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> _svg = new();

    public async ValueTask InitializeAsync()
    {
        _client.OnChartUpdated((id, json) => { Waiting(_json, id).TrySetResult(json); return Task.CompletedTask; });
        _client.OnSvgUpdated((id, svg) => { Waiting(_svg, id).TrySetResult(svg); return Task.CompletedTask; });
        await _client.ConnectAsync(_host.HubUrl);
    }

    private static TaskCompletionSource<string> Waiting(ConcurrentDictionary<string, TaskCompletionSource<string>> pending, string chartId) =>
        pending.GetOrAdd(chartId, _ => new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously));

    public async Task<string> JsonReceivedAsync(string chartId, Figure figure, CancellationToken ct)
    {
        var received = Waiting(_json, chartId);
        await _client.SubscribeAsync(chartId, ct);
        await _host.Publisher.PublishAsync(chartId, figure, ct);
        return await received.Task.WaitAsync(TimeSpan.FromSeconds(10), ct);
    }

    public async Task<string> SvgReceivedAsync(string chartId, Figure figure, CancellationToken ct)
    {
        var received = Waiting(_svg, chartId);
        await _client.SubscribeAsync(chartId, ct);
        await _host.Publisher.PublishSvgAsync(chartId, figure, ct);
        return await received.Task.WaitAsync(TimeSpan.FromSeconds(10), ct);
    }

    public async ValueTask DisposeAsync()
    {
        await _client.DisposeAsync();
        await _host.DisposeAsync();
    }
}

/// <summary>The hub client of the Blazor package, against a real hub: the JSON it is sent draws the figure's picture, and the
/// SVG it is sent is the figure's picture.</summary>
public class ComposedChartClientTests : IClassFixture<ComposedChartClientFixture>
{
    private readonly ComposedChartClientFixture _fixture;

    public ComposedChartClientTests(ComposedChartClientFixture fixture) => _fixture = fixture;

    [Theory]
    [MemberData(nameof(ComposedChartShapes.NamesAndVariants), MemberType = typeof(ComposedChartShapes))]
    public async Task TheJsonTheClientReceives_DrawsTheFiguresPicture(string name, bool missing)
    {
        var figure = ComposedChartShapes.Named(name).Build(missing).Build();

        string json = await _fixture.JsonReceivedAsync($"json-{name}-{missing}", figure, Xunit.TestContext.Current.CancellationToken);

        Assert.Equal(figure.ToSvg(), ChartServices.Serializer.FromJson(json).ToSvg());
    }

    [Theory]
    [MemberData(nameof(ComposedChartShapes.NamesAndVariants), MemberType = typeof(ComposedChartShapes))]
    public async Task TheSvgTheClientReceives_IsTheFiguresPicture(string name, bool missing)
    {
        var figure = ComposedChartShapes.Named(name).Build(missing).Build();

        string svg = await _fixture.SvgReceivedAsync($"svg-{name}-{missing}", figure, Xunit.TestContext.Current.CancellationToken);

        Assert.Equal(figure.ToSvg(), svg);
    }
}
