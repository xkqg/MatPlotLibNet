// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using MatPlotLibNet.AspNetCore;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Samples.Blazor.Services;

/// <summary>Simulates a federation of buses and publishes a control-room view of it.
///
/// <para><b>Where the alarm logic lives, and why it lives here.</b> The on-delay, the deadband, the worst-child
/// roll-up and the staleness clock are all in this file — in the sample, not in the charting library. A library
/// that decides when something counts as broken has started holding opinions about a domain it cannot see. It
/// supplies the FORM: the tile, the sparkline, the hatch, the pinned window. What that form MEANS is the
/// observability layer's business, and this simulator stands in for it.</para>
///
/// <para><b>Measurement and display are separate.</b> Collection runs at a fixed 250 ms and never waits for a
/// render; the publish pump runs on its own cadence and skips a beat rather than queueing up behind a slow
/// client. The operator can slow the charts down or freeze them — but never the tiles, because history may lag
/// and a warning may not.</para></summary>
public sealed class BusTelemetrySimulator : BackgroundService
{
    /// <summary>SignalR chart id for the throughput panel.</summary>
    public const string ThroughputChartId = "obs-throughput";

    /// <summary>SignalR chart id for the latency panel. Two ids, because one figure means one cadence, and
    /// these two panels are read at different rhythms.</summary>
    public const string LatencyChartId = "obs-latency";

    private static readonly TimeSpan CollectTick = TimeSpan.FromMilliseconds(250);
    private static readonly TimeSpan History = TimeSpan.FromHours(1);

    private readonly IChartPublisher _publisher;
    private readonly Random _rng = new(20260712);

    // Written from the UI circuit, read from the pump: lock-free by design.
    private long _refreshTicks = TimeSpan.FromMilliseconds(250).Ticks;
    private long _windowTicks = TimeSpan.FromMinutes(1).Ticks;
    private volatile bool _isPaused;

    // A publish that outruns the renderer must be dropped, not queued: a dashboard that is ten frames behind
    // is lying about the present. Interlocked, never a lock — this gate is contended by design.
    private int _inFlight;

    private readonly ConcurrentQueue<Sample> _samples = new();
    private readonly List<Bus> _buses = [];
    private volatile Snapshot _latest = Snapshot.Empty;

    private DateTime _lastCollect = DateTime.MinValue;
    private DateTime _lastPublish = DateTime.MinValue;

    /// <summary>Creates the simulator using the registered chart publisher.</summary>
    /// <param name="publisher">The chart publisher.</param>
    public BusTelemetrySimulator(IChartPublisher publisher)
    {
        _publisher = publisher;
        for (int i = 0; i < BusNames.Length; i++)
        {
            _buses.Add(new Bus(BusNames[i], 10 + (i % 11)));
        }
    }

    /// <summary>Display refresh interval for the CHARTS. The tiles are never throttled.</summary>
    public TimeSpan Refresh
    {
        get => TimeSpan.FromTicks(Interlocked.Read(ref _refreshTicks));
        set => Interlocked.Exchange(ref _refreshTicks, value > TimeSpan.Zero ? value.Ticks : CollectTick.Ticks);
    }

    /// <summary>How much history the charts show. Changing it re-buckets the same measurements — it never
    /// changes what is measured.</summary>
    public TimeSpan Window
    {
        get => TimeSpan.FromTicks(Interlocked.Read(ref _windowTicks));
        set => Interlocked.Exchange(ref _windowTicks, value.Ticks);
    }

    /// <summary>Freezes the charts. Collection continues; so do the tiles.</summary>
    public bool IsPaused
    {
        get => _isPaused;
        set => _isPaused = value;
    }

    /// <summary>Latches a fault until it is cleared. A fault that heals itself after thirty seconds cannot be
    /// inspected, and an operator who cannot inspect it learns to distrust the screen.</summary>
    public bool FaultInjected { get; set; }

    /// <summary>The current tile state, read by the page every tick. Immutable and swapped atomically, so the
    /// UI never reads a half-written snapshot.</summary>
    public Snapshot Latest => _latest;

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;

            // Collection writes state and returns. It never awaits a publish: a slow client must not be able
            // to stall the measurement of the thing it is watching.
            if (now - _lastCollect >= CollectTick)
            {
                _lastCollect = now;
                Collect(now);
            }

            if (!_isPaused && now - _lastPublish >= Refresh)
            {
                _lastPublish = now;
                PumpPublish(now, stoppingToken);
            }

