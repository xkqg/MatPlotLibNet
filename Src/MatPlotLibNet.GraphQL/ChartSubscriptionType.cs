// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Subscriptions;
using HotChocolate.Types;

namespace MatPlotLibNet.GraphQL;

/// <summary>GraphQL subscription type for receiving real-time chart updates.</summary>
public sealed class ChartSubscriptionType
{
    /// <summary>Subscribes to SVG updates for the specified chart.</summary>
    [Subscribe(With = nameof(SubscribeToChartSvg))]
    public string OnChartSvgUpdated([EventMessage] ChartEventMessage message) =>
        message.Payload;

    /// <summary>Subscribes to JSON updates for the specified chart.</summary>
    [Subscribe(With = nameof(SubscribeToChartJson))]
    public string OnChartUpdated([EventMessage] ChartEventMessage message) =>
        message.Payload;

    internal static ValueTask<ISourceStream<ChartEventMessage>> SubscribeToChartSvg(
        string chartId,
        [Service] ITopicEventReceiver receiver,
        CancellationToken ct) =>
        receiver.SubscribeAsync<ChartEventMessage>($"ChartSvg:{chartId}", ct);

    internal static ValueTask<ISourceStream<ChartEventMessage>> SubscribeToChartJson(
        string chartId,
        [Service] ITopicEventReceiver receiver,
        CancellationToken ct) =>
        receiver.SubscribeAsync<ChartEventMessage>($"ChartJson:{chartId}", ct);
}
