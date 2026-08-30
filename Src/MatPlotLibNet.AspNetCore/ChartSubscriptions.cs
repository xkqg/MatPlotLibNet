// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;

namespace MatPlotLibNet.AspNetCore;

/// <summary>Who is looking at which chart. SignalR groups carry no membership count, so a publisher that
/// renders to a group nobody has joined does the work for no one — and a server-rendered wall pays that per
/// frame, per chart. The hub keeps the ledger here; a render lane asks <see cref="HasSubscribers"/> before it
/// spends a frame.</summary>
public interface IChartSubscriptions
{
    /// <summary>Whether at least one connection is subscribed to <paramref name="chartId"/>.</summary>
    bool HasSubscribers(string chartId);

    /// <summary>How many connections are subscribed to <paramref name="chartId"/>.</summary>
    int Count(string chartId);
}

/// <summary>The default ledger: chart → the connections in it. Lock-free — two concurrent dictionaries, no
/// waits — because the hub calls it on every subscribe, unsubscribe and disconnect.</summary>
public sealed class ChartSubscriptions : IChartSubscriptions
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _byChart = new();

    /// <summary>Record that <paramref name="connectionId"/> joined <paramref name="chartId"/>.</summary>
    public void Subscribe(string chartId, string connectionId)
        => _byChart.GetOrAdd(chartId, static _ => new ConcurrentDictionary<string, byte>())[connectionId] = 0;

    /// <summary>Record that <paramref name="connectionId"/> left <paramref name="chartId"/>.</summary>
    public void Unsubscribe(string chartId, string connectionId)
    {
        if (_byChart.TryGetValue(chartId, out var connections))
        {
            connections.TryRemove(connectionId, out _);
        }
    }

    /// <summary>A connection went away: it leaves every chart it joined. A browser that closes never calls
    /// Unsubscribe, and a lane must not keep rendering for a tab that is gone.</summary>
    public void Disconnected(string connectionId)
    {
        foreach (var connections in _byChart.Values)
        {
            connections.TryRemove(connectionId, out _);
        }
    }

    /// <inheritdoc />
    public bool HasSubscribers(string chartId) => Count(chartId) > 0;

    /// <inheritdoc />
    public int Count(string chartId)
        => _byChart.TryGetValue(chartId, out var connections) ? connections.Count : 0;
}