            await Task.Delay(25, stoppingToken);
        }
    }

    /// <summary>Publishes one frame — unless the previous one is still rendering, in which case this beat is
    /// dropped. Fire-and-forget on purpose: the loop above must keep collecting while this runs.</summary>
    private void PumpPublish(DateTime now, CancellationToken ct)
    {
        if (Interlocked.CompareExchange(ref _inFlight, 1, 0) != 0)
        {
            return;   // the renderer is still busy; skip this beat rather than queue behind it
        }

        var window = Window;
        var samples = SamplesWithin(now - window);

        _ = Task.Run(async () =>
        {
            try
            {
                await _publisher.PublishSvgAsync(ThroughputChartId, BuildThroughput(now, window, samples), ct);
                await _publisher.PublishSvgAsync(LatencyChartId, BuildLatency(now, window, samples), ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                // shutting down
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[BusTelemetrySimulator] frame skipped: {ex.Message}");
            }
            finally
            {
                Interlocked.Exchange(ref _inFlight, 0);
            }
        }, ct);
    }

    // ── Measurement ──────────────────────────────────────────────────────────────────────────────

    private void Collect(DateTime now)
    {
        bool faulted = FaultInjected;

        double baseRate = 2400 + 220 * Math.Sin(now.Ticks / (double)TimeSpan.TicksPerSecond / 21);
        double publish = baseRate + (_rng.NextDouble() - 0.5) * 90;
        double consume = faulted ? Math.Min(publish, 2280) : publish * (0.995 + _rng.NextDouble() * 0.006);
        double drops = faulted ? 12 + _rng.NextDouble() * 8 : (_rng.NextDouble() < 0.06 ? _rng.NextDouble() * 6 : 0);
        double p50 = 4 + _rng.NextDouble() * 1.2 + (faulted ? 2 : 0);
        double p95 = 11 + _rng.NextDouble() * 3 + (faulted ? 10 : 0);
        double p99 = 19 + _rng.NextDouble() * 5 + (faulted ? 24 : 0);

        _samples.Enqueue(new Sample(now, publish, consume, drops, p50, p95, p99));
        while (_samples.TryPeek(out var oldest) && now - oldest.At > History)
        {
            _samples.TryDequeue(out _);
        }

        foreach (var bus in _buses)
        {
            bus.Evolve(_rng, now, faulted);
        }

        _latest = BuildSnapshot(now, publish, consume, drops, p99);
    }

    /// <summary>Rolls the fleet up into the six numbers a resting page shows.
    /// <para>The roll-up is <b>worst-child-wins</b>, never an average: one sick consumer among two hundred is
    /// exactly the thing you are looking for, and an average is precisely the operation that hides it.</para></summary>
    private Snapshot BuildSnapshot(DateTime now, double publish, double consume, double drops, double p99)
    {
        var busStates = _buses.Select(b => b.State(now)).ToArray();
        var procStates = _buses.SelectMany(b => b.ProcessStates(now)).ToArray();

        return new Snapshot(
            At: now,
            Buses: _buses.Count,
            BusesDeviating: busStates.Count(s => s != OpsState.Normal),
            BusState: Worst(busStates),
            Processes: procStates.Length,
            ProcessesDeviating: procStates.Count(s => s != OpsState.Normal),
            ProcessState: Worst(procStates),
            P99: p99,
            Backlog: Math.Max(0, publish - consume),
            Drops: drops,
            Alarms: procStates.Count(s => s == OpsState.Critical));
    }

    private static OpsState Worst(IEnumerable<OpsState> states) =>
        states.Aggregate(OpsState.Normal, (worst, s) => s > worst ? s : worst);

    private Sample[] SamplesWithin(DateTime from) => [.. _samples.Where(s => s.At >= from)];

    // ── Figures ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>Buckets the samples for the chosen window, honestly.
    /// <para>Rates are averaged, but keep their extremes, so a five-second burst survives a one-minute bucket.
    /// Percentiles carry their MAXIMUM — a p99 cannot be averaged, and averaging is exactly how a dashboard
    /// hides the spike you came to look for. Counters are summed.</para></summary>
    private static Bucket[] Bucketise(Sample[] samples, DateTime end, TimeSpan window)
    {
        if (samples.Length == 0)
        {
            return [];
        }

        long bucketTicks = Math.Max(TimeSpan.TicksPerSecond, window.Ticks / 200);
        return [.. samples
            .GroupBy(s => s.At.Ticks / bucketTicks)
            .OrderBy(g => g.Key)
            .Select(g => new Bucket(
                At: new DateTime(g.Key * bucketTicks, DateTimeKind.Utc),
                Publish: g.Average(s => s.Publish),
                PublishLow: g.Min(s => s.Publish),
                PublishHigh: g.Max(s => s.Publish),
                Consume: g.Average(s => s.Consume),
                Drops: g.Sum(s => s.Drops) / g.Count(),
                P50: g.Average(s => s.P50),
                P95: g.Max(s => s.P95),      // MAX, never mean
                P99: g.Max(s => s.P99)))];   // MAX, never mean
    }

    private Figure BuildThroughput(DateTime now, TimeSpan window, Sample[] samples)
    {
        var buckets = Bucketise(samples, now, window);
        double[] x = [.. buckets.Select(b => b.At.ToOADate())];

        return Plt.Create()
            .WithSize(760, 260)
            .WithTheme(Theme.OpsNight)
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.AxHSpan(2200, 2700, s => s.Alpha = 0.06);           // the learned normal band
                ax.FillBetween(x, [.. buckets.Select(b => b.PublishLow)],
                                  [.. buckets.Select(b => b.PublishHigh)],
                                  s => s.Alpha = 0.10);                 // the min/max envelope
                ax.Plot(x, [.. buckets.Select(b => b.Publish)], s => s.Label = "publish");
                ax.Plot(x, [.. buckets.Select(b => b.Consume)], s => s.Label = "consume");
                ax.Plot(x, [.. buckets.Select(b => b.Drops)], s => s.Label = "drops");
                PinWindow(ax, now, window);
                ax.SetYLabel("msg / s");
            })
            .Build();
    }

    private Figure BuildLatency(DateTime now, TimeSpan window, Sample[] samples)
    {
        var buckets = Bucketise(samples, now, window);
        double[] x = [.. buckets.Select(b => b.At.ToOADate())];

        return Plt.Create()
            .WithSize(520, 260)
            .WithTheme(Theme.OpsNight)
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.AxHSpan(0, 25, s => s.Alpha = 0.06);
                ax.Plot(x, [.. buckets.Select(b => b.P50)], s => s.Label = "p50");
                ax.Plot(x, [.. buckets.Select(b => b.P95)], s => s.Label = "p95");
                ax.Plot(x, [.. buckets.Select(b => b.P99)], s => s.Label = "p99");
                PinWindow(ax, now, window);
                ax.SetYLabel("ms");
            })
            .Build();
    }

    /// <summary>Pins the window: EXACT bounds, ROUND ticks. Rounding the bounds is what makes an axis stand
    /// still and then jump; pinning them is what makes it glide.</summary>
    private static void PinWindow(AxesBuilder ax, DateTime now, TimeSpan window)
    {
        ax.SetXLim((now - window).ToOADate(), now.ToOADate());
        ax.SetXDateFormat();
    }

    private static readonly string[] BusNames =
    [
        "synapse-ams-01", "synapse-ams-02", "synapse-rtd-01", "synapse-rtd-02", "synapse-fra-01",
        "synapse-fra-02", "synapse-lon-01", "synapse-lon-02", "synapse-par-01", "synapse-dub-01",
        "synapse-osl-01", "synapse-mad-01", "synapse-mil-01", "synapse-waw-01", "synapse-zrh-01"
    ];

    private readonly record struct Sample(
        DateTime At, double Publish, double Consume, double Drops, double P50, double P95, double P99);

    private readonly record struct Bucket(
        DateTime At, double Publish, double PublishLow, double PublishHigh,
        double Consume, double Drops, double P50, double P95, double P99);
}

