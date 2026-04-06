// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.SignalR;
using NSubstitute;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.AspNetCore.Tests;

/// <summary>Verifies <see cref="ChartPublisher"/> behavior.</summary>
public class ChartPublisherTests
{
    /// <summary>Verifies that PublishAsync sends JSON containing the chart title and series type to the specified group.</summary>
    [Fact]
    public async Task PublishAsync_SendsJsonToGroup()
    {
        var clientProxy = Substitute.For<IChartHubClient>();
        var clients = Substitute.For<IHubClients<IChartHubClient>>();
        clients.Group("test-chart").Returns(clientProxy);
        var hubContext = Substitute.For<IHubContext<ChartHub, IChartHubClient>>();
        hubContext.Clients.Returns(clients);

        var publisher = new ChartPublisher(hubContext, new MatPlotLibNet.Serialization.ChartSerializer(), new MatPlotLibNet.Transforms.SvgTransform(new MatPlotLibNet.Rendering.ChartRenderer()));
        var figure = Plt.Create().WithTitle("Test").Plot([1.0], [2.0]).Build();

        await publisher.PublishAsync("test-chart", figure);

        await clientProxy.Received(1).UpdateChart("test-chart", Arg.Is<string>(json =>
            json.Contains("\"title\":\"Test\"") && json.Contains("\"type\":\"line\"")));
    }

    /// <summary>Verifies that PublishSvgAsync sends SVG containing the chart title to the specified group.</summary>
    [Fact]
    public async Task PublishSvgAsync_SendsSvgToGroup()
    {
        var clientProxy = Substitute.For<IChartHubClient>();
        var clients = Substitute.For<IHubClients<IChartHubClient>>();
        clients.Group("my-chart").Returns(clientProxy);
        var hubContext = Substitute.For<IHubContext<ChartHub, IChartHubClient>>();
        hubContext.Clients.Returns(clients);

        var publisher = new ChartPublisher(hubContext, new MatPlotLibNet.Serialization.ChartSerializer(), new MatPlotLibNet.Transforms.SvgTransform(new MatPlotLibNet.Rendering.ChartRenderer()));
        var figure = Plt.Create().WithTitle("SVG Test").Plot([1.0], [2.0]).Build();

        await publisher.PublishSvgAsync("my-chart", figure);

        await clientProxy.Received(1).UpdateChartSvg("my-chart", Arg.Is<string>(svg =>
            svg.Contains("<svg") && svg.Contains("SVG Test")));
    }

    /// <summary>Verifies that PublishAsync targets the correct SignalR group name.</summary>
    [Fact]
    public async Task PublishAsync_UsesCorrectGroupName()
    {
        var clientProxy = Substitute.For<IChartHubClient>();
        var clients = Substitute.For<IHubClients<IChartHubClient>>();
        clients.Group("sensor-42").Returns(clientProxy);
        var hubContext = Substitute.For<IHubContext<ChartHub, IChartHubClient>>();
        hubContext.Clients.Returns(clients);

        var publisher = new ChartPublisher(hubContext, new MatPlotLibNet.Serialization.ChartSerializer(), new MatPlotLibNet.Transforms.SvgTransform(new MatPlotLibNet.Rendering.ChartRenderer()));
        var figure = Plt.Create().Build();

        await publisher.PublishAsync("sensor-42", figure);

        clients.Received(1).Group("sensor-42");
    }
}
