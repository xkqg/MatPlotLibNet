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

    [Theory]
    [InlineData(true, "chart-1", "<svg><rect/></svg>")]
    [InlineData(false, "chart-2", "{\"x\":1}")]
    public async Task ASubscription_DeliversPayloadToSubscriber(bool svg, string chartId, string payload)
    {
        var sp = BuildProvider();
        var receiver = sp.GetRequiredService<ITopicEventReceiver>();
        var sender   = sp.GetRequiredService<IChartEventSender>();

        var ct = TestContext.Current.CancellationToken;
        var msg = await receiver.FirstEventAsync((svg ? "ChartSvg:" : "ChartJson:") + chartId,
            async () => await (svg ? sender.SendSvgAsync(chartId, payload, ct) : sender.SendJsonAsync(chartId, payload, ct)), ct);

        Assert.Equal(chartId, msg.ChartId);
        Assert.Equal(payload, msg.Payload);
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