/// <summary>The severity ladder. Ordered so that <c>worst-child-wins</c> is a plain comparison.</summary>
public enum OpsState
{
    /// <summary>Nothing to do. Wears no colour.</summary>
    Normal = 0,

    /// <summary>The source has gone silent — a gap in knowledge, not a fault. Wears a hatch, not a colour.</summary>
    Unknown = 1,

    /// <summary>Out of band, but nothing is lost yet.</summary>
    Degraded = 2,

    /// <summary>Failing now.</summary>
    Critical = 3
}

/// <summary>The six numbers a resting control-room page shows.
/// <para>A RECORD CLASS, not a struct, and that is the point: the collector swaps it in with a single reference
/// write, which is atomic. A struct would be copied field by field, and the UI could read a snapshot whose bus
/// count came from one tick and whose alarm count came from the next — a screen that shows a state the system
/// was never in.</para></summary>
/// <param name="At">When the snapshot was taken.</param>
/// <param name="Buses">Total buses in the federation.</param>
/// <param name="BusesDeviating">How many are not normal — the BREADTH, which is what makes an operator stand up.</param>
/// <param name="BusState">The worst state among them — the SEVERITY, which is what makes them hurry.</param>
/// <param name="Processes">Total processes.</param>
/// <param name="ProcessesDeviating">How many are not normal.</param>
/// <param name="ProcessState">The worst process state.</param>
/// <param name="P99">Control-path latency, 99th percentile.</param>
/// <param name="Backlog">Publish minus consume: the gap that becomes an outage.</param>
/// <param name="Drops">Telemetry messages lost per second.</param>
/// <param name="Alarms">Active critical alarms.</param>
public sealed record Snapshot(
    DateTime At,
    int Buses, int BusesDeviating, OpsState BusState,
    int Processes, int ProcessesDeviating, OpsState ProcessState,
    double P99, double Backlog, double Drops, int Alarms)
{
    /// <summary>An empty federation — what the page shows before the first measurement lands.</summary>
    public static Snapshot Empty { get; } = new(
        DateTime.MinValue, 0, 0, OpsState.Normal, 0, 0, OpsState.Normal, 0, 0, 0, 0);
}
