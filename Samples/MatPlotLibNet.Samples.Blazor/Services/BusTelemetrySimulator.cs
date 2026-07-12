// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.AspNetCore;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Dashboard;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Samples.Blazor.Services;

/// <summary>Hosted simulator that publishes fake bus/observability telemetry to the
/// <c>obs-dashboard</c> SignalR chart. Data is collected on a fixed cadence while the
/// display refresh interval can be changed (or paused) independently from the UI.</summary>
public sealed class BusTelemetrySimulator : BackgroundService
{
    /// <summary>SignalR chart id subscribed to by <see cref="Components.Pages.ObsDashboard"/>.</summary>
    public const string ChartId = "obs-dashboard";

    private readonly IChartPublisher _publisher;
    private readonly Random _rng = new();

    // Written from the UI circuit, read from the background loop: lock-free by design.
    private long _refreshIntervalTicks = TimeSpan.FromSeconds(2).Ticks;
    private volatile bool _isPaused;

    // Rolling telemetry window.
    private readonly TimeSpan _collectionTick = TimeSpan.FromMilliseconds(200);
    private readonly TimeSpan _historyWindow = TimeSpan.FromMinutes(2);
    private DateTime _startTime;
    private DateTime _lastCollect;

    private readonly List<(DateTime T, double V)> _publishRate = [];
    private readonly List<(DateTime T, double V)> _consumeRate = [];

    private readonly List<StateChange> _busStateLog = [];
    private readonly List<StateChange> _exchangeStateLog = [];

    private BusState _busState = BusState.Up;
    private BusState _exchangeState = BusState.Up;

    private double _messagesPerSecond;
    private double _lagSeconds;
    private int _activeConsumers;
    private double _errorsPerSecond;
    private double _droppedPerSecond;

    /// <summary>Creates the simulator using the registered chart publisher.</summary>
    public BusTelemetrySimulator(IChartPublisher publisher) => _publisher = publisher;

    /// <summary>Current display refresh interval. Changes are picked up by the background loop on its next tick.</summary>
    public TimeSpan RefreshInterval
    {
        get => TimeSpan.FromTicks(Interlocked.Read(ref _refreshIntervalTicks));
        set => Interlocked.Exchange(
            ref _refreshIntervalTicks,
            value > TimeSpan.Zero ? value.Ticks : TimeSpan.FromSeconds(2).Ticks);
    }

