// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Transforms;
using Microsoft.AspNetCore.SignalR;
using NSubstitute;

namespace MatPlotLibNet.AspNetCore.Tests;

/// <summary>The SignalR hub a <see cref="ChartPublisher"/> sends through, for one chart: the group of every viewer, the group of every
/// viewer but those that patch, and one client per connection, each a substitute a test asks what it was sent.</summary>
internal sealed class ChartHubDouble
{
    /// <summary>Starts a hub for the chart a test publishes to.</summary>
    /// <param name="chart">The chart whose groups the hub answers; left out, <c>"chart"</c>.</param>
    public ChartHubDouble(string chart = "chart")
    {
        Clients.Group(chart).Returns(Group);
        Clients.GroupExcept(chart, Arg.Any<IReadOnlyList<string>>()).Returns(AllButParts);
        Clients.Client(Arg.Any<string>()).Returns(call => Proxy((string)call[0]));
        Context.Clients.Returns(Clients);
    }

    /// <summary>The clients of the hub, which a test asks which group or connection was addressed.</summary>
    public IHubClients<IChartHubClient> Clients { get; } = Substitute.For<IHubClients<IChartHubClient>>();

    /// <summary>The group of every viewer of the chart.</summary>
    public IChartHubClient Group { get; } = Substitute.For<IChartHubClient>();

    /// <summary>The group of every viewer of the chart except those that subscribed to its parts.</summary>
    public IChartHubClient AllButParts { get; } = Substitute.For<IChartHubClient>();

    /// <summary>The client of each connection that was asked for, by connection id.</summary>
    public Dictionary<string, IChartHubClient> Parts { get; } = new(StringComparer.Ordinal);

    /// <summary>The hub context a publisher sends through.</summary>
    public IHubContext<ChartHub, IChartHubClient> Context { get; } = Substitute.For<IHubContext<ChartHub, IChartHubClient>>();

    /// <summary>Who is subscribed to what.</summary>
    public ChartSubscriptions Subscriptions { get; } = new();

    /// <summary>The client of one connection, made on the first ask.</summary>
    /// <param name="connection">The connection id.</param>
    /// <returns>The substitute that stands for that connection.</returns>
    public IChartHubClient Proxy(string connection)
    {
        if (!Parts.TryGetValue(connection, out var proxy))
        {
            Parts[connection] = proxy = Substitute.For<IChartHubClient>();
        }

        return proxy;
    }

    /// <summary>A publisher that sends through this hub to the subscriptions of this hub.</summary>
    /// <returns>The publisher.</returns>
    public ChartPublisher Publisher() =>
        new(Context, new MatPlotLibNet.Serialization.ChartSerializer(), new SvgTransform(), Subscriptions);
}
