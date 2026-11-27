// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using HotChocolate.Execution;
using HotChocolate.Subscriptions;
using Microsoft.Extensions.DependencyInjection;

namespace MatPlotLibNet.GraphQL.Tests;

/// <summary>The wait for the first event of a topic every subscription test makes.</summary>
public sealed class TopicEventsTests
{
    private static ServiceProvider Provider() => new ServiceCollection()
        .AddGraphQLServer().AddQueryType<ChartQueryType>().AddSubscriptionType<ChartSubscriptionType>().AddInMemorySubscriptions()
        .Services.BuildServiceProvider();

    [Fact]
    public async Task TheFirstEvent_IsTheOneSentToTheTopic()
    {
        using var provider = Provider();
        var receiver = provider.GetRequiredService<ITopicEventReceiver>();
        var sender = provider.GetRequiredService<ITopicEventSender>();
        var ct = TestContext.Current.CancellationToken;

        var message = await receiver.FirstEventAsync("T:1", async () => await sender.SendAsync("T:1", new ChartEventMessage("c", "p"), ct), ct);

        Assert.Equal(new ChartEventMessage("c", "p"), message);
    }
}
