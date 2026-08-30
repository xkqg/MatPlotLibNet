// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Samples.Blazor.Services;

/// <summary>One bus in the federation, with its processes.
///
/// <para><b>Everything in this file is the OBSERVABILITY layer, not the charting library.</b> The alarm
/// conditioning, the roll-up and the staleness clock decide what counts as broken — which is a judgement about
/// a domain, and a chart library that made it would be holding an opinion it has no business holding.</para></summary>
public sealed class Bus
{
    /// <summary>A threshold must be breached for this long before a state turns. One sample over the line is
    /// noise, and a wall that colours on noise teaches people to stop looking at it.</summary>
    private static readonly TimeSpan OnDelay = TimeSpan.FromSeconds(3);

    /// <summary>And it must be clear for this long before it stands down. An alarm that vanishes too eagerly
    /// lies exactly as badly as one that arrives too eagerly.</summary>
    private static readonly TimeSpan OffDelay = TimeSpan.FromSeconds(8);

    /// <summary>No heartbeat for this long and the source is UNKNOWN — regardless of what it last said.
    /// <para>This is the most important line in the file. A dashboard watches the bus THROUGH the bus: when a
    /// bus falls over, its tiles simply stop updating, and a naive screen keeps showing the last thing it heard
    /// — green — for hours. In a federation "I can no longer see you" is the most common failure and it is a
    /// DIFFERENT failure from "you are broken".</para></summary>
    private static readonly TimeSpan StaleAfter = TimeSpan.FromSeconds(5);

    private readonly List<Process> _processes = [];
    private readonly Conditioned _condition = new();

    private DateTime _lastHeartbeat = DateTime.MinValue;

    /// <summary>Creates a bus with the given number of processes.</summary>
    /// <param name="id">The bus identity.</param>
    /// <param name="processCount">How many processes it carries.</param>
    public Bus(string id, int processCount)
    {
        Id = id;
        for (int i = 0; i < processCount; i++)
        {
            _processes.Add(new Process($"{id}/proc-{i + 1:00}"));
        }
    }

    /// <summary>The bus identity.</summary>
    public string Id { get; }

    /// <summary>Its processes.</summary>
    public IReadOnlyList<Process> Processes => _processes;

    /// <summary>Advances the simulation by one tick.</summary>
    /// <param name="rng">The random source.</param>
    /// <param name="now">The current instant.</param>
    /// <param name="faulted">Whether a fault has been injected into the federation.</param>
    public void Evolve(Random rng, DateTime now, bool faulted)
    {
        // A silent bus stops sending heartbeats — which is how it becomes Unknown rather than staying green.
        bool silent = faulted && Id.EndsWith("lon-02", StringComparison.Ordinal);
        if (!silent)
        {
            _lastHeartbeat = now;
        }

        OpsState raw = faulted && Id.EndsWith("ams-02", StringComparison.Ordinal)
            ? OpsState.Degraded
            : OpsState.Normal;

        _condition.Observe(raw, now, OnDelay, OffDelay);

        foreach (var process in _processes)
        {
            process.Evolve(rng, now, faulted);
        }
    }

    /// <summary>The bus's own state, worst-child-wins across its processes — never an average. One sick
    /// process among twenty is precisely the thing you are looking for, and an average is precisely the
    /// operation that hides it.</summary>
    /// <param name="now">The current instant.</param>
    /// <returns>The rolled-up state.</returns>
    public OpsState State(DateTime now)
    {
        if (now - _lastHeartbeat > StaleAfter)
        {
            return OpsState.Unknown;
        }

        var worst = _condition.State;
        foreach (var process in _processes)
        {
            var state = process.State(now);
            if (state > worst)
            {
                worst = state;
            }
        }

        return worst;
    }

    /// <summary>The states of every process on this bus.</summary>
    /// <param name="now">The current instant.</param>
    /// <returns>One state per process.</returns>
    public IEnumerable<OpsState> ProcessStates(DateTime now)
    {
        bool stale = now - _lastHeartbeat > StaleAfter;
        return stale
            ? _processes.Select(_ => OpsState.Unknown)   // we cannot see the bus, so we cannot see its processes
            : _processes.Select(p => p.State(now));
    }
}

