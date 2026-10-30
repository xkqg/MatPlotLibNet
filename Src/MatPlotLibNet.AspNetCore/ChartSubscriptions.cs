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

/// <summary>The default ledger: chart → the connections in it, each with the number it was given when it joined.
/// Lock-free — concurrent dictionaries and one interlocked counter per chart, no waits — because the hub calls it on every
/// subscribe, unsubscribe, disconnect and event.
/// <para>The number is what a viewer is called ("Viewer 3"). It follows the order of joining and is never given twice for
/// one chart: a viewer who leaves and comes back is a new viewer with the next number, so a name in a log or a cursor label
/// cannot later belong to someone else.</para></summary>
public sealed class ChartSubscriptions : IChartSubscriptions
{
    private sealed class Viewers
    {
        internal readonly ConcurrentDictionary<string, int> Numbers = new();
        internal int Last;
    }

    private readonly ConcurrentDictionary<string, Viewers> _byChart = new();

    /// <summary>Record that <paramref name="connectionId"/> joined <paramref name="chartId"/>. A connection that is already
    /// in the chart keeps its number.</summary>
    public void Subscribe(string chartId, string connectionId)
    {
        var viewers = _byChart.GetOrAdd(chartId, static _ => new Viewers());
        viewers.Numbers.GetOrAdd(connectionId, static (_, state) => Interlocked.Increment(ref state.Last), viewers);
    }

    /// <summary>Record that <paramref name="connectionId"/> left <paramref name="chartId"/>.</summary>
    public void Unsubscribe(string chartId, string connectionId)
    {
        if (_byChart.TryGetValue(chartId, out var viewers))
        {
            viewers.Numbers.TryRemove(connectionId, out _);
        }
    }

    /// <summary>A connection went away: it leaves every chart it joined. A browser that closes never calls
    /// Unsubscribe, and a lane must not keep rendering for a tab that is gone.</summary>
    public void Disconnected(string connectionId)
    {
        foreach (var viewers in _byChart.Values)
        {
            viewers.Numbers.TryRemove(connectionId, out _);
        }
    }

    /// <summary>The charts a connection is in, so the others in each can be told when it leaves.</summary>
    /// <param name="connectionId">The connection.</param>
    public IReadOnlyList<string> ChartsOf(string connectionId) =>
        [.. _byChart.Where(chart => chart.Value.Numbers.ContainsKey(connectionId)).Select(chart => chart.Key)];

    /// <summary>The name of a viewer of a chart ("Viewer 2"), or <see langword="null"/> when the connection is not in it.</summary>
    /// <param name="chartId">The chart.</param>
    /// <param name="connectionId">The connection.</param>
    public string? ViewerNameOf(string chartId, string connectionId) =>
        _byChart.TryGetValue(chartId, out var viewers) && viewers.Numbers.TryGetValue(connectionId, out int number)
            ? $"Viewer {number.ToString(System.Globalization.CultureInfo.InvariantCulture)}"
            : null;

    /// <inheritdoc />
    public bool HasSubscribers(string chartId) => Count(chartId) > 0;

    /// <inheritdoc />
    public int Count(string chartId)
        => _byChart.TryGetValue(chartId, out var viewers) ? viewers.Numbers.Count : 0;
}
