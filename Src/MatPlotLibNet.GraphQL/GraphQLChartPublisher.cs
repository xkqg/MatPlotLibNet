// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.AspNetCore;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.GraphQL;

/// <summary>Decorator over <see cref="IChartPublisher"/> that also pushes events to GraphQL subscriptions.</summary>
public sealed class GraphQLChartPublisher(
    IChartPublisher inner,
    IChartEventSender eventSender,
    IChartSerializer serializer,
    ISvgRenderer svgRenderer) : IChartPublisher
{
    /// <inheritdoc/>
    public async Task PublishAsync(string chartId, Figure figure, CancellationToken ct = default)
    {
        await inner.PublishAsync(chartId, figure, ct);
        var json = serializer.ToJson(figure);
        await eventSender.SendJsonAsync(chartId, json, ct);
    }

    /// <inheritdoc/>
    public async Task PublishSvgAsync(string chartId, Figure figure, CancellationToken ct = default)
    {
        await inner.PublishSvgAsync(chartId, figure, ct);
        var svg = svgRenderer.Render(figure);
        await eventSender.SendSvgAsync(chartId, svg, ct);
    }
}