/// <summary>One process on a bus.</summary>
public sealed class Process
{
    /// <summary>How many load samples a process remembers — enough for a three-minute strip at the sample rate.</summary>
    public const int TrendLength = 60;

    private readonly Conditioned _condition = new();
    private readonly Queue<double> _loads = new(TrendLength);
    private readonly double _idle;

    /// <summary>Creates a process.</summary>
    /// <param name="id">Its identity.</param>
    public Process(string id)
    {
        Id = id;
        // Every process has its own resting load, so the wall is not twenty identical cells.
        _idle = 2 + (Math.Abs(id.GetHashCode(StringComparison.Ordinal)) % 900) / 100.0;
    }

    /// <summary>CPU, as a percentage of ONE core — the unit a process measures itself in (a value above 100
    /// means more than one core). Never divided by the machine's core count: that is how 6 % becomes an alarm.</summary>
    public double Load { get; private set; }

    /// <summary>The recent loads, oldest first — a small-multiples panel's line.</summary>
    public IReadOnlyList<double> LoadTrend => [.. _loads];

    /// <summary>The process identity.</summary>
    public string Id { get; }

    /// <summary>Advances the simulation by one tick.</summary>
    /// <param name="rng">The random source.</param>
    /// <param name="now">The current instant.</param>
    /// <param name="faulted">Whether a fault has been injected.</param>
    public void Evolve(Random rng, DateTime now, bool faulted)
    {
        OpsState raw = faulted && Id.Contains("ams-02/proc-03", StringComparison.Ordinal)
            ? OpsState.Critical
            : OpsState.Normal;

        _condition.Observe(raw, now,
            TimeSpan.FromSeconds(2),    // a critical raises faster than a warning
            TimeSpan.FromSeconds(8));

        // The faulted process pegs a core and a half; everyone else breathes around its resting load.
        double target = raw == OpsState.Critical ? 140 : _idle;
        Load = Math.Max(0, Load + (target - Load) * 0.25 + (rng.NextDouble() - 0.5) * 1.5);
        _loads.Enqueue(Load);
        while (_loads.Count > TrendLength)
        {
            _loads.Dequeue();
        }
    }

    /// <summary>Its conditioned state.</summary>
    /// <param name="now">The current instant.</param>
    /// <returns>The state.</returns>
    public OpsState State(DateTime now) => _condition.State;
}

/// <summary>Alarm conditioning: a raw reading becomes a state only after it has held.
///
/// <para>Three mechanisms, and all three are needed. The <b>on-delay</b> ignores the single sample that
/// crosses the line — noise, not a fault. The <b>off-delay</b> keeps the alarm up for a while after the value
/// comes back, because an alarm that clears too eagerly is as much a lie as one that fires too eagerly. And a
/// <b>deadband</b> (the caller's job: trip and clear at different levels) stops a value that sits exactly on
/// the threshold from making the whole wall chatter.</para>
///
/// <para>Without them a dashboard cries wolf, and after a week nobody looks at it — which is a worse failure
/// than having no dashboard at all, because now the operators believe they are covered.</para></summary>
internal sealed class Conditioned
{
    private OpsState _candidate = OpsState.Normal;
    private DateTime _since = DateTime.MinValue;

    /// <summary>The conditioned state.</summary>
    public OpsState State { get; private set; } = OpsState.Normal;

    /// <summary>Feeds one raw observation.</summary>
    /// <param name="raw">What the measurement says right now.</param>
    /// <param name="now">The current instant.</param>
    /// <param name="onDelay">How long a worsening must hold before it is believed.</param>
    /// <param name="offDelay">How long an improvement must hold before it is believed.</param>
    public void Observe(OpsState raw, DateTime now, TimeSpan onDelay, TimeSpan offDelay)
    {
        if (raw == State)
        {
            _candidate = raw;
            _since = now;
            return;
        }

        if (raw != _candidate)
        {
            _candidate = raw;
            _since = now;
        }

        var required = raw > State ? onDelay : offDelay;
        if (now - _since >= required)
        {
            State = raw;
        }
    }
}