    /// <summary>When <c>true</c> the background loop keeps collecting data but stops publishing SVG updates.</summary>
    public bool IsPaused
    {
        get => _isPaused;
        set => _isPaused = value;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _startTime = DateTime.UtcNow;
        _lastCollect = _startTime;

        _busStateLog.Add(new StateChange(_startTime, BusState.Up));
        _exchangeStateLog.Add(new StateChange(_startTime, BusState.Up));

        // Seed the initial trend buffers so the first chart is not empty.
        CollectData(_startTime);
        await PublishAsync(stoppingToken);

        DateTime lastPublish = _startTime;

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;

            // Always collect telemetry at the fixed collection cadence.
            if (now - _lastCollect >= _collectionTick)
            {
                CollectData(now);
                _lastCollect = now;
            }

            // The deadline is re-derived from the CURRENT interval every tick, so shortening
            // the refresh rate takes effect immediately instead of after the old deadline.
            if (!IsPaused && now >= lastPublish + RefreshInterval)
            {
                await PublishAsync(stoppingToken);
                lastPublish = now;
            }

            await Task.Delay(50, stoppingToken);
        }
    }

    /// <summary>Publishes one frame. A failing frame is skipped, never fatal: the default
    /// <see cref="BackgroundServiceExceptionBehavior.StopHost"/> would otherwise tear down the whole sample app.</summary>
    private async Task PublishAsync(CancellationToken stoppingToken)
    {
        try
        {
            await _publisher.PublishSvgAsync(ChartId, BuildFigure(), stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[BusTelemetrySimulator] frame skipped: {ex.Message}");
        }
    }

    private void CollectData(DateTime now)
    {
        // Slowly drift the publish rate, then add occasional spikes.
        var baseRate = 2000 + 300 * Math.Sin((now - _startTime).TotalSeconds * 0.05);
        _messagesPerSecond = Math.Max(0, baseRate + _rng.NextDouble() * 200 + (_rng.NextDouble() < 0.05 ? _rng.NextDouble() * 800 : 0));

        _lagSeconds = 0.05 + _rng.NextDouble() * 0.35;
        if (_messagesPerSecond > 2400)
        {
            _lagSeconds += 0.2;
        }

        _activeConsumers = _rng.NextDouble() < 0.05 ? 11 : 12;
        if (_busState is BusState.Critical or BusState.Degraded)
        {
            // Drop far enough to cross the tile's warning (9) AND critical (<9) bands,
            // otherwise the red "consumers lost" accent can never render.
            _activeConsumers = Math.Max(5, _activeConsumers - _rng.Next(1, 6));
        }

        _errorsPerSecond = _rng.NextDouble() < 0.02 ? _rng.Next(1, 6) : 0;
        _droppedPerSecond = _errorsPerSecond > 0 ? _rng.NextDouble() * _errorsPerSecond * 2 : 0;

        _publishRate.Add((now, _messagesPerSecond));
        _consumeRate.Add((now, Math.Max(0, _messagesPerSecond - _droppedPerSecond)));

        TrimHistory(now);
        EvolveState(now, ref _busState, _busStateLog);
        EvolveState(now, ref _exchangeState, _exchangeStateLog);
    }

    private void TrimHistory(DateTime now)
    {
        var cutoff = now - _historyWindow;
        _publishRate.RemoveAll(p => p.T < cutoff);
        _consumeRate.RemoveAll(p => p.T < cutoff);
        TrimStateLog(_busStateLog, cutoff);
        TrimStateLog(_exchangeStateLog, cutoff);
    }

    private static void TrimStateLog(List<StateChange> log, DateTime cutoff)
    {
        int keepFrom = 0;
        for (int i = 0; i < log.Count - 1; i++)
        {
            if (log[i + 1].T > cutoff)
            {
                break;
            }

            keepFrom = i + 1;
        }

        if (keepFrom > 0)
        {
            log.RemoveRange(0, keepFrom);
        }
    }

    private void EvolveState(DateTime now, ref BusState state, List<StateChange> log)
    {
        // Bands must be ordered narrowest-first and disjoint: a wider bound tested first
        // swallows every narrower one behind it, which silently makes those states unreachable.
        var roll = _rng.NextDouble();
        var newState = state switch
        {
            BusState.Up => roll < 0.005 ? BusState.Unknown : roll < 0.025 ? BusState.Degraded : BusState.Up,
            BusState.Degraded => roll < 0.08 ? BusState.Critical : roll < 0.18 ? BusState.Up : BusState.Degraded,
            BusState.Critical => roll < 0.04 ? BusState.Up : roll < 0.16 ? BusState.Degraded : BusState.Critical,
            BusState.Unknown => roll < 0.20 ? BusState.Up : BusState.Unknown,
            _ => BusState.Up
        };

        if (newState != state)
        {
            state = newState;
            log.Add(new StateChange(now, state));
        }
    }

    private Figure BuildFigure()
    {
        var now = DateTime.UtcNow;

        var tiles = new OpsTile[]
        {
            new("Messages/s", _messagesPerSecond, "0"),
            new("Lag", _lagSeconds, "0.00' s'") { AccentThreshold = OpsTile.Threshold(null, Colors.Orange, Colors.Red, 0.30, 0.50) },
            new("Active", _activeConsumers, "0' of '12") { AccentThreshold = OpsTile.Threshold(Colors.Red, Colors.Orange, Colors.Tab10Green, 9, 12) },
            new("Errors/s", _errorsPerSecond, "0") { AccentThreshold = OpsTile.Threshold(null, Colors.Orange, Colors.Red, 1, 3) },
            new("Dropped/s", _droppedPerSecond, "0.0") { AccentThreshold = OpsTile.Threshold(null, Colors.Orange, Colors.Red, 1, 5) }
        };

        var timelines = new OpsStateTimeline[]
        {
            new("Service Bus", BuildSegments(_busStateLog, now)),
            new("Exchange", BuildSegments(_exchangeStateLog, now))
        };

        var trendLines = new OpsTrendLine[]
        {
            new("Publish", BuildX(_publishRate), BuildY(_publishRate)),
            new("Consume", BuildX(_consumeRate), BuildY(_consumeRate))
        };

        return FigureTemplates.OpsDashboard(
            tiles,
            timelines,
            trendLines,
            title: $"Bus Telemetry — {now:HH:mm:ss}")
            .WithTheme(Theme.Dark)
            .Build();
    }

    private IReadOnlyList<StateSegment> BuildSegments(List<StateChange> log, DateTime now)
    {
        var windowStart = now - _historyWindow;
        var segments = new List<StateSegment>();
        for (int i = 0; i < log.Count; i++)
        {
            var start = log[i].T;
            var end = i + 1 < log.Count ? log[i + 1].T : now;
            if (end <= windowStart)
            {
                continue;
            }

            if (start < windowStart)
            {
                start = windowStart;
            }

            var state = log[i].State;
            segments.Add(new StateSegment(
                (start - _startTime).TotalSeconds,
                (end - _startTime).TotalSeconds,
                state.ToString(),
                GetColor(state)));
        }
        return segments;
    }

    private double[] BuildX(List<(DateTime T, double V)> series) =>
        series.Select(p => (p.T - _startTime).TotalSeconds).ToArray();

    private static double[] BuildY(List<(DateTime T, double V)> series) =>
        series.Select(p => p.V).ToArray();

    private readonly record struct StateChange(DateTime T, BusState State);

    private enum BusState
    {
        Up,
        Degraded,
        Critical,
        Unknown
    }

    private static Color GetColor(BusState state) => state switch
    {
        BusState.Up => Colors.Tab10Green,
        BusState.Degraded => Colors.Orange,
        BusState.Critical => Colors.Red,
        BusState.Unknown => Colors.Gray,
        _ => Colors.Gray
    };
}
