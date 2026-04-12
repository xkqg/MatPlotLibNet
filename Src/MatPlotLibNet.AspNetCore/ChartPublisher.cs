// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.SignalR;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.AspNetCore;

/// <summary>Default <see cref="IChartPublisher"/> implementation that broadcasts chart updates via a SignalR hub.</summary>
public sealed class ChartPublisher(
    IHubContext<ChartHub, IChartHubClient> hubContext,
    IChartSerializer serializer,
    ISvgRenderer svgRenderer) : IChartPublisher
{
    /// <inheritdoc />
    public async Task PublishAsync(string chartId, Figure figure, CancellationToken ct = default)
    {
        var json = serializer.ToJson(figure);
        await hubContext.Clients.Group(chartId).UpdateChart(chartId, json);
    }

    /// <inheritdoc />
    public async Task PublishSvgAsync(string chartId, Figure figure, CancellationToken ct = default)
    {
        var svg = svgRenderer.Render(figure);
        await hubContext.Clients.Group(chartId).UpdateChartSvg(chartId, svg);
    }
}
