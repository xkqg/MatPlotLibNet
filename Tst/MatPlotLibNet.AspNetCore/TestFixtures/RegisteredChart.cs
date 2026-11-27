// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.AspNetCore;
using MatPlotLibNet.Models;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MatPlotLibNet.Tests;

/// <summary>A figure registered with the host of a server test under a chart id, and the readers connected to it: disposing it lets every
/// reader's connection go and unregisters the figure, as every such test ends.</summary>
internal sealed class RegisteredChart : IAsyncDisposable
{
    private readonly IHost _host;
    private readonly string _chartId;
    private readonly List<HubConnection> _connections = [];

    private RegisteredChart(IHost host, string chartId)
    {
        _host = host;
        _chartId = chartId;
    }

    /// <summary>Registers <paramref name="figure"/> with the host's registry.</summary>
    /// <param name="host">The host of the test.</param>
    /// <param name="chartId">The id the figure is registered under.</param>
    /// <param name="figure">The figure.</param>
    /// <param name="configure">What the chart's session does with the gestures of its readers; none leaves it at its resting handling.</param>
    /// <returns>The registered chart.</returns>
    internal static RegisteredChart Of(IHost host, string chartId, Figure figure, Action<ChartSessionOptions>? configure = null)
    {
        var registry = host.Services.GetRequiredService<FigureRegistry>();
        if (configure is null) registry.Register(chartId, figure);
        else registry.Register(chartId, figure, configure);
        return new RegisteredChart(host, chartId);
    }

    /// <summary>Connects one more reader to the chart.</summary>
    /// <param name="ct">Cancels the connecting.</param>
    /// <returns>The reader, started and subscribed.</returns>
    internal async Task<ChartViewer> JoinAsync(CancellationToken ct)
    {
        var connection = _host.ConnectToTheHub();
        _connections.Add(connection);
        return await ChartViewer.JoinAsync(connection, _chartId, ct);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        foreach (var connection in _connections) await connection.DisposeAsync();
        await _host.Services.GetRequiredService<FigureRegistry>().UnregisterAsync(_chartId);
    }
}
