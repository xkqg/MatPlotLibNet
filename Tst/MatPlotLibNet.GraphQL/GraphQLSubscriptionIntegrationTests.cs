// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using HotChocolate.Execution;
using HotChocolate.Subscriptions;
using MatPlotLibNet.GraphQL;
using Microsoft.Extensions.DependencyInjection;

namespace MatPlotLibNet.GraphQL.Tests;

/// <summary>Phase J.3 of the v1.7.2 follow-on plan — end-to-end topic-bus
/// integration for GraphQL subscriptions. Pre-J.3 only static unit tests
/// existed (does the subscribe method return the payload when called directly?);
/// the HotChocolate subscription pipeline that wires
/// <see cref="ChartEventSender.SendSvgAsync"/> → topic → subscriber was
/// never exercised.
///
/// <para>Uses HotChocolate's in-memory subscription provider so the test
/// runs without an external message bus (Redis, NATS, etc.). Subscribes,
/// sends, asserts the subscriber receives the expected payload.</para></summary>
public class GraphQLSubscriptionIntegrationTests
{
    private static IServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddGraphQLServer()
            .AddQueryType<ChartQueryType>()
            .AddSubscriptionType<ChartSubscriptionType>()
            .AddInMemorySubscriptions();
        services.AddSingleton<IChartEventSender, ChartEventSender>();
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task SvgSubscription_DeliversPayloadToSubscriber()
    {
        var sp = BuildProvider();
        var receiver = sp.GetRequiredService<ITopicEventReceiver>();
        var sender   = sp.GetRequiredService<IChartEventSender>();

        // Subscribe directly to the topic (matches what ChartSubscriptionType does internally).
        var ct = TestContext.Current.CancellationToken;
        await using var stream = await receiver.SubscribeAsync<ChartEventMessage>("ChartSvg:chart-1", ct);

        // Publish on a background task so ReadEventsAsync doesn't hang before send.
        _ = Task.Run(async () =>
        {
            await Task.Delay(50, ct);
            await sender.SendSvgAsync("chart-1", "<svg><rect/></svg>", ct);
        }, ct);

        await foreach (var msg in stream.ReadEventsAsync())
        {
            Assert.Equal("chart-1", msg.ChartId);
            Assert.Equal("<svg><rect/></svg>", msg.Payload);
            return;   // one event is enough
        }
        Assert.Fail("Subscription stream ended without delivering any event");
    }

    [Fact]
    public async Task JsonSubscription_DeliversPayloadToSubscriber()
    {
        var sp = BuildProvider();
        var receiver = sp.GetRequiredService<ITopicEventReceiver>();
        var sender   = sp.GetRequiredService<IChartEventSender>();

        var ct = TestContext.Current.CancellationToken;
        await using var stream = await receiver.SubscribeAsync<ChartEventMessage>("ChartJson:chart-2", ct);

        _ = Task.Run(async () =>
        {
            await Task.Delay(50, ct);
            await sender.SendJsonAsync("chart-2", "{\"x\":1}", ct);
        }, ct);

        await foreach (var msg in stream.ReadEventsAsync())
        {
            Assert.Equal("chart-2", msg.ChartId);
            Assert.Equal("{\"x\":1}", msg.Payload);
            return;
        }
        Assert.Fail("Subscription stream ended without delivering any event");
    }

    [Fact]
    public async Task Subscription_OtherChartId_DoesNotReceive()
    {
        var sp = BuildProvider();
        var receiver = sp.GetRequiredService<ITopicEventReceiver>();
        var sender   = sp.GetRequiredService<IChartEventSender>();

        var ct = TestContext.Current.CancellationToken;
        await using var stream = await receiver.SubscribeAsync<ChartEventMessage>("ChartSvg:chart-A", ct);

        _ = Task.Run(async () =>
        {
            await Task.Delay(50, ct);
            // Send to a DIFFERENT chart's topic.
            await sender.SendSvgAsync("chart-B", "<svg data-leak=\"true\"/>", ct);
        }, ct);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));
        try
        {
            await foreach (var msg in stream.ReadEventsAsync().WithCancellation(cts.Token))
            {
                // If we receive ANY message, it's a cross-topic leak — the topic scope
                // in ChartEventSender isolates chart IDs.
                Assert.Fail($"Cross-chart leak: received {msg.ChartId}:{msg.Payload}");
            }
        }
        catch (OperationCanceledException) { /* expected — no events arrived */ }
    }
}
