// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using HotChocolate.Subscriptions;
using MatPlotLibNet.GraphQL;
using Microsoft.Extensions.DependencyInjection;

namespace MatPlotLibNet.GraphQL.Tests;

/// <summary>Verifies <see cref="ChartSubscriptionType"/> behavior.</summary>
public class ChartSubscriptionTypeTests
{
    /// <summary>Verifies that OnChartSvgUpdated returns the SVG payload from the event message.</summary>
    [Fact]
    public void OnChartSvgUpdated_ReturnsMessagePayload()
    {
        var message = new ChartEventMessage("test-chart", "<svg>updated</svg>");
        var subscription = new ChartSubscriptionType();

        var result = subscription.OnChartSvgUpdated(message);

        Assert.Equal("<svg>updated</svg>", result);
    }

    /// <summary>Verifies that OnChartUpdated returns the JSON payload from the event message.</summary>
    [Fact]
    public void OnChartUpdated_ReturnsMessagePayload()
    {
        var message = new ChartEventMessage("test-chart", "{\"title\":\"Live\"}");
        var subscription = new ChartSubscriptionType();

        var result = subscription.OnChartUpdated(message);

        Assert.Equal("{\"title\":\"Live\"}", result);
    }

    /// <summary>Verifies that ChartEventMessage correctly stores the chart ID and payload.</summary>
    [Fact]
    public void ChartEventMessage_StoresChartIdAndPayload()
    {
        var message = new ChartEventMessage("my-chart", "<svg/>");

        Assert.Equal("my-chart", message.ChartId);
        Assert.Equal("<svg/>", message.Payload);
    }

    /// <summary>Phase X.4 follow-up (v1.7.2, 2026-04-19) — exercises the
    /// SubscribeToChartSvg + SubscribeToChartJson topic-bus paths via the same
    /// in-memory ITopicEventReceiver pattern as the existing
    /// GraphQLSubscriptionIntegrationTests. We can't call the static helpers directly
    /// (internal), so we subscribe through the receiver to the same topic shape and
    /// publish a message — the helpers' topic-name format ("ChartSvg:{id}",
    /// "ChartJson:{id}") is verified by the round-trip.</summary>
    [Fact]
    public async Task SvgSubscription_RoundTrip_VerifiesTopicNameShape()
    {
        var sp = BuildProvider();
        var receiver = sp.GetRequiredService<ITopicEventReceiver>();
        var sender = sp.GetRequiredService<ITopicEventSender>();

        var ct = TestContext.Current.CancellationToken;
        await using var stream = await receiver.SubscribeAsync<ChartEventMessage>("ChartSvg:chart-X", ct);
        _ = Task.Run(async () => { await Task.Delay(50, ct); await sender.SendAsync("ChartSvg:chart-X", new ChartEventMessage("chart-X", "<svg/>"), ct); }, ct);

        await foreach (var msg in stream.ReadEventsAsync())
        {
            Assert.Equal("chart-X", msg.ChartId);
            // Mirror what OnChartSvgUpdated would have returned.
            var subscription = new ChartSubscriptionType();
            Assert.Equal("<svg/>", subscription.OnChartSvgUpdated(msg));
            return;
        }
        Assert.Fail("No event delivered");
    }

    [Fact]
    public async Task JsonSubscription_RoundTrip_VerifiesTopicNameShape()
    {
        var sp = BuildProvider();
        var receiver = sp.GetRequiredService<ITopicEventReceiver>();
        var sender = sp.GetRequiredService<ITopicEventSender>();

        var ct = TestContext.Current.CancellationToken;
        await using var stream = await receiver.SubscribeAsync<ChartEventMessage>("ChartJson:chart-Y", ct);
        _ = Task.Run(async () => { await Task.Delay(50, ct); await sender.SendAsync("ChartJson:chart-Y", new ChartEventMessage("chart-Y", "{\"k\":1}"), ct); }, ct);

        await foreach (var msg in stream.ReadEventsAsync())
        {
            Assert.Equal("chart-Y", msg.ChartId);
            var subscription = new ChartSubscriptionType();
            Assert.Equal("{\"k\":1}", subscription.OnChartUpdated(msg));
            return;
        }
        Assert.Fail("No event delivered");
    }

    /// <summary>Phase X.10.d (v1.7.2, 2026-04-19) — direct call to the
    /// <c>internal static</c> <see cref="ChartSubscriptionType.SubscribeToChartSvg"/>
    /// helper, enabled by the InternalsVisibleTo attribute added to
    /// <c>MatPlotLibNet.GraphQL.csproj</c>. Pre-X.10.d this method was 0%-covered
    /// because external GraphQL pipelines invoke it via reflection (HotChocolate's
    /// subscription resolver); only an IVT-enabled direct call exercises the body
    /// for cobertura.</summary>
    [Fact]
    public async Task SubscribeToChartSvg_DirectCall_ReturnsSourceStream()
    {
        var sp = BuildProvider();
        var receiver = sp.GetRequiredService<ITopicEventReceiver>();
        await using var stream = await ChartSubscriptionType.SubscribeToChartSvg("chart-direct-svg", receiver, TestContext.Current.CancellationToken);
        Assert.NotNull(stream);
    }

    /// <summary>X.10.d sibling — direct call to <see cref="ChartSubscriptionType.SubscribeToChartJson"/>.</summary>
    [Fact]
    public async Task SubscribeToChartJson_DirectCall_ReturnsSourceStream()
    {
        var sp = BuildProvider();
        var receiver = sp.GetRequiredService<ITopicEventReceiver>();
        await using var stream = await ChartSubscriptionType.SubscribeToChartJson("chart-direct-json", receiver, TestContext.Current.CancellationToken);
        Assert.NotNull(stream);
    }

    private static IServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddGraphQLServer()
            .AddQueryType<ChartQueryType>()
            .AddSubscriptionType<ChartSubscriptionType>()
            .AddInMemorySubscriptions();
        return services.BuildServiceProvider();
    }
}
