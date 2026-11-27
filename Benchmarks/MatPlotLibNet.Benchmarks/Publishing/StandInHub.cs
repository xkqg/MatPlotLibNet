// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.AspNetCore;
using Microsoft.AspNetCore.SignalR;

namespace MatPlotLibNet.Benchmarks.Publishing;

/// <summary>A hub context whose every client is the same <see cref="CountingClient"/>: a send costs what the publisher spends
/// choosing and building it, with no connection behind it, so a measurement is the publisher's own work and not a socket's.</summary>
public sealed class StandInHub : IHubContext<ChartHub, IChartHubClient>
{
    private readonly StandInClients _clients;

    /// <summary>A hub whose clients all report to <paramref name="client"/>.</summary>
    public StandInHub(CountingClient client) => _clients = new StandInClients(client);

    /// <inheritdoc />
    public IHubClients<IChartHubClient> Clients => _clients;

    /// <inheritdoc />
    public IGroupManager Groups => throw new NotSupportedException("The publisher never manages groups.");

    private sealed class StandInClients(IChartHubClient client) : IHubClients<IChartHubClient>
    {
        public IChartHubClient All => client;
        public IChartHubClient AllExcept(IReadOnlyList<string> excludedConnectionIds) => client;
        public IChartHubClient Client(string connectionId) => client;
        public IChartHubClient Clients(IReadOnlyList<string> connectionIds) => client;
        public IChartHubClient Group(string groupName) => client;
        public IChartHubClient GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => client;
        public IChartHubClient Groups(IReadOnlyList<string> groupNames) => client;
        public IChartHubClient User(string userId) => client;
        public IChartHubClient Users(IReadOnlyList<string> userIds) => client;
    }
}

/// <summary>The viewer a <see cref="StandInHub"/> sends to: it counts what it was sent and keeps nothing else.</summary>
public sealed class CountingClient : IChartHubClient
{
    /// <summary>How many sends reached the client.</summary>
    public long Sends { get; private set; }

    /// <summary>How many characters of document the client was sent, a stand-in for bytes on the wire.</summary>
    public long Characters { get; private set; }

    /// <inheritdoc />
    public Task UpdateChart(string chartId, string figureJson) => Took(figureJson.Length);

    /// <inheritdoc />
    public Task UpdateChartSvg(string chartId, string svg) => Took(svg.Length);

    /// <inheritdoc />
    public Task UpdateChartParts(string chartId, ChartSvgParts parts) => Took(parts.Axes.Sum(a => a.Markup.Length) + (parts.RootTag?.Length ?? 0) + (parts.Background?.Length ?? 0) + (parts.Overlay?.Length ?? 0));

    /// <inheritdoc />
    public Task ReceiveTooltipContent(string chartId, string html) => Took(html.Length);

    /// <inheritdoc />
    public Task ReceiveCursor(string chartId, string viewerId, string viewerName, int axesIndex, double x, double y) => Took(0);

    /// <inheritdoc />
    public Task ViewChanged(string chartId, string viewerId, string viewerName) => Took(0);

    /// <inheritdoc />
    public Task ViewerLeft(string chartId, string viewerId) => Took(0);

    private Task Took(int characters)
    {
        Sends++;
        Characters += characters;
        return Task.CompletedTask;
    }
}
