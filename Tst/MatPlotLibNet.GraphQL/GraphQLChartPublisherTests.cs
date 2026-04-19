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

    /// <summary>PublishAsync (line 19-24): delegates to inner.PublishAsync, then
    /// serialises the figure to JSON and sends it via IChartEventSender.SendJsonAsync.
    /// Verified by subscribing to the JSON topic + asserting the payload arrives.</summary>
    [Fact]
    public async Task PublishAsync_DelegatesToInner_AndPushesJsonToSubscribers()
    {
        var (publisher, inner, receiver) = BuildHarness();
        var fig = Plt.Create().WithTitle("X.4.b").Plot([1.0], [2.0]).Build();

        await using var stream = await receiver.SubscribeAsync<ChartEventMessage>("ChartJson:chart-json-1");

        // Publish on a background task so the read loop below doesn't race the send.
        _ = Task.Run(async () =>
        {
            await Task.Delay(50);
            await publisher.PublishAsync("chart-json-1", fig);
        });

        await foreach (var msg in stream.ReadEventsAsync())
        {
            Assert.Equal("chart-json-1", msg.ChartId);
            Assert.Contains("\"title\":\"X.4.b\"", msg.Payload);
            break;
        }

        // Inner publisher was invoked exactly once with the same chartId + figure.
        await inner.Received(1).PublishAsync("chart-json-1", fig, Arg.Any<CancellationToken>());
    }

    /// <summary>PublishSvgAsync (line 27-32): delegates to inner.PublishSvgAsync, then
    /// renders the figure to SVG and sends it via IChartEventSender.SendSvgAsync.</summary>
    [Fact]
    public async Task PublishSvgAsync_DelegatesToInner_AndPushesSvgToSubscribers()
    {
        var (publisher, inner, receiver) = BuildHarness();
        var fig = Plt.Create().WithTitle("X.4.b SVG").Plot([1.0], [2.0]).Build();

        await using var stream = await receiver.SubscribeAsync<ChartEventMessage>("ChartSvg:chart-svg-1");

        _ = Task.Run(async () =>
        {
            await Task.Delay(50);
            await publisher.PublishSvgAsync("chart-svg-1", fig);
        });

        await foreach (var msg in stream.ReadEventsAsync())
        {
            Assert.Equal("chart-svg-1", msg.ChartId);
            Assert.StartsWith("<svg", msg.Payload);
            Assert.Contains("X.4.b SVG", msg.Payload);
            break;
        }

        await inner.Received(1).PublishSvgAsync("chart-svg-1", fig, Arg.Any<CancellationToken>());
    }
}
