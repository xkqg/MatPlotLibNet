// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using HotChocolate.Subscriptions;
using MatPlotLibNet.AspNetCore;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Transforms;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace MatPlotLibNet.GraphQL.Tests;

/// <summary>Phase X.4.b (v1.7.2, 2026-04-19) — drives every code path in
/// <see cref="GraphQLChartPublisher"/>. Pre-X the class was at 0%L because no test
/// exercised it. Two facts cover the two public methods (PublishAsync + PublishSvgAsync)
/// end-to-end through HotChocolate's in-memory topic bus + a real
/// <see cref="ChartEventSender"/>, with NSubstitute at the inner-publisher boundary
/// so the decorator's wiring (delegate-then-publish-event) is the actual code path
/// under test. The subscriber side reuses the integration pattern from
/// <see cref="GraphQLSubscriptionIntegrationTests"/>.</summary>
public class GraphQLChartPublisherTests
{
    private static (GraphQLChartPublisher publisher, IChartPublisher inner, ITopicEventReceiver receiver) BuildHarness()
    {
        var services = new ServiceCollection();
        services.AddGraphQLServer()
            .AddQueryType<ChartQueryType>()
            .AddSubscriptionType<ChartSubscriptionType>()
            .AddInMemorySubscriptions();
        services.AddSingleton<IChartEventSender, ChartEventSender>();
        var sp = services.BuildServiceProvider();

        var inner = Substitute.For<IChartPublisher>();
        var sender = sp.GetRequiredService<IChartEventSender>();
        var serializer = new ChartSerializer();
        ISvgRenderer svgRenderer = new SvgTransform(new ChartRenderer());
        var publisher = new GraphQLChartPublisher(inner, sender, serializer, svgRenderer);
        var receiver = sp.GetRequiredService<ITopicEventReceiver>();
        return (publisher, inner, receiver);
    }

    /// <summary>PublishAsync: delegates to inner.PublishAsync, then
    /// serialises the figure to JSON and sends it via IChartEventSender.SendJsonAsync.
    /// Verified by subscribing to the JSON topic + asserting the payload arrives.</summary>
    [Fact]
    public async Task PublishAsync_DelegatesToInner_AndPushesJsonToSubscribers()
    {
        var (publisher, inner, receiver) = BuildHarness();
        var fig = Plt.Create().WithTitle("X.4.b").Plot([1.0], [2.0]).Build();

        var ct = TestContext.Current.CancellationToken;
        var msg = await receiver.FirstEventAsync("ChartJson:chart-json-1", async () => await publisher.PublishAsync("chart-json-1", fig, ct), ct);
        Assert.Equal("chart-json-1", msg.ChartId);
        Assert.Contains("\"title\":\"X.4.b\"", msg.Payload);

        // Inner publisher was invoked exactly once with the same chartId + figure.
        await inner.Received(1).PublishAsync("chart-json-1", fig, Arg.Any<CancellationToken>());
    }

    /// <summary>PublishSvgAsync: delegates to inner.PublishSvgAsync, then
    /// renders the figure to SVG and sends it via IChartEventSender.SendSvgAsync.</summary>
    [Fact]
    public async Task PublishSvgAsync_DelegatesToInner_AndPushesSvgToSubscribers()
    {
        var (publisher, inner, receiver) = BuildHarness();
        var fig = Plt.Create().WithTitle("X.4.b SVG").Plot([1.0], [2.0]).Build();

        var ct = TestContext.Current.CancellationToken;
        var msg = await receiver.FirstEventAsync("ChartSvg:chart-svg-1", async () => await publisher.PublishSvgAsync("chart-svg-1", fig, ct), ct);
        Assert.Equal("chart-svg-1", msg.ChartId);
        Assert.StartsWith("<svg", msg.Payload);
        Assert.Contains("X.4.b SVG", msg.Payload);

        await inner.Received(1).PublishSvgAsync("chart-svg-1", fig, Arg.Any<CancellationToken>());
    }

