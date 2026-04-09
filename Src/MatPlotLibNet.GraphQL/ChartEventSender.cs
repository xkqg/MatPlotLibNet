// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using HotChocolate.Subscriptions;

namespace MatPlotLibNet.GraphQL;

/// <summary>Bridges chart update events to HotChocolate's topic-based subscription system.</summary>
public sealed class ChartEventSender(ITopicEventSender eventSender) : IChartEventSender
{
    /// <inheritdoc/>
    public async Task SendSvgAsync(string chartId, string svg, CancellationToken ct = default) =>
        await eventSender.SendAsync($"ChartSvg:{chartId}", new ChartEventMessage(chartId, svg), ct);

    /// <inheritdoc/>
    public async Task SendJsonAsync(string chartId, string json, CancellationToken ct = default) =>
        await eventSender.SendAsync($"ChartJson:{chartId}", new ChartEventMessage(chartId, json), ct);
}
