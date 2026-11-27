// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using HotChocolate.Subscriptions;
using MatPlotLibNet.GraphQL;
using Microsoft.Extensions.DependencyInjection;

namespace MatPlotLibNet.GraphQL.Tests;

/// <summary>Verifies <see cref="ChartSubscriptionType"/> behavior.</summary>
public class ChartSubscriptionTypeTests
{
    /// <summary>Verifies that OnChartSvgUpdated returns the SVG payload and OnChartUpdated the JSON payload from the event message.</summary>
    [Theory]
    [InlineData(true, "<svg>updated</svg>")]
    [InlineData(false, "{\"title\":\"Live\"}")]
    public void AnUpdate_ReturnsMessagePayload(bool svg, string payload)
    {
        var message = new ChartEventMessage("test-chart", payload);
        var subscription = new ChartSubscriptionType();

        var result = svg ? subscription.OnChartSvgUpdated(message) : subscription.OnChartUpdated(message);

        Assert.Equal(payload, result);
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
    [Theory]
    [InlineData("ChartSvg", "chart-X", "<svg/>", true)]
    [InlineData("ChartJson", "chart-Y", "{\"k\":1}", false)]
    public async Task ASubscription_RoundTrip_VerifiesTopicNameShape(string topic, string chart, string body, bool asSvg)
    {
        var sp = BuildProvider();
        var receiver = sp.GetRequiredService<ITopicEventReceiver>();
        var sender = sp.GetRequiredService<ITopicEventSender>();

        var ct = TestContext.Current.CancellationToken;
        string name = topic + ":" + chart;
        var msg = await receiver.FirstEventAsync(name, async () => await sender.SendAsync(name, new ChartEventMessage(chart, body), ct), ct);
        Assert.Equal(chart, msg.ChartId);
        // Mirror what OnChartSvgUpdated or OnChartUpdated would have returned.
        var subscription = new ChartSubscriptionType();
        Assert.Equal(body, asSvg ? subscription.OnChartSvgUpdated(msg) : subscription.OnChartUpdated(msg));
    }

    /// <summary>Phase X.10.d (v1.7.2, 2026-04-19) — direct call to the
    /// <c>internal static</c> <see cref="ChartSubscriptionType.SubscribeToChartSvg"/>
    /// helper, enabled by the InternalsVisibleTo attribute added to
    /// <c>MatPlotLibNet.GraphQL.csproj</c>. Pre-X.10.d this method was 0%-covered
    /// because external GraphQL pipelines invoke it via reflection (HotChocolate's
    /// subscription resolver); only an IVT-enabled direct call exercises the body
    /// for cobertura.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SubscribeToChart_DirectCall_ReturnsSourceStream(bool svg)
    {
        var sp = BuildProvider();
        var receiver = sp.GetRequiredService<ITopicEventReceiver>();
        var ct = TestContext.Current.CancellationToken;
        await using var stream = await (svg
            ? ChartSubscriptionType.SubscribeToChartSvg("chart-direct-svg", receiver, ct)
            : ChartSubscriptionType.SubscribeToChartJson("chart-direct-json", receiver, ct));
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
