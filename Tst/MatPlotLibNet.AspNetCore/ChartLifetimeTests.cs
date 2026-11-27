// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Streaming;
using Microsoft.AspNetCore.SignalR;
using NSubstitute;

namespace MatPlotLibNet.AspNetCore.Tests;

/// <summary>When a chart ends, everything held for it ends: its session (a streaming one included), and what the publisher
/// keeps of the last picture it sent. A chart ends when it is unregistered; what the publisher keeps of it is also let go of
/// when its last viewer leaves.</summary>
public sealed class ChartLifetimeTests
{
    private static Figure NewFigure() => Plt.Create().Plot([1.0, 2.0, 3.0], [4.0, 5.0, 6.0]).Build();

    [Fact]
    public async Task UnregisterAsync_EndsAStreamingChart()
    {
        // The streaming sessions were held in a map of their own that unregistering never looked in, so the stream kept
        // rendering and publishing, and kept taking gestures, after the chart was gone.
        var publisher = new KeepingChartPublisher();
        var registry = new FigureRegistry(publisher);
        using var sf = new StreamingFigure(NewFigure());
        registry.RegisterStreaming("s", sf);

        await registry.UnregisterAsync("s");
        sf.RequestRender();

        Assert.Equal(0, publisher.Svgs);
        Assert.False(registry.Publish("s", new ZoomEvent("s", 0, 0, 1, 0, 1)));
    }

    [Fact]
    public async Task UnregisterAsync_TellsThePublisherTheChartEnded()
    {
        var publisher = new KeepingChartPublisher();
        var registry = new FigureRegistry(publisher);
        registry.Register("c", NewFigure());

        await registry.UnregisterAsync("c");

        Assert.Equal(["c"], publisher.Forgotten);
    }

    [Fact]
    public async Task UnregisterAsync_OfAChartNobodyRegistered_TellsNobody()
    {
        var publisher = new KeepingChartPublisher();
        var registry = new FigureRegistry(publisher);

        await registry.UnregisterAsync("never");

        Assert.Empty(publisher.Forgotten);
    }

    [Fact]
    public void AChartRegisteredAgainAsAStream_IsOneChart()
    {
        // One id is one chart: registering a stream under an id that holds a still chart replaces it, and the other way round.
        var publisher = new KeepingChartPublisher();
        var registry = new FigureRegistry(publisher);
        using var sf = new StreamingFigure(NewFigure());

        registry.Register("c", NewFigure());
        registry.RegisterStreaming("c", sf);
        Assert.True(registry.Publish("c", new ZoomEvent("c", 0, 0, 1, 0, 1)));

        registry.Register("c", NewFigure());
        sf.RequestRender();
        Assert.True(registry.Publish("c", new ZoomEvent("c", 0, 0, 1, 0, 1)));
    }

    private static ChartHub Hub(FigureRegistry registry, ChartSubscriptions subscriptions, string connection)
    {
        var caller = Substitute.For<HubCallerContext>();
        caller.ConnectionId.Returns(connection);
        return new ChartHub(registry, subscriptions)
        {
            Context = caller,
            Groups = Substitute.For<IGroupManager>(),
            Clients = Substitute.For<IHubCallerClients<IChartHubClient>>(),
        };
    }

    [Fact]
    public async Task TheLastViewerUnsubscribing_TellsThePublisherToForgetThePicture()
    {
        var publisher = new KeepingChartPublisher();
        var registry = new FigureRegistry(publisher);
        var subscriptions = new ChartSubscriptions();
        subscriptions.Subscribe("c", "a");
        subscriptions.Subscribe("c", "b");

        await Hub(registry, subscriptions, "a").Unsubscribe("c");
        Assert.Empty(publisher.Forgotten);

        await Hub(registry, subscriptions, "b").Unsubscribe("c");
        Assert.Equal(["c"], publisher.Forgotten);
    }

    [Fact]
    public async Task TheLastViewerDisconnecting_TellsThePublisherToForgetThePicture()
    {
        var publisher = new KeepingChartPublisher();
        var registry = new FigureRegistry(publisher);
        var subscriptions = new ChartSubscriptions();
        subscriptions.Subscribe("lonely", "a");
        subscriptions.Subscribe("shared", "a");
        subscriptions.Subscribe("shared", "b");

        await Hub(registry, subscriptions, "a").OnDisconnectedAsync(null);

        Assert.Equal(["lonely"], publisher.Forgotten);
    }
}