    /// <summary>Over the library's own publisher the decorator draws nothing: it sends its subscribers the SVG the hub was sent,
    /// so a figure is drawn once per publish, not once for the hub and again for GraphQL.</summary>
    [Fact]
    public async Task OverTheLibrarysPublisher_ThePublishedSvgIsHandedOn_NotDrawnAgain()
    {
        var hub = Substitute.For<Microsoft.AspNetCore.SignalR.IHubContext<ChartHub, IChartHubClient>>();
        string? toHub = null;
        hub.Clients.Group("c").UpdateChartSvg("c", Arg.Do<string>(svg => toHub = svg)).Returns(Task.CompletedTask);
        var inner = new ChartPublisher(hub, new ChartSerializer(), new SvgTransform(new ChartRenderer()));
        var sender = Substitute.For<IChartEventSender>();
        string? toGraphQL = null;
        sender.SendSvgAsync("c", Arg.Do<string>(svg => toGraphQL = svg), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        var ownRenderer = Substitute.For<ISvgRenderer>();
        var publisher = new GraphQLChartPublisher(inner, sender, new ChartSerializer(), ownRenderer);

        await publisher.PublishSvgAsync("c", Plt.Create().WithTitle("once").Plot([0.0, 1], [1.0, 2]).Build(), TestContext.Current.CancellationToken);

        Assert.NotNull(toHub);
        Assert.Equal(toHub, toGraphQL);
        ownRenderer.DidNotReceive().Render(Arg.Any<Figure>());
    }

    /// <summary>Over a publisher of the caller's, which says nothing of what it sent, the decorator draws the SVG itself.</summary>
    [Fact]
    public async Task OverAPublisherOfTheCallers_TheDecoratorDrawsTheSvgItself()
    {
        var inner = Substitute.For<IChartPublisher>();
        var sender = Substitute.For<IChartEventSender>();
        var ownRenderer = Substitute.For<ISvgRenderer>();
        ownRenderer.Render(Arg.Any<Figure>()).Returns("<svg/>");
        var publisher = new GraphQLChartPublisher(inner, sender, new ChartSerializer(), ownRenderer);

        await publisher.PublishSvgAsync("c", Plt.Create().Plot([0.0, 1], [1.0, 2]).Build(), TestContext.Current.CancellationToken);

        await sender.Received(1).SendSvgAsync("c", "<svg/>", Arg.Any<CancellationToken>());
    }

    /// <summary>The decorator hands on the SVG it sent its subscribers, so one decorating it in turn draws nothing again.</summary>
    [Fact]
    public async Task TheDecorator_HandsOnTheSvgItSentItsSubscribers()
    {
        var inner = Substitute.For<IChartPublisher>();
        var sender = Substitute.For<IChartEventSender>();
        var ownRenderer = Substitute.For<ISvgRenderer>();
        ownRenderer.Render(Arg.Any<Figure>()).Returns("<svg/>");
        IChartPublisher publisher = new GraphQLChartPublisher(inner, sender, new ChartSerializer(), ownRenderer);

        string? handedOn = await publisher.PublishSvgAndHandOnAsync("c", Plt.Create().Plot([0.0, 1], [1.0, 2]).Build(),
            TestContext.Current.CancellationToken);

        Assert.Equal("<svg/>", handedOn);
        await sender.Received(1).SendSvgAsync("c", "<svg/>", Arg.Any<CancellationToken>());
        await inner.Received(1).PublishSvgAsync("c", Arg.Any<Figure>(), Arg.Any<CancellationToken>());
    }

    /// <summary>A session given the decorator tells the viewers who moved the view through it: the decorator hands the message
    /// to the publisher it wraps. It did not, so the interface's default (send nothing) answered and nobody was told.</summary>
    [Fact]
    public async Task PublishViewChangeAsync_IsHandedToTheInnerPublisher()
    {
        var inner = Substitute.For<IChartPublisher>();
        var publisher = new GraphQLChartPublisher(inner, Substitute.For<IChartEventSender>(), new ChartSerializer(), Substitute.For<ISvgRenderer>());

        await ((IChartPublisher)publisher).PublishViewChangeAsync("c", "conn-1", "Viewer 1", TestContext.Current.CancellationToken);

        await inner.Received(1).PublishViewChangeAsync("c", "conn-1", "Viewer 1", Arg.Any<CancellationToken>());
    }

    /// <summary>A chart that ends is forgotten by the publisher the decorator wraps, so what that publisher keeps of the chart
    /// is let go of when the registry unregisters it through the decorator.</summary>
    [Fact]
    public async Task AChartUnregisteredThroughTheDecorator_IsForgottenByTheInnerPublisher()
    {
        var hub = Substitute.For<Microsoft.AspNetCore.SignalR.IHubContext<ChartHub, IChartHubClient>>();
        hub.Clients.Group("c").UpdateChartSvg("c", Arg.Any<string>()).Returns(Task.CompletedTask);
        var subscriptions = new ChartSubscriptions();
        subscriptions.Subscribe("c", "viewer");
        var inner = new ChartPublisher(hub, new ChartSerializer(), new SvgTransform(new ChartRenderer()), subscriptions);
        var publisher = new GraphQLChartPublisher(inner, Substitute.For<IChartEventSender>(), new ChartSerializer(), Substitute.For<ISvgRenderer>());
        var registry = new FigureRegistry(publisher);
        var figure = Plt.Create().Plot([0.0, 1], [1.0, 2]).Build();
        registry.Register("c", figure);
        await publisher.PublishSvgAsync("c", figure, TestContext.Current.CancellationToken);
        Assert.True(inner.Holds("c"));

        await registry.UnregisterAsync("c");

        Assert.False(inner.Holds("c"));
    }
}
