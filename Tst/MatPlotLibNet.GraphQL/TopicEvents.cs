// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using HotChocolate.Subscriptions;

namespace MatPlotLibNet.GraphQL.Tests;

/// <summary>How a test waits for the first event of a subscription topic: it subscribes, sends from another task, and reads one event.</summary>
public static class TopicEvents
{
    extension(ITopicEventReceiver receiver)
    {
        /// <summary>Subscribes to <paramref name="topic"/>, runs <paramref name="send"/> on another task once the subscription stands, and
        /// returns the first event that arrives.</summary>
        /// <param name="topic">The topic, such as <c>ChartSvg:chart-1</c>.</param>
        /// <param name="send">Sends to the topic; it runs on a task of its own after a short delay, so the read below cannot race it.</param>
        /// <param name="ct">Cancels the wait.</param>
        public async Task<ChartEventMessage> FirstEventAsync(string topic, Func<Task> send, CancellationToken ct)
        {
            await using var stream = await receiver.SubscribeAsync<ChartEventMessage>(topic, ct);
            _ = Task.Run(async () =>
            {
                await Task.Delay(50, ct);
                await send();
            }, ct);

            await foreach (var message in stream.ReadEventsAsync())
            {
                return message;
            }

            Assert.Fail($"The subscription to '{topic}' ended without delivering an event");
            return null!;
        }
    }
}
