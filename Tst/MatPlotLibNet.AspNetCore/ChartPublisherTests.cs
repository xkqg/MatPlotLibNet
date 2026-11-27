// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using NSubstitute;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.AspNetCore.Tests;

/// <summary>Verifies <see cref="ChartPublisher"/> behavior.</summary>
public class ChartPublisherTests
{
    private static ChartHubDouble HubOf(string chart, out ChartPublisher publisher)
    {
        var hub = new ChartHubDouble(chart);
        publisher = hub.Publisher();
        return hub;
    }

    private static Figure Titled(string title) => Plt.Create().WithTitle(title).Plot([1.0], [2.0]).Build();

    /// <summary>Verifies that PublishAsync sends JSON containing the chart title and series type to the specified group.</summary>
    [Fact]
    public async Task PublishAsync_SendsJsonToGroup()
    {
        var hub = HubOf("test-chart", out var publisher);

        await publisher.PublishAsync("test-chart", Titled("Test"), TestContext.Current.CancellationToken);

        await hub.Group.Received(1).UpdateChart("test-chart", Arg.Is<string>(json =>
            json.Contains("\"title\":\"Test\"") && json.Contains("\"type\":\"line\"")));
    }

    /// <summary>Verifies that PublishSvgAsync sends SVG containing the chart title to the specified group.</summary>
    [Fact]
    public async Task PublishSvgAsync_SendsSvgToGroup()
    {
        var hub = HubOf("my-chart", out var publisher);

        await publisher.PublishSvgAsync("my-chart", Titled("SVG Test"), TestContext.Current.CancellationToken);

        await hub.Group.Received(1).UpdateChartSvg("my-chart", Arg.Is<string>(svg =>
            svg.Contains("<svg") && svg.Contains("SVG Test")));
    }

    /// <summary>Verifies that PublishAsync targets the correct SignalR group name.</summary>
    [Fact]
    public async Task PublishAsync_UsesCorrectGroupName()
    {
        var hub = HubOf("sensor-42", out var publisher);

        await publisher.PublishAsync("sensor-42", Plt.Create().Build(), TestContext.Current.CancellationToken);

        hub.Clients.Received(1).Group("sensor-42");
    }
}
