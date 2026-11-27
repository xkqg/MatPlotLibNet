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
    ISvgRenderer svgRenderer) : IChartPublisher, IKeepingChartPublisher
{
    /// <inheritdoc/>
    public async Task PublishAsync(string chartId, Figure figure, CancellationToken ct = default)
    {
        await inner.PublishAsync(chartId, figure, ct);
        var json = serializer.ToJson(figure);
        await eventSender.SendJsonAsync(chartId, json, ct);
    }

    /// <inheritdoc/>
    /// <remarks>The subscribers are sent the SVG the inner publisher published, when it hands it on (the library's own does): the
    /// figure is drawn once per publish. Only over a publisher that says nothing of what it sent is the SVG drawn here.</remarks>
    public async Task PublishSvgAsync(string chartId, Figure figure, CancellationToken ct = default) =>
        _ = await HandOnAsync(chartId, figure, ct);

    /// <summary>Publishes as <see cref="PublishSvgAsync"/> does and hands on the SVG the subscribers were sent, so a decorator of
    /// this one draws nothing again.</summary>
    Task<string?> IKeepingChartPublisher.PublishSvgAndHandOnAsync(string chartId, Figure figure, CancellationToken ct) =>
        HandOnAsync(chartId, figure, ct);

    /// <summary>The one publish of an SVG: through the publisher this decorates, then to the subscribers.</summary>
    private async Task<string?> HandOnAsync(string chartId, Figure figure, CancellationToken ct)
    {
        var svg = await inner.PublishSvgAndHandOnAsync(chartId, figure, ct) ?? svgRenderer.Render(figure);
        await eventSender.SendSvgAsync(chartId, svg, ct);
        return svg;
    }

    /// <inheritdoc/>
    /// <remarks>Handed to the publisher this decorates: a view change is a message to the viewers of the chart, which the
    /// GraphQL subscriptions do not carry.</remarks>
    public Task PublishViewChangeAsync(string chartId, string viewerId, string viewerName, CancellationToken ct = default) =>
        inner.PublishViewChangeAsync(chartId, viewerId, viewerName, ct);

    /// <summary>Handed to the publisher this decorates, which is what keeps something per chart.</summary>
    Task IKeepingChartPublisher.ForgetAsync(string chartId) => inner.ForgetAsync(chartId);
}
